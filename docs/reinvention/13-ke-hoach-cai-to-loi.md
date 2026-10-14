# 13 — Kế hoạch cải tổ: tập trung lõi, ổn định trước tính năng

Trạng thái: **Đang thực thi** (lập và duyệt ngày 2026-10-13). Chủ sản phẩm chỉ thị
"thực thi cho tới khi xong", nghĩa là áp dụng các phương án đề xuất ở §8.

- Các mục ở §9 không gắn nhãn `[chờ QĐ-x]` được làm ngay, theo thứ tự.
- Các mục có nhãn từng chờ quyết định ở §8; mọi quyết định đã được duyệt ngày 2026-10-13.

**Mục tiêu:** những việc người vận hành làm hằng ngày phải **chạy ổn định, mượt
và dễ phục hồi** trên máy thật. Đó là:

- tạo, mở và lưu mẫu;
- thiết kế;
- dữ liệu và thông tin trên tem (Excel/CSV);
- xem trước và in.

Kế hoạch ưu tiên điều này hơn việc tăng số tính năng.

Tài liệu đọc kèm:

- [`../LOCAL_LABEL_PRODUCT_CONTRACT.md`](../LOCAL_LABEL_PRODUCT_CONTRACT.md):
  sản phẩm là gì.
- [`../CORE_SOFTWARE_STANDARD.md`](../CORE_SOFTWARE_STANDARD.md): các ID
  D/DB/B/P và tiêu chí nghiệm thu. Kế hoạch này thực thi mục 8 của quy chuẩn.
- [`../REFERENCE_POLICY.md`](../REFERENCE_POLICY.md): cách chọn ý tưởng tham khảo.
- [`07-execution-plan.md`](07-execution-plan.md): live plan, trỏ về tài liệu này.
- [`DECISIONS.md`](DECISIONS.md): ADR-022 ghi quyết định cải tổ.

**Những gì kế hoạch không chạm tới:**

- Hợp đồng Text/TextBox trong [`AGENTS.md`](../../AGENTS.md) giữ nguyên.
- Không nới cổng an toàn in nào: vẫn in tới named queue, fail-closed, và không
  tự gửi lại lệnh in chưa rõ kết quả.

---

## 1. Tóm tắt

Phần nền kỹ thuật đang dùng được:

- Build sạch. 1.129 unit test và 211 bộ regression đều xanh (chạy ngày 2026-10-13).
- Lưu mẫu ghi an toàn: ghi ra file tạm rồi thay thế, có `.bak` và lưu trữ phiên
  bản; app từ chối file có schema mới hơn.
- In chạy trên luồng riêng, fail-closed với máy in, khổ tem và DPI.
- Đọc Excel dạng streaming, ngoài luồng giao diện, có hủy và timeout.

Điểm yếu nằm ở sáu chỗ khác:

1. **Công việc chưa được bảo toàn và chưa tới tay người dùng.**
   - Worktree có 184 mục chưa commit, gồm cả quy chuẩn lõi và các test bằng chứng.
   - Bản cài trên desktop là 0.266; mã nguồn ghi 0.306.
   - Version 0.289–0.305 không có changelog.
2. **Vòng đời file dễ làm mất dữ liệu.**
   - Tạo mới, Mở và Đóng không hỏi lưu.
   - Ctrl+S không hoạt động; không có "Lưu thành".
   - Không có bắt lỗi toàn cục, không tự lưu. Một exception bất ngờ là mất cả phiên
     thiết kế.
3. **Lỗi im lặng và giao diện đứng hình ở luồng dữ liệu và in.**
   - "Cập nhật Excel" hỏng mà không báo gì.
   - In nhanh và danh sách máy in chạy đồng bộ trên luồng giao diện.
   - Nhật ký job lớn dần mãi. Nếu dòng cuối bị hỏng thì mọi lệnh in bị chặn.
   - In thử sửa mẫu đang mở kể cả khi bấm Hủy.
   - Cảnh báo in trùng không bao giờ bật.
4. **Bề mặt ngoài lõi vẫn nằm trong app.**
   - App có 25 cửa sổ, trong đó 9 cửa sổ không còn đường mở.
   - Khoảng 5.100 dòng automation và NiceLabel JOB chỉ được test gọi tới.
   - UI chào 15 loại mã vạch, trong khi mẫu thật chỉ dùng Code 128 và QR.
5. **Nguồn giao việc tự động lệch phạm vi.**
   - AGY dispatcher lấy việc từ tài liệu lịch sử và các handoff.
   - Dry-run xếp 13 việc, không việc nào là lỗi lõi.
6. **Chưa có bằng chứng nghiệm thu.** Cả 32 ID của quy chuẩn lõi đang
   *Not verified*.

Hướng cải tổ: không mở bề mặt sản phẩm mới trong suốt kế hoạch.

```text
GĐ0  khóa nền: bảo toàn, phát hành, một nguồn việc
GĐ1  gọn phạm vi: gỡ bề mặt và mã không phục vụ luồng chính
GĐ2  file: tạo, mở, lưu không mất dữ liệu
GĐ3  thiết kế mượt và đúng
GĐ4  dữ liệu và thông tin trên tem (Excel/CSV)
GĐ5  mã vạch, xem trước và in: ít dừng vô lý, không đứng hình, kết quả trung thực
GĐ6  nghiệm thu trên mẫu thật và máy in thật
```

---

## 2. Hiện trạng có bằng chứng (2026-10-13)

Nguồn bằng chứng:

- Chạy build/test trên worktree hiện tại.
- Đọc mã nguồn.
- Log vận hành trong `%LOCALAPPDATA%\ANLAbel\logs`.
- Dry-run AGY dispatcher.
- Mapping [`../CORE_STANDARD_EVIDENCE_AUDIT.md`](../CORE_STANDARD_EVIDENCE_AUDIT.md)
  ngày 2026-09-16.

Mục ghi "nghi ngờ" là đọc từ mã nguồn, chưa có ca tái hiện.

### 2.1 Bảo toàn, phát hành và nguồn việc

| # | Quan sát | Bằng chứng | Ảnh hưởng |
| --- | --- | --- | --- |
| H1 | Build/test xanh | Build 0 cảnh báo/0 lỗi; unit 1.129/1.129; regression 211 PASS/0 FAIL | Nền đủ tốt để sửa từng lát nhỏ |
| H2 | Worktree chưa commit lớn | `git status` có 184 mục: 87 file sửa (+5.122/−1.449 dòng) và khoảng 10.500 dòng mã mới chưa được git theo dõi. Commit cuối ngày 2026-09-15 | Một sự cố ổ đĩa hay thao tác git sai là mất khoảng một tháng việc. Không truy được bản nào gây lỗi |
| H3 | Bản cài lệch nguồn | `%LOCALAPPDATA%\Programs\ANLAbel\ANLAbel.App.exe` = 0.266; nguồn = 0.306 | Các bản sửa từ 0.273 trở đi (khổ tem, CSV/Excel, máy in) chưa tới người dùng |
| H4 | Version không có lịch sử | 0.289–0.305 không có trong tài liệu hay commit nào. Quy tắc 2 của `agent.md` yêu cầu tăng version sau mỗi lần sửa code | Báo lỗi theo số version không truy được |
| H5 | Test ghi vào dữ liệu thật | 22.364 trên 22.375 dòng của `data-operations.jsonl` (7,3 MB, không xoay vòng) đến từ `TestOutput`. Chạy bộ test cũng ghi lại `designer-preferences.json`. Có khoảng 18 chỗ tự ghép đường dẫn `LocalApplicationData`/`ApplicationData` | Không dùng được nhật ký vận hành để chẩn đoán; test có thể đổi thiết lập người dùng |
| H6 | Nguồn việc lệch | AGY dispatcher dry-run: 13 việc từ `MASTER_PLAN.md` và các handoff CC-P5…P8, DATABASE_MANAGER, P3–P7, R4 (`DocumentWorkQueue.cs:55-66`) | Agent tự động kéo lại việc ngoài phạm vi |
| H7 | Tài liệu dàn trải | `docs/` có 117 tài liệu: 19 hiệu lực, 15 tham chiếu, 41 lịch sử, 42 gói ngoài phạm vi. Nhiều tài liệu lịch sử tự nhận là "active plan". Quy chuẩn lõi chưa được commit | Người và agent đọc nhầm tài liệu cũ |
| H8 | Chưa có bằng chứng nghiệm thu | 32/32 ID *Not verified*; 13 ID có mâu thuẫn giữa yêu cầu và mã. Mã ngày 10-04/05 nhắm vào DB01/06/08/10, P03/04/08 và B01 nhưng chưa có ghi nhận chạy | Không biết chắc phần nào đã đúng |

### 2.2 Vòng đời file

