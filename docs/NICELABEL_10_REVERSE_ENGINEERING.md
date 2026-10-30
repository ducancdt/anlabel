# NiceLabel 10 reverse engineering

Evidence record for the NiceLabel 10 (Designer 10.5 / PowerForms 21.5)
installation on this machine and the ANLAbel slice it supports: opening a
NiceLabel 10 `.nlbl` file as a new unsaved ANLAbel template. NiceLabel is a
reference only (see `docs/REFERENCE_POLICY.md`). On 2026-10-29 the owner
asked to drop the office-sheet ban and follow NiceLabel 10 for text as
well; §4 and §10 record what changed. The remaining print/data gates
(verified queue, DPI, scale, overflow) are unchanged.

## §1 Installation map (2026-10-27)

- Installed at `C:\Program Files\NiceLabel\NiceLabel 10`; entry points
  `NiceLabelDesigner.exe`, `NiceLabelPrint.exe`, Automation Builder/Manager
  and services in the install root.
- The application is managed .NET: `bin.net` holds ~160 assemblies. The UI
  is DevExpress WPF (v23.1); the label designer library is
  `DesignerLib.dll` (11.5 MB); the label model/loader core is `Core.dll`
  (4 MB, namespaces `EuroPlus.NiceLabel.Core.*`); shared utilities are in
  `NetCommon.dll`; ZIP handling uses a bundled DotNetZip (`Ionic.Zip`).
  Member inventories were taken with REA's managed inspection, decompiled
  source with `ilspycmd` 11; the vendor code is Dotfuscator-obfuscated
  (control flow and private names), public enums and serialization
  attributes are intact.
- 207 sample `.nlbl` files ship under
  `C:\Users\Public\Documents\NiceLabel 10\**\*.nlbl` and are the evidence
  corpus for this slice.

## §2 The .nlbl package

A `.nlbl` file is a ZIP archive whose entries use WinZip AES encryption
(compression method 99, AE extra field `0x9901`, AES-256, actual method
deflate). Two entries exist in every sampled file:

- `Formats/<label name>` — the label itself as XML.
- `<label name>.slnx` — the solution XML declaring variables, functions
  and database fields (207/207; the on-disk solution extension `.nsln`
  does not occur inside packages — an earlier revision of this document
  and of the reader assumed it, so the reader silently resolved no
  variable in any real file until 2026-10-29).

The loader chain in `Core.dll` is `a.c.a.l.k` (a `ZipLoader` plugin
registered for `*.nlbl`/`*.nsln`) over `a.c.a.l.u` (`IZipFileWrapper`),
which sets `ZipFile.Encryption = WinZipAes256` and a fixed package
password. That password is stored in the wrapper as a base64 blob and
decoded at runtime by `a.b.d.e.j` (in `NetCommon.dll`): AES-128-CBC
decryption with a key/IV/salt triple itself de-obfuscated by
`a.a.a.a.b` (per-character XOR keystream), yielding a 20-character ASCII
password. Running the vendor decoder once through reflection recovered it;
ANLAbel's reader carries the recovered constant so it can open the
operator's own files. It is a format constant, not a user credential:
labels additionally protected by a user password would fail the AES
integrity check and be refused.

WinZip AES follows the public specification, which ANLAbel implements
from scratch in `WinZipAesArchive` (no vendor code): PBKDF2-HMAC-SHA1,
1000 iterations, deriving encryption key + HMAC key + 2-byte password
check; AES in the WinZip little-endian CTR construction; the 10-byte
truncated HMAC-SHA1 and AE-1 CRC are verified before content is used.
Unsupported methods and traditional ZipCrypto fail closed.

## §3 The format XML

Root `<EuroPlus.NiceLabel Type="Format">` with `<Name>` and
`<Media Type="FormatMedia">`; geometry integers are 1/1000 mm
(`Width 100000` = 100 mm). `Media` is the page: the label itself is the
`PageLayout` element (`ElementWidth`/`ElementHeight`), equal to the page
for a `SingleFormatLayout` (187 files) and the repeated cell of a
multi-label sheet for a `WebFormatLayout` (20 files, with
`HorizontalCount`/`VerticalCount`, offsets and gaps; every object
coordinate lies inside the element). `PrintScenarioCollection` stores
the authoring printer (`Printer/Name`, `DriverName`, a `DevModeBuffer`)
and `PrintRotation`; the printer is machine-specific and is not carried
over. Document items are `<Item Type="...">`
elements; the `BackgroundDocumentItem` placeholder exists in every file
at 0 x 0 and is not a label object. Across the 207-file corpus:

