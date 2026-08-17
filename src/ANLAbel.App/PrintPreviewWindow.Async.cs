using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ANLAbel.App.ViewModels;
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
        string description)
    {
        return await ValidateRowsForUiAsync(rows, description, plan: null);
    }

    private async Task<PrintPreflightResult> ValidateRowsForUiAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        string description,
        PrintRenderPlan? plan)
    {
        var operation = BeginPreviewOperation(description);
        try
        {
            var progress = CreatePreviewProgress(operation, description);
            return plan is null
                ? await _printService.ValidateRowsAsync(_template, rows, operation.Token, progress)
                : await _printService.ValidateRowsAsync(_template, rows, plan, operation.Token, progress);
        }
        finally
        {
            EndPreviewOperation(operation, keepStatus: false);
        }
    }

    private async Task RefreshPreviewAsync(bool rebuildTrackingRows)
    {
        var description = rebuildTrackingRows ? "Preparing preview" : "Checking selected labels";
        var operation = BeginPreviewOperation(description);
        var keepStatus = false;
        try
        {
            ApplyPrintSetup();
            operation.Token.ThrowIfCancellationRequested();

            IReadOnlyDictionary<string, string>?[] rows;
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
                rows = BuildExpandedRowsFromTracking(nextTrackingRows, allRows);
            }
            else
            {
                rows = BuildExpandedRowsFromTracking();
            }
            operation.Token.ThrowIfCancellationRequested();
            var planResolution = await ResolvePreviewPlanAsync(operation.Token);
            operation.Token.ThrowIfCancellationRequested();
            var preflight = await _printService.ValidateRowsAsync(
                _template,
                rows,
                planResolution.Plan,
                operation.Token,
                CreatePreviewProgress(operation, description));
            preflight = MergePreviewPlanIssue(preflight, planResolution.Issue);
            operation.Token.ThrowIfCancellationRequested();
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

            _previewRows = rows;
            _previewPlan = planResolution.Plan;
            _previewPlanPrinterName = _selectedPrinterName ?? string.Empty;
            _previewPlanIssue = planResolution.Issue;
            _preflightResult = preflight;
            BuildPreviewPageMetadata();
            _currentPageIndex = Math.Min(_currentPageIndex, Math.Max(0, Pages.Count - 1));
            _pageInput = Pages.Count == 0 ? "0" : (_currentPageIndex + 1).ToString();

            // Let the dispatcher process Cancel before the one current-page
            // bitmap is rasterized.  No batch of bitmaps is built here.
            await Dispatcher.Yield(DispatcherPriority.Background);
            operation.Token.ThrowIfCancellationRequested();
            await EnsureCurrentPreviewImageAsync(operation.Token, operation);
            OnPropertyChanged();
        }
        catch (OperationCanceledException)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                _previewProgressText = "Preview update canceled.";
                OnPropertyChanged();
            }
        }
        catch (Exception ex)
        {
            if (IsCurrentPreviewOperation(operation))
            {
                keepStatus = true;
                _previewProgressText = $"Preview update failed: {ex.Message}";
                OnPropertyChanged();
                System.Diagnostics.Debug.WriteLine($"Preview refresh error: {ex}");
            }
        }
        finally
        {
            EndPreviewOperation(operation, keepStatus);
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
            return new PreviewPlanResolution(_previewPlan, _previewPlanIssue);
        }

        if (string.IsNullOrWhiteSpace(_selectedPrinterName))
        {
            return new PreviewPlanResolution(
                designPlan,
                new PrintPreflightIssue(
                    0,
                    "Printer contract",
                    "Output",
                    "No printer queue is selected. The page below is design-only; choose a verified industrial queue before printing."));
        }

        try
        {
            // Printer APIs can block while a driver responds. Keep that work
            // off the dispatcher so large-label preview refreshes remain
            // cancelable and the window stays responsive.
            var effectivePlan = await Task.Run(
                () => _printService.CreateEffectivePlan(_template, _selectedPrinterName),
                cancellationToken);
            return new PreviewPlanResolution(effectivePlan, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new PreviewPlanResolution(
                designPlan,
                new PrintPreflightIssue(
                    0,
                    "Printer contract",
                    "Output",
                    $"The selected queue could not be validated. Preview is design-only until the driver contract succeeds: {ex.Message}"));
        }
    }

    private static PrintPreflightResult MergePreviewPlanIssue(
        PrintPreflightResult result,
        PrintPreflightIssue? planIssue)
    {
        if (planIssue is null)
        {
            return result;
        }

        return new PrintPreflightResult(new[] { planIssue }.Concat(result.Issues).ToArray());
    }

    private sealed record PreviewPlanResolution(
        PrintRenderPlan Plan,
        PrintPreflightIssue? Issue);

    private IProgress<PrintPreflightProgress> CreatePreviewProgress(CancellationTokenSource operation, string description)
    {
        // Construct Progress<T> on the dispatcher thread so status updates are
        // marshaled back before they touch WPF/DataContext.
        return new Progress<PrintPreflightProgress>(value =>
        {
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
        });
    }

    private CancellationTokenSource BeginPreviewOperation(string description)
    {
        _previewOperationCts?.Cancel();
        _previewOperationCts?.Dispose();
        var operation = new CancellationTokenSource();
        _previewOperationCts = operation;
        _isRefreshing = true;
        _isPreviewBusy = true;
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
            _previewProgressText = "Canceling...";
            OnPropertyChanged();
        }
    }

}