| # | Quan sát | Bằng chứng | Ảnh hưởng |
| --- | --- | --- | --- |
| H9 | Không hỏi lưu | `New_Click`/`Open_Click` (`MainWindow.xaml.cs:734-798`) và mở từ thư viện thay mẫu mà không kiểm tra. Không có handler `Closing`, không có trạng thái "chưa lưu" | Bấm nhầm Mới, Mở hoặc Đóng là mất thiết kế |
| H10 | Lỗi bất ngờ làm sập app hoặc bị nuốt | <ul><li>`src` không có `DispatcherUnhandledException`, `AppDomain.UnhandledException` hay `UnobservedTaskException`; có 49 handler `async void`.</li><li>8 lệnh bất đồng bộ trong `MainViewModel` (dòng 170-184) không có xử lý lỗi; lỗi lọt ra chỉ ghi `Debug` (`AsyncRelayCommand.ExecuteAsync`).</li><li>Nghi ngờ: setter tọa độ/kích thước nhận NaN hoặc vô cực (`LabelObject.cs:76-92`), làm lỗi lúc chụp undo và lúc lưu.</li></ul> | App đóng mất phiên, không để lại crash log; hoặc thao tác thất bại mà người dùng không được báo |
| H11 | Thiếu thao tác file cơ bản | <ul><li>Không có "Lưu thành".</li><li>Ctrl+N/O/S ghi trong tooltip và Trợ giúp, nhưng chỉ Ctrl+P/Z/Y, Delete và F1 được gắn (`MainWindow.xaml.cs:43-57`).</li><li>Bộ cài đăng ký đuôi `.anlabel`, nhưng app không đọc tham số dòng lệnh.</li></ul> | Mở đúp file chỉ ra mẫu trống; không lưu được bản sao |
| H12 | Khởi động phụ thuộc file cấu hình | `DataSourceRegistry.Load()` chạy trong constructor của `MainViewModel` (dòng 133) và ném lỗi khi JSON hỏng hoặc schema mới hơn; không ai bắt lỗi này. File nằm ở `%APPDATA%` (roaming) | Một file `data-sources.json` hỏng có thể làm app không mở được |
| H13 | Lưu gắn chặt với phần phụ | <ul><li>Lưu trữ phiên bản phải thành công thì lần lưu mới thành công (`ProjectFileService.cs:64-73`).</li><li>Mẫu được serialize từ đối tượng đang sửa trong khi giao diện vẫn cho sửa.</li><li>Nút Lưu không bị khóa khi đang lưu.</li></ul> | Ít khả năng xảy ra, nhưng có thể ghi ra file trạng thái lẫn lộn, hoặc lưu thất bại vì phần phụ |

### 2.3 Dữ liệu, xem trước và in

| # | Quan sát | Bằng chứng | Ảnh hưởng |
| --- | --- | --- | --- |
| H14 | "Cập nhật Excel" hỏng mà không báo | `RefreshExcelDataAsync` (`MainViewModel.cs:1700`) không bắt lỗi; lệnh không có xử lý lỗi | File bị khóa hoặc thiếu sheet: không có thông báo; biểu ngữ "dữ liệu cũ" vẫn còn và việc in bị chặn mà không có lý do |
| H15 | Kiểm tra "Excel đã đổi" có kẽ hở | <ul><li>`FileSourceIdentity.cs:25` mở file với `FileShare.Read`, nên thất bại khi Excel đang mở file.</li><li>Danh tính file được lấy lúc mở xem trước, không phải lúc nhập.</li><li>In nhanh chỉ dựa vào file watcher (so mtime); lỗi khởi động watcher bị nuốt.</li></ul> | Có thể in dữ liệu cũ, hoặc chặn nhầm |
| H16 | Thiết lập liên kết dữ liệu nằm ngoài undo (nghi ngờ) | `DatabaseConfig` không phát thông báo thay đổi, nhưng nằm trong snapshot undo. Dòng tiêu đề và đường dẫn relink được ghi trước khi nhập thành công (`ExcelImportWindow.xaml.cs:96`, `MainViewModel.cs:2077`) | Undo một thao tác không liên quan có thể trả cột số bản hoặc file liên kết về giá trị cũ |
| H17 | Giao diện đứng hình | <ul><li>In nhanh đọc printer contract và kiểm tra mọi dòng đồng bộ trên luồng giao diện (`MainViewModel.cs:4467/4477`).</li><li>Liệt kê máy in chạy trên luồng giao diện.</li><li>Xem trước băm SHA-256 file Excel trên luồng giao diện.</li><li>Khi spooler tắt, app báo "không có máy in" thay vì nêu lỗi thật.</li></ul> | App như bị treo khi driver chậm, máy in mạng offline hoặc workbook lớn |
| H18 | Nhật ký job có thể chặn mọi lệnh in | <ul><li>Mỗi lần ghi đọc lại và băm lại toàn bộ `print-job-events.jsonl` (`PrintJobStateStore.cs:72`), 5–6 lần cho mỗi lệnh in và thêm một lần lúc khởi động.</li><li>Nhật ký không xoay vòng.</li><li>Dòng cuối hỏng (ví dụ sau khi mất điện) làm mọi lệnh in bị chặn (`:73-80`), và UI không có cách sửa.</li></ul> | In chậm dần theo tháng. Sau sự cố mất điện, trạm in không in được cho tới khi có người sửa file ẩn |
| H19 | Cảnh báo phục hồi bị nhiễu | Lệnh in thành công dừng ở `SpoolAccepted`/`QueueObserved`, không phải trạng thái kết thúc, nên luôn nằm trong "Xem lại N lệnh in" (`PrintJobRecoveryService.Analyze`). Log thật: 6/6 lệnh thành công còn trong danh sách | Người vận hành quen bỏ qua cảnh báo, kể cả cảnh báo thật |
| H20 | Cảnh báo in trùng không bao giờ bật | Dòng chỉ được đánh dấu đã in khi tem được xác nhận vật lý (`PrintPreviewWindow.xaml.cs:683-690`), điều app không bao giờ tạo ra | In lặp các dòng (ví dụ số serial) mà không có cảnh báo |
| H21 | In thử sửa mẫu đang mở | `PrinterSetupWindow` nhận mẫu đang mở (`MainWindow.xaml.cs:1129`) và ghi máy in, offset, scale, DPI vào đó trước khi bấm OK (`PrinterSetupWindow.xaml.cs:725-730`) | Bấm Hủy vẫn đổi thiết lập in của mẫu, trái RULES §1 |
| H22 | Ba đường in khác nhau | `PrintPreviewWindow.Print_Click` (436 dòng), in nhanh `DispatchTrackedPrintAsync`, và `NiceLabelJobRunner` (không ghi bằng chứng job bền vững) | Ba bộ quy tắc có thể lệch nhau |
| H23 | Có lần dừng in tránh được | <ul><li>Ngày 2026-09-15 có 4 lần dừng in trên XP-DT426B do "missing verified driver-ticket evidence"; bản 0.288 (chưa commit, chưa cài) ghi đã sửa nguyên nhân.</li><li>Mẫu gắn tên driver cứng sẽ bị chặn trên máy có tên driver khác dù cùng khổ và DPI (`DocumentPrinterIdentityContract.cs:56-60`).</li></ul> | Ca in dừng vì metadata driver, không phải vì tem sai |
| H24 | Log in thiếu danh tính mẫu | 11/11 lệnh in thật có `TemplateFilePath` rỗng. *Đính chính 2026-10-13:* log đã ghi `TemplateName`, `DocumentHash` và `ManifestFingerprint`; đường dẫn chỉ rỗng khi in từ mẫu chưa lưu, và cả 11 lệnh đều in từ mẫu thư viện mở thành bản chưa lưu (22, 23). Không đổi schema log | Khó truy ngược tem đã in từ mẫu nào (P08) |
| H25 | Thông báo kỹ thuật tiếng Anh | Khoảng 76 lệnh `throw new …Exception("…")` tiếng Anh ở Core/Data/Printing/Project có thể hiện lên hộp thoại. Thông điệp phục hồi và menu chuột phải của canvas còn tiếng Anh | Người vận hành không hiểu lỗi, không biết làm gì tiếp |

### 2.4 Bề mặt và mã nguồn

| # | Quan sát | Bằng chứng | Ảnh hưởng |
| --- | --- | --- | --- |
| H26 | Cửa sổ không còn đường mở | 9 cửa sổ: OperationsOverview, PrintQueueConsole, Analytics, LocalMaintenance, DataWorkspace, DocumentLibrary, DocumentWorkflow, Automation, AutomationConfiguration. Kèm theo là handler mồ côi trong `MainWindow.xaml.cs` và XAML chết ở tab Dữ liệu | Mã phải duy trì, dịch và test mà không phục vụ ai |
| H27 | Mã ngoài lõi lớn | <ul><li>Automation thả file: khoảng 3.556 dòng.</li><li>NiceLabel JOB: khoảng 1.608 dòng, chỉ test gọi tới.</li><li>`TRIAL_BUILD` và installer Trial/Commercial không còn build được.</li><li>`tools/KeyProvisioner` chỉ còn bin/obj.</li></ul> | Tăng thời gian build, test và dịch; gây hiểu nhầm phạm vi |
| H28 | Mã vạch rộng hơn nhu cầu | UI chào 15 loại. Mẫu thật (`form tem nhãn`, thư viện) chỉ dùng Code 128 và QR. UPC-E, Codabar, MSI, Plessey chưa có bằng chứng decode | Có thể in loại mã chưa nghiệm thu (B07) |
| H29 | Thiết kế nặng với mẫu có ảnh | <ul><li>Mỗi thay đổi thuộc tính serialize toàn bộ mẫu, kể cả ảnh base64, để chụp undo; giữ tới 200 bản (`MainViewModel.cs:4151/4353`).</li><li>Thêm ảnh không giới hạn kích thước.</li><li>Khoảng 87 binding của bảng thuộc tính được làm mới ở mỗi nhịp kéo.</li></ul> | Chậm và tốn bộ nhớ trên máy yếu |
| H30 | Điểm nóng mã nguồn | `LabelDesignerCanvas.cs` 5.812 dòng; `MainViewModel.cs` 4.946 dòng (file, thiết kế, dữ liệu, in và phục hồi trong một lớp); `PrintPreviewWindow.xaml.cs` 1.656 dòng; `ANLAbel.Tests/Program.cs` 11.439 dòng | Sửa một chỗ dễ ảnh hưởng chỗ khác; khó review |

Nhận xét về xu hướng:

- 40 version gần nhất (0.267–0.306) chủ yếu là sửa lỗi.
- Bề mặt ngoài lõi đến từ hai đợt mở rộng trước đó: 0.212–0.224 (automation,
  workflow, Control Center) và 0.258–0.263 (auto-update, mở rộng mã vạch, print
  method).
