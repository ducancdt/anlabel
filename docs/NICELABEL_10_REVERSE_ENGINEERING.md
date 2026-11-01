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
  `Code128CodeSet.B`; the human-readable text font and size follow §12. Code 39 carries `HasCheckDigit`
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
- 2026-10-30, after the formula text functions (UPPER, LOWER, TRIM, LEFT,
  RIGHT, with MID) and scripts calling UCase, LCase, Trim, Left, Right and
  Mid inside the concatenation: 62 of the 68 sample scripts translate;
  6,351 converted (98.2%). The 6 left are ISBT 128 check helpers
  (`ISO7064Mod37_2`, `ISBTNumericPair`) and `Right(x, Len(x)-1)`
  arithmetic; unconverted = 33 barcodes and 51 texts (HIBC, date
  arithmetic on prompted dates, variables computed from them), 13 graphics,
  4 rich-text templates, 15 report items.
- Print-and-scan sweep (2026-10-30, scratch tool, not committed): every
  sample label imported, printed by `LabelVisualRenderer` at 300 dpi with the
  designer's design-time row (prompt defaults, today's date), and every
  converted barcode decoded with ZXing inside its own frame (an X-sized code
  at its production width). Before: 370 of 527 decoded. Fixed in the
  importer: (1) linear frames counted by the shipped encoder instead of one
  Code 128 character per stored character (GS1-128 numeric runs pack two
  digits), so centre- and right-anchored codes no longer land off the left
  edge (GS1-128 sample at x = -32 mm); (2) a bound code whose stored
  contents are a placeholder or stale is sized from the design-time value and
  placed again at its anchor, and a bound automatic QR/Data Matrix keeps a
  frame holding both the stored contents and that value at the authored
  module (it kept the default 20 x 8 mm: VDA 4992 squeezed 679 characters
  into 8 mm, ECIA codes printed two-dot modules); (3) a variable that only
  mirrors another data source (`DataSourceReference` + default `[id]`, the
  database wizard's `Products.ProdCode`) binds that source instead of
  printing "[id]". After: 465 decoded. The 58 left are the fixed 52x52 VDA
  4994 envelopes refused as NiceLabel refuses them (296-322 characters),
  sample data that is not valid for its symbology (GS1 (01) with 15 digits,
  (15) "20.05.16", EAN-13 wrong check digit, Code 39 "*...*"), an X of
  0.13 mm, a 5-digit add-on typed as EAN-13, and layouts whose neighbours
  sit in the quiet zone. Local customer labels: the two codes not decoded
  hold a stray line break ("10062648" + CR LF, typed into the fixed text) and
  are refused by preflight with the character and position.
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
1.2 em per line). Bound barcodes are sized from the design-time row
once every prompt is known (§7, 2026-10-30) and placed again at their
anchor.

The anchor also decides where a code grows when a row's data is longer or
shorter than the design value: NiceLabel keeps the anchoring point. ANLAbel
does the same for X-sized linear codes (`LinearBarcodeProductionWidth.
AnchorOffsetMm`): print, designer and the preflight edge check place the
row's symbol at (frame width - symbol width) x anchor fraction along the
frame, and syncing the frame to the production width keeps the anchor
(`ObjectAnchorContract.Resize`). Top-left objects, ANLAbel's default, keep
their left edge as before. Gate: `x-sized barcodes grow around their
anchoring point`.

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

## §12 Barcode human-readable text (2026-10-30)

- Default font: `AbstractBarcodeData.GetDefaultFontDescriptor` is Arial,
  10 pt, LOGFONT weight 700 (bold). A barcode without a stored
  `HumanInterpretationFontDescriptor` prints its text in Arial bold.
- Size: `AutoFontScaling` (LoadSave default **true**) makes the encoder
  (`Barcodes.Core` base encoder) set the text height to 8 × the narrow bar
  width in device dots (`GetNarrowBarWidthInDeviceDots`, at least 1 dot);
  only with auto scaling off does the stored descriptor height (points)
  apply. `IsCustomHumanReadableFont` only says whether the font editors
  are enabled. The EAN/UPC encoder family uses another expression,
  not reproduced (no customer label uses it).
- Drawing: `BarcodeItem` draws the text with WPF `FormattedText`, em size
  = that height in device dots, the descriptor's face name and its weight
  and style, under (or over) the bars. `ModuleHeight` is the bar height;
  the text adds to the object's height, and the stored position and anchor
  refer to the whole object.
- Corpus: 30 of the 71 barcodes in the 12 local customer labels and 31 of
  the 575 in the 207 samples show their text; all customer ones use the
  default font and auto scaling (X = 0.125/0.25/0.375 mm).

ANLAbel before: the importer took the descriptor's 10 pt as the HRI size,
drew it in Segoe UI and kept the frame at `ModuleHeight`, so the text strip
ate the bars: 23 of the 30 customer codes were refused by preflight
("Barcode frame is too short for the HRI strip"), the rest printed bars
1–3 mm high. Now: `LabelObject.BarcodeHriFontFamily`/`BarcodeHriBold`
(designer, print, preflight and the layout share one typeface,
`BarcodeHriTextLayout.HriTypeface`); import sets the stored font (Arial bold
by default), the auto-scaled size (8 × X, at least ANLAbel's 5 pt HRI
minimum) and grows the frame by the measured text strip, so the bars keep
NiceLabel's height. The designer drew HRI text at the point value in DIPs
(75 % of print); it now uses the print size. UI: Mã vạch › Chi tiết mã and
Thuộc tính › Kiểu (Phông HRI, Đậm). Gate: `imported barcode text prints like
NiceLabel`.

