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

Not converted (no ANLAbel feature, out of core scope): Report,
forms/solutions, Access/SQL/ODBC wizards, RFID, licensing/partner groups.
Rich Text Box, Inverse, Format Painter ("Sao định dạng", Ctrl+Shift+C and
Ctrl+Shift+V), object markers and view rotation were built later.

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
| Title bar | Own caption (`MainWindow.Chrome.cs`, `WindowChrome`): app icon (click = system menu, double-click = close) · quick access Mới · Mở · Lưu · Hoàn tác · Làm lại · centred title "tên tem* — ANLAbel - Thiết kế tem v0.xyz" · thu nhỏ / phóng to–khôi phục / đóng | Maximised: WPF pulls the content back on the left, top and right; the rest that still lies under the taskbar becomes a bottom margin (measured live at 150 %: the status bar ends at the taskbar). No resize border while maximised. Windows 11 Snap Layouts flyout is not offered on the custom maximise button (Win+Z and drag-to-edge still work). |
| Removed | the 52 px header band | version → title bar and Giới thiệu; GPL notice → Giới thiệu; printer and label size → status bar |
| Tab row | Tệp (cyan) · Trang chủ · Dữ liệu · Xem · Trợ giúp · contextual Thiết kế + Chữ / Mã vạch / Hình ảnh / Hình dạng | Selected-tab text `#005A9E` and contextual fill `#3D6FC4` are darker than NiceLabel's so the text reaches WCAG AA 4.5:1. Tệp opens the backstage (below). |
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
| Chữ | Định dạng (font row) · Thiết lập chữ (Co giãn chữ ngang for Text, Vừa trong khung for Text Box — the protected Text/TextBox controls, bindings unchanged) · Hiệu ứng: Soi gương · Sắp xếp |
| Mã vạch | Mã vạch: loại mã (grouped list), dữ liệu, hiện chữ dưới mã — Thiết lập: Vạch bao ▾ (kiểu, độ dày) · Chi tiết mã ▾ (vị trí và cỡ chữ dưới mã, số kiểm tra, độ rộng vạch hẹp) · Chi tiết 2D ▾ (kích thước, mức sửa lỗi, vùng yên) — Sắp xếp |
| Hình ảnh | Hình ảnh: Thay ảnh, Cỡ ảnh ▾, Giữ tỉ lệ, Lật ngang, Lật dọc, Về cỡ gốc — Sắp xếp |
| Hình dạng | Đường viền: Kiểu viền ▾ · Màu viền ▾ (bảng màu, mã màu) · độ dày · bo góc — Tô nền: Kiểu tô ▾ · Màu tô ▾ — Sắp xếp |

Toolbox: flat 3 × 3 tiles (`ToolboxTile`) with the same vector family — Chữ,
Ô chữ, Chữ RTF, Mã vạch, Hình ảnh, Chữ nhật, Đường kẻ, Bầu dục, Đảo màu —
and the Data / Layers / Objects tabs use 16 DIP vector icons.

Every object tab repeats NiceLabel's Sắp xếp group (Đưa lên ▾, Đưa xuống ▾, Nhóm
đối tượng, Bỏ nhóm, Căn chỉnh ▾); the drop-downs bind the same properties as
the Properties panel, so both always agree.

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

The same family covers every other window (2026-10-30, after the owner
reported broken icons): the Data panel, the Properties header (one 16 px icon
per object type, the toolbox artwork), Print Preview (search, printed mark),
Update, the template library and the guide (topic list, notes, and its ribbon
and toolbox figures, which now show the real icons). The old PNG rasters
(`Icons/*.png`, blurred at 125–150 %) and emoji glyphs (WPF draws them as
faint outlines or empty boxes, e.g. 🖶) are gone; gate `windows draw vector
icons, not PNG or emoji` keeps them out of `src/ANLAbel.App`. Code builds
icons with `RibbonIconSet.Get("Excel16")`. The artwork is generated:
`python scripts/make_ribbon_icons.py src/ANLAbel.App/Themes/RibbonIcons.xaml`.

### Backstage (Tệp)