- Vì vậy việc cần làm bây giờ là **dọn phần dư, khóa quy trình và vá lỗ hổng vận
  hành**, không chỉ "dừng thêm tính năng".

### 2.5 Phần đang tốt — giữ nguyên khi sửa

- **Lưu và mở mẫu.**
  - Ghi file tạm có write-through rồi đổi tên.
  - `.bak` chỉ xoay vòng từ bản chính hợp lệ.
  - App từ chối schema mới hơn và giữ trường lạ.
  - Mẫu mới được parse xong rồi mới thay mẫu đang mở.
  - Khôi phục phiên bản kiểm tra nguồn và đổi file nguyên khối.
- **In.**
  - Chạy trên luồng riêng, với bản sao của mẫu và dữ liệu.
  - Bắt buộc named queue; bằng chứng trước khi gửi là bắt buộc.
  - In lại phải có manifest được duyệt; không tự retry.
- **Xem trước.**
  - Có thể hủy, bỏ qua lượt cũ, cache trang đã dựng.
  - Kiểm tra chạy ngoài luồng giao diện.
- **Excel.** Đọc streaming ngoài luồng giao diện, có hủy và timeout, chia sẻ file
  đọc-ghi, lỗi có kiểu rõ ràng.
- **Thiết kế.**
  - Một lần kéo hoặc resize là một bước undo; Hủy trả về khung ban đầu.
  - Cập nhật từng phần thay vì vẽ lại toàn bộ.
- **Vệ sinh mã.**
  - Không chặn chờ async.
  - Không `Dispatcher.Invoke` đồng bộ.
  - Cửa sổ gỡ đăng ký sự kiện khi đóng.
  - Không có TODO/FIXME.

---

## 3. Phạm vi lõi sau cải tổ

### 3.1 Luồng chính của người vận hành

```text
Tạo mới / mở mẫu / chọn từ thư viện
  → thiết kế (Text, TextBox, mã vạch, ảnh, hình, đường kẻ)
  → nối Excel/CSV, chọn dòng và số bản
  → chọn máy in và khổ tem
  → xem trước (cùng scene với lệnh in)
  → in → xem kết quả, đối soát khi kết quả chưa rõ
  → lưu, đóng, mở lại mà không mất gì
```

Mỗi việc trong kế hoạch phải làm luồng này **đúng hơn, ít dừng vô lý hơn, ít
đứng hình hơn hoặc dễ phục hồi hơn**. Việc không chạm luồng này được để sau kế
hoạch.

### 3.2 Phân loại tính năng

| Nhóm | Thành phần | Chính sách |
| --- | --- | --- |
| **Lõi** | <ul><li>Mẫu mới, Mở, Lưu, Lưu thành; backup và khôi phục.</li><li>Canvas thiết kế, bảng thuộc tính, undo/redo, căn chỉnh và snap, zoom.</li><li>Nhập Excel/CSV (chọn sheet, dòng tiêu đề); liên kết, cập nhật, bỏ liên kết; chọn dòng và số bản.</li><li>Cài đặt máy in và khổ tem; xem trước, preflight, in; nhật ký job và đối soát.</li></ul> | Mọi việc của kế hoạch tập trung ở đây |
| **Hỗ trợ lõi** | Thư viện mẫu; Phiên bản mẫu; Lịch sử in và Xuất Excel; Trung tâm in (đối soát, in lại có chủ đích); In thử và canh chỉnh; Quản lý nguồn dữ liệu dùng chung; Trợ giúp | Giữ. Chỉ sửa lỗi hoặc rút gọn khi cản luồng chính; không thêm chức năng |
| **Ngoài lõi, đóng băng** | Cập nhật qua GitHub (phần mã mạng duy nhất); cửa sổ Cài đặt (chỉ còn một lựa chọn bị vô hiệu); công cụ AGY dispatcher, đồng bộ icon Figma, IconGenerator | Không mở rộng. Giữ hay gỡ do §8 quyết định |
| **Ngoài lõi, đề xuất gỡ khỏi bản phát hành** | 9 cửa sổ không truy cập được, handler mồ côi, XAML chết; automation thả file; NiceLabel JOB runner; `TRIAL_BUILD`, installer Trial/Commercial, `tools/KeyProvisioner` | Đã duyệt (§8). Gỡ qua commit riêng, nên vẫn khôi phục được từ git. Giữ phần engine mà mẫu cũ cần để mở và in đúng, ví dụ các biến đổi dữ liệu đã lưu trong mẫu |

Lý do đề xuất gỡ là thành phần **không còn đường sử dụng**, **không có nơi gọi
trong sản phẩm** hoặc **trái hợp đồng sản phẩm** (không editions). Lý do không
phải là "xuất phát từ nghiên cứu cũ". Quyết định gỡ vẫn thuộc chủ sản phẩm, đúng
với [`../REFERENCE_POLICY.md`](../REFERENCE_POLICY.md) và mục 1 của quy chuẩn lõi.

### 3.3 Mã vạch

Mọi mẫu cũ phải **mở và in đúng** như trước. Đề xuất (chờ QĐ-8):

- Danh sách tạo mới chỉ gồm các loại đã có bằng chứng decode trong matrix B07
  hiện hành.
- UPC-E, Codabar, MSI và Plessey rời danh sách tạo mới cho tới khi có fixture
  dương/âm và decode round-trip.
- *Cập nhật 2026-10-13:* điều kiện trên đã đạt cho cả bốn loại. Từ v0.306,
  `ImportedBarcodeRoundTripTests` chạy đủ 15 loại, và fixture âm đã có sẵn. Vì
  vậy không loại nào bị ẩn. Xem ma trận B01–B07 trong
  [`../CORE_STANDARD_EVIDENCE_AUDIT.md`](../CORE_STANDARD_EVIDENCE_AUDIT.md).

---

## 4. Lộ trình

**Kích thước** là ước lượng tương đối, không phải cam kết: S ≤ 1 ngày,
M 2–3 ngày, L ≥ 4 ngày.

**Thứ tự giai đoạn:** GĐ1 và GĐ2 có thể chạy xen kẽ. Nếu QĐ-4…QĐ-6 chưa được
duyệt thì làm GĐ2 trước.

**Lỗi đang cản ca in:** nếu người vận hành báo một lỗi đang cản việc in, lỗi đó
được xử lý ngay ở bất kỳ giai đoạn nào. Quy trình: tái hiện, sửa nhỏ nhất qua
đường hiện có, thêm regression.

**Giao diện:** mọi thay đổi người dùng nhìn thấy cần node Figma trước khi làm WPF,
theo [`../figma-ui-handoff-template.md`](../figma-ui-handoff-template.md).

**Vùng được bảo vệ:** khi sửa `MainViewModel`, `LabelDesignerCanvas`,
`MainWindow` hoặc các renderer, không chạm phần Text/TextBox được bảo vệ. Danh
sách vùng này nằm trong `AGENTS.md`.

### GĐ0 — Khóa nền (làm trước mọi việc khác)

| Việc | Nội dung | Xong khi | Cỡ |
| --- | --- | --- | --- |
| 0.1 Bảo toàn worktree | <ul><li>Commit 184 mục theo phân hệ, đúng [`../COMMIT_WORKFLOW.md`](../COMMIT_WORKFLOW.md).</li><li>Thứ tự: Core/contracts → Data → Printing → App → test → tài liệu → release.</li><li>Gồm cả quy chuẩn lõi và các test bằng chứng đang untracked.</li><li>Không đưa vào repo dữ liệu khách hàng hay tài liệu/ảnh NiceLabel.</li></ul> | `git status` chỉ còn dữ liệu khách hàng (đã ignore); mỗi commit build và Fast đều xanh | M |
| 0.2 Version có lịch sử | <ul><li>Gộp 0.289–0.306 thành một mục changelog 0.306, dựng từ mã ngày 10-04/05.</li><li>Từ nay chỉ tăng version trong commit phát hành.</li></ul> | Mỗi version có một commit và một dòng changelog | S |
| 0.3 Đưa bản đã kiểm tới người dùng | Build từ commit sạch, chạy `deploy-desktop.ps1`, ghi lại version đã cài | Bản cài trùng commit phát hành mới nhất | S |
| 0.4 Cô lập dữ liệu khi chạy test | <ul><li>Một chỗ duy nhất quyết định thư mục dữ liệu app, có cách cho test trỏ sang thư mục tạm.</li><li>Thêm xoay vòng log.</li><li>Lưu trữ log hiện tại; không xóa.</li></ul> | Chạy Fast không đổi file nào trong `%LOCALAPPDATA%\ANLAbel` và `%APPDATA%\ANLAbel` | M |
| 0.5 Một nguồn giao việc | AGY dispatcher chỉ đọc checklist §9 của tài liệu này, thay cho `PLAN.md`, `MASTER_PLAN.md` và `*HANDOFF*.md` | Dry-run chỉ liệt kê việc của §9 | S |
| 0.6 Đối chiếu bằng chứng | <ul><li>Trên commit đã khóa: chạy Fast.</li><li>Rà lại 13 ID có mâu thuẫn so với mã ngày 10-04/05.</li><li>Cập nhật `CORE_STANDARD_EVIDENCE_AUDIT.md`; mỗi trạng thái (Pass, Fail, Not verified) ghi kèm lệnh, commit và thời điểm.</li></ul> | Bảng 32 ID có trạng thái gắn với một commit cụ thể | M |

### GĐ1 — Gọn phạm vi (không đổi hành vi luồng lõi)

