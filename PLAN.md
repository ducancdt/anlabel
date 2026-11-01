# ANLAbel Phase 1 Plan

## Sau v0.374

- Khi chọn nhiều đối tượng, khung của từng đối tượng giống khung trong của một đối tượng (nét gạch xanh #0072EB trên
  nền trắng ngay trong mép, như MoveThumb của NiceLabel 10); vật mốc vẫn có khung đậm riêng để căn chỉnh.
- Rê chuột qua một đối tượng trên bảng thiết kế thì đối tượng đó có viền xanh mờ (như NiceLabel 10: 2 DIP, cách 4
  DIP, hình elip thì viền tròn), cho biết bấm sẽ chọn đối tượng nào khi các đối tượng chồng nhau hoặc không có
  viền. Không hiện trên đối tượng đang chọn, khi đang cầm công cụ vẽ, hay trên tem được bảo vệ.

## v0.374 — Vùng chọn vẽ như NiceLabel: khung hai nét, điểm neo tô xanh, tay xoay và nút thuộc tính đúng chỗ

_Gồm các commit 01/11/2026 (14:30–15:07)._

- Nút thuộc tính của vùng chọn đặt như CommandBox của NiceLabel 10 (v0.366 tự chọn chỗ cạnh góc): đứng trên mép
  trên của đối tượng, ở đầu bên phải (NiceLabel: Margin 0 -20 0 0, canh phải), nút vuông xanh #0072EB 22 × 20 có
  viền trắng hai bên, bóng nhẹ, biểu tượng ba thanh 12 × 12. Nút nằm trên lớp tay nắm nên dừng cách góc 5 DIP
  (không che vùng bấm 10 DIP của tay nắm góc); đối tượng hẹp tới mức nút che tay nắm giữa cạnh trên, hoặc sát mép
  trên khung nhìn, thì nút về cạnh góc như trước.
- Vùng chọn vẽ đúng như NiceLabel 10 (đối chiếu BAML DesignerLib và ảnh chụp NiceLabel; v0.372 làm theo ảnh nên
  thiếu): một nét liền xanh ngay ngoài mép vật cùng một nét gạch xanh trên nền trắng ngay trong mép (nhìn thấy cả
  trên nền đen); nhóm nhiều vật chỉ có nét liền bao quanh, từng vật giữ khung gạch riêng. Tay nắm trắng viền đen;
  tay nắm ở điểm neo của đối tượng tô xanh (điểm đứng yên khi đối tượng đổi kích thước theo nội dung; không tô khi
  neo ở tâm, hoặc khi các vật được chọn neo khác nhau). Tay xoay màu tím như NiceLabel. Kích thước, vị trí tay nắm
  và khung đậm của vật mốc không đổi.
- Tay xoay lấy đúng kích thước của NiceLabel 10 (RotateThumb 8 × 29 trên khung): chấm 8 DIP, tâm cách đỉnh khung
  25 DIP, cuống 21 DIP (v0.372 đoán chấm 7 DIP cách 18 DIP). Khi bật Xem › Tên đối tượng, chấm cũ đè lên nhãn tên
  phía trên vùng chọn; giờ chấm nằm trọn phía trên nhãn, vùng bấm vẫn 10 DIP. Tay nắm đổi kích thước không đổi.

## v0.373 — Tem bảo vệ không còn tay nắm, ngưỡng X theo 2 điểm, ảnh mờ chỉ cảnh báo, tay nắm góc tròn

_Gồm các commit 01/11/2026 (13:10–13:32)._

- Tay nắm góc của vùng chọn tròn như NiceLabel 10, tay nắm giữa cạnh vẫn vuông; dấu vẫn 5 DIP, vùng bấm 10 DIP.
- Hai quy tắc an toàn in theo quyết định của chủ dự án (2026-11-01):
  - Mã vạch 1D có X do người thiết kế đặt chỉ bị chặn in khi vạch hẹp ra dưới 2 điểm ở DPI máy in; từ 2 điểm tới
    ngưỡng 0,19 mm thì chỉ cảnh báo (trước đây cứ dưới 0,19 mm là chặn). Lưu ý: 0,125 mm ở 203 hay 300 dpi chỉ
    được 1 điểm (300 dpi: 1,48 điểm làm tròn còn 1), nên vẫn bị chặn; ở 600 dpi được 3 điểm, chỉ cảnh báo.
  - Ảnh có mật độ thấp hơn lưới điểm của máy in (logo 138–205 PPI, cả ảnh 297–299 PPI) chỉ cảnh báo, không chặn.
- Tem được bảo vệ không còn hiện tay nắm của đối tượng đang chọn. Bảng thiết kế bị tắt trên tem bảo vệ, nhưng
  tay nắm nằm ở lớp trang trí không thừa hưởng việc tắt đó, nên kéo tay nắm (và tay xoay của v0.372) vẫn đổi được
  tem. Nay tay nắm ẩn khi bảng thiết kế tắt, hiện lại khi bỏ bảo vệ; đường đổi kích thước và xoay cũng từ chối
  trên bảng tắt. Phát hiện khi kiểm thử trực tiếp v0.372 (chuột thật, chủ dự án cho phép): kéo tay xoay 0° → 90°
  và Ctrl+Z về 0° đều đúng.

## v0.372 — Vùng chọn và tay xoay như NiceLabel, biểu tượng cây Dữ liệu, ô Nội dung cao

_Gồm các commit 01/11/2026 (11:56–12:41)._

- Vùng chọn như NiceLabel 10 (chủ dự án duyệt 2026-11-01, vùng tay nắm được bảo vệ): không còn nền xanh bên trong;
  một đối tượng có một khung nét đứt đúng mép cùng tám tay nắm như cũ (vùng bấm 10 DIP, dấu 5 DIP); thêm tay
  xoay (chấm xanh trên cuống, phía trên giữa khung): kéo để xoay quanh tâm theo bước 90° (đối tượng tem chỉ
  xoay 0/90/180/270, như NiceLabel bắt 90° cho chữ phông máy in và vùng đảo màu), khung xem trước xoay theo,
  thả chuột thì áp dụng, một bước hoàn tác; kéo ít hoặc hủy thì không đổi; đường kẻ không có tay xoay. Ô chữ
  giữ khung khi xoay.
- Cây Dữ liệu có biểu tượng theo loại như NiceLabel 10 và Quản lý dữ liệu (biến nhập khi in, cột dữ liệu, hàm,
  ngày in, số Serial) thay cho tiền tố “?” và dấu { }; cú pháp {?Tên} hiện ở gợi ý khi rê chuột (chủ dự án
  duyệt 2026-11-01).
- Thuộc tính › Nguồn: ô nội dung của Chữ và Ô chữ cao sẵn khoảng mười dòng như NiceLabel 10 (chủ dự án duyệt
  2026-11-01, vùng Text/TextBox được bảo vệ, chỉ đổi bố cục).
- Nhấp đúp bằng chuột thật vào một chữ trên app đã cài (v0.371) mở sửa tại chỗ, ô nhập nhận bàn phím, nội dung
  giữ nguyên; Esc đóng (chủ dự án cho dùng chuột thật 2026-11-01).

## v0.371 — Bảng Dữ liệu gọn như NiceLabel, gõ để sửa chữ

_Gồm các commit 01/11/2026 (11:50–11:50)._

- Bảng Dữ liệu gọn như NiceLabel 10 (chủ dự án duyệt 2026-11-01): nguồn dữ liệu là dòng đầu dưới “Trường dữ
  liệu” (“+ Liên kết Excel / CSV…” khi chưa có tệp; tên tệp · dòng đang chọn khi đã liên kết), kèm cảnh báo và
  nút sửa ngay: dữ liệu cũ (Cập nhật Excel), liên kết hỏng (Liên kết lại Excel…), tệp đang mở trong Excel, hàm
  lỗi. Hai cảnh báo dữ liệu cũ và hàm lỗi trước đây chỉ nằm trong một cây bị ẩn. Enter hoặc nhấp đúp dòng này
  mở Quản lý dữ liệu › Cơ sở dữ liệu. “Dòng Excel” và “Kiểm tra ràng buộc” thu gọn dưới cây; tiêu đề ghi trạng
  thái (đạt, N lỗi, chờ tệp Excel). Bỏ thẻ “Dữ liệu hiện tại”, “Chưa liên kết dữ liệu” và “Cài đặt dữ liệu”:
  cột theo dõi dòng và số bản mỗi dòng chuyển vào Quản lý dữ liệu › Cơ sở dữ liệu; bộ đếm Serial, biến nhập khi
  in, nguồn dùng chung và biến đổi vốn đã có ở Quản lý dữ liệu, ribbon Dữ liệu và cây. Lưới biến nhập khi in
  trong “Cài đặt dữ liệu” còn sửa được cả trên tem bảo vệ, lách qua khóa của v0.366; nay không còn.
- Gõ để sửa chữ như NiceLabel 10 (chủ dự án duyệt 2026-11-01): đang chọn một Chữ hoặc Ô chữ có nội dung cố
  định trên bảng thiết kế, gõ một ký tự là mở sửa ngay trên tem, ký tự đó thay nội dung. L, R, C khi đó là gõ
  chữ; không chọn chữ thì vẫn vẽ đường, chữ nhật, elip như cũ.

## v0.370 — Bảng thiết kế bỏ viền Chữ, ô màu chữ hiện đúng màu

_Gồm các commit 01/11/2026 (10:35–10:57)._

- Bảng thiết kế không vẽ viền quanh Chữ (chủ dự án duyệt 2026-11-01, vùng Text/TextBox được bảo vệ), như bản in
  và NiceLabel. Tem nhập từ NiceLabel và lưu trước v0.368 giữ viền mặc định 0,2 mm trong dữ liệu của chữ; bảng
  thiết kế từng vẽ khung quanh mọi chữ đó dù không in ra. Ô chữ vốn đã không vẽ; hình (chữ nhật, elip, đường)
  vẫn vẽ viền vì có in. Dữ liệu trong tệp không đổi.
- Ô màu chữ trên ribbon (Trang chủ, thẻ Chữ) hiện đúng màu của chữ đang chọn: ô màu trong bảng dù viết
  hoa hay thường, ô trống với màu ngoài bảng. Trước đây ô giữ ô màu của chữ chọn trước (cùng lỗi với ô
  phông và ô cỡ); nay dùng chung cơ chế với chúng.

## v0.369 — Ô phông chữ hiện đúng phông của đối tượng đang chọn

_Gồm các commit 01/11/2026 (10:23–10:23)._

- Ô phông chữ (ribbon Trang chủ, thẻ Chữ, Thuộc tính › Kiểu) và ô phông chữ dưới mã của mã vạch (menu
  ribbon, Thuộc tính) hiện đúng phông của đối tượng đang chọn, kể cả phông ngoài danh mục có bản quyền
  (tem nhập từ NiceLabel giữ “Arial Narrow”). Trước đây chọn chữ Arial rồi chữ Arial Narrow, ô vẫn ghi
  Arial: cùng lỗi với ô cỡ ở v0.368. Mọi ô phông và ô cỡ nay dùng chung một cách hiện và nhận giá trị;
  ô phông trên ribbon gõ được tên rồi Enter như NiceLabel, nhưng chỉ nhận phông trong danh mục.

## v0.368 — Chữ nhập từ NiceLabel không còn khung, ô cỡ chữ hiện đúng mọi cỡ

_Gồm các commit 01/11/2026 (10:03–10:03)._

- Đặt cạnh NiceLabel 10 với cùng tem mẫu AIAG B10 Mixed Load (tài liệu bố cục, mục 2026-11-01):
  - Chữ và Ô chữ nhập từ NiceLabel không còn mang viền mặc định 0,2 mm. Bảng thiết kế vẽ viền đó quanh
    mọi chữ nhập vào, trong khi NiceLabel không có và tem in ra cũng không có. Tệp đã nhập và lưu trước bản
    này vẫn giữ dữ liệu viền ấy, nên vẫn hiện khung trên bảng thiết kế (vùng Text/TextBox được bảo vệ:
    chờ chủ dự án quyết).
  - Ô cỡ chữ (ribbon Trang chủ, thẻ Chữ, Thuộc tính › Kiểu) hiện đúng cỡ của chữ đang chọn, kể cả cỡ
    ngoài danh sách; ô trên ribbon nay gõ được rồi Enter như NiceLabel. Trước đây chọn chữ 12 pt rồi
    chữ 48 pt, cả hai ô vẫn hiện 12, và chọn 12 không áp được. Danh sách cỡ là của NiceLabel 10
    (4–14, 16–28 số chẵn, 36, 48, 72), giữ thêm 32 như cũ.

## v0.367 — Sửa chữ trên tem đúng phím NiceLabel và giữ luật chặn tràn của Ô chữ

_Gồm các commit 01/11/2026 (09:29–09:29)._

- Sửa chữ trên tem theo đúng NiceLabel 10 (đối chiếu `TextSelectionEditItemView` dịch ngược; v0.366 dùng phím
  tự đoán): chữ hiện lên tem ngay khi gõ nên Lưu hay In giữa chừng vẫn dùng chữ mới; Enter xuống dòng như ô
  Nội dung ở Thuộc tính; Esc hoặc bấm ra ngoài là xong và giữ chữ, cả lần sửa là một bước hoàn tác (Ctrl+Z).
  Ô sửa áp cùng luật nhập với ô Nội dung, nên Ô chữ đặt Tràn = Lỗi từ chối chữ không vừa khung (v0.366 bỏ
  sót luật này). Ô sửa bám theo khi cuộn hay thu phóng; nhiều đối tượng đang chọn thì F2 và nhấp đúp vẫn mở
  Thuộc tính; menu Cắt/Chép/Dán của chính ô sửa không kết thúc lần sửa.

## v0.366 — Sửa chữ trên tem, nút thuộc tính ở góc vùng chọn, trang Kiểu theo nhóm NiceLabel, tem bảo vệ chỉ sửa bộ đếm

_Gồm các commit 01/11/2026 (23:56–09:01)._

- Sửa chữ ngay trên tem như NiceLabel 10 (chủ dự án duyệt 2026-11-01, vùng Text/TextBox được bảo vệ):
  với Chữ hoặc Ô chữ có nội dung cố định, F2, Enter trên bảng thiết kế, nhấp đúp hoặc menu “Sửa chữ”
  mở ô nhập đè lên khung, cùng phông và cỡ theo thu phóng; Enter (Shift+Enter xuống dòng trong Ô chữ)
  hoặc rời ô để áp dụng, một bước hoàn tác; Esc bỏ. Đi qua cùng đường với Thuộc tính › Nguồn › Chữ nên
  hợp đồng giữ nguyên (Chữ đo theo nội dung, khung Ô chữ không đổi). Đối tượng đọc nguồn dữ liệu và đối
  tượng khác: F2 và nhấp đúp vẫn mở Thuộc tính. Gõ một chữ cái không mở sửa vì L, R, C là phím vẽ.
- Trang Kiểu của Chữ và Ô chữ theo nhóm Text Style của NiceLabel 10 (chủ dự án duyệt 2026-11-01, vùng
  Text/TextBox được bảo vệ): thẻ Phông chữ, Căn lề, Khoảng cách, Đặc biệt; riêng Ô chữ thêm Tự co giãn
  chữ và Lề trong. Chỉ đổi bố cục: điều khiển, binding, ba preset đệm (Sát 0, Gọn .2, Thoáng 1), mặc
  định và hành vi giữ nguyên; docs/NICELABEL_TEXTBOX_RESEARCH.md ghi quyết định.
- Nút thuộc tính ở góc vùng chọn như CommandBox của NiceLabel 10 (quyết định của chủ dự án 2026-11-01):
  một nút xanh nhỏ ngay ngoài góc trên phải của đối tượng đang chọn, cách vùng bấm 10 DIP của tay nắm
  góc, mở Thuộc tính như F2; đi theo khi kéo, thu phóng, cuộn; ẩn khi không chọn gì, đang vẽ hoặc tem
  được bảo vệ. Là lớp phủ của MainWindow: SelectionResizeAdorner và LabelDesignerCanvas không đổi.
- Tem được bảo vệ (quyết định của chủ dự án 2026-11-01): Quản lý dữ liệu chỉ sửa được bộ đếm Serial và
  cơ sở dữ liệu; biến nhập khi in và hàm chỉ để xem (nút thêm, xóa, Thứ tự nhắc, Nhập nguồn dữ liệu và
  trình sửa biến/hàm tắt, cả “+ Thêm” ở bảng Dữ liệu và hai nút trên thẻ Dữ liệu). Trước đây sửa được
  biến, hàm và bộ đếm trên tem đã khóa.

## v0.365 — Bàn phím mở mục bảng Dữ liệu và menu của vùng chọn

_Gồm các commit 31/10/2026 (23:37–23:45)._

- Bàn phím làm được hai việc trước chỉ có chuột: Enter trên một mục của bảng Dữ liệu mở nó trong Quản lý
  dữ liệu như nhấp đúp; phím menu hoặc Shift+F10 trên bảng thiết kế mở menu của đối tượng đang chọn (của
  tem khi không chọn gì), như NiceLabel áp lệnh ngữ cảnh cho vùng chọn; trước đây menu bàn phím lấy đối
  tượng của lần chuột phải gần nhất.

## v0.364 — Danh sách Đối tượng đang lọc đi theo lựa chọn

_Gồm các commit 31/10/2026 (23:29–23:29)._

- Danh sách Đối tượng đang lọc bằng ô Tìm: đối tượng vừa đặt từ hộp công cụ bị bộ lọc giấu, hàng cũ vẫn
  sáng và bấm vào nó không chọn được (danh sách coi như đã chọn). Nay đối tượng đang chọn luôn có trong
  danh sách và hàng sáng theo nó. Tìm thấy khi kiểm thử trực tiếp bản cài v0.363 bằng UI Automation.

## v0.363 — Tên cho trình đọc màn hình, xem trước giá trị khi in, chèn nguồn vào điều kiện

_Gồm các commit 31/10/2026 (23:00–23:18)._

- Thuộc tính › Chung: dưới ô “Điều kiện hiển thị” có nút “Chèn nguồn ▾” chèn biến, cột, hàm hoặc giá trị
  khi in vào chỗ con trỏ rồi áp dụng (một bước hoàn tác), như trình soạn điều kiện của NiceLabel 10;
  trước đây phải tự gõ tên nguồn trong ngoặc nhọn. Menu nguồn dùng chung cho ▾ hộp công cụ, Nội dung
  ghép và điều kiện.
- Thuộc tính › Ràng buộc của đối tượng chỉ đọc giá trị lúc in (biến nhập khi in, ngày in, số sê-ri):
  “Xem trước” hiện giá trị mẫu như trên tem (vd. LOT ‹Lô›-A) và dòng trạng thái nói giá trị được nhập
  hoặc lấy khi in; trước đây ô trống và khuyên “chọn một dòng Excel”. Đối tượng đọc cột vẫn chờ dòng dữ liệu.
- Trình đọc màn hình đọc đúng chữ người dùng thấy: các mục của ô “Nguồn dữ liệu” trước đây có tên
  tự động hóa “DataSourceChoice { Group = …, Label = Lô, … }”, các hàng của bảng Dữ liệu là tên lớp
  hoặc bản ghi, các menu dựng trong mã (▾ hộp công cụ, Chèn nguồn ▾, danh sách Hoàn tác/Làm lại)
  không có tên. Nay là “Lô (Biến nhập khi in)”, “Lô”, “Biến nhập khi in”… Tìm thấy khi kiểm thử
  trực tiếp bản cài v0.362 bằng UI Automation.

## v0.362 — Trang Kiểu của hình, menu chuột phải và bảng Dữ liệu gọn như NiceLabel 10

_Gồm các commit 31/10/2026 (22:01–22:45)._

- Bảng Dữ liệu: ô “Tìm…” và nút “+ Thêm ▾” cùng mép trên và cùng chiều cao (nút từng cao 45 DIP vì
  ký tự ▾, lệch khỏi ô tìm 32 DIP).
- Bảng Dữ liệu có menu chuột phải như Data explorer của NiceLabel 10: “Sửa trong Quản lý dữ liệu…”,
  “Thêm chữ gắn với nguồn này” và “Gắn vào “<đối tượng đang chọn>”” (hai kết quả của kéo thả, mỗi lệnh
  một bước hoàn tác; lệnh gắn chỉ bật với đối tượng hiện dữ liệu). Tem được bảo vệ chỉ còn “Sửa…”;
  hàng nhóm không có menu. Trước đây muốn gắn nguồn phải kéo thả.
- Bảng Dữ liệu: dòng gợi ý “Nhập Excel hoặc CSV để xem trước các dòng và gán trường cho đối tượng.”
  bị cắt ở mép bảng (“… để xem trước c”) vì nằm trong một StackPanel ngang; nay xuống dòng đủ. Hai
  cổng mới giữ mọi dòng chữ xuống dòng nằm gọn trong bảng Dữ liệu và trong mọi trang Thuộc tính
  của mọi loại đối tượng.
- Menu chuột phải trên tem có thêm “Dấu đối tượng” (Tên đối tượng, Nguồn dữ liệu, Đối tượng khóa) sau
  “Căn và lưới”, như menu trang của NiceLabel 10; trước đây chỉ bật/tắt được ở thẻ ribbon Xem. Menu trên
  đối tượng không có mục này.
- Thẻ ribbon Hình dạng khớp trang Kiểu: ô “Bo góc (mm)” chỉ hiện với hình chữ nhật, nhóm “Tô nền” không
  hiện với đường kẻ (như NiceLabel 10 ẩn bo góc ngoài hình chữ nhật).
- Trang Kiểu của hình như trang Style của NiceLabel 10: thẻ “Đường viền” (độ dày, kiểu nét và màu,
  “Bo góc (mm)” chỉ với hình chữ nhật) rồi thẻ “Tô nền” (không có với đường kẻ), cùng tên với các
  nút trên thẻ ribbon Hình dạng. Trước đây một thẻ “Kiểu hình” trộn tô, bo góc, viền, độ dày và cho cả
  hình bầu dục ô Bo góc vô nghĩa.

## v0.361 — Hộp công cụ, bảng Dữ liệu và Đối tượng, thẻ Nguồn như NiceLabel 10

_Gồm các commit 31/10/2026 (20:35–21:50)._

- Danh sách Đối tượng như Object browser của NiceLabel 10: chuột phải lên một đối tượng là chọn nó và
  có menu Thuộc tính, Cắt, Sao chép, Xóa, Hiển thị, Khóa vị trí, Sắp xếp. Đối tượng đang ẩn không vẽ
  trên tem nên chỉ với tới được ở đây: “Hiển thị” cho nó hiện lại (một bước hoàn tác); trước đây phải
  vào Thuộc tính › Chung.
- Thẻ Nguồn chỉ hiện thẻ “Chưa liên kết nguồn Excel / Liên kết Excel…” cho đối tượng đọc tệp dữ liệu
  (một cột, hoặc một hàm có thể đọc cột); chữ cố định, biến nhập khi in, ngày in và số sê-ri không cần
  tệp nên không còn thẻ đó (trước đây hiện cho mọi đối tượng, như thể đều phải có Excel). Nhập dữ liệu
  vẫn ở bảng Dữ liệu và thẻ Dữ liệu trên ribbon.
- Bảng Dữ liệu như Data explorer của NiceLabel 10: nhấp đúp một biến nhập khi in hoặc một hàm là mở
  Quản lý dữ liệu ngay trên mục đó; một cột mở trang nguồn dữ liệu, “Số sê-ri” mở bộ đếm, “Ngày in”
  mở trang ngày giờ in; hàng nhóm vẫn chỉ gập/mở. Trước đây nhấp đúp không làm gì.
- Nguồn ghép như “Combined data source” của NiceLabel 10: với “Ghép chữ / công thức…”, thẻ Nguồn có ô
  “Nội dung ghép” hiện đúng nội dung đối tượng đang in (vd. LOT {?Lô}-{MaHang}); sửa rồi Enter hoặc rời
  ô để áp dụng (một bước hoàn tác, chỉ cho đối tượng ô đang hiện), nút “Chèn nguồn ▾” chèn biến, cột,
  hàm hoặc giá trị khi in vào chỗ con trỏ, nội dung không còn nguồn nào thành chữ cố định. Trước đây
  nội dung ghép không hiện ở đâu trên thẻ Nguồn (chỉ có trình tạo công thức trống); trình tạo vẫn còn,
  thu gọn bên dưới.
- Bảng bên trái còn hai thẻ Dữ liệu · Đối tượng: bỏ thẻ “Lớp” vì ANLAbel không có lớp (thẻ đó chỉ
  liệt kê lại các đối tượng). Mỗi hàng của danh sách Đối tượng ghi đối tượng in từ nguồn nào, đúng
  tên như ô “Nguồn dữ liệu” (“Biến nhập khi in: Lô”, “Trường dữ liệu: MaHang”, “Khi in: Ngày in”,
  “Ghép nhiều nguồn”), chữ đỏ khi không in được (“Cần cột: …”); thẻ Lớp cũ ghi “Đã liên kết Excel”
  cả cho biến nhập khi in, hàm và số sê-ri.
- Hộp công cụ như NiceLabel 10: ô Chữ, Ô chữ và Mã vạch có nút ▾ liệt kê các nguồn của bảng Dữ liệu
  theo nhóm (biến nhập khi in, cột dữ liệu, hàm, giá trị khi in); chọn một nguồn là đặt đối tượng đã
  gắn sẵn nguồn đó (giữ mặc định của ô, một bước hoàn tác), thay vì đặt mã vạch với dữ liệu mẫu rồi
  mới gắn ở Thuộc tính.
- Thuộc tính của hình (đường kẻ, chữ nhật, bầu dục, đảo màu) không còn thẻ Nguồn, như NiceLabel 10:
  chọn hình là mở thẳng thẻ Kiểu (trước đây thẻ Nguồn chỉ ghi “không có nguồn”). Chữ định dạng (RTF)
  chỉ còn ô soạn trên thẻ Nguồn: bỏ ô “Nguồn dữ liệu” và trang Mặt nạ, vì chữ RTF in nội dung của
  chính nó (chọn một cột ở đó trước đây không có tác dụng khi in).
- Thuộc tính của hình ảnh như các trang Source và Graphic Resizing của NiceLabel 10: thẻ Nguồn có ảnh
  và chính sách ảnh điểm (cách ảnh thành điểm in); trang kiểu của ảnh nay tên “Đổi cỡ” với cỡ ảnh, lật
  và Về cỡ gốc. Trước đây nút “Chọn hình ảnh…” có ở cả hai thẻ và chính sách ảnh điểm nằm lẫn với cỡ
  ảnh.

## v0.360 — Thuộc tính sắp như NiceLabel 10 (một ô Nguồn dữ liệu; các trang Mã vạch, Chung, Vị trí); bảng Dữ liệu có ô tìm; mở tem hay nạp lại dữ liệu không còn đổi nguồn của đối tượng

_Gồm các commit 31/10/2026 (19:20–20:26)._

- Bảng Dữ liệu như Data explorer của NiceLabel 10: ô tìm (lọc biến nhập khi in, cột dữ liệu và hàm
  theo tên, trên danh sách riêng nên các bảng khác không bị lọc) và nút “+ Thêm ▾” (Biến nhập khi
  in…, Hàm…) mở Quản lý dữ liệu ngay ở mục mới, như “Add new variable”. Ô tìm của bảng Đối tượng
  cũng có chữ gợi ý “Tìm…”.
- Thuộc tính › Chung và Vị trí như các trang General và Position của NiceLabel 10: Chung nay có Tên
  (kèm lý do khi tên bị từ chối), Không in, Hiển thị và Điều kiện hiển thị (trước đây chỉ có trên
  thẻ Thiết kế của thanh lệnh); Vị trí có thêm Khóa vị trí và Giữ tỉ lệ. Các nút căn, phân bố và thứ
  tự lớp (đã có trên thanh lệnh) thu vào mục “Nâng cao” đóng sẵn ở cuối trang Chung.
- Thuộc tính của mã vạch: trang “Kiểu” nay tên “Mã vạch” và chia trang như NiceLabel 10: Mã vạch
  (loại mã, hồ sơ ứng dụng, cỡ X, DPI; chế độ 2D và mức sửa lỗi chỉ với mã 2D) · Số kiểm tra · Chữ
  dưới mã · Chi tiết (vùng yên, bộ mã Code 128 với Code 128, tỉ lệ và khe Code 39 với Code 39, bù
  loang vạch, vạch bao). Trước đây mọi thiết lập nằm trong một cột dài, kể cả chế độ 2D cho Code 128
  và tỉ lệ Code 39 cho mọi mã; mã 2D không có Số kiểm tra, Chữ dưới mã và vạch bao.
- Thuộc tính › Nguồn: một ô “Nguồn dữ liệu” như “Connected data source” của NiceLabel 10 thay
  cho hai bước “Nguồn” rồi “Trường Excel”: chữ cố định, biến nhập khi in, cột dữ liệu, hàm, giá trị
  khi in (số sê-ri, ngày in) và “Ghép chữ / công thức…”, theo nhóm như bảng Dữ liệu. Ô luôn hiện
  nguồn đối tượng đang in (trước đây đối tượng đọc hàm, biến nhập khi in hay ngày in hiện là “Trường
  Excel” với danh sách trống; cột mà tệp không có vẫn hiện, kèm ghi chú). Chọn một mục gắn đối tượng
  như khi kéo từ bảng Dữ liệu: một bước hoàn tác, giữ tên đối tượng; chọn chữ cố định thì giữ giá
  trị đang hiện. Thẻ kiểm tra liên kết Excel xuống cuối trang để nguồn đứng đầu; Trợ giúp ghi theo ô
  mới.
- Dữ liệu: ô “Trường Excel” của Thuộc tính (trang Nguồn) nay hiện đúng cột mà đối tượng đang chọn
  in, và chỉ đổi cột khi chọn ngay trong ô đó. Trước đây ô này đi theo lựa chọn chung của danh sách
  trường: đang chọn một mã vạch có liên kết mà nạp lại dữ liệu (lúc lựa chọn khác cột đầu), thêm hay
  xóa trường, hoặc chọn một trường trong cửa sổ Nhập Excel, thì mã vạch bị gắn sang cột đó và đổi
  tên, rồi in sai cột mà kiểm tra trước khi in không báo; ô cũng hiện một cột không phải cột đối
  tượng đang in. Mở một tem có liên kết (đối tượng trên cùng được chọn sẵn) cũng đổi tên đối tượng đó
  thành “Trường: <cột>” và đánh dấu tem là chưa lưu; nay mở tem không đổi gì.

## v0.359 — Tờ nhiều tem giữ đủ mép phải và mép dưới của từng tem

_Gồm các commit 31/10/2026 (17:38–18:48)._

- In: tờ nhiều tem có lề rộng hơn khe giữa các tem (ví dụ tờ A5 8 tem 68 x 48 mm, lề 5 mm, khe
  2 mm) mất mép phải và mép dưới của tem: nền trắng của mỗi tem được tô từ góc ô, trước lề, nên
  tem in sau che một dải (lề trừ khe) của tem in trước — khung mất đường dưới, dòng chữ cuối bị
  cắt nửa, cả trên giấy lẫn trong Xem trước. Nay mỗi ô chỉ giữ phần của tem mình (cả khi tem
  xoay theo hướng nạp giấy). Tìm ra khi in thử nhãn thùng và nhãn túi trên A5.

## v0.358 — Mở tem không liên kết không còn giữ dữ liệu tem trước; Không in và Khóa vị trí áp cho cả vùng chọn; Xem trước nêu chung các cột thiếu; khung lỗi tiếng Việt

_Gồm các commit 31/10/2026 (17:25–17:32)._

- Quản lý dữ liệu, Kết quả của hàm: chỉ tính hàm đang chọn và các hàm nó đọc, nên lỗi ở hàm khác
  không còn biến mọi kết quả thành “Lỗi”; khi tem chưa liên kết tệp dữ liệu, cột hàm cần hiện là
  ‹Tên cột› để thấy hình dạng kết quả (ví dụ ‹PO›|L01), hàm kiểm tra dữ liệu (ngày, chữ số) ghi
  “Chưa tính được: hàm cần cột …”, thay vì “Không tìm thấy trường”; tệp đã liên kết mà không đọc
  được vẫn báo thiếu cột; hàm đọc một hàm hỏng không báo thêm “không tìm thấy” cho hàm đó.
- Thiết kế: khi chọn nhiều đối tượng, “Không in”, “Hiển thị”, “Khóa vị trí” và “Giữ tỉ lệ” (thẻ
  Thiết kế và Thuộc tính) nay áp cho mọi đối tượng đang chọn, như trang thuộc tính của NiceLabel 10,
  mỗi lần là một bước hoàn tác; trước đây chỉ đối tượng chính đổi, nên tick “Không in” cho ba đối
  tượng vẫn in hai. Góc xoay vẫn chỉ cho đối tượng chính (nhóm xoay từng phần sẽ rời ra).
- Thiết kế: khi các đối tượng đang chọn khác nhau, ô “Không in”, “Hiển thị”, “Khóa vị trí” và “Giữ
  tỉ lệ” hiện dấu lưng chừng (như trang thuộc tính của NiceLabel 10 chỉ hiện giá trị khi mọi đối
  tượng chung giá trị đó), thay vì trạng thái của riêng đối tượng chính; bấm vào thì cả vùng chọn
  nhận giá trị. Mục “Khóa vị trí” trong menu chuột phải chỉ có dấu khi mọi đối tượng đang chọn đều khóa.
- Xem trước khi in: chữ trên khung báo lỗi của đối tượng (“Invalid barcode data”, “Text overflow”,
  các câu về dòng chữ dưới mã, “No image”) nay là tiếng Việt (“Dữ liệu mã không hợp lệ”, “Chữ tràn
  khung”…); lớp in giữ tiếng Anh cho nhật ký và kiểm thử, ứng dụng đưa chữ tiếng Việt vào khi khởi động.
- Xem trước khi in: vấn đề “thiếu cột” và “dấu ngoặc {} chưa có tên cột” không còn nói đối tượng
  “sẽ in trống” (dòng đó bị giữ lại, không in) và nêu việc cần làm: thêm cột vào tệp dữ liệu hoặc đổi
  nguồn dữ liệu của đối tượng; lỗi công thức lấy dữ liệu nêu luôn lỗi của công thức (“Không tìm thấy
  trường “PO”.”).
- Thiết kế: phím F2 mở bảng Thuộc tính của đối tượng đang chọn, như nhấp đúp (NiceLabel 10 mở
  trình sửa thuộc tính bằng F2); với chữ, con trỏ vào ô nội dung, chữ được chọn sẵn để gõ đè. Hướng
  dẫn, Phím tắt có dòng F2.
