# NiceLabel 10 Designer UI layout → ANLAbel

Owner request 2026-10-29: research the NiceLabel 10 Designer interface and
convert ANLAbel's layout, UI and UX to it 1:1. This record is the evidence
and the mapping. ANLAbel keeps its own icons, colours, Vietnamese labels and
safety gates; only the arrangement and interaction model follow NiceLabel.

## Evidence

- Static: the BAML resources of `DesignerLib.dll`, `Gui.Core.dll` and
  `WpfControls.dll` (NiceLabel 10, `bin.net`) decompiled to XAML with
  `ICSharpCode.BamlDecompiler` 9.1 (223 + 159 + 16 views, no failures).
  Key views: `mainwindow/view/mainwindow.xaml` (DevExpress `DXRibbonWindow`,
  ribbon, quick access toolbar, backstage), `mainregion/view/mainview.xaml`
  (workspace grid), `statusbarregion/view/statusbarview.xaml`,
  `propertypages/view/documentitems/propertypagespanelview.xaml`,
  `designsurface/toolbox/view/toolbox.xaml`. Captions resolved from
  `MenuViewXaml.resx`.
- Runtime: NiceLabel Designer 10 opened on this machine with the shipped
  sample `Samples/Labels/Automotive/AIAG/AIAG B10 6x4 Mixed Load.nlbl`,
  screenshots of the landing page, the designer, every ribbon tab and the
  floating and pinned Properties panel (kept outside the repository).

## Window

```
┌ Title bar: QAT (Open · Save · Undo · Redo) ─ document title ─ window buttons ┐
├ Tabs: File │ Home │ Data │ View │ Help │ [Design │ Text/Barcode/Picture/Shape]┤  contextual tabs only with a selection
├ Ribbon groups of the selected tab (large buttons + small stacked buttons)    ┤
├──────────────┬─────────────────────────────────────────────┬────────────────┤
│ Toolbox      │ rulers                                      │ Properties     │
│ 3-column     │ design surface                              │ (pinned dock,  │
│ tiles        │                                             │  or floating   │
├──────────────┤                                             │  next to the   │
│ Data│Layers│ │                                             │  selection)    │
│ Objects      │                                             │                │
│ search + tree│ [Label Design] document tab (bottom)        │                │
├──────────────┴─────────────────────────────────────────────┴────────────────┤
│ Status: … printer icon + printer combo · zoom to document · zoom to objects · slider · % │
└──────────────────────────────────────────────────────────────────────────────┘
```

`mainview.xaml`: columns `Auto(min 200) | 5 splitter | * (min 300) | 0 | Auto`;
left column rows: toolbox (min 100) / splitter / tab strip + explorer
(min 100); the document `TabControl` uses `TabStripPlacement="Bottom"`.

## Ribbon (from `mainwindow.xaml`)

| Tab | Groups → commands |
| --- | --- |
| File (backstage) | New, Open, Save, Save As, Print, Info, Close… |
| Home | Clipboard (Paste · Cut · Copy · Format Painter) · Font (family, size, grow/shrink, B I U S, align L/C/R/J, colour) · Action (Print · Customize Print · Run Form) · Management (Dynamic Data Manager · Document Properties) · Align (left/centre/right, top/middle/bottom, distribute H/V) · Objects (Group · Arrange · Delete) |
| Data | Step-by-Step Database Wizard (Excel · Access · SQL Server · Text File · All Databases · Edit Database) · Data Source Management (Dynamic Data Manager · Import Data Sources · Prompt Order) · RFID |
| View | Zoom (Zoom to Document · Zoom to Objects · Zoom In · Zoom Out · %) · Object Markers Visibility (Object Name · Printer Element · Events · Data Source) · Alignment and Gridlines (Gridlines · Align to Objects · Align to Gridlines · Do Not Align) · Rotation (Rotate View) |
| Help | Help (Help · User Guides · Training Videos · Knowledge Base · Sample Files · Technical Support) · Product (About · Home Page) |
| Design (contextual, any object) | General (Not Printable · Visible · Visibility Settings · Name) · Positioning (Position · Anchoring Point · Relative Position · Keep Aspect Ratio · Lock) · Arrange (Bring Forward · Send Backward · Group · Ungroup · Align) |
| Text (contextual) | Format (font row) · Text Settings (Scaling · Character and Paragraph · Effects) · Arrange |
| Barcode (contextual) | Barcode (type · data) · Settings (Bearer Bar · Barcode Details · GS1 DataBar · Color) · Arrange |
| Picture / Shape (contextual) | Resize / Outline · Fill · Arrange |

## Panels

- Toolbox (`ToolboxContainer`, `WrapPanel` tiles, label format): Text, Text
  Box, Rich Text Box, Barcode, Picture, Rectangle, Line, Ellipse, Inverse,
  Report.
- Left explorer tabs (toggle buttons with a top marker): Solution Explorer,
  Dynamic Data Explorer ("Data"), Layers, Objects; Data shows a search box
  and the variable tree with "Add new variable".
- Properties (`PropertyPagesPanelView`): accent header with icon,
  "Properties - <name> [<type>]", pin and close; category tabs (Text:
  Source · Style · Position · General); sub-category tabs (Source · Mask);
  scrolling editor. Double-click opens it floating beside the object; pin
  docks it right.
- Status bar (`RibbonStatusBarControl`): right side printer selector, zoom
  to document, zoom to objects, zoom slider and percentage.

## ANLAbel mapping