| Việc | Nội dung | Xong khi | Cỡ |
| --- | --- | --- | --- |
| 1.1 Gỡ bề mặt chết | <ul><li>Gỡ 9 cửa sổ không truy cập được, handler mồ côi, XAML chết và command không gắn với nút nào.</li><li>Test chỉ phục vụ các cửa sổ đó đi cùng.</li><li>Giữ engine mà mẫu cũ cần.</li></ul> | Build và Fast xanh. Toàn bộ mẫu thư viện và mẫu thật mở, lưu, xem trước như trước | M |
| 1.2 Đưa automation ra khỏi bản phát hành | Chuyển automation thả file và NiceLabel JOB runner sang nhánh lưu trữ; test tương ứng đi cùng. Việc này cũng bớt một trong ba đường in (H22) | Bản phát hành không còn mã này; lịch sử git giữ nguyên | M |
| 1.3 Gỡ edition và trial | Gỡ `TRIAL_BUILD`, installer Trial/Commercial và thư mục rác `tools/KeyProvisioner`; giữ một installer | Script version và test chỉ còn một installer | S |
| 1.4 Một hướng dẫn tài liệu | <ul><li>Chuyển 42 gói ngoài phạm vi vào `docs/archive/` kèm README.</li><li>Sửa `CONTRIBUTING.md` trỏ về 07 → 13.</li><li>Ghi *Superseded* cho phần mâu thuẫn của ADR-008, ADR-010, ADR-020.</li><li>Đồng bộ `agent.md` quy tắc 2, 5, 10, 14.</li><li>Bỏ nhãn "active" khỏi tài liệu lịch sử.</li><li>Script version không ghi đè nội dung tài liệu tường thuật.</li></ul> | Câu hỏi "làm gì tiếp" chỉ có một câu trả lời: 07 → 13 | M |
| 1.5 Mã vạch theo bằng chứng | Áp dụng §3.3 cho danh sách tạo mới; mẫu cũ vẫn mở và in được | Fixture mở mẫu cũ dùng loại bị ẩn vẫn xanh | S |

### GĐ2 — File: tạo, mở, lưu không mất dữ liệu

Đề xuất bổ sung nhóm ID F vào quy chuẩn lõi (QĐ-9). Đây là lỗi mất dữ liệu trong
luồng lõi, nên phần sửa không chờ QĐ-9; chỉ phần UI cần node Figma trước.

| ID | Yêu cầu | Nghiệm thu |
| --- | --- | --- |
| F01 | Biết khi nào có thay đổi chưa lưu | <ul><li>Sửa mẫu thì tiêu đề có dấu `*`.</li><li>Undo về đúng trạng thái đã lưu thì dấu `*` biến mất.</li><li>Khi còn thay đổi, Tạo mới, Mở, Mở từ thư viện và Đóng đều hỏi *Lưu / Không lưu / Hủy*. Chọn Hủy thì phiên giữ nguyên.</li></ul> |
| F02 | Lưu thành và phím tắt | Có "Lưu thành…" (Ctrl+Shift+S). Ctrl+N/O/S chạy đúng như tooltip và Trợ giúp mô tả |
| F03 | Không có lỗi im lặng, không sập mất phiên | <ul><li>Bắt lỗi toàn cục: ghi crash log, lưu bản phục hồi của mẫu đang mở (không ghi đè file gốc), báo bằng tiếng Việt.</li><li>Mọi lệnh bất đồng bộ có xử lý lỗi mặc định: hiện trạng thái hoặc thông báo và ghi log, không chỉ `Debug`.</li><li>Model từ chối NaN và vô cực.</li><li>Không bao giờ tự gửi lại lệnh in.</li></ul> |
| F04 | Bản phục hồi tự động | Lưu định kỳ vào `%LOCALAPPDATA%\ANLAbel\Recovered`. Lần mở sau đề nghị khôi phục hoặc bỏ. Bản cũ được dọn |
| F05 | Khởi động bền | File cấu hình hoặc registry hỏng, hay có schema mới hơn, được cách ly (đổi tên); app dùng giá trị mặc định và báo cho người dùng. App vẫn mở |
| F06 | Mở file từ Explorer | Mở đúp file `.anlabel` mở đúng file đó |
| F07 | Lưu nhất quán | <ul><li>Lưu từ một bản chụp của mẫu.</li><li>Từ chối lần lưu thứ hai khi đang lưu.</li><li>Mọi lỗi lưu, kể cả lỗi lưu trữ phiên bản, được báo rõ bằng tiếng Việt; thay đổi vẫn còn trong app và trong bản phục hồi.</li><li>Giữ thiết kế an toàn hiện có của `ProjectFileService`: khi chưa lưu trữ được bản trước thì không ghi đè tệp (quyết định 2026-10-13, ưu tiên giữ dữ liệu hơn tính sẵn sàng).</li></ul> |
| D06 | Round-trip mẫu | 22 mẫu thư viện, 5 mẫu trong `form tem nhãn/anlabel` và fixture có ảnh, xoay, nhóm: mở → lưu → mở lại, nội dung không đổi |

### GĐ3 — Thiết kế mượt và đúng (D01–D07)

Đóng đúng các gap đã ghi, mỗi gap có test tái hiện. Không refactor lớn
`LabelDesignerCanvas`/`MainViewModel` chỉ để mã gọn hơn. Chỉ tách phần cần thiết
khi một lỗi cần seam kiểm thử.

- **D02 — undo.**
  - Một gesture là một bước undo cho resize, xoay, nhóm và dán.
  - Thay đổi liên kết dữ liệu (file, sheet, dòng tiêu đề, cột khóa, cột số bản)
    phải là bước undo riêng, hoặc nằm ngoài snapshot undo, để undo không âm thầm
    trả về liên kết cũ (H16).
- **D07 — tài nguyên thiếu.** Font hoặc ảnh thiếu phải báo ngay trên canvas,
  không đợi tới preflight in. Phiên sửa phải còn nguyên sau khi render lỗi.
- **D01/D03/D05 — tương tác thật.** Kiểm tra bằng chuột, Esc và mất capture ở
  zoom 50/100/200% và Windows scaling 100/125/150%, như mục 7 của quy chuẩn.
- **Độ mượt (H29).**
  - Snapshot undo không nhân bản ảnh base64.
  - Giới hạn hoặc thu nhỏ ảnh khi chèn.
  - Giảm tần suất làm mới bảng thuộc tính khi kéo.
  - Đo trước, đặt ngưỡng sau: độ trễ kéo/thả và zoom với mẫu thật lớn nhất và với
    200 object; thời gian mở mẫu.
- **Ngôn ngữ.** Menu chuột phải của canvas chuyển sang tiếng Việt. Mục dành cho
  lập trình viên "Show pointer performance" rời menu của người dùng.

### GĐ4 — Dữ liệu và thông tin trên tem (Excel/CSV, DB01–DB10)

- **Không in nội dung sai.**
  - DB02: mất số 0 đầu khi Excel thiếu number format.
  - DB04: công thức không có giá trị cache.
  - DB10: người vận hành bỏ qua dòng lỗi rồi in tiếp.
  - DB08: copies rỗng/âm bị đổi thành 1 và bị cắt ở 999; kiểm tra lại với
    `CopiesFieldContract` mới.
- **Không in dữ liệu cũ (DB05/DB06/DB07, H15).**
  - Danh tính file đọc bằng chế độ chia sẻ đọc-ghi.
  - Danh tính lấy lúc nhập được chuyển vào xem trước và hiển thị khi so sánh.
  - In nhanh kiểm tra lại danh tính trước khi gửi.
  - Hai lần nhập chồng nhau không được công bố sai thứ tự.
  - Danh tính dòng không phụ thuộc vị trí trong bảng.
- **Không thất bại im lặng (H14, H16).**
  - "Cập nhật Excel" báo lỗi rõ: file bị khóa, thiếu sheet, timeout.
  - Dòng tiêu đề và đường dẫn relink chỉ được ghi vào mẫu sau khi nhập thành công.
- **CSV.** Chốt [`../CSV_PARSER_FAILURE_POLICY_ADR.md`](../CSV_PARSER_FAILURE_POLICY_ADR.md)
  (đang *Proposed*). Luồng nhập tương tác đang âm thầm bù cột cho dòng thiếu cột.
- **Hiệu năng và giới hạn.**
  - Tái hiện lỗi treo khi nhập Excel trên máy 4 GB bằng bản hiện tại, rồi đóng lỗi
    hoặc ghi rõ giới hạn.
  - Đo với 1, 1.000 và 10.000 dòng: thời gian nhập, thời gian mở xem trước, bộ nhớ.
    Công bố giới hạn đã đo.
- **Thông báo lỗi.** Lỗi dữ liệu phải nêu file, sheet, dòng, cột và cách sửa, bằng
  tiếng Việt. Lỗi lặp được gộp theo nguyên nhân.
- **Mã vạch xuyên suốt.** Fixture dữ liệu của giai đoạn này chạy qua kiểm tra
  payload mã vạch B01/B02 ngay lúc đó. Payload lỗi phải chặn bước in. Không đợi
  tới GĐ5, theo mục 8.3 của quy chuẩn lõi.

### GĐ5 — Mã vạch, xem trước và in (B01–B07, P01–P08)

- **Một đường in (H22).** In nhanh đi chung đường với xem trước: đọc contract và
  kiểm tra bất đồng bộ, cùng bằng chứng job, cùng kiểm tra lại sát lúc gửi.
- **Không đứng hình (H17).**
  - Liệt kê máy in, đọc printer contract và băm file Excel đều chạy ngoài luồng
    giao diện.
  - Khi spooler lỗi, báo đúng lỗi spooler thay vì "không có máy in".
- **Nhật ký job bền (H18).**
  - Ghi nối tiếp, không đọc lại toàn bộ file mỗi lần ghi.
  - Xoay vòng theo kích thước hoặc ngày.
  - Dòng cuối hỏng được tự cách ly, có thông báo và để lại bằng chứng.
  - Chỉ coi nhật ký là hỏng khi nó thật sự hỏng.
  - An toàn khi hai cửa sổ app cùng chạy.
