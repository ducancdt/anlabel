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

Remaining differences: the Properties panel stays docked (no floating
popup beside the selection); sub-category tabs (Source · Mask) are not
split; Group/Ungroup, anchoring point, object markers and view rotation
have no ANLAbel feature yet.
