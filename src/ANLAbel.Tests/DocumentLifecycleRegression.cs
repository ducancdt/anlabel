using ANLAbel.App;
using ANLAbel.App.Services;
using ANLAbel.App.ViewModels;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Data;
using ANLAbel.Data.DataLogs;
using ANLAbel.Data.PrintLogs;
using ANLAbel.Project.SaveLoad;

/// <summary>
/// Plan 13 GĐ2 (F01–F07, D06): the file lifecycle must not lose an operator's
/// work. Each method is one named gate in Program.cs.
/// </summary>
internal static class DocumentLifecycleRegression
{
    public static async Task UnsavedMarkerFollowsEditsUndoAndSave()
    {
        var root = CreateRoot();
        try
        {
            var vm = new MainViewModel();
            Require(!vm.IsDirty && !vm.HasUnsavedChanges(), "A fresh document must start without unsaved changes.");
            Require(!vm.WindowTitle.Contains('*'), "A clean document title must not carry the unsaved marker.");

            vm.BeginTemplateEditGesture();
            vm.Template.Objects.Add(Rectangle("dirty-1", 5));
            vm.CommitTemplateEditGesture();
            Require(vm.IsDirty, "An edit must mark the document unsaved.");
            Require(vm.WindowTitle.Contains("* — ", StringComparison.Ordinal), "The window title must show the unsaved marker.");

            vm.UndoCommand.Execute(null);
            Require(!vm.IsDirty, "Undo back to the saved state must clear the unsaved marker.");
            vm.RedoCommand.Execute(null);
            Require(vm.IsDirty, "Redo away from the saved state must restore the unsaved marker.");

            var path = Path.Combine(root, "tem kho.anlabel");
            await vm.SaveAsync(path);
            Require(!vm.IsDirty && !vm.HasUnsavedChanges(), "Save must clear the unsaved marker.");
            Require(vm.WindowTitle.StartsWith("tem kho — ", StringComparison.Ordinal), "The title must name the saved file.");

            vm.Template.Objects.Single().XMm = 9;
            Require(vm.IsDirty && vm.HasUnsavedChanges(), "A property edit after saving must be unsaved again.");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public static async Task SaveWritesSnapshotAndRefusesConcurrentSave()
    {
        var root = CreateRoot();
        try
        {
            var files = new GatedProjectFileService();
            var vm = CreateViewModel(files, Path.Combine(root, "data-sources.json"));
            vm.Template.Objects.Add(Rectangle("snapshot-1", 4));
            vm.Template.Name = "Trước khi lưu";

            var firstSave = vm.SaveAsync(Path.Combine(root, "a.anlabel"));
            Require(vm.IsSaving, "The view model must report an active save.");
            var refused = false;
            try
            {
                await vm.SaveAsync(Path.Combine(root, "b.anlabel"));
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }

            Require(refused, "A second save while one is writing must be refused, not interleaved.");
            vm.Template.Name = "Sửa trong lúc lưu";
            files.Release();
            await firstSave;

            Require(files.Saved is not null && !ReferenceEquals(files.Saved, vm.Template), "Save must serialize a snapshot copy, never the live template.");
            Require(files.Saved!.Name == "Trước khi lưu", "An edit made while the file is written must not leak into it.");
            Require(vm.IsDirty, "An edit made during the write keeps the document unsaved.");
            Require(!vm.IsSaving, "The save flag must clear after the write.");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public static async Task RecoveryCopyIsOfferedOnceAndOpensUnsaved()
    {
        var root = CreateRoot();
        try
        {
            var recoveryDirectory = Path.Combine(root, "Recovered");
            var original = Path.Combine(root, "goc.anlabel");
            await new ProjectFileService().SaveAsync(new LabelTemplate { Name = "Gốc" }, original);
            var originalBytes = File.ReadAllBytes(original);

            var recovered = new LabelTemplate { Name = "Tem kho" };
            recovered.Objects.Add(Rectangle("recovered-1", 7));
            new DocumentRecoveryService(recoveryDirectory, 4242, 111, _ => null).Write(recovered, original, "Tem kho");
            var running = new DocumentRecoveryService(recoveryDirectory, 6161, 333, _ => null);
            running.Write(new LabelTemplate { Name = "Phiên khác" }, null, "Phiên khác");

            long? Liveness(int processId) => processId switch { 5151 => 222, 6161 => 333, _ => null };
            var current = new DocumentRecoveryService(recoveryDirectory, 5151, 222, Liveness);
            var orphans = current.FindOrphans();
            Require(orphans.Count == 1, "Only the copy of a session that is no longer running may be offered.");
            var entry = orphans[0];
            Require(entry.DocumentName == "Tem kho" && entry.OriginalFilePath == original, "The offer must name the document and its original file.");
            Require(!File.Exists(entry.DocumentPath + ".bak") && !Directory.Exists(Path.Combine(recoveryDirectory, ".session-4242-111.anlabel.revisions")),
                "A recovery copy must not create backups or revision archives.");

            var vm = new MainViewModel();
            await vm.OpenRecoveredAsync(entry.DocumentPath, entry.OriginalFilePath, entry.DocumentName);
            Require(vm.IsDirty, "A recovered document must open with unsaved changes.");
            Require(vm.CurrentFilePath == original, "Save must target the original file when it still exists.");
            Require(vm.Template.Objects.Count == 1 && vm.Template.Objects[0].Id == "recovered-1", "The recovered content must be the copy's content.");
            Require(File.ReadAllBytes(original).SequenceEqual(originalBytes), "Opening a recovery copy must not touch the original file.");

            DocumentRecoveryService.Delete(entry);
            Require(!File.Exists(entry.DocumentPath) && !File.Exists(entry.MetadataPath), "Deleting an offer must remove the copy and its metadata.");
            Require(current.FindOrphans().Count == 0, "A deleted copy must not be offered again.");

            current.Write(recovered, null, "Tem kho");
            current.DeleteSession();
            Require(!File.Exists(current.SessionDocumentPath), "A normal close must remove this session's copy.");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public static Task CorruptRegistryIsQuarantinedAtStartup()
    {
        var root = CreateRoot();
        try
        {
            var registryPath = Path.Combine(root, "data-sources.json");
            File.WriteAllText(registryPath, "{ not json");
            var vm = CreateViewModel(new ProjectFileService(), registryPath);
            Require(vm.StartupNotices.Count == 1, "A corrupt registry must produce one startup notice instead of a crash.");
            Require(!File.Exists(registryPath), "The corrupt registry must be moved aside.");
            Require(Directory.GetFiles(root, "data-sources.json.hong-*").Length == 1, "The corrupt registry must be kept for inspection.");
            Require(vm.DataSources.Count == 0, "The session must start with an empty registry.");
            return Task.CompletedTask;
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public static Task ExplorerArgumentOpensOnlyExistingDocuments()
    {
        var root = CreateRoot();
        try
        {
            var existing = Path.Combine(root, "mo tu explorer.anlabel");
            File.WriteAllText(existing, "{}");
            Require(MainWindow.FindDocumentArgument(["--log", Path.Combine(root, "missing.anlabel"), existing]) == existing,
                "The first existing .anlabel argument must be opened.");
            Require(MainWindow.FindDocumentArgument([Path.Combine(root, "note.txt")]) is null, "Other files must be ignored.");
            return Task.CompletedTask;
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public static async Task AsyncCommandFailureReachesTheOperator()
    {
        Exception? captured = null;
        var previous = AsyncRelayCommand.DefaultErrorHandler;
        AsyncRelayCommand.DefaultErrorHandler = exception => captured = exception;
        try
        {
            var command = new AsyncRelayCommand(() => throw new IOException("Tệp đang bị khóa"));
            await command.ExecuteAsync(null);
            Require(captured is IOException, "A command without its own error callback must report through the default handler.");
            Require(command.CanExecute(null), "A failed command must become executable again.");
        }
        finally
        {
            AsyncRelayCommand.DefaultErrorHandler = previous;
        }
    }

    public static async Task LibraryTemplatesRoundTripWithoutGeometryChange()
    {
        var root = CreateRoot();
        try
        {
            var service = new ProjectFileService();
            var library = Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "TemplateLibrary");
            var templates = Directory.GetFiles(library, "*.anlabel").OrderBy(path => path, StringComparer.Ordinal).ToArray();
            Require(templates.Length >= 20, "The template library must be present for the round-trip gate.");
            foreach (var file in templates)
            {
                var name = Path.GetFileName(file);
                var first = await service.LoadAsync(file);
                var a = Path.Combine(root, "a.anlabel");
                var b = Path.Combine(root, "b.anlabel");
                await service.SaveAsync(first, a);
                await service.SaveAsync(await service.LoadAsync(a), b);
                Require(File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b)), $"{name}: load/save must be byte-stable.");

                // Through the editor: open, save, reopen. Authored geometry and
                // text are data and must survive unchanged (AGENTS.md invariant 8).
                var vm = new MainViewModel();
                await vm.OpenAsync(file);
                var c = Path.Combine(root, "c.anlabel");
                await vm.SaveAsync(c);
                var reopened = await service.LoadAsync(c);
                Require(reopened.Objects.Count == first.Objects.Count, $"{name}: the editor must keep every object.");
                foreach (var original in first.Objects)
                {
                    var saved = reopened.Objects.Single(item => item.Id == original.Id);
                    Require(saved.XMm == original.XMm && saved.YMm == original.YMm && saved.WidthMm == original.WidthMm && saved.HeightMm == original.HeightMm && saved.Rotation == original.Rotation,
                        $"{name}/{original.Name}: open and save must not move or resize the object.");
                    Require(saved.Text == original.Text && saved.BindingExpression == original.BindingExpression,
                        $"{name}/{original.Name}: open and save must keep text and binding.");
                }

                Require(!vm.IsDirty, $"{name}: a document must be clean right after it was saved.");
            }
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public static async Task EditsWhileTheLinkedDataLoadsStayUnsaved()
    {
        var root = CreateRoot();
        try
        {
            var data = Path.Combine(root, "big.csv");
            var content = new System.Text.StringBuilder("Code,Qty\n");
            for (var row = 0; row < 400_000; row++)
            {
                content.Append('P').Append(row).Append(",1\n");
            }

            await File.WriteAllTextAsync(data, content.ToString());
            var templatePath = Path.Combine(root, "linked.anlabel");
            var template = new LabelTemplate { Name = "Đang mở", WidthMm = 50, HeightMm = 30 };
            template.Objects.Add(Rectangle("edit-me", 5));
            template.DatabaseConfig.FilePath = data;
            template.DatabaseConfig.SheetName = ANLAbel.Data.Excel.ExcelDataService.CsvSheetName;
            await new ProjectFileService().SaveAsync(template, templatePath);

            // A link that only refreshes keeps the opened document clean.
            var clean = new MainViewModel();
            await clean.OpenAsync(templatePath);
            Require(!clean.IsDirty, "Opening a linked template must not mark it changed.");

            // Edit once the data restore has started. Waiting only for the new
            // template raced the open on a pool thread (this runner has no UI
            // dispatcher): an edit before the document was marked clean became
            // part of the "as read" state. In the app the open's synchronous
            // part runs on the UI thread, so no edit can land there.
            var vm = new MainViewModel();
            var restoreStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.HasLinkedExcelSource))
                {
                    restoreStarted.TrySetResult();
                }
            };
            var opening = vm.OpenAsync(templatePath);
            await restoreStarted.Task.WaitAsync(TimeSpan.FromSeconds(20));

            Require(!opening.IsCompleted, "The fixture must edit while the linked data is still loading.");
            vm.Template.Objects.Single().XMm = 9;
            await opening;
            Require(vm.IsDirty, "An edit made while the linked data loaded must stay unsaved.");

            var lifecycle = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "MainWindow.DocumentLifecycle.cs")).Replace("\r\n", "\n");
            var closed = lifecycle[lifecycle.IndexOf("private void MainWindow_Closed(", StringComparison.Ordinal)..];
            closed = closed[..closed.IndexOf("private void AutosaveTimer_Tick(", StringComparison.Ordinal)];
            Require(closed.Contains("if (_closeConfirmed)", StringComparison.Ordinal) && closed.Contains("TryWriteRecoveryNow()", StringComparison.Ordinal),
                "Closing without a confirmed answer must keep the edits as a recovery copy.");
            Require(lifecycle.Contains("if (TryWriteRecoveryNow())", StringComparison.Ordinal),
                "An opened recovery copy may be deleted only after this session wrote its own.");
            var app = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "App.xaml.cs"));
            Require(app.Contains("OnSessionEnding", StringComparison.Ordinal) && app.Contains("PrepareForSessionEnd()", StringComparison.Ordinal),
                "Windows logoff must keep unsaved edits as a recovery copy.");
            var update = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "ANLAbel.App", "UpdateWindow.xaml.cs"));
            Require(update.IndexOf("PrepareForUpdateInstallAsync()", StringComparison.Ordinal) is > 0 and var ask
                    && update.IndexOf("AppUpdateService.LaunchInstaller(", StringComparison.Ordinal) > ask,
                "The update must ask to save before the installer closes ANLAbel.");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    public static async Task SavingOverAnotherWindowsSaveAsksFirst()
    {
        var root = CreateRoot();
        try
        {
            var path = Path.Combine(root, "shared.anlabel");
            var service = new ProjectFileService();
            var original = new LabelTemplate { Name = "Chung", WidthMm = 50, HeightMm = 30 };
            original.Objects.Add(Rectangle("shared-object", 5));
            await service.SaveAsync(original, path);

            var mine = new MainViewModel();
            await mine.OpenAsync(path);
            var theirs = new MainViewModel();
            await theirs.OpenAsync(path);

            theirs.Template.Objects.Single().XMm = 20;
            await theirs.SaveAsync(path);

            mine.Template.Objects.Single().XMm = 30;
            DocumentChangedOnDiskException? conflict = null;
            try
            {
                await mine.SaveAsync(path);
            }
            catch (DocumentChangedOnDiskException ex)
            {
                conflict = ex;
            }

            Require(conflict is not null, "Saving over a file another window saved must ask first.");
            Require((await service.LoadAsync(path)).Objects.Single().XMm == 20, "The other window's save must stay until the operator decides.");

            await mine.SaveAsync(path, overwriteExternalChanges: true);
            Require((await service.LoadAsync(path)).Objects.Single().XMm == 30, "Overwrite, once chosen, writes this window's version.");
            await mine.SaveAsync(path);
            Require(!mine.IsDirty, "A normal save after the overwrite needs no confirmation.");
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static MainViewModel CreateViewModel(IProjectFileService files, string registryPath)
    {
        return new MainViewModel(
            files,
            new ANLAbel.Data.Excel.ExcelDataService(),
            new ANLAbel.Printing.PrinterProfiles.PrintService(),
            new ANLAbel.Printing.PrinterProfiles.PrinterDiscoveryService(),
            new PrintLogService(),
            new DataOperationLogService(),
            new DataSourceRegistry(registryPath));
    }

    private static LabelObject Rectangle(string id, double x)
    {
        return new LabelObject { Id = id, Type = ObjectType.Rectangle, Name = id, XMm = x, YMm = 5, WidthMm = 10, HeightMm = 6 };
    }

    private static string CreateRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "anlabel-lifecycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteRoot(string root)
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Temp cleanup is best effort.
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ANLAbel.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Holds a save open until released, to observe the in-flight state.</summary>
    private sealed class GatedProjectFileService : IProjectFileService
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public LabelTemplate? Saved { get; private set; }

        public void Release() => _gate.TrySetResult();

        public async Task SaveAsync(LabelTemplate template, string filePath, CancellationToken cancellationToken = default)
        {
            Saved = template;
            await _gate.Task.ConfigureAwait(false);
        }

        public Task<LabelTemplate> LoadAsync(string filePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<ProjectLoadResult> LoadWithRecoveryAsync(string filePath, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