- **Cảnh báo có ý nghĩa (QĐ-10, H19/H20).**
  - Lệnh in thành công bình thường không nằm trong "Xem lại". Chỉ trạng thái
    Unknown, app dừng giữa chừng hoặc hàng đợi báo lỗi mới cần đối soát.
  - Dòng đã gửi tới máy in được đánh dấu "đã gửi" để cảnh báo in trùng hoạt động.
  - Vẫn giữ nguyên tắc "spool accepted không có nghĩa là tem đã ra".
- **In thử không sửa mẫu (H21).** In thử chạy trên bản sao. Hủy thì mẫu không đổi.
- **Dừng in đúng lúc (H23).**
  - Phân loại lại các điểm chặn theo ADR-021: chỉ chặn khi điều đó ảnh hưởng tới
    tính đúng của tem; thiếu metadata không thiết yếu chỉ là cảnh báo trong chi tiết.
  - Ca tái hiện: XP-DT426B ngày 2026-09-15.
  - Trường hợp tên driver khác nhưng cùng khổ, DPI và hàng đợi cần ca tái hiện và
    ADR (QĐ-12) trước khi đổi.
- **Kết quả trung thực.**
  - P07: exception xảy ra sau khi đã gửi phải thành Unknown, không phải Failed.
  - P08: lỗi lưu bằng chứng bắt buộc phải chặn trước khi gửi; kiểm tra lại với
    `PrintJobEvidenceAppend` mới.
  - P04: manifest giữ đúng thứ tự record/copy.
  - P05/P06: kiểm tra lại sát lúc gửi; chống nhấp đúp ở cấp cửa sổ.
- **Truy vết và ngôn ngữ (H24/H25).**
  - Log in ghi tên mẫu và đường dẫn, hoặc ghi "chưa lưu".
  - Exception kỹ thuật được ánh xạ thành câu tiếng Việt nêu vấn đề,
    dòng/đối tượng/job bị ảnh hưởng và việc cần làm. Chi tiết kỹ thuật để trong
    phần "Chi tiết".
- **Mã vạch.** Matrix B01–B07 cho các loại được giữ lại. Fixture lấy từ mẫu thật
  (Code 128, QR).

### GĐ6 — Nghiệm thu vận hành thật

- **Bộ mẫu nghiệm thu**, giữ cục bộ và không commit: 5 mẫu trong
  `form tem nhãn/anlabel`, các datasheet REDACTED-14/CUSTOMER-1/REDACTED-10, cùng 22 mẫu thư viện.
- **Máy in**: Xprinter XP-DT426B 203 dpi (máy đang dùng) và XP-470B. Cấu hình khác
  chỉ nghiệm thu khi có máy.
- **Kịch bản**:
  - Luồng chính từ đầu tới cuối.
  - Hết giấy, offline, tạm dừng hàng đợi.
  - Đổi ticket sau khi xem trước; khởi động lại spooler.
  - Mất điện giữa lúc ghi nhật ký.
  - Kết quả chưa rõ, dẫn tới đối soát.
- **Ba mức bằng chứng tách riêng**: capability của driver, spool, và tem thật
  (quét được, đúng dữ liệu).
- Chỉ đóng một ID khi có bằng chứng ở đúng mức tương ứng. Không gửi lệnh in thật
  để lấy bằng chứng khi chủ sản phẩm chưa cho phép.

---

## 5. Luật làm việc trong thời gian cải tổ

1. **Một nguồn việc.** Chỉ checklist §9 tạo việc. `PLAN.md`, `MASTER_PLAN.md`,
   `*HANDOFF*`, `CC_*` và các decision packet cũ là lịch sử. Quy tắc này áp dụng
   cho cả người, agent và dispatcher.
2. **Mỗi lát công việc thuộc đúng một loại:**
   - (a) sửa lỗi tái hiện được trong luồng lõi;
   - (b) gỡ hoặc giảm độ phức tạp ngoài lõi;
   - (c) thêm test hoặc fixture để đóng một ID;
   - (d) chẩn đoán một lỗi vận hành.

   Tính năng mới chỉ được làm khi qua 4 câu hỏi của
   [`../REFERENCE_POLICY.md`](../REFERENCE_POLICY.md) **và** được chủ sản phẩm duyệt.
3. **Mỗi lúc chỉ làm một lát.** Lát hiện tại phải xong (test xanh, commit khi được
   phép) trước khi mở lát mới. Không để thay đổi chưa commit chồng qua nhiều lát.
4. **Báo xong kèm bằng chứng.** Ghi lệnh đã chạy và kết quả. Chỉ ghi "Pass" khi có
   test hoặc fixture cụ thể; file test tồn tại chưa phải là Pass.
5. **Không sinh tài liệu thừa.** Lát nhỏ không cần decision packet, handoff hay
   audit mới; chỉ cập nhật §9 và changelog phát hành.
6. **Những gì được bảo vệ vẫn giữ nguyên:**
   - hợp đồng Text/TextBox;
   - cổng an toàn in, named queue, không tự gửi lại lệnh in;
   - UI qua Figma trước;
   - Mutation ≥ 90 khi sửa mã an toàn tem.
7. **Phát hành nhỏ và đều.**
   - Gom các lát đã xanh thành một bản, commit `release:`, cài lên desktop và ghi
     changelog.
   - Không có version nào chỉ tồn tại trong worktree.

---

## 6. Chỉ số theo dõi

| Chỉ số | Hiện tại (2026-10-13) | Mục tiêu khi xong kế hoạch |
| --- | --- | --- |
| Mục chưa commit | 184 | 0 sau mỗi lát |
| Bản cài so với bản phát hành | 0.266 so với 0.306 | Trùng nhau |
| ID quy chuẩn có bằng chứng Pass | 0/32 (F01–F07 chưa có) | Tất cả ID ở mức phần mềm; phần vật lý ghi rõ phạm vi đã nghiệm thu |
| Thao tác làm mất thay đổi chưa lưu | Tạo mới, Mở, Mở từ thư viện, Đóng, crash | Không còn |
| Lệnh lỗi chỉ ghi `Debug` trên luồng chính | 8 lệnh bất đồng bộ | 0 |
| Thao tác chạy đồng bộ trên luồng giao diện khi gọi driver hoặc băm file | In nhanh, liệt kê máy in, băm Excel lúc xem trước | 0 |
| Đường in độc lập | 3 | 1 |
| Lệnh in thành công nằm trong "Xem lại" | 6/6 | 0 |
| Dừng in tránh được (`print-operations.jsonl`) | 4 lần trong ngày 2026-09-15 | 0 |
| Dòng log do test ghi vào máy thật | 22.364 | 0 |
| Cửa sổ trong app | 25 (9 không truy cập được) | Chỉ còn cửa sổ lõi và hỗ trợ lõi |
| Thông báo tiếng Anh tới người vận hành trên luồng chính | Khoảng 76 nguồn exception, thông điệp phục hồi, menu canvas | 0 |

---

## 7. Không làm trong kế hoạch này

- Không thêm bề mặt sản phẩm mới: Control Center, analytics, approval workflow,
  automation console, PowerForms, SQL/ODBC, cloud/login, scripting, nhiều tem
  trên một trang.
- Không viết lại UI framework, không thêm renderer thứ hai, không làm Viewport V2.
- Không refactor lớn chỉ để mã gọn hơn.
- Không đổi hợp đồng Text/TextBox.
- Không nới cổng an toàn để "in cho qua". Không tự gửi lại lệnh in chưa rõ kết quả.
- Không gửi lệnh in thật để thu bằng chứng khi chưa được cho phép.

---

## 8. Quyết định cần chủ sản phẩm duyệt

Ngày 2026-10-13 chủ sản phẩm chỉ thị "thực thi cho tới khi xong". Mọi quyết
định dưới đây được duyệt theo phương án đề xuất. Những quyết định có điều kiện
ghi rõ phương án được chọn trong cột Trạng thái.

