using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Project.NiceLabel5;

/// <summary>
/// Creates a new ANLAbel template from verified NiceLabel 5 header facts.
/// The label stock size is carried over, plus every object record the
/// reader could fully cross-check (barcodes, fixed texts, paragraphs and
/// rectangle/line shapes); other objects inside the <c>.lbl</c> file are
/// not converted. The template keeps the label name stored inside the
/// .lbl header when one was saved, and falls back to the source file
/// name when the stored name is empty.
/// </summary>
public static class NiceLabel5LblImporter
{
    public static LabelTemplate CreateTemplate(
        NiceLabel5LblHeader header,
        string sourcePath,
        IReadOnlyList<NiceLabel5BarcodeRecord>? barcodes = null,
        IReadOnlyList<NiceLabel5TextRecord>? texts = null,
        IReadOnlyList<NiceLabel5ShapeRecord>? shapes = null,
        IReadOnlyList<NiceLabel5TextRecord>? paragraphs = null)
    {
        ArgumentNullException.ThrowIfNull(header);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        var template = new LabelTemplate
        {
            Name = string.IsNullOrWhiteSpace(header.Name)
                ? Path.GetFileNameWithoutExtension(sourcePath)
                : header.Name.Trim(),
            WidthMm = header.LabelWidthHundredthsMm / 100.0,
            HeightMm = header.LabelHeightHundredthsMm / 100.0
        };

        foreach (var record in barcodes ?? [])
        {
            var symbology = record.InternalSymbologyId switch
            {
                1 => BarcodeSymbology.Ean13,
                3 => BarcodeSymbology.UpcA,
                5 => BarcodeSymbology.ITF,
                6 or 33 => BarcodeSymbology.Code39,
                _ => (BarcodeSymbology?)null
            };
            if (symbology is null)
            {
                continue;
            }

            var item = new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                BarcodeSymbology = symbology.Value,
                Text = record.VariableName is null ? record.Data : string.Empty,
                XMm = record.LeftHundredthsMm / 100.0,
                YMm = record.TopHundredthsMm / 100.0,
                WidthMm = (record.RightHundredthsMm - record.LeftHundredthsMm) / 100.0,
                HeightMm = (record.BottomHundredthsMm - record.TopHundredthsMm) / 100.0
            };
            if (record.VariableName is { } variableName)
            {
                item.BindingExpression = "{?" + variableName + "}";
                if (!template.PromptVariables.Any(v => string.Equals(v.Name, variableName, StringComparison.Ordinal)))
                {
                    template.PromptVariables.Add(new PromptVariableDefinition { Name = variableName });
                }
            }

            template.Objects.Add(item);
        }

        foreach (var record in texts ?? [])
        {
            AddTextLike(template, record, ObjectType.Text);
        }

        // A NiceLabel paragraph owns its frame and wraps inside it, which
        // is exactly the ANLAbel TextBox contract; the stored frame is
        // carried over once, at import, and content never resizes it.
        foreach (var record in paragraphs ?? [])
        {
            AddTextLike(template, record, ObjectType.TextBox);
        }

        foreach (var record in shapes ?? [])
        {
            template.Objects.Add(new LabelObject
            {
                Type = record.IsLine ? ObjectType.Line : ObjectType.Rectangle,
                XMm = record.LeftHundredthsMm / 100.0,
                YMm = record.TopHundredthsMm / 100.0,
                WidthMm = (record.RightHundredthsMm - record.LeftHundredthsMm) / 100.0,
                HeightMm = (record.BottomHundredthsMm - record.TopHundredthsMm) / 100.0
            });
        }

        return template;
    }

    private static void AddTextLike(LabelTemplate template, NiceLabel5TextRecord record, ObjectType type)
    {
        var item = new LabelObject
        {
            Type = type,
            Text = record.Content,
            XMm = record.LeftHundredthsMm / 100.0,
            YMm = record.TopHundredthsMm / 100.0,
            WidthMm = (record.RightHundredthsMm - record.LeftHundredthsMm) / 100.0,
            HeightMm = (record.BottomHundredthsMm - record.TopHundredthsMm) / 100.0,
            Style =
            {
                FontFamily = record.FontFamily,
                Bold = record.IsBold,
                Alignment = record.Anchor switch
                {
                    NiceLabel5HorizontalAnchor.Center => TextAlignmentMode.Center,
                    NiceLabel5HorizontalAnchor.Right => TextAlignmentMode.Right,
                    _ => TextAlignmentMode.Left
                }
            }
        };
        if (record.FontSizePt > 0)
        {
            item.Style.FontSizePt = record.FontSizePt;
        }

        if (record.VariableName is { } variableName)
        {
            var runStart = record.Content.IndexOf('?');
            var runEnd = record.Content.LastIndexOf('?');
            item.BindingExpression =
                record.Content[..runStart] + "{?" + variableName + "}" + record.Content[(runEnd + 1)..];
            item.Text = record.Content[..runStart] + record.Content[(runEnd + 1)..];
            if (!template.PromptVariables.Any(v => string.Equals(v.Name, variableName, StringComparison.Ordinal)))
            {
                template.PromptVariables.Add(new PromptVariableDefinition { Name = variableName });
            }
        }

        template.Objects.Add(item);
    }
}
