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
/// exposes them to the Template Library window.
/// </summary>
public sealed class TemplateLibraryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public IReadOnlyList<LibraryTemplateItem> Items { get; }

    public TemplateLibraryService()
    {
        Items = LoadItems();
    }

    /// <summary>Returns a fresh, editable copy of the template.</summary>
    public LabelTemplate Materialize(LibraryTemplateItem item)
    {
        return JsonSerializer.Deserialize<LabelTemplate>(item.Json, JsonOptions)
               ?? throw new InvalidDataException("Template could not be read.");
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
        return "Tem công nghiệp";
    }
}
