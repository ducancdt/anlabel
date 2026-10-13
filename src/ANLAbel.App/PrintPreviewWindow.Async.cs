using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Printing;
using ANLAbel.Printing.PrinterProfiles;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.App;

public partial class PrintPreviewWindow
{
    // Keep the existing event-handler call sites small while routing all new
    // refresh requests through the cancelable implementation below.
    private void RefreshPreview() => StartPreviewRefresh();

    private void RefreshPreviewPagesOnly() => StartPreviewRefreshPagesOnly();

    private void CancelPreview_Click(object sender, RoutedEventArgs e)
    {
        CancelPreviewOperation();
    }

    private void StartPreviewRefresh()
    {
        _ = RefreshPreviewAsync(rebuildTrackingRows: true);
    }

    private void StartPreviewRefreshPagesOnly()
    {
        _ = RefreshPreviewAsync(rebuildTrackingRows: false);
    }

    private async Task<PrintPreflightResult> ValidateRowsForUiAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        IReadOnlyList<int> sourceRows,
        string description,
        PrintRenderPlan plan)
    {
        var operation = BeginPreviewOperation(description, invalidateContent: false);
        try
        {
            var progress = CreatePreviewProgress(operation, description);
            return await ValidateSelectedContentAsync(rows, sourceRows, plan, operation.Token, progress);
        }
        finally
        {
            EndPreviewOperation(operation, keepStatus: false);
        }
    }

    private async Task RefreshPreviewAsync(bool rebuildTrackingRows)
    {
        var description = rebuildTrackingRows ? "Đang chuẩn bị xem trước" : "Đang kiểm tra các nhãn đã chọn";
        var operation = BeginPreviewOperation(description);
        var keepStatus = false;
        try
        {
            ApplyPrintSetup();
            operation.Token.ThrowIfCancellationRequested();

            IReadOnlyDictionary<string, string>?[] rows;
            IReadOnlyList<LabelInstance> instances;
            IReadOnlyDictionary<string, string>?[]? refreshedAllRows = null;
            List<TrackingRowViewModel>? refreshedTrackingRows = null;
            List<string>? refreshedExcelColumns = null;
            if (rebuildTrackingRows)
            {
                var allRows = GetRows().ToArray();
                var excelColumns = CollectExcelColumns(allRows);
                var nextTrackingRows = new List<TrackingRowViewModel>(allRows.Length);
                var pageNumber = 1;
                var sourceRowNumber = 0;
                foreach (var row in allRows)
                {
                    operation.Token.ThrowIfCancellationRequested();
                    sourceRowNumber++;
                    var copies = GetCopiesFromRow(row);
                    nextTrackingRows.Add(new TrackingRowViewModel
                    {
                        RecordId = CreateSessionRecordId(sourceRowNumber),
                        SourceRowNumber = sourceRowNumber,
                        PageNumber = pageNumber,
                        IsSelected = true,
                        Copies = copies,
                        Col1 = GetPreviewCol(row, excelColumns, 0),
                        Col2 = GetPreviewCol(row, excelColumns, 1),
                        Col3 = GetPreviewCol(row, excelColumns, 2),
                        Col4 = GetPreviewCol(row, excelColumns, 3)
                    });
                    pageNumber += copies;
                }

                refreshedAllRows = allRows;
                refreshedTrackingRows = nextTrackingRows;
                refreshedExcelColumns = excelColumns;
                var prepared = BuildPreparedRowsFromTracking(nextTrackingRows, allRows);
                rows = prepared.Rows;
                instances = prepared.Instances;
            }
            else
            {
                var prepared = BuildPreparedRowsFromTracking(_trackingRows, GetRows().ToArray());
                rows = prepared.Rows;
                instances = prepared.Instances;
            }
            operation.Token.ThrowIfCancellationRequested();
            var planResolution = await ResolvePreviewPlanAsync(operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            var preflight = await ValidateSelectedContentAsync(
                rows,
                instances.Select(instance => instance.SourceRowNumber).ToArray(),
                planResolution.Plan,
                operation.Token,
                CreatePreviewProgress(operation, description));

            // Merge DB08 copies diagnostics from selected tracking rows (before
            // expansion). Invalid copies must block Ready even when expansion
            // dropped those rows by resolving copies to 0.
            var trackingForCopies = refreshedTrackingRows ?? _trackingRows;
            var rowsForCopies = refreshedAllRows ?? (_allRowsCache.Length > 0 ? _allRowsCache : GetRows().ToArray());
            if (!CopiesFieldContract.TryAuthorizePrepare(
                    _template.DatabaseConfig.CopiesField,
                    trackingForCopies
                        .Where(row => row.IsSelected)
                        .Select(row => (
                            row.SourceRowNumber,
                            row.SourceRowNumber > 0 && row.SourceRowNumber <= rowsForCopies.Length
                                ? rowsForCopies[row.SourceRowNumber - 1]
                                : null)),
                    out var copiesIssues)
                && copiesIssues.Count > 0)
            {
                var merged = preflight.Issues
                    .Concat(copiesIssues.Select(issue => new PrintPreflightIssue(
                        issue.SourceRowNumber,
                        issue.FieldName,
                        CopiesIssueObjectType,
                        issue.Message,
                        IsBlocking: true)))
                    .ToArray();
                preflight = new PrintPreflightResult(merged);
            }

            if (!planResolution.Plan.SceneCompilationVerified)
            {
                preflight = new PrintPreflightResult(preflight.Issues
                    .Concat(new[]
                    {
                        new PrintPreflightIssue(
                            0,
                            "Template",
                            "Design",
                            PreviewInitializationTrust.SceneBlockMessage(planResolution.Plan.SceneDiagnostics),
                            IsBlocking: true)
                    })
                    .ToArray());
            }

            var trustLevel = PreviewInitializationTrust.Combine(
                planResolution.TrustLevel,
                planResolution.Plan.SceneCompilationVerified,
                preflight.IsSuccess);
            operation.Token.ThrowIfCancellationRequested();

            // CreateEffectivePlanAsync / ValidateRowsAsync resume on the
            // threadpool when the window was opened before a
            // DispatcherSynchronizationContext exists (ctor + first Ctrl+P).
            // Apply every first-refresh UI mutation inside this callback —
            // do not continue after an empty hop and touch controls there.
            await Dispatcher.InvokeAsync(() =>
            {
                if (!IsCurrentPreviewOperation(operation))
                {
                    return;
                }

                if (refreshedAllRows is not null && refreshedTrackingRows is not null && refreshedExcelColumns is not null)
                {
                    _allRowsCache = refreshedAllRows;
                    _trackingRows.Clear();
                    _trackingRows.AddRange(refreshedTrackingRows);
                    TrackingList.ItemsSource = _trackingRows;
                    RowFilterColumnCombo.ItemsSource = new[] { AllColumnsFilterOption }.Concat(refreshedExcelColumns).ToList();
                    RowFilterColumnCombo.SelectedIndex = 0;
                    RowFilterBox.Text = string.Empty;
                    FilterMatchCountText.Visibility = Visibility.Collapsed;
                }

                // Only the winning operation mutates the live, data-bound tracking rows
                // (this fires PropertyChanged on the visible grid). A superseded/canceled
                // refresh returns above via the IsCurrentPreviewOperation guard and never
                // rewrites the displayed page numbers.
                ApplyTrackingPageNumbers(_trackingRows, instances);

                _previewRows = rows;
                _previewInstances = instances;
                _previewPlan = planResolution.Plan;
                _previewPlanPrinterName = _selectedPrinterName ?? string.Empty;
                _previewPlanIssue = planResolution.Issue;
                _previewTrustLevel = trustLevel;
                SetPreflightResult(preflight);
                BuildPreviewPageMetadata();
                _currentPageIndex = Math.Min(_currentPageIndex, Math.Max(0, Pages.Count - 1));
                _pageInput = Pages.Count == 0 ? "0" : (_currentPageIndex + 1).ToString();
            });

            operation.Token.ThrowIfCancellationRequested();
            if (!IsCurrentPreviewOperation(operation))
            {
                return;
            }

            await EnsureCurrentPreviewImageAsync(operation.Token, operation);
            await Dispatcher.InvokeAsync(OnPropertyChanged);
        }
        catch (OperationCanceledException)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                await Dispatcher.InvokeAsync(() =>
                {
                    _previewProgressText = "Đã hủy cập nhật xem trước.";
                    OnPropertyChanged();
                });
            }
        }
        catch (Exception ex)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                await Dispatcher.InvokeAsync(() =>
                {
                    _previewProgressText = "Cập nhật xem trước thất bại: " + ANLAbel.App.Services.OperationFailureExplainer.StatusLine(ex);
                    OnPropertyChanged();
                });
                System.Diagnostics.Debug.WriteLine($"Preview refresh error: {ex}");
            }
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => EndPreviewOperation(operation, keepStatus));
        }
    }

    private async Task<PreviewPlanResolution> ResolvePreviewPlanAsync(CancellationToken cancellationToken)
    {
        var designPlan = _printService.CreateDesignPlan(_template);
        if (_previewPlan is not null
            && _previewPlanIssue is null
            && string.Equals(_previewPlanPrinterName, _selectedPrinterName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(_previewPlan.DocumentHash, designPlan.DocumentHash, StringComparison.Ordinal))
        {
            return new PreviewPlanResolution(
                _previewPlan,
                _previewPlan.OutputContractTicketVerified
                    ? PreviewTrustLevel.EffectiveReady
                    : PreviewTrustLevel.EffectiveUnverified,
                _previewPlanIssue);
        }

        if (string.IsNullOrWhiteSpace(_selectedPrinterName))
        {
            return new PreviewPlanResolution(
                designPlan,
                PreviewTrustLevel.DesignOnly,
                new PrintPreflightIssue(
                    0,
                    "Máy in đã chọn",
                    "Output",
                    "Chưa chọn máy in. Bạn có thể xem nhãn và chọn dòng. Hãy chọn máy in trong Cài đặt máy in trước khi in."));
        }

        try
        {
            // PrintQueue/PrintTicket are STA COM. Task.Run is MTA and can
            // native-abort the process on industrial drivers. Use the same
            // dedicated-STA helper as print dispatch.
            var effectivePlan = await _printService.CreateEffectivePlanAsync(
                _template,
                _selectedPrinterName,
                cancellationToken);
            return new PreviewPlanResolution(
                effectivePlan,
                effectivePlan.OutputContractTicketVerified
                    ? PreviewTrustLevel.EffectiveReady
                    : PreviewTrustLevel.EffectiveUnverified,
                null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new PreviewPlanResolution(
                designPlan,
                PreviewTrustLevel.EffectiveBlocked,
                new PrintPreflightIssue(
                    0,
                    "Máy in đã chọn",
                    "Output",
                    $"Cài đặt máy in cần chú ý. Bản xem trước này sử dụng thiết kế nhãn, chưa xác thực đầu ra máy in. Hãy mở Cài đặt máy in để xem lại hàng đợi, khổ giấy và trình điều khiển. Chi tiết: {ex.Message}"));
        }
    }


    private sealed record PreviewPlanResolution(
        PrintRenderPlan Plan,
        PreviewTrustLevel TrustLevel,
        PrintPreflightIssue? Issue);

    private IProgress<PrintPreflightProgress> CreatePreviewProgress(CancellationTokenSource operation, string description)
    {
        // Progress may be raised from a worker. Always hop to this window's
        // dispatcher before touching DataContext.
        return new Progress<PrintPreflightProgress>(value => ApplyPreviewProgress(operation, description, value));
    }

    private void ApplyPreviewProgress(
        CancellationTokenSource operation,
        string description,
        PrintPreflightProgress value)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.BeginInvoke(() => ApplyPreviewProgress(operation, description, value));
            return;
        }

        if (!IsCurrentPreviewOperation(operation))
        {
            return;
        }

        if (value.Percent == _previewProgressPercent && value.CompletedUnits != value.TotalUnits)
        {
            return;
        }

        _previewProgressPercent = value.Percent;
        _previewProgressText = $"{description}: {value.Percent}%";
        OnPropertyChanged();
    }

    private CancellationTokenSource BeginPreviewOperation(string description, bool invalidateContent = true)
    {
        _previewOperationCts?.Cancel();
        _previewOperationCts?.Dispose();
        var operation = new CancellationTokenSource();
        _previewOperationCts = operation;
        _isRefreshing = true;
        _isPreviewBusy = true;
        if (invalidateContent)
        {
            _previewTrustLevel = PreviewTrustLevel.Stale;
        }
        _previewProgressPercent = 0;
        _previewProgressText = description;
        OnPropertyChanged();
        return operation;
    }

    private bool IsCurrentPreviewOperation(CancellationTokenSource operation)
    {
        return ReferenceEquals(_previewOperationCts, operation) && !operation.IsCancellationRequested;
    }

    private void EndPreviewOperation(CancellationTokenSource operation, bool keepStatus)
    {
        if (!ReferenceEquals(_previewOperationCts, operation))
        {
            return;
        }

        _previewOperationCts = null;
        _isRefreshing = false;
        _isPreviewBusy = false;
        _previewProgressPercent = keepStatus ? _previewProgressPercent : 0;
        if (!keepStatus)
        {
            _previewProgressText = string.Empty;
        }

        operation.Dispose();
        OnPropertyChanged();
    }

    private void CancelPreviewOperation()
    {
        _previewOperationCts?.Cancel();
        if (_isPreviewBusy)
        {
            _previewProgressText = "Đang hủy...";
            OnPropertyChanged();
        }
    }

}
