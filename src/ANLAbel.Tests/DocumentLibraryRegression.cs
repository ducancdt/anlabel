using ANLAbel.App.ViewModels;

internal static class DocumentLibraryRegression
{
    public static async Task Run()
    {
        var missing = new DocumentLibraryViewModel(() => Path.Combine(Path.GetTempPath(), "anlabel-missing-root-" + Guid.NewGuid()));
        await missing.RefreshAsync();
        Require(missing.Items.Count == 0 && missing.RootStatus.Contains("not configured", StringComparison.OrdinalIgnoreCase), "Unavailable root must stay explicit rather than silently falling back.");

        var root = Path.Combine(Path.GetTempPath(), "anlabel-library-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "valid.anlabel"), "{}");
            File.WriteAllText(Path.Combine(root, "legacy.json"), "{}");
            File.WriteAllText(Path.Combine(root, "ignored.txt"), "ignored");
            Directory.CreateDirectory(Path.Combine(root, "nested"));
            File.WriteAllText(Path.Combine(root, "nested", "not-scanned.anlabel"), "{}");
            var vm = new DocumentLibraryViewModel(() => root);
            await vm.RefreshAsync();
            Require(vm.Items.Count == 2, "Library must list only supported top-level local files.");
            vm.SearchText = "valid";
            Require(vm.Items.Single().RelativePath == "valid.anlabel", "Search must operate on relative local identity.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }

        var xamlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ANLAbel.App", "DocumentLibraryWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);
        foreach (var id in new[] { "CC.P3.Library.Root", "CC.P3.Library.RootStatus", "CC.P3.Library.FileResults", "CC.P3.Library.Revisions" }) Require(xaml.Contains(id, StringComparison.Ordinal), $"Library must expose '{id}'.");
        foreach (var forbidden in new[] { "Workflow", "CheckOut", "Approve", "License" }) Require(!xaml.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Library must not invent '{forbidden}'.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
