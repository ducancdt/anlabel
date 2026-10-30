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

Owner hand check (2026-10-30): with the Properties panel unpinned, a
double-click on an object opens the floating panel beside it — confirmed
working on the installed app.

Owner decision (2026-10-30, UI audit): with a Rich Text Box selected, the
Home ribbon Font group formats its RTF content, as NiceLabel 10 does. It
applies to the text selected in the Properties editor, or to the whole
content when nothing is selected, and stores the RTF as one undoable edit.
The Text / Text Box font group is unchanged and steps aside for rich text
(gate `ribbon formats the selected rich text`).

Owner decisions (2026-10-30, UI audit, accessibility pass): the keyboard
focus ring is the app's solid blue outline (gate `keyboard focus ring is a
visible blue outline`); the owner checks it on screen by hand. Windows
high-contrast mode is not supported: the app keeps its own colours.

## Side-by-side check (2026-10-30)

Each ANLAbel ribbon tab (rendered by the layout gate into
`TestOutput/nicelabel-layout/`) was placed under the NiceLabel 10 screenshot
of the same tab (kept outside the repository) and compared group by group.

| Tab | NiceLabel 10 groups | ANLAbel | Remaining difference |
| --- | --- | --- | --- |
| Home | Clipboard · Font · Action · Management · Align · Objects | Bộ nhớ tạm (Dán, Cắt, Sao chép, Sao định dạng) · Phông chữ (phông, cỡ, tăng/giảm, B I U, căn 4 kiểu) · Thao tác · Quản lý · Căn chỉnh · Đối tượng (Đưa lên/xuống, Nhóm/Bỏ nhóm, Xóa) | — (strikethrough and colour added 2026-10-30) |
| Data | Database wizard · Data Source Management · RFID | Nhập dữ liệu (Excel/CSV, Cập nhật) · Quản lý nguồn dữ liệu | Access/SQL/ODBC, RFID — out of scope by owner decision |
| View | Zoom · Object Markers · Alignment and Gridlines · Rotation | Thu phóng (Vừa tài liệu, Vừa đối tượng, Phóng to/Thu nhỏ, %) · Hiện dấu đối tượng (Tên, Nguồn dữ liệu) · Căn và lưới (Theo đối tượng, Theo lưới, Không căn) · Bảng · Xoay khung nhìn | Printer Element / Events markers (no such ANLAbel concepts) |
| Help | Help · Product | Trợ giúp (Trợ giúp, Mẫu tem) · Sản phẩm (Cập nhật) | — |
| Design | General · Positioning · Arrange | Chung (Không in, Hiển thị, Khóa vị trí, Tên) · Điểm neo · Sắp xếp | Relative Position (see below) |
| Text | Format · Text Settings · Arrange | Định dạng (phông, cỡ, tăng/giảm, B I U, căn) · Thiết lập chữ (Co giãn chữ ngang, Vừa trong khung) | Character and Paragraph spacing (see below) |

Also added in this pass: Design > Không in (`LabelObject.IsPrintable`, left
out of print, preview and preflight), View > Vừa đối tượng and Không căn,
Grow/Shrink Font and Text-tab alignment.

Added after the side-by-side check (2026-10-30): Strikethrough and a text
colour picker (Home and Text tabs), Text > Hiệu ứng > Soi gương (mirrored
text in designer, preview and print), Design > Giữ tỉ lệ (resize always
proportional) and Design > Điều kiện hiển thị (`VisibilityConditionContract`:
the object prints only on rows where the field/formula is not empty, 0,
false, no or không; preflight skips it on hidden rows).

Still different, by design:

- Relative Position: in NiceLabel it moves an object when a variable-size
  neighbour grows. ANLAbel Text keeps its frame at print time (it
  compresses into it), so there is no growing neighbour to follow.
- Character and Paragraph spacing: WPF's text layout used for print parity
  has no letter-spacing; adding it needs a glyph-level layout of its own.
- Printer Element and Events markers: ANLAbel has no printer-internal
  elements or object events to mark.

Full-window side-by-side (2026-10-30): the NiceLabel 10 designer (sample
label, Properties pinned) next to ANLAbel (layout gate render) — same
regions in the same places: quick access row, tab strip, ribbon; toolbox
above the Data/Layers/Objects explorer on the left; rulers and design
surface in the middle with the document tab below it (added in this pass:
`Shell.DocumentTabs`, "Thiết kế tem · <tên>"); pinned Properties with
Source/Style/Position/General and Source/Mask on the right; status bar
with printer and zoom on the right. Follow-up (same day): the Data explorer now opens with NiceLabel's tree —
"Biến nhập khi in" (prompt variables) and "Trường dữ liệu" (linked fields)
— and the design surface shades the physical sheet around the label with
the other label cells outlined when the sheet is larger than the label.

## Shell rebuild (2026-10-30)

Owner request (2026-10-30, with a screenshot of the v0.341 header): the top
of the window was unreasonable — a Windows title bar over a 52 px navy band
(logo, version badge, boxed quick access buttons, printer and paper chips),
a blue "Tệp" pill, crude tabs, and a Data tab with a few small icons in white
cards. UI had been added without consistency and without functions behind
it. Rebuild it consistently, use the reverse-engineering evidence, and
rebuild the core features 1:1. This section is the record; it supersedes the
header rows of the side-by-side table above.

### Evidence

- Ribbon tree of `mainwindow.xaml` (DevExpress `DXRibbonWindow`, Office 2010
  style, quick access shown above the ribbon, i.e. in the title bar):
  QAT Open · Save · Undo ▾ · Redo ▾; pages and groups as in the table above.
- Screenshots of NiceLabel 10 at 150 % (outside the repository), measured in
  device pixels and converted to DIP: title bar 34 DIP, `#0E1D77`, the tab row
  on the same navy (~22 DIP); File in light cyan `#94D7F0`, other tabs white;
  the selected tab `#F1F1F1` with a `#D6D6D6` border and blue text;
  contextual tabs a lighter blue block; ribbon body `#F1F1F1`, ~89 DIP with
  the group caption (~`#666666`) on the bottom edge and `#D6D6D6` separators;
  32 DIP icons with the caption under them, 16 DIP icons with the caption
  beside them, three 22 DIP rows per column; no card backgrounds.