| Item type | Count | Geometry |
| --- | ---: | --- |
| TextDocumentItem | 4705 | PositionGeometry / RectGeometry |
| LineDocumentItem | 1016 | PointGeometry |
| BarcodeDocumentItem | 575 | PositionGeometry |
| GraphicDocumentItem | 126 | RectGeometry |
| RectangleDocumentItem | 30 | RectGeometry |
| Report* items | 15 | GroupGeometry |

- Every geometry (`AbstractGeometry` in `Core.dll`, `[LoadSave]`
  attributes) stores `Rotation` (double, degrees, default 0) and
  `AnchoringPoint` (`AnchoringPointEnum`, default LeftTop). The corpus
  stores rotation on 205 items (182 texts and 17 barcodes at 270, 4
  graphics at 270, 2 texts at 90) and a non-default anchor on 615 items.
  The stored point (`X`/`Y`, or `Left`/`Top` of a RectGeometry) is the
  anchor's position, not the top-left corner; see §8.
- String values never contain a raw line break: a multi-line value
  (`FixedContents`, `Contents/FixedValue/StringValue`, a variable's
  `DefaultValue`/`SampleValue`) is stored as base64 UTF-8 text (486 such
  values in the corpus, 0 raw line breaks). ANLAbel decodes a value only
  when it is well-formed base64 whose strict UTF-8 decoding is printable
  text with a line break (`NiceLabel10RichText.DecodeStoredText`); the
  one corpus value that is base64-shaped but decodes to flat nonsense is
  kept as stored.
- Text: `FontDescriptor` (Name + Height in points) and `LogFontWrapper`
  (LOGFONT; Weight >= 700 = bold, `Italic`/`Underline` 255 = set);
  content in `FixedContents`; an optional `DataSourceReference`/`Id`
  binds the item to a solution data source (§9). Mixed contents
  (`Contents[ExtendedDataValue]` with `DataSourceReferences`) store the
  text with each reference written as `[id]`.
- Framed texts (RectGeometry, `TextType` present) own their frame like
  ANLAbel TextBox; position texts are free-flowing like ANLAbel Text.
  Position texts may also store `ScaleFactor` (`TextDocumentItem` in
  `FormatDocument.dll`, default 100: horizontal font scaling in percent;
  151 position texts at 76-104). Framed texts may store `BestFit`
  (`BestFitType` None 0, ScaleByFontSize 1, ScaleByFontScaling 2,
  VariableHeight 3; 25 and 4 items) with `BestFitMinimum/MaximumFontSize`
  (points) or `BestFitMinimum/MaximumFontScaling` (percent).
- Barcode: `BarcodeData Type="..."` selects the symbology (Code128 family
  271, Code39 family 154, DataMatrix family 94, PDF417 26, EAN-13 family
  23, QR 5, GS1 DataBar 2). `BaseBarWidth` is the module (X)
  width and `ModuleHeight` the symbol height, both 1/1000 mm; two-
  dimensional types store `ModuleHeight 0` and no symbol frame (version
  and size are encoder-chosen). `HumanInterpretationPosition` is the
  vendor enum NoInterpretation 0, Below 1, Above 2, FloatingAbove 3,
  FloatingBelow 4; the vendor loader (`AbstractBarcodeData`,
  `[LoadSave(DefaultValue = Below)]`) reads an absent element as Below.
  The corpus stores an explicit 0 on 544 of 575 barcodes: their
  human-readable text is off and captions are separate text objects.
- Graphic: `RectGeometry` frame, `GraphicFileName` (authoring-machine
  path) and `EmbeddedGraphicContents[BinaryContents]/Base64StringValue`,
  which carries the payload in 89 of 126 items (75 placeable WMF, 14
  PNG); `ResizeMode` is `GraphicResizeMode` None 0, Fill 1, Uniform 2,
  UniformFill 3, Tile 4, plus `KeepAspectRatio`.
- Line: `PointGeometry` start/end points and `Thickness`.
- Rectangle: `RectGeometry` Left/Top/Width/Height, plus `Thickness`,
  `FillStyle` and `FillColor`. The vendor style enums were recovered
  from `Interfaces.dll`/`Utils.dll`: `FillStyles` = None, Clear, Solid,
  then hatch patterns; `TextBoxAlignment` = None, Left, Center, Right,
  Justified (values 2/3 in the corpus). Framed-text alignment, solid
  fills (with the ARGB colour) and line/rectangle stroke thickness are
  mapped; hatch fills keep the default outline look. Each item's ARGB
  `Color` maps to ANLAbel's `Style.StrokeColor`, which the designer and
  print renderer already use as the glyph/stroke brush — this carries
  the 10 white reverse texts in the corpus correctly instead of
  ANLAbel's slate default.

