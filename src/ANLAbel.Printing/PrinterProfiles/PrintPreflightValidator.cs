using System.IO;
using System.Security.Cryptography;
using System.Text;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Expressions;
using ANLAbel.Core.Expressions.Formulas;
using ANLAbel.Core.Geometry;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;
using ANLAbel.Printing.RenderPipeline;

namespace ANLAbel.Printing.PrinterProfiles;

public sealed class PrintPreflightValidator
{
    private static readonly IQrCapacityProvider QrCapacityProvider = new QrCapacityTable();
    private readonly IBarcodeRenderer _barcodeRenderer;

    public PrintPreflightValidator()
        : this(new ZxingBarcodeRenderer())
    {
    }

    public PrintPreflightValidator(IBarcodeRenderer barcodeRenderer)
    {
        _barcodeRenderer = barcodeRenderer;
    }

    public PrintPreflightResult Validate(
        LabelTemplate template,
        IReadOnlyList<IReadOnlyDictionary<string, string>?> rows,
        int? printDpi = null,
        int? printDpiY = null,
        CancellationToken cancellationToken = default,
        IProgress<PrintPreflightProgress>? progress = null)
    {
        var issues = new List<PrintPreflightIssue>();
        var visibleItems = template.Objects.Where(item => item.IsVisible).ToArray();
        var totalUnits = Math.Max(1, visibleItems.Length * Math.Max(1, rows.Count));
        var completedUnits = 0;
        var reportStride = Math.Max(1, totalUnits / 100);
        progress?.Report(new PrintPreflightProgress(0, totalUnits));
        ValidateLabelStock(template, issues);
        ValidatePrintDpi(template, issues);
        ValidatePrintScale(template, issues);
        ValidatePrintMethod(template, issues);

        foreach (var item in visibleItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidateObjectWithinLabel(template, item, issues);
            ValidateTextFont(item, issues);
            ValidateImage(item, printDpi, printDpiY ?? printDpi, issues);
            ValidateBarcodeModuleSizeAtPrintDpi(item, printDpi, printDpiY ?? printDpi, issues);
            ValidateLinearBarcodeModuleAtPrintDpi(item, printDpi, printDpiY ?? printDpi, issues);
            ValidateBarcodeApplicationGeometry(item, issues);
            ValidateCode39RatioAndQuietZone(item, printDpi, printDpiY ?? printDpi, issues);
            ValidateBearerBars(item, issues);
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var row = rows[rowIndex];
                ValidateBindingFieldsPresent(item, row, rowIndex, issues);
                var data = ResolveExpression(item, row);
                ValidateTextGlyphCoverage(item, data, rowIndex, issues);

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

                completedUnits++;
                if (completedUnits % reportStride == 0)
                {
                    progress?.Report(new PrintPreflightProgress(completedUnits, totalUnits));
                }
            }
        }

