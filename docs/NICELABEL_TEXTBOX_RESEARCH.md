# NiceLabel Text Box research baseline

> Reference status (2026-09-15): competitor research, not a full implementation
> requirement or parity backlog. Apply [reference policy](REFERENCE_POLICY.md)
> and [core standard](CORE_SOFTWARE_STANDARD.md) to select ANLAbel core behavior.
> The user-approved ANLAbel Text/TextBox decisions recorded here remain
> protected by AGENTS.md.

Ngay doi chieu: 2026-08-11. Nguon uu tien: tai lieu chinh thuc Loftware/NiceLabel Help Center va user guide chinh thuc. Tai lieu nay tach ro du lieu duoc NiceLabel cong bo va quyet dinh implementation cua ANLAbel; khong suy dien tinh nang cua NiceLabel tu hanh vi WPF.

## Nguon chinh thuc da kiem tra

1. [Label Objects - Text va Text Box](https://help.nicelabel.com/hc/en-001/articles/4402152643729-Label-Objects): dinh nghia hai object, Source, Style, Text Fit, Effects, Boundaries, Position va General.
2. [Working with Objects](https://help.nicelabel.com/hc/en-001/articles/4402145579537-Working-with-Objects): cach tao object, click-drag, paste single-line/multi-line, resize va anchoring point.
3. [Tabs and Ribbons - Text contextual tab](https://help.nicelabel.com/hc/en-001/articles/4402145576209-Tabs-and-Ribbons): nhan UI chinh thuc cho Text Fit va y nghia Ignore excessive content.
4. [Variables](https://help.nicelabel.com/hc/en-001/articles/4403719472145-Variables): NiceLabel khuyen dung Text Box thay cho Output Rules multiline cua bien; word wrap tai space la logic cua output rule cu.
5. [New in Design & Print](https://help.nicelabel.com/hc/en-001/articles/10989241351313-New-in-Design-Print): same-size group cho nhieu Text Box dung chung ket qua fit font/scale.
6. [NiceLabel 2019 Designer User Guide, muc 3.3.2, trang 130-136](https://help.nicelabel.com/hc/article_attachments/25194666085777): ban tham chieu day du ve Source, Style, Text Fit, Effects, Boundaries, Position va General.
7. [Designing Labels with Variable Length](https://help.nicelabel.com/hc/en-001/articles/4402145604241-Designing-Labels-with-Variable-Length): object thay doi kich thuoc co the lam label tang chieu dai va can relative positioning.

## Contract NiceLabel da duoc xac minh

### Text va Text Box la hai object khac nhau

| Thuoc tinh | Text | Text Box |
| --- | --- | --- |
| Quyen so huu kich thuoc | Noi dung; object tang/giam theo ky tu | Width/height co the dat truoc; Text Fit quyet dinh height/font |
| Resize bang click-drag | Khong dinh nghia size khi tao | Co the click-drag de dinh nghia frame |
| Du lieu bien dai | Object tiep tuc doi kich thuoc | Noi dung duoc fit trong field da thiet ke |
| Multiline | Khong phai lua chon khuyen nghi cho variable multiline | La object NiceLabel khuyen nghi |

NiceLabel ghi ro Text object khong cho sua Width/Height bang tay; cac gia tri nay chi la thong tin ve kich thuoc hien tai. Text Box ton tai de du lieu co do dai bien thien van nam trong vung thiet ke.

### Text Fit

NiceLabel cong bo bon che do:

1. `None`: frame va font khong tu thay doi. Neu noi dung vuot box, bao loi va khong in.
2. `Ignore excessive content at print`: tuy chon cho phep in trong che do None; chi phan nam trong box duoc dung, phan con lai bi loai bo.
3. `Adjust height to fit content`: giu width de wrap va tu tang/giam height theo noi dung.
4. `Fit content by adjusting font size`: font co the tang hoac giam trong khoang Minimum size - Maximum size.
5. `Fit content by scaling font`: font co the co/ngang hoac gian/ngang trong khoang Minimum font scaling - Maximum font scaling.

Same-size group la lop dieu phoi bo sung cho font-size fit hoac font-scale fit. Tat ca Text Box cung group dung ket qua chung, bang ket qua han che nhat trong group, de typography tren label nhat quan.

### Layout va style co lien quan den fit

- Horizontal alignment: Left, Center, Right va Justified; Justified chi co o Text Box.
- Co line spacing va character spacing rieng.
- Font scaling 100% la binh thuong, 50% la nua chieu rong, 200% la gap doi chieu rong.
- Effects gom Inverse, Mirror va RTL printing.
- Boundary trai/phai co shape, width va height; boundary thay doi duong text flow ben trong object, khong phai border trang tri.
- Anchoring point quyet dinh huong object variable-size tang/giam: kich thuoc thay doi ve phia doi dien anchor.
- Relative positioning giu khoang cach voi bien label/object khac khi object variable-size thay doi.

### Source va print safety

- Source co the la Fixed data, Variable, Function, Database hoac Counter; content mask chay truoc khi noi dung duoc hien thi/in.
- `None` + overflow mac dinh la fail-closed: co error va label khong in.
- `Ignore excessive content` la quyet dinh co chu dich va mat du lieu; UI phai noi ro phan du bi discard, khong dung nhan mo ho `Allow overflow`.
- Internal printer fonts phu thuoc printer driver. Ket qua metric phai duoc kiem tra tren printer/font thuc; khong duoc xem preview font thay the la bang chung print-safe.

## Mapping sang ANLAbel

| NiceLabel | ANLAbel | Trang thai |
| --- | --- | --- |
| Text auto dimensions | `Text + AutoFit` | Co |
| Text size is font-driven; cannot hand-edit W/H | NiceLabel: size informational only | Co (doc) |
| Text Font Scaling (50%–200% width stretch) | `Style.FontScalePercent` (10–400%) tren shared layout + frame AutoFit; border-drag van khoa `FixedFrame` + frame-fit compress | Co (2026-10-29) |
| Text Box None | `TextBox + FixedFrame` | Co |
| Overflow blocks print | `TextOverflow.Error` | Co, mac dinh |
| Ignore excessive content | `TextOverflow.Clip` | Co; UI doi thanh discard ro rang |
| Adjust height | `TextSizing.AdjustHeight`: giu Width, Height theo noi dung (designer + print tung row) | Co (chu san pham doi contract 2026-10-29) |
| Fit by font size min/max | `ShrinkFont` + `TextFitMinimum/MaximumFontSizePt` | Co; ten enum cu duoc giu de doc file cu |
| Fit by scaling min/max | `ScaleWidth` + `TextFitMinimum/MaximumScale` | Co |
| Same-size group | Chua co group resolver | Gap cap 2 |
| Justified | `TextAlignmentMode.Justify` | Co |
| Line spacing | `LineHeightPt` | Co o muc line-height; chua co character spacing |
| Effects/Boundaries | RTL co; inverse/mirror/boundary shape chua co | Gap cap 3 |
| Anchor/relative position | Rotation/position co; variable-size anchor/relative chain chua co | Gap cap 2 |
| Ellipsis | `TextOverflow.Ellipsis` | Mo rong rieng ANLAbel, khong gan nhan NiceLabel |

### Nhan UI tieng Viet (chu san pham duyet 2026-10-15)

Chi doi chu hien thi trong Bang thuoc tinh va loi bao tran chu tren canvas. Hanh vi, enum, gia tri luu file va preset padding khong doi.

| Enum | Nhan cu | Nhan moi |
| --- | --- | --- |
| `TextSizing.FixedFrame` | Wrap in fixed frame | `Xuống dòng, giữ cỡ phông` |
| `TextSizing.ShrinkFont` | Reduce font size to fit | `Đổi cỡ phông cho vừa khung` (fit co the tang hoac giam trong min/max, nen khong goi la "giam") |
| `TextSizing.ScaleWidth` | Fit by scaling font width | `Co giãn chiều ngang chữ cho vừa` |
| `TextSizing.AdjustHeight` | Adjust height to fit content | `Giữ chiều rộng, chiều cao theo nội dung` (2026-10-29) |
| `TextOverflow.Error` | Block print and warn | `Chặn in và cảnh báo` |
| `TextOverflow.Clip` | Ignore excessive content (discard) | `Cắt bỏ phần chữ dư (không in)`: van noi ro phan du bi bo, dung quyet dinh o muc Source va print safety |
| `TextOverflow.Ellipsis` | End with ellipsis (ANLAbel) | `Thêm dấu ba chấm ở dòng cuối` |

- Banner Text: `Chữ tự do theo nội dung và có thể dài ra theo chiều ngang. Dùng Ô chữ khi nội dung in phải nằm gọn trong một vùng cố định.`
- Banner TextBox: `Ô chữ có khung: kéo hoặc nhập Rộng/Cao để định khung. Chữ xuống dòng hoặc co cho vừa bên trong, không làm đổi khung và không bao giờ in ra ngoài khung.`
- Canvas: `Chữ tràn ra ngoài khung Ô chữ. Nới khung, bớt chữ hoặc giảm cỡ chữ.` (Text khoa khung: `Chữ tràn khung đã khóa. …`.)
- Placeholder cua Ô chữ moi: `Ô chữ` thay cho `Text Box` (chu san pham duyet 2026-10-15; invariant 5 trong AGENTS.md va gate `new text box uses compact label-aware frame` cap nhat cung luc). Chu ngan hon nen khung 32 x 6 mm va tem 20 x 8 mm van khong overflow. Chu mau khi them Chữ moi: `Chữ tự do dài ra theo nội dung`.
- Gate: `text box overflow is reported in Vietnamese on the canvas`; scanner Viet hoa nay doc ca `Setter Value` cua Text/Content/ToolTip/Header, la cho sau tuy chon va hai banner tieng Anh da lot qua.

## Thuat toan ANLAbel duoc phep dung

1. Resolve data cua tung row truoc khi layout.
2. Tru padding vat ly khoi frame de co content rectangle.
3. Wrap theo Unicode grapheme/word va ton trong newline; khong cat surrogate pair hay combining sequence.
4. `FixedFrame`: dung authored font; clip visual tai frame; Error block preflight, Clip discard phan du.
5. Font-size fit: tim font lon nhat trong min/max van fit ca width va height; neu minimum van khong fit thi ap overflow policy. Khong mutate frame.
6. Font-scale fit: lay ty le contentWidth/widestLine va clamp vao min/max; transform theo Left/Center/Right anchor. Height overflow van do overflow policy xu ly. Khong mutate frame.
7. **Free Text frame-fit compress:** do natural ink; neu authored content width/height nho hon natural, dat `HorizontalScale` va/hoac `VerticalScale` = content/natural (clamp 0.01–1, doc/lap doc lap — distortion OK). Khong bat `ShouldConstrainToBox`, khong wrap Text nhu TextBox, khong Error preflight chi vi selection hep. Designer va print dung chung `CreateTextLayout`/`DrawTextLayout`.
8. Sua content, binding hay PreviewRow chi reflow glyph; Width/Height chi doi khi nguoi dung keo resize hoac nhap Properties (Text AutoFit van co the mo frame theo content).
9. Preview, print va preflight phai goi chung metric resolver. Thuoc tinh fit phai nam trong clone, save/load, immutable snapshot va scene hash.

## Toi uu dien tich cho tem nho

- Tai lieu NiceLabel mo ta Left/Center/Right la canh chu voi bien object va khong quy dinh inset 1 mm bat buoc. Vi vay padding la style co chu dich, khong phai chi phi an mac dinh cua Text Box.
- BarTender tach Single Line (khong wrap) va Multi-line (nguoi dung dat width, text wrap). Dieu nay cung co nghia frame khoi tao chi can du de thao tac, khong nen mang san mot doan van dai chiem cho.
- Baseline cu cua ANLAbel la 42 x 16 mm + padding 1 mm moi canh. Tren frame 20 x 6 mm, content chi con 18 x 4 mm = 60% dien tich; voi tem thap, Y=18 mm con co the dat object ngoai label.
- Quyet dinh implementation: object moi toi da 32 x 6 mm, co margin thich nghi kich thuoc label, padding 0.2 mm, placeholder ngan va vertical Center. Frame 20 x 6 mm con 18.6 x 5.6 mm, tuong duong 91.4% dien tich in huu dung.
- Preset `Tight 0` cho phep dung 100% frame khi font/ky tu cho phep; `Compact 0.2` la mac dinh can bang ink-edge safety; `Comfort 1` chi dung tren tem rong.
- Selection chrome khong duoc danh doi khong gian in: hit target va marker tach rieng, hit target 10 DIP nhung marker nhin thay chi 5 DIP.

### Trang Kieu theo nhom Text Style cua NiceLabel 10 (chu san pham duyet 2026-11-01)

NiceLabel 10 `textstylepropertypageview`: nhom Font, Alignment, Spacing, Special; Text Box co trang
Automatic sizing va boundaries rieng. ANLAbel tach the "Kiểu chữ" mot khoi thanh cac the cung ten:
`Phông chữ` (phong, co, dam/nghieng/gach, ti le chu ngang cua Text, mau, loi phong), `Căn lề`,
`Khoảng cách` (chieu cao dong), `Đặc biệt` (chieu chu); rieng Text Box: `Tự co giãn chữ` (vua trong
khung, khi noi dung qua cao, min/max phong va co gian) va `Lề trong` (dem trong, ba preset, ghi de canh).

Chi doi bo cuc: moi dieu khien, binding, preset (`Sát 0`, `Gọn .2`, `Thoáng 1` = Tight 0, Compact 0.2,
Comfort 1), mac dinh va hanh vi giu nguyen; khong doi sizing, wrap, clip, padding hay vong doi resize.
Gate: `the text style page follows NiceLabel's groups` (thu tu nhom, nhom cua Text va Text Box, preset
giu ten/gia tri va van dat padding); bay gate Text/TextBox duoc bao ve van dat.

### Sua chu tai cho tren tem (chu san pham duyet 2026-11-01)

NiceLabel 10 `DocumentPageView`/`TextSelectionItem`: F2 dua item ho tro sua tai cho vao EditMode (item khac:
mo trinh sua thuoc tinh), Enter chi vao EditMode, nhap dup vao chu va lenh Edit text cua menu cung vay.
O sua cua NiceLabel (`TextSelectionEditItemView`, BAML `designsurface/selectionitem/view/textselectionedititemview`):
`WatermarkTextBox` voi `AcceptsReturn=True`, `TextWrapping=Wrap`, `Text` gan voi noi dung bang
`UpdateSourceTrigger=PropertyChanged` (tem doi ngay khi go); `KeyDown` chi bat Esc -> `DisableEditMode` (tat
EditMode, `PushToUndoStack`, tra focus ve phan tu truoc), `LostKeyboardFocus` -> `DisableEditMode` (giu chu).

ANLAbel: voi Chữ hoac Ô chữ co noi dung co dinh (khong doc nguon du lieu), chon mot minh, tren tem khong bi bao
ve, F2, Enter tren bang thiet ke, nhap dup hoac menu `Sửa chữ` mo mot o nhap de len khung doi tuong, cung phong,
co va canh le theo thu phong, noi dung duoc chon san de go de. Tem doi ngay khi go (Luu hay In giua chung dung chu
moi); Enter xuong dong nhu o Noi dung o Thuoc tinh; Esc hoac roi o la xong va giu chu, ca lan sua la mot buoc
hoan tac (Ctrl+Z de bo). Menu Cat/Chep/Dan cua chinh o sua khong ket thuc lan sua. O sua bam theo khi cuon hay thu
phong. Go mot chu cai khong mo sua tai cho vi L, R, C la phim ve cua ANLAbel. Doi tuong doc nguon du lieu, nhieu
doi tuong dang chon va doi tuong khac: F2, nhap dup van mo Thuoc tinh. (v0.366 cho Enter la xong, Esc la bo: phim
tu doan, da sua theo bang chung tren.)

Hop dong giu nguyen: noi dung di qua cung duong voi Thuộc tính › Nguồn › Chữ (`MainViewModel.SetObjectText` dat
`Text`; o sua ap cung luat nhap `CanApplyTextBoxInput`, nen Ô chữ dat Tràn = Lỗi tu choi chu khong vua khung, dieu
v0.366 bo sot), nen Text van lay co theo noi dung (invariant 1), Ô chữ giu khung du chu dai (invariant 2, tru
AdjustHeight da duyet), khong doi wrap, clip, padding, tay nam hay vong doi resize; o nhap la lop phu cua
MainWindow, LabelDesignerCanvas va SelectionResizeAdorner khong doi. Gate: `in-place editing edits a text on the
label` (tem doi khi go, Enter khong ket thuc, Esc giu, roi o giu, menu rieng cua o, mot buoc hoan tac, thu phong,
khung Ô chữ, Tràn = Lỗi, chu doc nguon, menu `Sửa chữ`, nhap dup, F2, nhieu doi tuong, tem bao ve); cac gate nhap
dup va F2 cu dung ma vach cho duong Thuoc tinh.

## Thu tu parity tiep theo

1. Same-size group resolver theo row va cung fit mode; scene identity phai mang group name.
2. Character spacing, inverse/mirror va non-rectangular text boundaries.
3. Content mask rieng cho textual object, khong tron voi binding/formula engine.

Nhung muc gap tren khong duoc mo ta la da tuong duong NiceLabel. Core containment/fit da dung contract; parity nang cao chi duoc danh dau hoan thanh khi co model, renderer, preflight, persistence va regression test tuong ung.