- Dữ liệu: mở một tem không liên kết tệp dữ liệu (tệp, bản phục hồi, mẫu thư viện) không còn giữ các
  dòng đã đọc cho tem mở trước đó; trước đây thiết kế điền trường của tem mới từ các dòng đó và “In
  mọi dòng” in tem mới một lần cho mỗi dòng của tệp kia. Nay như tạo tem mới: chưa có dòng nào cho tới
  khi liên kết tệp. Mở tem khác khi tem trước còn đang đọc tệp dữ liệu cũng không còn để dữ liệu và
  liên kết của tem trước đổ vào tem mới, và tem mở sau cùng là tem được giữ.
- Xem trước khi in: khi tệp dữ liệu thiếu nhiều cột tem dùng (chọn sai trang tính, dòng tiêu đề hay
  tệp), một dòng nêu chung các cột thiếu (“Tệp dữ liệu thiếu 3 trong 3 cột tem dùng (…)”) và, khi thiếu
  phần lớn, nhắc kiểm tra trang tính và dòng tiêu đề, thay vì chỉ một dòng “thiếu cột” cho mỗi đối tượng.

## v0.357 — Phím tắt thiết kế như NiceLabel 10; dán sang tem khác mang theo hàm và biến; hỏi trước khi xóa hàm hay biến đang dùng; dấu khóa trên đối tượng

_Gồm các commit 31/10/2026 (17:14–17:22)._

- Thiết kế: phím tắt như NiceLabel 10: Ctrl+A chọn mọi đối tượng, Ctrl+X cắt (nút Cắt vốn ghi
  Ctrl+X mà phím không làm gì), Ctrl+G và Ctrl+Shift+G nhóm và bỏ nhóm, Ctrl+B, Ctrl+I, Ctrl+U
  cho chữ đậm, nghiêng, gạch chân của mọi chữ đang chọn, PgUp/PgDn đưa lên, xuống một lớp
  (thêm Ctrl: lên trên cùng, xuống dưới cùng), Ctrl+F mở ô tìm của thẻ Đối tượng, hiện lại bảng
  bên trái nếu đang ẩn. Khi đang gõ trong một ô, các phím trừ Ctrl+F để ô đó dùng; không phím nào
  tác động khi màn hình Tệp đang mở hay đổi một tem đang khóa sửa; ribbon,
  menu chuột phải và trang Phím tắt của Hướng dẫn ghi các phím này; menu chuột phải trên vùng
  trống của tem có thêm Chọn tất cả.
- Thiết kế: Đưa lên một lớp, Đưa xuống một lớp, Lên trên cùng, Xuống dưới cùng (ribbon, menu
  chuột phải, PgUp/PgDn) nay áp cho mọi đối tượng đang chọn và giữ thứ tự giữa chúng, như
  NiceLabel 10; trước đây chỉ đối tượng mốc đổi lớp. Lên trên cùng, xuống dưới cùng chỉ đổi
  lớp của đối tượng được chuyển; hai đối tượng cùng giá trị lớp không còn làm lệnh như không
  có tác dụng.
- Thiết kế: dán sang tem khác, hay dán lại sau khi cắt, giữ nguyên vị trí và tên đối tượng, như
  NiceLabel 10; chỉ khi chỗ đó đã có bản gốc hay một bản sao, bản dán mới lệch thêm 3 mm tới chỗ
  trống đầu tiên và mang tên “(bản sao)”. Đối tượng dán mang theo hàm và biến nhập khi
  in mà nó đọc (kèm quy tắc nhập) nếu tem đích chưa có; tên tem đích đã có thì giữ định nghĩa của
  tem đích và thanh trạng thái báo khi hai định nghĩa khác nhau; hàm trùng tên một cột của tệp
  dữ liệu không được thêm (đối tượng đọc cột đó). Một lần Ctrl+Z bỏ cả đối tượng lẫn định nghĩa
  đã thêm.
- Quản lý dữ liệu: xóa một hàm hay biến nhập khi in đang được đối tượng hoặc hàm khác dùng nay
  hỏi trước, nêu tên các đối tượng, hàm đang dùng và hậu quả (thiếu trường; câu hỏi khi in mất
  quy tắc nhập), mặc định Giữ lại; NiceLabel 10 không cho xóa mục đang được dùng. Mục không ai
  dùng vẫn xóa ngay như cũ.
- Thiết kế: Tab và Shift+Tab chọn đối tượng kế tiếp hay trước đó theo thứ tự lớp (bỏ qua đối
  tượng đang ẩn, quay vòng; một nhóm là một bước và được chọn cả nhóm như khi bấm chuột), như mặt
  thiết kế của NiceLabel 10, để chọn được đối tượng nhỏ nằm dưới đối tượng khác; Ctrl với dấu
  cộng, dấu trừ phóng to, thu nhỏ và Ctrl+0 về 100 %. Ribbon, thanh trạng thái và trang Phím tắt
  ghi các phím này.
- Thiết kế: đối tượng đã khóa vị trí có dấu ổ khóa ở góc trên bên phải (Xem › Hiện dấu đối tượng ›
  Đối tượng khóa), như dấu “Locked object” của NiceLabel 10; bấm giữ đối tượng khóa đang chọn
  để kéo thì thanh trạng thái nói nó đã khóa và cách mở khóa, thay vì không có phản hồi.
- Quản lý dữ liệu: hàm đang chọn nay ghi nơi dùng nó như biến nhập khi in (“Dùng ở đối tượng “QR”;
  hàm “Tổng hợp”.”), kể cả khi đổi tên (các đối tượng chuyển sang tên mới khi bấm Đồng ý, cả đối
  tượng gọi hàm bằng tên gần đúng như {Mã_QR}, như khi in); biến nhập khi in ghi thêm các hàm đọc nó.
- Hướng dẫn, Phím tắt: các phím chia theo việc (Chọn và di chuyển, Bộ nhớ tạm và nhóm, Chữ và
  lớp, Vẽ, Xem), thêm Ctrl+Shift+C/V chép và dán định dạng, L, R, C vẽ đường kẻ, hình chữ nhật,
  hình elip; mọi phím Ctrl của cửa sổ chính đều có dòng trong trang này.
- Thiết kế: danh sách Đối tượng và cây đối tượng của thẻ Lớp ghi “· ẩn” (chữ mờ) cho đối tượng
  đang ẩn và “· khóa vị trí” cho đối tượng đã khóa; đối tượng ẩn không vẽ trên tem và không in,
  nên trước đây chỉ tìm được trong danh sách mà không biết nó đang ẩn.

## v0.356 — Tem NiceLabel chưa liên kết tệp chờ dữ liệu thay vì báo hỏng; dòng không có dữ liệu cho tem không in; nhắc dấu phân cách hàng nghìn trong mã vạch

_Gồm các commit 31/10/2026 (17:01–17:11)._

- Kiểm tra trước khi in: dữ liệu mã vạch tuyến tính trông như số có dấu phân cách hàng
  nghìn (“1,200”, thường do định dạng số kiểu phẩy của ô Excel) được nhắc, không chặn; máy
  quét đọc cả dấu phẩy, nơi NiceLabel in 1200. Mã 2D, số không dấu và “12,5” không bị nhắc.
- Thiết kế: ô Tên đối tượng trên ribbon không nhận tên đã có ở đối tượng khác (không phân
  biệt hoa thường) hay tên trống; tên cũ được giữ và thanh trạng thái nói lý do, như NiceLabel
  10 (“Another element with the same name already exists. The original name will be restored.”).
- Thiết kế: khi chọn nhiều chữ khác phông hoặc khác cỡ, ô Phông chữ và ô Cỡ chữ trên ribbon
  để trống thay vì hiện cỡ của một chữ như thể của cả nhóm; chọn một cỡ ở đó áp cho mọi chữ
  đang chọn, như các trang thuộc tính của NiceLabel 10.
- Dữ liệu: tem NiceLabel vừa nhập, chưa liên kết tệp Excel, không còn mở ra với khung đỏ
  “12 đối tượng bị hỏng…” và “thiếu trường trong bảng tính hiện tại” (khi chưa có bảng tính
  nào); Kiểm tra ràng buộc nay để màu trung tính, nêu các cột tệp Excel cần có và mỗi đối
  tượng ghi “Cần cột: …”. Tệp đã liên kết mà chưa đọc được ghi “Chưa đọc được tệp Excel đã
  liên kết”; lỗi thật, hoặc tệp đã liên kết mà thiếu cột, vẫn báo như cũ.
- Dữ liệu: công thức đọc cột chưa có (chưa liên kết tệp) không còn báo thêm “Không tìm thấy
  trường …” cho chính cột đã nêu là thiếu, nên tem NiceLabel có công thức cũng chờ tệp như
  trường thường; lỗi công thức thật vẫn báo. Mã Data Matrix quá nhỏ cho dữ liệu báo bằng tiếng
  Việt (“Dữ liệu cần 79 từ mã, quá sức chứa của khổ Data Matrix đã chọn…”), thay câu tiếng Anh
  của thư viện mã vạch.
- Xem trước khi in: khi in bỏ qua các dòng bị chặn, câu hỏi ghi các dòng thành khoảng (“dòng
  3–200”) thay vì liệt kê từng số; một sổ Excel của khách có 1 bản ghi và công thức kéo sẵn
  tới dòng 200 từng làm câu hỏi dài 198 số.
- Thuộc tính mã vạch: khi chưa có dòng dữ liệu, mã vạch có liên kết không còn bị kiểm tra
  bằng chữ giữ chỗ (“{Mã Viettel}” từng bị báo “ký tự không mã hóa được: ã”, chữ giữ chỗ dài
  từng bị báo “Dòng chữ dưới mã rộng…”); dòng chữ dưới mã quá rộng chỉ báo ở phần thiết lập mã,
  không còn mở đầu bằng “Dữ liệu không hợp lệ”. Dữ liệu thật vẫn được kiểm tra như cũ.
- Xem trước khi in: dòng không có dữ liệu cho tem (để trống mọi cột tem đọc, hoặc một đoạn từ
  3 dòng ở cuối bảng chỉ lặp một giá trị kéo xuống, ít nhất 2/3 cột để trống) không còn được
  chọn sẵn, có dòng nhắc nêu các dòng đó; “In mọi dòng” bỏ qua các dòng để trống và nói rõ.
  Sổ Excel của khách: 1 bản ghi và 198 dòng chỉ có công thức khối lượng nay chỉ chọn 1 tem và
  qua kiểm tra; dòng trùng nhau làm bản sao, đoạn 2 dòng, bản ghi để trống một cột và bảng
  thiếu cột của tem giữ nguyên như cũ.

## v0.355 — Hoàn tác có tên từng bước; cài đặt mã vạch cho mọi mã đang chọn; chèn RS/GS/EOT trong hàm; số Excel không còn khoảng trắng; nút In luôn trong tầm mắt

_Gồm các commit 31/10/2026 (16:44–16:55)._

- Phiên bản mẫu tem: danh sách phiên bản bỏ cột “Mã băm” (thông tin kỹ thuật cho hỗ trợ);
  mã băm của bản hiện tại và bản sao lưu vẫn có ở phần chi tiết bên dưới.
- Nhập tem NiceLabel: cột ngày của cơ sở dữ liệu giữ định dạng in đã đặt trong NiceLabel
  (ví dụ dd/MM/yyyy HH:mm:ss) thay vì dạng yyyy-MM-dd của ô ngày Excel; trước đây mã 2D
  ghép ngày sản xuất (kiểu AIAG “15D…”) và mã vạch in ngày đổi nội dung so với tem in
  bằng NiceLabel, hệ thống nhận có thể không đọc được.