## §13 Stacking, layers and visibility conditions (2026-10-30)

- Stacking: every item stores `ZOrder`; NiceLabel draws from the lowest.
  The file order differs from it in 157 of the 207 sample labels (1 of the
  12 local customer labels). Layer items carry the layer in their value
  (layer 20000 holds items 20001…), so the item `ZOrder` alone gives the
  stacking. ANLAbel imported every object at `ZIndex` 0 (file order); the
  importer now ranks `ZIndex` by `ZOrder` (equal values keep file order).
- Groups: the format's `GroupedItems` holds one
  `SolutionItemReferenceCollectionWrapper` per group listing member ids (3
  sample labels); imported members share one ANLAbel `GroupId`.
- `IsPrintable` False (4 sample items): kept as ANLAbel "Không in".
- `PositionLocked` (4 sample items) imports as ANLAbel "Khóa vị trí"; text
  `InverseText` (4 sample items, white glyphs on a black box) imports as the
  Text plus an ANLAbel Inverse area with the text's frame, stacked right above.
  Not used by the corpus and not built: `Mirror`, `RTLPrinting`, `Stacked`,
  `SizeGroup`/`UseSameSize`, `IgnoreExcessiveContent`, `AlternativeRendering`.
- Rectangle `FillStyle` (vendor `FillStyles`: None 0, Clear 1, Solid 2, then
  hatches): only Solid fills. ANLAbel's style default is a solid white fill,
  and the importer set the style only for Solid, so every open NiceLabel box
  became opaque and hid what lay under it (11 customer table labels draw the
  frame box after their captions). Open styles now import as no fill; gate
  `an imported box leaves what lies under it visible`.
- Layers: `DocumentDesign/Layers/Layer` with `ChildReferences` (item ids),
  `ZOrder`, `IsVisibleInDesignTime`, `IsItemOnBackSide` (two-sided print,
  not built) and a visibility condition. 10 sample labels have layers; the
  `Label_layers` sample prints a Code 128 variant or a QR variant at the same
  place depending on a variable.
- Visibility condition (vendor `DocumentItem`, items and layers):
  `IsVisibilityConditional`, `VisibilityConditionLeftValue` /
  `VisibilityConditionRightValue` (a data source or fixed text) and
  `VisibilityConditionOperator` (`ComparisonOperator` Equals 0, Unequals 1).
  `DocumentItemRuntime` compares the two values' text with
  `OrdinalIgnoreCase` and shows the item only when the result matches the
  operator.
- ANLAbel: the condition becomes the object's Điều kiện hiển thị, written
  with the new formula functions `EQUALS(a, b)` (same text ignoring case,
  "true"/"false"), `NOT(x)` and `AND(…)` (true = not empty, 0, false, no,
  không, the visibility rule, `FormulaTruth`); an item and its layer both
  conditional give `AND`. A prompt used only by a condition is asked at print
  (`JobVariableContract.FindTokens` reads conditions too). Sample sweep: the
  `Label_layers` QR decodes and its hidden Code 128 variant is not printed.

## §14 Design-time samples of database fields (2026-10-30)

A text bound to a data source stores a placeholder in `FixedContents` (the
contents it had before binding: "Text", "Text Box", "Po:"); NiceLabel shows
the data source's design value instead: a variable's `DefaultValue`, a
database field's `SampleValue` (`DBField`/`CsvDBField`, also cached per data
set in `FieldsSampleValueCache`). In the 12 local customer labels 35 of the 48
bound texts have a field sample. The importer sized position texts from the
placeholder, so ANLAbel Text (which keeps its frame at print and compresses
longer values into it, by the protected Text contract) squeezed every value
into the width of "Text". It now measures the field sample, and composes
fixed text with references ("Po: [id]") from each source's design value.
Bound barcodes keep their stored-contents sizing (§9).

## §15 Prompted dates (2026-10-31)

A variable whose `DefaultValue` is `DateTimeContents` is a Date variable the
operator enters at print: `InputFormat` (how it is typed, e.g. `d. MM. yyyy`),
`OutputFormat` (how it prints, e.g. `yyyyMMdd`), `DateTimeValue` (an OLE date;
0 or 2958465.99… = 9999-12-31 for none), `UserValue` (`.` for none, the date as
typed, or `[id]` when linked to another source), `DisplayType` 1, `UsageType` 0
and a zero `Offset` in all 190 sample variables (26 input/output pairs; input
and output differ in most). ANLAbel imported them as text prompts, so the
operator had to type the printed form and the designer showed the item's
placeholder contents. Now `PromptVariableDefinition.DateFormat` (output) and
`DateInputFormat` (input): the print dialog offers a calendar (vi-VN order) and
accepts the input format, dd/MM/yyyy or yyyy-MM-dd; the job keeps yyyy-MM-dd and
`FormatPromptValue` prints the output format, then the prefix, suffix and
padding; the designer shows the job day in the output format until a date is
given. Import keeps both formats and the default.