| QĐ | Câu hỏi | Đề xuất | Nếu không duyệt | Trạng thái |
| --- | --- | --- | --- | --- |
| QĐ-1 | Có commit 184 mục theo phân hệ trên nhánh `codex/promote-anlabel` không? | Có, trước mọi việc khác | Mọi lát sau tiếp tục chồng lên worktree chưa được bảo toàn | Đã duyệt 2026-10-13 (commit cục bộ, không push) |
| QĐ-2 | Chỉ tăng version ở commit phát hành (sửa `agent.md` quy tắc 2 và `VERSIONING.md`)? | Có | Version tiếp tục tăng mà không có lịch sử | Đã duyệt 2026-10-13 |
| QĐ-3 | AGY dispatcher chỉ lấy việc từ §9? | Có | Agent tiếp tục kéo việc Control Center và parity | Đã duyệt 2026-10-13 |
| QĐ-4 | Gỡ 9 cửa sổ không truy cập được và mã chết đi kèm? | Có. Người dùng không bị ảnh hưởng vì đã không còn đường mở | Tiếp tục duy trì, dịch và test mã không ai dùng | Đã duyệt 2026-10-13 |
| QĐ-5 | Đưa automation thả file và NiceLabel JOB ra khỏi bản phát hành (giữ trong git)? | Có, trừ khi có khách hàng đang cần in tự động khi thả file | Khoảng 5.100 dòng mã và các file test liên quan tiếp tục tốn công bảo trì; vẫn còn một đường in thứ ba | Đã duyệt 2026-10-13 (giữ trong nhánh `archive/automation-2026-10`) |
| QĐ-6 | Gỡ trial, commercial và edition? | Có. Hợp đồng sản phẩm đã ghi "không editions" | Script version và test tiếp tục phải lo 3 installer | Đã duyệt 2026-10-13 |
| QĐ-7 | Cập nhật qua GitHub: giữ hay gỡ? | Giữ dạng người dùng tự bấm nếu đang phát hành qua GitHub (và sửa version cứng "0.258"); gỡ nếu cài tay tại xưởng | Bản cài báo sai version khi kiểm tra cập nhật | Đã duyệt 2026-10-13: giữ dạng tự bấm, vì repo phát hành qua GitHub Releases; sửa version cứng |
| QĐ-8 | Danh sách mã vạch tạo mới chỉ gồm loại đã có bằng chứng (§3.3)? | Có. Mẫu cũ vẫn mở và in được | Tiếp tục chào loại mã chưa nghiệm thu | Đã duyệt 2026-10-13 |
| QĐ-9 | Thêm nhóm F01–F07 vào mục 3 của `CORE_SOFTWARE_STANDARD.md`? | Có | Vòng đời file không có tiêu chí nghiệm thu | Đã duyệt 2026-10-13 |
| QĐ-10 | Lệnh in thành công bình thường không còn trong "Xem lại", và dòng đã gửi được đánh dấu để cảnh báo in trùng? Cần một ADR bổ sung cho ADR-016/017; vẫn không gọi là "đã in vật lý" | Có | Cảnh báo phục hồi tiếp tục bị nhiễu; in trùng không được cảnh báo | Đã duyệt 2026-10-13 (cần ADR bổ sung) |
| QĐ-11 | Chuyển 42 tài liệu ngoài phạm vi vào `docs/archive/`? | Có | Người và agent tiếp tục đọc nhầm tài liệu cũ | Đã duyệt 2026-10-13 |
| QĐ-12 | Mẫu ghi tên driver khác máy hiện tại nhưng cùng khổ, DPI và hàng đợi: cảnh báo kèm xác nhận lại, thay vì chặn? | Có, sau khi có ca tái hiện và regression. Vẫn chặn khi khổ, DPI hoặc hàng đợi sai | Mẫu dùng chung giữa các trạm in phải cài lại máy in ở từng máy | Đã duyệt có điều kiện 2026-10-13: chỉ đổi khi ca tái hiện và regression chứng minh an toàn |

---

## 9. Checklist thực thi (nguồn việc duy nhất)

- Mục có nhãn `[chờ QĐ-x]` chỉ được làm sau khi QĐ-x ở §8 có trạng thái
  "Đã duyệt". Từ 2026-10-13 mọi QĐ đã được duyệt; nhãn được giữ lại để truy vết.
- Mỗi mục xong phải ghi kèm lệnh kiểm tra và kết quả.
- Mục nào thay đổi giao diện người dùng thì cần node Figma trước.

**GĐ0 — Khóa nền**

- [x] 0.1 Commit worktree theo phân hệ theo `docs/COMMIT_WORKFLOW.md` `[chờ QĐ-1]` — xong 2026-10-13: `7a0a834`, `e43cc80`, `52a98d6`. Các thay đổi mã liên quan chéo nhau nên gom thành một commit release để commit nào cũng build được.
- [x] 0.2 Changelog 0.289–0.306 và quy tắc version mới `[chờ QĐ-2]` — xong 2026-10-13: `PLAN.md` mục v0.306; `VERSIONING.md` và quy tắc 2 của `agent.md` (`e43cc80`).
- [x] 0.3 Phát hành từ commit sạch và cài lên desktop `[chờ QĐ-1]` — xong 2026-10-13: bản cài 0.306.0.0 khởi động và đóng bình thường. Script deploy giờ đóng app nhẹ nhàng thay vì kill (`d7c4eea`).
- [x] 0.4 Một nguồn đường dẫn dữ liệu app, có override cho test, và xoay vòng log — xong 2026-10-13 (`9831415`): unit 1137/1137, regression 212/0. Ảnh chụp trước/sau cho thấy không file nào trong thư mục dữ liệu thật thay đổi.
- [x] 0.5 Dispatcher chỉ đọc §9 của tài liệu này `[chờ QĐ-3]` — xong 2026-10-13 (`5f1026c`): test dispatcher 14/14; dry-run chỉ liệt kê các mục §9.
- [x] 0.6 Cập nhật trạng thái 32 ID theo commit đã khóa — xong 2026-10-13 tại `fcfb9ad` (v0.308): bảng 32 ID cùng F01–F07, ba mức bằng chứng, trong `docs/CORE_STANDARD_EVIDENCE_AUDIT.md`.

**GĐ1 — Gọn phạm vi**

- [x] 1.1 Gỡ 9 cửa sổ không truy cập được, handler mồ côi và XAML chết `[chờ QĐ-4]` — xong 2026-10-13 (`a699825`): build 0/0, unit 1104/1104, regression 213/0. Mã cũ giữ ở nhánh `archive/automation-2026-10`.
- [x] 1.2 Đưa automation thả file và NiceLabel JOB ra khỏi bản phát hành `[chờ QĐ-5]` — xong 2026-10-13 (`a699825`). Giữ `FileDropPreparedBatchIdentity` vì manifest đã ghi cần kiểm chứng được.
- [x] 1.3 Gỡ `TRIAL_BUILD`, installer Trial/Commercial và `tools/KeyProvisioner` `[chờ QĐ-6]` — xong 2026-10-13 (`a699825`); `tools/KeyProvisioner` không còn trong repo.
- [x] 1.4 Chuyển tài liệu ngoài phạm vi vào `docs/archive/` và đồng bộ các tài liệu dẫn đường `[chờ QĐ-11]` — xong 2026-10-13 (`59acbcf`): 42 tài liệu, liên kết đã viết lại.
- [x] 1.5 Danh sách mã vạch tạo mới theo bằng chứng `[chờ QĐ-8]` — xong 2026-10-13: cả 15 loại đã có fixture dương/âm và decode round-trip, nên không loại nào bị ẩn (§3.3). `BarcodeEvidenceMatrixTests` (30 ca) chặn loại mới thiếu bằng chứng. Tên nhóm đổi thành “Mã vạch 1D” và “Mã 2D”. Trên thẻ Figma `37:142`, chỉ phần tên nhóm còn áp dụng.

**GĐ2 — File**

- [x] F01 Trạng thái "chưa lưu" và hỏi lưu khi Tạo mới, Mở, Mở từ thư viện, Đóng — xong 2026-10-13 (cả khi mở Phiên bản): gate `unsaved marker follows edits, undo and save`; Figma `36:141`, `36:150`.
- [x] F02 "Lưu thành…" và gắn Ctrl+N/O/S, Ctrl+Shift+S — xong 2026-10-13: nút `Shell.QuickAccess.SaveAs`, icon `save_as.png`, `ApplicationCommands`; Trợ giúp cập nhật; Figma `36:165`.
- [x] F03 Bắt lỗi toàn cục, crash log, bản phục hồi khi lỗi, xử lý lỗi mặc định cho lệnh bất đồng bộ, chặn NaN và vô cực — xong 2026-10-13: `App.xaml.cs`, `CrashReporter` (`logs/crash-*.log`, `logs/errors.log`); gate `async command failure reaches the operator`; unit `Non_finite_geometry_*`; Figma `36:188`.
- [x] F04 Tự lưu bản phục hồi và đề nghị khôi phục khi mở lại — xong 2026-10-13: mỗi 60 giây khi có thay đổi, `%LOCALAPPDATA%\ANLAbel\Recovered`; gate `recovery copy is offered once and opens unsaved`; Figma `36:201`.
- [x] F05 File cấu hình hỏng hoặc có schema mới hơn không chặn khởi động — xong 2026-10-13: `DataSourceRegistry.LoadOrQuarantine`; gate `corrupt data-source registry is quarantined at startup`; Figma `36:216`.
- [x] F06 Mở file `.anlabel` từ Explorer — xong 2026-10-13: `MainWindow.FindDocumentArgument`; gate `explorer argument opens only existing documents`.
- [x] F07 Lưu từ bản chụp, khóa nút Lưu khi đang lưu, lỗi lưu trữ phiên bản được báo rõ — xong 2026-10-13: gate `save writes a snapshot and refuses a concurrent save`; giữ fail-closed của bản lưu trữ phiên bản (xem bảng F ở GĐ2).
- [x] D06 Round-trip toàn bộ mẫu thư viện và mẫu thật — mẫu thư viện xong 2026-10-13: gate `library templates round-trip without geometry change` (22 mẫu, byte-stable, mở→lưu qua editor không đổi X/Y/W/H/xoay/nội dung). Mẫu khách hàng kiểm cục bộ, không commit (xem GĐ6).

**GĐ3 — Thiết kế**

