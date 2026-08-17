# NiceLabel Text Box research baseline

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
| Text Font Scaling (50%–200% width stretch) | ANLAbel: free-Text border-drag → `FixedFrame` lock + frame-fit `HorizontalScale`/`VerticalScale` on shared layout (distortion allowed); not TextBox wrap/clip | Co (product mapping; NiceLabel Text cannot border-drag) |
| Text Box None | `TextBox + FixedFrame` | Co |
| Overflow blocks print | `TextOverflow.Error` | Co, mac dinh |
| Ignore excessive content | `TextOverflow.Clip` | Co; UI doi thanh discard ro rang |
| Adjust height | Khong ap dung | Chu dich loai bo: ANLAbel TextBox phai luon cho nguoi thiet ke so huu ca Width/Height |
| Fit by font size min/max | `ShrinkFont` + `TextFitMinimum/MaximumFontSizePt` | Co; ten enum cu duoc giu de doc file cu |
| Fit by scaling min/max | `ScaleWidth` + `TextFitMinimum/MaximumScale` | Co |
| Same-size group | Chua co group resolver | Gap cap 2 |
| Justified | `TextAlignmentMode.Justify` | Co |
| Line spacing | `LineHeightPt` | Co o muc line-height; chua co character spacing |
| Effects/Boundaries | RTL co; inverse/mirror/boundary shape chua co | Gap cap 3 |
| Anchor/relative position | Rotation/position co; variable-size anchor/relative chain chua co | Gap cap 2 |
| Ellipsis | `TextOverflow.Ellipsis` | Mo rong rieng ANLAbel, khong gan nhan NiceLabel |

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

## Thu tu parity tiep theo

1. Same-size group resolver theo row va cung fit mode; scene identity phai mang group name.
2. Character spacing, inverse/mirror va non-rectangular text boundaries.
3. Content mask rieng cho textual object, khong tron voi binding/formula engine.

Nhung muc gap tren khong duoc mo ta la da tuong duong NiceLabel. Core containment/fit da dung contract; parity nang cao chi duoc danh dau hoan thanh khi co model, renderer, preflight, persistence va regression test tuong ung.