`DateAddFunction` (15 sample functions): `InputDataSourceReference` is the date
moved (a current-date variable in all 15), `Day`/`Month`/`Year` are fixed
amounts or a `DataSourceReference` (11: a prompt "Expiry (days)" defaulting to
100, or a database field). Fixed amounts stay `{#NOW+n…:format}` tokens; amounts
from a source now import as `DATEFORMAT(DATEADD(FIELD("#NOW…:yyyy-MM-dd"),
FIELD("?Expiry (days)"), "d", "yyyy-MM-dd"), "<format>")` (formula functions
DATEADD/DATEFORMAT, carried through templates by a raw formula marker). Not
built: a `DateAddFunction` whose input is a prompted date.

## §16 Observations not mapped (2026-10-31)

- 2D code page: three local customer QR codes store `CodePage` 932
  (Shift-JIS) and concatenate fields that hold Vietnamese text ("HĐMB").
  Shift-JIS has no "Đ", so NiceLabel most likely encodes it as "?"; ANLAbel
  encodes UTF-8 (ZXing `CHARACTER_SET`, with ECI), which keeps the text. Not
  mapped, deliberately.
- Media orientation: three local customer labels (100 x 77 mm) store
  `Media/Orientation` 2 (landscape, DEVMODE `DMORIENT_LANDSCAPE`). The importer
  ignores it and the print service never sets `PageOrientation` for thermal
  drivers; whether these labels feed the right way round needs a test on the
  owner's printer before any mapping.
- Not built (vendor samples only): HIBC version 2.6 and quantity fields (the
  rest of HIBC is mapped, §21), the GS1 DataBar function, EAN/UPC add-on
  symbols, pictures read from a file named by a data source (13 graphics),
  cutter and batch printing (driver settings), label sets.

## §17 EAN/UPC number line and descender bars (2026-10-31)

- Barcodes.Core: every EAN/UPC data class carries `CheckDigitOptions =
  Supported | UserGenerated`, neither `Optional` nor
  `OptionalHumanInterpretation`, so `HasCheckDigit`, `AutomaticCheckDigit` and
  `DisplayCheckDigit` are forced on: the number line always shows the check
  digit. Nine sample EAN-13 items store `DisplayCheckDigit` False; it has no
  effect for EAN.
- A new EAN/UPC object starts with the number below, `AutoFontScaling`,
  `AutomaticCheckDigit`, `HasDescenderBars` and `HasQuietZones` on (EAN base
  class initializer). All 21 sample EAN-13 items store `HasDescenderBars` True.
- UPC-E data is six digits; number system 1 is a separate flag
  (`NumberSystemOneEncoding`), otherwise number system 0.
- Drawing (Neodynamic `BarcodeBuilder`): `GuardBar = HasDescenderBars` with a
  computed `GuardBarHeight`, else flat guard bars (`GuardBarHeight =
  BarHeight`); `DisplayLightMarginIndicator = HasEanWhiteSpaceEnabled`;
  `AddChecksum = AutomaticCheckDigit`.
- ANLAbel: `RetailBarcodeNumber.Complete` gives the number the symbol
  carries (check digit added, six-digit UPC-E as number system 0); the encoder
  input and the number line both follow it, so the printed digits are the
  scanned digits (gate `retail codes print the number the scanner reads`).
  ANLAbel still draws EAN/UPC bars at one height with the number centred below
  (no descender bars, no first digit left of the symbol, no white-space marks,
  no add-ons).

## §18 Placeholders, spaces and reference defaults (2026-10-31)

- A bound item whose data source has no value at design time stores "?"
  repeated to the source's length as `FixedContents` (over 1,100 sample items,
  lengths 1-372; a Concatenate of two keeps its separator: "??????? ???"). A
  255-character Excel field put a Text frame 0.5 m wide on a 50 mm label and
  ANLAbel's preflight refused to print it ("outside the design label"). The
  importer now narrows such a frame from its anchoring point until it is
  inside the label; fixed texts are never narrowed.
- `XDocument.Parse` drops whitespace-only text, so `<Separator> </Separator>`
  (32 sample functions) and a fixed `<StringValue> </StringValue>` part (3)
  were read as empty and "8 PC" printed "8PC". Whitespace-only leaf values are
  marked `xml:space="preserve"` before parsing.
- A variable fed by an `ExtendedDataValueFunction` stores the reference
  template ("[id] / [id] KG") as its default; the design sample composes each
  reference's own sample ("647 / 491 KG") instead of measuring the ids.
