using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.App.TemplateLibrary;

/// <summary>One entry in the built-in template gallery.</summary>
public sealed class LibraryTemplateItem
{
    public required string ResourceName { get; init; }
    public required string Json { get; init; }
    public required LabelTemplate Template { get; init; }
    public required string TypeText { get; init; }
    public required string Group { get; init; }

    public string Name => Template.Name;
    public string SizeText => $"{Template.WidthMm:0.##} × {Template.HeightMm:0.##} mm";
}

/// <summary>
/// Loads the label templates that ship embedded inside the application and
/// exposes them to the Template Library window. Also unpacks the bundled
/// sample Excel workbook to a writable location so bound templates work
/// out-of-the-box on any machine.
/// </summary>
public sealed class TemplateLibraryService
{
    private const string SampleExcelResourceSuffix = ".TemplateLibrary.sample-data.xlsx";
    private const string SampleExcelFileName = "sample-data.xlsx";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string SampleDataPath { get; }
    public IReadOnlyList<LibraryTemplateItem> Items { get; }

    public TemplateLibraryService()
    {
        SampleDataPath = ExtractSampleData();
        Items = LoadItems();
    }

    /// <summary>Returns a fresh, editable copy of the template with its Excel link repaired.</summary>
    public LabelTemplate Materialize(LibraryTemplateItem item)
    {
        var template = JsonSerializer.Deserialize<LabelTemplate>(item.Json, JsonOptions)
                       ?? throw new InvalidDataException("Template could not be read.");

        var linked = template.DatabaseConfig.FilePath;
        if (!string.IsNullOrWhiteSpace(linked) &&
            string.Equals(Path.GetFileName(linked), SampleExcelFileName, StringComparison.OrdinalIgnoreCase))
        {
            template.DatabaseConfig.FilePath = SampleDataPath;
        }

        return template;
    }

    private static string ExtractSampleData()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ANLAbel");
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, SampleExcelFileName);

        var asm = Assembly.GetExecutingAssembly();
        var resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(SampleExcelResourceSuffix, StringComparison.OrdinalIgnoreCase));
        if (resName is not null)
        {
            try
            {
                using var rs = asm.GetManifestResourceStream(resName)!;
                using var fs = File.Create(target);
                rs.CopyTo(fs);
            }
            catch
            {
                // If the file is locked (already open) keep the existing copy.
            }
        }
        return target;
    }

    private static List<LibraryTemplateItem> LoadItems()
    {
        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames()
            .Where(n => n.Contains(".TemplateLibrary.", StringComparison.Ordinal) &&
                        n.EndsWith(".anlabel", StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new List<LibraryTemplateItem>();
        foreach (var name in names)
        {
            try
            {
                using var stream = asm.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var template = JsonSerializer.Deserialize<LabelTemplate>(json, JsonOptions);
                if (template is null)
                {
                    continue;
                }

                items.Add(new LibraryTemplateItem
                {
                    ResourceName = name,
                    Json = json,
                    Template = template,
                    TypeText = DescribeType(template),
                    Group = DescribeGroup(name)
                });
            }
            catch
            {
                // Skip any template that fails to parse rather than breaking the gallery.
            }
        }
        return items;
    }

    private static string DescribeType(LabelTemplate t)
    {
        var hasBarcode = t.Objects.Any(o => o.Type == ObjectType.BarcodeCode128);
        var has2D = t.Objects.Any(o => o.Type is ObjectType.QRCode or ObjectType.DataMatrix);
        return (hasBarcode, has2D) switch
        {
            (true, true) => "Mã vạch + QR",
            (true, false) => "Mã vạch",
            (false, true) => "QR / 2D",
            _ => "Chỉ chữ"
        };
    }

    private static string DescribeGroup(string resourceName)
    {
        // file names like "...TemplateLibrary.20_sample_..." belong to the sample set
        var file = resourceName.Split('.').Reverse().Skip(1).FirstOrDefault() ?? resourceName;
        return file.StartsWith("2", StringComparison.Ordinal) && file.Contains("sample", StringComparison.OrdinalIgnoreCase)
            ? "Mẫu DELTEC / Jabil"
            : "Tem công nghiệp";
    }
}
