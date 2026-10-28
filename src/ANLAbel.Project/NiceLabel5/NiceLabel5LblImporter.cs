using ANLAbel.Core.Models;

namespace ANLAbel.Project.NiceLabel5;

/// <summary>
/// Creates a new ANLAbel template from verified NiceLabel 5 header facts.
/// Only the label stock size is carried over today; label objects inside
/// the <c>.lbl</c> file are not converted yet, so the result is an empty
/// template at the NiceLabel label size, named after the source file.
/// </summary>
public static class NiceLabel5LblImporter
{
    public static LabelTemplate CreateTemplate(NiceLabel5LblHeader header, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        return new LabelTemplate
        {
            Name = Path.GetFileNameWithoutExtension(sourcePath),
            WidthMm = header.LabelWidthHundredthsMm / 100.0,
            HeightMm = header.LabelHeightHundredthsMm / 100.0
        };
    }
}