- With the application's text measurer, objects whose turned bounds leave
  the label fell from 49 to 42 across the 207 samples. Most of the rest are
  fixed texts 0.01-1 mm past an edge (WPF advance widths a little wider than
  NiceLabel's GDI ones) and codes the samples place partly outside; preflight
  still blocks them (protected rule, reported to the owner).

## §19 Rich text and GS1 auto check digits (2026-10-31)

- Rich text items (`RichTextFormatting` True, `TextType` 2) store base64 RTF
  as `FixedContents` and the same content as a WPF `Section` in
  `XamlContents` (14 of 26 sample items); the RTF and XAML agree (for example
  Arial Narrow `s8` = 5.33 DIP = 4 pt). Seven of the ten distinct items
  are fixed text; three reference data. Fixed rich text now becomes an ANLAbel
  Rich Text Box with the same RTF and frame; rich text that shows data stays a
  TextBox with its plain text, because a Rich Text Box holds fixed text only.
- `GS1128DataValue.AutoCheckDigitCalculation` (AdvancedFunctions): the input
  length allowed is the AI's maximum minus one, and `GetValue` takes
  `Substring(0, MaximumLength - 1)` before computing the check digit, so a
  full-length value has its check digit computed again. ANLAbel imports it as
  `GS1CHECKDIGIT(LEFT(value, n - 1))` for the AIs whose length is fixed (00,
  8017, 8018: 18; 01, 02: 14; 402: 17; 410-417: 13). Decoded sample codes:
  493 -> 501 of 525.

## §20 Counters (2026-10-31)

- Core.Model `Variable`: `IncrementType` (Interfaces enum None 0, Increment 1,
  Decrement 2), `Step` and `Count` (labels per value) default 1,
  `HasMinimumValue`/`MinimumValue`, `HasMaximumValue`/`MaximumValue`,
  `HasRolloverOnMinimumMaximumValue`, reset rules
  `HasRolloverOnVariableChange` (+ `VariableChangeDataSourceReference`) and
  `HasRolloverOnDateChange` (+ `DateRolloverChangeType`,
  `WeekRolloverChangeType`, `DayRolloverTime`), `DeviceInternalCounterType`
  (printer counter if possible). Samples: 8 counters (ISBT128, Serialization,
  Serialization_Label, report labels), two prompted for their start value.
- The reader ignored them, so a counter imported as a prompt and every label
  of a job printed the same number. The importer now makes the first numeric
  counter the template's serial counter (`{#SERIAL}`): default = start value,
  step negated for Decrement, `Count` = change every, limits and rollover,
  prefix and suffix, digits from a fixed length, zero padding or leading
  zeros, maximum from the field length. Not kept: the start-value prompt
  (ANLAbel continues the reserved sequence), reset rules, a second counter
  (stays a prompt). Gate `an imported NiceLabel serial label numbers every
  label`.
- `PromptOrder` (default -1; set on every sample variable) orders the print
  dialog. Imported prompts follow it; prompts without one come after, in the
  order the label first uses them.
- `DataSourceConstraints.MinLength` (default 0; 112 sample variables): when
  above 0 the data source gets a minimum-length validator, and a `Length`
  below it is refused. ANLAbel prompts carry it as `MinLength`, checked on a
  non-empty answer unless padding fills the value to its maximum length.

## §21 HIBC, zero placeholders and prompts without a default (2026-10-31)

- `HibcFunction` (AdvancedFunctions `aq`, HIBC Supplier Labeling Standard
  2.5): `Mode` 0 Primary, 1 Secondary (`PrimaryFunctionReference`), 2
  Concatenated (default when absent); `SecondaryPartFormat` 1 "+" (Julian
  date, YYJJJ only), 2 "+$" lot, 3 "+$$" date and lot, 4 "+$+" serial, 5
  "+$$+" date and serial; `DateFormat` (`HibcDateFormat`: Unknown 0, MMYY 1,
  MMDDYY 2 ... YYJJJHH 6) is written as its number before the date, nothing
  for MMYY and "7" without a date. Primary = "+" LIC, product number, unit of
  measure; a secondary ends with the primary's check character as link; the
  concatenated form joins both with "/" under one check character. The check
  character is the modulo-43 sum of the Code 39 values. ANLAbel adds
  `HIBCCHECK(data)` (data + check character) and imports the function as a
  formula. Not mapped: quantity fields, `ProductionDate`, version 2.6, the
  pre-version-10 two-output form (`IsConcatenated` False without `Mode`).
  The three UDI-HIBC samples print "+A999F29440Q"; the check character was
  verified independently.
- A bound barcode stores "0" repeated to its source's length as
  `FixedContents`, the barcode form of the "?" placeholder (§18; 37 Code 128
  and 70 Code 39 sample items, up to 402 zeros). The importer kept frames
  sized from contents that encode, so the UDI-HIBC code (32 zeros, centre
  anchor at 75.2 mm) got a 54.6 mm frame past the 100 mm label edge and
  preflight refused to print a 44 mm code. All-zero contents of a bound code
  are now a placeholder: the frame follows the design-time value. Sample
  sweep: 504 of 527 codes decode (was 501), objects outside the label 41
  (was 43).
- Prompts without a default: the importer gave such a prompt the bound
  item's stored contents as default — the placeholders above or what the item
  held before it was bound ("123456789012", "Text"). In the samples that put
  "??????????" (215 prompts), "123456789012" (79), zeros (10) and others (6)
  in the print dialog, printed as data when the operator pressed Enter.
  NiceLabel's print form starts such a variable empty; ANLAbel now does too
  (none of the 12 local customer labels had one).
- `ExecuteScriptFunction` scripts that carry comment lines are stored with
  `Base64Encoded="true"` (UTF-8). Of 106 scripts, 34 plain and 5 encoded
  ones are a single `Result = ...` statement once `Rem`/`'` lines are
  skipped; the encoded ones were not decoded. MAT label's 6D date of
  manufacture (date + "094", ANSI X12.3 qualifier) now converts, and with it
  its Data Matrix. Still unconverted and out of scope: PZN/PPN (German
  pharmacy numbers, multi-line mod-11/mod-97 scripts), the Voice Pick code
  (produce traceability, a label-internal function), ISBT 128 (script
  helper `ISBTNumericPair`).

## §22 Every stored property the importer does not read (2026-10-31)

Scan of all object items in the label XML and all variables and functions in
the .slnx of the 207 sample and 12 local customer labels, against the names
the reader uses. Counts are items with a non-default value (customer /
samples).

- Objects: `MergeName` (every item; the name in printer data, ANLAbel uses
  `Name`), `PrintAsGraphics`, `UsageType`, `EditorHeights` (designer only),
  `TextType` (Unknown 0, Text 1, TextBox 2, TextOnPath 3; the importer takes
  Text/TextBox from the geometry, no TextOnPath item exists), the four
  `...Cutout` values of a paragraph (6 / 43, stored as 10 mm but used only by
  a custom `Left/RightBoundaryShape`, which no label sets), `XamlContents`
  (14 rich texts that also store their RTF, read from there),
  `IsItemOnBackSide` (0 / 5, two-sided card printing), `CropVariableGraphics`
  and `DesiredWidth/Height` (designer sizing), report items (out of scope).
  None changes what an imported label prints.
- Variables: `MergeName`, `DeviceInternalCounterType`, `DayRolloverTime`
  (counter reset time), `MultilineLineLength`, `MultilineNumberOfLines`,
  `MultilineEnabled`, `MultilineWordWrap` (no variable in any label has
  `IsMultiline` True), `Description` (0 / 358, a designer note; NiceLabel's
  print form asks with `PromptText`).
- Functions: `ConcatenateFunction.IgnoreEmptyValues` (4 / 111) skips an empty
  value and its separator. All four customer uses have no separator (their
  ";" and "@" are values of their own), so skipping changes nothing; 14
  sample functions (VDA 4994 "PackageId_Human_Readable", space separator)
  would print two spaces where NiceLabel prints one when a value is empty.
  Not built: no customer need, and the formula language has no join that
  skips empty values. `ExecuteScriptFunction.IsExpression` (28 distinct
  scripts) are all one `Result = ...` statement, already read;
  `ScriptType` 2 (16, not VBScript) is not read. `SubsetFunction`
  `Delimiter` '"' and `FieldNumber` are stored defaults; a `Delimited`
  subset is refused and its objects are named as not converted.

## §23 Designer property pages against ANLAbel (2026-10-31)

DesignerLib registers 62 property pages (`[PropertyPage(CategoryName =
"PropertyPagesMessages....")]`). Without PowerForms (`Form*`), RFID and Report
pages, the label-design pages and their state in ANLAbel:

- Read and printed: barcode main, details (`InterCharacterGap`), check digit
  (generate, verify, display), human readable (position, font, bold, italic,
  auto scaling), content mask, 2D details, graphic style and resize options
  (none, uniform, fill, flips), text and rich text style, text automatic sizing
  (`BestFit` with `BestFitMinimum/MaximumFontSize` and `...FontScaling`,
  `ScaleFactor`), inverse text, format dimensions (labels across/down, gaps,
  margins, processing order), paper, printer, the object `IsPrintable` flag.
- Owner decisions, listed in §16: descender bars and quiet zones
  (`HasDescenderBars`, `HasQuietZones`).
- Stored but not used by any label here: text effects `Mirror`,
  `RtlPrinting` and text on a path (`TextType` 3), bearer bars
  (`FixedThickness`, `VariableThickness`, `VerticalBars`), bar width
  reduction and space correction, variable label length (`IsReceiptPrinting`,
  `VariableFormatSize`), paragraph custom boundaries (§22).
- Relative position (`RelativePositionPropertyPage`): stored as
  `Geometry/Positioning Type="ItemPositioning"` with
  `HorizontalPositioningKind` 2, `HorizontalBorderKind` and
  `HorizontalRelativeItem` (the reference item's id); 24 sample items (ECIA
  product, GM1724A, GS1-128, HIBC labels), horizontal only, none in the
  customer labels. The stored X/Y is the design-time result, so the import
  places the item right for the sample values; NiceLabel moves it with the
  reference item's edge when the data is longer or shorter, ANLAbel keeps it
  where it was. Not built.
- Label corner radius (`HorizontalRadius`, `VerticalRadius`, 50 samples):
  the die-cut shape shown in the designer; not printed. Not built.
- Batch printing, cutter, stocks, custom scripts: §16 and out of scope.

## §24 NiceLabel's validation messages against ANLAbel's checks (2026-10-31)

`EuroPlus.NiceLabel.Core.Resources.ValidationErrorCode` and the format and
designer resources list what NiceLabel 10 checks in a label. Most entries
name features a product level lacks ("Text box", "Rich text formatting",
"Printer counter is not supported"); the ones about the label and its data,
with ANLAbel's counterpart:

| NiceLabel check | ANLAbel |
| --- | --- |
| `ElementOutOfFormat` "Object out of label." | Preflight "Object extends outside the design label bounds" |
| `TextDoesNotFit` "The text cannot fit into its placeholder." | Text Box overflow and fixed Text frame checks |
| `MissingDBField`, `DBFieldDeleted` | Missing field blocks the row; the import note names the column (DB03) |
| `DefaultValueChainReference` "Chain reference detected." | Data Manager functions: dependency cycle error |
| `MaximumValueSmallerThanMinimum`, `InvalidMinimumValue` | Prompt and counter definitions refuse a minimum above the maximum |
| `VisibilityConditionDataSourceN` "No data source for visibility condition selected." | A condition reading a column the data lacks hid its object on every label without a word; since v0.354 the print stops and names the missing column |
| `InvalidImage`, `FileDoesNotExist`, `CannotReadFile` | Missing resources chip and the image preflight |
| `NoApplicationIdentifiers`, `DataBarGS1128Error` | GS1 `(AI)value` validation against the GS1 registry |
| `DimensionsDoNotMatchStockWarning` | Stocks are out of scope |

NiceLabel also offers "Limit field length (truncate excessive content)" on a
database field. ANLAbel does not cut data: a value too long for its frame is
reported (Text Box overflow, barcode capacity) instead of printed shortened.

## §25 Numbers from a database field (2026-10-31)

NiceLabel reads Excel through OLE DB (`Databases.Core`: Jet, falling back to
ACE 12), so a numeric column arrives as a double, not as the text Excel shows.
The field's `FormattingContents` (`DoubleContents` in Utils.dll) prints it:

- `DoubleContents.GetFormattedValue(value, OutputFormat, OutputFormatInfo)`
  clones the field's `NumberFormatInfo`; with `AutoDecimalDigits` it sets
  `NumberDecimalDigits = 99`, calls `value.ToString(format, info)`, then
  trims trailing zeros and a trailing decimal separator. REA call edges of
  Utils.dll (SHA-256 `bd421e4b…`, evidence `ev_0405c335…`, page
  `ev_d1b0fd50…`) show that sequence: `Clone`, `set_NumberDecimalDigits`,
  `Double.ToString`, `TrimEnd`, `EndsWith`, `Substring`.
- `NumberFormatInfoWrapper` stores `DecimalSeparator` (default "."),
  `GroupSeparator` (default ","), `DecimalDigits` and `AutoDecimalDigits`.
  Its constructor copies the machine's culture, but the document loader
  replaces an element that is absent from the file by the `LoadSave`
  default, so an absent separator means "." or ",", not the culture's.
- NiceLabel runs on .NET Framework 4.7.2, where `N99` gives at most 15
  significant digits: 0.1 prints "0.1".

So `OutputFormat` "n" with automatic decimals prints 1200 as "1200" when the
group separator is stored empty and "1,200" when it is absent; 1200.5 prints
"1200.5" either way, whatever format the Excel cell has.

The customer labels use 22 numeric fields (quantities, PO, date codes): 18
with an empty group separator, 4 without one, all with automatic decimals.
ANLAbel prints a number as the Excel cell formats it (invariant separators):
General "1200", `#,##0` and comma style "1,200", `0.00` "1200.00". The two
agree only when the cell format happens to match the field; a quantity in a
comma-style column prints "1,200" in ANLAbel and "1200" in NiceLabel, inside
barcodes too. Owner decision (number separators); not changed.

## §26 Editing several selected objects (2026-10-31)

NiceLabel 10's property pages, which also back the ribbon's font group, edit
every selected object at once:

- `TextStylePropertyPageViewModel.FontSize` (DesignerLib) calls
  `SetValue(value, …, TextPropertyXaml.FontSizeUndoDesc, (item, size) => …)`
  with an action that sets the size on an item that has a font and skips
  the others. With nothing selected it sets
  `ApplicationSettings.FontSize`, the size of new text objects.
- `SolutionItemPropertyPageViewModel<TItem>.SetValue` (Gui.Core) passes the
  action to a helper that copies the page's item list and calls the action
  for each item, under the one undo description.
- A page shows only when it suits the whole selection
  (`TextStylePropertyPageViewModel.IsValidPropertyPage` refuses a selection
  with a rich text box or a form table), and choices such as the font sizes
  offered are the intersection over the selected items.
- Barcode pages work the same way (`Barcode2DPropertyPageViewModel`
  overrides `SetValue`). The `*SelectionItemCommandArguments` types (font
  width and height, bar width, module height) record the undo of a resize
  of the selection on the design surface, not property edits.

ANLAbel's Properties panel and ribbon edit the key object, and these go to
the whole selection: font family, size, style, colour and alignment to every
selected text and text box (v0.354); the X dimension and the text under the
code to every selected linear barcode (v0.355); Không in, Hiển thị, Khóa vị
trí and Giữ tỉ lệ to every selected object, shown mixed when the objects
differ (v0.358, §29). X/Y, size, rotation and anchoring stay with the key
object.

## §27 Object names, shared values, locks and record selection (2026-10-31)

Evidence: Core.dll `3659e860…`, DesignerLib.dll `ac59f773…`, Gui.Core.dll
`3f07b396…`, Interfaces.dll `589df2b6…` (SHA-256 prefixes), decompiled with
ilspycmd.

- New items. `AbstractDocumentDesign` (Core) names an item added to the
  design with `ISolutionService.GetSolutionItemUniqueName`: the proposed name,
  trimmed, or the first free `Name_1`, `Name_2` … (`Name1` when the feature
  `DefaultVariableNameCounterSeparatorIsNotUsed` is on).
  `IsSolutionItemNameUnique` compares with `CurrentCultureIgnoreCase`.
- Renaming. `OptionsPropertyPageViewModel.Name` (DesignerLib) carries
  `RequiredUniqueNameValidatorWithDialog` and `RequiredNameValidatorWithDialog`:
  a name another element has, case ignored, or an empty name sets
  `IsSetValueAborted` and shows an error dialog, "Invalid name: Another
  element with the same name already exists. The original name will be
  restored." or "Property name cannot be empty. The original name will be
  restored." (`DesignerMessages` resources).
- A multiple selection's values. `SolutionItemPropertyPageViewModel<TItem>`
  (Gui.Core) reads a page field through `GetCommonValue` / `UpdateCommonValue`:
  the first item's value when every selected item has the same one
  (`object.Equals`), else the field's default, which is null for the nullable
  fields of the barcode text page (`FontSize`, `IsBold`, `IsItalic`). A field
  of a mixed selection is therefore blank.
- Locked items. `DeleteDocumentItemCommand.CanExecute` asks only for a
  selected item, so a position-locked item is deleted like any other; the
  move commands of `DesignSurfaceCoreCommand` leave out items with
  `PositionLocked`.
- Record selection. A label's use of a data set (`IDataSetUsage`, Interfaces)
  holds a filter reference, labels per record, how records are retrieved,
  records to skip, whether the operator selects records and single record.
  The customer labels store only labels per record (1): no filter and no
  skipped records, so NiceLabel reads every row of the sheet, including rows
  that hold only a formula filled down past the records (one customer sheet
  has 1 record and 198 such rows).
- Layers and stacking. An item's `ZOrder` includes its layer's base (10000,
  20000 … in the layered sample pallet and container labels), so sorting by
  item `ZOrder`, as the importer does, keeps each layer above the one below.
- Undo. Each property edit names its undo step with a resource string
  (`TextPropertyXaml.FontSizeUndoDesc` and the like, §26), and the Undo and
  Redo drop-downs (`Gui.Core/undoredodropdown`) list those names.

ANLAbel (v0.355–v0.356): a new object takes a free name ("Chữ 2"), the
ribbon's name box refuses a taken or empty name and keeps the old one, the
ribbon's font boxes are blank for a mixed selection, and undo steps have
names with Undo ▾ and Redo ▾ lists. Delete removes locked objects, as in
NiceLabel.

## §28 Designer keys, arrange, paste and deleting an item in use (2026-10-31)

Evidence: DesignerLib.dll `ac59f773…` and Core.dll `3659e860…` (SHA-256
prefixes), decompiled with ilspycmd; window and page views read from the
DesignerLib BAML.

- Keys. `mainwindow.xaml` binds Ctrl+A (select all), Ctrl+F (object browser
  search), Ctrl+Z / Ctrl+Y and Ctrl+Shift+Z / Ctrl+Shift+Y (undo, redo),
  Ctrl+S, Ctrl+O, Ctrl+P and Shift+Alt+S (print backstage), Ctrl+Shift+C /
  Ctrl+Shift+V (format painter), Ctrl+L and Ctrl+Shift+N (new label),
  Ctrl+Shift+O (open solution), Ctrl+D (default form), Ctrl+R, Ctrl+F9 and
  Ctrl+Shift+R (run form), Ctrl+Alt+T (test print) and Ctrl+F4 (close page);
  `documentpageview.xaml` binds Ctrl+G (group) and Ctrl+Shift+G (ungroup).
  Ribbon items carry their own gestures: Ctrl+X, Ctrl+C, Ctrl+V, Ctrl+B,
  Ctrl+I, Ctrl+U, PgUp (bring forward), Ctrl+PgUp (to front), PgDn (send
  backward) and Ctrl+PgDn (to back), each disabled while the backstage is open
  (`IsEnabled` bound to the inverse of `backstageViewControl.IsOpen`).
  `DocumentPageView` handles more keys itself: Tab and Shift+Tab select the
  next or previous item (`MoveSelectionToNextDocumentItemCommand`), arrows
  move the selection, arrows with a modifier resize it
  (`ResizeSelectedItemsCommand`), a key turns the selected item 90° about its
  centre (`RotateSelectedItemsCommand`), Ctrl with + or − zooms, numpad 0
  restores the designer zoom, and F2 or Enter start editing the selected item.
- Arrange. `ChangeZOrderCommand` takes `GetSelectedDocumentItems` ordered by
  `ZOrder`: every selected item moves, among the items with the same parent.
  To front and to back move them to the end (`MoveToEnd`) in the order they
  had; forward and backward step each item past the next item, taking the
  selection from the far end.
- Paste. `PasteSolutionItemCommand.ResetOffsetAndSetItemToOriginalPosition`
  keeps a pasted item's stored position unless its original, or an earlier
  paste of it (`OriginalId`), is in the document; only then is it offset
  (`GetOffset`, `baseOffset` = 50). Names go through
  `GetSolutionItemUniqueName` (§27). `AddElements` adds the data sources the
  items use to the target solution, and `PasteDocumentItemsUndoData` keeps
  them (`NewDataSources`) so one undo removes them with the items.
- Deleting an item in use. `DeleteSolutionItemCommand` asks
  `ActiveSolution.FindRelatedItems`; an item with related items is kept, with
  "Cannot delete the item because it is used by other items."
  (`ValidationErrorCode.DeleteRelatedSolutionItems`); the designer API raises
  `DesignerApiErrorCode.SolutionItemInUse`.
- Object markers (`MenuViewXaml`, View › Object Markers Visibility): Data
  Source, Counter, Events, Locked Object and Printer Element, each switched
  on its own.

ANLAbel (v0.357): Ctrl+A, Ctrl+X, Ctrl+G, Ctrl+Shift+G, Ctrl+F (which
also shows a hidden left panel), Ctrl+B, Ctrl+I, Ctrl+U, PgUp and PgDn (Ctrl:
to the top or the bottom), Tab and Shift+Tab, and Ctrl with plus, minus or 0;
no key acts while a text box has the keyboard or behind the File backstage,
and none edits a protected label. The arrange commands move the whole
selection in its order. A paste into another label, or after a cut, keeps
place and names and brings the functions and prompt variables the objects
read; a name the target already has keeps the target's definition, and the
status line says when the two differ. The Data Manager asks before deleting a
prompt or function in use, naming the objects and functions that use it, and
keeps it by default: refusing outright, as NiceLabel does, would make the
operator close the draft-based Data Manager to rebind the objects first. A
padlock marks locked objects. Not built: the keyboard resize and rotate keys
(the Text and Text Box resize lifecycle is a protected contract), the form
keys (forms are out of scope), Ctrl+Alt+T and Ctrl+F4 (one label is open at a
time), and the Counter, Events and Printer Element markers (no such objects).

## §29 Ribbon map, test print and property pages on a selection (2026-10-31)

Evidence: DesignerLib `mainwindow.xaml` and `statusbarview.xaml` (BAML),
`DocumentPageView`, `TestPrintCommand`, `PositionPropertyPageViewModel` and
`FormatPositionPropertyPageViewModel` (ilspycmd), Core `a.c.a.ae` (the print
process initializer).

- Ribbon. Home: Clipboard; Font; Action (print, store, test print, customize
  and express print, printing shortcut, run form, export); Management (data
  sources, document properties); Align; Objects (group, order, delete);
  Object Settings (print object, cut object); Color Processing; Partner and
  activation. Data: Database (Excel, Access, SQL Server, text, ODBC, other,
  edit, delete); Data Source Management (define, import, prompt order); RFID.
  View: Zoom; Object Markers; Alignment and Gridlines; Format Rotation.
  Solution, Control Center (check in and out) and Help. Contextual pages:
  Design (visibility, printing optimization, name; position, anchoring
  point, relative position; arrange), Barcode, Text (format with text
  scaling; character and paragraph, effects, automatic sizing, custom
  boundaries), Picture (resize), Shape (outline, fill) and the form pages.
  The status bar holds a document lock item, RFID, Windows mode, printers
  (hidden) and the zoom slider.
- ANLAbel covers the label-design groups. Out of scope or not built: forms,
  RFID, Control Center, color processing, partner and activation, store and
  recall, export, printing optimization (ANLAbel prints through the Windows
  driver), the cut object flag, relative position (§23), and the database
  wizard's Create Objects page (`CreateObjects`, `CreateTableObject`,
  `DoNotCreateObjects`: one label object per database field). ANLAbel's
  fields are dragged from the Data panel one at a time.
