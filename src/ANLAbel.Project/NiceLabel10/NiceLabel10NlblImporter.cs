using System.Globalization;
using System.Text.RegularExpressions;
using ANLAbel.Barcode.Options;
using ANLAbel.Barcode.Renderers;
using ANLAbel.Core.Barcode;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Core.Printing;

namespace ANLAbel.Project.NiceLabel10;

/// <summary>
/// Creates a new ANLAbel template from a parsed NiceLabel 10 label. The
/// label stock size and name are carried over, plus every object whose
/// stored facts map completely onto ANLAbel semantics: fixed and
/// variable-bound texts, framed texts (as frame-owned TextBox), linear
/// barcodes whose symbology ANLAbel encodes, QR and DataMatrix codes
/// (sized through ANLAbel's own 2D auto-size contract from the authored
/// module width), PDF417 codes (frame seeded from the production
/// encoder's native module grid at the stored element size), embedded
/// graphics (raster payloads as they are, metafiles rasterised at the
/// authored frame), lines and rectangles. Every object is placed through
/// <see cref="NiceLabel10Placement"/> so the stored anchoring point and
/// rotation are honoured. Variables keep their prompt text, default,
/// length and digit constraints; current-date variables become
/// <c>{#NOW:format}</c> fields and concatenation functions become mixed
/// binding expressions. GS1 DataBar barcodes, graphics without an
/// embedded payload, report items and references the package does not
/// declare are not converted: no stored facts map them completely onto
/// ANLAbel semantics, so they stay visible in the caller's
/// converted/total count instead of being guessed. The source
/// <c>.nlbl</c> file is never written.
/// </summary>
public static partial class NiceLabel10NlblImporter
{
    private const int MaxFunctionDepth = 8;

    public static LabelTemplate CreateTemplate(
        NiceLabel10Document document,
        string sourcePath,
        INiceLabel10TextMeasurer? textMeasurer = null)
        => CreateTemplate(document, sourcePath, out _, textMeasurer);

    /// <summary>
    /// Creates the template and reports what was done with the stored
    /// printer/media facts (see <see cref="ApplyPrintSetup"/>).
    /// </summary>
    public static LabelTemplate CreateTemplate(
        NiceLabel10Document document,
        string sourcePath,
        out NiceLabelPrinterSetupResult printerSetup,
        INiceLabel10TextMeasurer? textMeasurer = null)
    {
        var template = CreateTemplateCore(document, sourcePath, textMeasurer);
        printerSetup = ApplyPrintSetup(template, document.PrintSetup);
        return template;
    }

    /// <summary>
    /// Carries the stored printer/media facts into the template's printer
    /// profile, as NiceLabel 10 prints them:
    /// <list type="bullet">
    /// <item>A label that takes its settings from the printer driver
    /// (<c>UseDefaultPrinterSettings</c>) keeps ANLAbel's driver settings
    /// source, as in NiceLabel; a stored sheet layout keeps its grid.</item>
    /// <item>Otherwise the stored page is the physical media — label stock,
    /// an office sheet (A4/Letter/Legal/A3) or a sheet of several labels —
    /// with label settings, manual paper size, the first label's margin as
    /// the output offset, the label grid (counts, gaps, fill order, start
    /// corner) and the authored driver DPI when it is an industrial
    /// resolution.</item>
    /// </list>
    /// <c>PrintRotation</c> UpsideDown always maps to a 180° output rotation.
    /// The authoring printer is reported, never selected: a queue must be
    /// chosen and verified on this machine.
    /// </summary>
    internal static NiceLabelPrinterSetupResult ApplyPrintSetup(LabelTemplate template, NiceLabel10PrintSetup? setup)
    {
        if (setup is null)
        {
            return new NiceLabelPrinterSetupResult(NiceLabelMediaSetup.None, null, 0, 0, 1, 1, 0);
        }

        var profile = template.PrinterProfile;
        profile.Rotated180 = setup.PrintUpsideDown;
        profile.LabelWidthMm = template.WidthMm;
        profile.LabelHeightMm = template.HeightMm;
        if (setup.StockName is { } stockName)
        {
            profile.LabelStockName = stockName;
        }

        var authoredPrinter = setup.UseDefaultPrinter ? null : setup.PrinterName;
        NiceLabelPrinterSetupResult Result(NiceLabelMediaSetup kind, int dpi = 0) => new(
            kind, authoredPrinter, setup.PageWidthMm, setup.PageHeightMm, setup.LabelsAcross, setup.LabelsDown, dpi);

        var pageHoldsLabels = LabelStockContract.Evaluate(
                template.WidthMm, template.HeightMm, setup.PageWidthMm, setup.PageHeightMm).IsAllowed
            && SheetLayoutContract.Evaluate(
                template.WidthMm, template.HeightMm, setup.PageWidthMm, setup.PageHeightMm,
                setup.OffsetXMm, setup.OffsetYMm, setup.LabelsAcross, setup.LabelsDown,
                setup.HorizontalGapMm, setup.VerticalGapMm).IsAllowed;

        if (setup.UseDriverSettings)
        {
            profile.SettingsSource = PrinterSettingsSource.Driver;
            if (setup.LabelsPerPage > 1 && pageHoldsLabels)
            {
                // The grid needs the sheet size; the driver still chooses the paper.
                ApplyPage(profile, setup);
            }

            return Result(NiceLabelMediaSetup.DriverSettings);
        }

        if (!pageHoldsLabels)
        {
            // A page the contracts would refuse is left for Printer Setup.
            return Result(NiceLabelMediaSetup.None);
        }

        profile.SettingsSource = PrinterSettingsSource.Label;
        profile.PaperSizeSource = PaperSizeSource.Manual;
        profile.MediaProvenance = PrinterMediaProvenance.UserCustom;
        ApplyPage(profile, setup);
        var appliedDpi = 0;
        if (IndustrialPrintDpiContract.IsIndustrialDpi(setup.DriverDpi))
        {
            profile.Dpi = setup.DriverDpi;
            template.Dpi = setup.DriverDpi;
            appliedDpi = setup.DriverDpi;
        }

        var kind = setup.LabelsPerPage > 1
            ? NiceLabelMediaSetup.MultipleLabelsPerPage
            : LabelStockContract.IsOfficeSheet(setup.PageWidthMm, setup.PageHeightMm)
                ? NiceLabelMediaSetup.OfficeSheet
                : NiceLabelMediaSetup.LabelStock;
        return Result(kind, appliedDpi);
    }