- [x] D02 Undo một bước cho resize, xoay, nhóm và dán; liên kết dữ liệu không bị undo âm thầm — xong 2026-10-13. Undo, Redo và hủy thao tác giữ nguyên liên kết dữ liệu hiện tại (tệp, sheet, dòng tiêu đề, cột số bản). Trước đây Undo một thao tác không liên quan có thể trả liên kết về giá trị cũ (H16); gate này đỏ khi bỏ phần sửa. Dán nhiều đối tượng là một bước. Gate `undo and redo keep the current data link`, `rotation and multi-object gestures are one undo step`, `canvas paste and group resize are single gestures`. Chưa kiểm bằng chuột thật (D01/D03).
- [x] D07 Báo font hoặc ảnh thiếu ngay trên canvas — xong 2026-10-13: thanh trạng thái có chip “Thiếu 1 phông chữ, 1 ảnh” (Figma `37:259`). Chip có tooltip theo từng đối tượng: phông nào thiếu và đang thay bằng phông nào, ảnh nào không có dữ liệu. Bấm chip sẽ chọn đối tượng đầu tiên. Không vẽ đè lên tem. Gate `missing font or image is shown while editing`. Ảnh có dữ liệu nhưng hỏng vẫn chỉ được bắt ở kiểm tra trước khi in.
- [ ] D01/D03/D05 Kiểm tra tương tác thật ở các mức zoom và Windows scaling — *một phần, 2026-10-13:* kéo tay nắm thay đổi kích thước ở zoom 50/100/200 % cho đúng số mm và là một gesture (gate `handle resize keeps millimetres at every zoom`, đi qua tay nắm thật nhưng sự kiện kéo được tạo trong test). D05: gate `designer preview row keeps object geometry`. Còn lại: thao tác bằng chuột thật và Windows scaling 125/150 %, cần đổi thiết lập hiển thị nên phải kiểm trên máy thử.
- [x] Snapshot undo không nhân bản ảnh; giới hạn ảnh khi chèn; giảm làm mới bảng thuộc tính — xong 2026-10-13. Mỗi ảnh được giữ một lần trong lịch sử, snapshot chỉ chứa khóa nội dung. Với ảnh 2 MB, 31 bước undo giữ dưới 620 nghìn ký tự thay vì khoảng 62 triệu; bản phục hồi và bản lưu vẫn có đủ ảnh. Ảnh trên 10 MB, trên 25 megapixel hoặc không đọc được bị từ chối khi chèn hay thay, kèm lý do. Khi kéo hoặc đổi kích thước, bảng thuộc tính làm mới một lần lúc thả thay vì ở mọi nhịp (100 nhịp: 0 lần trong lúc kéo, 1 lần khi thả). Gate `undo history keeps each image once and restores it`, `canvas gesture refreshes the property panel once`, `oversized or unreadable images are refused on insert`.
- [x] Menu chuột phải canvas tiếng Việt; bỏ mục dành cho lập trình viên — xong 2026-10-13: menu và mọi thông báo trạng thái của canvas (dính, lưới, đường gióng, căn, phân bố, dịch chuyển) là tiếng Việt. Mục “Show pointer performance” đã bỏ; thuộc tính chẩn đoán vẫn giữ cho test. Gate `canvas context menu is Vietnamese without developer items`. Hai thông báo tràn chữ của Text/TextBox vẫn tiếng Anh vì nằm trong vùng được bảo vệ (AGENTS.md).
- [x] Đo độ trễ thao tác với mẫu thật lớn nhất và 200 object — xong 2026-10-13 (gate `design stays responsive with 200 objects`, in dòng MEASURE). Mẫu 200 đối tượng: mở 28 ms, sửa thuộc tính 4 ms/lần, hoàn tác 21 ms, dựng canvas 62 ms. Kéo lúc đầu đo được 48,7 ms/nhịp vì mỗi khung hình kiểm lại lỗi của mọi mã vạch và khung chữ; nay chỉ kiểm lại đối tượng đang thao tác, còn 6 ms/nhịp. Mẫu thật lớn nhất “tem ngoai REDACTED-14” (49 đối tượng, chỉ có trên máy này): kéo 3,6 → 0,8 ms/nhịp. Số đo trên máy phát triển; máy 4 GB chưa đo.

**GĐ4 — Dữ liệu**

- [x] DB02, DB04, DB10: không in nội dung sai (mất số 0 đầu, công thức không cache, bỏ qua dòng lỗi) — xong 2026-10-13. DB04: ô có công thức nhưng tệp chưa lưu giá trị đã tính (thường do phần mềm khác ghi tệp) trước đây đọc thành ô trống; nay việc nhập bị chặn và nêu ô, ví dụ B3 (`XlsxFormulaValueInspector`, unit `A_formula_without_a_calculated_value_stops_the_import_at_its_cell`). Ô báo lỗi Excel và định dạng số không hỗ trợ đã bị chặn từ 0.282/0.286. DB02: chuỗi `000123`, khoảng trắng và định dạng số `000000` giữ đúng (`DataPayloadPreservationTests`, `ExcelNumericPayloadTests`). Số General không có số 0 đầu trong dữ liệu nên không có gì bị mất. DB10: lỗi ở bất kỳ dòng đã chọn nào chặn cả lô. Bỏ dòng lỗi chỉ xảy ra khi người vận hành bấm Có, các dòng đó được bỏ chọn, phần còn lại được kiểm tra lại, và kết quả in nêu các dòng đã bỏ.
- [x] DB05, DB06, DB07, DB08: danh tính file và dòng, refresh nguyên khối, copies — xong 2026-10-13. DB05: khi hai lần nhập chồng nhau, chỉ lần mới nhất được công bố; lần cũ chậm hơn bị bỏ (gate `overlapping imports publish only the latest`). DB06: danh tính tệp đọc với chia sẻ đọc-ghi (`d61d6db`) và được băm trước khi công bố. In nhanh băm lại tệp ngay trước khi in, nên phát hiện cả trường hợp thay nội dung mà giữ nguyên kích thước và giờ sửa, điều file watcher không thấy (gate `quick print rechecks the linked file before printing`). DB07: số dòng và thứ tự in theo tệp nguồn, không đổi khi sắp xếp bảng dữ liệu (gate `record numbers follow the source file, not the grid sort`). DB08: ô số bản trống, âm, thập phân hoặc vượt giới hạn chặn cả lô, không bị đổi thành 1 hay cắt bớt (`CopiesFieldContractTests`, `DatabaseConfigCopiesTests`, `QuickPrintQuantityTests`).
- [x] "Cập nhật Excel" báo lỗi rõ; thiết lập liên kết chỉ ghi sau khi nhập thành công — xong 2026-10-13. `DataFailureExplainer` báo tiếng Việt cho tệp bị khóa, hết thời gian đọc, thiếu tệp hoặc sheet (kèm danh sách sheet hiện có), tệp hỏng, sai dòng tiêu đề, ô lỗi, công thức chưa tính và CSV sai số trường: nêu tệp, sheet, ô và cách sửa. Áp dụng cho hộp thoại lệnh thất bại (tiêu đề “Chưa đọc được dữ liệu”), cửa sổ nhập và thanh trạng thái (Figma `37:269`). Dòng tiêu đề và đường dẫn liên kết lại chỉ được ghi sau khi đọc thành công; nhập lỗi giữ nguyên liên kết và dữ liệu đang dùng (H16). Gate `data failures name the file, row and column`, `failed import or relink keeps the working link`.
- [x] Fixture dữ liệu chạy qua kiểm tra payload mã vạch B01/B02; payload lỗi chặn in — xong 2026-10-13. Payload từ ô CSV/XLSX tới decode: `ImportedBarcodeRoundTripTests` (15 loại, 90 ca). Một dòng CSV có EAN-13 sai số kiểm tra chặn đúng dòng đó ở kiểm tra trước khi in, và thông báo nêu “Dòng 2 · “EAN” (mã vạch)” (gate `invalid barcode data in a file blocks printing`).
- [x] Chốt ADR CSV về dòng thiếu hoặc thừa cột — xong 2026-10-13: ADR-024. Dòng thiếu hay thừa trường so với tiêu đề đều chặn việc nhập và nêu số dòng.
- [ ] Tái hiện và xử lý treo khi nhập Excel trên máy 4 GB — *chưa làm được, 2026-10-13:* cần máy 4 GB thật. Trên máy phát triển, bản hiện tại không treo: 10.000 dòng XLSX nhập trong 0,45 s và chỉ thêm 3,5 MB. Việc nhập chạy ngoài luồng giao diện, có Hủy và tự dừng sau 45 s (tệp mạng 30 s).
- [x] Đo 1, 1.000 và 10.000 dòng; công bố giới hạn — xong 2026-10-13: bảng “Measured limits” trong [`../LOCAL_LABEL_PRODUCT_CONTRACT.md`](../LOCAL_LABEL_PRODUCT_CONTRACT.md). Nhập 10.000 dòng: XLSX 0,45 s, CSV 0,04 s. Mở Xem trước 10.000 tem: 16,6 s (đo một lần; gate thường chạy đến 1.000 dòng để bộ test không chậm).

**GĐ5 — Mã vạch, xem trước và in**