- Test print (Ctrl+Alt+T, "Prints label with initial values").
  `TestPrintCommand` prints one label (`PrintQuantity.FixedValue = 1`) from a
  fresh runtime instance of the format; the `TestPrint` flag is read only by
  `RecallPrinting`. ANLAbel's "In thử" prints the calibration label. A test
  print of the design is not built: a serial printed on a test label without
  committing the counter would repeat a production number (owner decision).
- F2 and Enter. `DocumentPageView`: F2 puts an item that supports in-place
  editing into `EditMode`, otherwise opens the property editor
  (`OpenDocumentItemPropertyEditorCommand`, `PopupMode = true`); Enter starts
  in-place editing only. ANLAbel opens Properties on a double-click and
  (v0.358) with F2, a text's content selected to type over. Owner decision
  2026-11-01: in-place editing as NiceLabel's, for a Text or Text Box with
  fixed content (F2, Enter, a double-click, Sửa chữ); other items keep F2
  and the double-click opening their properties.
- Property pages on a selection. `PositionPropertyPageViewModel` sets
  `Width`, `Height` (`SetPositionValue`; `double.NaN` shows a mixed value)
  and `Rotation` on every selected item; `PositionLocked` and
  `KeepAspectRatio` are `bool?`, null when the selected items differ.
  `FormatPositionPropertyPageViewModel` sets `Left`, `Top`, the line end
  point and length, and `AnchoringPoint` the same way.
- ANLAbel (v0.358): Không in, Hiển thị, Khóa vị trí and Giữ tỉ lệ reach
  every selected object and show a mixed tick when the selected objects
  differ. X and Y, width and height, rotation and the anchoring point stay
  with the key object: the key's X and Y also change while the selection is
  dragged, width and height of Text and Text Box are a protected contract, a
  rotation applied member by member would take an ANLAbel group apart, and the
  anchoring point feeds the protected AutoFit Text layout.