    /// <summary>Vendor <c>WebSettings</c> flags (Interfaces.dll).</summary>
    private const int WebSettingsVertical = 2;
    private const int WebSettingsBottom = 8;
    private const int WebSettingsRight = 0x20;

    private static void ApplyPage(PrinterProfile profile, NiceLabel10PrintSetup setup)
    {
        profile.PhysicalWidthMm = setup.PageWidthMm;
        profile.PhysicalHeightMm = setup.PageHeightMm;
        profile.OffsetXMm = setup.OffsetXMm;
        profile.OffsetYMm = setup.OffsetYMm;
        profile.LabelsAcross = setup.LabelsAcross;
        profile.LabelsDown = setup.LabelsDown;
        profile.HorizontalGapMm = setup.HorizontalGapMm;
        profile.VerticalGapMm = setup.VerticalGapMm;
        profile.FillDownFirst = (setup.WebSettings & WebSettingsVertical) != 0;
        profile.StartFromRight = (setup.WebSettings & WebSettingsRight) != 0;
        profile.StartFromBottom = (setup.WebSettings & WebSettingsBottom) != 0;
    }

    private static LabelTemplate CreateTemplateCore(
        NiceLabel10Document document,
        string sourcePath,
        INiceLabel10TextMeasurer? textMeasurer)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        }

        var template = new LabelTemplate
        {
            Name = string.IsNullOrWhiteSpace(document.Name)
                ? Path.GetFileNameWithoutExtension(sourcePath)
                : document.Name.Trim(),
            WidthMm = document.WidthMm,
            HeightMm = document.HeightMm
        };

        var context = new ImportContext(document, template, textMeasurer, Path.GetDirectoryName(Path.GetFullPath(sourcePath)));
        foreach (var record in document.Objects)
        {
            var item = record.Kind switch
            {
                NiceLabel10ObjectKind.Text => TextLike(record, ObjectType.Text, context),
                NiceLabel10ObjectKind.FramedText => TextLike(record, ObjectType.TextBox, context),
                NiceLabel10ObjectKind.Barcode => Barcode(record, context),
                NiceLabel10ObjectKind.Line => Line(record),
                NiceLabel10ObjectKind.Rectangle => Rectangle(record),
                NiceLabel10ObjectKind.Graphic => Graphic(record, context),
                _ => null
            };
            if (item is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(record.Name))
            {
                item.Name = record.Name.Trim();
            }

            if (record.ContentMask is { } mask)
            {
                item.ContentMask = mask;
                item.ContentMaskCharacter = record.ContentMaskCharacter;
            }

            template.Objects.Add(item);
        }

        return template;
    }

    private static LabelObject? TextLike(NiceLabel10ObjectRecord record, ObjectType type, ImportContext context)
    {
        if (!context.TryResolveBinding(record, out var expression))
        {
            return null;
        }

        var item = new LabelObject
        {
            Type = type,
            Text = expression is null ? record.Content : string.Empty,
            XMm = record.XMm,
            YMm = record.YMm
        };
        if (type == ObjectType.TextBox)
        {
            // A NiceLabel framed text owns its frame exactly like an ANLAbel
            // TextBox: the stored frame is taken over once, here at import.
            item.WidthMm = record.WidthMm;
            item.HeightMm = record.HeightMm;
            // Vendor TextBoxAlignment (Utils.dll): None, Left, Center,
            // Right, Justified in stored order 0-4.
            item.Style.Alignment = record.TextAlignmentValue switch
            {
                1 => TextAlignmentMode.Left,
                2 => TextAlignmentMode.Center,
                3 => TextAlignmentMode.Right,
                4 => TextAlignmentMode.Justify,
                _ => item.Style.Alignment
            };
        }

        if (!string.IsNullOrWhiteSpace(record.FontFamily))
        {
            item.Style.FontFamily = record.FontFamily;
        }

        if (record.FontSizePt > 0)
        {
            item.Style.FontSizePt = record.FontSizePt;
        }

        item.Style.Bold = record.Bold;
        item.Style.Italic = record.Italic;
        item.Style.Underline = record.Underline;
        ApplyItemColor(item, record);
        if (expression is not null)
        {
            item.BindingExpression = expression;
        }

        if (type == ObjectType.TextBox)
        {
            ApplyBestFit(item, record);
            Place(item, record, record.WidthMm, record.HeightMm);
            return item;
        }

        // A position text is content-owned: its frame is its measured
        // bounds, so the imported object starts at the natural size of its
        // design-time contents instead of the new-object default frame
        // (which would centre small captions a few millimetres too low).
        var natural = context.MeasureText(item, context.SampleText(record));
        // Vendor ScaleFactor narrows (< 100) or widens (> 100) the glyphs; it
        // is ANLAbel's Text FontScalePercent, a style factor that keeps the
        // Text content-owned (AutoFit), so the frame is the scaled natural width.
        var scale = FontScalePercent(record);
        item.Style.FontScalePercent = scale;
        item.WidthMm = natural.WidthMm * scale / 100.0;
        item.HeightMm = natural.HeightMm;
        Place(item, record, item.WidthMm, item.HeightMm);
        return item;
    }

    private static double FontScalePercent(NiceLabel10ObjectRecord record)
    {
        var percent = record.ScaleFactorPercent;
        return double.IsFinite(percent) && percent > 0 ? Math.Clamp(percent, 10, 400) : 100;
    }

    /// <summary>
    /// Vendor <c>BestFitType</c> (FormatDocument.dll): ScaleByFontSize 1 is
    /// ANLAbel's ShrinkFont with the stored point bounds; ScaleByFontScaling
    /// 2 is ScaleWidth with the stored percent bounds; VariableHeight 3 is
    /// AdjustHeight (height follows the content). None keeps the frame.
    /// </summary>
    private static void ApplyBestFit(LabelObject item, NiceLabel10ObjectRecord record)
    {
        switch (record.BestFitValue)
        {
            case 1:
                item.Style.TextSizing = TextSizingMode.ShrinkFont;
                if (record.BestFitMinimumFontSizePt > 0)
                {
                    item.Style.TextFitMinimumFontSizePt = record.BestFitMinimumFontSizePt;
                }

                if (record.BestFitMaximumFontSizePt > 0)
                {
                    item.Style.TextFitMaximumFontSizePt = record.BestFitMaximumFontSizePt;
                }

                break;
            case 2:
                item.Style.TextSizing = TextSizingMode.ScaleWidth;
                if (record.BestFitMinimumScalePercent > 0)
                {
                    item.Style.TextFitMinimumScale = record.BestFitMinimumScalePercent / 100.0;
                }

                if (record.BestFitMaximumScalePercent > 0)
                {
                    item.Style.TextFitMaximumScale = record.BestFitMaximumScalePercent / 100.0;
                }

                break;
            case 3:
                item.Style.TextSizing = TextSizingMode.AdjustHeight;
                break;
        }
    }

    /// <summary>
    /// NiceLabel stores one ARGB colour per item; ANLAbel's shared style
    /// carries it as StrokeColor, which both the designer and the print
    /// renderer already use as the glyph/stroke brush. Imported items
    /// therefore keep their stored colour (notably white reverse text)
    /// instead of ANLAbel's slate default.
    /// </summary>
    private static void ApplyItemColor(LabelObject item, NiceLabel10ObjectRecord record)
    {
        if (record.ItemColorHex is { Length: 8 } hex
            && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            item.Style.StrokeColor = "#" + hex.ToUpperInvariant();
        }
    }

    /// <summary>
    /// Moves the stored anchor point onto ANLAbel's frame origin and applies
    /// the stored rotation (see <see cref="NiceLabel10Placement"/>).
    /// </summary>
    private static void Place(LabelObject item, NiceLabel10ObjectRecord record, double widthMm, double heightMm)
    {
        var (left, top, rotation) = NiceLabel10Placement.ResolveFrame(
            record.XMm,
            record.YMm,
            widthMm,
            heightMm,
            record.AnchoringPoint,
            record.RotationDegrees);
        item.XMm = left;
        item.YMm = top;
        item.Rotation = rotation;
        // The vendor anchor also decides where a variable-size object grows
        // (LeftTop 0 ... RightBottom 8; None 9 is top-left).
        item.AnchorPoint = record.AnchoringPoint is >= 0 and <= 8
            ? (ObjectAnchorPoint)record.AnchoringPoint
            : ObjectAnchorPoint.TopLeft;
    }

    private static LabelObject? Barcode(NiceLabel10ObjectRecord record, ImportContext context)
    {
        if (!context.TryResolveBinding(record, out var expression))
        {
            return null;
        }

        LabelObject? item;
        if (record.BarcodeDataType is "QRBarcodeData" or "DatamatrixBarcodeData" or "GS1DatamatrixBarcodeData")
        {
            item = TwoDimensionalBarcode(record, expression);
        }
        else if (record.BarcodeDataType == "Pdf417BarcodeData")
        {
            item = Pdf417Barcode(record, expression);
        }
        else
        {
            item = LinearBarcode(record, expression);
        }

        if (item is null)
        {
            return null;
        }

        ApplyHumanReadable(item, record);
        Place(item, record, item.WidthMm, item.HeightMm);
        return item;
    }

    /// <summary>
    /// Vendor <c>HumanInterpretationPosition</c> (Barcodes.Interfaces.dll):
    /// NoInterpretation 0, Below 1, Above 2, FloatingAbove 3,
    /// FloatingBelow 4; the vendor loader defaults an absent element to
    /// Below. The corpus stores 0 on almost every barcode, i.e. the text
    /// is off and any caption is a separate text object.
    /// </summary>
    private static void ApplyHumanReadable(LabelObject item, NiceLabel10ObjectRecord record)
    {
        item.BarcodeHriPlacement = record.BarcodeHriPosition switch
        {
            0 => BarcodeHriPlacement.None,
            2 or 3 => BarcodeHriPlacement.Above,
            _ => BarcodeHriPlacement.Below
        };
    }

    private static LabelObject? LinearBarcode(NiceLabel10ObjectRecord record, string? expression)
    {
        var (symbology, profile) = record.BarcodeDataType switch
        {
            "Code128BarcodeData" or "Code128BBarcodeData" =>
                (BarcodeSymbology.Code128, BarcodeApplicationProfile.General),
            "GS1128BarcodeData" =>
                (BarcodeSymbology.Code128, BarcodeApplicationProfile.Gs1),
            "Code39BarcodeData" or "Code39FullAsciiBarcodeData" =>
                (BarcodeSymbology.Code39, BarcodeApplicationProfile.General),
            "Ean13BarcodeData" or "BooklandBarcodeData" =>
                (BarcodeSymbology.Ean13, BarcodeApplicationProfile.General),
            _ => ((BarcodeSymbology?)null, BarcodeApplicationProfile.General)
        };
        if (symbology is null)
        {
            return null;
        }

        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = symbology.Value,
            BarcodeApplicationProfile = profile,
            Text = expression is null ? record.Content : string.Empty,
            XMm = record.XMm,
            YMm = record.YMm
        };
        if (expression is not null)
        {
            item.BindingExpression = expression;
        }

        if (record.BarcodeDataType == "Code128BBarcodeData")
        {
            // The vendor type is literally Code 128 subset B.
            item.Code128CodeSet = Code128CodeSet.B;
        }

        if (record.BarcodeHriFontSizePt > 0)
        {
            item.BarcodeTextFontSizePt = record.BarcodeHriFontSizePt;
        }

        if (symbology == BarcodeSymbology.Code39)
        {
            // Code 39 details the package stores: an optional modulo-43 check
            // character (generated when encoding), whether the HRI shows it,
            // the wide/narrow ratio and the inter-character gap.
            item.BarcodeCheckDigitPolicy = record.BarcodeHasCheckDigit
                ? BarcodeCheckDigitPolicy.Generate
                : BarcodeCheckDigitPolicy.None;
            item.BarcodeHriShowCheckDigit = record.BarcodeDisplayCheckDigit;
            item.Code39WideNarrowRatio = Code39Ratio(record.BarcodeRatio);
            if (record.BarcodeIntercharacterGap is >= 1 and <= 5)
            {
                item.Code39IntercharacterGapModules = record.BarcodeIntercharacterGap;
            }
        }

        if (record.BarcodeModuleHeightMm > 0)
        {
            item.HeightMm = record.BarcodeModuleHeightMm;
        }

        if (record.BarcodeModuleWidthMm > 0)
        {
            // NiceLabel stores the authored module (X) width; keep it as the
            // production module width and estimate the designer frame from
            // the symbology's module count so the frame starts close.
            item.BarcodeModuleWidthMm = record.BarcodeModuleWidthMm;
            item.BarcodeWidthMode = BarcodeWidthMode.SizedFromX;
            var modules = EstimateModules(record.BarcodeDataType!, record.Content.Length, record.BarcodeRatio);
            if (modules > 0)
            {
                item.WidthMm = modules * record.BarcodeModuleWidthMm;
            }
        }

        return item;
    }

    private static Code39WideNarrowRatio Code39Ratio(double ratio) => ratio switch
    {
        >= 1.95 and <= 2.05 => Code39WideNarrowRatio.Ratio2_0,
        >= 2.15 and <= 2.25 => Code39WideNarrowRatio.Ratio2_2,
        >= 2.45 and <= 2.55 => Code39WideNarrowRatio.Ratio2_5,
        >= 2.95 and <= 3.05 => Code39WideNarrowRatio.Ratio3_0,
        _ => Code39WideNarrowRatio.LegacyEngineDefault
    };

    /// <summary>
    /// Vendor <c>LineStyles</c> (Interfaces.dll, flags): Solid 1, Dash 2,
    /// Dot 4, DashDot 8, DashDotDot 16, Clear 32. Dash-dot patterns keep
    /// the nearest ANLAbel dashed look; Clear draws no stroke.
    /// </summary>
    private static void ApplyStrokeStyle(LabelObject item, NiceLabel10ObjectRecord record)
    {
        item.Style.OutlineStyle = record.LineStyleValue switch
        {
            2 or 8 or 16 => OutlineStyle.Dash,
            4 => OutlineStyle.Dot,
            32 => OutlineStyle.None,
            _ => item.Style.OutlineStyle
        };
    }

    /// <summary>
    /// PDF417 conversion. The package stores the security level, the
    /// truncated flag and the element (module/row) size, but no symbol
    /// frame: layout is encoder-chosen in NiceLabel too (it bundles the
    /// same ZXing.Net family ANLAbel encodes with). The frame is therefore
    /// seeded from the native module grid ANLAbel's own production
    /// encoder produces for the stored contents and settings, multiplied
    /// by the stored element size — the same size the print pipeline will
    /// integer-scale into. Items without contents or element size cannot
    /// be sized and stay unconverted.
    /// </summary>
    private static LabelObject? Pdf417Barcode(NiceLabel10ObjectRecord record, string? expression)
    {
        if (string.IsNullOrWhiteSpace(record.Content)
            || record.BarcodeElementWidthMm <= 0
            || record.BarcodeElementHeightMm <= 0)
        {
            return null;
        }

        var item = new LabelObject
        {
            Type = ObjectType.BarcodeCode128,
            BarcodeSymbology = BarcodeSymbology.Pdf417,
            Text = expression is null ? record.Content : string.Empty,
            XMm = record.XMm,
            YMm = record.YMm,
            Pdf417ErrorCorrectionLevel = record.BarcodeErrorLevel is >= 0 and <= 8
                ? record.BarcodeErrorLevel
                : -1,
            Pdf417Truncated = record.BarcodeTruncated
        };
        if (expression is not null)
        {
            item.BindingExpression = expression;
        }

        var modules = new ZxingBarcodeRenderer().CountMatrixModules(
            record.Content,
            BarcodeType.Pdf417,
            BarcodeRenderOptions.FromObject(item));
        if (modules is null)
        {
            return null;
        }

        item.WidthMm = modules.Value.WidthModules * record.BarcodeElementWidthMm;
        item.HeightMm = modules.Value.HeightModules * record.BarcodeElementHeightMm;
        return item;
    }

    /// <summary>
    /// QR and DataMatrix conversion. The vendor enums were recovered from
    /// <c>Barcodes.Interfaces.dll</c>: QR error levels are L/M/Q/H in stored
    /// order 0-3, QR version 0 and DataMatrix format 0 both mean the
    /// encoder chooses the symbol — which is exactly ANLAbel's
    /// AutoSizeByData contract, fed here with the authored module width.
    /// An explicit DataMatrix format (C&lt;rows&gt;x&lt;cols&gt;) gives an
    /// exact frame in modules, so it is applied after the content is set.
    /// </summary>
    private static LabelObject TwoDimensionalBarcode(NiceLabel10ObjectRecord record, string? expression)
    {
        var isQr = record.BarcodeDataType == "QRBarcodeData";
        var item = new LabelObject
        {
            BarcodeSymbology = isQr ? BarcodeSymbology.QRCode : BarcodeSymbology.DataMatrix,
            BarcodeApplicationProfile = record.BarcodeDataType == "GS1DatamatrixBarcodeData"
                ? BarcodeApplicationProfile.Gs1
                : BarcodeApplicationProfile.General,
            XMm = record.XMm,
            YMm = record.YMm
        };

        if (isQr && record.BarcodeErrorLevel is >= 0 and <= 3)
        {
            item.QrErrorCorrection = record.BarcodeErrorLevel switch
            {
                0 => QrErrorCorrection.L,
                1 => QrErrorCorrection.M,
                2 => QrErrorCorrection.Q,
                _ => QrErrorCorrection.H
            };
        }

        if (record.BarcodeModuleWidthMm > 0)
        {
            // Carry the authored module width into ANLAbel's module-size
            // fields (pixels at the object's QR DPI, 300 by default).
            item.QrModuleSizePx = Math.Max(1,
                (int)Math.Round(record.BarcodeModuleWidthMm / 25.4 * item.QrDpi));
        }

        if (isQr && record.BarcodeFormatValue > 0)
        {
            item.QrSizingMode = QrSizingMode.FixedVersionAndModuleSize;
            item.QrFixedVersion = record.BarcodeFormatValue;
        }

        item.Text = expression is null ? record.Content : string.Empty;
        if (expression is not null)
        {
            item.BindingExpression = expression;
        }

        // Setting Type last lets the model's own 2D auto-size contract run
        // with content and module width already in place.
        item.Type = isQr ? ObjectType.QRCode : ObjectType.DataMatrix;

        if (!isQr
            && record.BarcodeModuleWidthMm > 0
            && DataMatrixCells(record.BarcodeFormatValue) is { } cells)
        {
            item.WidthMm = cells.Columns * record.BarcodeModuleWidthMm;
            item.HeightMm = cells.Rows * record.BarcodeModuleWidthMm;
        }

        return item;
    }

    /// <summary>
    /// ECC 200 symbol sizes for the vendor <c>DatamatrixFormat</c> values
    /// 1-30 (Auto and DMRE variants return null). The vendor names are
    /// C&lt;rows&gt;x&lt;cols&gt;.
    /// </summary>
    private static (int Columns, int Rows)? DataMatrixCells(int format) => format switch
    {
        1 => (10, 10),
        2 => (12, 12),
        3 => (14, 14),
        4 => (16, 16),
        5 => (18, 18),
        6 => (20, 20),
        7 => (22, 22),
        8 => (24, 24),
        9 => (26, 26),
        10 => (32, 32),
        11 => (36, 36),
        12 => (40, 40),
        13 => (44, 44),
        14 => (48, 48),
        15 => (52, 52),
        16 => (64, 64),
        17 => (72, 72),
        18 => (80, 80),
        19 => (88, 88),
        20 => (96, 96),
        21 => (104, 104),
        22 => (120, 120),
        23 => (132, 132),
        24 => (144, 144),
        25 => (18, 8),
        26 => (32, 8),
        27 => (26, 12),
        28 => (36, 12),
        29 => (36, 16),
        30 => (48, 16),
        _ => null
    };

    /// <summary>
    /// Logical module counts for the supported linear symbologies, used
    /// only to seed the designer frame; production width in
    /// <see cref="BarcodeWidthMode.SizedFromX"/> is recomputed from the
    /// actual encoded content.
    /// </summary>
    private static int EstimateModules(string barcodeDataType, int contentLength, double ratio)
    {
        return barcodeDataType switch
        {
            // 95 modules of symbol pattern, quiet zones excluded.
            "Ean13BarcodeData" or "BooklandBarcodeData" => 95,
            // Start + data + check at 11 modules each, plus 13-module stop.
            "Code128BarcodeData" or "Code128BBarcodeData" or "GS1128BarcodeData" =>
                11 * (contentLength + 2) + 13,
            // Per character: 3 wide elements at the stored ratio plus 6
            // narrow elements and a 1-module gap; start/stop included.
            // NiceLabel's own default ratio is 3 when none is stored.
            "Code39BarcodeData" or "Code39FullAsciiBarcodeData" =>
                (int)Math.Round((3 * (ratio > 0 ? ratio : 3) + 6 + 1) * (contentLength + 2.0)),
            _ => 0
        };
    }

    private static LabelObject Line(NiceLabel10ObjectRecord record)
    {
        var item = new LabelObject
        {
            Type = ObjectType.Line,
            XMm = record.XMm,
            YMm = record.YMm,
            LineEndXMm = record.LineEndXMm,
            LineEndYMm = record.LineEndYMm,
            WidthMm = Math.Abs(record.LineEndXMm - record.XMm),
            HeightMm = Math.Abs(record.LineEndYMm - record.YMm)
        };
        if (record.StrokeThicknessMm > 0)
        {
            item.Style.BorderThicknessMm = record.StrokeThicknessMm;
        }

        ApplyStrokeStyle(item, record);
        ApplyItemColor(item, record);
        return item;
    }

    private static LabelObject Rectangle(NiceLabel10ObjectRecord record)
    {
        var item = new LabelObject
        {
            Type = ObjectType.Rectangle,
            XMm = record.XMm,
            YMm = record.YMm,
            WidthMm = record.WidthMm,
            HeightMm = record.HeightMm
        };
        if (record.StrokeThicknessMm > 0)
        {
            item.Style.BorderThicknessMm = record.StrokeThicknessMm;
        }

        if (record.CornerRadiusMm > 0)
        {
            item.Style.CornerRadiusMm = record.CornerRadiusMm;
        }

        ApplyStrokeStyle(item, record);
        ApplyItemColor(item, record);

        // Vendor FillStyles (Interfaces.dll): None, Clear, Solid, then
        // hatch patterns ANLAbel does not offer. Only Solid maps; hatch
        // patterns keep the default outline look rather than a wrong fill.
        if (record.FillStyleValue == 2)
        {
            item.Style.FillStyle = FillStyle.Solid;
            if (record.FillColorHex is { Length: 8 } hex
                && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            {
                item.Style.FillColor = "#" + hex.ToUpperInvariant();
            }
        }

        Place(item, record, record.WidthMm, record.HeightMm);
        return item;
    }

    /// <summary>
    /// Graphics. Raster payloads (PNG/JPEG/BMP/GIF) are carried as they
    /// are; Windows metafiles are rasterised at the authored frame (vendor
    /// <c>GraphicResizeMode</c> Uniform or <c>KeepAspectRatio</c> keeps the
    /// drawing's aspect, Fill stretches it). A graphic without an embedded
    /// payload is read from its referenced file when that file exists
    /// (as stored, or beside the label; the corpus keeps 24 such clipart
    /// files next to their labels); otherwise, and for payloads GDI+ cannot
    /// play, the graphic stays unconverted.
    /// </summary>
    private static LabelObject? Graphic(NiceLabel10ObjectRecord record, ImportContext context)
    {
        if (record.WidthMm <= 0 || record.HeightMm <= 0)
        {
            return null;
        }

        byte[]? payload = null;
        if (record.GraphicBase64 is not null)
        {
            try
            {
                payload = Convert.FromBase64String(record.GraphicBase64);
            }
            catch (FormatException)
            {
                return null;
            }
        }
        else if (record.GraphicFileName is { } fileName)
        {
            payload = context.ReadGraphicFile(fileName);
        }

        if (payload is null)
        {
            return null;
        }

        string base64;
        int pixelWidth = 0, pixelHeight = 0;
        if (IsRasterImage(payload))
        {
            base64 = Convert.ToBase64String(payload);
            (pixelWidth, pixelHeight) = ReadPngSize(payload);
        }
        else if (OperatingSystem.IsWindows() && WindowsMetafileRasterizer.LooksLikeMetafile(payload))
        {
            var keepAspect = record.GraphicResizeMode == 2 || record.GraphicKeepAspectRatio;
            var raster = WindowsMetafileRasterizer.RasterizeToPng(payload, record.WidthMm, record.HeightMm, keepAspect);
            if (raster is null)
            {
                return null;
            }

            base64 = Convert.ToBase64String(raster.Value.Png);
            pixelWidth = raster.Value.PixelWidth;
            pixelHeight = raster.Value.PixelHeight;
        }
        else
        {
            return null;
        }

        var item = new LabelObject
        {
            Type = ObjectType.Image,
            ImageDataBase64 = base64,
            ImagePixelWidth = pixelWidth,
            ImagePixelHeight = pixelHeight,
            XMm = record.XMm,
            YMm = record.YMm,
            WidthMm = record.WidthMm,
            HeightMm = record.HeightMm,
            // Vendor Fill stretches the picture to its frame; None (whose frame is the
            // picture's own size), Uniform and the rest keep its proportions.
            PictureSizing = record.GraphicResizeMode == 1 ? PictureSizing.Stretch : PictureSizing.Proportional,
            PictureFlipHorizontal = record.GraphicFlipHorizontal,
            PictureFlipVertical = record.GraphicFlipVertical,
            Style = { BorderThicknessMm = 0, OutlineStyle = OutlineStyle.None }
        };
        Place(item, record, record.WidthMm, record.HeightMm);
        return item;
    }

    private static bool IsRasterImage(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 8)
        {
            return false;
        }

        return (bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G')
            || (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            || (bytes[0] == (byte)'B' && bytes[1] == (byte)'M')
            || (bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'8');
    }

    /// <summary>PNG IHDR dimensions; (0, 0) for other raster formats.</summary>
    private static (int Width, int Height) ReadPngSize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 24 || bytes[0] != 0x89 || bytes[1] != (byte)'P')
        {
            return (0, 0);
        }

        var width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
        var height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
        return width > 0 && height > 0 ? (width, height) : (0, 0);
    }

    /// <summary>
    /// Per-import state: variable lookups, prompt registration and text
    /// measurement.
    /// </summary>
    private sealed class ImportContext
    {
        private readonly NiceLabel10Document _document;
        private readonly LabelTemplate _template;
        private readonly INiceLabel10TextMeasurer? _measurer;
        private readonly string? _sourceDirectory;
        private readonly HashSet<string> _registeredPrompts = new(StringComparer.Ordinal);

        private const long MaxGraphicFileBytes = 32 * 1024 * 1024;

        public ImportContext(NiceLabel10Document document, LabelTemplate template, INiceLabel10TextMeasurer? measurer, string? sourceDirectory)
        {
            _document = document;
            _template = template;
            _measurer = measurer;
            _sourceDirectory = sourceDirectory;
        }

        /// <summary>
        /// Reads a referenced graphic file: the stored path when it exists,
        /// else the same path or file name resolved beside the label. Only
        /// existing regular files up to 32 MB are read; nothing is written.
        /// </summary>
        public byte[]? ReadGraphicFile(string storedPath)
        {
            foreach (var candidate in Candidates())
            {
                try
                {
                    var info = new FileInfo(candidate);
                    if (info.Exists && info.Length > 0 && info.Length <= MaxGraphicFileBytes)
                    {
                        return File.ReadAllBytes(info.FullName);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    // An unreadable candidate is simply not this graphic's file.
                }
            }

            return null;

            IEnumerable<string> Candidates()
            {
                var normalized = storedPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalized))
                {
                    yield return normalized;
                }

                if (_sourceDirectory is null)
                {
                    yield break;
                }

                if (!Path.IsPathRooted(normalized))
                {
                    yield return Path.Combine(_sourceDirectory, normalized);
                }

                var fileName = Path.GetFileName(normalized);
                if (fileName.Length > 0)
                {
                    yield return Path.Combine(_sourceDirectory, fileName);
                }
            }
        }

        /// <summary>
        /// Resolves the item's binding: null expression for plain fixed
        /// contents; false when a referenced data source is unknown to
        /// the package or its expression cannot be written (fail closed).
        /// </summary>
        public bool TryResolveBinding(NiceLabel10ObjectRecord record, out string? expression)
        {
            expression = null;
            if (record.HasUnresolvedReference)
            {
                return false;
            }

            var prompts = new List<NiceLabel10Variable>();
            if (record.ContentTemplate is { } contentTemplate)
            {
                expression = ExpandTemplate(contentTemplate, 0, prompts);
                if (expression is null)
                {
                    return false;
                }
            }
            else if (record.VariableId is { } id && _document.VariablesById.TryGetValue(id, out var variable))
            {
                expression = ExpressionFor(variable, 0, prompts);
                if (expression is null)
                {
                    return false;
                }
            }

            if (expression is not null && NiceLabel10FunctionMarkers.ContainsMarker(expression))
            {
                // A print-time transformation (GS1 check digit, substring)
                // needs ANLAbel's formula form of the same binding.
                expression = NiceLabel10FunctionMarkers.ToFormula(expression);
                if (expression is null)
                {
                    return false;
                }
            }

            foreach (var prompt in prompts)
            {
                RegisterPrompt(prompt, record);
            }

            return true;
        }

        /// <summary>
        /// The design-time text a position text is sized from: the bound
        /// variable's default when it has one (the item's own FixedContents
        /// is often a long placeholder for bound items), otherwise the
        /// item's contents.
        /// </summary>
        public string SampleText(NiceLabel10ObjectRecord record)
        {
            if (record.VariableId is { } id
                && _document.VariablesById.TryGetValue(id, out var variable)
                && !string.IsNullOrEmpty(variable.DefaultValue))
            {
                return record.IsRichText ? NiceLabel10RichText.ToPlainText(variable.DefaultValue) : variable.DefaultValue;
            }

            return record.Content;
        }

        public (double WidthMm, double HeightMm) MeasureText(LabelObject item, string sampleText)
        {
            var measured = _measurer?.MeasureText(item, sampleText);
            return measured ?? EstimateTextSize(item, sampleText);
        }

        private string? ExpressionFor(NiceLabel10Variable variable, int depth, List<NiceLabel10Variable> prompts, string? dateFormatOverride = null)
        {
            if (variable.DataFieldName is { } field)
            {
                // A database column binds to the linked data row by name.
                return field.IndexOfAny(new[] { '{', '}' }) >= 0 ? null : "{" + field + "}";
            }

            if (variable.SystemDateFormat is { } storedFormat)
            {
                // A date function (DateAddFunction) applies its offsets to an
                // input date; only the current date can be reproduced, so the
                // input must itself be a current-date variable (chains allowed).
                int days = variable.SystemDateOffsetDays, months = variable.SystemDateOffsetMonths, years = variable.SystemDateOffsetYears;
                var input = variable;
                var hops = 0;
                while (input.DateInputId is { } inputId)
                {
                    if (++hops > MaxFunctionDepth
                        || !_document.VariablesById.TryGetValue(inputId, out var next)
                        || next.SystemDateFormat is null)
                    {
                        return null;
                    }

                    days += next.SystemDateOffsetDays;
                    months += next.SystemDateOffsetMonths;
                    years += next.SystemDateOffsetYears;
                    input = next;
                }

                var format = dateFormatOverride ?? storedFormat;
                if (format.IndexOfAny(new[] { '{', '}' }) >= 0)
                {
                    return null;
                }

                var offset = Offset(days, "d") + Offset(months, "mo") + Offset(years, "y");
                return "{#NOW" + offset + ":" + format + "}";

                static string Offset(int amount, string unit) =>
                    amount == 0 ? string.Empty : amount.ToString("+#;-#", CultureInfo.InvariantCulture) + unit;
            }

            if (variable.FunctionTemplate is { } template)
            {
                return ExpandTemplate(template, depth + 1, prompts);
            }

            if (variable.Name.IndexOfAny(new[] { '{', '}' }) >= 0)
            {
                return null;
            }

            prompts.Add(variable);
            return "{?" + variable.Name + "}";
        }

        private string? ExpandTemplate(string template, int depth, List<NiceLabel10Variable> prompts)
        {
            if (depth > MaxFunctionDepth || template.IndexOfAny(new[] { '{', '}' }) >= 0)
            {
                return null;
            }

            // Function markers wrap their own expansion; a literal that merely
            // contains a marker character cannot occur in NiceLabel data.

            var unresolved = false;
            var expanded = ReferencePattern().Replace(template, match =>
            {
                if (!_document.VariablesById.TryGetValue(match.Groups[1].Value, out var variable))
                {
                    unresolved = true;
                    return string.Empty;
                }

                var formatOverride = match.Groups[2].Success ? match.Groups[2].Value : null;
                var inner = ExpressionFor(variable, depth, prompts, formatOverride);
                if (inner is null)
                {
                    unresolved = true;
                    return string.Empty;
                }

                return inner;
            });
            return unresolved ? null : expanded;
        }

        private void RegisterPrompt(NiceLabel10Variable variable, NiceLabel10ObjectRecord record)
        {
            if (!_registeredPrompts.Add(variable.Name)
                || _template.PromptVariables.Any(v => string.Equals(v.Name, variable.Name, StringComparison.Ordinal)))
            {
                return;
            }

            // A variable feeding a rich-text item stores its default in the
            // same rich encoding (base64 text/RTF) as the item's contents.
            var storedDefault = record.IsRichText
                ? NiceLabel10RichText.ToPlainText(variable.DefaultValue)
                : variable.DefaultValue;
            _template.PromptVariables.Add(new PromptVariableDefinition
            {
                Name = variable.Name,
                Prompt = variable.PromptText?.Trim() ?? string.Empty,
                // The stored default; the item's design-time contents stand
                // in when the variable declares none.
                DefaultValue = string.IsNullOrEmpty(storedDefault) ? record.Content : storedDefault,
                MaxLength = Math.Max(0, variable.MaxLength),
                Required = variable.Required,
                DigitsOnly = variable.DigitsOnly
            });
        }

        /// <summary>
        /// Font-metric estimate used only when no measurer is supplied:
        /// average Latin advance of about 0.55 em (0.6 em bold) and a line
        /// height of 1.2 em.
        /// </summary>
        private static (double WidthMm, double HeightMm) EstimateTextSize(LabelObject item, string sampleText)
        {
            var emMm = Math.Max(1, item.Style.FontSizePt) * 25.4 / 72.0;
            var lines = string.IsNullOrEmpty(sampleText) ? new[] { string.Empty } : sampleText.Split('\n');
            var longest = lines.Max(line => line.TrimEnd('\r').Length);
            var advance = item.Style.Bold ? 0.6 : 0.55;
            return (Math.Max(1, longest) * advance * emMm, lines.Length * 1.2 * emMm);
        }
    }

    // "[id]" references a solution data source; "[id:format]" asks a date
    // source for a specific .NET format (GS1 date AIs need yyMMdd).
    [GeneratedRegex(@"\[([0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12})(?::([^\]\{\}]+))?\]")]
    private static partial Regex ReferencePattern();
}
