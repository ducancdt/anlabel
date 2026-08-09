using System.Text;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.Printing.PrinterProfiles;

public sealed class PrintPreflightValidator
{
    private readonly IBarcodeRenderer _barcodeRenderer;

    public PrintPreflightValidator()
        : this(new ZxingBarcodeRenderer())
    {
    }

    public PrintPreflightValidator(IBarcodeRenderer barcodeRenderer)
    {
        _barcodeRenderer = barcodeRenderer;
    }

    public PrintPreflightResult Validate(LabelTemplate template, IReadOnlyList<IReadOnlyDictionary<string, string>?> rows, int? printDpi = null)
    {
        var issues = new List<PrintPreflightIssue>();
        foreach (var item in template.Objects.Where(item => item.IsVisible))
        {
            ValidateObjectWithinLabel(template, item, issues);
            ValidateBarcodeModuleSizeAtPrintDpi(item, printDpi, issues);
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                ValidateBindingFieldsPresent(item, row, rowIndex, issues);
                var data = ResolveExpression(item, row);

                switch (item.Type)
                {
                    case ObjectType.Text:
                        ValidateTextWithinLabel(template, item, data, rowIndex, issues);
                        break;
                    case ObjectType.BarcodeCode128:
                    case ObjectType.QRCode:
                    case ObjectType.DataMatrix:
                        ValidateBarcode(item, data, rowIndex, issues);
                        break;
                    case ObjectType.TextBox:
                        ValidateTextBox(item, data, rowIndex, issues);
                        break;
                }
            }
        }