## §4 What ANLAbel converts

`NiceLabel10NlblReader` + `NiceLabel10NlblImporter`
(`src/ANLAbel.Project/NiceLabel10/`), wired into `ProjectFileService`
for `.nlbl` exactly like the `.lbl` path: a new unsaved template at the
stored size/name; the source file is never written; the open dialog
offers `Tem NiceLabel (*.lbl;*.nlbl)`. The application passes
`NiceLabel10TextMeasurer` (the designer's own FormattedText layout) so
anchored or rotated position texts are placed from their real size.

Converted: position texts (Text) and framed texts (TextBox with the
stored frame), with bold/italic/underline, colour and alignment; a
position text's `ScaleFactor` becomes the Text style factor
`FontScalePercent` (narrowing below 100, widening above 100, AutoFit
frame at the scaled width); framed-text `BestFit` ScaleByFontSize
becomes `ShrinkFont` with the stored point bounds, ScaleByFontScaling
becomes `ScaleWidth` with the stored scale bounds and VariableHeight
becomes `AdjustHeight` (width kept, height follows the content);
bindings to solution variables (`{?Name}` with the stored prompt text,
default, length, required and digits-only facts), current-date variables
(`{#NOW[+Nd]:format}`), concatenation functions and mixed contents
(expressions mixing literal text and fields), database fields
(`{Column}`); linear barcodes (Code128/GS1-128, Code39, EAN-13/Bookland)
with content, module height, authored module width
(`BarcodeWidthMode.SizedFromX`; the designer frame is seeded from the
symbology module count) and the stored human-readable placement
(None/Below/Above); QR and DataMatrix codes through ANLAbel's
AutoSizeByData contract fed with the authored module width (vendor
enums from `Barcodes.Interfaces.dll`: `QRBarcodeErrorCorrectionLevel` =
LevelL/M/Q/H at 0-3; `DatamatrixFormat` = Auto then ECC 200 C<rows>x<cols>
sizes), an explicit DataMatrix format giving an exact frame; PDF417
(§6); embedded graphics (§6); lines and rectangles. Every object is
placed through the anchor/rotation contract of §8.

GS1-128 compositions (`GS1128Function`) become ANLAbel's own
`(AI)value` notation, ANSI MH10.8.2 data-identifier fields
(`AscFactFunction`) and the ISO/IEC 15434 format 06 envelope
(`TransferDataSyntaxFunction`) become expressions with the identifier
prefixes and separator characters, and date arithmetic on the current
date (`DateAddFunction`) becomes `{#NOW+Nd+Nmo+Ny:format}` (§9).

Two print-time transformations have no `{Field}` form and are written
as ANLAbel formulas instead (`CONCAT`/`FIELD` plus the function added
for them, 2026-10-29): a GS1-128 part whose check digit NiceLabel
computes at print time (`AutoCheckDigitCalculation`) becomes
`GS1CHECKDIGIT(FIELD("?Var"))` inside the `(AI)` expression, and a
`SubsetFunction` (skip `Offset`, take `SubstringLength`) becomes
`MID(FIELD(...), start, length)`. The job-variable contract reads
prompt and date names out of formula `FIELD` calls so such labels still
prompt the operator.

Not converted (fail-closed, counted in the UI status via
`NiceLabelTotalObjectCount`): objects bound to solution functions
ANLAbel cannot reproduce (VBScript/Python `ExecuteScriptFunction`, HIBC,
delimited `SubsetFunction`, `GS1DataBarFunction`, envelope formats other
than 06, data-identifier fields printing their short title, date
arithmetic on a prompted date)
or to variables computed from them; GS1 DataBar barcodes (no ANLAbel
symbology); graphics without an embedded payload or bound to a variable;
report/group items. Rich-text framed texts (`RichTextFormatting`, 26 in
the corpus; contents stored as raw RTF, base64 RTF or base64 text) are
converted with their recovered plain text (`NiceLabel10RichText`:
paragraph/line/cell breaks kept, hex escapes in the document code page,
`\uN` with the `\ucN` fallback, formatting dropped).

## §5 Designer architecture (for the "rebuild like them" question)

`DesignerLib.dll` holds 1,676 types; 136 live in
`EuroPlus.NiceLabel.Designer.DesignSurface`. The surface is a custom WPF
canvas in the same family as ANLAbel's `LabelDesignerCanvas`:

- One `SelectionItem`/`OverlayItem` pair per object kind (Text, Barcode,
  Line, Rectangle, Ellipse, Group, report items), with dedicated thumbs
  (`MoveThumb`, `ResizeThumb`, `RotateThumb`, `LineRotateThumb`,
  `ZoomThumb`, `BarcodeResizer`) instead of generic framework editing.
- Rulers (`Ruler`, `RulerDipHelper`), an alignment grid and snap lines
  (`AlignmentGrid`, `AlignmentLines`), rubber-band multi-selection
  (`MultipleSelectionItem`, `SelectionRectangle`), and a drag-and-drop
  `Toolbox`/`ToolboxContainer`.
- Property editing is page-based MVVM: one View/ViewModel pair per
  concern (BarcodeMain, BarcodeDetails, Barcode2DDetails,
  HumanReadable, CheckDigit, BearerBar, QR, Datamatrix, Pdf, GS1DataBar,
  color separation, functions…), mirroring how ANLAbel splits its
  Properties panels.

Conclusion from evidence: NiceLabel 10's designer and ANLAbel's designer
are the same architecture (custom canvas + adorners + per-kind selection
+ MVVM property pages), built independently. A wholesale rebuild "like
them" would therefore replace ANLAbel's verified print/data contracts
with a copy of a shape ANLAbel already has, for no operator-visible gain.
The full-change direction taken instead is the one with concrete
operator value: ANLAbel now opens both NiceLabel generations natively
(`.lbl` and `.nlbl`) into its own designer, and remaining NiceLabel ideas
are adopted per `docs/REFERENCE_POLICY.md` only with a concrete need and
an acceptance check.

## §6 PDF417, graphics, HRI

- PDF417: NiceLabel 10 bundles the same ZXing.Net family ANLAbel uses
  (`zxing.dll`, `ZXing.PDF417.PDF417Writer`). The package stores
  `SecurityLevel` (vendor `Pdf417SecurityLevel` Level0-Level8 at 0-8),
  `TruncatedBarcode`, `ElementWidth`/`ElementHeight` (module width and
  row height, 1/1000 mm) but no symbol frame and no row/column layout
  (`Rows`/`CodeWordsPerRow` are 0 in all 26 items). ANLAbel therefore
  seeds the frame from the native module grid its own production encoder
  produces for the stored contents and settings
  (`ZxingBarcodeRenderer.CountMatrixModules`) times the stored element
  size — the grid the print pipeline integer-scales into — instead of a
  guessed size.
- Graphics: an earlier revision reported the embedded-content element
  empty; it is empty only where the authoring machine's file was
  missing. 89 items carry their payload: PNG payloads become ANLAbel
  images unchanged (pixel size read from IHDR); placeable WMF clipart is
  played by GDI+ (`System.Drawing.Common`, Windows only) into a
  300-DPI PNG of the authored frame (`WindowsMetafileRasterizer`), fitted
  when `ResizeMode` is Uniform or `KeepAspectRatio` is set, stretched for
  Fill. A graphic without a payload is read from its `GraphicFileName`
  when that file exists as stored or beside the label (24 corpus clipart
  files sit next to their labels); the remaining 13 (no file name, or
  bound to a variable) stay unconverted.
- Barcode detail mapping: `Code128BBarcodeData` pins
  `Code128CodeSet.B`, and the `HumanInterpretationFontDescriptor` height
  becomes `BarcodeTextFontSizePt`. Code 39 carries `HasCheckDigit`
  (modulo-43 check character → `BarcodeCheckDigitPolicy.Generate`),
  `DisplayCheckDigit` (→ `BarcodeHriShowCheckDigit`), `Ratio` (2, 2.2,
  2.5, 3 → `Code39WideNarrowRatio`) and `InterCharacterGap` (narrow-bar
  widths). Line/rectangle `LineStyle` (vendor `LineStyles` flags: Solid
  1, Dash 2, Dot 4, DashDot 8, DashDotDot 16, Clear 32) maps onto
  `OutlineStyle`, rectangle `Radius` onto the corner radius. `HumanInterpretationPosition` maps
  onto `BarcodeHriPlacement` (0 → None, 2/3 → Above, otherwise Below,
  absent → Below per the vendor loader default); check-digit flags align
  with ANLAbel defaults and are not mapped.

## §7 Evidence

- Production reader/importer sweep over all 207 installed sample files
  (2026-10-29, after the solution-entry fix and the placement/data-source
  mappings): 207/207 opened; 6,467 stored objects, 6,233 converted
  (96.4%); unconverted = 85 barcodes and 117 texts bound to script/HIBC/
  delimited-subset functions or date arithmetic on prompted dates (or to
  variables computed from them, 40 of the total), 13 graphics without
  payload or file, or bound to a variable, 4 rich-text templates
  referencing such functions, 15 report items. The earlier figure of 97.4% counted
  bound objects whose binding had silently fallen back to their sample
  text because no variable resolved (§2); they now either bind or are
  reported.
- 2026-10-30, after translating one-statement concatenating scripts
  (`ExecuteScriptFunction`, `Result = "UN" & SupplierID & [Package Number]`:
  52 of the 68 sample scripts, the license plates and data-identifier fields
  of the GTL, VDA 4902 and GM1724 labels): 6,345 converted (98.1%);
  unconverted = 33 barcodes and 57 texts bound to other scripts (UCase,
  Left/Right, check-digit helpers), HIBC or date arithmetic on prompted
  dates, 13 graphics, 4 rich-text templates, 15 report items. The 12 local
  customer labels convert 334/334.
- 205 rotated and 615 anchored objects are placed through §8; the
  importer's frames are consistent with the vendor runtime geometry for
  every quarter turn (unit tests `NiceLabel10PlacementTests`).
- Regression gate `opening a niceLabel 10 nlbl imports its label as a
  new template` builds a synthetic WinZip-AES-256 package with an
  independent test-side writer (public spec) and a `.slnx` solution
  entry; it asserts size/name, text, variable binding with prompt facts,
  a `#NOW` date field, TextBox frame, barcode module width and HRI
  placement, a rotated RightTop-anchored text placed from the
  application measurer, a CenterMiddle-anchored barcode, an embedded PNG
  graphic, line, rectangle, the stored-vs-converted counts, the
  print-pipeline render, and that a tampered package is refused by its
  integrity check. Unit tests `NiceLabel10ImportMappingTests` cover the
  XML mapping on decrypted entries, including a hand-built placeable WMF
  rasterised at the frame.
- The vendor-produced files remain the primary oracle: the synthetic
  writer and the production reader were both validated against them,
  not only against each other.
- Render sweep (2026-10-27): every imported template (207/207)
  rasterizes through the production `LabelVisualRenderer` with zero
  failures. Three automation samples render blank with an empty data
  row because every object on them is variable-bound and their data
  arrives from CSV/XML at print time — the faithful import of those
  files, not a defect.

## §8 Placement: anchoring point and rotation

Evidence: `EuroPlus.NiceLabel.Interfaces.AnchoringPointEnum` (LeftTop,
CenterTop, RightTop, LeftMiddle, CenterMiddle, RightMiddle, LeftBottom,
CenterBottom, RightBottom, None) and `Rotations` (0/90/180/270 only) in
`Interfaces.dll`; the runtime geometry `a.c.a.e.s` in `Core.dll`, whose
anchor-to-bounds conversion uses width/height multipliers
(`GetWidthAnchoringMultiplier` 0/0.5/1 for Left/Center/Right,
`GetHeightAnchoringMultiplier` for Top/Middle/Bottom) and the rotation
quadrant `floor(rotation / 90)`; and, for the rotation sense, the 13
corpus texts that store both a LOGFONT `Escapement` and a geometry
rotation (Escapement 900 = GDI 90° counter-clockwise ↔ Rotation 270,
Escapement 2700 ↔ Rotation 90), so the stored rotation is clockwise,
the same sense as WPF's `RotateTransform`.

Contract (implemented in `NiceLabel10Placement`): with the unrotated
object size W × H, multipliers wm/hm, and the stored point P, the
axis-aligned box of the rotated object has its top-left at P − offset,
where offset is (W·wm, H·hm) at 0°, (H·(1−hm), W·wm) at 90°,
(W·(1−wm), H·(1−hm)) at 180° and (H·hm, W·(1−wm)) at 270°, and the box is
W × H (0°/180°) or H × W (90°/270°). ANLAbel stores the unrotated frame
and rotates it about its own centre, so the frame is centred on that
box and carries the same rotation. RectGeometry items follow the same
rule with `Left`/`Top` as P: the corpus confirms it (a LeftBottom-anchored
framed text stores Top 68.4 mm with Height 19.7 mm on a 70 mm label).

Object sizes used: framed texts, rectangles and graphics have their
stored frame; barcodes use the frame the importer seeds; position texts
are measured with the application's text layout (`INiceLabel10TextMeasurer`),
or, without one, estimated from font metrics (0.55 em per character,
1.2 em per line). Variable-bound 2D codes have no size until data
arrives, so their anchor offset is zero.

## §9 Solution data sources

The `.slnx` entry lists `Variables`, `Functions` and `Databases`. Of the
3,228 object references in the corpus, 2,593 point at variables, 182 at
`ExecuteScriptFunction`, 112 at `ConcatenateFunction`, 96 at
`AscFactFunction`, 79 at `TransferDataSyntaxFunction`, 63 at database
fields (`DBField`/`CsvDBField`), 51 at `GS1128Function`, 28 at
`SubsetFunction`, 10 at `HibcFunction`, 8 at `DateAddFunction`, 2 at
`GS1DataBarFunction` (and 4 at bare references).

Variable facts read: `PromptText`, `DefaultValue` (`StringContents`
StringValue, `PickListContents` SelectedValue, `DateTimeContents`
UserValue with "." meaning empty), `Constraints/Length` and
`IsRequired`, `AllowedCharactersForCustomFormat` = "0123456789" (digits
only), `IsNotPrompted`. A `SystemDateTimeContents` default marks a
current-date variable: `OutputFormat` (a .NET format string) and
`Offset/Days` become `{#NOW[+Nd]:format}`. A variable with an
`InternalFunction[ExtendedDataValueFunction]` stores a template whose
`[id]` references expand recursively (depth-limited) into the referenced
sources' expressions; `ConcatenateFunction` (data values joined by
`Separator`) is read into the same template form. Database fields bind
by `FieldName` as `{Column}`. Counters (`IncrementType`, 8 variables)
are read as plain prompts; their increment is not reproduced.