Evidence: `Gui.Core/usercontrols/backstage/*.xaml` and
`DesignerLib/mainregion/landingpage/view/*.xaml` (decompiled): backstage items
are plain text (margin 22,11,60,11), pages sit on `#f1f1f1` under a 36 pt
Segoe UI Light title; Open has big Browse buttons, a `#2a7cc2` rule and
"Recent Documents" (24 pt, `#2a579a`); the landing page is a 400 DIP card
(new label, new from samples, open, recent files) beside resource boxes.

ANLAbel (`MainWindow.Backstage.cs`): Tệp opens a page that covers the ribbon
and workspace below the title bar; ← or Esc returns to the designer.

| Item | Kind | Does |
| --- | --- | --- |
| Bắt đầu | page | Tạo tem mới · Tạo từ mẫu có sẵn · Mở…, Tệp gần đây; Tài liệu và trợ giúp (hướng dẫn, trợ giúp, tệp mẫu, hỗ trợ, trang chủ); Máy in (the label's printer, Thuộc tính tem…) |
| Mới | page | Tem trống… · Từ mẫu có sẵn… |
| Mở | page | Duyệt… · Nhập tem NiceLabel… beside Tệp gần đây; Ctrl+O and the quick access Mở open this page, as in NiceLabel |
| Nhập tem NiceLabel | command | opens `.nlbl` / `.lbl` as a new template |
| Lưu · Lưu thành · In · Phiên bản · Tùy chọn · Thoát | commands | close the backstage, then run the existing command (unsaved changes are still asked about) |
| Giới thiệu | page | version, GPL-3.0 and no-warranty notice, updates, project page, third-party licences |

Tệp gần đây: the labels opened or saved last (at most 12, newest first,
each once) in `recent-documents.json` under the local app-data root
(`RecentDocumentsService`); a file that has moved is marked and can be
removed from the list. ANLAbel always has a label open, so NiceLabel's
"no document" landing page is the Bắt đầu page here, not a start-up screen.
Not built: Store, Protection, Close (no ANLAbel equivalent).

### Quản lý dữ liệu (Dynamic Data Manager)

Evidence: `datasources/view/datasourcesdialogview.xaml` — a dialog with a small
ribbon (General, Add, Database, Variables › Prompt Order, Import and Export),
a 250 DIP tree of data sources on the left, the selected item's property
editor on the right, OK / Cancel; `variablepromptorderdialogview.xaml`
(variables with Move Up / Move Down) and `datasourceimportdialogview.xaml`
(source file, Overwrite or Merge).

ANLAbel (`DataManagerWindow`, opened from Trang chủ › Quản lý dữ liệu ▾ — the
▾ opens a section directly — and Dữ liệu › Quản lý dữ liệu):

| Part | ANLAbel |
| --- | --- |
| Ribbon | Thêm: Biến nhập khi in · Hàm · Bộ đếm Serial — Sửa: Xóa — Cơ sở dữ liệu: Excel · Tệp văn bản · Sửa cơ sở dữ liệu — Biến: Thứ tự nhắc — Nhập: Nhập nguồn dữ liệu |
| Tree | Biến nhập khi in (n) · Hàm (n) · Bộ đếm Serial · Cơ sở dữ liệu · Biến nội bộ ({#NOW}, {#SERIAL}) |
| Editors | variable: name, prompt, default, max length, digits only, required, which objects use it; function: name, formula, its result on the current data row, syntax help; counter: every setting, next number, start a new sequence; database: file, fields, edit / reload / unlink; internal variables: how to write them and today's value |
| OK / Hủy | Variables, functions and the counter are edited on a draft; Đồng ý checks names (empty, duplicate, braces, clash with a database field), warns about formulas and counter settings that would block printing, then applies everything as one undo step. Hủy asks before dropping changes. Database commands act at once, as on the Data tab. |

Thứ tự nhắc now decides the print dialog: `JobVariableContract.RequiredPrompts`
asks defined variables in the order of the label's list, then any `{?Tên}`
without a definition in order of first use (before: always first use, so a
prompt order list would have had no effect). Nhập nguồn dữ liệu reads an
ANLAbel or NiceLabel label and merges its variables and functions by name
(`DataDefinitionImport`: keep existing or overwrite); the serial counter is not
imported, so each label keeps its own number sequence. Dữ liệu › Nhập nguồn dữ
liệu and Thứ tự nhắc work on the label directly (one undo step each).

Not built: NiceLabel's global/persistent variables and the database
connectors outside ANLAbel's scope (Access, SQL Server, ODBC).

### Still different

- Text › Character and Paragraph / Scaling drop-downs stay in the Properties
  panel: they set the protected Text / TextBox layout (AGENTS.md), which this
  rebuild does not touch.

## Label Properties: paper (2026-10-30)

Owner request "biên dịch phần cốt lõi từ khổ giấy, chọn giấy": Thuộc tính tem
follows NiceLabel 10 Label Properties for paper size and paper choice
(evidence and mapping: `docs/NICELABEL_10_REVERSE_ENGINEERING.md` §10.1).

| NiceLabel 10 | ANLAbel |
| --- | --- |
| Printer | Máy in (unchanged) |
| Label Dimensions › Label Dimensions | Kích thước tem › Rộng, Cao, Hướng |
| Label Dimensions › Margins | Kích thước tem › Lề: Trái, Trên, Phải, Dưới (right/bottom read-only, computed) |
| Label Dimensions › Labels Across | Kích thước tem › Số tem trên trang: Số tem ngang/dọc, Khoảng cách, Thứ tự in, Tính số tem vừa tờ |
| Paper › Paper Type | Giấy › Loại giấy (tem trên cuộn / tờ giấy) |
| Paper › Paper | Giấy › Giấy (driver papers) and its size |
| Paper › Orientation › Rotated | Giấy › Hướng in › Xoay 180° |
| Paper › Preview | Giấy › Xem trước (page with numbered label cells) |

Calibration scale stays under Giấy › Căn chỉnh khổ in. Gates: `label
properties lay labels on a sheet like NiceLabel`, `one label on a sheet prints
the whole sheet`.

Printing a sheet of labels (NiceLabel 10 print form "Number of skipped labels" and
its page preview): Xem trước khi in shows each label on the whole sheet it prints
on (`PrintService.CreateSheetPreview`, the renderer's own drawing per cell), says
which sheet and cell it lands in, and Tờ nhiều tem › Bỏ qua N ô đầu của tờ đầu
leaves the used cells of a partly used first sheet empty; printing sends the same
number (`PrintRowsWithResultAsync(skipLabels:)`). Gates: `label sheets fill every
cell in processing order` (A4 3 × 8 and 2 × 5, A5 2 × 2 turned 180°, skipped cells,
every cell decoded on the printed page and on the preview), `print preview shows
sheets whole and skips used cells`, `turned objects print readable`.

## Picture: Resize options and flips (2026-10-30)

NiceLabel 10 picture properties (`ResizeGraphicOptions`, `GraphicStyle`) map to
the Hình ảnh tab and the Properties card:

| NiceLabel 10 | ANLAbel |
| --- | --- |
| Keep original picture size (`GraphicResizeMode.None`) | Cỡ ảnh › Giữ cỡ gốc: pixels and the file's resolution (96 dpi when it states none), from the frame's top-left corner, cut at the frame |
| Resize proportionally (`Uniform`) | Cỡ ảnh › Co giãn theo tỉ lệ (default): the whole picture, centred |
| Resize to the designed size (`Fill`) | Cỡ ảnh › Kéo đầy khung |
| Revert to original picture size | Về cỡ gốc: the frame takes the picture's own size (one undo step) |
| Flip picture horizontal / vertical | Lật ngang / Lật dọc |
| Source › picture file, Original size | Thuộc tính › Nguồn › Nguồn ảnh: embedded in the label (prints without the original file), pixels, resolution and original size; Chọn hình ảnh… |

`PictureLayoutContract.Place` is the one placement used by the designer, preview,
print and the image density check (a picture kept at its own size is checked at its
own resolution). The designer gives an original-size picture larger than its frame
a layout slot as large as itself, so WPF does not cut it before the flip; the frame
cuts it, as print does. Import: vendor `Fill` becomes Kéo đầy khung, the mirrors
become the flips; vendor `None` keeps proportional sizing because its stored frame
is already the picture's own size. NiceLabel 10 does not change the frame when the
resize option changes; only Revert does. Gate: `pictures size and flip in the
designer as in print` (six pictures, measured on the printed label and on the
designer canvas against positions worked out from the option definitions).

## New Label Setup Wizard (2026-10-30)

NiceLabel 10 creates a label through `NewFormatWizard` pages: Select the Printer,
Set the Page Size (roll of labels, sheet of paper, pre-defined stock), Select the
Label Layout (orientation, rotated), Specify the Label Dimensions, Summary. ANLAbel:
Tạo mẫu tem mới (name, size, DPI) offers "Tiếp theo: chọn máy in và giấy" (on by
default); Thuộc tính tem then opens guided (`PrinterSetupWindow.IsGuided`): Máy in ›
Kích thước tem › Giấy with Quay lại / Tiếp and Hoàn tất on the last page, the same
pages and checks as Label Properties. No stock step (owner: no software stock
catalogue). Gate: `a new label walks label properties step by step`.

## Protection (2026-10-30)

NiceLabel 10 Backstage › Info › Protection: "Protect document from editing",
optionally "Requires a password to unlock document"; a locked document shows
"Label or solution editing is locked. Unlock to start editing." ANLAbel: Tệp ›
Bảo vệ (`LabelTemplate.ProtectFromEditing`, `EditingPasswordHash` = salted
PBKDF2-SHA256 via `EditingProtection`). A protected label opens locked
(`MainViewModel.IsEditingLocked`, reset whenever a document is opened, created or
recovered): the design surface, toolbox, Layers, Objects, Properties, the Home
groups Bộ nhớ tạm / Phông chữ / Căn chỉnh / Đối tượng, the contextual tabs, undo and
the drawing shortcuts are off; data, printer (Thuộc tính tem) and printing stay
available. The banner on the design surface unlocks for the session (with the
password when one is set). Protection can be changed only while unlocked. It
guards against accidental edits; it is not file encryption. Gate: `a protected
label opens for printing only`.

## Variable input and output rules (2026-10-30)

NiceLabel 10 variable pages (`variables/inputrules…`, `outputrules…`): Pick list,
Allowed characters, Length with Fixed length, Value required; Prefix and Suffix,
Padding (on left, on right, surrounding) with a pad character. ANLAbel prompts
(`PromptVariableDefinition`): PickList (one value per line), FixedLength,
AllowedCharacters, Prefix, Suffix, Padding Left/Right and PadCharacter, edited in
Quản lý dữ liệu › Biến nhập khi in. `JobVariableContract.ValidatePromptValue`
checks the answer; `FormatPromptValue` pads it to the maximum length and adds the
prefix and suffix when it prints (`TryResolveValues`, and the designer row). The
print job keeps the answer as entered, so a reprint applies the same rules once.
The operator dialog offers a pick list as a list and shows "Sẽ in: …". Not built:
surrounding padding, numeric range, time and currency data types (minimum length is
built: "Số ký tự tối thiểu", 2026-10-31; dates: see
the RE record §15), multiline,
counters (ANLAbel's serial counter is separate). Gates: `prompt rules guide the
operator`; unit tests in `JobVariableTests` and the .nlbl mapping.

## Prompt editor layout (2026-10-31, design pass)

The Data Manager prompt editor groups its rules under "Quy tắc nhập" (length as
one row "tối thiểu … tối đa …", fixed length, digits only, allowed characters,
pick list) and "Khi in" (labelled "Tiền tố" and "Hậu tố" boxes, padding; the pad
character is enabled only with padding). Đồng ý refuses a minimum length above
the maximum (`JobVariableContract.ValidateDefinition`), which NiceLabel 10 also
refuses. Gate: `the prompt editor follows the kind of value`.

## Data explorer drag and drop (2026-10-31)

NiceLabel 10 (`DocumentPageView.HostCanvas_Drop`, `DropGenericSelectItemOnSurface`,
`DropGenericSelectorItemOnObject`): a data source dragged from the Dynamic Data
Explorer onto the design surface creates an object bound to it (a Text, or the
armed toolbox tool), onto an object assigns the data source to that object when
it can show data, and with Ctrl creates a new object instead. ANLAbel: the Data
tree's prompts (`{?Name}`) and fields (`{Field}`) drag onto `LabelDesignerCanvas`
(`DataSourceDragFormat`); `DataSourceDropTarget` finds the topmost visible,
unlocked object under the point; `MainViewModel.DropDataSource` binds a Text,
Text Box, barcode, QR or Data Matrix, or adds a toolbox-default Text whose
top-left corner is the drop point, as one undo step; a protected label takes
no drop. Not built: dropping onto an armed toolbox tool, dropping a whole data
set. Gate: `fields and prompts drop on the label`.

Functions and print-time values (2026-10-31, v0.353): NiceLabel's explorer
lists functions and internal values beside variables. The Data tree adds
"Hàm" (the Data Manager functions, dropped as `{Name}`) and "Khi in" (the
serial `{#SERIAL}` while the counter is on, the print date
`{#NOW:dd/MM/yyyy}`), shown by label with the binding as tooltip; a text
dropped from "Khi in" is named "Số sê-ri" or "Ngày in". One mapping
(`MainWindow.DataTreeBinding`) turns a tree item into its binding. Gate:
`the data panel lists functions and print-time fields`.

## Object browser and layers (2026-10-31)

From DesignerLib `mainregion/view/objectbrowser.xaml`,
`objectbrowseritemadditionalcontrols.xaml` and `layerexplorer.xaml`:

- Object browser: a generic selector list of the label's objects (drag
  enabled); the only per-row extra is a lightning marker for objects with
  actions (PowerForms). ANLAbel's "Đối tượng" tab lists name and type with a
  search box and selects on the canvas: on par for labels.
- Layer explorer: layers with show/hide and lock toggles, new, delete,
  rename, properties, move up and down, copy, paste, duplicate, merge down,
  merge visible, hide other layers, show all, lock other layers, unlock all.
  ANLAbel has no layers; an imported label keeps its objects, and objects on
  a hidden layer are not printed (the `Label_layers` sample). No customer
  label uses layers. Not built.

## Database wizard (2026-10-31)

NiceLabel 10's database wizard (`wizards/view/database`, `DatabaseWizard`,
`OpenDatabaseWizardDialogCommand`) runs: welcome, connection type,
connection, connection settings, table and fields, other data set settings
(Data, Fields and Usage tabs), printing options (labels per record),
elements creation. ANLAbel's Excel import covers the steps a workbook or CSV
needs: file, sheet, header row, the
fields used on the label, and labels per record through the copies column.

Elements creation offers "create objects", "create a table" (a Report, out of
scope) or nothing. "Create objects" (`AddFormatObjects`) adds one Text per
field of the data set, bound to it, at x = 5 mm, stacked downwards from
y = 5 mm. Not built: ANLAbel's Data panel drags each field onto the label.

## Print form (2026-10-31)

NiceLabel 10 prints through a form: the default "Printing Form" (stored in
the label, `Forms_Printing Form.xml`) holds the quantity, a live label
preview, the printer list and its setup button, the variable prompts
(`PromptControl`) and the Print button (open label, set printer, redirect to
file, print). Its settings are solution variables, also in three customer
labels: `PrintingForm_Quantity`, `PrintingForm_SkippedLabels`,
`PrintingForm_IdenticalCopies`, `PrintingForm_LabelSets`,
`PrintingForm_PrinterName`, `PrintingForm_PrintToFile`, `PrintingForm_LabelName`.

ANLAbel splits it: the print questions first (`PromptValuesWindow`, Enter moves
to the next question), then Print Preview with the label pages, the Excel rows
and their copies, labels to skip on a part-used sheet, the printer and its
setup. Not built: label sets (the whole job repeated N times) and printing to
a file of printer commands. Out of scope: custom PowerForms.

## Status bar, function editors and smaller dialogs (2026-10-31)

- Status bar (`statusbarregion/view/statusbarview.xaml`, `StatusBarViewModel`):
  a lock marker on the left while the label or solution is protected ("Label
  or solution editing is locked. Unlock to start editing."); on the right the
  RFID tag state, "Windows mode" ("Printer does not use advanced driver
  interface"), a printer selector that the view keeps hidden, and the zoom
  slider. ANLAbel: the protection banner above the label says the same and
  holds the unlock box; the status bar has the printer, the label size, zoom
  to document and to objects, the zoom slider with its percentage, and chips
  for a printer queue problem, an uncertain print job and missing resources.
  Windows mode does not apply: ANLAbel always prints through the Windows
  driver. RFID is out of scope.
- Function editors (`propertypages/view/functions`, `FunctionPropertyXaml`):
  each function type has its own form. Concatenate lists its input data
  sources (add a data source, "Add fixed text", move, delete) with a delimiter
  and "Ignore empty values"; Subset takes a fixed offset and length, or a
  field number of delimited text with a text qualifier; DateOffset adds days,
  months and years, each fixed or from a data source, and formats the result;
  Linear computes f(x) = ax ± b or x/a ± b with decimal places and
  separators. ANLAbel's Data Manager function is one formula with its result
  for the current row, and an object's data source joins values directly
  (`{?Lot}-{#SERIAL}`), which covers Concatenate without a function. The 12
  customer labels hold four functions, all Concatenate, and they import as
  formulas. Not built: a form per function type.
- Print gap adjustment (`printergapadjustment`, Label Properties › Printer):
  "Enter the print gap adjustment values for the desired color ribbons", one
  value per ribbon colour (black, blue, CMYK, green, orange, red, white,
  yellow and three custom ones) for printers with several ribbons. No customer
  or sample label stores it. Not built.
- Message dialog (`Gui.Core/historydialog`): a warning icon and message, an
  expander with the earlier messages, and Ctrl+C copies the text, as Windows
  message boxes do. ANLAbel's `OperatorDialog` states the problem, what was
  affected and the next action, with technical detail in a muted line; its
  text cannot be copied.
- Find and replace (`Gui.Core/searchpanel`) belongs to the rich text editor:
  find, find next and previous, replace, replace all. ANLAbel edits rich text
  in the Properties panel (bold, italic, underline, font and size), without
  find and replace.

## Prompts, special characters, fonts and symbols (2026-10-31)

- Print-time prompts (`Gui.Core/variablesvaluepromptcontrol`): one grid with
  a prompt column and a value column; the cell shows the formatted value and
  the validation error as a tooltip. ANLAbel asks the same questions one per
  row in `PromptValuesWindow` (Enter moves to the next) and repeats the
  answers in Print Preview.
- Special characters: every NiceLabel value box has "Insert special
  character" (a menu of control characters such as GS, RS and EOT, also on a
  button beside the box). ANLAbel adds the GS1 separators itself from the
  `(AI)value` notation, and an imported ISO/IEC 15434 envelope keeps its
  control characters, but a formula string only knows `\n`, `\r` and `\t`,
  so a new envelope (`[)>` RS 06 GS … RS EOT) cannot be typed. No customer
  label uses one. Not built.
- Font picker (`menuregion/fontselectordropdown.xaml`): each family drawn in
  its own face with an "Abc 123" sample, an icon for TrueType or printer
  (device) fonts, and "Show printer fonts only". ANLAbel's lists draw each
  family in its own face; printer fonts do not apply because ANLAbel prints
  through the Windows driver, and preflight names a font that has no glyph
  for a character of the data.
- Clipart gallery (`Gui.Core/clipartgallerydialog`): pick a picture from a
  clipart folder, with its path. The installed NiceLabel 10 has no clipart
  folder. Eight customer labels carry one picture each; those checked are a
  RoHS mark, which imports as a picture. Not built: a built-in symbol set.
- Undo and redo (`Gui.Core/undoredodropdown`): drop-down lists of the named
  actions to undo several at once. ANLAbel (v0.355) names each step
  ("Di chuyển “PO”", "Đổi phông chữ 3 đối tượng"); the Undo and Redo buttons
  name the next step in their tooltips, the status bar names the step just
  taken, and the quick access arrows list the steps to undo or redo several
  at once.

## Coverage of the NiceLabel 10 views (2026-10-31)

Every view group extracted from DesignerLib, Gui.Core and WpfControls, and
where this document or `NICELABEL_10_REVERSE_ENGINEERING.md` compares it.

| View group (count) | ANLAbel counterpart or decision | Compared in |
| --- | --- | --- |
| `mainwindow`, ribbon, panels | Ribbon tabs, toolbox, Data/Layers/Objects, Properties | Ribbon, Panels, Shell rebuild |
| `menuregion` (11): backstage, passwords, font picker, new label tiles, print form | Tệp backstage, Bảo vệ, font lists, New Label Wizard, Print Preview | Backstage, Protection, Prompts…fonts, Print form |
| `wizards` (13): new label, database | New Label Wizard, Excel import | New Label Setup Wizard, Database wizard |
| `datasources` (5): Dynamic Data Manager, import, prompt order, explorer | Quản lý dữ liệu (where each prompt and function is used; deleting one in use asks first; the result box works out the selected function's own chain), Nhập nguồn dữ liệu, Thứ tự nhắc, Data panel; a paste brings the definitions its objects read (RE §28) | Quản lý dữ liệu, Data explorer drag and drop |
| `propertypages` (128) | Properties panel; the font, the linear barcode settings, Không in, Hiển thị, Khóa vị trí and Giữ tỉ lệ reach every selected object, with a blank or mixed value when the selected objects differ (RE §26, §29); X/Y, size, rotation and anchoring stay with the key object; PowerForms, RFID and Report pages out of scope | RE §23, Picture, Variable input and output rules, Status bar…function editors |
| `editors` (6): combo, date, time, rich text, custom boundaries | Prompt editors (dates RE §15), rich text in Properties; custom boundaries unused (RE §22) | Prompts…fonts |
| `mainregion` (20): landing page, object browser, layers, page menu | Start page, Đối tượng tab (Ctrl+F) marking hidden and locked objects; right-click menus on objects and on the label; layers not built; banners and solutions out | Object browser and layers |
| `designsurface` (28): selection frames, handles, toolbox, menus, in-place text, command box | Canvas adorners (protected Text/TextBox area), toolbox tiles, right-click menus, the designer keys (RE §28: Ctrl+A, Ctrl+G, Tab, PgUp/PgDn, Ctrl+B/I/U, zoom; F2 opens Properties, RE §29), name, data source and lock markers; keyboard resize and rotate are not built (protected resize lifecycle); in-place text editing is an owner decision; the properties button above a selection is not built; surface tips are a placeholder | Window, Status bar…function editors |
| `statusbarregion`, `printergapadjustment`, `historydialog`, `searchpanel` | Status bar and chips; gap adjustment not needed; operator dialog; no find and replace | Status bar…smaller dialogs |
| `variablesvaluepromptcontrol`, special characters, clipart, undo list | Prompt window and Print Preview answers; GS1 separators and the Data Manager's control characters; pictures; named undo steps with Undo ▾ and Redo ▾ lists | Prompts…symbols |
| `serialportdataeditor` | Values from a serial port (a scale): not built, no customer label reads one | — |
| `advancedcolorseparation` | Spot colours for colour printers: not built (one-colour thermal and office printers) | — |
| `actionseditor`, `Gui.Core/view/actioncontentview`, `scriptbuilder` | PowerForms actions and scripts: out of scope; imported functions become formulas (RE §21) | Print form |
| `backgroundoperations`, `debug`, styles | Progress and developer views: no operator need | — |
| `Gui.Core/usercontrols` (78), `propertyeditor` (53), `WpfControls` | Shared editors, licensing and activation (not applicable), record navigation, data grids | Panels, Data explorer |
