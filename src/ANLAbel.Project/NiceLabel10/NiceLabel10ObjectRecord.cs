namespace ANLAbel.Project.NiceLabel10;

/// <summary>The kinds of NiceLabel 10 document items the importer can see.</summary>
public enum NiceLabel10ObjectKind
{
    Text,
    FramedText,
    Barcode,
    Line,
    Rectangle,
    Graphic,
    Unsupported
}

/// <summary>
/// One document item read from the format XML inside a NiceLabel 10
/// <c>.nlbl</c> package. All geometry is already converted from the stored
/// 1/1000 mm integers to millimetres. Fields that do not apply to a kind
/// stay at their defaults; <see cref="NiceLabel10ObjectKind.Unsupported"/>
/// items are reported but never converted.
/// </summary>
/// <remarks>
/// <see cref="XMm"/>/<see cref="YMm"/> are the stored anchor coordinates
/// (<c>X</c>/<c>Y</c> of a PositionGeometry, <c>Left</c>/<c>Top</c> of a
/// RectGeometry). Which point of the object sits there is given by
/// <see cref="AnchoringPoint"/> (vendor <c>AnchoringPointEnum</c>: LeftTop 0,
/// CenterTop 1, RightTop 2, LeftMiddle 3, CenterMiddle 4, RightMiddle 5,
/// LeftBottom 6, CenterBottom 7, RightBottom 8, None 9) after the object
/// has been rotated clockwise by <see cref="RotationDegrees"/>; see
/// <see cref="NiceLabel10Placement"/>.
/// </remarks>
public sealed record NiceLabel10ObjectRecord(
    NiceLabel10ObjectKind Kind,
    string Name,
    string? SourceType,
    double XMm,
    double YMm,
    double WidthMm,
    double HeightMm,
    double LineEndXMm,
    double LineEndYMm,
    string Content,
    string? VariableName,
    string FontFamily,
    double FontSizePt,
    bool Bold,
    bool Italic,
    string? BarcodeDataType,
    double BarcodeModuleWidthMm,
    double BarcodeModuleHeightMm,
    double BarcodeRatio,
    int BarcodeFormatValue,
    int BarcodeErrorLevel,
    double BarcodeHriFontSizePt,
    double BarcodeElementWidthMm,
    double BarcodeElementHeightMm,
    bool BarcodeTruncated,
    int TextAlignmentValue,
    int FillStyleValue,
    string? FillColorHex,
    double StrokeThicknessMm,
    string? ItemColorHex)
{
    /// <summary>Stored clockwise rotation in degrees (0 when absent).</summary>
    public double RotationDegrees { get; init; }

    /// <summary>Vendor anchoring point value; 0 (LeftTop) when absent.</summary>
    public int AnchoringPoint { get; init; }

    /// <summary>LOGFONT underline flag of a text item.</summary>
    public bool Underline { get; init; }

    /// <summary>Identifier of the referenced data source, when the item binds to one.</summary>
    public string? VariableId { get; init; }

    /// <summary>
    /// Stored horizontal font scaling in percent (vendor <c>ScaleFactor</c>,
    /// default 100): glyphs of a position text are drawn at this fraction
    /// of their natural width.
    /// </summary>
    public double ScaleFactorPercent { get; init; } = 100;

    /// <summary>
    /// Vendor <c>BestFitType</c> of a framed text: None 0, ScaleByFontSize 1,
    /// ScaleByFontScaling 2, VariableHeight 3.
    /// </summary>
    public int BestFitValue { get; init; }

    /// <summary>Stored font-size bounds (points) for ScaleByFontSize; 0 when absent.</summary>
    public double BestFitMinimumFontSizePt { get; init; }

    public double BestFitMaximumFontSizePt { get; init; }

    /// <summary>Stored font-scaling bounds (percent) for ScaleByFontScaling; 0 when absent.</summary>
    public double BestFitMinimumScalePercent { get; init; }

    public double BestFitMaximumScalePercent { get; init; }

    /// <summary>
    /// The item's <c>DataSourceReference</c> names a data source the package
    /// does not declare, so its value cannot be known.
    /// </summary>
    public bool HasUnresolvedReference { get; init; }

    /// <summary>
    /// For items whose <c>Contents</c> mix fixed text with data-source
    /// references: the stored text with each reference written as
    /// <c>[id]</c>. Null when the contents are plain.
    /// </summary>
    public string? ContentTemplate { get; init; }

    /// <summary>
    /// Stored <c>HumanInterpretationPosition</c> (vendor enum
    /// <c>HumanInterpretationPosition</c>: NoInterpretation 0, Below 1,
    /// Above 2, FloatingAbove 3, FloatingBelow 4); -1 when the element is
    /// absent, which the vendor loader reads as Below.
    /// </summary>
    public int BarcodeHriPosition { get; init; } = -1;

    /// <summary>Base64 payload of an embedded graphic, or null when none is stored.</summary>
    public string? GraphicBase64 { get; init; }

    /// <summary>
    /// Vendor <c>GraphicResizeMode</c>: None 0, Fill 1, Uniform 2,
    /// UniformFill 3, Tile 4.
    /// </summary>
    public int GraphicResizeMode { get; init; }

    /// <summary>Stored <c>KeepAspectRatio</c> flag of a graphic item.</summary>
    public bool GraphicKeepAspectRatio { get; init; }
}