`GS1128Function` (`AdvancedFunctions.dll`): a list of `GS1128DataValue`
parts, each with `ApplicationIdentifierId` (an index into the vendor AI
table, the embedded resource
`EuroPlus.NiceLabel.Functions.Resources.ApplicationIdentifiers.xml`,
version 6.5, 217 rows; attribute bit 16 = value carries a check digit,
bit 64 = the stored `Multiplier` digit completes a three-digit AI such
as 310 → 3103), `AutoCheckDigitCalculation` and a data source. The table
is carried in `NiceLabel10Gs1ApplicationIdentifiers`; each part becomes
`(AI)[id]`, date AIs (11-17) asking the referenced date for `yyMMdd`, so
the function reads exactly like ANLAbel's `(AI)value` authoring notation
(`BarcodeApplicationContract`). A part with auto check-digit
calculation is wrapped in `GS1CHECKDIGIT(...)` and the whole binding
written as a formula (§4).
`DateAddFunction` stores Day/Month/Year offsets, an input date source and
an `OutputFormat`; when the input is (a chain of) current-date sources
the offsets add up into one `{#NOW...}` field, otherwise it stays
unresolved. Corpus: 30 GS1-128 functions (12 contain an auto check-digit
part), 15 date-add functions.

`AscFactFunction` composes ANSI MH10.8.2 data-identifier fields: each
`AscFactDataValue` names its identifier (`AscFactIdentifierName`, 0-2
digits and a capital letter from the embedded catalogue
`EuroPlus.NiceLabel.Functions.AscFact.Resources.AscFactIdentifiers.xml`,
version 7.0) and a data source or fixed value; `IsUsingShortTitle`
(false on all 1,243 corpus fields) would print the catalogue title
instead. ANLAbel writes identifier + value per field, several fields
joined by the group separator U+001D. `TransferDataSyntaxFunction` wraps
its data values in the ISO/IEC 15434 envelope; the vendor's fixed-field
formats build `[)>` RS format GS field … RS EOT (decompiled), and format
06 — the only one in the corpus (81 functions, 79 wrapping one
`AscFactFunction`) — carries data-identifier fields separated by GS. The
corpus uses single-identifier functions on 94 Code 39/Code 128
barcodes (e.g. AIAG `1S` serials) and the envelope on 79 DataMatrix/
PDF417 codes (ECIA/automotive 2D labels).