- [x] In nhanh đi chung đường với xem trước — xong 2026-10-13 (`d61d6db`): một bản chụp mẫu cho contract, kiểm tra, manifest và lệnh gửi; contract và kiểm tra chạy ngoài luồng giao diện; gate `quick print uses one snapshot and the async driver contract`.
- [x] Liệt kê máy in, đọc contract, băm Excel ngoài luồng giao diện; báo đúng lỗi spooler — xong 2026-10-13 (`d61d6db`): `PrinterQueueLoader` (luồng STA riêng, báo spooler dừng); băm Excel bằng `Task.Run` và đọc với `FileShare.ReadWrite` (hết chặn "Excel đã thay đổi" khi Excel đang mở tệp); unit `FileSourceIdentitySharingTests`.
- [x] Nhật ký job ghi nối tiếp, xoay vòng, tự cách ly dòng cuối hỏng — xong 2026-10-13: chỉ đọc lại khi tệp đổi, dòng cuối ghi dở tự cất sang `.hong-*.jsonl` (`c95a10c`); nhật ký trên 16 MB mà mọi lệnh đã xong được lưu nguyên tệp sang `.luu-*.jsonl` khi mở app, nhật ký còn lệnh cần đối soát hoặc đang chuẩn bị thì giữ nguyên; unit `PrintJobLogRepairTests` (10).
- [x] In thử chạy trên bản sao của mẫu — xong 2026-10-13 (`9169716`): gate `calibration print uses an isolated copy of the label`.
- [x] Phân loại lại điểm dừng in (ca XP-DT426B ngày 2026-09-15) — xong 2026-10-13. Bằng chứng từ `print-operations.jsonl`: 4 lần dừng lúc 15:12–15:21 (3 lần "output contract is missing verified driver-ticket evidence", 1 lần "changed immediately before dispatch (effective-ticket, prepared-expectation)") do đồng hồ ticket Seagull, đã sửa ở v0.288; sau đó cùng hai mẫu in 4 lần trên XP-DT426B (15:42–19:26, spool accepted, ticket verified). Các điểm dừng còn lại đều là thay đổi thiết lập thật nên vẫn chặn (ADR-021); thông báo nay là tiếng Việt.
- [x] Lệnh in thành công không còn trong "Xem lại"; cảnh báo in trùng hoạt động `[chờ QĐ-10]` — xong 2026-10-13 (`c95a10c`, ADR-023): trên bản sao log thật, 0/10 lệnh bị liệt kê (trước là 6/6 lệnh in thành công); dòng đã gửi được đánh dấu để cảnh báo in trùng.
- [x] P07 Unknown sau khi gửi; P08 chặn khi lỗi lưu bằng chứng; P04 thứ tự record/copy — xong 2026-10-13: P07 `d61d6db`, gate `failure after spool acceptance is unknown, not failed`; P08 cả hai đường in dùng `PrintJobEvidenceAppend`, unit `RequiredPreDispatchAppendFailureBlocksSend`; P04 unit `TryExpand_KeepsRecordAndCopyIdentityInPrintOrder` và `TwoThenOneCopyOrdinalsChangeTheManifestAndAMissingSequenceStaysV3`. P05: `DispatchRevalidationContractTests`; P06: gate `print preview busy gate blocks duplicate dispatch`, `async relay command rejects re-entry until the first call completes`.
- [x] Log in ghi mẫu; thông báo lỗi in tiếng Việt kèm việc cần làm — xong 2026-10-13: log đã có danh tính mẫu (xem đính chính H24); `PrintFailureExplainer` và `PreflightIssueText` nêu vấn đề, dòng dữ liệu/đối tượng và việc cần làm, chi tiết kỹ thuật để riêng; gate `print stops are explained in plain Vietnamese`, `preflight issues name the data row in Vietnamese`. Bố cục dùng hộp thoại F03 (`36:188`); nội dung chữ ở handoff §3.
- [ ] Tên driver khác máy: ca tái hiện và ADR `[chờ QĐ-12]` — chưa có ca tái hiện (cần máy thứ hai); giữ hành vi chặn hiện tại theo QĐ-12.
- [x] Matrix B01–B07 cho các loại mã được giữ — xong 2026-10-13: bảng theo từng loại trong `CORE_STANDARD_EVIDENCE_AUDIT.md`, gồm fixture dương/âm, round-trip CSV/XLSX × 203/300/600 DPI và bộ đọc dùng. Mức phần mềm có đủ; mức vật lý vẫn Not verified vì cùng thư viện ZXing (Plessey dùng bộ đọc trong test) và chưa quét tem thật.

**GĐ6 — Nghiệm thu thật**

- [ ] Luồng chính trên bộ mẫu thật với XP-DT426B — *mức phần mềm xong, 2026-10-13* (gate `real templates open, save, render and pass preflight`). Cả 5 mẫu trong `form tem nhãn/anlabel` mở và lưu không đổi byte, vẽ ra tem có nội dung. Với contract driver XP-DT426B (chỉ đọc, không gửi lệnh in), hai mẫu 75×100 qua kiểm tra và ticket được xác minh. Mẫu 110×150 bị driver báo khổ 75×100: đây là cuộn tem đang lắp, cần đổi khổ trên máy in, không phải lỗi app. **Cần chủ sản phẩm quyết định:** hai mẫu 100×70 (“tem cqs”, “tem trong data”) bị chặn in vì khung chữ 14 pt của dòng cuối dư 0,18 mm qua mép dưới tem, nằm trong khoảng trống dưới nét chữ. Có hai cách: dời hai dòng đó lên ≥0,2 mm trong mẫu, hoặc đổi luật kiểm tra theo nét chữ thật. Cách sau đụng hợp đồng Text được bảo vệ. Còn lại: in vật lý, cần cho phép.
- [ ] Kịch bản lỗi máy in (hết giấy, offline, tạm dừng, restart spooler, mất điện khi ghi nhật ký, kết quả chưa rõ) — *mức phần mềm:* trạng thái hàng đợi ánh xạ thành điểm dừng (`PrinterQueueHealthContractTests`) và được giải thích bằng tiếng Việt (gate `print stops are explained in plain Vietnamese`). Dòng cuối nhật ký ghi dở được tự cất (`PrintJobLogRepairTests`). Kết quả chưa rõ thành Unknown (gate `failure after spool acceptance is unknown, not failed`). Chưa thử trên máy in thật vì phải tạm dừng hoặc rút máy in, cần người vận hành cho phép.
- [x] Ghi bằng chứng ba mức vào `docs/CORE_STANDARD_EVIDENCE_AUDIT.md` — xong 2026-10-13 (phần mềm / tương tác / vật lý cho từng ID). Mức vật lý ghi rõ là Not verified.

**Kiểm tra toàn ứng dụng sau kế hoạch 13 (2026-10-13, phát hành v0.309)**

- [x] Năm hướng kiểm tra (đường in, bảo toàn dữ liệu, bất đồng bộ/luồng, độ bền ghi tệp, ngôn ngữ giao diện); mỗi phát hiện được đối chiếu với mã trước khi sửa, kèm gate.
  - In: `bc38442` (Xem trước so với lần nhập; lỗi khi gửi là Unknown), `2550b66` (chặn tem placeholder, một khóa in nhanh, không in thiết lập chưa thấy), `a163d72` (danh sách máy in có giới hạn chờ; lịch sử in ghi bản đã in, dựng ngoài luồng giao diện), `1bb8ec9` (gỡ lối in cũ ngoài đường có theo dõi).
  - Dữ liệu và tệp: `43f4f5e`, `f07458b`, `53a7a67`, `54a50c3` (ghi nguyên tử, append chịu cửa sổ khác, bản lưu trữ vừa tạo luôn được giữ, bản cũ không xóa được không làm hỏng việc lưu).
  - Ngôn ngữ: `53cdd4a`, `1ec8827`, `7ac6251`, `9b35bab`, `6729d86`, `97df01b`; gate `printer, print, template and data failures read in Vietnamese` neo từng quy tắc vào thông điệp lõi.
  - Kho mã: `2aacfdb` (ngừng theo dõi dữ liệu khách hàng và ảnh bên thứ ba; `.gitattributes`; bộ lọc tên khách hàng chỉ lưu mã băm).
  - Kiểm tra cuối: build 0/0; unit 1188/1188; regression 252 PASS / 0 FAIL; Fast quality loop đạt; bản cài v0.309 qua UIA smoke 15/15.
  - Đối chiếu lại năm báo cáo với các bản sửa (v0.310): `6935733` (bản phục hồi hỏi trước khi ghi đè bản lưu sau sự cố; trường mới ở chuyển đổi dữ liệu và nguồn dùng chung; giữ báo cáo lỗi đầu tiên; báo khi mất bằng chứng sau khi gửi), `37429c9` (kiểm tra máy in đã lưu có giới hạn chờ; đóng hoặc cập nhật khi đang in nhanh thì hỏi/chờ), `7df69bc` (tên tệp mẫu thư viện không còn tên khách hàng; ngừng theo dõi PDF NiceLabel). Giữ có chủ đích: Hủy trong Xem trước vẫn chờ trình điều khiển. Kiểm tra: unit 1189/1189; regression 256 PASS / 0 FAIL; UIA smoke 15/15 trên bản cài v0.310.
**Sau v0.310 — hai ID còn “Một phần” ở mức phần mềm (2026-10-14)**

Chỉ số §6 yêu cầu mọi ID đạt mức phần mềm. Bảng bằng chứng còn hai ID chưa đạt: B06 và DB03.

- [x] B06 Designer, xem trước và in vẽ cùng một mã vạch — xong 2026-10-14. Đo trước khi sửa (203 DPI): designer vẽ ở `QrDpi` rồi kéo giãn ảnh vào khung, nên QR khung 16 mm hiện 13,8 mm trong khi tem có 12,5 mm, và Code 128 khung 45 mm hiện 33,9 mm trong khi tem có 25,0 mm. Nay designer vẽ ở DPI in, mã 2D đúng kích thước theo module nguyên và đặt giữa khung, mã 1D theo X giữ chiều rộng sản xuất từ mép trái (ADR-025). Bản in và Xem trước không đổi. Gate `designer, preview and print draw the same barcode`: QR, Data Matrix, PDF417, Code 128 theo khung và theo X × 203/300 DPI; designer lệch bản in ≤ 0,17 mm, cả ba đầu ra decode đúng, trang Xem trước trùng từng byte với bản vẽ của paginator. Khi bỏ bản sửa, 12/16 ca đỏ. Kiểm tra: build 0/0; unit 1189/1189; regression 257 PASS / 0 FAIL, gồm bảy gate Text/TextBox. Không có bố cục hay chữ mới nên không cần node Figma.
- [ ] DB03 Báo vị trí khi tiêu đề trống hoặc trùng, và khi cột đang dùng bị đổi tên hay xóa giữa hai lần nhập; liên kết không được trượt sang cột khác theo vị trí mới. Tiêu đề trống đang được đặt tên theo vị trí (`Column3`), tiêu đề trùng theo thứ tự (`Part No_2`), nên thêm hoặc bớt một cột trong tệp có thể đổi cột mà liên kết trỏ tới.

- [ ] Còn chờ chủ sản phẩm: banner và tùy chọn tiếng Anh của Ô chữ trong Bảng thuộc tính (hợp đồng Text/TextBox); viết lại lịch sử git để xóa dữ liệu khách hàng đã lên `origin`; xử lý 3 worktree cũ trong `.claude/worktrees/`; hai mẫu 100×70; in vật lý; thẻ Figma cho các chữ mới (handoff §3b).