        progress?.Report(new PrintPreflightProgress(totalUnits, totalUnits));
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
    private static void ValidateBarcodeModuleSizeAtPrintDpi(LabelObject item, int? printDpiX, int? printDpiY, List<PrintPreflightIssue> issues)
    {
        if (printDpiX is null || printDpiX <= 0 || printDpiY is null || printDpiY <= 0 || item.QrDpi <= 0 || !item.IsSquare2DCodeLike() || item.QrSizingMode != QrSizingMode.FixedVersionAndModuleSize)
        {
            return;
        }

        var effectiveDotsX = item.QrModuleSizePx * (double)printDpiX.Value / item.QrDpi;
        var effectiveDotsY = item.QrModuleSizePx * (double)printDpiY.Value / item.QrDpi;
        if (effectiveDotsX < 2 || effectiveDotsY < 2)
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                $"Module is only ~{effectiveDotsX:0.#}×{effectiveDotsY:0.#} dot(s) when printed at {printDpiX}×{printDpiY} DPI — likely to fail scanning. Increase Module px, set QrDpi to match the printer, or use Auto size."));
        }
    }

    /// <summary>
    /// Industrial 1D module (X-dimension) check at print-plan DPI. Uses the authored
    /// <see cref="LabelObject.BarcodeModuleWidthMm"/> when set; otherwise estimates
    /// module width from the object frame and the encoded module count. Sub-2-dot
    /// modules and values below the industrial X floor are reported (not silently
    /// stretched). Row-independent when X is authored; when derived, uses empty
    /// data-free geometry only for fixed text samples via the vector encode.
    /// </summary>
    private void ValidateLinearBarcodeModuleAtPrintDpi(
        LabelObject item,
        int? printDpiX,
        int? printDpiY,
        List<PrintPreflightIssue> issues)
    {
        if (printDpiX is null || printDpiX <= 0 || printDpiY is null || printDpiY <= 0)
        {
            return;
        }

        if (item.Type is not ObjectType.BarcodeCode128 || item.IsSquare2DCodeLike())
        {
            return;
        }

        var dpi = Math.Min(printDpiX.Value, printDpiY.Value);
        LinearBarcodeModuleResolution resolution;
        try
        {
            if (item.BarcodeModuleWidthMm > 0)
            {
                resolution = LinearBarcodeModuleContract.Resolve(item.BarcodeModuleWidthMm, dpi);
            }
            else
            {
                // Legacy: derive module from frame / pure logical module count.
                // Do NOT use frame-scaled RenderBarcodeVector.WidthModules — that is
                // pixel columns after stretch (~1 printer dot per column), which
                // falsely fails every comfortable frame-owned 1D barcode.
                var barcodeType = BarcodeTypeMapper.ToRendererType(item.BarcodeSymbology);
                if (!_barcodeRenderer.ValidateData(item.Text ?? string.Empty, barcodeType)
                    && string.IsNullOrEmpty(item.BindingExpression))
                {
                    // Bound rows are validated per-row; static empty data cannot estimate modules.
                    return;
                }

                var sample = string.IsNullOrEmpty(item.Text) ? "0" : item.Text;
                if (!_barcodeRenderer.ValidateData(sample, barcodeType))
                {
                    return;
                }

                var options = CreateBarcodeRenderOptions(item);
                var logicalModules = _barcodeRenderer.CountLinearModules(sample, barcodeType, options);
                if (logicalModules is null or <= 0)
                {
                    return;
                }

                resolution = LinearBarcodeModuleContract.ResolveForObject(
                    authoredModuleWidthMm: 0,
                    frameWidthMm: item.WidthMm,
                    totalModules: logicalModules.Value,
                    dpi: dpi);
            }
        }
        catch
        {
            return;
        }

        if (!resolution.HasIndustrialRisk)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            0,
            item.Name,
            item.Type.ToString(),
            LinearBarcodeModuleContract.FormatIndustrialRiskMessage(resolution)));
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

    private static void ValidateTextFont(LabelObject item, List<PrintPreflightIssue> issues)
    {
        if (item.Type is not (ObjectType.Text or ObjectType.TextBox)
            || string.IsNullOrWhiteSpace(item.Style.FontFamily)
            || TextBoxOverflowDetector.IsFontAvailable(item.Style.FontFamily))
        {
            return;
        }

        var requested = item.Style.FontFamily.Trim();
        var fallback = TextBoxOverflowDetector.ResolveFontFamilyName(requested);
        issues.Add(new PrintPreflightIssue(
            0,
            item.Name,
            item.Type.ToString(),
            $"Font '{requested}' is not installed on this machine. Preview and print will use '{fallback}', so install the font or choose an approved installed family before production printing."));
    }

    private static void ValidateImage(LabelObject item, int? printDpiX, int? printDpiY, List<PrintPreflightIssue> issues)
    {
        if (item.Type != ObjectType.Image)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(item.ImageDataBase64))
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                "Image has no embedded data. Replace the image before production printing."));
            return;
        }

        if (!ImageRasterContract.IsSupported(item.ImageRasterMode))
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                $"Image raster mode '{item.ImageRasterMode}' is not supported by this build. Choose DriverManaged or an approved monochrome mode before production printing."));
            return;
        }

        byte[] bytes;
        try
        {
            // Refuse obviously oversized payloads before WPF allocates a decoder
            // buffer. Templates remain portable, but a corrupt/hostile image
            // cannot freeze the preflight worker with an unbounded allocation.
            const int maxEncodedBytes = 64 * 1024 * 1024;
            const int maxBase64Characters = ((maxEncodedBytes + 2) / 3) * 4;
            if (item.ImageDataBase64.Length > maxBase64Characters)
            {
                throw new InvalidDataException("embedded image payload is larger than 64 MB");
            }
            bytes = Convert.FromBase64String(item.ImageDataBase64);
            if (bytes.Length == 0 || bytes.Length > maxEncodedBytes)
            {
                throw new InvalidDataException("embedded image payload is empty or larger than 64 MB");
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or ArgumentException)
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                $"Image data is not a valid embedded bitmap ({ex.Message}). Reinsert the image before printing."));
            return;
        }

        try
        {
            // Decode through the same versioned transform used by the designer
            // and print presenter. This makes a monochrome/alpha policy an
            // evidence-bearing contract instead of a driver-side surprise.
            var bitmap = ImageRasterizer.Decode(item.ImageDataBase64, item.ImageRasterMode)
                ?? throw new InvalidDataException("the configured raster policy could not decode the image");

            // Decode dimensions are attacker-controlled even when the encoded
            // payload is small (a highly-compressed PNG can otherwise allocate
            // gigabytes on the preflight worker). Keep the production path
            // bounded before measuring the image against the printer grid.
            const long maxDecodedPixels = 64_000_000;
            var decodedPixels = (long)bitmap.PixelWidth * bitmap.PixelHeight;
            if (decodedPixels <= 0 || decodedPixels > maxDecodedPixels)
            {
                throw new InvalidDataException("decoded image is empty or larger than 64 megapixels");
            }

            if ((item.ImagePixelWidth > 0 && item.ImagePixelWidth != bitmap.PixelWidth)
                || (item.ImagePixelHeight > 0 && item.ImagePixelHeight != bitmap.PixelHeight))
            {
                throw new InvalidDataException(
                    $"stored source dimensions {item.ImagePixelWidth}x{item.ImagePixelHeight} do not match decoded {bitmap.PixelWidth}x{bitmap.PixelHeight}");
            }

            var payloadFingerprint = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(item.ImageDataBase64)));
            var identity = ImageRasterContract.Describe(
                payloadFingerprint,
                item.ImageDataBase64.Length,
                item.ImageRasterMode,
                bitmap.PixelWidth,
                bitmap.PixelHeight);
            if (!identity.IsValid)
            {
                throw new InvalidDataException("image raster identity is incomplete");
            }

            var observation = ImageResolutionContract.Observe(
                bitmap.PixelWidth,
                bitmap.PixelHeight,
                item.WidthMm,
                item.HeightMm);
            var dpiX = printDpiX.GetValueOrDefault();
            var dpiY = printDpiY.GetValueOrDefault();
            if (dpiX > 0
                && dpiY > 0
                && !observation.MeetsDeviceGrid(dpiX, dpiY))
            {
                issues.Add(new PrintPreflightIssue(
                    0,
                    item.Name,
                    item.Type.ToString(),
                    $"Image source density is only {observation.EffectivePpiX:0.#}×{observation.EffectivePpiY:0.#} PPI in its {item.WidthMm:0.##}×{item.HeightMm:0.##} mm frame, below the effective {dpiX}×{dpiY} DPI printer grid. Use a higher-resolution image or reduce the frame before production printing."));
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                $"Image could not be decoded for printing ({ex.Message}). Reinsert the image before production printing."));
        }
    }

    private static void ValidateTextGlyphCoverage(LabelObject item, string data, int rowIndex, List<PrintPreflightIssue> issues)
    {
        if (item.Type is not (ObjectType.Text or ObjectType.TextBox))
        {
            return;
        }

        var observation = TextBoxOverflowDetector.ObserveFont(item, data);
        // A missing family already has a template-level diagnostic. Do not
        // duplicate it with glyphs measured against the deterministic fallback.
        if (!observation.RequestedFamilyAvailable
            || !observation.GlyphMapAvailable
            || !observation.HasMissingGlyphs)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            rowIndex + 1,
            item.Name,
            item.Type.ToString(),
            $"Font '{observation.RequestedFamily}' has no glyph(s) for {observation.MissingGlyphSummary}. Install a family covering this data or choose an approved font before production printing."));
    }

    private static void ValidateTextWithinLabel(LabelTemplate template, LabelObject item, string data, int rowIndex, List<PrintPreflightIssue> issues)
    {
        if (TextBoxOverflowDetector.ShouldConstrainToBox(item))
        {
            ValidateTextBox(item, data, rowIndex, issues);
            return;
        }

        var value = string.IsNullOrEmpty(data) ? " " : data;
        var widthDip = MmConverter.MmToDip(item.WidthMm);
        var heightDip = MmConverter.MmToDip(item.HeightMm);
        double textWidth;
        double textHeight;
        TextLayoutMetrics metrics;
        if (TextBoxOverflowDetector.HasExplicitLineHeight(item))
        {
            var layout = TextBoxOverflowDetector.CreateTextLayout(item, value, widthDip, heightDip, constrainToBox: false, System.Windows.Media.Brushes.Black);
            metrics = layout.Metrics;
            textWidth = metrics.WidthDip;
            textHeight = metrics.HeightDip;
        }
        else
        {
            var text = TextBoxOverflowDetector.CreateFormattedText(item, value, System.Windows.Media.Brushes.Black);
            TextBoxOverflowDetector.ApplyLayoutBounds(text, item, widthDip, heightDip, constrainToBox: false);
            metrics = TextBoxOverflowDetector.Measure(text, item, widthDip, heightDip, constrainToBox: false, sourceValue: value);
            textWidth = text.WidthIncludingTrailingWhitespace;
            textHeight = text.Height;
        }

        // Keep this check on the exact shared text policy used by the designer
        // and print renderer: explicit center/right text is bounded to the
        // alignment frame, while left-aligned static text remains auto-sized.
        var contentWidth = TextBoxOverflowDetector.GetContentWidthDip(item, widthDip, constrainToBox: false);
        var xDip = MmConverter.MmToDip(item.XMm) + Math.Max(0, (widthDip - contentWidth) / 2);
        var yDip = MmConverter.MmToDip(item.YMm) + metrics.VerticalOffsetDip;
        var rightDip = item.Style.Alignment == ANLAbel.Core.Enums.TextAlignmentMode.Left
            ? xDip + textWidth
            : xDip + contentWidth;
        var bottomDip = yDip + textHeight;
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
        var applicationSymbology = GetEffectiveBarcodeSymbology(item);
        var applicationErrors = BarcodeApplicationContract.ValidateData(
            item.BarcodeApplicationProfile,
            applicationSymbology,
            data);
        if (applicationErrors.Count > 0)
        {
            issues.Add(new PrintPreflightIssue(
                rowIndex + 1,
                item.Name,
                item.Type.ToString(),
                string.Join(" ", applicationErrors)));
            return;
        }

        foreach (var checkDigitError in BarcodeCheckDigitContract.Validate(
                     applicationSymbology,
                     data,
                     item.BarcodeCheckDigitPolicy))
        {
            issues.Add(new PrintPreflightIssue(rowIndex + 1, item.Name, item.Type.ToString(), checkDigitError));
        }

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

        ValidateMatrixFrameForRow(item, data, rowIndex, issues);

        if (item.Type == ObjectType.QRCode && item.QrSizingMode == QrSizingMode.FixedVersionAndModuleSize)
        {
            var byteCount = Encoding.UTF8.GetByteCount(data);
            var version = item.QrFixedVersion;
            var capacity = QrVersionHelper.IsValidVersion(version)
                ? QrCapacityProvider.GetByteModeCapacity(version, item.QrErrorCorrection)
                : 0;
            if (!QrVersionHelper.IsValidVersion(version)
                || !QrCapacityProvider.CanEncodeByteMode(data, version, item.QrErrorCorrection))
            {
                issues.Add(new PrintPreflightIssue(
                    rowIndex + 1,
                    item.Name,
                    item.Type.ToString(),
                    $"Fixed QR capacity exceeded: {byteCount} UTF-8 byte(s) for version {version} / {item.QrErrorCorrection}, capacity {capacity}. Choose a larger version, reduce data, or use Auto size."));
            }
        }
    }

    private static void ValidateMatrixFrameForRow(
        LabelObject item,
        string data,
        int rowIndex,
        List<PrintPreflightIssue> issues)
    {
        if (!item.IsSquare2DCodeLike())
        {
            return;
        }

        // Auto-size is an authoring aid, but a bound row must never silently
        // shrink its modules into an undersized authored frame at print time.
        // Resolve without max-size clamping so this remains a per-row blocking
        // check rather than a second mutation of document geometry.
        var requiredSizeMm = QrObjectGeometryContract.ResolveTargetSizeMm(item, data);
        if (requiredSizeMm is null)
        {
            return;
        }

        var authoredSizeMm = Math.Min(item.WidthMm, item.HeightMm);
        if (requiredSizeMm.Value <= authoredSizeMm + QrObjectGeometryContract.SizeToleranceMm)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            rowIndex + 1,
            item.Name,
            item.Type.ToString(),
            $"2D barcode frame is too small for this row: requires at least {requiredSizeMm.Value:0.##} mm but the authored frame is {authoredSizeMm:0.##} mm. Increase the frame or choose a fixed module/version that fits."));
    }

    private static void ValidateTextBox(LabelObject item, string data, int rowIndex, List<PrintPreflightIssue> issues)
    {
        if (TextBoxOverflowDetector.ShouldBlockOverflow(item)
            && TextBoxOverflowDetector.IsOverflowing(
                item,
                data,
                MmConverter.MmToDip(item.WidthMm),
                MmConverter.MmToDip(item.HeightMm)))
        {
            issues.Add(new PrintPreflightIssue(
                rowIndex + 1,
                item.Name,
                item.Type.ToString(),
                item.Type == ObjectType.Text
                    ? "Fixed text frame overflow. Increase the frame or reduce text/font size."
                    : "Text box overflow. Increase object size or reduce text/font size."));
        }
    }

    private string? ValidateBarcodeCanRender(LabelObject item, string data, BarcodeType type)
    {
        if (!_barcodeRenderer.ValidateData(data, type))
        {
            return "Check empty text, unsupported characters, or required length.";
        }

        var hriLayout = BarcodeHriTextLayout.Measure(
            type,
            data,
            item.WidthMm,
            item.HeightMm,
            item.BarcodeHriPlacement,
            item.BarcodeTextFontSizePt);
        if (!hriLayout.IsValid)
        {
            return hriLayout.ErrorMessage;
        }

        try
        {
            var symbolHeightMm = hriLayout.IsEnabled ? hriLayout.SymbolHeightMm : item.HeightMm;
            _barcodeRenderer.RenderBarcode(data, type, item.WidthMm, symbolHeightMm, item.QrDpi, CreateBarcodeRenderOptions(item));
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
            var lineBounds = LineBoundsContract.GetBounds(
                item.XMm,
                item.YMm,
                endXMm,
                endYMm,
                item.Style.OutlineStyle,
                item.Style.BorderThicknessMm);
            return new RectMm(
                lineBounds.Left,
                lineBounds.Top,
                lineBounds.Right,
                lineBounds.Bottom);
        }

        var borderPaddingMm = item.Type is ObjectType.Rectangle or ObjectType.Ellipse
            && item.Style.OutlineStyle != OutlineStyle.None
            ? Math.Max(0, item.Style.BorderThicknessMm) / 2
            : 0;
        var transformed = TransformedBoundsContract.GetBounds(item);
        return new RectMm(
            transformed.Left - borderPaddingMm,
            transformed.Top - borderPaddingMm,
            transformed.Right + borderPaddingMm,
            transformed.Bottom + borderPaddingMm);
    }

    private static BarcodeRenderOptions CreateBarcodeRenderOptions(LabelObject item)
    {
        return new BarcodeRenderOptions
        {
            ErrorCorrection = item.QrErrorCorrection.ToString(),
            QuietZoneModules = item.QrQuietZoneModules,
            IsGs1 = item.BarcodeApplicationProfile == BarcodeApplicationProfile.Gs1,
            Code39WideNarrowRatio = item.Code39WideNarrowRatio,
            BearerBarStyle = item.BearerBarStyle,
            BearerBarThicknessMm = item.BearerBarThicknessMm
        };
    }

    private static void ValidateBearerBars(LabelObject item, List<PrintPreflightIssue> issues)
    {
        if (item.BearerBarStyle == BearerBarStyle.None)
        {
            return;
        }

        if (item.IsSquare2DCodeLike())
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                "Bearer bars are only supported on 1D linear barcodes (such as ITF-14, Code 128, Code 39), not 2D matrix symbols. Set BearerBarStyle to None."));
            return;
        }

        if (item.BearerBarThicknessMm < 0.2 || item.BearerBarThicknessMm > 5.0)
        {
            issues.Add(new PrintPreflightIssue(
                0,
                item.Name,
                item.Type.ToString(),
                $"Bearer bar thickness is {item.BearerBarThicknessMm:0.##} mm, outside the standard range of 0.2 to 5.0 mm."));
        }
    }

    private static void ValidateCode39RatioAndQuietZone(
        LabelObject item,
        int? printDpiX,
        int? printDpiY,
        List<PrintPreflightIssue> issues)
    {
        if (item.Type is not ObjectType.BarcodeCode128 || item.BarcodeSymbology != BarcodeSymbology.Code39)
        {
            return;
        }

        var dpi = printDpiX.HasValue && printDpiX.Value > 0 ? printDpiX.Value : 300;
        var effectiveX = item.BarcodeModuleWidthMm > 0
            ? LinearBarcodeModuleContract.Resolve(item.BarcodeModuleWidthMm, dpi).EffectiveModuleWidthMm
            : 0;

        if (item.Code39WideNarrowRatio != Code39WideNarrowRatio.LegacyEngineDefault)
        {
            if (effectiveX > 0 && !Code39RatioContract.IsLegal(item.Code39WideNarrowRatio, effectiveX))
            {
                issues.Add(new PrintPreflightIssue(
                    0,
                    item.Name,
                    item.Type.ToString(),
                    $"Code 39 wide:narrow ratio 2.0:1 requires an effective X-dimension of at least {Code39RatioContract.Ratio2MinimumXmm:0.###} mm (current effective X is {effectiveX:0.###} mm at {dpi} DPI). Choose a ratio of 2.2:1 or higher, or increase the module width."));
            }
        }

        if (effectiveX > 0)
        {
            var requiredQzMm = Code39RatioContract.RequiredQuietZoneMmPerSide(effectiveX);
            var observedQzMm = Code39RatioContract.ObservedQuietZoneMmPerSide(
                item.QrQuietZoneModules,
                LinearBarcodeModuleContract.Resolve(item.BarcodeModuleWidthMm, dpi));
            if (observedQzMm + 1e-6 < requiredQzMm)
            {
                issues.Add(new PrintPreflightIssue(
                    0,
                    item.Name,
                    item.Type.ToString(),
                    $"Code 39 quiet zone is {observedQzMm:0.##} mm per side ({item.QrQuietZoneModules} modules), which is below the required standard minimum of {requiredQzMm:0.##} mm (at least max(10X, 2.54 mm)). Increase quiet zone modules to at least {Math.Ceiling(requiredQzMm / effectiveX)}."));
            }
        }
    }

    private static void ValidateLabelStock(LabelTemplate template, List<PrintPreflightIssue> issues)
    {
        var decision = LabelStockContract.Evaluate(
            template.WidthMm,
            template.HeightMm,
            template.PrinterProfile.PhysicalWidthMm,
            template.PrinterProfile.PhysicalHeightMm,
            template.PrinterProfile.PaperName);
        if (decision.IsAllowed)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            0,
            "Template",
            "LabelStock",
            decision.Diagnostic));
    }

    private static void ValidatePrintDpi(LabelTemplate template, List<PrintPreflightIssue> issues)
    {
        var decision = IndustrialPrintDpiContract.Evaluate(
            template.PrinterProfile.Dpi,
            template.Dpi);
        if (decision.IsAllowed)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            0,
            "Template",
            "PrintDpi",
            decision.Diagnostic));
    }

    private static void ValidatePrintScale(LabelTemplate template, List<PrintPreflightIssue> issues)
    {
        var decision = PrintScaleContract.Evaluate(
            template.PrinterProfile.ScaleX,
            template.PrinterProfile.ScaleY);
        if (decision.IsAllowed)
        {
            return;
        }

        issues.Add(new PrintPreflightIssue(
            0,
            "Template",
            "PrintScale",
            decision.Diagnostic));
    }

    private static void ValidatePrintMethod(LabelTemplate template, List<PrintPreflightIssue> issues)
    {
        if (template.PrinterProfile.PrintMethod == PrintMethod.PrinterNative)
        {
            var printerName = string.IsNullOrWhiteSpace(template.PrinterProfile.PrinterName)
                ? "(none)"
                : template.PrinterProfile.PrinterName;
            issues.Add(new PrintPreflightIssue(
                0,
                "Template",
                "PrinterProfile",
                $"Print method is set to PrinterNative, but a verified thermal direct-command driver is not configured for printer '{printerName}'. Switch PrintMethod to ApplicationGraphic for exact designer rendering."));
        }
    }

    private static void ValidateBarcodeApplicationGeometry(LabelObject item, List<PrintPreflightIssue> issues)
    {
        if (item.Type is not (ObjectType.BarcodeCode128 or ObjectType.QRCode or ObjectType.DataMatrix)
            || item.BarcodeApplicationProfile == BarcodeApplicationProfile.General)
        {
            return;
        }

        var errors = BarcodeApplicationContract.ValidateGeometry(
            item.BarcodeApplicationProfile,
            GetEffectiveBarcodeSymbology(item),
            item.QrQuietZoneModules,
            item.ShowBarcodeText,
            item.BarcodeTextFontSizePt);
        foreach (var error in errors)
        {
            issues.Add(new PrintPreflightIssue(0, item.Name, item.Type.ToString(), error));
        }
    }

    private static BarcodeSymbology GetEffectiveBarcodeSymbology(LabelObject item)
        => item.Type switch
        {
            ObjectType.QRCode => BarcodeSymbology.QRCode,
            ObjectType.DataMatrix => BarcodeSymbology.DataMatrix,
            _ => item.BarcodeSymbology
        };

}

public sealed record PrintPreflightIssue(int RowNumber, string ObjectName, string ObjectType, string Message)
{
    public string Summary => RowNumber <= 0
        ? $"Template, {ObjectName} ({ObjectType}): {Message}"
        : $"Row {RowNumber}, {ObjectName} ({ObjectType}): {Message}";
}

/// <summary>
/// Progress for a potentially large preflight.  Units are object/row checks,
/// not labels printed; this keeps the value deterministic even when a template
/// contains both row-independent and row-dependent diagnostics.
/// </summary>
public sealed record PrintPreflightProgress(int CompletedUnits, int TotalUnits)
{
    public int Percent => TotalUnits <= 0
        ? 100
        : Math.Clamp((int)Math.Round(CompletedUnits * 100d / TotalUnits), 0, 100);
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