## §10 Printer and paper

Evidence: the serialized members of the vendor classes `PageMedia`
(`FormatMedia`, `Core.dll`), `PaperDetails` and the enums `MediaType`
(Unknown, Sheet, Roll; default Roll), `PrintRotationType` (Undefined,
Normal, UpsideDown) and `WebSettings` (`Interfaces.dll`),
`AbstractPageLayout`/`WebPageLayout` (`Core.dll`), `PrintScenario`
(`FormatDocument.dll`) and `Printer` (`Core.dll`), all read from their
`[LoadSave]` attributes in the decompiled sources; and every
`DevModeBuffer` of the 207-file corpus decoded as a Windows DEVMODEW
(public `wingdi.h` layout; the vendor converter is plain base64).

- `Media/Width`/`Height` are the page (sheet) or roll section in 1/1000 mm
  whatever `MediaUnit` says (`MeasurementUnit` is display only).
  `Media/PaperFormat` is the Windows `DMPAPER_*` id — 9 A4, 1 Letter, 256
  user-defined — equal to the DEVMODE `dmPaperSize` in all 124 files whose
  DEVMODE carries one; `PaperDetails/Name` is the driver's paper name.
  For label printers the DEVMODE `dmPaperWidth`/`dmPaperLength` (0.1 mm)
  equal the media size, and `dmPrintQuality` is the driver resolution
  (203, 300 or 600 in the corpus).
