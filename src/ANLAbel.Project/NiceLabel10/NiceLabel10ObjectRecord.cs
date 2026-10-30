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
    /// The item stored rich text (<c>RichTextFormatting</c>); <see cref="Content"/>
    /// and <see cref="ContentTemplate"/> then hold its recovered plain text.
    /// </summary>
    public bool IsRichText { get; init; }

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
    /// Vendor content mask (<c>HasMaskedContents</c> + <c>ContentsMask</c>);
    /// null when the item is not masked.
    /// </summary>
    public string? ContentMask { get; init; }

    /// <summary>Vendor <c>MaskValue</c>: the mask character (default <c>*</c>).</summary>
    public string ContentMaskCharacter { get; init; } = "*";

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

    /// <summary>
    /// Stored <c>ZOrder</c>: NiceLabel draws objects from the lowest to the
    /// highest, whatever their order in the file.
    /// </summary>
    public int ZOrder { get; init; }

    /// <summary>Stored <c>IsPrintable</c> (default true); false keeps the object on screen only.</summary>
    public bool IsPrintable { get; init; } = true;

    /// <summary>Stored <c>PositionLocked</c>: the object cannot be moved in the designer.</summary>
    public bool PositionLocked { get; init; }

    /// <summary>Stored text <c>InverseText</c>: white glyphs on a black box.</summary>
    public bool InverseText { get; init; }

    /// <summary>
    /// Visibility conditions of the item and of the layer holding it; the
    /// object prints only when all hold.
    /// </summary>
    public IReadOnlyList<NiceLabel10VisibilityCondition> VisibilityConditions { get; init; } = Array.Empty<NiceLabel10VisibilityCondition>();

    /// <summary>
    /// Stored HRI font name (<c>HumanInterpretationFontDescriptor</c>); null
    /// when absent, which the vendor loader reads as Arial 10 bold.
    /// </summary>
    public string? BarcodeHriFontName { get; init; }

    /// <summary>The stored HRI font is bold (LOGFONT weight 600 or more); true when no font is stored.</summary>
    public bool BarcodeHriBold { get; init; } = true;

    /// <summary>
    /// Vendor <c>AutoFontScaling</c> (default true): the HRI is drawn eight
    /// narrow bars high instead of at the stored font size.
    /// </summary>
    public bool BarcodeHriAutoFontScaling { get; init; } = true;

    /// <summary>
    /// Vendor <c>LineStyles</c> flags of a line/rectangle stroke: Solid 1,
    /// Dash 2, Dot 4, DashDot 8, DashDotDot 16, Clear 32; 0 when absent.
    /// </summary>
    public int LineStyleValue { get; init; }

    /// <summary>Stored rectangle corner radius in millimetres.</summary>
    public double CornerRadiusMm { get; init; }

    /// <summary>Stored <c>HasCheckDigit</c> (the symbol carries a computed check character).</summary>
    public bool BarcodeHasCheckDigit { get; init; }

    /// <summary>Stored <c>DisplayCheckDigit</c> (the check character appears in the HRI text).</summary>
    public bool BarcodeDisplayCheckDigit { get; init; }

    /// <summary>Stored Code 39 <c>InterCharacterGap</c> in narrow-bar widths; 0 when absent.</summary>
    public int BarcodeIntercharacterGap { get; init; }

    /// <summary>Base64 payload of an embedded graphic, or null when none is stored.</summary>
    public string? GraphicBase64 { get; init; }

    /// <summary>Stored <c>GraphicFileName</c>: the authoring-machine path of the graphic file.</summary>
    public string? GraphicFileName { get; init; }

    /// <summary>
    /// Vendor <c>GraphicResizeMode</c>: None 0, Fill 1, Uniform 2,
    /// UniformFill 3, Tile 4.
    /// </summary>
    public int GraphicResizeMode { get; init; }

    /// <summary>Stored <c>KeepAspectRatio</c> flag of a graphic item.</summary>
    public bool GraphicKeepAspectRatio { get; init; }

    /// <summary>Stored <c>HorizontalMirror</c> of a graphic item ("Flip picture horizontal").</summary>
    public bool GraphicFlipHorizontal { get; init; }

    /// <summary>Stored <c>VerticalMirror</c> of a graphic item ("Flip picture vertical").</summary>
    public bool GraphicFlipVertical { get; init; }
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
/// <param name="SystemDateOffsetMonths">Stored month offset (DateAddFunction).</param>
/// <param name="SystemDateOffsetYears">Stored year offset (DateAddFunction).</param>
/// <param name="DateInputId">
/// For a <c>DateAddFunction</c>: the data source whose date the offsets
/// apply to; null for a variable's own current date.
/// </param>
/// <param name="PickList">Stored pick list values (<c>PickListContents</c>); null when the variable has none.</param>
/// <param name="Prefix">Stored output prefix, spaces kept.</param>
/// <param name="Suffix">Stored output suffix, spaces kept.</param>
/// <param name="Padding">Stored padding: 0 none, 1 on left, 2 on right, 3 surrounding.</param>
/// <param name="PaddingCharacter">Stored padding character (<c>PaddingValue</c>).</param>
/// <param name="FixedLength">Stored "Fixed length" constraint.</param>
/// <param name="AllowedCharacters">Stored allowed-character set other than the ten digits.</param>
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
    string? DataFieldName = null,
    int SystemDateOffsetMonths = 0,
    int SystemDateOffsetYears = 0,
    string? DateInputId = null,
    IReadOnlyList<string>? PickList = null,
    string? Prefix = null,
    string? Suffix = null,
    int Padding = 0,
    string? PaddingCharacter = null,
    bool FixedLength = false,
    string? AllowedCharacters = null)
{
    /// <summary>
    /// The design-time sample NiceLabel shows for a database field (its
    /// stored <c>SampleValue</c>); null for other data sources.
    /// </summary>
    public string? SampleValue { get; init; }

    /// <summary>For a prompted date (<c>DateTimeContents</c>): the stored output format; null otherwise.</summary>
    public string? DateFormat { get; init; }

    /// <summary>For a prompted date: the stored input format the operator types it in.</summary>
    public string? DateInputFormat { get; init; }
}

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

    /// <summary>Stored printer and media facts; null when the format stores none.</summary>
    public NiceLabel10PrintSetup? PrintSetup { get; init; }
}

/// <summary>One side of a NiceLabel visibility condition: a data source by id, or fixed text.</summary>
public sealed record NiceLabel10ConditionValue(string? SourceId, string? FixedText);

/// <summary>
/// A NiceLabel 10 visibility condition (item or layer): the left value
/// equals the right one ignoring case, or with <see cref="Unequal"/> differs
/// from it (vendor <c>ComparisonOperator</c> Equals 0, Unequals 1).
/// </summary>
public sealed record NiceLabel10VisibilityCondition(NiceLabel10ConditionValue Left, NiceLabel10ConditionValue Right, bool Unequal);