- Đọc Excel: số định dạng kiểu dấu phẩy/kế toán (Comma Style, “_(* #,##0_)”) không còn
  mang khoảng trắng căn cột vào dữ liệu: 1200 đọc là “1,200” thay vì “  1,200 ”; trước đây
  khoảng trắng đó đi vào cả mã vạch và mã 2D (ví dụ “Q  1,200 ”). Dấu phân cách nghìn vẫn
  theo định dạng ô Excel.
- Thuộc tính: khi chọn nhiều đối tượng, tiêu đề bảng ghi rõ “Thuộc tính - Vendor [Chữ] ·
  3 đối tượng đang chọn”, vì đổi phông nay áp dụng cho mọi chữ đang chọn; trước đây tiêu
  đề chỉ nêu một đối tượng.
- Bảng Dữ liệu, “Kiểm tra ràng buộc”: tem chưa có đối tượng nào lấy dữ liệu Excel nay ghi
  “Chưa đối tượng nào lấy dữ liệu từ Excel; chưa có liên kết để kiểm tra.” trên nền trung
  tính, thay cho dòng xanh “Tất cả liên kết Excel đều khớp…” khi chẳng có gì để kiểm tra.
- Thiết kế: chọn nhiều mã vạch tuyến tính rồi đổi độ rộng vạch (X) hoặc chữ dưới mã (vị
  trí, phông, cỡ, đậm) thì mọi mã đang chọn cùng đổi, một lần Ctrl+Z hoàn tác, như trang
  thuộc tính mã vạch của NiceLabel 10; mã 2D trong vùng chọn giữ thiết lập riêng.
- Quản lý dữ liệu, Hàm: nút “Ký tự điều khiển ▾” chèn RS, GS, EOT hay xuống dòng vào công
  thức (chuỗi trong công thức nay đọc được \u001E, \u001D, \u0004…), để làm mã 2D theo
  ISO/IEC 15434 ([)> RS 06 GS … RS EOT) mà bàn phím không gõ được, như “Insert special
  character” của NiceLabel; kết quả xem trước ghi các ký tự đó bằng tên (‹RS›, ‹GS›).
- Hoàn tác có tên: mỗi bước mang tên (“Di chuyển “PO””, “Đổi phông chữ 3 đối tượng”,
  “Đổi khổ tem”); nút Hoàn tác và Làm lại ghi bước sẽ làm, thanh trạng thái ghi bước vừa
  làm, và mũi tên ▾ cạnh hai nút mở danh sách để hoàn tác hay làm lại nhiều bước một lần,
  như NiceLabel 10.
- Thiết kế: đối tượng thêm vào tem nhận một tên chưa dùng (“Chữ”, “Chữ 2”, “Chữ 3”, không
  phân biệt hoa thường), như NiceLabel 10 đánh số tên mục mới, để thông báo kiểm tra và
  bước hoàn tác chỉ đúng một đối tượng; tên sẵn có trong tem giữ nguyên.
- Xem trước khi in: nút “In” và “Đóng” luôn nằm ở chân khung Cài đặt in; trước đây chúng
  ở cuối phần cuộn, nên khi có câu trả lời nhập khi in, lưu ý ô gộp và danh sách cảnh báo
  người in phải cuộn xuống mới bấm được In.

## v0.354 — Menu chuột phải trên đối tượng và tem; đổi phông cho mọi chữ đang chọn; mã 2D không còn đầu ECI, GS1 Data Matrix có FNC1; nhắc khoảng trắng đầu cuối và ô gộp trong dữ liệu

_Gồm các commit 31/10/2026 (04:20–16:42)._

- Kiểm tra trước khi in: dữ liệu mã vạch có khoảng trắng ở đầu hoặc cuối (thường do
  xuất từ ERP/Excel, ví dụ “SP-0001 ”) được nhắc trong danh sách vấn đề, gộp thành
  một dòng nêu các dòng dữ liệu; máy quét đọc cả khoảng trắng nên tra cứu ở nơi
  nhận dễ lỗi. Chỉ nhắc, không chặn: tem in đúng như dữ liệu.
- Nhập Excel/CSV: tệp CSV hoặc sổ Excel chỉ có một trang tính không còn hỏi chọn
  trang tính (chỉ có một lựa chọn); vẫn hỏi khi tệp có nhiều trang.
- Thiết kế, chuột phải như NiceLabel 10: trên đối tượng có Thuộc tính, Cắt, Sao
  chép, Dán, Xóa, Khóa vị trí, Nhóm/Bỏ nhóm, Sắp xếp (4 lớp) và Căn chỉnh (6 kiểu,
  phân bố đều); chuột phải chọn luôn đối tượng dưới con trỏ. Trên vùng trống của tem
  có Thuộc tính tem, Dán, Vừa tài liệu, Vừa đối tượng. Dính, lưới và đường gióng nằm
  trong “Căn và lưới”. Trước đây chuột phải chỉ có dính, lưới và đường gióng.
- Xem trước khi in liệt kê giá trị đã nhập khi in (“Giá trị nhập khi in — Số lô:
  L2610 · Ngày sản xuất: 31/10/2026”) để soát cả giá trị chỉ nằm trong mã vạch/QR,
  và có nút “Sửa giá trị nhập…” nhập lại ngay trong cửa sổ xem trước (tem được làm
  lại, giữ các dòng đã chọn), như mẫu in của NiceLabel đặt câu hỏi cạnh tem xem
  trước. Trước đây muốn sửa phải đóng xem trước rồi in lại. In lại một lệnh cũ
  vẫn giữ giá trị của lệnh gốc.
- Nhập Excel: ô gộp (ví dụ một số lô gộp qua ba dòng) chỉ lưu giá trị ở ô đầu, nên
  các dòng sau đọc là trống và tem in thiếu giá trị mà không ai biết. Lúc nhập, thanh
  trạng thái nêu vùng gộp và các dòng; Xem trước khi in nhắc lại khi tem dùng cột đó
  (“Ô gộp B2:B4 (“Lot”) trong Excel chỉ có giá trị ở ô đầu: tem của dòng 2, 3 sẽ để
  trống “Lot”…”). Chỉ nhắc: không tự điền giá trị xuống các dòng gộp.
- Xem trước khi in: khi chỉ có cảnh báo, dòng trạng thái không còn khuyên “mở rộng
  mã vạch hoặc tăng DPI” cho mọi cảnh báo (cả nét kẻ mảnh hay khoảng trắng cuối dữ
  liệu); nay chỉ nói vẫn được in, mỗi cảnh báo bên dưới có lời khuyên riêng.
- Thiết kế: chọn nhiều chữ rồi đổi phông, cỡ, đậm, nghiêng, gạch chân, gạch ngang,
  màu hoặc căn lề thì mọi chữ và ô chữ đang chọn cùng đổi (một lần Ctrl+Z hoàn tác),
  như NiceLabel 10; trước đây chỉ đối tượng mốc đổi. Tăng/giảm cỡ chữ tăng từng chữ
  từ cỡ của nó. Mã vạch trong vùng chọn giữ phông riêng; ô chữ giữ khung.
- Mã 2D: Data Matrix, QR, PDF417 và Aztec không còn mang tiêu đề ECI 26 (UTF-8) khi
  dữ liệu chỉ có ký tự ASCII; trước đây mọi Data Matrix và mọi QR ở chế độ byte đều
  mang nó, và máy quét bật truyền ECI gửi kèm mã đó trước dữ liệu. GS1 DataMatrix nay
  bắt đầu bằng FNC1 nên máy quét nhận đúng là GS1 (]d2); trước đây thiếu FNC1, máy quét
  GS1 đọc như chữ thường. Dữ liệu có dấu tiếng Việt vẫn mã hóa UTF-8 kèm ECI.
- Lỗi của hàm (Quản lý dữ liệu) gọi đúng tên “hàm” như cửa sổ dùng, kèm việc cần làm:
  “Các hàm đọc lẫn nhau thành vòng: A, B; sửa công thức…”, “Công thức của hàm “Lô2”: …”,
  “Đã chặn in: lỗi ở hàm của Quản lý dữ liệu…”; trước đây gọi là “trường chuyển đổi”,
  một tên không có ở đâu trong giao diện.
- In: đối tượng có điều kiện hiển thị đọc một cột không còn trong dữ liệu (cột đổi tên
  hoặc bị xóa trong Excel) nay chặn in và nêu tên cột; trước đây điều kiện đọc ra trống
  nên đối tượng (ví dụ chữ “HÀNG DỄ VỠ”) bị bỏ khỏi mọi tem mà không có thông báo.
  Điều kiện theo giá trị nhập khi in không bị ảnh hưởng.

## v0.353 — Dòng ẩn trong Excel không in mặc định; nhắc lưu Excel trước khi in; hàm đọc được số sê-ri; bảng Dữ liệu có hàm và giá trị khi in

_Gồm các commit 31/10/2026 (03:43–04:10)._

- Thiết kế, kiểm tra liên kết: khi tem đã liên kết Excel, công thức đọc biến
  nhập khi in (FIELD("?Tên")), ngày in (FIELD("#NOW:…")) hay số sê-ri
  ({#SERIAL}) không còn bị liệt kê là liên kết hỏng ("Liên kết có lỗi xác thực",
  "thiếu trường #SERIAL"); xem trước công thức hiện giá trị mẫu. {#SERIAL} vẫn
  được báo khi chưa bật bộ đếm.
- Quản lý dữ liệu, Hàm: hàm đọc số sê-ri (FIELD("#SERIAL")) nay nhận đúng số
  của từng tem khi in, hiện số kế tiếp khi thiết kế và số bắt đầu trong cửa sổ
  Hàm; trước đây phần đó in ra trống và bị chặn in vì thiếu trường.
- Bảng Dữ liệu (bên trái) liệt kê thêm các hàm của Quản lý dữ liệu và mục "Khi
  in" (số sê-ri khi đã bật bộ đếm, ngày in), như bảng dữ liệu của NiceLabel;
  kéo một mục lên tem để thêm chữ gắn với nó, không cần nhớ cú pháp {Tên},
  {#SERIAL} hay {#NOW:dd/MM/yyyy}. Chữ kéo vào được đặt tên dễ đọc ("Ngày in",
  "Số sê-ri") thay vì "#NOW:dd/MM/yyyy"; cửa sổ Hàm nhắc FIELD đọc được ngày
  in và số sê-ri.
- Nhập CSV: tệp Excel lưu kiểu “CSV (Comma delimited)” trên Windows tiếng Việt
  (bảng mã Windows-1258, không phải UTF-8) vẫn bị từ chối để không in sai dấu,
  nhưng thông báo nay nói rõ nguyên nhân và cách làm: nhập thẳng tệp .xlsx, hoặc
  lưu lại kiểu “CSV UTF-8 (Comma delimited)”.
- Dữ liệu Excel: khi tệp đã liên kết đang mở trong Excel, thẻ Dữ liệu hiện tại và
  Xem trước khi in nhắc “Tệp đang mở trong Excel: nếu vừa sửa số liệu, hãy Lưu
  trong Excel rồi bấm Cập nhật Excel trước khi in”. ANLAbel đọc tệp đã lưu, nên
  số liệu vừa gõ mà chưa lưu sẽ không được in; trước đây không có dấu hiệu nào.
  Chỉ nhắc, không chặn in; kiểm tra lại mỗi khi quay về cửa sổ ANLAbel.
- Dữ liệu Excel: dòng đang ẩn trong Excel (bộ lọc AutoFilter loại ra, hoặc bị Ẩn)
  không còn được in mặc định. Xem trước khi in để các dòng đó chưa chọn và ghi rõ
  “2 dòng đang ẩn trong Excel (bộ lọc) chưa được chọn in: dòng 2, 4”; “In mọi dòng”
  bỏ qua chúng và nêu tên trong dòng trạng thái. Trước đây lọc trong Excel còn 3
  đơn hàng vẫn in ra 5 tem. Dữ liệu vẫn được đọc đủ; đánh dấu lại một dòng ẩn thì
  dòng đó được in.

**Kiểm tra:** build 0 cảnh báo, 1713 unit, hồi quy 322/322 (vòng kiểm tra Fast).

## v0.352 — Mở tem NiceLabel đúng hơn (mã HIBC, câu hỏi không có sẵn giá trị giả); đổi tên biến và hàm giữ liên kết; hàm đọc được giá trị nhập khi in

_Gồm các commit 31/10/2026 (02:21–03:26)._

- Mở tệp .nlbl: thông báo nêu tên các đối tượng chưa chuyển đổi được ("chuyển đổi
  14/15 đối tượng. Còn 1 đối tượng chưa chuyển đổi: Logo") và nhắc tem in sẽ thiếu
  chúng. Trước đây con số được tính bằng phép trừ nên có thể báo "toàn bộ" khi
  một chữ đảo màu (thêm một vùng đảo màu) che mất một đối tượng bị bỏ.
- Mở tệp .nlbl: câu hỏi khi in mà NiceLabel không có giá trị mặc định nay để
  trống như NiceLabel. Trước đây ô được điền sẵn ký tự giữ chỗ NiceLabel lưu
  trong đối tượng ("??????????", "000000", "123456789012", "Text"), người in
  bấm Enter là in ra dữ liệu giả (310 câu hỏi trong các tem mẫu; 12 tem của
  khách không bị ảnh hưởng).
- Mở tệp .nlbl: mã HIBC (nhãn thiết bị y tế) được chuyển đổi: phần chính,
  phần phụ (lô, hạn dùng, số sê-ri) và dạng ghép, kèm ký tự kiểm tra mod 43 như
  NiceLabel. Công thức có hàm mới HIBCCHECK(...) để tự làm mã HIBC.
- Mở tệp .nlbl: mã vạch gắn dữ liệu mà NiceLabel lưu dãy "000…" giữ chỗ nay lấy
  khung theo giá trị lúc thiết kế; trước đây khung tính theo dãy số 0 nên mã căn
  giữa bị báo "ra ngoài tem" và chặn in (mẫu UDI-HIBC: khung 54,6 mm vượt mép tem
  100 mm cho mã thực 44 mm).
- Mở tệp .nlbl: kịch bản một dòng có dòng chú thích (Rem …) nay được đọc, nên
  Data Matrix của tem vật tư MAT (ngày sản xuất 6D = ngày + "094") được chuyển
  đổi thay vì bị bỏ.
- Quản lý dữ liệu: đổi tên một biến nhập khi in nay đổi theo ở mọi đối tượng đang
  dùng nó ({?Tên}, FIELD("?Tên") trong công thức, điều kiện hiển thị, hàm), như
  NiceLabel. Trước đây đối tượng giữ tên cũ: khi in, hộp nhập hỏi tên cũ mà không
  còn quy tắc nhập (chỉ số, độ dài tối đa, bắt buộc), và câu "sẽ báo lỗi liên kết"
  trong cửa sổ là sai vì không có lỗi nào được báo. Trước khi bấm Đồng ý, cửa sổ
  nêu đối tượng nào sẽ chuyển sang tên mới; Ctrl+Z hoàn tác cả hai.
- Quản lý dữ liệu, Hàm: hàm đọc biến nhập khi in (FIELD("?Tên")) hoặc ngày in
  (FIELD("#NOW:…")) nay chạy được, như cửa sổ vẫn hướng dẫn và như NiceLabel:
  khi in, hộp nhập hỏi biến mà hàm đọc, rồi hàm được tính với giá trị vừa nhập;
  cửa sổ Hàm hiện kết quả với giá trị mẫu thay vì báo lỗi. Trước đây hàm luôn
  được tính trước khi có giá trị nhập: biến không được hỏi, phần đó của tem in
  ra trống (có dữ liệu Excel thì bị chặn in vì "lỗi chuyển đổi dữ liệu"), và
  màn hình thiết kế báo nhầm thiếu cột "?Tên".
- Quản lý dữ liệu, Hàm: đổi tên một hàm cũng đổi theo ở đối tượng ({Tên}) và ở
  các hàm khác đọc nó (FIELD("Tên")), như NiceLabel. Trước đây đối tượng còn
  dùng tên cũ nên bị báo thiếu trường và chặn in.

**Kiểm tra:** build 0 cảnh báo, 1705 unit, hồi quy 318/318 (vòng kiểm tra Fast).

## v0.351 — Quét mã chọn đúng dòng cần in; Enter sang câu hỏi kế tiếp

_Gồm các commit 31/10/2026 (02:14–02:15)._

- Xem trước khi in, ô lọc dòng: quét hoặc gõ đúng một mã (ví dụ "SP-0001") nay
  chỉ chọn dòng có ô đúng bằng mã đó; trước đây lọc theo "có chứa" nên chọn cả
  SP-00010…SP-00019 để in. Khi không ô nào khớp đúng, lọc vẫn theo "có chứa".
  Dòng đếm ghi rõ "Đúng …" hay "Có chứa …".
- Hộp nhập khi in: phím Enter chuyển sang câu hỏi kế tiếp, Enter ở câu cuối mới
  xác nhận. Máy quét gửi Enter sau mỗi lần quét nên trước đây lần quét đầu đã gửi
  cả hộp thoại kèm giá trị cũ của các ô còn lại; danh sách chọn đang mở vẫn nhận
  Enter để chốt mục.

**Kiểm tra:** build 0 cảnh báo, 1681 unit, hồi quy 315/315 (vòng kiểm tra Fast).

## v0.350 — Giá trị quét quá dài bị trả lại thay vì bị cắt; Xem trước khi in gọn hơn và nêu tên cột

_Gồm các commit 31/10/2026 (02:02–02:07)._

- Xem trước khi in (soát thiết kế): các giá trị kỹ thuật của kế hoạch in (DPI,
  độ lệch, vùng in đã xác thực) nằm trong mục "Chi tiết kỹ thuật" thu gọn; hai câu
  ghi chú về cửa sổ Thiết lập tem gộp thành chú thích của nút cài đặt; nút "In"
  nổi bật như nút chính.
- Hộp nhập khi in: giá trị dài hơn số ký tự tối đa (ví dụ máy quét gửi 9 ký tự vào
  ô số lô tối đa 8) nay bị trả lại bằng tiếng Việt; trước đây ô chữ cắt bớt ký tự
  thừa nên tem in giá trị bị cắt mà không báo.
- Kịch bản thiết kế mới (kiểm thử hồi quy): hộp thuốc mang GS1 DataMatrix với
  GTIN và số lô từ Excel, hạn dùng từ biến ngày, số serial từ bộ đếm; mỗi hộp
  giải đúng, có dấu phân cách GS sau số lô và serial tăng dần.
- Xem trước khi in: rê chuột lên một dòng dữ liệu hiện tên từng cột kèm giá trị
  ("Ma: SP-0001", "Ten: …"), vì danh sách chỉ hiện giá trị.

**Kiểm tra:** build 0 cảnh báo, 1681 unit, hồi quy 314/314 (vòng kiểm tra Fast).

## v0.349 — Ngày gõ sai khi in bị trả lại thay vì lặng lẽ in ngày cũ; trình soạn biến nhập gọn và rõ hơn

_Gồm các commit 31/10/2026 (01:46–01:54)._

- Quản lý dữ liệu, trình soạn biến nhập (soát thiết kế): hai nhóm "Quy tắc
  nhập" và "Khi in"; số ký tự tối thiểu và tối đa trên một hàng; ô tiền tố và
  hậu tố có nhãn riêng; ô ký tự đệm chỉ bật khi có đệm. Bấm Đồng ý với số ký tự
  tối thiểu lớn hơn tối đa bị từ chối (người in sẽ không nhập được giá trị nào).
- Hộp nhập khi in, biến kiểu ngày: gõ sai ngày ("31/13/2026", "ngày mai") nay bị
  trả lại bằng tiếng Việt và chữ gõ sai hiện lại trong ô. Trước đây lịch tự quay
  về ngày hợp lệ trước đó khi rời ô, nên lệnh in lặng lẽ lấy ngày cũ (in sai hạn
  dùng). Ngày gõ theo định dạng nhập của biến (ví dụ yyMMdd) được nhận và hiện
  lên lịch; chữ ngày nằm giữa ô như các ô khác.

**Kiểm tra:** build 0 cảnh báo, 1681 unit, hồi quy 312/312 (vòng kiểm tra Fast).

## v0.348 — Số EAN/UPC in đủ, nhập NiceLabel giữ bộ đếm serial, chữ định dạng, nhóm và dấu cách; biến nhập có độ dài tối thiểu

_Gồm các commit 31/10/2026 (00:37–01:36)._

- Nhập .nlbl giữ nhóm đối tượng của NiceLabel (GroupedItems): các đối tượng
  cùng nhóm nhận chung một nhóm ANLAbel nên chọn và di chuyển cùng nhau.
- Mã EAN-13, EAN-8, UPC-A, UPC-E: dòng số dưới mã luôn in đủ số máy quét đọc,
  kể cả số kiểm tra phần mềm tự tính (trước đây nhập 12 số thì chỉ in 12 số).
  UPC-E nhập 6 số được hiểu là hệ số 0 như NiceLabel 10 (trước đây bị chặn in
  với lỗi kỹ thuật của bộ mã hóa); UPC-E 7–8 số phải bắt đầu bằng 0 hoặc 1 và có
  số kiểm tra đúng. Lỗi dữ liệu EAN/UPC được báo bằng tiếng Việt.
- Nhập .nlbl: hàm ghép chuỗi (Concatenate) giữ dấu cách phân cách và phần cố
  định chỉ là dấu cách (trước đây "8 PC" in thành "8PC"); Text gắn biến có giá
  trị mặc định dạng tham chiếu ("[id] / [id] KG") đo khung theo giá trị mẫu
  ("647 / 491 KG"), không theo chuỗi mã id; Text gắn trường chưa có giá trị
  (NiceLabel lưu "?" lặp theo độ dài trường, ví dụ 255 ký tự) được thu khung
  từ điểm neo cho nằm trong tem, nên không còn bị chặn in vì "nằm ngoài tem".
- Nhập .nlbl: chữ định dạng cố định (Rich Text) của NiceLabel thành Ô chữ định
  dạng (RichTextBox) giữ nguyên phông, cỡ, đậm nghiêng như bản gốc (trước đây
  thành ô chữ thường mất định dạng và thường bị chặn in vì tràn khung); chữ
  định dạng có biến vẫn là ô chữ để giữ dữ liệu.
- Nhập .nlbl: GS1-128 tự tính số kiểm tra làm như NiceLabel, tính trên (độ dài
  AI − 1) chữ số đầu: GTIN 14 số được tính lại chữ số cuối thay vì nối thành 15
  số và bị chặn in (8 mã mẫu NiceLabel nay đọc được).
- Nhập .nlbl: biến đếm của NiceLabel (tăng/giảm) thành bộ đếm số serial của
  tem: số bắt đầu, bước, số tem cùng một số, giới hạn, quay vòng, tiền tố, hậu
  tố và số chữ số (giữ số 0 đầu) theo bản gốc. Trước đây nó thành biến nhập nên
  mọi tem trong một lệnh in cùng một số. Bộ đếm ANLAbel giữ chỗ số cho từng lệnh
  nên không trùng số; bộ đếm NiceLabel hỏi số bắt đầu khi in thì nay tự nối tiếp
  số đã in; quy tắc đặt lại theo ngày hoặc theo biến khác không chuyển được.
- Nhập .nlbl: hộp nhập khi in hỏi theo thứ tự câu hỏi NiceLabel lưu
  (PromptOrder), không theo thứ tự đối tượng trên tem.
- Biến nhập khi in có quy tắc "Số ký tự tối thiểu" (NiceLabel 10 Minimum
  length): hộp nhập ghi "từ N đến M ký tự" và trả lại giá trị quét hoặc gõ thiếu
  bằng tiếng Việt; đặt trong Quản lý dữ liệu, nhập được từ .nlbl.
- Kịch bản thiết kế mới (kiểm thử hồi quy): mã bán lẻ in đúng số máy quét đọc;
  tem serial nhập từ NiceLabel đánh số mọi tem, lệnh sau nối tiếp; số serial
  trên tờ nhiều tem đi đúng thứ tự xếp tem; tem thùng SSCC tự đánh số với số
  kiểm tra hợp lệ.

**Kiểm tra:** build 0 cảnh báo, 1681 unit, hồi quy 312/312 (vòng kiểm tra Fast).

## v0.347 — Biến nhập kiểu ngày, kéo thả trường lên tem, hàm ngày DATEADD/DATEFORMAT, nhập .nlbl thêm đầy đủ

_Gồm các commit 30–31/10/2026 (23:50–00:30)._

- Biến nhập kiểu ngày theo NiceLabel 10 (Date variable): Quản lý dữ liệu › Biến
  nhập khi in có “Kiểu giá trị: Chữ / Ngày”; biến ngày chỉ hiện định dạng ngày
  khi in (dd/MM/yyyy, yyMMdd, yyyyMMdd…) và tiền tố/hậu tố, các quy tắc chữ ẩn
  đi. Khi in,
  người in chọn ngày trên lịch (thứ tự ngày/tháng/năm) hoặc gõ theo định dạng nhập
  của biến; dòng “Sẽ in” cho thấy giá trị in ra; lệnh in giữ ngày dạng yyyy-MM-dd
  nên in lại vẫn đúng. Chưa có ngày thì bảng thiết kế hiện ngày hôm nay theo định
  dạng in. Nhập .nlbl giữ định dạng nhập/in và ngày mặc định của biến ngày (190
  biến trong bộ mẫu); quét in 207 tem mẫu đọc được 493/525 mã (trước 463) vì mã
  GS1 chứa ngày giờ mang ngày thật.
- Kéo thả từ ô Dữ liệu như NiceLabel 10: kéo một biến nhập hoặc trường dữ liệu
  lên tem thì thêm chữ gắn với nó, góc trên trái tại điểm thả; thả lên chữ, ô
  chữ, mã vạch, QR hoặc Data Matrix thì gắn đối tượng đó; giữ Ctrl để luôn thêm
  chữ mới. Mỗi lần thả là một bước hoàn tác; tem đang bảo vệ không nhận thả.
- Nhập .nlbl giữ “Khóa vị trí” (PositionLocked) và chữ đảo màu (InverseText:
  chữ trắng trên nền đen) bằng một vùng Đảo màu đúng khung chữ, đặt ngay trên chữ.
- Hàm ngày trong công thức: DATEADD(ngày, số, đơn vị[, định dạng[, định dạng
  đọc]]) cộng ngày (d), tuần (w), tháng (mo), năm (y), ví dụ hạn dùng = ngày sản
  xuất trong Excel + 24 tháng; DATEFORMAT(ngày, định dạng[, định dạng đọc]) đổi
  cách ghi ngày, ví dụ ngày Excel thành yyMMdd cho mã GS1 (17). Ngày đọc được ở
  dạng yyyy-MM-dd (ô ngày Excel), dd/MM/yyyy, dd.MM.yyyy, yyyyMMdd hoặc theo
  định dạng nêu ra; ngày trống cho giá trị trống; lỗi báo bằng tiếng Việt.
- Nhập .nlbl: hàm cộng ngày của NiceLabel có số ngày/tháng/năm lấy từ nguồn khác
  (hạn dùng = ngày sản xuất + “Expiry (days)”) thành công thức DATEADD trên ngày
  in; biến số ngày thành biến nhập khi in.
- Kịch bản thiết kế mới (kiểm thử hồi quy): tem bảng nhập từ NiceLabel in đúng
  mọi dòng Excel; dòng dữ liệu chọn biến thể in (QR hoặc Code 128); tem thùng
  GS1 lấy hạn dùng từ biến ngày; hạn dùng tính từ ngày sản xuất trong Excel.

**Kiểm tra:** build 0 cảnh báo, 1645 unit, hồi quy 308/308 (vòng kiểm tra Fast).

## v0.346 — Trình tạo tem mới, bảo vệ tem, quy tắc biến nhập, hàm chữ và điều kiện; tem .nlbl in như NiceLabel

_Gồm các commit 30/10/2026 (22:01–23:27)._

- Tạo tem mới theo trình tạo tem của NiceLabel 10 (New Label Setup Wizard):
  hộp Tạo mẫu tem mới có ô “Tiếp theo: chọn máy in và giấy” (bật sẵn); Thuộc
  tính tem mở ngay theo từng bước Máy in › Kích thước tem › Giấy với Quay lại /
  Tiếp, bước cuối là Hoàn tất. Không có bước kho tem có sẵn (quyết định của chủ).
- Bảo vệ tem khỏi chỉnh sửa theo NiceLabel 10 (Info › Protection): Tệp › Bảo vệ
  bật bảo vệ, có thể kèm mật khẩu (lưu dạng băm PBKDF2, tệp không chứa mật khẩu).
  Tem được bảo vệ mở ra ở chế độ chỉ in: khóa bảng thiết kế, hộp công cụ, Lớp,
  Đối tượng, Thuộc tính, các nhóm sửa trên ribbon, tab ngữ cảnh, hoàn tác và phím
  tắt vẽ; vẫn chọn dữ liệu, máy in và in được. Dải báo trên bảng thiết kế có Mở
  khóa (mật khẩu nếu có) cho phiên làm việc; mở lại tệp thì khóa lại. Chống sửa
  nhầm khi vận hành, không phải mã hóa tệp.
- Quy tắc biến nhập theo NiceLabel 10 (Input/Output rules): danh sách chọn (người
  in chọn trong danh sách), độ dài cố định, chỉ nhận các ký tự, tiền tố, hậu tố,
  đệm trái/phải bằng một ký tự đến số ký tự tối đa. Hộp nhập khi in hiện danh
  sách thả xuống, gợi ý quy tắc và dòng “Sẽ in: …”; sai quy tắc bị trả lại bằng
  tiếng Việt. Lệnh in lưu câu trả lời gốc, quy tắc áp khi in nên in lại vẫn đúng.
  Nhập .nlbl giữ các quy tắc này (PickListContents, Prefix/Suffix, Padding,
  FixedLength, AllowedCharactersForCustomFormat).
- Hàm công thức đổi và cắt chữ: UPPER(x), LOWER(x), TRIM(x), LEFT(x, n),
  RIGHT(x, n) (cùng MID có sẵn); có trong Trình tạo công thức và gợi ý của Quản
  lý dữ liệu, lỗi đối số báo bằng tiếng Việt. Nhập .nlbl dịch script NiceLabel
  gọi UCase, LCase, Trim, Left, Right, Mid trong phép nối thành công thức cùng
  nghĩa: 62/68 script trong bộ mẫu (trước là 52), 6.351 đối tượng chuyển được.
- Chữ dưới mã vạch theo NiceLabel 10: chọn phông và chữ đậm (Mã vạch › Chi tiết
  mã, Thuộc tính › Phông HRI, Đậm); mã vạch mới vẫn Segoe UI thường như cũ.
  Nhập .nlbl: chữ dưới mã in Arial đậm (hoặc phông tem lưu), cỡ tự co như
  NiceLabel (cao 8 vạch hẹp, tối thiểu 5 pt), nằm ngoài chiều cao vạch: khung
  mã nới thêm dải chữ nên vạch giữ đúng chiều cao NiceLabel in. Trước đây chữ
  10 pt bị nhét vào khung: 23/30 mã có chữ trong các tem khách trên máy bị chặn
  in (“khung quá thấp”), số còn lại vạch chỉ cao 1–3 mm. Bảng thiết kế hiện
  chữ dưới mã đúng cỡ in (trước nhỏ hơn 25%).
- Nhập .nlbl theo thứ tự vẽ và điều kiện của NiceLabel 10: đối tượng chồng lớp
  theo ZOrder (157/207 tem mẫu có thứ tự trong tệp khác thứ tự vẽ), giữ “Không
  in” (IsPrintable). Điều kiện hiển thị của đối tượng và của lớp (Thiết kế ›
  Lớp: mỗi biến thể chỉ in khi một biến bằng một giá trị) thành Điều kiện hiển
  thị của ANLAbel với hàm mới EQUALS (so sánh không phân biệt hoa thường như
  NiceLabel), NOT, AND. Biến nhập chỉ dùng trong điều kiện hiển thị nay cũng
  được hỏi khi in. Mọi hàm công thức đều mở đầu được một công thức (trước chỉ
  FIELD, CONCAT, MID, GS1CHECKDIGIT nên UPPER(…) đứng đầu bị in nguyên văn).
- Sửa nhập .nlbl: hình chữ nhật NiceLabel không tô (None/Clear) giữ phần trong
  trống. Trước đây hình nhập vào nhận kiểu mặc định của ANLAbel là tô trắng
  đặc, nên che mọi thứ nằm dưới: trên tem dạng bảng của khách, khung bao vẽ
  sau che hết nhãn cột trái và tên công ty.
- Nhập .nlbl: ô chữ gắn trường Excel lấy khung theo giá trị mẫu NiceLabel lưu
  cho trường (cũng là giá trị NiceLabel hiện khi thiết kế), kể cả ô ghép chữ
  cố định với trường (“Po: …”). Trước đây khung đo theo chữ giữ chỗ của ô
  (“Text”, “Po:”) nên khi in giá trị thật bị nén vào bề rộng của chữ đó.

**Kiểm tra:** build 0 cảnh báo, 1625 unit, hồi quy 302/302 (vòng kiểm tra Fast).

## v0.345 — Tờ nhiều tem xem trước cả tờ, ảnh và Data Matrix theo NiceLabel 10, nhập .nlbl in đúng như thiết kế

_Gồm các commit 30/10/2026 (19:53–21:51)._

- In tờ nhiều tem theo NiceLabel 10: Xem trước khi in hiện cả tờ chứa tem
  đang xem (tờ mấy, ô mấy) và có “Bỏ qua N ô đầu của tờ đầu” để in tiếp tờ
  đã dùng dở; lệnh in gửi đúng số ô bỏ qua.
- Kịch bản thiết kế/in nhiều trường hợp: tờ A4 3 × 8 và 2 × 5, A5 2 × 2 xoay
  180°, mọi thứ tự in, ô bỏ qua, trang cuối thiếu; mã xoay 0/90/180/270°. Mọi
  ô được giải mã trên trang in và trên ảnh xem trước.
- Ảnh theo NiceLabel 10: Cỡ ảnh (Co giãn theo tỉ lệ / Kéo đầy khung / Giữ cỡ
  gốc theo điểm ảnh và độ phân giải, cắt ở khung), Lật ngang, Lật dọc, Về cỡ
  gốc; thiết kế, xem trước, in và kiểm tra mật độ ảnh dùng chung một phép đặt
  ảnh. Nhập .nlbl giữ chế độ kéo đầy khung và hai kiểu lật.
- Thuộc tính › Nguồn của ảnh: thẻ Nguồn ảnh (ảnh nhúng trong tem, số điểm
  ảnh, dpi, cỡ gốc, Chọn hình ảnh…) thay cho ô “Nguồn: Chữ” không dùng được.
- Data Matrix theo NiceLabel 10: “Tự động” là khổ vuông nhỏ nhất (trước đây
  bộ mã hóa có thể chọn chữ nhật: 8 chữ cái in thành dải 19 × 4,7 mm trong khung
  20 × 20); Khổ Data Matrix chọn 24 khổ vuông hoặc 6 khổ chữ nhật, in đúng khổ,
  dữ liệu dài quá khổ thì chặn dòng đó với lý do rõ ràng; khung theo hình khổ chữ
  nhật; nhập .nlbl giữ khổ (DatamatrixFormat).
- Nhập .nlbl: script NiceLabel chỉ nối chuỗi (`Result = "UN" & SupplierID &
  [Package Number]`, 52/68 script mẫu: biển số kiện GTL, VDA 4902, GM1724) được
  dịch thành mẫu ghép trường; bộ mẫu NiceLabel chuyển được 98,1% đối tượng (trước
  96,4%). Script khác (UCase, Left…) vẫn báo chưa chuyển.
- Nhập .nlbl in ra đúng như thiết kế: in thử mọi tem mẫu NiceLabel bằng bộ vẽ
  của ANLAbel và quét lại từng mã (370 → 465 / 527 mã đọc được). Khung mã vạch
  tuyến tính đo bằng bộ mã hóa thật (mã GS1-128 neo giữa không còn lệch ra
  ngoài mép trái); mã có liên kết mà nội dung lưu là ký tự giữ chỗ thì đo theo
  giá trị thiết kế rồi đặt lại theo điểm neo; QR/Data Matrix tự động có liên
  kết không còn bị ép vào khung mặc định 20 × 8 mm; biến chỉ “soi” trường CSDL
  (Products.ProdCode) liên kết thẳng trường đó thay vì in “[id]”.
- Mã vạch cỡ theo X lớn ra quanh điểm neo như NiceLabel 10: dữ liệu dòng dài
  hoặc ngắn hơn thì mã neo giữa vẫn giữ tâm, neo phải giữ mép phải (in, thiết kế,
  kiểm tra tràn mép). Chọn một mã có liên kết không còn co khung về độ rộng của
  “0” (60 mm còn 19 mm chỉ vì một cú nhấp): khung theo dòng xem trước, giữ điểm
  neo; dòng chưa đủ dữ liệu thì giữ nguyên khung.
- Đầu bảng Thuộc tính (tên | loại | rộng × cao) cập nhật khi đổi tên, kéo hay
  gõ kích thước đối tượng đang chọn; trước đây chỉ đổi khi chọn đối tượng khác.

**Kiểm tra:** build 0 cảnh báo, 1578 unit, hồi quy 297/297.

## v0.344 — Icon vector mọi cửa sổ, khổ giấy và chọn giấy theo NiceLabel 10, tem A5 đủ 15 loại mã với Excel

_Gồm các commit 30/10/2026 (19:13–19:46)._

- Sửa icon lỗi: mọi cửa sổ dùng một bộ icon vector (thêm 38 icon 16 px).
  Bỏ ảnh PNG cũ (mờ ở 125–150 %) và emoji (WPF vẽ thành nét mờ hoặc ô
  trống) ở bảng Dữ liệu, đầu bảng Thuộc tính, Xem trước khi in (ô tìm, dấu
  đã in), Cập nhật, Thư viện mẫu và Hướng dẫn (danh mục, ghi chú, hình
  ribbon/hộp công cụ dùng icon thật; mục đang chọn không còn bị mờ chữ).
- Khổ giấy và chọn giấy theo NiceLabel 10 (Thuộc tính tem): Kích thước tem
  có Lề (trái/trên nhập, phải/dưới tự tính), Số tem trên trang (ngang/dọc,
  khoảng cách, thứ tự in, tính số tem vừa tờ); trang Giấy có loại giấy, khổ
  giấy của máy in, Xoay 180° và hình xem trước. Áp dụng chặn lưới tràn tờ.
- In tem trên tờ lớn hơn tem (ví dụ tem trên A5/A4): mỗi trang gửi máy in
  là cả tờ, tem đặt đúng lề; trước đây trang chỉ bằng tem nên phần bị lề
  đẩy ra có thể mất. Xoay 180° xoay cả trang.
- Sửa Plessey: ZXing.Net vẽ Plessey theo 1/5 vạch hẹp nên mã 6 chữ số bị tính
  1 245 mô-đun (316 mm) và luôn bị chặn hoặc ép; nay vẽ bằng mô-đun nguyên
  (205 mô-đun, 57 mm ở X 0,254 mm).
- Tem kiểm thử A5 đủ 15 loại mã gắn với sổ Excel thật: gate giải mã mọi mã ở
  30 dòng (300 DPI) và 10 dòng (600 DPI); dữ liệu sai chặn đúng dòng, đúng mã.
  Đặt `ANLABEL_A5_KIT_DIR` để lấy sổ Excel và tệp .anlabel đem in thật.
- In thật một tờ A5 (dòng 4 của sổ Excel, 15 loại mã) trên DocuCentre-IV 3065
  qua PrintService: khổ ISOA5 của trình điều khiển, 600 DPI, vùng in 4,11 mm
  đã xác minh, preflight đạt; hàng đợi nhận lệnh #96 (1 trang) và trả về
  trống. Nhật ký lệnh của chính máy in (CentreWare) ghi lệnh “ANLAbel A5
  kiem thu ma vach dong 4”: Completed, 1 trang, ra khay giữa dưới; khay 1 là
  giấy A5, không có cảnh báo. Mã trên giấy chưa được quét lại.

**Kiểm tra:** build 0 cảnh báo, 1556 unit, hồi quy 290/290.

## v0.343 — Quản lý dữ liệu, tab ngữ cảnh và hộp công cụ theo NiceLabel 10

_Gồm các commit 30/10/2026 (17:18–17:51)._

- Cửa sổ phóng to: thanh trạng thái không còn bị thanh tác vụ che 11 px (đo
  trực tiếp ở 150 %).
- Quản lý dữ liệu theo Dynamic Data Manager của NiceLabel 10 (`DataManagerWindow`):
  ribbon nhỏ, cây Biến nhập khi in / Hàm / Bộ đếm Serial / Cơ sở dữ liệu / Biến
  nội bộ, trình sửa từng mục, Đồng ý áp dụng một bước Hoàn tác, Hủy bỏ bản
  nháp. Trang chủ › Quản lý dữ liệu ▾ mở thẳng từng mục.
- Dữ liệu › Nhập nguồn dữ liệu (lấy biến và hàm từ tem ANLAbel hoặc NiceLabel,
  giữ hoặc ghi đè tên trùng) và Thứ tự nhắc; thứ tự này nay quyết định thứ tự
  câu hỏi khi in.
- Tab ngữ cảnh theo NiceLabel: Mã vạch (loại mã, dữ liệu; Vạch bao ▾, Chi tiết
  mã ▾, Chi tiết 2D ▾), Hình dạng (Kiểu viền ▾, Màu viền ▾, độ dày, bo góc; Kiểu
  tô ▾, Màu tô ▾); nhóm Sắp xếp trên mọi tab đối tượng.
- Hộp công cụ phẳng như NiceLabel (icon vector 32 px, tên dưới icon) và icon
  vector cho thẻ Dữ liệu / Lớp / Đối tượng.

**Kiểm tra:** build 0 cảnh báo, 1552 unit, hồi quy 286/286.

## v0.342 — Giao diện NiceLabel 10: thanh tiêu đề, dải tab, ribbon và trang Tệp

_Gồm các commit 30/10/2026 (16:57–17:10)._

- Dựng lại phần đầu cửa sổ theo NiceLabel 10 (chủ dự án yêu cầu 30/10/2026,
  `docs/NICELABEL_10_UI_LAYOUT.md` mục "Shell rebuild"): thanh tiêu đề tự vẽ
  màu xanh đậm chứa thanh truy cập nhanh (Mới, Mở, Lưu, Hoàn tác, Làm lại),
  bỏ dải tiêu đề 52 px; dải tab cùng màu với Tệp màu xanh nhạt; thân ribbon
  xám nhạt với nút lớn 32 px, nút nhỏ 16 px, tên nhóm ở cạnh dưới.
- Bộ icon vector riêng của ANLAbel cho toàn ribbon (một bảng màu, một nghĩa
  cho mỗi icon); thay các icon PNG đơn sắc và glyph font lẫn lộn.
- Trang chủ: Thao tác = In + Tùy chỉnh in ▾; Quản lý = Quản lý dữ liệu +
  Thuộc tính tem; Căn chỉnh dạng lưới 3 × 3; Đối tượng = Nhóm ▾, Sắp xếp ▾,
  Xóa. Dữ liệu: Excel, Tệp văn bản, Sửa / Nạp lại / Xóa cơ sở dữ liệu, Quản lý
  dữ liệu, Nguồn dùng chung. Xem: ô %, Lưới ▾ (hiện lưới, bước lưới). Trợ
  giúp: Tài liệu hướng dẫn, Hỗ trợ kỹ thuật, Giới thiệu, Trang chủ.
- Thanh trạng thái: máy in, kích thước tem, vừa tài liệu / đối tượng, thanh
  thu phóng.
- Tệp mở trang Backstage như NiceLabel: Bắt đầu (tạo, mở, tệp gần đây, tài
  liệu, máy in), Mới, Mở (Duyệt…, Nhập tem NiceLabel…, tệp gần đây; Ctrl+O),
  Lưu, Lưu thành, In, Phiên bản, Tùy chọn, Giới thiệu (phiên bản, GPL-3.0),
  Thoát. Danh sách tệp gần đây (12 tệp) lưu cùng tùy chọn của máy.

**Kiểm tra:** build 0 cảnh báo, 1547 unit, hồi quy 285/285.

## v0.341 — Trợ năng: tên cho trình đọc màn hình, chú thích nút, thứ tự Tab, viền tiêu điểm, độ tương phản

_Gồm các commit 30/10/2026 (14:32–15:20)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Mọi điều khiển nhận tiêu điểm có tên cho trình đọc màn hình (trước đó 31
  điều khiển không tên); cổng kiểm mọi ô nhập, danh sách chọn, thanh trượt và
  thẻ.
- Nút ▲ ▼ ☑ trong Xem trước và chín chấm Điểm neo có chú thích.
- Phím Tab đi theo màn hình: tiêu đề → ribbon → hộp công cụ → Dữ liệu → bảng
  thiết kế → Thuộc tính → thanh trạng thái; Ghim trước Đóng; thẻ Dữ liệu/Lớp/
  Đối tượng có tên.
- Viền tiêu điểm xanh liền 2 px ở mọi cửa sổ (chủ dự án tự xem trên màn hình).
- Chữ phụ đổi sang #5B6B80, đạt WCAG AA 4,5:1 trên mọi nền sáng. Quyết định:
  không hỗ trợ chế độ tương phản cao của Windows.

**Kiểm tra:** build 0 cảnh báo, 1543 unit, hồi quy 284/284.

## v0.340 — Ribbon định dạng Chữ định dạng (RTF); Trung tâm in; kích thước tối thiểu của hộp thoại

_Gồm các commit 30/10/2026 (13:54–14:12)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Chủ dự án chọn: khi chọn Chữ định dạng, nhóm Phông chữ trên ribbon định dạng
  phần chữ bôi đen trong ô soạn, hoặc toàn bộ nội dung nếu không bôi đen; mỗi
  lần bấm là một bước Hoàn tác.
- Trung tâm in: cột “Bộ đệm in” và nút Làm mới hiện đúng; thẻ Dữ liệu tách
  “Dòng Excel” và “Dòng 1 / 2”; tiêu đề Quản lý nguồn dữ liệu thống nhất.
- Kích thước tối thiểu: Xem trước 960 × 600, Trợ giúp 720 × 480 (hình tự thu
  nhỏ), Phiên bản 1000 px, Chọn hàng tiêu đề Excel 520 × 380; tiêu đề Nhập
  dữ liệu Excel thống nhất. Cổng mới: mọi cửa sổ đổi cỡ được phải có kích
  thước tối thiểu.

**Kiểm tra:** build 0 cảnh báo, 1543 unit, hồi quy 283/283.

## v0.339 — Rà soát trên app thật: thanh trạng thái, Thuộc tính mọi loại đối tượng, các hộp thoại

_Gồm các commit 30/10/2026 (11:36–12:21), kiểm bằng UI Automation trên bản
cài, màn hình 150 %._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Thanh trạng thái: thẻ cảnh báo máy in không còn bị nút Máy in che.
- Thuộc tính mã vạch: “Chế độ 2D” và “Mức ECC” hiện đủ giá trị.
- Phiên bản mẫu tem, Nhập dữ liệu Excel và sơ đồ Trợ giúp không còn cắt chữ;
  danh sách Đối tượng và chín nút công cụ có tên cho trình đọc màn hình; tiêu
  đề Thuộc tính không chạy dưới nút ghim.
- Chữ định dạng: nút B/I/U/A+/A-/căn hiện đủ; thẻ Kiểu của Chữ định dạng và
  Đảo màu có ghi chú; hình dạng không còn mời “Liên kết Excel…”.
- Lịch sử in: ô tìm có nhãn, nút Làm mới gọn, cột “Mẫu tem / hàng đợi” hiện đủ.

**Kiểm tra:** build 0 cảnh báo, 1543 unit, hồi quy 282/282.

## v0.338 — Rà soát giao diện: Trợ giúp theo bố cục mới, hộp thoại theo màu thật, ribbon thu gọn khi cửa sổ hẹp

_Gồm các commit 30/10/2026 (11:06–11:26)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Trợ giúp mô tả thanh lệnh theo thẻ, hộp công cụ 3 × 3 và các thẻ Thuộc tính;
  cột của Phiên bản mẫu tem không còn bị cắt.
- Mẫu tem mới không còn để nút che ô DPI; nút “☑ Tất cả” và “✕” trong Xem
  trước hiện đủ; ô phông và cỡ chữ trên ribbon căn giữa.
- Dưới 1200 px, ribbon gộp lệnh in phụ thành “Thêm lệnh in ▾” và tám nút căn
  thành “Căn chỉnh ▾”.

**Kiểm tra:** build 0 cảnh báo, 1543 unit, hồi quy 282/282.

## v0.337 — Rà soát giao diện: nhóm ribbon thẳng hàng, nút xoay dùng được, bảng dữ liệu Xem trước, Thiết lập máy in

_Gồm các commit 30/10/2026 (10:38–10:56)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Nhãn các nhóm ribbon thẳng hàng; thẻ Thiết kế không còn bị co nhỏ; chưa
  chọn máy in thì hiện “Chưa chọn máy in” thay vì để trống; hộp công cụ gọn ở
  màn nhỏ.
- Nút xoay 0°/90°/180°/270° trong bảng Thuộc tính trước đây luôn bị khóa; nay
  dùng được. Thẻ Vị trí có thêm xoay và điểm neo.
- Tiêu đề cột bảng dữ liệu trong Xem trước nằm đúng phía trên các dòng.
- Các bước bên trái của Thiết lập máy in dùng kiểu phẳng, bước đang chọn có
  vạch xanh.

**Kiểm tra:** build 0 cảnh báo, 1543 unit, hồi quy 282/282.

## v0.336 — NiceLabel 10: Đảo màu hiện đúng khi thiết kế, Không in, soi gương, điều kiện hiển thị, cây Dữ liệu

_Gồm các commit sáng 30/10/2026 (08:23–09:57)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Bảng thiết kế vẽ vùng Đảo màu đảo thật như khi in.
- Thiết kế › Không in (đối tượng chỉ để thiết kế); Xem › Vừa đối tượng và
  Không căn; Tăng/Giảm cỡ chữ.
- Gạch ngang, màu chữ, Soi gương, Giữ tỉ lệ, Điều kiện hiển thị (chỉ in ở dòng
  có giá trị).
- Thẻ “Thiết kế tem · <tên>” dưới bảng thiết kế; cây Biến nhập khi in / Trường
  dữ liệu; tô vùng tờ giấy quanh tem khi tờ lớn hơn tem.
- Ghi quyết định phạm vi của chủ dự án (không làm Report, PowerForms, kết nối
  CSDL, RFID) và kết quả chủ dự án tự kiểm bảng Thuộc tính nổi.

**Kiểm tra:** build 0 cảnh báo, 1543 unit, hồi quy 281/281.

## v0.335 — Giao diện thiết kế theo NiceLabel 10: ribbon theo thẻ, bảng Thuộc tính nổi, nhóm, mặt nạ, Đảo màu, Chữ định dạng

_Gồm các commit đêm 29/10/2026 (23:19–00:03)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Ribbon theo thẻ: Tệp, Trang chủ, Dữ liệu, Xem, Trợ giúp, cùng thẻ Thiết kế
  và Chữ/Mã vạch/Hình ảnh/Hình dạng chỉ hiện khi chọn đối tượng. Hộp công cụ
  ba cột; thẻ Dữ liệu/Lớp/Đối tượng có tìm kiếm; bảng Thuộc tính chia
  Nguồn/Kiểu/Vị trí/Chung; thanh trạng thái có máy in và thu phóng.
- Bảng Thuộc tính ghim bên phải hoặc nổi cạnh đối tượng (nút ghim, F4, nhấp
  đúp). Điểm neo 9 vị trí cho đối tượng giãn theo nội dung.
- Nhóm/Bỏ nhóm; dấu tên và dấu nguồn dữ liệu trên đối tượng; xoay khung nhìn
  (không xoay tem).
- Mặt nạ nội dung (Nguồn › Mặt nạ) và Sao định dạng (Ctrl+Shift+C /
  Ctrl+Shift+V).
- Đối tượng mới: Đảo màu (đổi đen/trắng phần bên dưới khi in) và Chữ định
  dạng (RTF, cắt tại khung như Ô chữ).

**Kiểm tra:** build 0 cảnh báo, 1535 unit, hồi quy 279/279.

## v0.334 — Theo NiceLabel 10: khổ giấy văn phòng, tờ nhiều tem, tỉ lệ phông Chữ, Ô chữ giãn theo nội dung

_Commit tối 29/10/2026 (22:42) theo yêu cầu của chủ dự án._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

**Giấy và máy in**

- Bỏ chặn A4, Letter, Legal, A3 (cạnh 8–1000 mm).
- Lưới tem trên tờ: số tem ngang/dọc, khoảng cách, thứ tự điền, góc bắt đầu;
  kiểm tra lưới nằm trong tờ; mỗi dòng dữ liệu in vào một ô, mỗi trang một
  tờ. Mã băm tài liệu tem đơn không đổi.
- Nhập `.nlbl` áp khoảng cách và tờ nhiều tem làm khổ vật lý.

**Chữ** (đổi hợp đồng Text/TextBox theo yêu cầu; cập nhật AGENTS.md, PLAN.md
và tài liệu TextBox)

- Tỉ lệ phông của Chữ 10–400 % (Font Scaling của NiceLabel); ScaleFactor khi
  nhập dùng thuộc tính này.
- Ô chữ “Giãn chiều cao theo nội dung” (VariableHeight): giữ bề rộng, chiều
  cao theo chữ khi thiết kế và khi in.

**Kiểm tra:** build 0 cảnh báo, 1518 unit, hồi quy 274/274.

## v0.333 — Nhập .nlbl: ảnh cạnh tệp tem, biến nhập khi in, máy in và khổ giấy

_Gồm các commit tối 29/10/2026 (19:36–20:22)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Ảnh không nhúng được đọc từ tệp đã lưu hoặc nằm cạnh tệp tem (chỉ đọc, tối
  đa 32 MB).
- Bảng Thuộc tính không còn báo `{?Tên}` và `{#NOW…}` là cột thiếu trong bảng
  tính; liên kết chỉ gồm chúng mang nhãn “Biến nhập khi in”.
- Nhập cài đặt máy in và khổ giấy trong hợp đồng in: tem cuộn nhận khổ thủ
  công, độ lệch tem đầu và DPI; tem theo trình điều khiển giữ khổ tự động; máy
  in của người soạn chỉ được nêu, không tự chọn.
- Dòng giải thích khi nhập tem không còn bị cảnh báo hàng đợi máy in đè mất.
- Bộ cài NiceLabel dùng để đối chiếu không được đưa vào repo.

**Kiểm tra:** 104 tem theo trình điều khiển, 92 tờ văn phòng, 11 tem cuộn;
mọi khổ áp dụng đều qua hợp đồng khổ tem và DPI.

## v0.332 — Nhập .nlbl: khổ tem theo bố cục trang; chi tiết Code 39, nét, bo góc; hàm MID và GS1CHECKDIGIT

_Gồm các commit tối 29/10/2026 (19:19–19:31)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Khổ tem lấy từ phần tử bố cục trang (tờ nhiều tem: ô lặp, ví dụ tem
  101,6 × 50,8 mm trên A4); chữ gắn biến đo theo giá trị mặc định của biến.
- Code 39: số kiểm tra, hiện số kiểm tra dưới mã, tỉ lệ rộng/hẹp, khoảng cách
  ký tự; nét đứt, chấm, không viền; bo góc hình chữ nhật.
- Công thức có thêm `MID` và `GS1CHECKDIGIT`; biến nhập khi in nằm trong công
  thức cũng được hỏi lúc in. Phần GS1 cần số kiểm tra lúc in được chuyển
  thành công thức.

**Kiểm tra:** 6.209/6.467 đối tượng mẫu chuyển được (96,0 %).

## v0.331 — Nhập .nlbl: co giãn chữ, chữ định dạng, nhiều dòng, GS1-128, ngày và mã định danh

_Gồm các commit tối 29/10/2026 (18:37–19:11)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- ScaleFactor dưới 100 % thành Chữ khung cố định co ngang; BestFit của ô chữ
  thành Thu nhỏ phông hoặc Co ngang.
- Lấy chữ thường từ mục chữ định dạng (RTF, base64), kể cả giá trị mặc định
  của biến; giá trị nhiều dòng lưu dạng base64 được giải mã.
- GS1-128 theo bảng AI của NiceLabel (217 dòng); cộng trừ ngày thành trường
  `{#NOW+…}`; mã định danh ANSI MH10.8.2 và phong bì ISO/IEC 15434 (định
  dạng 06).
- Chữ vị trí bắt đầu ở khung đo theo nội dung, không còn lệch xuống chồng lên
  mã vạch.

**Kiểm tra:** 6.154/6.467 đối tượng mẫu chuyển được.

## v0.330 — Mở tem NiceLabel 10 (.nlbl) thành mẫu mới

_Gồm các commit tối 29/10/2026 (18:28–18:32)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Đọc gói `.nlbl` (WinZip AES-256, tự cài đặt theo đặc tả công khai) và
  chuyển: chữ, ô chữ, mã vạch tuyến tính, QR, DataMatrix, PDF417, ảnh PNG/WMF
  nhúng, đường kẻ, hình chữ nhật. Đối tượng đặt theo điểm neo và góc xoay của
  NiceLabel.
- Tệp giải pháp `.slnx` cung cấp biến nhập khi in, trường ngày hiện tại, ghép
  chuỗi và cột dữ liệu.
- Đối tượng dùng script, hàm GS1/HIBC, GS1 DataBar, ảnh không kèm dữ liệu, chữ
  RTF và báo cáo để nguyên và được đếm trong dòng trạng thái.
- Hộp Mở tệp nhận “Tem NiceLabel (*.lbl;*.nlbl)”; tem NiceLabel truyền qua dòng
  lệnh cũng mở được. Mã ma trận lấy kích thước theo lưới mô-đun của bộ mã hóa.

**Kiểm tra:** 207/207 tệp mẫu mở được, 5.971/6.467 đối tượng chuyển; cổng hồi
quy với gói tổng hợp (hình học, liên kết, trường ngày, vị trí neo và xoay, ảnh
nhúng, kết xuất qua đường in, từ chối gói bị sửa).

## v0.329 — Hộp thoại nhiều lựa chọn bằng tiếng Việt; mã vạch .lbl lưu kích thước cũ

_Gồm các commit đầu chiều 29/10/2026 (13:24–14:13)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Hộp hỏi in lại, hỏi bỏ qua dòng lỗi và mọi hộp Có/Không/Hủy còn lại (xóa và
  dọn nguồn dữ liệu, đặt lại bộ đếm Serial, mở tệp vừa xuất, bỏ liên kết
  Excel, thiết lập hàng đợi máy in, hủy lệnh in, khôi phục phiên bản, tải và
  cài cập nhật) dùng hộp thoại tiếng Việt của app. Kết quả và lựa chọn mặc
  định giữ nguyên, kể cả mặc định an toàn khi đặt lại Serial. Đóng mục “Còn
  sót” của v0.326.
- `.lbl`: mã vạch có kích thước lưu từ lúc thiết kế (đã cũ) vẫn chuyển được
  nhờ đối chiếu theo vị trí; tỉ lệ kích thước giới hạn 0,5–2.
- Ghi chép: biến V33 và nội dung RTF của `.lbl` để nguyên.

**Kiểm tra:** các commit của đợt này không ghi số liệu cổng riêng.

## v0.328 — Nhập tem NiceLabel 5 (.lbl) kèm đối tượng

_Gồm các commit sáng 29/10/2026 (11:16)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Chuyển đối tượng khi mở `.lbl`: mã vạch (giá trị cố định, gắn biến, biến
  thể UPC-A), chữ một và nhiều dòng với phông, cỡ, đậm và căn ngang, đoạn văn
  thành Ô chữ, hình chữ nhật và đường kẻ; đối chiếu kích thước theo
  inch/cm/mm; nhận tùy chọn PDF417 và bộ đếm Serial.
- Dòng trạng thái báo đúng số đối tượng chưa chuyển. Bản ghi chưa chứng minh
  được (biến toàn cục, đoạn RTF, ảnh ngoài, MaxiCode, Bookland, EAN-14) để
  nguyên, không đoán.

**Kiểm tra:** cổng nhập `.lbl` mở rộng: mã vạch và chữ gắn biến, phông Courier
New, nội dung nhiều dòng, đoạn văn thành Ô chữ, chữ neo phải, biến thể UPC-A,
mực in kiểm qua đường in thật, tổng số đối tượng.

## v0.327 — Mở tem NiceLabel 5 (.lbl) thành mẫu mới theo khổ tem

_Gồm các commit chiều 28/10/2026 (10:02–14:34)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

- Mở tệp `.lbl` của NiceLabel 5: bộ đọc tiêu đề (tệp OLE compound, chữ ký
  NicePower, định dạng phiên bản 29) lấy khổ tem theo 1/100 mm và tạo mẫu
  ANLAbel trống đúng khổ, đặt tên theo tệp. Biến thể bố cục khác bị từ chối
  kèm lý do; đối tượng chưa được chuyển.
- Tệp NiceLabel mở ra như tài liệu mới chưa lưu, nên không bao giờ bị ghi đè
  bằng tệp ANLAbel.
- Ghi chép dịch ngược định dạng `.lbl` (bảng ký hiệu TD32, luồng TIO, tiêu đề
  HIBIC, mã số loại mã vạch).
- Nhật ký v0.326 bổ sung bằng chứng hộp “Một số dòng bị chặn” qua hàng đợi
  cổng FILE: (không gửi tem nào).

**Kiểm tra:** 65 tem mẫu cài sẵn của NiceLabel: 28 nhận, 37 biến thể bị từ
chối đúng thiết kế; cổng hồi quy với tệp tổng hợp; build, 1406 unit và vòng
chất lượng nhanh xanh.

## v0.326 — Ký tự ẩn trong dữ liệu mã vạch; ô Chữ nhận Enter; mức PDF417 và bảng ký tự Serial

_Gồm các commit sáng 28/10/2026 (07:49–09:40)._ _Số phiên bản được gắn bổ sung ngày 30/10/2026 theo `docs/VERSIONING.md`: các commit này đã nằm trong bản cài vẫn ghi v0.325._

**Mã vạch và bộ đếm Serial** (đối chiếu bộ mã vạch của NiceLabel 5)

- PDF417: mức sửa lỗi 0–8 và mã rút gọn; tắt sẵn, mã cũ in y hệt.
- Bộ đếm Serial thêm bảng ký tự: thập lục phân, chữ cái, chữ số kèm chữ cái và
  bảng tự đặt (có kiểm tra), dùng chung cơ chế giữ chỗ số hiện có.
- Kiểm tra trước in gợi ý (không chặn) chiều cao EAN-13, EAN-8, UPC-A, UPC-E
  theo GS1.
- Ghi chép dịch ngược NiceLabel 5 (hiệu chỉnh khoảng trắng, cờ banding, phiên
  in): chỉ đối chiếu, không áp dụng thêm.

**Mã vạch** (báo lỗi 2026-10-28: ảnh chụp từ một bản cũ hơn v0.306 báo "Preview
blocked: row 2 … invalid Code128 data")

- Chạy tệp Excel của thao tác viên qua bộ kiểm tra trước in hiện tại: các dòng
  có mã hàng đều đạt; dòng bị chặn là dòng **trống ô mã hàng** (Excel điền
  xuống cột MFG và công thức "-" nên dòng vẫn được đọc). Bản hiện tại báo "dữ
  liệu mã Code128 đang trống", và Xem trước cho bỏ qua các dòng lỗi để in phần
  còn lại thay vì chặn cả xem trước như bản cũ.
- Cùng bộ tệp đó, sheet lịch sử có mã hàng dán từ web/ERP mang khoảng trắng
  không ngắt (U+00A0) ở đầu và cuối. Code 128 không mã hóa được ký tự này, còn
  thông báo cũ không nói ký tự nào.
- Sửa: `BarcodeDataText` (Core) bỏ ký tự ẩn ở hai đầu dữ liệu mã vạch (khoảng
  trắng ngoài ASCII, ký tự rộng 0, BOM) trên mọi đường đi: thiết kế, xem trước,
  kiểm tra trước in, in và dòng chữ dưới mã. Dấu cách, tab và ký tự ở giữa giữ
  nguyên. Ký tự không mã hóa được ở giữa nay được nêu tên và vị trí
  (ví dụ "gạch ngang dài “–” (U+2013) ở vị trí 6") trong tiếng Việt.

**Thiết kế**

- Ô "Chữ" trong bảng thuộc tính nhận phím Enter để xuống dòng (bộ vẽ và bộ đo
  đã hỗ trợ nhiều dòng từ trước). Không đổi hợp đồng Text/TextBox: khung, bọc
  chữ, cắt, đệm và vòng đời kéo giữ nguyên.

**Kiểm tra:**

- Build 0/0; unit 1406/1406 (thêm `BarcodeDataTextTests`); regression 268 PASS
  / 0 FAIL, gồm bảy gate Text/TextBox và gate ngôn ngữ thao tác viên với hai
  câu mới. Smoke UI 15/15 trên bản đã cài; gửi phím Enter vào ô "Chữ" của bản
  đã cài cho giá trị hai dòng.
- Tệp Excel thật (17 dòng, 5 dòng có mã hàng) qua `PrintPreflightValidator`:
  5 dòng đạt, 12 dòng trống bị chặn đúng với thông báo tiếng Việt.
- Trên app đã cài: tem 75 × 100 dựng theo ảnh báo lỗi (Vendor code, PO, mã
  hàng khách là Code 128) liên kết tệp đó, mở bằng UI Automation → Xem trước mở
  bình thường, "Nhãn 1 / 17", mã Code 128 của dòng 1 vẽ được, bảng "Vấn đề
  nội dung" nêu đúng "Dòng 6, 7, 8, 9, 10 (+7 dòng khác) · “<mã hàng> Copy”
  (mã vạch): dữ liệu mã Code128 đang trống" kèm nút "Đi tới tem đầu tiên bị
  ảnh hưởng". Bấm In với hai hàng đợi tem trên máy (Xprinter XP-DT426B,
  Datamax E-4203, cả hai đang tắt): dừng ở "Chưa xác nhận được cài đặt máy
  in … đang ngoại tuyến. Chưa có tem nào được gửi"; hàng đợi 0 → 0 việc in,
  không có bản ghi lệnh in.
- Bước hỏi bỏ qua dòng lỗi (không có máy in thật): tạo tạm hàng đợi
  "ANLAbel Test Datamax" dùng driver Datamax E-4203 có sẵn, cổng FILE:,
  mẫu đặt khổ thủ công 75 × 100 (chế độ "tự động theo driver" giữ khổ mặc định
  4 × 6 của driver nên bị chặn "driver đổi khổ giấy"). Xem trước báo "Cài đặt
  máy in đã chọn đã được xác thực"; bấm In → hộp "Một số dòng bị chặn": "12
  dòng có lỗi và sẽ được bỏ qua: dòng 6, 7, …, 17 · “PO”, “<mã hàng> Copy” (mã
  vạch): dữ liệu mã Code128 đang trống. Tiếp tục in 5 dòng còn lại?" Đóng hộp
  (Cancel): hàng đợi 0 → 0, không có print-operations.jsonl. Hàng đợi tạm đã
  xóa. Còn sót: ba nút của hộp này là Yes / No / Cancel tiếng Anh (MessageBox
  Win32), chưa Việt hóa.

## v0.325 — Sửa mã vạch, số kiểm tra tự sinh, bộ đếm Serial và giá trị theo lệnh in

**Mã vạch** (sửa lỗi tìm thấy khi đối chiếu với phần mềm tham chiếu)

- Code 39 có tỉ lệ vạch đặt tay: in vạch đều theo số chấm nguyên, tỉ lệ thực
  2,2–3,0:1 (ISO/IEC 16388); không còn mất vạch đầu khi vùng trống bằng 0.
- Chế độ số kiểm tra Auto không còn giấu một ký tự dữ liệu thật trong dòng chữ
  dưới mã (1/10 giá trị ITF).
- Thanh bearer của khung đè lên vạch ngoài cùng: kiểm tra trước in nay chặn.

**Tính năng mới** (chủ sản phẩm duyệt 2026-10-17; tất cả tắt sẵn, lưu trong mẫu)

- Số kiểm tra `Generate` và số kiểm tra tùy chỉnh (modulus, trọng số, bảng giá
  trị; mẫu có sẵn GS1 mod 10, Luhn, Mod 11 X, UPU S10).
- Giảm bề rộng vạch theo số chấm nguyên; chặn khi vạch hẹp còn dưới 2 chấm.
- Mặt nạ dòng chữ dưới mã, khoảng cách ký tự Code 39, độ dày bearer, chọn bộ mã
  Code 128 (Auto/A/B/C/Manual).
- Bộ đếm Serial `{#SERIAL}`: số được giữ chỗ cho từng lệnh in trước khi in, không
  bao giờ dùng lại; in lại dùng đúng số cũ; có thể dùng chung qua thư mục mạng.
- Giá trị theo lệnh in `{#NOW...}` và `{?Tên}`: ghi lại theo lệnh, in lại lặp
  đúng giá trị cũ.
- Cảnh báo (không chặn): EAN/UPC ngoài 80–200 % GS1; nét mảnh hơn một chấm máy in.

Mẫu dùng các tính năng này cần bản này trở lên.

**Kiểm tra:**

- Build 0/0; unit 1344/1344; regression 268 PASS / 0 FAIL, gồm gate in ảo 203 DPI
  giải mã lại bằng ZXing. Bộ đếm dùng chung đã chạy qua SMB với 4 tiến trình
  (300 số, không trùng, liên tục).
- Chưa kiểm: giảm bề rộng vạch trên máy quét/máy kiểm thật; in Serial trên hàng
  đợi máy in thật; thư mục chung trên một PC khác.

## v0.324 — Ô chữ mới mang chữ “Ô chữ”; hướng dẫn sử dụng viết lại

**Thiết kế** (chủ sản phẩm duyệt 2026-10-15; đổi bất biến 5 của hợp đồng Text/TextBox)

- Ô chữ mới thêm có tên và nội dung mẫu “Ô chữ” thay cho “Text Box”. Khung mặc định
  (tối đa 32 × 6 mm, lề 4 %), cỡ 9 pt, căn giữa dọc, đệm 0,2 mm không đổi. Mẫu đã
  lưu không bị đổi.

**Tài liệu**

- Hướng dẫn sử dụng đi kèm bộ cài (Start menu → Huong dan su dung) được viết lại
  theo app hiện tại; bản cũ vẫn mô tả bản beta v0.042.

**Kiểm tra:**

- Build 0/0; unit 1220/1220; regression 265 PASS / 0 FAIL, gồm bảy gate
  Text/TextBox (gate `new text box uses compact label-aware frame` nay kiểm “Ô
  chữ”). Mutation gate không bắt buộc: không đổi contract Core nào trong danh
  sách mutation.

## v0.323 — Sửa theo review: Hủy khi nhập quá giờ, chữ tiếng Việt còn sót

**Dữ liệu**

- Bấm Hủy trong lúc việc nhập quá giờ đang nhả tệp nay được hiểu là Hủy, không
  hiện thêm lời báo hết giờ.
- Lỗi thật của một lượt đọc đã bị bỏ dở lại được ghi vào `logs\errors.log`.

**Thiết kế**

- Chữ mới thêm bắt đầu bằng “Chữ tự do dài ra theo nội dung” thay cho câu tiếng
  Anh; gỡ liên kết của một đối tượng trống điền “Chữ”. Hai lời báo dùng “trình
  điều khiển” và “tệp” như phần còn lại của app.

**Kiểm tra:**

- Review độc lập các thay đổi ngày 2026-10-15: không có lỗi mức vừa hay cao; năm
  điểm mức thấp đã sửa, hai unit mới đỏ khi gỡ bản sửa.
- Build 0/0; unit 1220/1220; regression 265 PASS / 0 FAIL, gồm bảy gate
  Text/TextBox. Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.

## v0.322 — Chiều chữ và tên đối tượng trong mẫu thư viện bằng tiếng Việt

**Thiết kế**

- Danh sách “Chiều chữ” trong Bảng thuộc tính hiện “Tự động”, “Trái sang phải”,
  “Phải sang trái” thay cho tên enum “Auto”…
- Tên đối tượng trong 22 mẫu thư viện, hiện ở danh sách Lớp và thẻ Kiểm tra ràng
  buộc, dùng tiền tố tiếng Việt: “Trường: PartName”, “Mã vạch: PartNo”, “Chú thích:
  Qty”, “Giá trị: Lot”, “Đường kẻ 1”… (329 tên). Chỉ đổi tên hiển thị; mã viết tắt
  như `r1` giữ nguyên. Mẫu đã lưu từ trước không bị đổi.
- Tìm ra khi chụp Bảng thuộc tính trên bản cài v0.321.

**Kiểm tra:**

- Build 0/0; unit 1218/1218; regression 265 PASS / 0 FAIL, gồm bảy gate
  Text/TextBox và hai gate mẫu thư viện. Mutation gate không bắt buộc: không đổi
  contract Core nào trong danh sách mutation.

## v0.321 — Thẻ Ô chữ trong Bảng thuộc tính bằng tiếng Việt

**Thiết kế** (chủ sản phẩm duyệt 2026-10-15; hợp đồng Text/TextBox, chỉ đổi chữ)

- Sáu tùy chọn của “Vừa trong khung” và “Khi nội dung quá cao”, hai banner giải
  thích Chữ và Ô chữ, và lời báo tràn chữ trên canvas nay là tiếng Việt. Ví dụ:
  “Chặn in và cảnh báo”, “Cắt bỏ phần chữ dư (không in)”, “Chữ tràn ra ngoài khung
  Ô chữ. Nới khung, bớt chữ hoặc giảm cỡ chữ.” Hành vi, giá trị lưu tệp và preset
  không đổi; chữ mẫu “Text Box” của Ô chữ mới giữ nguyên theo hợp đồng.
- Bộ quét Việt hóa nay đọc cả chữ đặt qua Style/Trigger, chỗ các chữ này đã lọt
  qua.

**Bên trong** (không đổi hành vi, đối chiếu ngẫu nhiên với mã cũ: 0 khác biệt)

- Lệnh in và phần kiểm tra tên khớp ngang nhau dùng chung một quy tắc chọn tên
  trường; giải mã ký tự thoát trong chuỗi công thức và đổi số cột ra chữ cái cột
  mỗi thứ còn một chỗ.

**Kiểm tra:**

- Build 0/0; unit 1218/1218; regression 265 PASS / 0 FAIL, gồm bảy gate
  Text/TextBox và gate mới `text box overflow is reported in Vietnamese on the
  canvas`. Mutation gate không bắt buộc: không đổi contract Core nào trong danh
  sách mutation.
- Hai mẫu thật 100×70 qua kiểm tra trước khi in sau khi dời dòng cuối 0,2 mm
  (sửa tệp mẫu của khách, không phải mã).

## v0.320 — Nhập Excel quá giờ thì dừng đọc và nhả tệp ngay

**Dữ liệu**

- Khi việc đọc tệp Excel hoặc CSV vượt giới hạn (45 s với tệp trên máy, 30 s với
  tệp mạng), người vận hành nhận lời báo hết giờ nhưng việc đọc vẫn chạy ngầm tới
  hết tệp. Trong lúc đó tệp bị giữ nên Excel không lưu đè được, và nếu bấm thử
  lại thì hai lượt đọc chạy cùng lúc. Trên máy yếu, đây là kiểu chồng tải trông
  như treo. Nay việc đọc dừng theo và tệp được nhả trước khi lời báo hiện.
- Đã thử thêm các kiểu tệp có thể làm treo máy 4 GB: ô lạc ở dòng cuối trang tính,
  dòng trống có kẻ khung tới cuối trang tính, và 12 sheet × 20.000 dòng. Không
  kiểu nào treo hay tốn bộ nhớ (tối đa 90 MB). Chậm nhất là dòng có kẻ khung tới
  cuối trang tính: xem trước 3,0 s, nhập 6,5 s. Số đo ghi trong “Measured limits”
  của `docs/LOCAL_LABEL_PRODUCT_CONTRACT.md`.

**Kiểm tra:**

- Build 0/0; unit 1218/1218; regression 264 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.
- Unit `TimedOutRead_StopsAndReleasesTheFile` đỏ trên bản cũ. Với .xlsx (đo bằng
  probe tạm), tệp được nhả cùng lúc báo lỗi thay vì 6,6 s sau.
- Chưa kiểm chứng trên máy 4 GB thật.

## v0.319 — Máy in rút cáp thì dừng in, không gửi lệnh vào hàng đợi

**In**

- Khi máy in USB bị rút cáp, Windows đánh dấu hàng đợi là “làm việc ngoại tuyến”
  nhưng trạng thái vẫn báo sẵn sàng. ANLAbel trước đây vẫn gửi lệnh in: lệnh nằm
  chờ trong hàng đợi và tự in ra lúc máy in được cắm lại, có thể in trùng nếu
  người vận hành đã in lại. Nay lệnh in dừng trước khi gửi với lời báo “Máy in
  “Xprinter XP-DT426B” đang ngoại tuyến (offline). Chưa có tem nào được gửi.” và
  cách sửa “Bật máy in, kiểm tra cáp hoặc mạng rồi in lại.”
- Máy in còn trong Windows nhưng chưa in được (ngoại tuyến, tạm dừng, hết tem…)
  không còn bị báo là “Không tìm thấy máy in”.
- Tìm ra khi in thử có phép trên XP-DT426B ngày 2026-10-14; lúc đó trong hàng đợi
  còn một lệnh từ 2026-10-01 chưa in.

**Kiểm tra:**

- Build 0/0; unit 1217/1217; regression 264 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.
- Đã kiểm chuột thật ở 150 % trên bản cài v0.318 (kéo, dời, hoàn tác, tooltip).
  Tem in thử chưa ra vì máy in đang rút cáp.

## v0.318 — Một ô lạc ở cột xa không còn làm việc nhập Excel chậm và tốn bộ nhớ

**Dữ liệu**

- Một ô có chữ nằm xa bên phải dữ liệu (ví dụ ở cột XFD) từng làm mọi cột trống
  trước nó thành trường: 16.384 trường; nhập 1.000 dòng mất 25–31 s và tới
  1,4 GB bộ nhớ, rồi lưới dữ liệu còn tạo một cột cho mỗi trường. Đây là một
  nguyên nhân có thể làm treo máy 4 GB. Nay cột không có tiêu đề và không có dữ
  liệu không thành trường: cùng tệp nhập trong 0,5 s, 74 MB. Tệp CSV lưu từ trang
  tính như vậy cũng được xử lý như thế.
- Khi có cột trống ở giữa, lời báo cột thiếu vẫn nêu đúng chữ cái cột cũ: mỗi
  trường nhớ cột gốc của nó.
- Đã đo: bộ nhớ không tăng sau nhiều lần Cập nhật Excel, nhập lại hay mở mẫu.

**Kiểm tra:**

- Build 0/0; unit 1213/1213; regression 263 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.
- Chưa kiểm chứng trên máy 4 GB thật; chưa có bằng chứng in vật lý mới.

## v0.317 — Lời báo đúng cách sửa cho QR phiên bản cố định và dấu ngoặc trống

Review lần ba các thay đổi của v0.316 không thấy lỗi mức vừa hay cao. Bản này
sửa hai lỗi nhỏ review tìm ra và một lời khuyên sai của v0.316.

**Mã vạch**

- QR đặt phiên bản cố định mà dữ liệu vượt dung lượng: designer nay báo như khi
  in, “Dữ liệu (… byte) vượt dung lượng phiên bản QR đã chọn; chọn phiên bản lớn
  hơn hoặc Tự động”. v0.316 khuyên nới khung, nhưng ở chế độ này khung do phiên
  bản quyết định.

**Dữ liệu**

- Dấu ngoặc không có tên cột ở giữa (ví dụ `Size: { }`) vẫn chặn in, nay nói rõ
  “dấu ngoặc {} chưa có tên cột nên đối tượng sẽ in trống; nhập tên cột vào giữa
  hai dấu ngoặc hoặc xóa chúng”. Danh sách Lớp hiện “Lỗi liên kết” thay vì một
  cột thiếu không có tên.
- Liên kết viết có dấu cách như `{ Lot }` khi tệp không có cột đó được đếm là
  một trường thiếu (trước hiện “Thiếu 2 trường”).

**Kiểm tra:**

- Build 0/0; unit 1211/1211; regression 262 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.
- Chưa có bằng chứng in vật lý mới.

## v0.316 — Sửa theo review lần hai: giá trị in không bị trống, liên kết không bị đổi, lời báo đúng cách sửa

Review mã nguồn lần hai các thay đổi của v0.311–v0.315 xác nhận 15 lỗi; bản này
sửa cả 15.

**In**

- Designer, bước kiểm tra trước khi in và luồng in dùng chung bộ đọc công thức.
  Khi hai bên đọc cùng lúc (ví dụ bấm chọn đối tượng trong lúc đang in), một tem
  có thể in ra ô trống dù kiểm tra đã đạt. Nay mỗi lần đọc công thức độc lập.
- Kiểm tra trước khi in mã 2D nhanh hơn: mỗi dòng chỉ mã hóa một lần (2.000 dòng
  QR + Code 128: 3,3 s → 2,0 s).

**Dữ liệu (DB03)**

- Liên kết tới trường chuyển đổi (ví dụ `{LotNo}` khi tệp có cột “Lot No”) hoặc
  tên khớp ngang nhau với hai cột không còn bị tự đổi sang cột trông giống khi
  nhập hay cập nhật dữ liệu.
- Tên gần giống khớp ngang nhau vẫn bị chặn khi cùng liên kết cũng đọc đúng tên
  cột đó; cột số bản lấy qua trường chuyển đổi cũng được kiểm.
- Dòng in giữ thứ tự cột của tệp rồi tới trường chuyển đổi, đúng thứ tự ANLAbel
  kiểm tra.
- Công thức có tên trường ghép từ dữ liệu (ví dụ `FIELD(CONCAT("Price_",
  FIELD("Currency")))`) không còn bị báo là thiếu cột, và lời báo sau khi nhập
  không còn nêu `{ }` là một cột thiếu (lệnh in vẫn dừng; v0.317 nói rõ lý do).
- Tên khớp ngang nhau được báo đúng cách sửa: “Tên khớp nhiều cột: PartNo” trong
  danh sách Lớp, “đổi tên một trong các cột đó hoặc liên kết đúng tên tiêu đề”
  trong Bảng thuộc tính. Sau Mở, Xác thực, Liên kết lại, dòng trạng thái giữ số
  liên kết hỏng.

**Mã vạch**

- Designer đánh dấu mã 2D cần nhiều chấm in hơn khung (di chuột để xem lý do);
  trước đây mã vuông bị bóp trông vẫn bình thường cho tới khi lệnh in bị chặn.
- QR phiên bản cố định không chứa nổi dữ liệu chỉ báo một lỗi về dung lượng.
- Mã 1D vượt ra ngoài tem được báo là “tràn ra ngoài mép tem”.

**Đo lại**

- Mở Xem trước 1.000 tem: 1,0–1,3 s; 10.000 tem: 8,6–9,6 s (ba lần, máy phát
  triển rảnh). Số đã công bố trước đó là 2,2 s và 16,6 s.

**Kiểm tra:**

- Build 0/0; unit 1211/1211; regression 262 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mỗi kiểm tra mới đỏ khi gỡ đúng bản sửa của nó. Mutation gate
  không bắt buộc: không đổi contract Core nào trong danh sách mutation.
- Chưa có bằng chứng in vật lý mới.

## v0.315 — Sửa theo review mã nguồn: không mở nhầm, không lách chặn cột, mã 2D đúng khung

Review mã nguồn độ kỹ cao các thay đổi B06, DB03 và B05 xác nhận 23 lỗi; bản
này sửa các lỗi có tác động.

**Tệp**

- Mẫu có công thức chứa dấu gạch chéo (ví dụ `FIELD("W\H")`) có thể làm việc
  mở mẫu dừng giữa chừng sau khi mẫu mới đã hiện, và lần Lưu sau có thể ghi đè
  tệp cũ. Nay công thức được đọc đúng quy tắc của bộ công thức, không còn lỗi.

**Dữ liệu (DB03)**

- Không còn cách nào khác để in từ cột chỉ nhận ra theo vị trí: tên trường viết
  có dấu cách trước ngoặc, ghép bằng CONCAT, tên trường chuyển đổi viết gần đúng,
  tên khớp ngang nhau với hai tiêu đề gần giống (ví dụ “Part No” và “Part-No”),
  hay cột số bản ghi trong ngoặc nhọn đều bị chặn như nhau. ANLAbel không còn tự
  đổi liên kết sang cột đầu tiên trong số các tiêu đề gần giống.
- Lời báo cột thiếu/trùng không còn bị mất khi mở mẫu, xác thực hay liên kết lại;
  lời báo tiêu đề không dùng tới không che số liên kết hỏng; cột số bản và đầu vào
  bị mất của trường chuyển đổi được nêu.
- Xem trước không còn đề nghị “bỏ dòng lỗi” khi cả mẫu đang bị chặn.

**Mã vạch**

- Mã 2D (QR, Data Matrix, Aztec, PDF417) cần nhiều chấm in hơn khung bị chặn in,
  và designer vẽ đúng như tem sẽ in (mã vuông bị bóp vào khung, PDF417 tràn khỏi
  khung).
- Chữ dưới mã 1D theo X được kiểm theo chiều rộng của chính dòng đó; đổi máy in
  hoặc DPI thì designer vẽ lại mã vạch; nhiều dòng tràn mép tem được gộp thành
  một mục “Dòng 2, 3 …”.
- Giới hạn còn lại: khi trình điều khiển giữ thiết lập hoặc ép DPI khác, designer
  vẫn vẽ theo DPI của mẫu; Xem trước luôn đúng như bản in.

**Kiểm tra:**

- Build 0/0; unit 1210/1210; regression 261 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.
- Chưa có bằng chứng in vật lý mới.

## v0.314 — Báo ảnh hỏng ngay khi thiết kế

**Thiết kế (D07, bổ sung)**

- Ảnh có dữ liệu nhưng không đọc được trước đây chỉ hiện ô trống trên canvas và
  chỉ bị báo khi lệnh in bị chặn. Nay chip trên thanh trạng thái báo “1 ảnh hỏng”;
  di chuột lên chip để xem ảnh nào, bấm vào chip để chọn ảnh đó rồi chèn lại.
- Mỗi ảnh chỉ được đọc thử một lần, nên thao tác sửa không chậm đi.

**Kiểm tra:**

- Build 0/0; unit 1199/1199; regression 259 PASS / 0 FAIL. Smoke giao diện (UIA)
  không còn dừng vì lỗi COM thoáng qua của UI Automation.
- Chưa có bằng chứng in vật lý mới.

## v0.313 — Bảng thuộc tính báo cột trùng/trống tiêu đề ngay khi mở mẫu

**Dữ liệu (DB03, bổ sung)**

- Kiểm tra v0.312 trên bản cài cho thấy: khi mở mẫu có liên kết dữ liệu, cảnh báo
  máy in ghi đè dòng trạng thái báo cột trùng/trống tiêu đề, còn Bảng thuộc tính
  vẫn báo liên kết khớp. Người vận hành chỉ biết khi bấm in.
- Nay đối tượng đọc cột như vậy được báo ngay trong Bảng thuộc tính, ví dụ “Cột D
  có tiêu đề “Part No” trùng với cột A (ANLAbel tạm gọi là “Part No_2”).”, và
  được đánh dấu trong danh sách Lớp (“Cột chỉ theo vị trí: Part No_2”).
- Trường chuyển đổi dữ liệu không còn bị báo nhầm là “Thiếu trường”: mọi dòng in
  đều có trường đó.

**Kiểm tra:**

- Build 0/0; unit 1199/1199; regression 259 PASS / 0 FAIL. Gate ghi nhật ký dữ
  liệu không còn đỏ chập chờn khi máy bận (đọc log trong lúc app đang ghi).
- Chưa có bằng chứng in vật lý mới.

## v0.312 — Chặn dòng làm mã vạch tràn mép tem

**In ấn (B05)**

- Mã 1D đặt độ rộng theo vạch (“Size from X”) được in theo đúng chiều rộng mà dữ
  liệu của từng dòng cần, nên dòng dài hơn mẫu làm mã rộng ra quá khung. Trước
  đây kiểm tra trước khi in chỉ xét khung, nên một dòng có thể in ra mã bị cắt ở
  mép tem mà kiểm tra vẫn đạt. Nay dòng đó bị chặn, kể cả khi mã xoay, kèm cách
  sửa: rút ngắn dữ liệu, giảm độ rộng vạch hoặc dời mã vào trong tem.
- Chưa mẫu thật hay mẫu thư viện nào dùng chế độ này, nên mẫu đang dùng không bị
  ảnh hưởng.

**Kiểm tra:**

- Build 0/0; unit 1199/1199; regression 259 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.
- Chưa có bằng chứng in vật lý mới.

## v0.311 — Mã vạch trên màn hình đúng kích thước in; cột dữ liệu không trượt

Hai ID cuối cùng của quy chuẩn lõi còn “Một phần” ở mức phần mềm (B06, DB03) nay
đạt, mỗi ID có gate đo được.

**Thiết kế (B06, ADR-025)**

- Mã vạch trên designer giờ có đúng kích thước sẽ in. Trước đây designer vẽ ở
  DPI riêng của đối tượng (300) rồi kéo giãn vào khung, nên ở máy in 203 DPI mã
  trên màn hình to hơn tem thật: QR khung 16 mm hiện 13,8 mm trong khi tem có
  12,5 mm; Code 128 khung 45 mm hiện 33,9 mm vạch trong khi tem có 25,0 mm.
- Sau khi cập nhật, mã trên designer có thể trông nhỏ hơn trước. Đó là kích
  thước tem vẫn in ra từ trước tới nay; bản in và Xem trước không đổi.

**Dữ liệu (DB03, ADR-026)**

- Sau mỗi lần nhập hoặc Cập nhật Excel, dòng trạng thái nêu theo chữ cái cột:
  cột đang dùng không còn ở dòng tiêu đề (và lần trước nó ở cột nào), cột đang
  dùng có tiêu đề trống hoặc trùng, hoặc chỉ nơi tiêu đề trống/trùng.
- Đối tượng (hoặc cột số bản) đọc cột không có tiêu đề hoặc có tiêu đề trùng bị
  chặn in, vì thêm, bớt hay đảo cột trong tệp có thể làm tem in nhầm cột mà
  không ai thấy. Cách sửa: đặt tiêu đề riêng cho cột trong tệp rồi Cập nhật
  Excel. Cột có tiêu đề riêng không bị ảnh hưởng.

**Kiểm tra:**

- Build 0/0; unit 1199/1199; regression 258 PASS / 0 FAIL (gồm bảy gate
  Text/TextBox). Mutation gate không bắt buộc: không đổi contract Core nào trong
  danh sách mutation.
- Chưa có bằng chứng in vật lý mới.

## v0.310 — Hoàn tất kiểm tra toàn ứng dụng: đóng các lỗ hổng còn sót

Đối chiếu lại năm báo cáo kiểm tra với các bản sửa của v0.309; những phần còn sót:

**Dữ liệu và tệp**

- Mở bản phục hồi rồi lưu: nếu tệp gốc đã được cửa sổ hoặc máy khác lưu sau khi
  ANLAbel đóng bất thường, ANLAbel hỏi trước khi ghi đè (trước đây ghi đè không hỏi).
  Bản phục hồi của phiên bản cũ vẫn mở như trước.
- Trường chuyển đổi dữ liệu trong mẫu và danh sách nguồn dữ liệu dùng chung giữ
  nguyên các trường do bản ANLAbel mới hơn ghi.
- Báo cáo lỗi đầu tiên của một phiên luôn được giữ, kể cả khi có hàng loạt lỗi
  tiếp theo.

**In ấn**

- Khi đã gửi lệnh in nhưng không ghi được kết quả vào nhật ký, In nhanh và Xem
  trước báo rằng lệnh sẽ hiện trong Trung tâm in như chưa rõ kết quả và cần kiểm
  tra tem trước khi in lại.
- Kiểm tra máy in đã lưu dừng chờ sau 20 giây khi Print Spooler treo và báo
  “Không kiểm tra được máy in đã lưu”; không chồng thêm luồng chờ.
- Đóng ANLAbel khi đang gửi lệnh in nhanh sẽ hỏi trước (mặc định “Chờ in xong”);
  cài bản cập nhật cũng chờ in xong.
- Giữ nguyên có chủ đích: Hủy trong Xem trước vẫn chờ trình điều khiển trả lời
  thay vì chồng thêm luồng; nút “Đóng” luôn dùng được.

**Kho mã**

- Bốn mẫu thư viện (20, 21, 23, 24) đổi tên tệp, không còn tên khách hàng trong
  DLL; bộ lọc kiểm cả tên tệp, tên mẫu và tên đối tượng. Ngừng theo dõi PDF hướng
  dẫn NiceLabel còn sót.

**Kiểm tra:**

- Build 0/0; unit 1189/1189; regression 256 PASS / 0 FAIL. Mutation gate không bắt
  buộc: không đổi contract Core nào trong danh sách mutation.
- Chưa có bằng chứng in vật lý mới.

## v0.309 — Kiểm tra toàn ứng dụng: không mất dữ liệu, in đúng thứ đã xem, giao diện tiếng Việt

**In ấn**

- Xem trước so dữ liệu với lần nhập thật (trước đây so tệp với chính nó): tệp
  Excel đổi sau khi nhập thì Xem trước không in dữ liệu cũ.
- Lỗi xảy ra trong lúc gửi lệnh (spooler khởi động lại, rút cáp) được ghi là
  “chưa rõ kết quả” và đưa vào Trung tâm in để đối soát. ANLAbel không tự in lại.
- In nhanh:
  - từ chối mẫu có trường dữ liệu khi chưa có dòng dữ liệu (trước đây in ra chữ
    “{Trường}” và mã hóa nó vào mã vạch);
  - hai lệnh in nhanh dùng chung một khóa;
  - giới hạn 100.000 tem như Xem trước;
  - nêu các dòng có số bản bằng 0.
- Xem trước không in thiết lập máy in hoặc lựa chọn mà người vận hành chưa thấy:
  làm mới và báo lại.
- Danh sách máy in không còn treo khi Print Spooler bị treo: sau 20 giây có thông
  báo; bấm lại không chồng thêm luồng.
- Lịch sử in ghi đúng mẫu đã in (không phải bản sửa trong lúc gửi) và được dựng
  ngoài luồng giao diện. Làm mới Xem trước ngay sau khi in không còn làm mất lịch sử.
- Gỡ các lối in cũ nằm ngoài đường in có theo dõi (không còn nơi nào gọi).

**Dữ liệu và tệp**

- Một lần nhập bị thay thế không còn xóa dữ liệu mới hơn. Khôi phục liên kết thất
  bại không còn làm “Cập nhật Excel” báo nhầm “đã mới nhất”. Nguồn dùng chung chỉ
  được lưu vào mẫu sau khi nhập thành công.
- Tệp dữ liệu bị ghi lại trong lúc đọc bị từ chối, kèm giải thích.
- Danh sách nguồn dữ liệu dùng chung gộp thay đổi của cửa sổ khác. Tệp không đọc
  được hoặc do bản mới hơn ghi thì không bị ghi đè.
- Cập nhật phần mềm, đăng xuất Windows và mở mẫu chậm không còn làm mất thay đổi
  chưa lưu. Bản phục hồi chỉ bị xóa khi đã có bản mới.
- Mẫu do bản ANLAbel mới hơn ghi giữ nguyên các trường lạ ở mọi cấp. Tệp chính
  không đọc được được lưu trữ trước khi bị ghi đè. Lưu đè lên bản mà cửa sổ hoặc
  máy khác vừa lưu sẽ hỏi trước.
- Cài đặt (giao diện, trình thiết kế, máy in) ghi nguyên tử. Lịch sử in và nhật ký
  lệnh in chịu được mất điện và cửa sổ khác đang ghi. Bản lưu trữ phiên bản vừa tạo
  luôn được giữ, kể cả khi đồng hồ bị lùi; một bản cũ không xóa được không còn làm
  hỏng việc lưu. Tên nhật ký lỗi không trùng nhau.

**Giao diện tiếng Việt**

- Tiếng Việt ở: thanh trạng thái, chip máy in, thông báo in và trạng thái hàng
  đợi, lỗi mở/lưu mẫu, kiểm tra liên kết Excel, Bảng thuộc tính (tên loại đối
  tượng, danh sách chọn, lỗi công thức và mã vạch), bảng thiết kế (lời nhắc vẽ,
  nhãn hít, tên đối tượng mới), Xem trước, Cài đặt máy in, Trung tâm in, Lịch sử
  in, Phiên bản mẫu tem, cập nhật phần mềm, tên mẫu trong Thư viện và Hướng dẫn sử
  dụng.
- Thông điệp lõi giữ tiếng Anh cho nhật ký và bằng chứng; chi tiết kỹ thuật để
  riêng. Gate ngôn ngữ neo từng quy tắc vào thông điệp lõi.
- Chưa đổi (thuộc hợp đồng Text/TextBox, cần chủ sản phẩm duyệt): banner và các
  tùy chọn của Ô chữ trong Bảng thuộc tính.

**Kho mã**

- Ngừng theo dõi dữ liệu khách hàng và ảnh tài liệu bên thứ ba; chúng vẫn còn
  trong lịch sử cũ. Thêm `.gitattributes`; quy tắc bỏ qua neo ở thư mục gốc. Kiểm
  tra mẫu thật bỏ qua khi máy không có thư mục. Bộ lọc tên khách hàng chỉ lưu mã băm.

**Kiểm tra:**

- Build 0/0; unit 1188/1188; regression 252 PASS / 0 FAIL. Mutation gate không
  bắt buộc: không đổi contract Core nào trong danh sách mutation.
- Chưa có bằng chứng in vật lý mới. Chưa thử trên máy 4 GB và Windows scaling
  125/150 %.

## v0.308 — In báo rõ, dữ liệu không in sai, thiết kế nhẹ hơn (kế hoạch 13, GĐ3–GĐ5)

**In ấn (GĐ5)**

- In nhanh dùng một bản chụp mẫu cho contract, kiểm tra, manifest và lệnh gửi
  (như Xem trước). Contract và kiểm tra chạy ngoài luồng giao diện.
- Danh sách máy in đọc trên luồng riêng, không đứng hình. Khi spooler tắt, app báo
  đúng là spooler tắt.
- Excel đang mở không còn gây chặn nhầm “dữ liệu đã đổi”.
- Lỗi xảy ra sau khi hàng đợi đã nhận lệnh được ghi là “chưa rõ kết quả”, không
  phải “thất bại”. ANLAbel không tự in lại.
- Nhật ký lệnh in:
  - chỉ đọc lại khi tệp thay đổi;
  - dòng cuối ghi dở (mất điện) được tự cất sang tệp riêng;
  - có nút lưu trữ nhật ký hỏng;
  - nhật ký trên 16 MB mà mọi lệnh đã xong được lưu nguyên tệp và bắt đầu tệp mới.
- “Xem lại” chỉ còn lệnh in chưa rõ kết quả (ADR-023). Dòng đã gửi được đánh dấu
  để cảnh báo in trùng.
- In thử không còn sửa mẫu đang mở khi bấm Hủy.
- Thông báo dừng in và chặn in là tiếng Việt: nêu vấn đề, dòng dữ liệu hoặc đối
  tượng, việc cần làm. Chi tiết kỹ thuật để riêng.

**Dữ liệu (GĐ4)**

- Ô có công thức nhưng tệp chưa lưu giá trị đã tính trước đây in thành ô trống;
  nay việc nhập dừng lại và nêu đúng ô.
- Dòng CSV thiếu hoặc thừa trường so với tiêu đề bị chặn và báo số dòng (ADR-024),
  vì trước đây giá trị có thể bị dồn sang cột khác.
- Lỗi nhập hoặc Cập nhật Excel được báo bằng tiếng Việt: tệp bị khóa, hết thời gian,
  thiếu sheet, ô lỗi. Thông báo nêu tệp, sheet, ô và cách sửa.
- Dòng tiêu đề và đường dẫn chỉ được ghi sau khi đọc thành công; nhập lỗi giữ nguyên
  liên kết đang dùng.
- Hai lần nhập chồng nhau: chỉ lần mới nhất được dùng.
- In nhanh băm lại tệp liên kết ngay trước khi in.
- Số dòng và thứ tự in theo tệp nguồn, không đổi khi sắp xếp bảng.

**Thiết kế (GĐ3)**

- Undo/Redo không còn âm thầm trả liên kết dữ liệu về giá trị cũ. Dán nhiều đối
  tượng là một bước undo.
- Thanh trạng thái báo phông chữ chưa cài và ảnh không có dữ liệu; bấm để chọn đối
  tượng.
- Lịch sử undo giữ mỗi ảnh một lần.
- Ảnh trên 10 MB, trên 25 megapixel hoặc không đọc được bị từ chối khi chèn.
- Kéo hoặc đổi kích thước không còn làm mới toàn bộ bảng thuộc tính ở mọi nhịp, và
  không còn kiểm lại mọi mã vạch ở mỗi khung hình (200 đối tượng: 48,7 → 6 ms/nhịp).
- Menu chuột phải và thông báo trạng thái của canvas là tiếng Việt; bỏ mục dành
  cho lập trình viên.
- Nhóm mã vạch: “Mã vạch 1D”, “Mã 2D”. Cả 15 loại đã có fixture dương/âm và decode
  round-trip, nên không loại nào bị ẩn (QĐ-8).

**Kiểm tra:**

- Build 0/0; unit 1175/1175; regression 243 PASS / 0 FAIL.
- Đo trên máy phát triển: nhập 10.000 dòng XLSX 0,45 s; mở Xem trước 1.000 tem
  2,2 s. Bảng “Measured limits” nằm trong `docs/LOCAL_LABEL_PRODUCT_CONTRACT.md`.
- Mẫu thật (`form tem nhãn`): cả 5 mở, lưu, vẽ được. Hai mẫu 75×100 qua kiểm tra
  với driver XP-DT426B (chỉ đọc). Hai mẫu 100×70 bị chặn vì khung chữ dòng cuối dư
  0,18 mm; việc này chờ chủ sản phẩm quyết định.
- Chưa có bằng chứng in vật lý mới. Chưa thử trên máy 4 GB và Windows scaling
  125/150 %.

## v0.307 — Không mất thiết kế; gọn phạm vi; một nguồn việc (kế hoạch 13, GĐ0–GĐ2)

**Vòng đời tệp (GĐ2, F01–F07)**

- Tiêu đề hiện `*` khi mẫu có thay đổi chưa lưu; Undo về đúng bản đã lưu thì
  hết `*`.
- Tạo mới, Mở, Mở từ thư viện, Phiên bản và Đóng hỏi *Lưu / Không lưu / Hủy*.
- Có nút “Lưu thành…” (Ctrl+Shift+S). Ctrl+N/O/S nay hoạt động đúng như tooltip.
- Lỗi bất ngờ:
  - ghi `logs/crash-*.log`;
  - lưu bản phục hồi;
  - báo bằng tiếng Việt, kèm lựa chọn “Tiếp tục làm việc” hoặc “Đóng ANLAbel”.
- Lệnh bất đồng bộ thất bại (Cập nhật Excel, In, Liên kết lại…) được báo cho người
  vận hành và ghi `logs/errors.log`, không còn im lặng.
- Bản phục hồi tự động:
  - lưu mỗi 60 giây khi có thay đổi;
  - lần mở sau đề nghị khôi phục;
  - tệp gốc không bị ghi đè.
- `data-sources.json` hỏng hoặc có schema mới hơn được cất sang
  `.hong-<thời gian>`; app vẫn mở.
- Mở đúp tệp `.anlabel` trong Explorer sẽ mở đúng tệp đó.
- Lưu từ bản chụp và không cho lưu chồng. Mọi lỗi lưu được báo rõ; vẫn giữ quy tắc
  không ghi đè khi chưa lưu trữ được bản trước.
- Model bỏ qua tọa độ hoặc kích thước NaN/vô cực.

**Gọn phạm vi (GĐ1)**

- Gỡ 9 cửa sổ không còn đường mở, automation thả file, NiceLabel JOB runner, bản
  trial/commercial. Mã được giữ ở nhánh `archive/automation-2026-10`.
- 42 tài liệu ngoài phạm vi chuyển vào `docs/archive/`.

**Khóa nền (GĐ0)**

- Worktree được commit. Test không còn ghi vào thư mục dữ liệu thật của người dùng.
- Log phụ trợ có xoay vòng.
- AGY dispatcher chỉ lấy việc từ mục 9 của kế hoạch 13.
- Script deploy đóng app nhẹ nhàng thay vì kill.

**Kiểm tra:**

- Build 0/0; unit 1112/1112; regression 220 PASS / 0 FAIL.
- `scripts/Invoke-ANLAbelLifecycleSmoke.ps1` trên app thật: hỏi lưu, Hủy, Không
  lưu, sập rồi khôi phục, lưu khi đóng — 15/15 đạt.
- Chưa có bằng chứng in vật lý mới.

## v0.306 — Ghi bù 0.289–0.306: copies, độ mới dữ liệu, bằng chứng job, preview trust

Ghi bù ngày 2026-10-13 khi commit lần đầu (kế hoạch 13, mục 0.2). Các version
0.289–0.306 được tăng trong worktree mà không có changelog hay commit. Mục này
dựng lại từ mã ngày 2026-10-04/05; các version trung gian không còn tách được.

**Dữ liệu và số bản**

- DB08/DB10 `CopiesFieldContract`: ô copies không hợp lệ ở dòng được chọn chặn cả
  batch, kèm chẩn đoán dòng và field. Không lặng lẽ bỏ dòng.
- `QuickPrintQuantity`: in nhanh dùng cùng quy tắc copies với xem trước. Copies
  rỗng, không nguyên, âm hoặc vượt giới hạn chặn batch. Số bản nhập tay không bị
  cắt ngầm.
- DB06 `ExcelDataFreshnessPolicy`: refresh chỉ bỏ qua bước đọc lại khi danh tính
  nội dung khớp. Riêng mtime khớp không đủ.
- Bổ sung test cho Excel/CSV: encoding nghiêm ngặt, dò delimiter, ô lỗi, payload
  số, giữ nguyên payload.

**In và xem trước**

- P08 `PrintJobEvidencePolicy` và `PrintJobEvidenceAppend`:
  - Lỗi ghi bằng chứng bắt buộc trước khi gửi thì chặn.
  - Lỗi ghi log sau khi spool chấp nhận không tạo Failed giả và không retry.
- P03 `PreviewInitializationTrust`, `PrintPreviewContracts`: Ready gắn với một
  snapshot đã chuẩn bị. Scene lỗi thì không Ready.
- `PrinterSelectionApplier`: Printer Setup và Xem trước ghi queue, page source,
  media và kích thước mm cùng một lúc.
- `PrintTicketXmlFingerprint` (từ v0.288) và các contract đọc-only cho driver,
  hàng đợi và media.

**Mã vạch**

- B01: test render cho check digit mã bán lẻ.
- Round-trip GS1-128 FNC1 và round-trip barcode nhập từ dữ liệu.

**Giao diện và công cụ**

- Việt hóa phần còn lại: `Localization/UiCatalog`, `SettingsWindow`.
- NiceLabel JOB runner: chỉ test gọi tới; theo kế hoạch 13 sẽ rời bản phát hành.
- Công cụ AGY dispatcher, `PrinterCapabilityProbe`.

**Kiểm tra trước commit (2026-10-13):** build 0 cảnh báo/0 lỗi; unit 1.129/1.129;
regression 211 PASS/0 FAIL. Chưa có bằng chứng in vật lý.

## v0.288 — Seagull ticket clock no longer invalidates print preparation

- Reproduced Xprinter XP-DT426B 75 × 100 mm / 203 DPI preview/preparation
  fingerprint drift without dispatching a job. Seagull `JobTicketProperties`
  / `strSeagullDriver` / `CreationTime` changes when the driver reads/merges
  a ticket, although print settings are unchanged.
- Fingerprint normalization now ignores only that metadata value at its exact
  namespace-qualified path. Driver version, private settings, media and DPI
  remain part of the identity; unknown properties are retained. The actual
  PrintTicket sent to the driver is not modified.
- Regression tests distinguish clock-only changes from setting/version and
  similarly named extension changes. Driver checks exercise preview, prepare
  and final validation across clock boundaries without physical printing.
- Existing job fingerprints remain historical evidence; reopen old previews
  to prepare with the new identity. No stored jobs or documents are migrated.
  Text/TextBox behavior is unchanged; physical output remains unverified.
- Verification on 2026-09-15: solution build 0 warnings/errors; 1,039 unit
  tests passed; application regressions 201 passed / 2 failed (printer setup
  orientation expected height 50 but observed 75; legacy thin linear barcode
  expected blocking preflight but observed success). All seven protected
  Text/TextBox gates passed. Configured mutation gate: 94.80%; it does not
  cover the new fingerprint helper. Three delayed real-driver preparation
  cycles had identical preview/prepared/final hashes. The full Fast gate
  remains red because of the two application failures; no release claim.

> Historical implementation log. Use the [live plan](docs/reinvention/07-execution-plan.md)
> for next work and [reference policy](docs/REFERENCE_POLICY.md) for competitor
> research. Old proposals and unchecked phases are not assignments. Accepted
> ANLAbel contracts, especially Text/TextBox, remain protected by AGENTS.md.

## Pure industrial uniform vector icons & pixel-perfect geometry (0.266)

- Rewrote all 40 application icons using 100% pure mathematical vector geometry (replacing string-based text rendering to guarantee zero font-metric distortion or alignment drift).
- Established a unified industrial color system matching professional engineering suites (Solid Dark Slate `#1E293B` outlines, Industrial Blue `#1D4ED8` primary actions, and calibrated spot accents: Amber `#D97706`, Emerald `#059669`, and Crimson `#DC2626`).
- Pixel-perfect optical centering on a $48\times 48\text{ px}$ grid with $40\times 40\text{ px}$ active bounding box and uniform $2.0-2.4\text{ px}$ stroke weights.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## High-contrast non-inverted icon color palette & optical alignment (0.265)

- Re-rendered all 40 icons with solid, high-contrast foreground ink and vibrant semantic accents, completely eliminating inverted/washed-out light-blue strokes on light button backgrounds.
- Calibrated optical centering and bounding box geometry ($48\times 48\text{ px}$ canvas with uniform $40\times 40\text{ px}$ optical target and $2.0-2.6\text{ px}$ stroke weights).
- Added `RenderOptions.BitmapScalingMode="HighQuality"`, `SnapsToDevicePixels="True"`, and `UseLayoutRounding="True"` to all button and header icon containers.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## App-wide modern vector icons & distinct semantic color coding (0.264)

- Redesigned and generated all 40 application vector icons with high-DPI anti-aliased geometry, distinct color coding, and semantic clarity.
- Eliminated dull inverse monochrome duplicates; organized functional domains by vibrant hues (Sapphire/Amber for Files, Coral/Emerald for Undo/Redo & History, Crimson for Delete, Obsidian/Cyan for Selection & Snapping, Royal Purple/Indigo for Text/Box, Emerald for Excel, Azure for Database, Obsidian/Blue for Thermal Printing, and Slate/Cyan for Settings).
- Refined `IconImage` and button scaling styles with `HighQuality` bicubic filtering and `SnapsToDevicePixels`.
- Added standalone `tools/IconGenerator` project for automated icon rendering.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## ITF-14 & linear barcode Bearer (guard) bars (0.263)

- Added `BearerBarStyle` enum (`None`, `TopBottom`, `Frame`) and `BearerBarThicknessMm` to `LabelObject`, `LabelObjectCloner`, `SceneObjectSnapshot`, and `BarcodeRenderOptions`.
- Vector and raster barcode renderers draw top/bottom and enclosing box frames for shipping container and master carton packaging barcodes.
- Added Bearer Bars and Bearer thickness controls to barcode properties panel in `MainWindow.xaml`.
- Preflight validation enforces linear-only restriction and thickness range (0.2–5.0 mm).
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## Print method: Graphic vs Printer-native (0.262)

- Added `PrintMethod` enum (`ApplicationGraphic`, `PrinterNative`) to `PrinterProfile`.
- Manifest and machine-parseable `PrintOperationLogEntry` explicitly record `PrintMethod` and `NativeCommandsUsed` metadata without silent fallback.
- Added Print Method setting to `PrintPreviewWindow` setup panel.
- Preflight validation fails closed with actionable diagnostic when `PrinterNative` is selected without an active direct hardware command driver.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## GS1 AI registry growth & logistics standards (0.261)

- Extended `Gs1AiRegistry` with standard supply-chain / logistics AIs: variants (20, 22), secondary serial/source (250, 251), monetary amounts & prices (390x, 391x, 392x, 393x), country codes & subdivisions (423, 427), NATO stock (7001), meat cuts (7002), potency (7004), asset & service IDs (8003 GRAI, 8004 GIAI, 8006 ITIP, 8018/8019 GSRN).
- Fail-closed preflight validation for all newly added AI structures, lengths, and check digits.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## 2D industrial parity (QR & Data Matrix) (0.260)

- Polished 2D barcode authoring terminology in Properties panel (`2D Mode`, `ECC level`, `Symbol version`, `Module size (px)`).
- Unified 2D matrix module sizing and preflight checks across QR Code and DataMatrix (`IsSquare2DCodeLike`).
- Sub-2-dot fixed modules at print DPI fail closed with actionable diagnostics.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## Code 39 wide:narrow ratio & physical quiet zone (0.259)

- Added Code 39 wide:narrow ratio selection (`Code39WideNarrowRatio`, `Code39RatioContract`) supporting `2.0:1`, `2.2:1`, `2.5:1`, `3.0:1`.
- SizedFromX / render pipeline reweights Code 39 wide and narrow runs with exact ratio mathematics.
- Preflight validation enforces USS-39 / ISO 16388 rules (Ratio 2.0:1 requires effective X >= 0.508 mm).
- Added physical quiet zone readout (mm) at real print-plan DPI with fail-closed preflight against standard minimums (`max(10X, 2.54 mm)` per side).
- Updated Barcode Properties panel in MainWindow with Code 39 ratio selector and physical quiet zone readout.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## GitHub release auto-update (0.258)

- Added GitHub Releases auto-update checker and downloader (`AppUpdateService`, `GitHubReleaseParser`, `UpdateWindow`).
- Integrated "Update" button on MainWindow Ribbon (Help section) and "Check for Updates" button in User Guide HelpWindow (About section).
- Asynchronous non-blocking GitHub API query with semver tag parsing, changelog notes display, setup installer download with progress bar, and automated installer launch.
- Fallback to browser release URL on network/installer errors or missing setup asset.
- Text/TextBox industrial contract, canvas layout, and print pipeline unchanged.

## Excel alignment icons replace dropdowns (0.251)

- Horizontal Left/Center/Right/Justify and vertical Top/Middle/Bottom
  are 24 px Excel paragraph icons (`26:5`). No Align/Vertical ComboBox
  in Text Style. `TextStyleAlignmentContract` owns exclusive on-state.
- Text/TextBox contract unchanged.

## Excel icon font toolbar + licensed catalog (0.250)

- B/I/U are a 24 px segmented Excel icon group (`Properties.TextStyle.IconGroup`).
- Font list is `TextStylePickerCatalog.LicensedFamilies` only: Windows
  inbox + SIL/Apache faces if installed. Unlicensed machine fonts
  (`CustomerBrand`, `Comic Sans MS`, `Arial Narrow`) stay out.
- Figma: `kqyNBI0DgRHnPzJTDBIui5` node `25:5`. Text/TextBox contract
  unchanged.

## Excel-like font and size picker (0.249)

- Properties Text Style is one compact row: typeable font family with
  live family preview, typeable point size (4–200 via
  `TextStylePickerCatalog`), and B/I/U toggles.
- Figma authority: `kqyNBI0DgRHnPzJTDBIui5` node `24:5`
  (`ANLAbel — Excel-like font size picker v0.249`). Handoff:
  `docs/TEXT_STYLE_PICKER_UI_HANDOFF.md`.
- Text/TextBox industrial contract, padding presets and fit modes are
  unchanged.

## Mutation scope adds LabelGuideContract (0.248)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector`, `Code39RatioContract`,
  `ImageResolutionContract`, `LinearBarcodeModuleContract`,
  `MatrixSquareModuleFit`, `BarcodeCheckDigitContract`, `BarcodeHriLayout`,
  `LineBoundsContract` and `LabelGuideContract`. Thresholds stay
  `high/low/break = 90`.
- Combined mutation score `94.80%` (654 killed, 31 survived, 2 timeout).
  `LabelGuideContract` scored `91.67%` of its tested mutants (44 killed,
  4 survived). Leftover survivors are equivalent `length <= 0` vs `< 0`,
  coverage-based `ThrowIfNull`, and a NaN probe early-return that still
  yields null.
- Tests cover clamp axes, non-finite length, 3-decimal AwayFromZero,
  IsValid bounds, 8 DIP hit window via `MmConverter`, locked/hidden
  filters, ordinal and position order. Text/TextBox unchanged.

## Mutation scope adds LineBoundsContract (0.247)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector`, `Code39RatioContract`,
  `ImageResolutionContract`, `LinearBarcodeModuleContract`,
  `MatrixSquareModuleFit`, `BarcodeCheckDigitContract`, `BarcodeHriLayout`
  and `LineBoundsContract`. Thresholds stay `high/low/break = 90`.
- Combined mutation score `95.03%` (610 killed, 27 survived, 2 timeout).
  `LineBoundsContract` scored `100%` of its tested mutants (50 killed).
- Tests cover half-stroke hull, OutlineStyle.None, Dash/Dot, reversed and
  vertical endpoints, 0,0 width/height fallback on both LabelObject and
  SceneObjectSnapshot, single-zero real endpoints, non-finite coordinates
  and non-positive stroke. Text/TextBox unchanged.

## Mutation scope adds BarcodeHriLayout (0.246)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector`, `Code39RatioContract`,
  `ImageResolutionContract`, `LinearBarcodeModuleContract`,
  `MatrixSquareModuleFit`, `BarcodeCheckDigitContract` and
  `BarcodeHriLayout`. Thresholds stay `high/low/break = 90`.
- Combined mutation score `94.61%` (559 killed, 27 survived, 3 timeout).
  `BarcodeHriLayout` scored `100%` of its tested mutants (58 killed).
- Tests cover Disabled clamp, unsupported placement, non-finite frame/ink,
  application font bounds 5–20 pt, width slack 0.001 mm, 0.5 mm bar floor,
  Below/Above frame occupancy and the bool `showHri` mapping. Text/TextBox
  unchanged.

## Mutation scope adds BarcodeCheckDigitContract (0.245)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector`, `Code39RatioContract`,
  `ImageResolutionContract`, `LinearBarcodeModuleContract`,
  `MatrixSquareModuleFit` and `BarcodeCheckDigitContract`. Thresholds stay
  `high/low/break = 90`.
- Combined mutation score `94.03%` (502 killed, 27 survived, 2 timeout).
  `BarcodeCheckDigitContract` scored `96.20%` of its tested mutants; the 2
  survivors are equivalent `All`/`Any` and `||`/`&&` on the ITF non-digit
  early-out (both paths still fail closed).
- Tests drive public `Validate`, `FormatHriText`, `ComputeCode39CheckDigit`,
  `ComputeItfCheckDigit` and `HasValidTrailingCheckDigit` with published
  Mod-43 / Mod-10 identities, empty/unsupported symbology fail-closed and
  length-2 hide/verify boundaries. Text/TextBox unchanged.

## Mutation scope adds MatrixSquareModuleFit (0.244)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector`, `Code39RatioContract`,
  `ImageResolutionContract`, `LinearBarcodeModuleContract` and
  `MatrixSquareModuleFit`. Thresholds stay `high/low/break = 90`.
- Combined mutation score `93.65%` (426 killed, 25 survived, 2 timeout).
  `MatrixSquareModuleFit` leftover survivors are the unused shrink-while
  safety net (no integer-dot fixture overshoots the floor) plus equivalent
  `±1e-9` arithmetic.
- Tests cover square/non-square DPI, X-limited vs Y-limited frames, 1-dot
  tight frame and fail-closed native/frame/DPI. Text/TextBox unchanged.

## Mutation scope adds LinearBarcodeModuleContract (0.243)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector`, `Code39RatioContract`,
  `ImageResolutionContract` and `LinearBarcodeModuleContract`. Thresholds stay
  `high/low/break = 90`.
- Combined mutation score `94.90%` (372 killed, 20 survived).
  `LinearBarcodeModuleContract` scored `95.38%` of its tested mutants; the 3
  survivors are the equivalent `+ 1e-9 <` vs `<=` floor compare and two
  interpolated message fragments.
- Tests cover 2-dot/300 DPI floor-only risk, `HasIndustrialRisk` either-flag,
  exact 0.19 mm floor, Estimate/SizedFromX/OnePrinterDot fail-closed and
  exception messages. Text/TextBox unchanged.

## Mutation scope adds ImageResolutionContract (0.242)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector`, `Code39RatioContract` and
  `ImageResolutionContract`. Thresholds stay `high/low/break = 90`.
- Combined mutation score `94.80%` (310 killed, 17 survived).
  `ImageResolutionContract` scored `100%` of its tested mutants.
- Tests cover 25.4 mm = 1 inch PPI identity, independent axes, device-grid
  epsilon, `IsValid` fail-closed fields and exception messages on Observe.
  Text/TextBox unchanged.

## Mutation scope adds Code39RatioContract (0.241)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState`, `SnapCandidateSelector` and `Code39RatioContract`.
  Thresholds stay `high/low/break = 90`.
- Combined mutation score `93.73%` (254 killed, 17 survived).
  `Code39RatioContract` scored `100%` of its tested mutants.
- Tests cover USS-39 0.508 mm / 2.54 mm bounds, every authored ratio,
  unsupported enum, 10X-or-floor quiet zone, non-negative observed modules
  and the Ratio 2.0 epsilon boundary. Text/TextBox unchanged.

## Mutation scope adds SnapCandidateSelector (0.240)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract`,
  `SnapHysteresisState` and `SnapCandidateSelector`. Thresholds stay
  `high/low/break = 90`.
- Combined mutation score `93.03%` (227 killed, 17 survived).
  `SnapCandidateSelector` scored `94.74%` of its tested mutants; the 1
  survivor is the `ThrowIfNull` statement under coverage-based testing.
- Tests cover Delta, priority-over-distance, closer-then-ordinal tie-break,
  zero distance, exact acquire boundary, negative distance, null input and
  non-positive/non-finite acquire tolerance. Text/TextBox unchanged.

## Mutation scope adds SnapHysteresisState (0.239)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract`, `SnapGridContract` and
  `SnapHysteresisState`. Thresholds stay `high/low/break = 90`.
- Combined mutation score `92.89%` (209 killed, 16 survived).
  `SnapHysteresisState` scored `100%` of its tested mutants.
- Tests cover hold inside the release window, ignore a competing candidate,
  exact boundary, zero-tolerance exact-only hold, negative tolerance fail-closed
  and Reset. Text/TextBox unchanged.

## Mutation scope adds SnapGridContract (0.238)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract`, `NudgeStepContract` and `SnapGridContract`.
  Thresholds stay `high/low/break = 90`.
- Combined mutation score `92.52%` (198 killed, 16 survived).
  `SnapGridContract` scored `100%` of its tested mutants.
- Tests cover default/min/max step, AwayFromZero midpoints, non-finite
  position → origin, unsafe step fallback, exact acquire boundary and
  `+Infinity` tolerance reject. Text/TextBox unchanged.

## Mutation scope adds NudgeStepContract (0.237)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`,
  `SnapToleranceContract` and `NudgeStepContract`. Thresholds stay
  `high/low/break = 90`.
- Combined mutation score `91.88%` (181 killed, 16 survived).
  `NudgeStepContract` scored `100%` of its tested mutants.
- Tests cover named physical-mm steps, all four directions, unknown mode
  fallback and unknown direction no-move. Text/TextBox unchanged.

## Mutation scope adds SnapToleranceContract (0.236)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract`, `MediaDimensionContract`
  and `SnapToleranceContract`. Thresholds stay `high/low/break = 90`.
- Combined mutation score `91.75%` (178 killed, 16 survived).
  `SnapToleranceContract` scored `90%` of its tested mutants; the 1 survivor
  is equivalent (`< 0` vs `<= 0` on a zero DIP budget still yields 0 mm).
- Tests cover zoom clamp/fallback, acquire/release ratio, inverse zoom
  scaling and invalid screen budgets. Text/TextBox unchanged.

## Mutation scope adds MediaDimensionContract (0.235)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract`, `PrintableAreaContract` and `MediaDimensionContract`.
  Thresholds stay `high/low/break = 90`.
- Combined mutation score `91.85%` (169 killed, 15 survived).
  `MediaDimensionContract` scored `62.86%` of its tested mutants; the 13
  survivors are equivalent (zero/non-finite inputs still fail the later DIP
  comparison). Two DeviceDotQuantizer equivalents remain.
- Tests cover exact 25.4 mm = 96 DIP identity, each axis, zero tolerance,
  wider tolerance, swapped axes and non-finite/non-positive inputs.
  Text/TextBox unchanged.

## Mutation scope adds PrintableAreaContract (0.234)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer`,
  `EffectiveDpiContract` and `PrintableAreaContract`. Thresholds stay
  `high/low/break = 90`.
- Combined mutation score `98.66%` (147 killed, 2 equivalent DeviceDotQuantizer
  survivors). `PrintableAreaContract` scored `100%`.
- Strengthened `PrintableAreaContractTests` cover both axes, exact DIP
  tolerance, missing/invalid media, overflow, non-finite values and empty
  `FailureCode` → `unverified`. Text/TextBox unchanged.

## Mutation scope adds EffectiveDpiContract (0.233)

- Blocking mutate list is `MmConverter`, `DeviceDotQuantizer` and
  `EffectiveDpiContract`. Thresholds stay `high/low/break = 90`.
- Combined mutation score `97.40%` (75 killed, 2 equivalent DeviceDotQuantizer
  survivors). `EffectiveDpiContract` scored `100%`.
- Strengthened `EffectiveDpiContractTests` cover 0 / negative / 2400 / 2401
  on both axes. Text/TextBox and print-queue fail-closed behavior unchanged.

## QR fills the frame without distorting modules (0.232)

- Square 2D uses `MatrixSquareModuleFit`: integer module scale, leftover is even
  quiet-zone pad smaller than one module. Independent W/H nearest-neighbour
  stretch is gone.
- Preview/print draw the fitted bitmap centered; designer uses `Stretch.Uniform`.
- Linear SizedFromX and Text/TextBox are unchanged. Existing quiet-zone values stay.
- Tests: `MatrixSquareModuleFitTests`, `QrFrameFillContractTests` (including a
  non-square 40×28 mm box), `qr fills authored frame when object is enlarged`.

## QR fills the authored object frame (0.231)

- Matrix barcodes (QR / Data Matrix / Aztec / PDF417) encode the native module
  matrix (authored quiet zone only) then nearest-neighbour fit the exact object
  width/height. ZXing integer scale-and-center leftover no longer grows into a
  white ring when the operator drags the object larger.
- Preview/print draw that bitmap into the authored symbol rectangle.
- New QR objects default to a 2-module quiet zone (existing files keep authored
  values). Text/TextBox contract unchanged.
- Regressions: `QrFrameFillContractTests`,
  `qr fills authored frame when object is enlarged`.

## Mutation scope expansion (0.230)

- Blocking Stryker mutate list is `Geometry/MmConverter.cs` plus
  `Geometry/DeviceDotQuantizer.cs`. Thresholds stay `high/low/break = 90`.
- DeviceDotQuantizer tests now cover SnapDip idempotence, AwayFromZero
  midpoints, negative DPI/width and non-finite DIP. MmConverter midpoint
  rounding is asserted on the shipped `MmToPrinterDots` path.
- Header uniqueness and 1024×600 shell fixtures remain closed history.

## Designer header uniqueness + target-scale shell (0.229)

- Official working tree is `H:\00_REPOS_PROJECTS\ANLABEL`. Deleted C: Grok
  clones cannot create work. Only `docs/LOCAL_LABEL_PRODUCT_CONTRACT.md` and
  `docs/reinvention/07-execution-plan.md` are the active backlog.
- Figma `https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc` node `5:2`
  (`ANLAbel — Header uniqueness v0.229`) is the exact header authority.
- Quick Access + Ribbon: one command, one placement, unique PNG. Zoom stays on
  `Shell.Status.Zoom`. Snap-to-objects uses `snap_objects.png`; snap-to-grid
  uses `snap_grid.png`.
- Handoff: `docs/DESIGNER_HEADER_UNIQUENESS_UI_HANDOFF.md`.
- Regressions: `designer header commands are unique`,
  `designer shell layout at target scales`,
  `DesignerHeaderChromeContractTests.ShippedHeader_HasOneCommandPlacementAndUniqueGlyphs`.
- Text/TextBox ownership, wrap/clip and resize lifecycle unchanged.

## Atomic local data transforms

- `DataTransformPipeline` now treats a transform set as an all-or-nothing
  operation: any evaluation failure returns the unchanged source record and
  publishes no derived-field lineage.
- A derived output may not shadow a case-insensitive source field; that schema
  conflict fails before any formula evaluation.
- Print Preview receives the same fully transformed row set as Quick/Batch
  Print; a failed row blocks preview preparation rather than falling back to raw
  `DataView` values.
- A linked Excel source is identified by SHA-256 content, length and last-write
  metadata at import. Any different or unavailable identity blocks Preview and
  quick-print preparation; displayed pages remain snapshot evidence only.
- This prevents preview or print preparation from observing values created
  before a later invalid transform; cycle, parse and evaluation failures all
  remain fail-closed.
- Unit regression: `TransformPipelineDoesNotExposePartialValuesWhenAnyTransformFails`.

## Local automation prepared-batch identity

- L4 preparation now creates a payload-free immutable identity binding the
  detected event, source/configuration fingerprints, exact template scene hash
  and ordered prepared-record value hash.
- The identity is deterministic for the same inputs and changes when any record
  value changes; it creates no manifest, queue request or print operation.
- Regression: `automation prepared batch binds source configuration template and
  data identities`.

## Automation manifest provenance

- `PrintJobManifest` v3 binds optional local automation event/trigger,
  configuration/source and prepared-batch identities into its fingerprint.
- No raw file path or prepared record value is persisted; existing manifest v1/v2
  history remains readable through compatibility fingerprint validation.
- Unit regression verifies provenance tampering fails and raw row values do not
  appear in serialized manifest metadata.

## Automation exact preflight

- Prepared automation batches now call the shared preflight validator only when
  their configuration/data/template hashes, saved template queue and an injected
  named-queue lookup all agree.
- The service is evidence-only: it creates no manifest, durable job, queue call
  or file mutation. Missing/mismatched queues fail closed without default-printer
  fallback.

## Automation explicit dispatch and recovery

- Local automation records `Prepared -> Dispatching` before invoking its explicit
  dispatcher. The same event cannot enter that state twice, so duplicate
  notifications never cause a second invocation.
- Accepted submission ends in `Dispatched` without claiming physical output;
  rejection, exception, cancellation, or a restart during dispatch ends in
  `Blocked`. There is no automatic retry.
- Regression: `automation dispatch is explicit, durable and duplicate-safe`.

## Automation local archive and quarantine

- Archive/quarantine uses a durable moving state before a same-volume
  `File.Move`; cross-volume copy/delete is rejected.
- The source must be a non-link file within the configured watch root; the
  destination root must be outside it and contains an event-ID directory.
- Regression: `file-drop archive move is local, atomic and path-validated`.

## Automation history projection

- `AutomationJobHistoryStore` records an integrity-chained, payload-free link
  from automation event and prepared batch to the durable job ID and optional
  manifest fingerprint.
- Dispatch is finalized only after that link is durable; a dispatcher result
  without a job ID or an unwritable history link fails closed as `Blocked`.

## Current release override: single unrestricted local build (0.225)

- Product licensing code is removed: no trial clock, activation key, machine
  binding, activation dialog, runtime entitlement check or key generator remains.
- Release packaging produces one local Windows application through
  `build-release.ps1` and `installer/ANLAbel-x64.iss`.
- The legal `LICENSE` file and third-party notices remain distribution metadata;
  they do not gate application execution.
- Release gates verify there is no product-licensing source/project dependency
  and the single installer shares the canonical application version.

## Muc tieu Phase 1
- Tao ung dung Windows desktop WPF ten ANLAbel.
- Dung kien truc MVVM, tach model/service ro rang.
- Luu toa do va kich thuoc object bang mm, khong luu pixel.
- Tao label canvas theo kich thuoc that bang mm, preview chuyen doi mm sang WPF device-independent units.
- Ho tro object Text, Rectangle, Line.
- Ho tro chon, keo tha, resize va sua properties co ban.
- Luu/mo template JSON voi duoi `.anlabel`.

## Pham vi khong lam trong Phase 1
- Chua import Excel/CSV.
- Chua barcode/QR/Data Matrix.
- Chua in tem.
- Chua multi-select, ruler day du, undo/redo nang cao.
- Chua export PDF/PNG.

## Cau truc solution
- `src/ANLAbel.App`: WPF UI, Views, ViewModels, Controls.
- `src/ANLAbel.Core`: Models, Enums, Interfaces, Geometry, Commands dung chung.
- `src/ANLAbel.Project`: Service luu/mo file `.anlabel` bang JSON.
- `src/ANLAbel.Data`: Placeholder cho Excel/CSV Phase 2.
- `src/ANLAbel.Barcode`: Placeholder interface/engine barcode Phase 3.
- `src/ANLAbel.Printing`: Placeholder cho print pipeline Phase 4.
- `src/ANLAbel.Tests`: Test runner nhe cho conversion, expression, save/load.
- `docs`: Tai lieu kien truc va ghi chu in/barcode.

## Buoc thuc hien

### 1. Scaffold solution
- Tao `ANLAbel.sln`.
- Tao WPF project `ANLAbel.App`.
- Tao class library cho Core, Project, Data, Barcode, Printing.
- Tao test runner nhe `ANLAbel.Tests`.
- Them reference giua cac project.

### 2. Core models va geometry
- Tao `LabelTemplate`.
- Tao `LabelObject`.
- Tao `ObjectType`, `LabelOrientation`, `TextAlignmentMode`.
- Tao `MmConverter` cho:
  - mm -> WPF DIP theo 96 DPI.
  - mm -> printer dots theo DPI may in.
  - DIP -> mm cho thao tac keo tha/resize tren canvas.

### 3. Project service
- Tao `IProjectFileService`.
- Tao `ProjectFileService` dung `System.Text.Json`.
- Dam bao JSON doc duoc Unicode va object property ro rang.
- Luu/mo file `.anlabel`.

### 4. MVVM ha tang
- Tao `ObservableObject`.
- Tao `RelayCommand`.
- Tao `MainViewModel`.
- Tao selected object binding hai chieu voi properties panel.

### 5. Designer UI
- Tao `MainWindow`.
- Layout:
  - Toolbar tren cung.
  - Toolbox ben trai.
  - Canvas thiet ke o giua.
  - Properties panel ben phai.
- Them command:
  - New template.
  - Save.
  - Open.
  - Add Text.
  - Add Rectangle.
  - Add Line.
  - Zoom in/out.

### 6. Label canvas control
- Tao `LabelDesignerCanvas`.
- Ve label background theo width/height mm.
- Hien grid nhe theo mm.
- Render object theo toa do mm.
- Chon object bang click.
- Keo object bang mouse.
- Resize object bang grip goc duoi phai.
- Cap nhat model bang mm sau moi thao tac.

### 7. Properties panel
- Sua duoc:
  - X mm
  - Y mm
  - Width mm
  - Height mm
  - Text
  - Font size
- Disable khi chua chon object.

### 8. Documentation
- Tao `docs/architecture.md`.
- Tao `docs/print-calibration.md`.
- Tao `docs/barcode-notes.md`.
- Tao `docs/license-notices.md`.

### 9. Verification
- Build solution.
- Chay test runner:
  - mm conversion.
  - expression binding placeholder co ban.
  - save/load template.
- Neu co the, chay app bang `dotnet run --project src/ANLAbel.App`.

## Tieu chi hoan thanh Phase 1
- Build thanh cong.
- Mo duoc app WPF.
- Tao template moi theo width/height mm.
- Them Text/Rectangle/Line.
- Keo object tren canvas va toa do properties doi theo mm.
- Resize object va kich thuoc properties doi theo mm.
- Sua properties va object tren canvas cap nhat.
- Luu template `.anlabel`.
- Mo lai template va giu dung toa do/kich thuoc mm.

## Phase 2 - Excel Binding

### Pham vi theo dieu chinh moi
- Chi uu tien Excel `.xlsx` va `.xlsm`.
- Khong lam CSV trong buoc nay.

### Buoc thuc hien Phase 2
- Them `ClosedXML` vao `ANLAbel.Data`.
- Tao `ExcelDataService`:
  - Lay danh sach sheet.
  - Doc header dong 1.
  - Trim khoang trang.
  - Loai bo ky tu xuong dong trong cell.
  - Giu Unicode tieng Viet.
- Them nut `Import Excel` tren toolbar.
- Cho chon sheet khi import.
- Hien thi data bang DataGrid preview phia duoi.
- Cho chon mot row trong DataGrid.
- Them `BindingExpression` cho Text object.
- Khi chon row, Text object render theo cu phap `{ColumnName}`.

### Tieu chi hoan thanh Phase 2 buoc dau
- Import duoc `.xlsx/.xlsm`.
- Chon duoc sheet.
- Hien thi du lieu tren table preview.
- Chon dong preview lam tem cap nhat.
- Binding nhu `P{PartNo} Q{Qty} 1T{Lot}` hoat dong voi Text object.

## Phase 3 - Barcode

### Pham vi dang lam
- Dung `ZXing.Net` sau interface `IBarcodeRenderer`.
- Ho tro:
  - Code 128
  - QR Code
  - Data Matrix ECC200
- Render barcode thanh pixel buffer theo kich thuoc mm va DPI truyen vao.
- UI WPF chuyen pixel buffer thanh `BitmapSource` de preview.
- Khi du lieu binding hoac kich thuoc object thay doi, barcode duoc render lai.

### Tieu chi hoan thanh Phase 3 buoc dau
- Them duoc object Code 128, QR Code, Data Matrix tu toolbar/toolbox.
- Barcode lay du lieu tu `BindingExpression` neu co row Excel.
- Barcode lay static text khi khong binding Excel.
- Validate barcode data rong.
- Co test render Code 128, QR Code, Data Matrix.

### Dieu chinh barcode UI
- Toolbar/toolbox chi con mot nut `Barcode`.
- Chuan barcode duoc chon trong panel properties ben phai.
- Moi object barcode luu `BarcodeSymbology`.
- Danh sach barcode gom Code 128, QR Code, Data Matrix, Code 39, Code 93, EAN-13, EAN-8, UPC-A, UPC-E, ITF, Codabar, PDF417, Aztec, MSI, Plessey.

## Phase 4 - Printing

### Dinh huong san pham bat buoc
- ANLAbel duoc dinh huong truoc tien cho may in tem nhan cong nghiep va workflow tem san xuat/logistics.
- Moi quyet dinh lien quan den printer setup, khổ giay, orientation, DPI, calibration, paper feed, preview va print pipeline phai uu tien hanh vi cua may in tem nhan cong nghiep truoc may in van phong.
- Driver can duoc xem la nguon su that chinh cho kho giay/huong giay neu driver expose du lieu theo cach doc duoc.
- Khong duoc mac dinh chi can `PrintCapabilities.PageMediaSizeCapability` la du cho nhom may in nay.
- Neu WPF `PrintCapabilities` khong du, huong tiep theo hop ly la fallback doc danh sach kho giay qua API gan driver hon nhu `System.Drawing.Printing.PrinterSettings.PaperSizes`, sau do moi den driver preferences/DEVMODE, va cuoi cung moi la nhap tay neu van khong lay duoc.
- Khong dung lai danh sach kho giay hardcode cua phan mem cho luong printer setup chinh.

### Pham vi buoc dau
- In qua Windows PrintDialog / Windows printer driver.
- Them nut `Print` de in template voi row Excel hien tai.
- Them nut `Test Print` de in calibration ruler.
- Tao `PrintService` rieng trong `ANLAbel.Printing`.
- Tao `LabelVisualRenderer` render tu model mm sang WPF physical units.
- Barcode duoc render lai trong print pipeline theo DPI cua print ticket/profile.

### Nguyen tac da ap dung
- Khong in anh preview cua canvas.
- Khong luu toa do pixel.
- Page size tinh tu label width/height mm.
- Calibration visual co vach 10 mm de do sai lech thuc te.

### Viec can lam tiep trong Phase 4
- Them UI luu printer profile theo tung may in.
- In selected rows.
- Can test thuc te tren may in tem vi moi driver xu ly page media size khac nhau.

### Da bo sung trong Phase 4
- UI chinh printer profile:
  - Label width/height mm.
  - DPI.
  - Offset X/Y mm.
  - Scale X/Y.
- `Print All Rows` cho toan bo Excel preview.
- `Copies` co dinh.
- `Copy field` theo cot Excel, vi du `QtyPrint` hoac `{QtyPrint}`.
- Startup printer setup dialog:
  - Liet ke Windows printers bang `System.Printing`.
  - Uu tien printer co ten/driver giong may in tem nhan.
  - Lay paper size tu driver theo huong uu tien cho may in tem nhan cong nghiep, khong gioi han duy nhat o `PrintCapabilities.PageMediaSizeCapability`.
  - Chon paper size se set template width/height va printer profile.
  - PrintDialog tu chon lai printer da luu neu con ton tai.
- Print Preview bang `Ctrl+P`:
  - Preview tung tem/page theo row Excel.
  - 5 row Excel = 5 tem/page preview.
  - Panel output ben phai co printer name, driver setting va print mode.
  - Nut printer settings mo Windows PrintDialog de chon driver/output.
- Print history log:
  - Ghi log sau khi lenh in gui thanh cong.
  - Luu tap trung vao `%AppData%/ANLAbel/print-history.xlsx`.
  - Log ap dung cho moi template `.anlabel`.
  - Ghi template, printer, kich thuoc tem, DPI, mode in, so row, so label, file Excel va sheet.

## Dieu chinh UI/Designer sau Phase 4

### Da hoan thanh
- Dieu chinh lai logic Excel/database field theo huong dung workflow thiet ke tem:
  - Them model `DatabaseField` gom Name, DisplayName va SampleValue.
  - `DatabaseConfig` luu rieng `AvailableFields` la toan bo cot doc tu Excel va `LabelFields` la cac cot duoc phep dung trong tem.
  - Sau khi import Excel, panel Data Sources hien 2 danh sach: `All fields from Excel` va `Fields used on label`.
  - Co nut `Add`, `Add all`, `Remove`, `Clear` de chon truong dua vao tem.
  - Properties khong con lay field truc tiep tu tat ca header Excel; ComboBox `Excel field` chi dung `LabelFields`.
  - Function tab co danh sach selected label fields; bam field se chen `FIELD("TenCot")` vao object dang chon.
  - Function module van giu `FIELD`/`CONCAT` hien co de dam bao tuong thich, nhung luong su dung chinh da dua theo field nguoi dung chon.
- Them Function Builder de ghep cong thuc khong can tu viet `CONCAT(...)`:
  - Nguoi dung bam field trong `LabelFields` de add vao chuoi cong thuc.
  - Co cac nut separator nhanh nhu ` - `, ` | `, ` / `, `_`, khoang trang va `: `.
  - Co o nhap fixed text va nut `Add text`.
  - Danh sach formula parts hien tung thanh phan dang ghep, co `Remove`, `Clear`.
  - Preview chi hien ket qua theo row Excel dang chon; an toan bo expression/code ky thuat, error text va Advanced modules de UI chi con nut thao tac can dung.
  - Nut `Apply` gan cong thuc da build vao object Text/Barcode/QR/Data Matrix dang chon.
  - Chuyen Function Builder khoi panel trai sang Properties va phan nhom thanh `2D Code Data Builder`, chi hien khi barcode standard thuoc nhom ma 2D/ma tran: QR Code, Data Matrix, Aztec, PDF417.
  - Dropdown `Barcode standard` trong Properties da duoc nhom thanh `1D barcode` va `2D / matrix code` de nguoi dung chon dung loai ma nhanh hon.
- Chuyen quan ly Excel/database field ra dialog rieng khi bam `Import Excel`:
  - Tao `ExcelImportWindow` rieng.
  - Dialog co duong dan file Excel va nut `Browse...` de chon file bat ky trong may.
  - Sau khi chon file, app hoi sheet roi import data.
  - Dialog hien 2 bang `All fields from Excel` va `Fields used on label` de add/remove/clear field.
  - Data Sources panel ben trai khong con chua UI add/remove field, chi con thong tin cay Template/Objects/Database va nhac dung ribbon Import Excel.
- Bo bang `Excel Preview` phia duoi workspace:
  - Khong con panel preview Excel trong man hinh thiet ke chinh.
  - Khong con splitter/hang duoi chiem dien tich khi chua co data.
  - State/command `IsExcelPreviewVisible` va `HideExcelPreviewCommand` duoc go bo khoi ViewModel.
  - Excel data van duoc giu trong ViewModel de print/binding, nhung quan ly thong qua dialog `Import Excel`.
- Grid nen tem tren designer doi tu 5 mm thanh 1 mm.
- Gioi han font chu cho Text/TextBox theo nhom phu hop tem logistic/san xuat:
  - Khong hien toan bo font he thong nua.
  - Danh sach uu tien: Arial, Arial Narrow, Bahnschrift, Calibri, Consolas, Courier New, Lucida Console, Segoe UI Semibold, Tahoma, Verdana.
  - Chi hien font co cai tren may; neu may khong co font nao trong danh sach thi fallback `Segoe UI`.
  - Font mac dinh cua object moi doi sang `Arial`.
- Text thuong tu dong fit khung xanh theo noi dung:
  - Object `Text` do kich thuoc chu hien tai va cap nhat Width/Height theo noi dung.
  - Khung chon/resize mau xanh bam sat noi dung text hon, khong con rong dai du thua.
  - Van gioi han trong bien label de text khong vuot kho giay.
  - `TextBox` khong auto-fit, van giu kich thuoc box co dinh de wrap/clip noi dung.
- Them nut `Update Excel` tren ribbon Data:
  - Nut nam canh `Import Excel` de phu hop luong dung thuc te khi file Excel thay doi du lieu.
  - Reload lai dung file Excel va sheet da gan trong `Template.DatabaseConfig`.
  - Sau khi reload, cap nhat lai data rows, PreviewRow, AvailableFields va giu lai LabelFields neu cot con ton tai.
  - Nut tu disable khi chua co file/sheet Excel hop le hoac file da bi xoa/doi vi tri.
- Tang do tin cay cho ket noi Excel -> Text/Barcode theo huong gan NiceLabel hon:
  - `BindingExpression` dang `{Field}` va formula `FIELD("Field")` khong chi match exact/case-insensitive, ma con fallback theo ten field da normalize.
  - Normalize field name bo qua khoang trang, gach noi, dau cau va khac biet hoa-thuong de giam vo binding khi header Excel thay doi nhe.
  - Khi import/reload Excel, app co gang repair lai `BindingExpression`/formula ve ten cot thuc te cua workbook hien tai neu tim thay match hop ly.
  - Truong hop nguoi dung go `Part_No` va file Excel doi thanh `Part No`, preview/in va formula builder van tiep tuc resolve dung field thay vi mat lien ket.
- Bo sung feedback truc tiep cho ket noi Excel trong Properties:
  - Object co `BindingExpression` gio hien them khung `Binding Preview`.
  - Khung nay hien `Source type`, `Preview value`, `Linked fields`, `Missing fields`, `Errors`, va `Binding status`.
  - Cung luat resolve field voi print/preview engine duoc dung de bao tinh trang lien ket, giup kiem tra nhanh object nao dang hop le va object nao dang mat field.
  - Formula van co khung `Formula Output` rieng, con placeholder binding thong thuong gio cung co feedback ro rang hon theo workflow gan NiceLabel.
- Bo sung tong quan `Binding Issues` o panel trai:
  - Data Sources hien them danh sach object dang vo field hoac co loi formula theo workbook hien tai.
  - Moi dong hien object name/type, status, missing fields va error text.
  - Bam vao mot issue se chon ngay object tuong ung de sua trong Properties/Designer.
  - Muc tieu la ra soat template nhanh theo kieu NiceLabel, khong can mo tung object moi biet binding nao dang hong.
- Don dep UX properties panel:
  - Them object summary card o dau Properties de thay nhanh object name/type/kich thuoc.
  - Card `Content` hien them workbook/sheet dang link de nguoi dung biet object dang an theo nguon du lieu nao.
  - `Content source` duoc dong bo lai theo object dang chon, tranh UI hien `Text` trong khi object thuc te dang bind Excel.
  - Font mac dinh cho object moi doi sang `Arial`; danh sach font uu tien cung dua `Arial` len dau.
  - Sua luong chon `Excel Field` trong Properties: khi doi field trong combo, object dang chon duoc bind lai ngay vao `{Field}` va canvas cap nhat theo row Excel hien tai, khong can bam lai source hoac tao object moi.
  - Neu chuyen source sang `Excel Field` khi chua co field dang chon, app tu lay field dau tien trong danh sach label fields de bind ngay cho object.
  - Import Excel lan dau tu dong dua cac header vao `Label fields`, nen object co the bind field ngay ma khong can vao cua so import bam `Add all` truoc.
  - Panel `Data Sources` co them bang `Excel Rows`; click tung row se cap nhat `PreviewRow` va day du lieu vao text/barcode object dang bind tren canvas.
- Cung co do ben save/load file thiet ke voi Excel link:
  - `.anlabel` van luu `DatabaseConfig.FilePath` va `SheetName`.
  - Khi mo lai template, neu file Excel lien ket van con ton tai thi app tu restore Excel data/sheet thay vi chi nho duong dan.
  - Neu file Excel cu da mat, template van mo duoc va app bao ro link cu khong con ton tai.
  - Luu them `LastSelectedRow` va khi mo lai template se co gang quay dung row Excel cuoi cung da dung de preview/thiet ke.
  - Panel `Data Sources` hien them workbook/sheet va row hien tai de nguoi dung thay ngay context du lieu dang duoc restore.
- Cung lam cay `Objects` de gan cach quan sat cua NiceLabel hon:
  - Moi object hien ro `Name` + `Type` ngay trong danh sach thay vi chi co ten.
  - Object dang co `BindingExpression` se hien them badge trang thai theo dung tinh trang hien tai: `Linked Excel`, `Formula linked`, hoac bao loi nhu `Missing: PartNo`, `Formula error`.
  - Badge nay doi mau theo trang thai, giup nhin ngay object nao dang bind on dinh va object nao dang loi ma khong can bam vao tung item.
  - Muc nay giup ra soat template nhanh hon khi co nhieu text/barcode object dang bind Excel.
- Sua hit-test cho `Rectangle` tren designer:
  - Vung ruot rectangle khong con bat click/chon object nua, nen text/barcode nam ben trong khung co the duoc chon truc tiep.
  - Chi cac vung vien rectangle moi nhan hit-test de chon/keo rectangle.
  - Visual fill/stroke cua rectangle van giu nguyen; thay doi chi ap dung cho hanh vi chon tren canvas.
- Tang do on dinh cho print/preflight:
  - Tao shared `PrintPreflightValidator` trong `ANLAbel.Printing` de barcode invalid, fixed QR qua suc chua, va text box overflow deu bi chan bang cung mot luong.
  - `PrintService` tu dong chay preflight truoc khi tao visual/gui print job; neu co loi thi throw message ro rang va khong gui job loi xuong printer.
  - `Print Preview` hien trang thai preflight ngay trong panel settings, giup thay truoc label nao chua an toan de in.
  - Luong print tu preview gio block som va thong bao ro ly do thay vi de driver/renderer xu ly muon hon.
  - `Print Preview` hien them danh sach `Preflight issues` theo row/object va cho bam de nhay toi label page dang loi.
  - Sua bug giu khong on dinh driver paper trong `Print Preview`: neu khong match lai duoc `PaperName` thi khong tu dong nhay sang giay dau tien cua driver nua; uu tien match theo ten, sau do theo kich thuoc da luu, neu van khong thay thi giu nguyen khổ da save.
  - `Print Preview` hien them trang thai match paper de biet dang dung dung driver paper, dang manual, hay dang giu khổ da save vi driver khong match.
  - `Print Preview` co them bang `Label / Excel tracking` gom page, source row, copy, PartNo, Name, Lot, Qty; bam vao dong se nhay sang label page tuong ung.
  - Sap xep lai `Print Preview`: bang tracking Excel/page duoc chuyen sang duoi vung preview tem ben trai, con panel setting ben phai co scroll rieng de khong bi khuất thong tin cau hinh.
  - `Print Preview` hien them vung in driver/may in: overlay net dut tren tem preview va thong tin `PageImageableArea` gom origin + kich thuoc vung in driver.
  - Luong in that tu dong bu `PageImageableArea.OriginWidth/OriginHeight` cua driver de giam tinh trang tem bi lech phai/xuong do WPF/driver dat goc in vao vung imageable.
  - Calibration tool trong `Print Preview` hien ro offset/scale hien tai, co nut `Print calibration` va `Reset offset/scale`; offset/scale van luu theo printer profile de tinh chinh tung may.
  - Muc nay giup kiem tra lo tem loi nhanh hon khi in nhieu row Excel, gan voi workflow kiem tra print truoc khi xuat job.
- Them object `TextBox` rieng voi luat chu khong tran ra ngoai box:
  - TextBox wrap va clip noi dung trong kich thuoc object.
  - Text thuong van khong wrap va co the tran ngang theo noi dung.
- Toolbar/toolbox co nut them Text, Text Box, Barcode, QR Code, Data Matrix, Line, Rectangle.
- Nut QR Code goi dung command tao object QR Code.
- Properties panel co phan Style hoat dong that:
  - Chon font chu tu font he thong.
  - Chon co chu.
  - Bold, Italic, Underline.
  - Canh trai/giua/phai.
  - Mau stroke/text, mau fill, do day border.
- Print renderer da phan biet Text va TextBox theo dung luat tran/khong tran.
- Them zoom bang `Ctrl + con lan chuot`:
  - Tren designer canvas, thay doi zoom nen thiet ke tem tu 25% den 400%.
  - Tren Print Preview, scale toan bo danh sach tem preview tu 25% den 400%.
  - Khi khong giu Ctrl, con lan chuot van cuon binh thuong.
- Them che do ve snap 1 mm cho Line va Rectangle:
  - Bam tool Line/Rectangle se vao che do ve, chua tao object co dinh ngay.
  - Click hoac chuot phai tren canvas dat diem dau, diem nay duoc snap vao giao diem luoi 1 mm.
  - Keo chuot toi diem con lai thi diem cuoi tiep tuc snap theo luoi 1 mm lien tuc.
  - Nha chuot de hoan tat object.
  - Bam Esc de huy che do bat diem/dang ve.
  - Line luu diem dau va diem cuoi that, render/print theo hai diem nay.
- Them style Outline/Fill cho Line va Rectangle:
  - Thickness tinh bang mm.
  - Outline style: None, Solid, Dash, Dot.
  - Outline color dung ma mau WPF/hex.
  - Corner radius tinh bang mm cho Rectangle/TextBox border khi render/print.
  - Fill style: None, Solid.
  - Background color dung ma mau WPF/hex.
  - Canvas preview va print renderer deu ap dung cac style nay.
- Them Delete/Undo/Redo:
  - Xoa object dang chon bang nut Delete tren toolbar hoac phim Delete.
  - Xoa duoc Line, Rectangle va cac object khac khi dang duoc chon.
  - Undo bang nut Undo hoac `Ctrl+Z`.
  - Redo bang nut Redo hoac `Ctrl+Y`.
  - History dung snapshot template nen ap dung cho them/xoa object va chinh properties.
- Them copy/paste object tren canvas:
  - `Ctrl+C` copy object dang chon hoac ca group selection.
  - `Ctrl+V` paste object/group moi, tao Id moi, day ZIndex len tren va offset 3 mm moi lan paste.
  - Paste giu nguyen text, binding, barcode standard, line endpoint va style cua object goc.
  - Object/group vua paste duoc select ngay de co the di chuyen tiep.
- Viet lai logic ve shape theo huong CAD cho Line/Rectangle/Ellipse:
  - Phim tat `L` vao lenh Line.
  - Phim tat `R` vao lenh Rectangle.
  - Phim tat `C` vao lenh Ellipse/Circle.
  - Click diem dau bat vao giao diem luoi 1 mm.
  - Keo chuot de preview va bat diem cuoi theo luoi 1 mm.
  - Co the nhap kich thuoc bang ban phim khi dang ve:
    - Line: nhap `20` roi Enter de ve line dai 20 mm theo huong con tro dang chi.
    - Rectangle: nhap `30,10` roi Enter de ve khung 30 x 10 mm.
    - Ellipse/Circle: nhap `30,10` de ve ellipse, hoac `20` de ve circle duong kinh 20 mm.
  - Esc huy lenh dang ve.
  - Them object `Ellipse`, canvas preview va print renderer deu ho tro.
  - Properties panel tu loc nhom phu hop theo object:
    - Text/TextBox hien text source va font.
    - Line/Rectangle/Ellipse hien Outline.
    - Rectangle/Ellipse hien Fill.
    - Barcode/QR/Data Matrix hien Barcode standard.
- Sua loi mo app sau khi them phim tat CAD:
  - WPF khong ho tro `KeyGesture` mot phim chu don le nhu `L`, `R`, `C` trong `InputBindings`.
  - Chuyen `L/R/C` sang `PreviewKeyDown` cua `MainWindow`.
  - Bo qua phim tat khi focus dang o TextBox/ComboBox de khong pha thao tac nhap lieu.
  - Xac nhan app mo duoc va process `ANLAbel.App` dang chay.
- Dieu chinh lai luong ve CAD cho on dinh hon:
  - Khong hoan tat object khi nha chuot nua.
  - Click diem dau de bat dau lenh va tao preview object.
  - Re chuot de preview lien tuc theo luoi.
  - Click diem thu hai de hoan tat, hoac nhap kich thuoc roi Enter.
  - Text command hien trang thai cu the: specify first point, specify next point, hoac size dang nhap.
- Them chon vung va di chuyen nhom object:
  - Keo chuot tren nen trong de tao vung chon marquee.
  - Cac object giao voi vung chon duoc dua vao selected group.
  - Keo mot object bat ky trong group se di chuyen ca nhom.
  - Group move co clamp trong bien label, ap dung cho ca Line co diem dau/diem cuoi rieng.
- Dieu chinh chon vung/thickness:
  - Sau khi khoanh vung, canvas giu focus de nhan phim dieu huong.
  - Phim mui ten di chuyen group 1 mm moi lan.
  - `Shift + mui ten` di chuyen group 10 mm moi lan.
  - Sua render Line de bounds co padding theo stroke thickness, tranh bi cat net khi tang do day line.
- Hoan thien feedback group selection:
  - Moi object trong group selection duoc ve overlay xanh dashed de thay ro da duoc gom.
  - Nut Delete va phim Delete goi lenh xoa selection tren canvas.
  - Delete xoa toan bo group neu dang co group selection, khong chi xoa object selected dau tien.
- Sua do benh lenh in:
  - Kiem tra printer da duoc chon truoc khi ghi XPS/print job.
  - Dung PrintTicket mac dinh cua queue neu PrintDialog khong tra ve ticket rieng.
  - Bọc lỗi in Current/All/Calibration bang status message thay vi de app crash.
  - Bọc lỗi Print Preview bang MessageBox `Print failed`.
- Sua loi Print Preview bao `The calling thread cannot access this object because a different thread owns it`:
  - Nguyen nhan do cua so preview giu lai `PrintDialog/PrintQueue` WPF object roi dung lai luc in, trong khi object nay bi WPF gan thread.
  - Print Preview chi luu `PrinterName` dang chon, khong giu/reuse `PrintDialog` cu.
  - Khi bam Print, `PrintService` tao `PrintDialog/PrintQueue` moi tu ten printer ngay trong luong in hien tai.
  - Build/test xac nhan khong con loi compile va print pipeline van PASS.
- Sua crash Print Preview:
  - Loi goc: `VisualPreviewHost` gan cung mot `DrawingVisual` vao nhieu parent WPF khi preview refresh/show.
  - Chuyen Print Preview sang render tung page visual thanh `RenderTargetBitmap`/`ImageSource`.
  - XAML preview dung `Image` thay vi gan truc tiep `Visual`.
- Nang cap lich su in tem:
  - Log ghi theo tung tem/label thay vi chi tong hop job.
  - Them cot `LabelIndex`, `PartNo`, `ItemName`, `Lot`, `Quantity`, `RowData`.
  - `RowData` luu tat ca field Excel cua dong in theo dang `Field=Value`.
  - Ho tro ten cot pho bien: PartNo/Part No/PN, Name/ItemName/TenHang, Lot/LotNo/Batch, Qty/Quantity/SoLuong.
- Hoan thien print history theo yeu cau thuc te:
  - Tat ca lenh in ghi vao mot file duy nhat `%AppData%/ANLAbel/print-history.xlsx`.
  - Them cot `LabelContent` de ghi noi dung thuc te tren tung tem sau khi resolve binding/formula theo row Excel.
  - Moi tem in ra la mot dong log rieng, co LabelIndex, row Excel, noi dung tem va du lieu field goc.
  - Sau khi in tu ribbon hoac Ctrl+P preview, app mo file print history de nguoi dung thay lich su in ngay.
  - Them nut `Print History` tren ribbon Printer de mo dung file history duy nhat bat cu luc nao.
  - Header history luon duoc dong bo lai, giup file cu cung hien dung cot `LabelContent` nam truoc `RowData`.
  - Neu Excel dang mo/khoa `print-history.xlsx`, Print Preview se bao rieng `Print history is open` thay vi hieu nham la loi in chinh.
- Lam lai Ctrl+P Print Preview:
  - Preview chi hien 1 tem tai mot thoi diem.
  - Thanh dieu huong ben duoi co Previous/Next, o nhap so thu tu tem va trang thai `Label x of n`.
  - Ben duoi preview hien tom tat row hien tai: PartNo, Name, Lot, Qty neu co.
  - Panel printer ben phai hien printer dang chon trong khung rieng, kich thuoc tem va nut `Select printer / properties`.
  - Khi in tu preview, log cung ghi tung tem va du lieu row thuc te.
- Tang do phan giai Print Preview:
  - Preview page duoc render noi bo o 300 DPI thay vi 96 DPI.
  - Anh preview duoc scale ve dung kich thuoc hien thi theo mm, giu net hon khi zoom/kiem tra tem.
- Gon lai Properties/Printer profile:
  - Phan printer profile dai ben phai duoc thay bang `Label size` nho gon va `Printer calibration` dang thu gon.
  - Label size chinh bind vao `Template.WidthMm/HeightMm`, canvas theo dung kho tem hien tai.
  - Printer paper W/H trong calibration dong bo nguoc lai template size khi can chinh truc tiep.
  - `PrinterProfile` chuyen sang ObservableObject de cac thay doi printer/paper size cap nhat UI va history on dinh.
- Chuyen setup kho in vao dung ngu canh:
  - Bo `Label size`, `Copies`, `Copy field`, `Printer calibration` khoi Properties object ben phai.
  - Them `Print setup` trong cua so Ctrl+P/Print Preview.
  - Trong `Print setup` co Label W/H, Copies, Copy field va nut `Apply print setup`.
  - Calibration trong Print Preview dang thu gon, gom DPI, Scale X/Y, Offset X/Y.
  - Doi binding preview tu `Template` sang `LabelTemplate` de tranh trung ten WPF `Control.Template`.
- Bo fallback kho giay hardcode trong printer setup:
  - Danh sach `Driver paper sizes` khong duoc phu thuoc duy nhat vao `PrintCapabilities.PageMediaSizeCapability`.
  - Neu driver may in tem khong expose du lieu day du qua WPF, phai bo sung fallback doc paper size theo API phu hop voi driver cong nghiep truoc khi chap nhan trang thai khong co khổ giay.
  - Khong tu sinh danh sach kho giay hardcode cua phan mem cho workflow chinh.
- Them chon huong in trong printer setup:
  - Cua so setup may in co them `Portrait` va `Landscape`.
  - Lua chon nay cap nhat `Template.Orientation`, kich thuoc label theo huong chon, va luu `PrinterProfile.PaperName`.
  - Khi mo `PrintDialog` va khi gui job in, `PrintService` ap lai `PageOrientation` va co gang match dung `PageMediaSize` cua driver theo ten/kich thuoc giay da luu.
- Nang cap `Print setup` theo huong gan workflow NiceLabel cho may in tem:
  - Bo sung `Printer settings source`: `Label` hoac `Driver`.
  - Bo sung `Page size source`: `Driver automatic` hoac `Manual`.
  - Bo sung danh sach `Driver paper` theo may in dang chon ngay trong Print Preview.
  - Bo sung `Orientation` ngay trong Print Preview, khong phai quay lai man hinh chinh de doi.
  - Them nut `Label printer setup...` rieng voi nut `Driver properties...` de tach ro luong setup nhan/giay voi luong mo driver Windows.
  - `Copies` va `Copy field` trong Print Preview da anh huong that den preview page count va lenh in.
- Mo rong them nhom media handling theo huong NiceLabel:
  - Bo sung `Media type`: `Gap`, `BlackMark`, `Continuous`, `Notch`.
  - Bo sung `Gap mm`.
  - Bo sung `Feed direction`.
  - Bo sung `Printable margin mm`.
  - Bo sung `Rotate output 180°`.
  - Preview/in da su dung `Rotate output 180°` trong render pipeline.
  - Preview/in da su dung `Printable margin mm` de ve va clip `printable area`.
  - Calibration preview hien them `Media`, `Gap`, va `Rotated 180` de nguoi dung doi chieu nhanh khi test may.
- Bo sung fallback doc khổ giay theo driver cong nghiep:
  - Ngoai `PrintCapabilities.PageMediaSizeCapability`, app goi them Win32 spooler `DeviceCapabilitiesW` de doc `paper names` va `paper sizes`.
  - Huong nay uu tien phu hop cho nhieu driver may in tem nhan cong nghiep hon so voi chi dung WPF capabilities.
  - Da sua loi startup do buffer `DC_PAPERSIZE` dung sai kieu du lieu; app mo lai on dinh sau khi doi sang layout phu hop voi Win32.
- Don dep Properties panel:
  - Bo tab placeholder `Source / Barcode / Position / General` vi chua co hanh vi that va gay nhieu.
  - Khi chua chon object, chi hien `No object selected`; cac field X/Y/Width/Height bi an han.
- Sap xep lai workspace/module chinh:
  - Menu tren cung chia lai theo nhom chuyen nghiep hon: File, Edit, Data, View, Print, Insert.
  - Ribbon tren cung bo bot nut ve object trung lap; tool ve nam chinh trong toolbox ben trai, ribbon giu File/Edit/Data/Print/View/Zoom.
  - Toolbox/Data ben trai, Properties ben phai va Excel preview ben duoi co header rieng va nut dong `x`.
  - Menu `View` co checkbox mo/tat lai Toolbox/Data, Properties, Excel preview va lenh `Restore workspace`.
  - Cac panel trai/phai/duoi co `GridSplitter` de keo resize truc tiep.
  - Khi tat panel, cot/hang cua panel thu ve 0 de khong de lai khoang trong du thua tren designer.
- Test runner:
  - File test print log doi sang ten unique de tranh bi khoa file khi Excel/app dang giu file cu.
- Verify:
  - `dotnet build ANLAbel.slnx` thanh cong.
  - `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj` tat ca test PASS.
  - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong sau khi sua printer setup.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS.
  - Mo `src\ANLAbel.App\bin\Debug\net8.0-windows\ANLAbel.App.exe`, xac nhan app chay len voi title `ANLAbel - Label Designer v0.021`.
  - Sau khi them `Binding Preview` cho Excel/Text/Barcode, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them `Binding Issues` o panel trai, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them `PrintPreflightValidator` va preflight status trong Print Preview, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them danh sach `Preflight issues` va jump-to-page trong Print Preview, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi don dep Properties va them auto-restore Excel link khi mo template, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them restore `LastSelectedRow` va hien row context trong `Data Sources`, build/test van PASS va app van mo duoc binh thuong.
  - Sau khi them overlay vung in driver, thong tin imageable area, bu origin driver va nut calibration/reset trong Print Preview, `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS sau thay doi print area/calibration.
  - Mo app debug va ban publish, xac nhan title `ANLAbel - Label Designer v0.026`.
  - Publish Release vao `dist\ANLAbel` thanh cong cho version `0.026`.
  - Sau khi lam lai print pipeline de template design la nguon kich thuoc duy nhat:
    - Preview/print dung `Template.WidthMm/HeightMm`, khong dung size cu trong `PrinterProfile` de render tem.
    - Print ticket chi chon driver paper neu kich thuoc khop template; neu khong thi dung custom page size theo template.
    - Bo bu am `PageImageableArea` mac dinh; driver imageable area chi de hien thi/canh bao, con chinh lech thuc te dung calibration offset/scale.
    - Bo clip theo printable margin trong visual in that de noi dung sat mep thiet ke khong bi mat; margin chi la thong tin setup/canh bao.
    - Text trong print renderer canh doc/ngang gan voi designer hon, tranh lech chu trong tem nho.
  - Them test `print preview follows design label size` va `print renderer keeps edge content`.
  - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong cho version `0.027`.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS cho version `0.027`.
  - Sau khi tiep tuc ra soat mismatch designer/print, print renderer cho barcode dung `LabelObject.QrDpi` giong designer, khong lay DPI driver lam doi rule render barcode ngoai y muon.
  - Them test `print barcode uses object dpi` de khoa luong nay.
  - Version hien thi duoc tang len `0.028` cho thay doi print alignment tiep theo.
  - Bo sung preflight de tranh case designer thay noi dung ngoai mep tem nhung print renderer cat theo kho tem:
    - Object visible nam vuot `Template.WidthMm/HeightMm` bi chan truoc khi in.
    - Text thuong sau khi resolve Excel theo tung row neu vuot kho tem se bi chan truoc khi in.
    - Thong bao preflight noi ro can move object, giam font, rut ngan data hoac dung Text Box.
  - Them test `print preflight blocks object outside label` va `print preflight blocks text outside label`.
  - Version hien thi duoc tang len `0.029` cho thay doi preflight chong in bi clip.
  - Sua dut diem luong ngang/doc cua print:
    - Print ticket khong con tin vao `Template.Orientation` cu neu no lech voi kich thuoc thiet ke.
    - `PageOrientation` duoc tinh truc tiep tu `Template.WidthMm/HeightMm`: width >= height la Landscape, nguoc lai la Portrait.
    - Driver paper match phai dung dung chieu width/height, khong con match bang min/max vi cach do co the lay kho giay doc cho tem ngang.
    - Khi tao template moi hoac chon paper tu driver, app dong bo `Template.Orientation` theo kich thuoc thuc te cua tem.
    - Them `LabelGeometry` de dung chung rule orient size, tranh moi noi swap ngang/doc mot kieu.
  - Them test `label orientation follows design dimensions`.
  - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong cho version `0.032`.
  - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca test PASS cho version `0.032`.
  - Bo sung chuan hoa driver media size cho truong hop driver tra paper theo chieu doc nhung tem thiet ke dang ngang:
    - Neu driver co paper cung kich thuoc nhung bi dao width/height, app giu driver media name nhung tao `PageMediaSize` theo dung width/height cua thiet ke.
    - Cach nay tranh viec tem ngang 100x50 bi gui thanh paper doc 50x100 va lam mat chu.
  - Mo rong test `label orientation follows design dimensions` de kiem tra paper driver bi dao chieu van normalize ve landscape design.
   - Version hien thi duoc tang len `0.033` cho thay doi driver media size orientation.
   - Sua dut diem chieu in bi xoay tren may in tem cong nghiep:
     - `PrintService.ResolvePageOrientation()` gio luon tra ve `PageOrientation.Portrait` cho may in tem nhan cong nghiep.
     - Nguyen nhan: khi label la landscape (width > height), code cu set `PageOrientation.Landscape` lam driver xoay noi dung 90°, tem in ra bi doc.
     - Kich thuoc giay chinh xac van duoc truyen qua `PageMediaSize`, khong can page orientation thay doi.
     - Cap nhat test `label orientation follows design dimensions` de dung voi hanh vi moi.
   - Bo sung xoay object (Rotation) trong designer va print:
     - `LabelDesignerCanvas` ap dung `RenderTransform` voi `RotateTransform` theo `LabelObject.Rotation` (0°, 90°, 180°, 270°).
     - `LabelVisualRenderer.DrawObject()` ap dung `RotateTransform` trong print pipeline, centered on object.
     - Properties panel co phan `Transform` voi ComboBox chon goc xoay 0°/90°/180°/270°.
   - `dotnet build ANLAbel.slnx -p:UseSharedCompilation=false -nodeReuse:false` thanh cong cho version `0.034` (0 warning, 0 error).
   - Chay `src\ANLAbel.Tests\bin\Debug\net8.0-windows\ANLAbel.Tests.exe` tat ca 17 test PASS cho version `0.034`.
   - Sua loi in tem bi mo, bi cat con mot phan noi dung va bi xoay doc tren may in tem:
     - GDI print pipeline dat `Graphics.PageUnit = Pixel` va ve vao rectangle tinh theo DPI thiet bi, khong con dua pixel bitmap vao don vi 1/100 inch cua GDI.
     - Bitmap print duoc render theo DPI X/Y thuc te cua driver va clone khoi stream PNG truoc khi gui xuong printer.
     - Driver paper size chi match khi width/height cung chieu voi thiet ke tem; khong match kho giay bi dao chieu vi se lam driver xoay output 90 do.
     - Giu `Landscape = false`; kich thuoc tem ngang/doc do width/height thiet ke quyet dinh, phu hop workflow may in tem cong nghiep.
   - Them test khoa hoi quy cho driver paper match cung chieu va rectangle pixel theo DPI may in.
   - Version hien thi va assembly duoc dong bo len `0.037`.
   - Quay lai pipeline in vector cho ban `0.038` vi huong GDI bitmap lam tem in bi mo tren may in nhiet:
     - Bo `System.Drawing.Printing.PrintDocument`, `RenderTargetBitmap`, PNG trung gian va `Graphics.DrawImage` khoi lenh in that.
     - `PrintService` dung WPF `DocumentPaginator` + `PrintDialog.PrintDocument` de gui visual theo dang vector xuong driver.
     - Van ep `PageMediaSize` bang dung width/height thiet ke tem va `PageOrientation.Portrait` de tranh driver xoay tem ngang thanh doc.
     - Calibration print cung dung chung pipeline vector, khong con raster toan bo tem thanh anh bitmap.
     - Version hien thi va assembly duoc dong bo len `0.038`.

### Phien ban 0.040 - Barcode text, auto-grow, compact UI, window fixes
- Sua loi Printer Setup dialog hien 2 lan khi mo app:
  - Bo `ShowPrinterSetupDialog()` trong `MainWindow_Loaded`; chi hien khi user bam nut `Printer Setup` tren ribbon.
- Sua loi window bi khuut title bar tren may man hinh nho/DPI cao:
  - `MainWindow_Loaded` clamp window size vao `SystemParameters.WorkArea`.
  - `Width/Height` toi da bang kich thuoc man hinh thuc te, tru taskbar.
- Sua loi Print Preview nhay ra app rieng tren taskbar:
  - `PrintPreviewWindow` them `ShowInTaskbar="False"`.
  - Constructor clamp kich thuoc cua so vao work area.
- Them tinh nang barcode noi dung text (ShowBarcodeText):
  - `LabelObject` them `ShowBarcodeText` (mac dinh: true) va `BarcodeTextFontSizePt` (mac dinh: 7pt).
  - `LabelVisualRenderer.DrawBarcodeText()` ve text centered duoi barcode.
  - 1D barcodes (vector): reserve textHeight tu barcode height, ve text ben duoi.
  - 2D codes (QR, DataMatrix): scale barcode nho hon, text ben duoi.
  - Properties panel co checkbox `Show text` + TextBox font size cho barcode.
- Auto-grow barcode width khi noi dung dai:
  - Vector renderer tinh `requiredWidthDip` tu `WidthModules * moduleWidthDip`.
  - Neu content dai hon container, tu dong mo rong `rect.Width` de barcode khong bi compress.
  - Dam bao in ra luon doc duoc, khong bi "squished".
- Compact Properties panel UI/UX:
  - Giam padding: 7→5, 9→6.
  - Giam margin: `0,0,0,6` → `0,0,0,3`.
  - Giam fontSize header: 13→12.
  - Giam fontSize labels: them `FontSize="11"` cho labels.
  - Giam MinHeight TextBox: 30→26.
  - Giam spacing giua ComboBox/TextBox (margin bottom: 8→5).
  - Tiet kiem ~30% khong gian doc, chua cho tinh nang sau nay.
- Version hien thi va assembly duoc dong bo len `0.040`.
- `dotnet build ANLAbel.slnx --nologo -v q` thanh cong (0 warning, 0 error).

### Quy tac lam viec tiep theo
- Sau moi phan hoan thanh, cap nhat outline nay voi noi dung da lam va cach verify.
- Sau khi build/test xong, tu dong mo app de test neu thay doi lien quan UI hoac workflow nguoi dung.

### Phien ban 0.058 - Designer stability va nen Data Source Manager

- Loai bo mutation geometry trong render/PreviewRow cua designer; text auto-fit chi con la visual.
- Sua matrix square sizing theo property nguoi dung vua doi va giu tam cho chieu app tu dieu chinh.
- Giam snap 3 mm xuong 1 mm, Alt tam tat snap, clamp drag du bon canh.
- Lost mouse capture/Esc khoi phuc ca group drag de tranh object teleport.
- Excel async co cancel, timeout UNC/network 30 giay va `FileShare.ReadWrite`; re-link khong doc workbook tren UI thread.
- Them nen `DataSource`/`DataSourceRegistry`, `DataSourceId`, `KeyValue`; UI manager va watcher lam o dot tiep.
- Them regression test geometry khi doi preview row, cancel Excel va registry CRUD.

### Phien ban 0.061 - Designer interaction controls

- Them toggle `Snap objects` tren ribbon va context menu cua canvas; Alt van tam bo snap trong luc keo.
- Luu preference snap rieng theo may tai `%LocalAppData%\ANLAbel\designer-preferences.json`, khong ghi vao template.
- Keyboard nudge hien X/Y moi hoac so object da move tren status bar.
- Them xUnit cho round-trip preference va fallback an toan khi JSON bi hong.

### Phien ban 0.062 - Excel reliability TC7

- Them `ExcelDataReadException` voi ma loi file mat, workbook hong, sheet mat, header row sai.
- Missing-sheet message liet ke cac sheet hien co; workbook hong co message ro rang thay vi exception thu vien kho hieu.
- Them 6 xUnit cho missing/corrupt/renamed sheet/header trung-rong/file dang mo/header ngoai vung.

### Phien ban 0.063 - Print unit round-trip reliability

- Khoa round-trip mm/DIP va mm/printer dots tai 203/300/600 DPI, sai so toi da 0.05 mm.
- Fail-fast neu DPI bang 0 hoac am de chan print plan vo nghia tu som.

### Phien ban 0.064 - Row Tracking Key

- Them ComboBox chon KeyField trong Data Sources; option rong de tracking theo row index.
- Luu KeyValue cua row dang chon va khoi phuc dung ban ghi sau refresh du row bi chen/xoa.
- Them regression test chen row phia tren `PN-200`, refresh van giu `PN-200`.

### Text / TextBox industrial contract va NiceLabel parity baseline (0.193)

> CONTRACT LOCK: day la hanh vi da duoc nguoi dung chot. Cac task khong yeu cau ro thay doi Text/TextBox khong duoc refactor, doi default, gom logic hoac sua incidental phan nay. Guardrail cho cac lan Codex sau nam trong `AGENTS.md`; moi thay doi contract duoc phep phai cap nhat plan, research va regression trong cung change.

#### Muc tieu san pham

- `Text` va `TextBox` la hai cong cu co muc dich khac nhau, khong phai hai bien the cua cung mot che do.
- `Text` la content-owned: noi dung chay tu do theo metric font; selection frame duoc AutoFit theo noi dung. Width/Height cu trong file khong duoc bien `Text` thanh mot field co wrap/clip.
- **Text frame-fit compress (explicit refinement, NiceLabel-aware):** NiceLabel `Text` khong cho sua Width/Height bang tay (size theo font; co `Font Scaling` style). ANLAbel cho phep keo vien `Text`: (1) khoa selection bang `TextSizing=FixedFrame` de AutoFit khong bung frame lai, (2) glyph compress vao frame qua scale ngang/doc (cho phep bien dang) tren `CreateTextLayout`/`DrawTextLayout` dung chung designer + print. Van khong phai TextBox ownership: `ShouldConstrainToBox(Text)` luon `false`, khong wrap-as-field, khong Error block chi vi selection hep. `AutoFit` van dung cho Text moi / content-owned cho den khi user keo vien.
- `TextBox` la frame-owned tuyet doi: nguoi thiet ke keo/nhap ca Width va Height. Noi dung chi reflow/fit/clip trong frame va khong bao gio duoc tu thay doi kich thuoc object.
- Designer, preview, preflight va print phai dung cung mot layout contract; khong chap nhan truong hop canvas dung nhung ban in sai.

#### Invariant bat buoc

| Object | Frame owner | Wrap theo Width | Clip tai frame | Overflow production |
| --- | --- | --- | --- | --- |
| Text | Content | Khong | Khong | Chi kiem tra vuot kho tem |
| TextBox | User | Co, theo grapheme/word | Luon co | Error/Ignore excessive/Ellipsis extension; khong co AllowOverflow |

- `ShouldConstrainToBox(Text)` luon `false`, ke ca file cu co `TextSizing=FixedFrame/ShrinkFont/ScaleWidth`.
- `ShouldConstrainToBox(TextBox)` luon `true`, ke ca file cu co `TextOverflow=AllowOverflow`.
- `AllowOverflow` duoc giu trong enum chi de doc file cu; voi TextBox runtime phai fail-closed thanh `Error`.
- `AutoFit` trong TextBox file cu duoc normalize thanh `FixedFrame`; Text duoc normalize ve `AutoFit + AllowOverflow` de model the hien dung y nghia object.
- ShrinkFont va ScaleWidth chi thay doi glyph layout ben trong; ca hai khong duoc mutate Width/Height cua TextBox.
- **Doi contract 2026-10-29 (chu san pham: "bo lenh cam, ca text box cu lam theo NiceLabel 10"):** `TextSizing=AdjustHeight` la NiceLabel `Adjust height to fit content` — TextBox giu Width, Height theo noi dung da wrap (designer theo text thiet ke, print theo gia tri tung row, khong overflow doc, preflight khong block vi chieu cao). PreviewRow van khong doi frame. Text co `Style.FontScalePercent` (NiceLabel `Font Scaling`, 10-400%): style factor ngang ap truoc frame-fit compress va vao frame AutoFit. Gate: `Text font scale widens and narrows glyphs like NiceLabel`, `TextBox variable height follows content like NiceLabel`.

#### Layout TextBox

- Khung mac dinh phai compact va phu thuoc kho tem: toi da 32 x 6 mm, inset theo 4% canh ngan (clamp 0.5-2 mm), va luon nam tron trong label. Font 9 pt, vertical Center, padding compact 0.2 mm bon canh, khong ve outline line.
- Noi dung khoi tao chi la `Ô chữ` (chu san pham duyet 2026-10-15; truoc la `Text Box`), khong dung doan huong dan dai lam object vua tao overflow va chiem dien tich thiet ke.
- Muc tieu dien tich: tren frame 20 x 6 mm, content rectangle mac dinh phai dat it nhat 90% dien tich frame. Properties co preset Tight 0 / Compact 0.2 / Comfort 1 de doi nhanh theo kich thuoc tem.
- Tay nam selection giu hit target 10 DIP de de keo nhung marker chi 5 DIP, tranh che kin chu tren object thap; marker chi la designer chrome va khong tham gia print geometry.
- Content width = frame width - left/right padding; content height = frame height - top/bottom padding.
- Wrap theo Unicode grapheme, ton trong newline; mot grapheme rong hon content width van phai danh dau overflow ngang.
- Height overflow tinh tu glyph metrics/line box thuc, khong uoc luong bang so ky tu.
- Keo resize Width/Height phai hoat dong voi TextBox. Resize width reflow ngay; resize height thay doi so dong nhin thay. Sua noi dung hay doi PreviewRow tuyet doi khong duoc doi frame.
- Vong doi resize chi commit/cancel qua `Thumb.DragCompleted(Canceled)`. `LostMouseCapture` xay ra ca khi nha chuot binh thuong, vi vay khong duoc dung su kien nay de restore snapshot; neu dung se gay loi frame rong ra trong luc keo roi thu lai khi tha.
- `Error`: canvas/preview clip de bao ve output, hien canh bao va preflight block print.
- `Clip`: mapping cua NiceLabel `Ignore excessive content`; clip/discard phan du co chu dich, preflight khong block va UI phai canh bao mat du lieu.
- `Ellipsis`: gioi han so dong theo height, dong cuoi hien dau ellipsis, preflight khong block.
- `ShrinkFont`: ten enum compatibility cho NiceLabel fit-by-font-size; tim font lon nhat trong min/max cau hinh, co the tang hoac giam, khong mutate authored font.
- `ScaleWidth`: giu font size/line height, scale ngang trong min/max cau hinh (co the co hoac gian), theo anchor Left/Center/Right.
- Nghien cuu nguon, ma tran parity va gap nang cao duoc luu tai `docs/NICELABEL_TEXTBOX_RESEARCH.md`.

#### Properties panel

- Summary card co icon dung loai object (Text, TextBox, Barcode, QR, shape, image) de nhan dien nhanh.
- Text va TextBox dung chung nhom typography; phan frame/overflow/padding chi hien cho TextBox.
- Callout noi ro ngay trong panel:
  - Text: free-flowing, dung TextBox neu can gioi han vung in.
  - TextBox: fixed frame, wrap, khong bao gio in ra ngoai object.
- Bo `AutoFit`, `AdjustHeight` va `AllowOverflow` khoi lua chon TextBox; label UI dung ngon ngu theo tac vu. Tu 2026-10-15 (chu san pham duyet, chi doi chu hien thi): `Xuống dòng, giữ cỡ phông`, `Đổi cỡ phông cho vừa khung`, `Co giãn chiều ngang chữ cho vừa`, `Chặn in và cảnh báo`, `Cắt bỏ phần chữ dư (không in)`, `Thêm dấu ba chấm ở dòng cuối`; banner Text/TextBox va loi tran chu tren canvas cung tieng Viet. Bang doi chieu trong `docs/NICELABEL_TEXTBOX_RESEARCH.md`; gate `text box overflow is reported in Vietnamese on the canvas`.
- Cac tuy chon it dung/ky thuat phai nam sau nhom chinh hoac trong Advanced de panel ngan, de scan.
- Figma mockup phai dung icon SVG/asset tu codebase, auto-layout, section ro rang va validate screenshot rieng cho summary, content, typography va TextBox frame.

#### Compatibility va verification gates

- Save/load/clone/scene identity van mang du TextSizing/TextOverflow/padding de khong lam mat du lieu file cu.
- File cu co `Text + FixedFrame` phai render nhu Text tu do; file cu co `TextBox + AllowOverflow` phai clip va block khi overflow.
- Regression tests bat buoc:
  - Text khong bi constrained boi sizing flag cu.
  - TextBox khong the AllowOverflow.
  - TextBox khong doi Width/Height khi sua noi dung.
  - Selection handle keo doi duoc ca Width/Height; resize width lam doi so dong wrap va host frame khop kich thuoc model.
  - Normal mouse-capture release khong phat `ResizeCanceled`; regression UI Automation phai do Width truoc/trong/sau thao tac keo that va xac nhan kich thuoc sau 1 giay van bang kich thuoc luc tha chuot.
  - Doi PreviewRow khong mutate frame TextBox.
  - TextBox moi tren tem 20 x 8 mm nam tron trong tem, khong overflow voi placeholder, va content area dat it nhat 90% frame.
  - Overflow Error/Clip/Ellipsis, ShrinkFont, ScaleWidth dung cung ket qua o designer/preview/print/preflight.
  - Text shrink-frame: frame nho hon natural ink → HorizontalScale/VerticalScale &lt; 1 va ink nam trong authored frame; TextBox ScaleWidth/ShrinkFont khong bi bat cho Text.
  - XAML load/build thanh cong; manual smoke test voi Text va TextBox dat sat object ke ben de xac nhan glyph TextBox khong de len object do.

### GS1 FNC1 fixed-length guard (0.194)

- Hoan tat software slice tiep theo trong execution plan ma khong can may in that: `BarcodeApplicationContract` nay nhan dien dung series trade-measure co do dai co dinh `31xx` den `36xx` (6 chu so value) va AI `7003` (expiry date/time `YYMMDDhhmm`).
- AI `7003` khong con bi xem la variable-length; khi theo sau boi AI khac, normalized payload khong duoc chen GS/FNC1 sai vi tri.
- Sai dinh dang measure, ngay, gio hoac phut fail-closed truoc renderer/preflight. Regression bao gom chuoi fixed measure + `7003` + lot va cac vector gia tri hop le/khong hop le.
- Van con open: full GS1 AI registry su dung Barcode Syntax Resource, vector FNC1 toan bo AI, barcode verifier/grade va thiet bi Zebra/TSC/Godex that. Khong duoc goi phan registry subset nay la full GS1 certification.

### Image alpha va 1-bpp raster fixture (0.195)

- Bo sung regression byte-level cho `ImageRasterizer`: alpha trong suot va nua trong suot duoc composite tren nen tem trang truoc khi threshold, tranh thanh block den khi in nhiet.
- Fixture Bayer 4 x 4 voi gray 128 xac nhan `MonochromeOrderedDither` tao dung 8/16 pixel den, trong khi `MonochromeThreshold` khong tu y thay doi thanh dither. Raster app-owned chi chua den/trang va deterministic.
- Day la software parity cho alpha va 1-bpp. Color profile, driver-managed dither, byte raster Zebra/TSC/Godex va physical-label/verifier van can driver, may in va evidence that.

### CSV data source compatibility slice (0.196)

- `ExcelDataService` giu API va behavior workbook cu, dong thoi nhan `.csv` UTF-8 qua pseudo-sheet co ten on dinh `CSV`. Import, refresh, relink, header-row picker va binding hien co dung chung luong nay.
- CSV parser local ho tro delimiter comma/semicolon/tab, field quote `"..."`, escaped quote `""`, UTF-8 BOM va FileShare.ReadWrite; CSV chi co mot pseudo-sheet nen ten sheet sai fail-closed.
- Regression bao gom UTF-8, quoted comma, semicolon delimiter, preview va load. ODBC/SQL, streaming-page source va typed connector/schema da chuyen sang roadmap R4 va van can thiet ke domain rieng.

### GS1 country-code fixed-length guard (0.197)

- Mo rong GS1 production subset voi AI `422`, `424`, `425`, `426`: validate country-code numeric co do dai co dinh 3/6 va khong chen GS/FNC1 giua cac element string fixed-length lien nhau.
- Regression khoa ca valid composite `422 + 425 + lot` va invalid digit/length. Day la sua separator correctness trong subset, khong phai full GS1 registry/certification.

### Document envelope v2 va unknown-extension preservation (0.198)

- `.anlabel` moi ghi `schemaVersion: 2`; file envelope v1 va raw legacy template van doc duoc theo migration path cu. Schema tuong lai van fail-closed va khong bi overwrite.
- `LabelTemplate` giu lai unknown JSON member qua `[JsonExtensionData]`; metadata extension trong template v1 khong bi app cu im lang xoa khi mo va luu lai o v2.
- Regression khoa v1 -> v2 round trip voi object extension co nested value. Policy nay ap dung cho extension o template payload; schema envelope moi cua future version van fail-closed de tranh danh roi semantics ma app khong hieu.
- Extension fingerprint da di vao immutable `DocumentSnapshot` va document/scene identity theo canonical JSON order; hai template khac extension metadata khong con dung chung audit/cache identity.

### Scene compiler line safety/parity (0.199)

- Scene compiler chap nhan line ngang/doc co endpoint tuong minh, thay vi ep `Width` va `Height` deu duong nhu shape. Line zero-length hoac endpoint khong finite fail-closed.
- Object type enum khong duoc biet cung fail-closed truoc khi tao compiled node. Regression bao gom horizontal, vertical, degenerate line va unknown type.

### Typed data connector foundation (0.200)

- Them Core contract cho connector, schema typed, record immutable, page/continuation va cancellation. Contract khong mang `DataRow`, WPF hay secret de co the dung lai cho CSV/Excel/database sau nay.
- `DataTableDataConnector` la compatibility adapter read-only; `MainViewModel` publish typed connector sau import Excel/CSV trong khi `DataView` cu van giu cho UI/binding. New/Unlink xoa connector cu cung data cu.
- Regression cover schema type, paging, cancellation va CSV -> ViewModel -> typed page. ODBC/SQL credentials, transform lineage, data workspace UI van la cac slice R4 tiep theo.
- Typed record nay reuse thang `BindingExpressionEvaluator` va Formula AST co san, nen transform Formula/field normalize cho connector moi khong bi drift so voi object binding hien tai.

### Immutable connector snapshot va GS1 registry boundary (0.201)

- `DataTableDataConnector` nay capture schema va gia tri record thanh immutable snapshot ngay khi import. UI co the tiep tuc thay doi `DataTable` de preview ma page dung cho binding/in van determinstic, khong ro ri `DataRow` mutable.
- GS1 subset nay co version `ANL-industrial-subset-2026.08` va fail-closed voi AI chua co dinh nghia thay vi doan fixed/variable length va chen FNC1 sai. AI 10/21 hop le duoc sua ket thuc validation dung, khong roi xuong nhanh unknown registry.
- Regression cover mutate source table sau khi tao connector va AI chua dang ky. Full GS1 Barcode Syntax Resource registry van la hang muc con mo; subset versioned khong phai chung nhan GS1 hoan chinh.

### Typed transform va lineage (0.202)

- Them `DataTransformPipeline` thuần Core cho Formula AST tren `DataRecord` immutable. Transform co the tham chieu transform khac du khai bao nguoc thu tu; pipeline topo-sort truoc khi evaluate.
- Ket qua mang lineage input-field -> output-field de Data Workspace/print audit co the giai thich gia tri duoc tao ra tu dau. Duplicate output, formula sai va dependency cycle fail-closed, khong tao gia tri in order-dependent.
- Regression cover dependency reorder, lineage va cycle. Variable prompt/database/counter, transform UI va persist workflow van la slice R4 tiep theo.

### CSV logical-record va invalid-data gate (0.203)

- CSV delimiter nay duoc detect tren record logic dau tien, khong phai physical `ReadLine`, nen header co quoted field xuong dong van dung comma/semicolon/tab.
- Quote khong dong trong header/data fail-closed qua `ExcelDataReadError.InvalidData`, co message va path ro rang thay vi im lang materialize row loi de binding/in.
- Application regression cover quoted multiline header va malformed CSV. Streaming connector/lazy large file van la hang muc R4 sau do.

### GS1 data-driven boundary registry (0.205)

- Them `Gs1AiRegistry` versioned: catalog pattern AI nhu `31xx`..`36xx`, `9x` quyet dinh fixed/variable element-string boundary o mot noi duy nhat. Normalizer va validation dung chung catalog nay.
- AI khong co trong catalog bi reject truoc validation; khong con danh sach delimiter rieng co the drift voi validation. Unit cover fixed family, variable internal family va unknown AI.
- Catalog hien van la industrial subset co chu y, chua phai import/full sync 542 AI tu GS1 Barcode Syntax Resource.

### GS1 pre-defined-length correction (0.206)

- Doi chieu GS1 DataMatrix guideline: fixed data length khong dong nghia pre-defined length. Chi family 00/01/02, 11-17, 31-36 va 41 khong can separator khi con element sau.
- `Gs1AiRegistry` nay luu boundary `PredefinedLength`/`SeparatorRequired`; 422/424/425/426 va 7003 van validate fixed data length nhung chen GS/FNC1 dung khi theo sau boi AI khac. Regression da sua cho composite vectors nay.
- Day la correction an toan cua v0.194/v0.197, dua normalizer ve dung huong dan GS1 thay vi assumption fixed-length cu.

### Official GS1 offline registry bundle (0.207)

- Bundle gzip JSON-LD chinh thuc GS1 offline trong Core, parse provenance version/last-modified/SHA-256 va hon 500 AI. Runtime fallback dung regex + separator flag cua source khi AI nam ngoai curated industrial subset.
- Regression khoa parser snapshot, bundle offline va AI `253` ngoai subset co normalize/FNC1 dung. Check-digit/association rule dac thu van chi duoc nang dan bang semantic validator, khong tu nhan barcode certification.

### Transform persistence va document identity (0.208)

- `LabelTemplate` nay persist DataTransform definitions; fingerprint deterministic cua transform duoc dua vao `DocumentSnapshot`/document hash. Thay doi transform vi vay khong the dung lai preview/print/audit identity cu.
- Regression cover fingerprint stable khi cung config va thay doi khi Formula thay doi. UI Data Workspace va wiring transform vao preview/dispatch la buoc R4 ke tiep.

### Transform preview wiring (0.209)

- `MainViewModel` nay evaluate transform persisted khi tao PreviewRow; binding va print row dung chung transformed values. Formula sai publish `DataTransformError` thay vi silently materialize derived data sai.
- Application regression cover CSV import + transform ra field `PrintName` trong PreviewRow. Data Workspace de author transform va preflight block diagnostic la buoc UX tiep theo.

### Transform dispatch guard (0.210)

- Quick Print Current Row fail-closed khi `DataTransformError` co gia tri; khong dispatch gia tri raw sau khi Formula transform da fail.
- App build Debug zero warning/error. All-row aggregate transform diagnostic va Data Workspace la buoc tiep theo.

### Batch transform dispatch guard (0.211)

- Print All Rows evaluate transform tung row va block ca batch neu bat ky row nao fail; khong de row hop le cuoi cung xoa diagnostic cua row loi truoc do.
- Full application regression pass sau gate nay. Data Workspace authoring/diagnostics la next product slice.

### Release version normalization (0.211)

- `eng/Version.props` la nguon public version duy nhat. App assembly/file/info,
  title/channel, Help, Commercial/Trial installer va package scripts phai derive
  tu cung snapshot `0.211`; regression `release metadata stays synchronized`
  khoa drift.
- Hardware, driver, installer signing/install, display visual va user testing
  khong co trong may hien tai duoc ghi la `deferred external evidence`; khong
  dung de chan software checkpoint va khong duoc suy dien thanh pass.
- Quy trinh va gate: `docs/VERSIONING.md`.

### Central release projection command

- `scripts/Set-ANLAbelReleaseVersion.ps1` is the standard bump command. It
  updates canonical `eng/Version.props`, app display strings and both public
  installer projections as one required operation; the release regression then
  verifies compiled/source metadata and current documentation projections.

### Data Workspace draft transforms (0.212)

- Data Workspace dung draft rieng va `DataTransformPipeline` cho sample/lineage;
  Apply moi thay collection transform cua template theo mot lan atomic. Duplicate,
  parse va cycle khong co raw fallback va khong partial apply.
- Version public bump `0.212` qua `eng/Version.props`. Hardware/user/install gates
  tiep tuc la `deferred external evidence` theo `docs/VERSIONING.md`.

### Local document Workflow host (0.213)

- Saved documents now expose a modeless local Workflow window. The Core state
  graph fails closed, while a path-safe `.workflow.jsonl` sidecar keeps an
  append-only integrity chain separate from print job history.
- A changed document hash starts a Draft revision and a corrupt audit tail blocks
  further transitions. The host deliberately has no Print, queue or unattended
  dispatch action. Workflow transition/audit and 1024 x 600 WPF Automation-tree
  regressions pass; physical and operator-review evidence remains deferred under
  `docs/VERSIONING.md`.

### File-drop claim foundation (internal, no public version bump)

- `FileDropClaimContract` defines deterministic trigger/config/source identity
  plus fail-closed detected, claimed, blocked, dispatched, quarantined and
  changed-after-claim states. `FileDropClaimLedger` persists its valid prefix
  as a hash-chained local JSONL ledger and refuses duplicate or corrupt-tail
  claims.
- This is not a watcher, file mover, Start button, queue call or automatic
  printing capability. P8 owner decisions still gate every such host action;
  the internal foundation therefore did not increment the public 0.213 version.

### Local Automation evidence console (0.214)

- P8 now has a modeless local Automation console for the fingerprint-ledger
  read model: it shows an explicit Stopped/no-runner state, no active
  configuration, redacted durable-event summary, and existing-owner links to
  History and Print Center.
- The console has no watcher, source consumption, file move, queue call,
  manifest creation or automatic print action. Its 1024 x 600 WPF Automation
  tree regression proves UI availability while machine/operator/unattended
  dispatch evidence remains deferred under `docs/VERSIONING.md`.

### Local Automation configuration (0.215)

- The P8 console now opens a local configuration dialog for one trigger ID/name,
  absolute watch root, file-name pattern, recursive mode and future-runner
  enabled flag. Settings use a checked fingerprint snapshot and are displayed
  back in the evidence console.
- Saving configuration never arms a watcher, consumes a file, chooses a queue,
  creates a manifest or prints. Those runtime steps remain separately gated.

### File-drop detect-only runner (internal)

- `FileDropDetectionService` is an independent, configured `FileSystemWatcher`
  with per-path debounce. It reads a share-safe file hash and writes only a
  durable `Detected` event; a repeat notification resolves to the same ledger
  identity.
- The runner owns no parser, claim/move/quarantine, document, queue, manifest,
  preflight or printer API. Its Start/Stop/lock/duplicate regression coverage is
  software-only; operational driver and operator evidence remains deferred.

### Detect-only runner lifecycle UI (0.216)

- The Automation console now starts and stops only the configured detect-only
  watcher and reports `Stopped`, `Running`, configuration and watcher errors.
  Start requires enabled, valid local configuration and never enables claim,
  parser, queue, manifest or print behavior.

### Detect-only lifecycle recovery (internal)

- Start/Stop/Error now append to a separate hash-chained lifecycle journal. On
  next console open, a previous `Running` entry is shown as stopped and requires
  explicit Start again; it never claims a watcher survived application restart.

### Document workflow publication-policy foundation (internal)

- `DocumentWorkflowPrintPolicy` now evaluates `Off` versus
  `RequirePublished` fail-closed against saved-document, audit-health,
  current-hash and exact-Published state inputs. It changes no print path yet;
  a future configuration owner must explicitly compose it at preview/prepare/
  dispatch with existing normal preflight and queue checks.

### Automation recent evidence projection (0.217)

- The local Automation console now shows up to 20 recent local-time event rows
  with state and redacted event fingerprint/reason. It never displays source
  paths or payload data, and the list remains evidence-only with no claim or
  dispatch action.

### P8 dispatch authority boundary

- Detect-only work is complete in software. Dispatch is not waiting on a real
  printer or a human test: it requires an explicit product choice for supported
  source schema/parser, target document/revision, named queue rule,
  claim/archive/quarantine protocol, retry semantics and P4 policy mode.
  Until then the code deliberately cannot progress from `Detected` to a print
  job, rather than silently selecting unsafe defaults.

### Explicit fingerprint-ledger claim (0.218)

- The Automation console now provides an explicit review action that advances
  each latest `Detected` evidence item to `Claimed` in the durable ledger. It
  neither reads/moves the source nor parses, queues or prints it; repeated use
  cannot claim an already claimed identity.

### Claimed-source verification (internal)

- `FileDropSourceVerificationService` rehashes supplied source bytes before a
  future parser stage. A mismatch writes terminal `ChangedAfterClaim`; a match
  remains only verified evidence and still cannot parse, queue or print.

### CSV automation-source parser (internal)

- A UTF-8 header CSV parser now returns Core `DataRecord` values and explicit
  row/header diagnostics. It is not wired to a document, transform, manifest,
  queue or dispatch path until those product bindings are chosen.

### Dispatch binding configuration (0.219)

- P8 configuration now records an optional explicit target template path, named
  queue and workflow policy. A separate dispatch-readiness contract requires all
  three; detection and preparation do not treat their absence as an error or
  invent a Windows-default queue.

### Dispatch-readiness status (0.220)

- The Automation evidence console now states whether the saved configuration has
  explicit template/queue/policy binding or names the missing prerequisite. A
  complete binding remains informational: no dispatch action has been installed.

### Template binding validation (internal)

- Future automation dispatch now has a validator that loads the configured
  template, captures its exact document hash and evaluates the configured
  workflow policy against the sidecar audit. It stops before manifest/queue/
  print and fails closed for an unpublished `RequirePublished` document.

### Template-policy action (0.221)

- Automation console exposes a template-policy validation action. It displays
  the current exact-template/policy result and explicitly states that no
  manifest, queue or print operation was created.

### Verified CSV preparation (internal)

- A claimed CSV is rehashed then parsed into in-memory records. Parser diagnostics
  durably transition the item to `Blocked`; valid records still cannot select a
  document, create a manifest, choose a queue or dispatch.

### Prepared source state (internal)

- A successful verified CSV parse now records durable `Prepared`, so any future
  manifest/preflight implementation must start from verified prepared records,
  never directly from raw detection or claim evidence.

### Prepared-source revalidation (0.222)

- `Prepared` is not an authorization boundary. Source verification accepts only
  the latest `Claimed` or `Prepared` evidence and rehashes bytes again before
  any future manifest/queue stage. A mismatch durably transitions the event to
  terminal `ChangedAfterClaim`; previously parsed records cannot be dispatched.
- Software evidence passed: solution build with 0 warnings/errors, 356 unit
  tests and the complete application regression suite. Hardware, spooler and
  operator checks remain deferred external evidence under `docs/VERSIONING.md`.

### Prepared data/template binding gate (0.223)

- A pre-dispatch validator now loads the exact configured template only after
  the document-workflow policy succeeds, extracts its explicit `{Field}`
  bindings, and blocks every prepared batch that lacks any required field.
- The gate deliberately creates no manifest, queue request, print job or durable
  copy of source payloads. It is the required handoff from prepared CSV records
  to a future shared print spine, not a second dispatch implementation.

### Local-label scope and automated quality loop (0.224)

- Documentation execution scope is now one offline/local Windows label product:
  designer, CSV/Excel binding, deterministic preview/preflight, explicit named
  queue, recovery and local file-drop automation. Web/cloud/login/sync/remote
  features and owner-sign-off work are excluded.
- Every user-visible UI change must be created or updated through `@figma` with
  an exact node/state/AutomationId handoff before WPF implementation.
- `scripts/Invoke-ANLAbelQualityLoop.ps1` closes build -> 356 xUnit -> application
  regression -> mutation testing. Stryker.NET is pinned locally; the first
  physical-unit conversion baseline killed 14/15 mutants for `93.33%`, above
  the unchanged release-blocking 90% threshold. Mutation scope expands only as
  additional label-safety contracts reach the same threshold.

### Compact industrial ribbon groups (0.194-0.197)

- Bo dải tab Home/Insert/Data/Print/View/Help khong co chuc nang. File va Edit duoc chuyen thanh quick-access icon tren header: New, Open, Save, Undo, Redo va Revisions.
- Gom toolbar thanh 6 nhom that: Templates, Data, Print & Output, Workspace, Selection, Help & Zoom. Cac lenh print thu cap (All rows, History, Export, Test print) dung button ngang nho xep 2 hang; Preview va Current van la thao tac chinh.
- Ribbon dung Viewbox `DownOnly` de co gian theo chieu rong cua cua so, khong con thanh cuon ngang che workspace. Snap object/grid rut gon nhan, khong con chu lon bi cat.
- Figma frame `ANLAbel — Compact Icon Groups v0.194` la ban chuan bo cuc 1440 x 170; code WPF giu Segoe UI, Figma dung Arimo vi file khong co Segoe UI.
- Release metadata dong bo `0.194` o app project, window/help, Trial va Commercial installer. Gate `release metadata stays synchronized` la bang chung chinh.
- Contrast correction `0.195`: quick-access va printer chip dung surface sang + vien xanh de icon PNG xanh/den co tuong phan ro tren header; paper chip khong co icon van giu nen xanh.
- Text clipping correction `0.196`: ribbon action cao 64 px, padding doc 3 px de icon + hai dong label co du line box; khong cat chan chu Excel/Setup/Current khi Segoe UI render theo display metrics.
- DPI-safe caption correction `0.197`: ribbon action cao 68 px; hai dong caption dung line box 15 px va `Ideal` glyph metrics rieng ben trong Viewbox, tranh lam tron xuong cat chan chu tai display scale 125%/150%. Ap dung dong bo cho Templates, Data, Print, Panels, Selection va Help; khong anh huong Text/TextBox tren canvas hay output in.

### Frequency-first Workspace va Properties panels (0.198)

- Figma frame `ANLAbel — Frequency-first Panels v0.198` tach dung hai nhiem vu `Layers` va `Data`, dung auto-layout, icon SVG, empty-state compact va card Document; screenshot rieng tung panel + screenshot tong khong co clip/overlap. File: `https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5`, root node `8:2`.
- Workspace WPF rong 268 DIP, co tab that `Layers`/`Data`; Data giu mac dinh de khong pha luong cu. Import Excel/CSV la primary action khi chua link. Excel rows van hien khi co data; Tracking/Copies/Transforms/Shared sources vao `Data settings` mac dinh dong; Binding checks luon thay va doi xanh/do theo trang thai.
- Properties rong 280 DIP; bo zoom trung lap vi ribbon + status bar da so huu zoom. Empty-state lon duoc thay bang card compact; khi chua chon object, card Document hien size tem, printer, data source va action Label & printer setup.
- Selected-object Properties giu nguyen controls/binding va thu tu frequency-first da co: summary -> Position & Size -> Advanced collapsed -> Content -> type-specific style. Khong thay doi bat ky invariant Text/TextBox nao.
- Tu duy, IA, tokens, kich thuoc, progressive disclosure va validation gate duoc ghi tai `docs/industrial-panel-design.md`.
- Runtime gate da qua tren display DPI 144 (150%): full window co dong thoi Workspace va Properties, ribbon/header/icon khong clip; Data no-link action va Document empty-state hien dung thu tu uu tien.
- Verification gate da qua: solution build `0 warning / 0 error`, `332/332` unit tests va regression executable exit code `0`.

### Compact geometry utility trong Properties (0.199)

- Figma selected-state `ANLAbel — Properties selected v0.199` (`13:2`) dat Content va Text Box behavior lam khoi chinh; `Position & size` va `Advanced` thanh utility row 48 px, nen phu, mac dinh dong. File: `https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5`.
- WPF bo card X/Y/Width/Height mo san: header Expander chi hien summary `X, Y · W × H mm`; cac TextBox/binding/Enter commit cu van nam nguyen ben trong va chi hien khi nguoi dung mo.
- Quy tac UX: canvas drag/resize la duong chinh; nhap so chinh xac la on-demand utility. Khong thay doi geometry model, resize lifecycle, Text/TextBox wrapping/clipping hay output in.
- Runtime gate DPI 144 da qua: selected Text Box hien hai utility row compact, khong clip header; UI Automation Toggle mo them dung 4 geometry fields va dong lai ve trang thai cu. Build 0 warning/0 error, 332/332 unit tests va regression exit code 0.

### Tabbed label-first Properties (0.200)

- Figma frame `ANLAbel — Properties tabs v0.200` (`18:69`) chot ba tab that: `Label`, `Layout`, `Advanced`. `Label` la mac dinh va dat Content, Text Box print-fit/overflow, Font/Size/Align/Bold/Italic/Underline len truoc; geometry va arrange/layer khong con chen vao luong soan noi dung tem.
- WPF `PropertiesModeTabs` giu summary object o tren cung. Tab `Label` chua Content va style theo object; `Layout` chua Position & Size; `Advanced` chua rotate, align, distribute, layer, visible va locked. Moi tab an/hien chinh subtree control cu nen binding/command/Enter commit van duoc bao toan.
- Khong thay doi contract Text/TextBox: khong sua ownership, frame size, wrapping, clipping, padding default, resize lifecycle, overflow detector hay output in.
- Release metadata duoc dong bo len `0.200` trong app project, title/header/help va hai installer.

### Evidence-based Excel verification in Properties (0.201)

- Dòng `No Excel file linked` thụ động trong card `Content` được thay bằng một vùng hành động thật theo Figma component `Excel Link Verification` (`22:82`): `Not linked`, `Checking`, `Verified`, `Stale`, `Failed` có màu, icon, nút và hướng dẫn riêng.
- `VerifyExcelLinkCommand` gọi trực tiếp `ExcelDataService.TestConnectionAsync`, nên trạng thái xanh chỉ xuất hiện sau khi mở được workbook, tìm đúng sheet và đọc được header. Evidence hiển thị số cột, số dòng và thời điểm kiểm tra.
- Import/refresh thành công tự xác minh vì đã đọc toàn bộ sheet. Khi file đổi trên disk, trạng thái tự hạ xuống `Stale`; nhấn `Update & verify` phải refresh snapshot trước khi trả lại `Verified`. File mất hoặc sheet lỗi chuyển sang `Failed`, không giữ success giả.
- Nút `Link Excel...` dùng lại chính `ExcelImportWindow`; không tạo luồng import thứ hai. Contract Text/TextBox không thay đổi.
- Release metadata đồng bộ lên `0.201`; regression thêm gate `properties excel verification is evidence based and refreshes stale rows`.
- Gate da qua tai DPI 144 (150%): solution build `0 warning / 0 error`, `332/332` unit tests, custom regression exit code `0`; runtime screenshot khong clip tab/icon/header. UI Automation xac nhan `Label` chi hien Content/Text Style (10 edit), `Layout` hien Position & Size truc tiep (6 edit), `Advanced` hien Align/Layer truc tiep; chuyen tab khong lam lo noi dung cua tab khac.

### Execution backlog sau 0.194

| Hang muc | Trang thai | Dieu kien thuc hien |
| --- | --- | --- |
| GS1 AI/FNC1 production subset | Da version/fail-closed, mo rong fixed-length 31xx-36xx, 422/424/425/426 va 7003; full registry con mo | Can import/duy tri nguon GS1 Barcode Syntax Resource va compatibility review |
| Image alpha/color-profile/1-bpp/dither parity | Alpha va app-owned 1-bpp/dither da co byte fixture; color profile va driver-byte parity con mo | Driver/may in that can cho color-management, driver dither va raster golden |
| Platform-neutral glyph/ink metrics | Con mo | Thay doi render architecture, can proposal rieng |
| Typed data connectors/schema | Da co immutable CSV/Excel snapshot, schema/page, Formula transform + lineage; connector ngoai va data workspace con mo | Mo rong model/data workflow, can yeu cau san pham cu the |
| CSV data source | Da co compatibility slice UTF-8 comma/semicolon/tab | ODBC/SQL, paging/lazy-load va typed connector schema con mo |
| Driver ticket/raster, spooler restart/hot-unplug, verifier, physical label | Khong the dong chi bang code | Can queue, driver, hardware va operator evidence that |

### NiceLabel printer/media architecture research (2026-09-14)

- Printer Setup da duoc sap xep thanh workflow bon tab `Printer`, `Label Dimensions`, `Paper / Media`, `Stocks`; print pane van giu printer, quantity, du lieu va preview trong mot cua so. UI neu ro stock la preset phan mem, media la capability cua driver, va chi `Apply` moi commit thay doi.
- Printer Setup cap nhat live compatibility va disable Apply cho den khi exact queue/capability/media request co the dispatch. Print Preview dung clone theo session; cac edit trong preview khong mutate label dang mo cho den action explicit `Save current print settings to label`.

- Doc nguon moi `ug-NiceLabel_2019_PowerForms_User_Guide-en.md` da duoc doc tai cac muc Printer and Status Bar, Label Setup Wizard, Label Properties, Printing va Set Printer/Print Parameters.
- Chot kien truc dich tai `docs/NICELABEL_PRINTER_MEDIA_ARCHITECTURE.md`: queue discovery, lazy driver capability, page/media, stock preset va authored label geometry la cac boundary rieng.
- Khong duoc gan `StandardLabelSizes.All` thanh capability cua moi printer trong kien truc cuoi; preset phai co provenance va chi duoc apply boi thao tac operator ro rang.
- Doi printer chi reload capability/compatibility state; khong mutate Width/Height cua label dang mo truoc explicit Apply.
- Preview/dispatch tiep tuc revalidate effective PrintTicket/media va output-contract hash; capability cache khong phai bang chung dispatch.
- Text/TextBox industrial contract khong thay doi va khong file protected nao bi sua trong dot nghien cuu nay.
- Figma shell moi `ANLAbel - NiceLabel Workspace Overview v2` nam trong file `https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc`, node `12:2`; da verify screenshot 1440 x 900 khong clip/overlap.
- Da code foundation: queue-only discovery, lazy WPF/Win32 capability service tren STA, pure media resolver, provenance/driver identity persistence va Printer Setup tach Stock / Page-Media / Authored Label.
- Manual mode gui exact named/custom media; DriverAutomatic khong ep `PageMediaSize` va tiep tuc fail-closed neu effective ticket bi driver coerce sai khich thuoc.
- Automated gate moi bao phu same-orientation resolution, incomplete/unavailable/custom status, dedupe provenance, stock/capability separation va round-trip/apply driver identity. Hardware matrix van la external evidence.
- Tiep tuc capability hardening: read WPF va Win32 catalog theo kieu bo sung (khong chi fallback khi WPF rong), gioi han 8 giay cho driver treo, giu partial diagnostic/`IsComplete=false`, va hien canh bao ngay trong Printer Setup. Raw Win32 media identity van giu format cu de khong pha document da save.
- Them unit gate `MergeAddsWin32RowsMissingFromWpfCatalog`; doi chieu lai NiceLabel Label Properties/New Label Setup Wizard va Microsoft `DeviceCapabilitiesW`. Khong doi Text/TextBox contract.
- Khoa them media/label compatibility ngay trong pure `PrinterMediaResolver`: manual media khong duoc `Compatible` neu physical W/H khong match authored label; explicit orientation swap van duoc chap nhan, nhung candidate driver van phai match dung chieu physical request. Printer Setup vi vay block sai kho truoc khi ghi profile.
- Capability snapshot doc them `PageResolutionCapability` va effective ticket DPI. Printer Setup khong con coi danh sach DPI hard-code la capability cua moi queue: chi hien square DPI driver advertise, van giu authored DPI trong list va canh bao neu driver khong advertise; non-square mode khong bi flatten thanh scalar gia.
- Them `tools/PrinterCapabilityProbe`: CLI chi-doc, nhan exact queue name hoac `--all`, xuat JSON media/DPI/provenance/completeness/diagnostic va khong gui print job. Day la cong cu thu evidence cho driver matrix, khong phai physical-output verifier.
- Probe that tren `Xprinter XP-DT426B` va `Datamax E-4203`: ca hai effective `203 x 203`; Xprinter khong advertise resolution capability va chi co mot Win32 `USER`, Datamax co mot WPF resolution va 21 Win32 media. UI duoc sua de capability rong khong fallback sang danh sach DPI chung: sau snapshot chi giu authored DPI + effective/advertised square DPI.
- Khoa driver drift tai effective-plan boundary: document cu co `DriverIdentity` rong van load/in theo legacy-unverified policy; khi identity da duoc capture, driver khong doc duoc hoac ten driver sau cung queue thay doi se block truoc PrintTicket/preflight/dispatch voi thong bao `no label was submitted`. Win32 fallback khong con dung queue name gia lam driver identity.
- Printer Setup co action `Driver preferences...` dung Windows `PrintUIEntry /e /n <exact queue>` qua `ProcessStartInfo.ArgumentList`, khong compose shell string va khong gui test/print job. Khi dialog dong, capability snapshot cu bi bo va media/DPI duoc reload.
- Capability snapshot/probe co them effective page media va imageable-area origin/extent. `PrinterPageObservation.IsImageableAreaVerified` goi dung shared `PrintableAreaContract`; Printer Setup hien effective page va chi goi imageable verified khi geometry nam trong media.
- Probe page geometry: Xprinter page `104 x 180 mm` tra imageable `8196.31 x 0 mm` nen bi danh dau invalid/unverified; Datamax page `62.5 x 40 mm` tra imageable `60.05 x 40.03 mm` voi origin `1.25 x 0 mm` va pass bounded tolerance. Queue available khong duoc dung thay readiness evidence.
- Hoan thien `PrinterSettingsSource`: Printer Setup cho chon `Saved with label` hoac `Current driver preferences`. Label mode moi apply saved PageMediaSize/DPI; Driver mode giu ticket hien tai cua driver, sau do van bat buoc merge/validate va effective media/DPI/imageable revalidation truoc spool.
- Them read-only queue-health contract cho `Paused/Offline/PaperOut/PaperJam/UserIntervention/Error/Unknown`. Production lookup va `CreateEffectivePlan` deu `Refresh()` va fail closed tren blocking state; automation preflight cung dung `CanSubmit`, con `Ready` chi co nghia khong thay Windows blocker, khong phai physical-output proof.
- Main shell warning, Operations Overview va Print Queue Console cung dung `CanSubmit` thay vi chi `IsAvailable`, nen installed queue dang offline/paused/media fault khong con hien nhu mot queue san sang.
- Them pure `PrinterDriverEvidenceContract` de probe khong gom nham `queue Ready` voi driver evidence day du: tach `QueueBlocked`, `QueueHealthUnknown`, missing/invalid effective page-imageable area, partial catalog va `FullyObserved`. Day chi la read-only diagnostic; template-specific effective PrintTicket van duoc revalidate o dispatch va `FullyObserved` van khong phai physical-output proof.
- `SupportsCustomMedia` khong con hard-code: capability STA doc Win32 `DC_FIELDS` + `DC_MINEXTENT/DC_MAXEXTENT`, chi chap nhan khi driver support `dmPaperWidth/dmPaperLength` va range min/max hop le. Manual resolver chi cho `CustomSupported` neu requested W/H nam trong range; thieu range, ngoai range hoac sentinel suy bien deu fail closed.
- `PrinterMediaResolver` cung fail closed voi `QueueBlocked` truoc ca nhanh `DriverAutomatic`, de Printer Setup khong luu mot cau hinh nhu the san sang khi snapshot vua thay offline/paused/media fault. Dispatch van refresh queue-health lan nua ngay truoc ticket merge/spool.
- Capability snapshot/probe ghi SHA-256 `ObservedTicketHash` cua current driver PrintTicket bang cung `PrintTicketFingerprint` ma dispatch dung cho requested/effective contract. Khong luu raw XML; serialization fail duoc phan loai `ObservedTicketUnverified`, khong the thanh `FullyObserved`.
- Probe co `--require-fully-observed`: exit `2` cho unavailable queue, exit `3` cho queue ton tai nhung driver evidence chua day du, va `0` chi khi strict matrix dat. Hai lan doc lien tiep tren Xprinter/Datamax cho cung ticket hash; Xprinter strict fail vi invalid imageable area, Datamax pass.
- Capability snapshot/probe doc them Print Schema standard features: effective + advertised `PageMediaType` va `OutputQuality`. List rong duoc giu la rong, khong invent default; speed/darkness vendor extension van khong hard-code hay gan nham thanh generic capability.
- Probe that: Xprinter current ticket co `PageMediaType=Plain` nhung driver advertise media-type list rong; UI danh dau day la current value khong phai selectable capability. Datamax expose rong ca media type va output quality. Cac feature optional nay la diagnostic, khong tu dong block dispatch.
- Them secure Print Schema feature inventory: parser chi giu Feature QName/namespace, gioi han 2 MiB + 512 identities, prohibit DTD/external entity va khong giu Option/Value/raw XML. Probe that cho Xprinter 5 standard/0 private features; Datamax 6 standard/0 private (them `PageResolution`). Khong co evidence de map darkness/speed thanh generic setting; tiep tuc de driver preferences/DEVMODE vendor so huu.

### NiceLabel Print Preview architecture research (2026-09-14)

- Chương 5 của NiceLabel xác nhận Print Preview là printing form gồm printer, prompted/database data, record selection, quantity, copies, sets, skipped first-page slots và physical page preview; không chỉ là ảnh của một tem.
- Kiến trúc đích được ghi tại `docs/NICELABEL_PRINT_PREVIEW_ARCHITECTURE.md`: immutable preview session input, explicit `QuantityPlan`, deterministic label-instance sequence, physical page/roll composer, trust state và một prepared-snapshot handoff duy nhất sang dispatch.
- Giữ các reliability contract hiện có: shared compiled scene/renderer, effective PrintTicket trên STA, stale-operation rejection, LRU raster cache, per-row preflight, source freshness, manifest/hash và last-mile dispatch revalidation.
- Khoảng trống P0: preview đang giữ live mutable template; label/page đang bị đồng nhất; copies bị flatten mất set/copy/slot identity; readiness phân tán qua nhiều bool/issue thay vì một trust state.
- Migration giữ `PrintPreviewWindow` làm host nhưng chuyển policy/orchestration khỏi giant code-behind; không tạo renderer hoặc dispatch path thứ hai. Phase đầu là pure contracts, chưa đổi UI/runtime trong đợt nghiên cứu này.
- Mọi UI implementation tiếp theo phải vẽ state-specific trong Figma trước, sau đó kiểm chứng runtime/UIA; Text/TextBox protected contract không thay đổi.
- Product scope được chốt là luồng cơ bản và tin cậy: thiết kế tem → Excel → field binding → chọn dòng/copies → barcode preflight → preview → đúng queue/media/DPI → dispatch/audit. ANLAbel không nhằm sao chép toàn bộ NiceLabel PowerForms.
- Không ưu tiên custom printing forms, scripting/action graph, prompted-variable phức tạp, multi-label sheet/labels-across, skipped slots, physical-page quantity, label sets, unlimited data, Store/Recall hoặc connector ngoài Excel/CSV khi chưa có nhu cầu sản xuất cụ thể.
- Mặc định kiến trúc và UI là một label mỗi feed/page; chỉ giữ stable source-row/copy identity. Ưu tiên tuyệt đối: không sai dữ liệu, barcode quét ổn định, đúng printer/media/DPI, preview-print cùng snapshot và không báo thành công/in trùng sai.

### NiceLabel JOB command file & execution architecture (2026-09-14)

- Đọc và phân tích toàn bộ Chương 9.1 `Command Files` (đặc biệt mục 9.1.1 `JOB Command File`) trong `ug-NiceLabel_2019_PowerForms_User_Guide-en.md`.
- File kịch bản `.job` là giao thức tích hợp công nghiệp chuẩn của NiceLabel dành cho ERP (SAP, Oracle, Infor), MES và WMS để in nhãn tự động qua cơ chế file drop hoặc pipeline.
- Tập lệnh NiceLabel JOB gồm 18 lệnh cốt lõi: `LABEL`, `PRINTER`, `SET`, `CLEARVARIABLEVALUES`, `PRINT`, `PORT`, `PRINTJOBNAME`, `SETPRINTPARAM`, `SESSIONSTART`, `SESSIONPRINT`, `SESSIONEND`, `IGNOREERROR`, `CREATEFILE`, `DELETEFILE`, `TEXTQUALIFIER`, `COMMENT (;)`.
- Chốt tài liệu kiến trúc và lộ trình thực thi tại `docs/archive/NICELABEL_JOB_COMMAND_ARCHITECTURE.md`.
- Nguyên tắc thiết kế:
  1. Bảo vệ nghiêm ngặt hợp đồng `Text / TextBox` theo `AGENTS.md` (không đổi ownership, frame size, padding, clipping hay layout renderer).
  2. Parser phân tích cú pháp bất biến (Immutable AST: `IJobCommand`), hỗ trợ quoted strings, escape `\r\n` và text qualifiers.
  3. Context state machine quản lý biến, counter (`step`/`repetitions`), template active, máy in active và trạng thái phiên gom nhãn `SESSION`.
  4. Lập kế hoạch in (`JobExecutionPlanner`) chuyển đổi các lệnh thành `PrintIntent` và `DataRecord` có cấu trúc.
  5. Fail-closed: Mọi lệnh sai cú pháp, template không tồn tại, printer queue offline/unavailable hoặc preflight vi phạm (barcode, missing field) đều dừng ngay và báo lỗi, trừ khi có cờ `IGNOREERROR on`.
  6. Tích hợp trực tiếp với `PrintPreflightValidator`, `IPrinterQueueLookup`, `PrintService` (dedicated STA) và lưu vết audit bất biến vào `AutomationJobHistoryStore`.
- Lộ trình 5 Phase:
  - Phase 1: Pure Contracts & Parser (`ANLAbel.Core.Automation.Job`).
  - Phase 2: Execution Context & Planning Engine.
  - Phase 3: Preflight & Print Dispatch Pipeline.
  - Phase 4: File-Drop Automation Watcher & Durable History.
  - Phase 5: Regression Gates & Integration Fixtures.