### What the shell is now

| Region | ANLAbel | Notes |
| --- | --- | --- |
| Title bar | Own caption (`MainWindow.Chrome.cs`, `WindowChrome`): app icon (click = system menu, double-click = close) · quick access Mới · Mở · Lưu · Hoàn tác · Làm lại · centred title "tên tem* — ANLAbel - Thiết kế tem v0.xyz" · thu nhỏ / phóng to–khôi phục / đóng | Maximised window fits the monitor work area (`WM_GETMINMAXINFO`), no resize border while maximised. Windows 11 Snap Layouts flyout is not offered on the custom maximise button (Win+Z and drag-to-edge still work). |
| Removed | the 52 px header band | version → title bar and Giới thiệu; GPL notice → Giới thiệu; printer and label size → status bar |
| Tab row | Tệp (cyan) · Trang chủ · Dữ liệu · Xem · Trợ giúp · contextual Thiết kế + Chữ / Mã vạch / Hình ảnh / Hình dạng | Selected-tab text `#005A9E` and contextual fill `#3D6FC4` are darker than NiceLabel's so the text reaches WCAG AA 4.5:1. Tệp opens a menu until the backstage lands. |
| Ribbon body | `#F1F1F1`, 92 DIP; shared styles in `Themes/Ribbon.xaml` (large, small, icon, split buttons, check boxes, combos, captions, separators) | Every tab fits 1024 DIP (the minimum window width) without shrinking; the per-tab `Viewbox` only shrinks below that. |
| Status bar | messages and warning chips · printer (opens Thuộc tính tem) · label size · vừa tài liệu · vừa đối tượng · − slider + · % | NiceLabel's printer combo changes the printer at once; ANLAbel opens Thuộc tính tem so the printer, media and DPI checks still run (no safety contract bypassed). |

### Tabs and commands