/// <summary>
/// One variable declared by the solution entry inside a NiceLabel 10
/// package, with the facts ANLAbel can act on.
/// </summary>
/// <param name="Id">Identifier objects reference.</param>
/// <param name="Name">Operator-visible name.</param>
/// <param name="PromptText">Question shown at print time; null when none is stored.</param>
/// <param name="DefaultValue">Stored default (string contents) or empty.</param>
/// <param name="MaxLength">Stored length constraint; 0 when none.</param>
/// <param name="Required">Stored <c>IsRequired</c> constraint.</param>
/// <param name="DigitsOnly">Stored allowed-character set is exactly the ten digits.</param>
/// <param name="IsNotPrompted">The variable is not asked at print time (fed by functions or data).</param>
/// <param name="SystemDateFormat">
/// For a current-date/time variable (<c>SystemDateTimeContents</c>): the
/// stored .NET output format; null for every other variable.
/// </param>
/// <param name="SystemDateOffsetDays">Stored day offset of a current-date variable.</param>
/// <param name="FunctionTemplate">
/// For a variable computed by an <c>ExtendedDataValueFunction</c>, or a
/// solution <c>ConcatenateFunction</c>: the stored text with each
/// referenced data source written as <c>[id]</c>; null otherwise.
/// </param>
/// <param name="DataFieldName">
/// For a database field (<c>DBField</c>/<c>CsvDBField</c>): the stored
/// column name; null for every other data source.
/// </param>
public sealed record NiceLabel10Variable(
    string Id,
    string Name,
    string? PromptText,
    string DefaultValue,
    int MaxLength,
    bool Required,
    bool DigitsOnly,
    bool IsNotPrompted,
    string? SystemDateFormat,
    int SystemDateOffsetDays,
    string? FunctionTemplate,
    string? DataFieldName = null);

/// <summary>
/// A parsed NiceLabel 10 label: stored name, label size in millimetres,
/// every document item found in the format XML (converted or not), and the
/// variables declared by the solution entry inside the same package, keyed
/// by the identifier objects reference.
/// </summary>
public sealed record NiceLabel10Document(
    string Name,
    double WidthMm,
    double HeightMm,
    IReadOnlyList<NiceLabel10ObjectRecord> Objects,
    IReadOnlyDictionary<string, NiceLabel10Variable> VariablesById)
{
    /// <summary>Stored document items, excluding the background placeholder.</summary>
    public int TotalObjectCount => Objects.Count;
}