        return new PrintPreflightResult(issues);
    }

    /// <summary>
    /// Flags rows where a bound Excel column is missing (print-preview-reliability-plan R3).
    /// The plain "{Field}" syntax silently resolves missing fields to an empty string
    /// (<see cref="BindingExpressionEvaluator.Evaluate"/>), so without this check the
    /// object would print blank with no warning at all. Formula bindings already surface
    /// "field not found" via <see cref="FormulaEvaluationResult.Errors"/>, so those are
    /// simply forwarded instead of re-detected.
    /// </summary>
    private static void ValidateBindingFieldsPresent(LabelObject item, IReadOnlyDictionary<string, string>? row, int rowIndex, List<PrintPreflightIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(item.BindingExpression) || row is null)
        {
            return;
        }

        if (FormulaBindingEvaluator.LooksLikeFormula(item.BindingExpression))
        {
            var result = FormulaBindingEvaluator.Evaluate(item.BindingExpression, row);
            if (result.Errors.Count > 0)
            {
                issues.Add(new PrintPreflightIssue(
                    rowIndex + 1,
                    item.Name,
                    item.Type.ToString(),
                    $"Formula binding error: {string.Join("; ", result.Errors)}"));
            }
            return;
        }

        var missing = BindingExpressionEvaluator.GetFields(item.BindingExpression)
            .Where(field => !FieldNameResolver.TryGetValue(row, field, out _, out _))
            .ToArray();
        if (missing.Length > 0)
        {
            issues.Add(new PrintPreflightIssue(
                rowIndex + 1,
                item.Name,
                item.Type.ToString(),
                $"Missing field(s) in Excel data: {string.Join(", ", missing)}. This object will print blank for this row."));
        }
    }

    /// <summary>
    /// Warns when a fixed-size matrix barcode's module would print at under ~2 physical
    /// dots on the printer's actual DPI (print-preview-reliability-plan R5/item 8) —
    /// modules that small are unreliable to scan on industrial thermal printers. Only
    /// meaningful for <see cref="QrSizingMode.FixedVersionAndModuleSize"/>, where the
    /// module size in pixels is an explicit design choice rather than computed to fit
    /// the label. Row-independent (depends only on object config), so it is checked once
    /// per object rather than once per row.
    /// </summary>
    private static void ValidateBarcodeModuleSizeAtPrintDpi(LabelObject item, int? printDpi, List<PrintPreflightIssue> issues)
    {
        if (printDpi is null || printDpi <= 0 || item.QrDpi <= 0 || !item.IsSquare2DCodeLike() || item.QrSizingMode != QrSizingMode.FixedVersionAndModuleSize)
        {
            return;
        }

        var effectiveDots = item.QrModuleSizePx * (double)printDpi.Value / item.QrDpi;
        if (effectiveDots < 2)
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                $"Module is only ~{effectiveDots:0.#} dot(s) when printed at {printDpi} DPI — likely to fail scanning. Increase Module px, set QrDpi to match the printer, or use Auto size."));
        }
    }

    private static void ValidateObjectWithinLabel(LabelTemplate template, LabelObject item, List<PrintPreflightIssue> issues)
    {
        var bounds = GetObjectBoundsMm(item);
        if (bounds.Left >= 0
            && bounds.Top >= 0
            && bounds.Right <= template.WidthMm
            && bounds.Bottom <= template.HeightMm)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            0,
            item.Name,
            item.Type.ToString(),
            $"Object extends outside the design label bounds ({template.WidthMm:0.##} x {template.HeightMm:0.##} mm). Move it inside the label so preview and print do not clip it."));
    }

    private static void ValidateTextWithinLabel(LabelTemplate template, LabelObject item, string data, int rowIndex, List<PrintPreflightIssue> issues)
    {
        var text = TextBoxOverflowDetector.CreateFormattedText(item, string.IsNullOrEmpty(data) ? " " : data, System.Windows.Media.Brushes.Black);
        var xDip = MmConverter.MmToDip(item.XMm) + 2;
        var yDip = MmConverter.MmToDip(item.YMm) + Math.Max(0, (MmConverter.MmToDip(item.HeightMm) - text.Height) / 2);
        var rightDip = xDip + text.WidthIncludingTrailingWhitespace;
        var bottomDip = yDip + text.Height;
        var labelWidthDip = MmConverter.MmToDip(template.WidthMm);
        var labelHeightDip = MmConverter.MmToDip(template.HeightMm);
        if (xDip >= 0 && yDip >= 0 && rightDip <= labelWidthDip && bottomDip <= labelHeightDip)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            rowIndex + 1,
            item.Name,
            item.Type.ToString(),
            "Text extends outside the design label for this row. Shorten the data, reduce font size, move the object, or use Text Box so preview and print do not clip it."));
    }

    private void ValidateBarcode(LabelObject item, string data, int rowIndex, List<PrintPreflightIssue> issues)
    {
        var barcodeType = item.Type switch
        {
            ObjectType.QRCode => BarcodeType.QRCode,
            ObjectType.DataMatrix => BarcodeType.DataMatrix,
            _ => BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology)
        };

        var renderError = ValidateBarcodeCanRender(item, data, barcodeType);
        if (renderError is not null)
        {
            issues.Add(new PrintPreflightIssue(rowIndex + 1, item.Name, item.Type.ToString(), $"Invalid {barcodeType} data. {renderError}"));
            return;
        }

        if (item.Type == ObjectType.QRCode && item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize)
        {
            var byteCount = Encoding.UTF8.GetByteCount(data);
            var capacity = EstimateFixedQrCapacity(item.WidthMm, item.HeightMm, item.QrErrorCorrection);
            if (byteCount > capacity)
            {
                issues.Add(new PrintPreflightIssue(
                    rowIndex + 1,
                    item.Name,
                    item.Type.ToString(),
                    $"Fixed QR capacity exceeded: {byteCount} bytes for {item.WidthMm:0.#}x{item.HeightMm:0.#} mm, capacity about {capacity}. Increase size or use Auto size."));
            }
        }
    }

    private static void ValidateTextBox(LabelObject item, string data, int rowIndex, List<PrintPreflightIssue> issues)
    {
        if (TextBoxOverflowDetector.IsOverflowing(
                item,
                data,
                MmConverter.MmToDip(item.WidthMm),
                MmConverter.MmToDip(item.HeightMm)))
        {
            issues.Add(new PrintPreflightIssue(
                rowIndex + 1,
                item.Name,
                item.Type.ToString(),
                "Text box overflow. Increase object size or reduce text/font size."));
        }
    }

    private string? ValidateBarcodeCanRender(LabelObject item, string data, BarcodeType type)
    {
        if (!_barcodeRenderer.ValidateData(data, type))
        {
            return "Check empty text, unsupported characters, or required length.";
        }

        try
        {
            _barcodeRenderer.RenderBarcode(data, type, item.WidthMm, item.HeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return ex.Message;
        }
    }

    private static string ResolveExpression(LabelObject item, IReadOnlyDictionary<string, string>? row)
    {
        if (string.IsNullOrWhiteSpace(item.BindingExpression))
        {
            return item.Text;
        }

        if (row is null)
        {
            return item.BindingExpression;
        }

        return FormulaBindingEvaluator.LooksLikeFormula(item.BindingExpression)
            ? FormulaBindingEvaluator.Evaluate(item.BindingExpression, row).Value
            : BindingExpressionEvaluator.Evaluate(item.BindingExpression, row);
    }

    private static RectMm GetObjectBoundsMm(LabelObject item)
    {
        if (item.Type == ObjectType.Line)
        {
            var endXMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.XMm + item.WidthMm : item.LineEndXMm;
            var endYMm = item.LineEndXMm == 0 && item.LineEndYMm == 0 ? item.YMm + item.HeightMm : item.LineEndYMm;
            var strokePaddingMm = item.Style.OutlineStyle == OutlineStyle.None ? 0 : Math.Max(0, item.Style.BorderThicknessMm) / 2;
            return new RectMm(
                Math.Min(item.XMm, endXMm) - strokePaddingMm,
                Math.Min(item.YMm, endYMm) - strokePaddingMm,
                Math.Max(item.XMm, endXMm) + strokePaddingMm,
                Math.Max(item.YMm, endYMm) + strokePaddingMm);
        }

        var borderPaddingMm = item.Type is ObjectType.Rectangle or ObjectType.Ellipse
            && item.Style.OutlineStyle != OutlineStyle.None
            ? Math.Max(0, item.Style.BorderThicknessMm) / 2
            : 0;
        return new RectMm(
            item.XMm - borderPaddingMm,
            item.YMm - borderPaddingMm,
            item.XMm + item.WidthMm + borderPaddingMm,
            item.YMm + item.HeightMm + borderPaddingMm);
    }

    private static BarcodeRenderOptions CreateBarcodeRenderOptions(LabelObject item)
    {
        return new BarcodeRenderOptions
        {
            ErrorCorrection = item.QrErrorCorrection.ToString(),
            QuietZoneModules = item.QrQuietZoneModules
        };
    }

    private static int EstimateFixedQrCapacity(double widthMm, double heightMm, QrErrorCorrection errorCorrection)
    {
        var safeSize = Math.Max(1, Math.Min(widthMm, heightMm));
        var baseline = Math.Floor(safeSize * safeSize);
        var factor = errorCorrection switch
        {
            QrErrorCorrection.L => 1.15,
            QrErrorCorrection.M => 1.0,
            QrErrorCorrection.Q => 0.8,
            QrErrorCorrection.H => 0.65,
            _ => 1.0
        };

        return Math.Max(1, (int)Math.Floor(baseline * factor));
    }
}

public sealed record PrintPreflightIssue(int RowNumber, string ObjectName, string ObjectType, string Message)
{
    public string Summary => RowNumber <= 0
        ? $"Template, {ObjectName} ({ObjectType}): {Message}"
        : $"Row {RowNumber}, {ObjectName} ({ObjectType}): {Message}";
}

internal readonly record struct RectMm(double Left, double Top, double Right, double Bottom);

public sealed record PrintPreflightResult(IReadOnlyList<PrintPreflightIssue> Issues)
{
    public bool IsSuccess => Issues.Count == 0;

    public string ToUserMessage(int maxIssues = 5)
    {
        if (IsSuccess)
        {
            return "Print preflight passed.";
        }

        var lines = Issues.Take(maxIssues).Select(issue => $"- {issue.Summary}").ToList();
        if (Issues.Count > maxIssues)
        {
            lines.Add($"- ...and {Issues.Count - maxIssues} more issue(s).");
        }

        return $"Print blocked because label content is not safe to print:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
}
