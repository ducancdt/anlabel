using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ANLAbel.Core.Models;

namespace ANLAbel.Project.SaveLoad;

public sealed class ProjectFileService : IProjectFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task SaveAsync(LabelTemplate template, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, template, JsonOptions, cancellationToken);
    }

    public async Task<LabelTemplate> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        await using var stream = File.OpenRead(filePath);
        var template = await JsonSerializer.DeserializeAsync<LabelTemplate>(stream, JsonOptions, cancellationToken);
        return template ?? throw new InvalidDataException("The template file is empty or invalid.");
    }
}