- `PageLayout`: `ElementWidth`/`ElementHeight` (the label), the first
  label's `HorizontalOffset`/`VerticalOffset` on the page, corner radii
  and corrections; a `WebFormatLayout` adds `HorizontalCount`/
  `VerticalCount`, `HorizontalGap`/`VerticalGap` (1/1000 mm) and the
  `WebSettings` flags — Horizontal 1 / Vertical 2 (fill order), Top 4,
  Bottom 8, Left 0x10, Right 0x20 (start corner). Every multi-label
  file in the corpus stores 21 (across then down from the top-left).
- `PrintScenario`: `Printer` (`Name`, `DriverName`, `DevModeBuffer`,
  `UseDefaultPrinterSettings` — paper size and resolution from the driver
  rather than the label), `UseDefaultPrinter`, `PrintRotation`, cutter and
  double-sided options.

Corpus: 104 labels take the driver's settings; 92 are authored on office
sheets (A4/Letter, mostly on the XPS writer, 14 of them with several
labels per sheet); 11 are one label per page on label stock (Zebra/CAB
rolls, or label-sized pages). No file stores `PrintRotation` UpsideDown
or a landscape DEVMODE.

ANLAbel (`NiceLabel10NlblImporter.ApplyPrintSetup`), following NiceLabel
(owner request 2026-10-29): driver settings → `SettingsSource=Driver`
(driver-automatic paper; a multi-label grid keeps its sheet size);
otherwise the stored page is the physical media — label stock, an office
sheet or a sheet of several labels — with label settings, manual paper
size (`MediaProvenance=UserCustom`), the first label's offsets, the
label grid (`PrinterProfile.LabelsAcross/LabelsDown`, gaps, fill order,
start corner) and the DEVMODE resolution when `IndustrialPrintDpiContract`
accepts it; UpsideDown → `Rotated180`; `StockName` → `LabelStockName`.
`LabelStockContract` no longer refuses office sheets (edges 8–1000 mm);
`SheetLayoutContract` checks that the grid lies on the sheet, and the
print service then puts each row's label in its cell, one sheet per page.
The authoring printer is never selected — a queue must be chosen and
verified on this machine — but the import status names it
(`NiceLabelPrinterSetupText`). Sweep: 104 driver settings, 78 office
sheets, 14 multi-label sheets, 11 label stocks; every applied page, grid
and DPI passes the contracts.