| NiceLabel | ANLAbel |
| --- | --- |
| File backstage | `Tệp` menu button: Mới, Mở, Lưu, Lưu thành, Phiên bản, Cài đặt |
| Home / Data / View / Help | `Trang chủ` / `Dữ liệu` / `Xem` / `Trợ giúp` tabs with the same groups where ANLAbel has the feature |
| Design + object tab | `Thiết kế` + `Chữ` / `Mã vạch` / `Hình ảnh` / `Hình dạng`, visible only with a selection |
| Print / Customize Print | `In` (dòng này) / `Xem trước` and the print group (all rows, history, test print, export) |
| Dynamic Data Manager / Document Properties | `Nguồn dữ liệu` / `Cài đặt tem & máy in` |
| Sample Files | `Mẫu tem` (template library) in Help |
| Toolbox tiles | three-column tiles: Chữ, Ô chữ, Mã vạch, Hình ảnh, Hình chữ nhật, Đường kẻ, Hình bầu dục |
| Data · Layers · Objects | `Dữ liệu` · `Lớp` · `Đối tượng` tabs under the toolbox |
| Properties header, pin/close, category tabs | `Thuộc tính - <tên> [<loại>]`, ẩn/hiện, category tabs |
| Status printer + zoom | printer button + zoom to document + slider + % |

Not converted (no ANLAbel feature, out of core scope): Rich Text Box,
Inverse, Report, forms/solutions, Access/SQL/ODBC wizards, RFID, Format
Painter, object markers, view rotation, licensing/partner groups.

## Delivered (2026-10-29)

- Ribbon: `Tệp` menu, `Trang chủ` (Bộ nhớ tạm, Phông chữ, Thao tác, Quản
  lý, Căn chỉnh, Đối tượng), `Dữ liệu`, `Xem` (Thu phóng, Căn và lưới,
  Bảng), `Trợ giúp`, contextual `Thiết kế` and `Chữ`/`Mã vạch`/`Hình
  ảnh`/`Hình dạng` (`MainViewModel.SelectedRibbonTab`, `RibbonTab`).
  Every tab panel stays in the visual tree (collapsed, not a `TabControl`)
  so UI automation reaches every command.
- Left column: three-column toolbox in NiceLabel order; explorer tabs
  `Dữ liệu` · `Lớp` · `Đối tượng` (searchable object browser).
- Properties: header `Thuộc tính - <tên> [<loại>]`; categories `Nguồn` ·
  `Kiểu` · `Vị trí` · `Chung`.
- Status bar: printer button, zoom to document, slider, percentage.
- Gate: `designer layout follows the NiceLabel 10 ribbon and panels`.

Second pass (2026-10-29):

- Properties pin (header pin button, F4): pinned docks right; unpinned the
  same panel floats beside the selection and opens on double-click.
- Anchoring Point (`LabelObject.AnchorPoint`, vendor order 0-8, 3x3 picker
  on the Design tab): variable-size Text and variable-height TextBox grow
  away from the anchor in designer and print; imported from `.nlbl`.
- Group / Ungroup (`LabelObject.GroupId`, Home > Objects and Design >
  Arrange): a click on a member selects the group, which moves together;
  pasted groups get new ids.
- View tab: Object Markers (object name tag, data-source marker) and Rotate
  View (quarter turns of the design surface only).
- Gates: `properties panel pins right or floats beside the selection`,
  `group objects select and move together like NiceLabel`, and the layout
  gate covers the new commands.

Third pass (2026-10-29):

- Source > Mask sub-page: `LabelObject.ContentMask`/`ContentMaskCharacter`
  with the vendor rule (`ContentMaskContract`: each mask character takes
  the next value character, others are literal, each line restarts, extra
  value characters are kept). Applied in designer, preflight and print;
  imported from `HasMaskedContents`/`ContentsMask`/`MaskValue` (87 masked
  items in the corpus).
- Format Painter (Home > Clipboard, Ctrl+Shift+C / Ctrl+Shift+V):
  `LabelObjectCloner.CopyStyle`; across object types the target keeps its
  fit, overflow and padding settings.
- The floating Properties panel was rendered and checked by eye from the
  regression gate (`TestOutput/properties-floating.png`); this machine's
  session did not accept synthetic mouse input for a manual check.

Fourth pass (2026-10-30):

- Inverse object (`ObjectType.Inverse`, toolbox "Đảo màu"): print and
  preview rasterise the content beneath its frame at the print resolution
  and swap black and white; the designer shows the same inverted content
  (rendered by the print renderer after every change), as NiceLabel does. Gate: `inverse object swaps black and white
  beneath it in print`.
- Rich Text Box (`ObjectType.RichTextBox`, toolbox "Chữ RTF", RTF in
  `LabelObject.RichText`): one `RichTextLayout` for print, preview and the
  designer, wrapped at the frame width and clipped at the frame; Properties
  editor with bold/italic/underline/size/alignment. Gate: `rich text box
  prints formatted text clipped at its frame`.

Not converted: the Report object (2 of 207 corpus labels; it prints many
data records on one label, which changes ANLAbel's one-row-per-label print
job and needs its own decision), and NiceLabel products/integrations outside
the label designer — PowerForms forms and solutions, Access/SQL Server/ODBC
connectors, RFID encoding.

## Owner decision (2026-10-30)

The conversion is complete for ANLAbel's scope. The owner chose not to
build the Report object (only NiceLabel's own samples use it; ANLAbel keeps
one data row per label) and not to add products outside the label designer
(PowerForms forms/solutions, Access/SQL Server/ODBC connectors, RFID).

Verification (2026-10-30): `designer shows the inverse area inverted` and
`double-click opens the floating properties beside the object` (a
double-click event on the canvas with the panel unpinned moves the panel
into the floating popup beside the object; with the window shown the popup
reported open).