| Tab | Groups → commands (all wired to existing features) |
| --- | --- |
| Trang chủ | Bộ nhớ tạm: Dán · Cắt · Sao chép · Sao định dạng — Phông chữ: phông, cỡ, tăng/giảm, B I U S, căn 4 kiểu, màu (rich text: same group formats the RTF) — Thao tác: In (Ctrl+P, xem trước và in) · Tùy chỉnh in ▾ (in dòng hiện tại, in mọi dòng, in thử, lịch sử in, xuất lịch sử) — Quản lý: Quản lý dữ liệu · Thuộc tính tem — Căn chỉnh: 3 × 3 icon grid in NiceLabel's column order — Đối tượng: Nhóm đối tượng ▾ · Sắp xếp ▾ (4 layer moves) · Xóa |
| Dữ liệu | Cơ sở dữ liệu: Excel (asks for a workbook at once) · Tệp văn bản (CSV) · Sửa cơ sở dữ liệu · Nạp lại dữ liệu · Xóa cơ sở dữ liệu (unlink, confirmed) — Quản lý nguồn dữ liệu: Quản lý dữ liệu · Nguồn dùng chung |
| Xem | Thu phóng: Vừa tài liệu · Vừa đối tượng · Phóng to · Thu nhỏ · % box — Hiện dấu đối tượng: Tên · Nguồn dữ liệu — Căn và lưới: Lưới ▾ (hiện lưới, bước 0,5–10 mm; saved in designer preferences) · Căn theo đối tượng · Căn theo lưới · Không căn — Xoay khung nhìn — Bảng: Hiện lại bảng |
| Trợ giúp | Trợ giúp: Trợ giúp · Tài liệu hướng dẫn (installed guide) · Tệp mẫu · Hỗ trợ kỹ thuật (project issues page) — Sản phẩm: Giới thiệu · Trang chủ · Kiểm tra cập nhật |
| Thiết kế | Chung: Không in · Hiển thị · Điều kiện hiển thị ▾ · Tên ▾ — Vị trí: Điểm neo ▾ · Giữ tỉ lệ · Khóa vị trí — Sắp xếp: Đưa lên ▾ · Đưa xuống ▾ (split buttons) · Nhóm đối tượng · Bỏ nhóm · Căn chỉnh ▾ |
| Chữ | Định dạng (font row) · Thiết lập chữ (Co giãn chữ ngang for Text, Vừa trong khung for Text Box — the protected Text/TextBox controls, bindings unchanged) · Hiệu ứng: Soi gương |
| Mã vạch / Hình ảnh / Hình dạng | data and human readable · Thay ảnh, Giữ tỉ lệ · độ dày nét |

Deliberate differences: Căn theo đối tượng and Căn theo lưới stay two
independent toggles (ANLAbel can snap to both; NiceLabel's three radio
options would remove that); Bảng › Hiện lại bảng recovers hidden panels.

### Icons

ANLAbel draws its own vector icons (`Themes/RibbonIcons.xaml`): 32 × 32 DIP
for large buttons, 16 × 16 DIP for small ones, one flat palette (ink
`#3D4B5F`, blue `#2D6CC0`, green `#21804A` for spreadsheets and databases,
purple `#7451AE` for variables and data sources, red `#C93A3A` for delete),
1 DIP outlines on half-pixel coordinates. No vendor artwork is copied.
Rules (gates `designer header commands are unique` and the
`DesignerHeaderChromeContractTests`): the ribbon uses no PNG rasters, and one
icon never stands for two different actions. The quick access row and the
window buttons use Segoe Fluent Icons glyphs in white, as NiceLabel does.

### Still to rebuild

- Tệp: the backstage (Start page with recent files, New, Open, Import,
  Save, Save As, Print, Close, Options, About, Exit).
- Quản lý dữ liệu: NiceLabel's Dynamic Data Manager window (variables,
  counters, functions, databases in one tree with a property editor), with
  Thứ tự nhắc (prompt order) and Nhập nguồn dữ liệu.
- Contextual drop-downs: bearer bar, barcode details, colours, outline and
  fill styles.