### §10.1 Label Properties: Label Dimensions and Paper (2026-10-30)

Evidence: the decompiled `DesignerLib` views and view models
`FormatDimensionsPropertyPageView(Model)` and `FormatPaperPropertyPageView(Model)`,
`ProcessingOrder` and `ProcessingOrderType` (`Interfaces.dll`), the margin and
grid helpers `a.c.a.k.c` (`Core.dll`) and the strings of
`FormatPropertyDialogXaml.resx`.

- **Paper** page: *Paper Type* — "Automatically set page size based on label
  dimensions (labels on a roll)" or "Manually set page size (sheets of
  paper)" (`Media.Type` Roll/Sheet); *Paper* lists the driver's papers
  (`IPrinterService.GetPaperDetails(printer, DevMode)`, sorted by name) and
  selects the one whose id equals `Media.PaperFormat.Id` (the `DMPAPER_*`
  id), else the first; width/height are editable only for the custom paper.
  Choosing a paper stores it in `Media.PaperFormat`; only the new-label wizard
  also makes the label that size. *Orientation* Portrait/Landscape
  (`Media.Orientation` 1/2) and *Rotated* (`PrintRotation` UpsideDown);
  *Preview* shows the printer and screen layout.
- **Label Dimensions** page: width/height (`PageLayout.ElementWidth/Height`);
  *Margins* — left/top are `PageLayout.HorizontalOffset/VerticalOffset`, right
  is `Media.Width − PageLayout.GetTotalLayoutWidth()` and bottom likewise
  (what the paper leaves after the labels); *Labels Across* — horizontal and
  vertical count and gap, *Processing order* (eight `ProcessingOrderType`
  values: Horizontally/Vertically × start at top left 21 / top right 37 /
  bottom left 25 / bottom right 41 for horizontal, 22/38/26/42 for vertical —
  the `WebSettings` flags of §10); *Automatic labels across*; corner radius
  and variable label size (receipts).

ANLAbel (`PrinterSetupWindow`, Thuộc tính tem) now has the same two pages for
paper size and paper choice: Kích thước tem › Lề (Trái/Trên = the profile
offsets, Phải/Dưới computed by `SheetLayoutContract.ExtentMm`), Số tem trên
trang (counts, gaps, Thứ tự in with the eight orders, "Tính số tem vừa tờ" =
`SheetLayoutContract.FitCount`), and Giấy › Loại giấy, Giấy (the driver's
papers), Xoay 180°, Xem trước (the page with numbered cells). Apply refuses a
page whose labels leave the sheet (`SheetLayoutContract.EvaluatePlacement`).
A label on a larger sheet chosen by hand, and every grid, prints whole-sheet
pages with the label at its margins (`SheetLayoutContract.PrintsWholeSheet`);
before, a single label on A5/A4 went to the driver as a page of the label's
size. Rotated 180° turns the whole sheet. Not built: landscape paper
orientation, custom paper sizes, corner radius, variable label size and the
stock catalogue (owner: no software stock catalogue).

## §11 Data Matrix symbol size (2026-10-30)

Evidence (installed NiceLabel 10, `bin.net`):

- `DesignerLib` `BarcodeDatamatrixPropertyPageViewModel` lists the *Format*
  choices: Automatic, 24 square sizes 10x10…144x144, six rectangular ECC 200
  sizes 8x18, 8x32, 12x26, 12x36, 16x36, 16x48, and DMRE sizes (ISO/IEC 21471,
  8x48…26x64, plus *AutoDmre*). Stored as `DatamatrixFormat`: Auto 0, the
  square sizes 1–24, the rectangular sizes 25–30.
- Barcodes are drawn by the bundled Neodynamic library
  (`Neodynamic.WPF.Barcode.dll`). Its automatic search walks the first 30
  sizes and takes the first that holds the data codewords **and is square**,
  unless `DataMatrixIncludeRectFormatsInAutoMode` is set ("whether to consider
  Rectangular DataMatrix Formats when DataMatrixFormat is set to Auto. Default
  is false"). `Barcodes.Core` never sets it, so NiceLabel's Automatic is the
  smallest square symbol. *AutoDmre* searches the DMRE sizes only.
- A chosen size whose capacity is below the data codewords is not enlarged:
  the library throws `BarcodeException(15000)` and the label does not print.
- Corpus: 88 Data Matrix codes in the 207 sample labels; 29 use a chosen
  size (`DatamatrixFormat` 15 = 52x52 ×28, 10 = 32x32 ×1), none DMRE.

ANLAbel before: `DatamatrixFormat` only sized the imported frame; the encoder
(ZXing, `SymbolShapeHint.FORCE_NONE`) picked its own size and could pick a
rectangle in Auto (eight letters gave 8x32, printed as a 19 x 4.7 mm strip in a
20 x 20 mm frame). Now (`DataMatrixSymbolSize`, `DataMatrixSymbolSizes`):
Auto encodes `FORCE_SQUARE`; a chosen size encodes exactly that size
(`MIN_SIZE` = `MAX_SIZE`, square or rectangle shape); data too long for it is
refused by preflight and marked in the designer ("dữ liệu (N ký tự) không vừa
khổ Data Matrix R × C đã chọn; chọn khổ lớn hơn hoặc Tự động"); choosing a
rectangular size gives the frame the symbol's proportions with its quiet zone;
import keeps the vendor size (DMRE stays Auto: the encoder has no DMRE). UI:
Mã vạch › Chi tiết 2D and Thuộc tính › Kiểu › Khổ Data Matrix. Gate: `data
matrix sizes print as chosen`.
