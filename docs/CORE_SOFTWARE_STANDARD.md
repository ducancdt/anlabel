# Bộ khung quy chuẩn ANLAbel

Ngày chốt: 2026-09-14. Trạng thái: quy chuẩn phát triển và nghiệm thu; không phải chứng nhận runtime đã đạt toàn bộ tiêu chí.

## 1. Phạm vi và thứ tự ưu tiên

Một ứng dụng Windows offline để thiết kế mẫu tem, liên kết Excel/CSV, kiểm tra, xem trước và in qua đúng Windows printer queue. “Database” trong sản phẩm nghĩa là quản lý dữ liệu từ Excel/CSV, không phải một hệ quản trị cơ sở dữ liệu.

Ưu tiên: đúng dữ liệu và barcode → không mất dữ liệu/không sửa thiết kế ngoài ý muốn → đúng máy in và khổ tem → thao tác đơn giản → tốc độ → tính năng mới. Designer và dữ liệu là hai hạng mục ổn định phải hoàn tất trước khi mở rộng automation.

Không SQL, SQLite, ODBC, server database hoặc connector database khác. File JSON dùng lưu mẫu, cấu hình và bằng chứng job không phải nguồn dữ liệu tem thay thế Excel/CSV. Không bổ sung cloud, tài khoản, Control Center, PowerForms, scripting, ERP/MES, sheet nhiều tem, skipped slots hoặc label sets vào phạm vi lõi. Code lịch sử ngoài phạm vi không tự tạo yêu cầu triển khai mới và không bị xóa chỉ vì quy chuẩn này.

Lấy từ nghiên cứu NiceLabel: tách thiết kế khỏi dữ liệu, map field rõ ràng, chọn dòng/copies, preview đúng instance, driver là nguồn capability, kiểm tra trước in. Không lấy toàn bộ kiến trúc hay toàn bộ tập lệnh NiceLabel.

Nguồn nội bộ: [product contract](LOCAL_LABEL_PRODUCT_CONTRACT.md), [preview](NICELABEL_PRINT_PREVIEW_ARCHITECTURE.md), [printer/media](NICELABEL_PRINTER_MEDIA_ARCHITECTURE.md), [JOB](NICELABEL_JOB_COMMAND_ARCHITECTURE.md), [designer audit](designer-stability-plan.md), [database plan](database-plan.md), [Text/TextBox](NICELABEL_TEXTBOX_RESEARCH.md). Đây là tổng hợp quyết định sản phẩm từ tài liệu trong repo, không xác nhận lại mọi phát biểu về NiceLabel trong các nghiên cứu.

Quy chuẩn này chốt phạm vi cho kế hoạch tiếp theo. Các đề xuất SQL/ODBC, full JOB và multi-label trong tài liệu lịch sử không phải backlog. [AGENTS.md](../AGENTS.md) vẫn là hợp đồng bảo vệ Text/TextBox; tài liệu này không sửa bất kỳ invariant nào của hợp đồng đó.

## 2. Kiến trúc tối thiểu, tận dụng module hiện có

### Nguyên tắc quyết định — cập nhật 2026-09-15

- Độ tin cậy công nghiệp gồm đúng dữ liệu/đầu ra, giữ thiết kế, không in trùng,
  vận hành ổn định và phục hồi dễ hiểu. Nhiều lớp kiểm tra hoặc nhiều hộp lỗi
  không tự chứng minh độ tin cậy cao.
- Luồng thường dùng phải ngắn: mở mẫu → nối/chọn dữ liệu → chọn máy/khổ tem →
  xem trước → in → xem kết quả. Cấu hình chuyên sâu và bằng chứng kỹ thuật đặt
  trong phần chi tiết; không buộc người vận hành hiểu hash, contract hay pipeline.
- Chặn khi có lỗi ảnh hưởng tính đúng đắn: dữ liệu/barcode không hợp lệ, sai
  máy/khổ tem, snapshot đã stale hoặc không đủ thông tin thiết yếu để chuẩn bị
  đúng đầu ra. Kết quả gửi lệnh chưa rõ thì đối soát, không tự gửi lại.
- Thiếu metadata không thiết yếu, driver tự chuẩn hóa cấu hình nhưng giữ đầu ra
  hợp lệ, hoặc cảnh báo chất lượng quét không tự động là lỗi chặn. Đánh giá theo
  dữ liệu driver, đầu ra và policy hiện hành; không đoán capability, không lặng
  lẽ bỏ kiểm tra hay giảm mức lỗi đã bảo vệ. Thay đổi policy runtime phải có ca
  tái hiện, lý do an toàn và regression tương ứng.
- Mỗi lỗi cho biết việc gì chưa làm được, ảnh hưởng ở đâu và cách xử lý. Gộp
  lỗi lặp theo nguyên nhân; giữ số dòng bị ảnh hưởng và đường tới chi tiết.
- Sửa lỗi đang cản công việc lõi trước việc mở rộng kiến trúc. Không tạo dự án
  audit, framework hoặc agent mới cho một thay đổi nhỏ đã đủ bằng chứng.
- NiceLabel chỉ cung cấp ý tưởng tham khảo theo [reference policy](REFERENCE_POLICY.md).
  Một tính năng thiếu so với họ không phải defect của ANLAbel. Các hợp đồng
  ANLAbel đã được người dùng chốt vẫn có hiệu lực dù tài liệu gốc là nghiên cứu.

Không thêm framework, bus sự kiện, microservice hay renderer thứ hai chỉ để đạt sơ đồ kiến trúc.

| Module hiện có | Trách nhiệm chuẩn | Ranh giới |
| --- | --- | --- |
| `ANLAbel.App` | Thao tác designer, bảng dữ liệu, preview, lệnh in | UI phát lệnh; không tự parse dữ liệu hay quyết định barcode hợp lệ |
| `ANLAbel.Core` | Model mm, binding, geometry, snapshot, quantity và policy thuần | Không gọi driver/file/UI; cùng input phải cùng kết quả |
| `ANLAbel.Data` | Đọc Excel/CSV, source identity, refresh và log cục bộ | Thay snapshot nguyên khối sau khi đọc thành công; không sửa geometry |
| `ANLAbel.Project` | Đọc/ghi `.anlabel`, schema, backup/recovery | Ghi an toàn, giữ bản tốt trước đó; schema mới không bị hạ cấp âm thầm |
| `ANLAbel.Barcode` | Validate/encode và dữ liệu module | Không tự sửa payload để làm cho encode thành công |
| `ANLAbel.Printing` | Scene chung, effective plan, preflight, preview, dispatch | Một đường `PrintService`; bitmap preview không là nguồn in |

Luồng chuẩn: document + data snapshot + lựa chọn dòng/copies + printer contract → sequence → scene/preflight → prepared job → kiểm tra lại identity → dispatch → bằng chứng kết quả.

Document lưu nội dung tác giả; data snapshot lưu giá trị của một phiên đọc; preview session giữ lựa chọn và override của phiên; prepared job bất biến giữ đúng nội dung đã kiểm tra. Close preview không được tự ghi session override vào mẫu. Chỉ lệnh lưu/apply rõ ràng mới persist thay đổi.

Không tạo một lớp/service cho mỗi danh từ trong tài liệu. Chỉ tách khi có trách nhiệm đang trộn, lỗi cần cô lập hoặc seam kiểm thử có ích; chuyển từng bước qua đường hiện tại.

## 3. Hợp đồng Designer

| ID | Bắt buộc | Nghiệm thu |
| --- | --- | --- |
| D01 | Geometry persist theo mm; zoom/DIP chỉ là biểu diễn | Zoom, pan, render lặp và đổi PreviewRow không đổi geometry đã authored |
| D02 | Một gesture/lệnh chỉnh sửa là một transaction undo | Drag/resize/rotate/group/paste → undo → redo khôi phục đúng dữ liệu, không drift |
| D03 | Commit/cancel input rõ ràng | Mouse-up resize commit qua `Thumb.DragCompleted`; mất capture sau commit không rollback; Esc/cancel theo lifecycle hiện hành |
| D04 | Text và TextBox riêng biệt | Giữ nguyên toàn bộ AGENTS.md, kể cả Text compress glyph khi kéo khung và TextBox frame-owned |
| D05 | Binding/refresh chỉ đổi nội dung hiển thị | Dòng ngắn/dài/rỗng không tự đổi X/Y/W/H, padding hoặc rotation |
| D06 | Save/load bảo toàn nội dung và tài nguyên | Round-trip giữ geometry, font, binding, barcode options; file hỏng/schema không hỗ trợ báo rõ và giữ document đang mở |
| D07 | Lỗi hiển thị không làm mất phiên chỉnh sửa | Font/image thiếu, dữ liệu lỗi, render thất bại có diagnostic; không crash hay tự thay nội dung |

Phạm vi thao tác lõi: tạo/chọn/xóa, di chuyển, resize, xoay, copy/paste, undo/redo, căn chỉnh và snap trên Text, TextBox, barcode, image, shape, line. Kiểm tra cả object đơn và nhóm, object xoay, nhãn nhỏ và nhiều object. Không dùng quy tắc tổng quát để vô hiệu hóa sizing có chủ đích đã được hợp đồng Text hoặc barcode hiện hành cho phép.

## 4. Hợp đồng Excel/CSV và binding

| ID | Bắt buộc | Ca nghiệm thu tối thiểu |
| --- | --- | --- |
| DB01 | Import có cấu hình sheet/header/encoding/delimiter rõ | Nhiều sheet, header không ở dòng 1, Unicode tiếng Việt, BOM, dấu phẩy/chấm phẩy, quoted newline và quote escape |
| DB02 | Không suy diễn làm biến dạng mã | `000123`, mã số dài, dấu cách có ý nghĩa, ngày tháng mơ hồ giữ theo policy đã hiển thị; không tự trim/ép số/đổi locale |
| DB03 | Schema không mơ hồ | Header trống/trùng, cột bị đổi tên/xóa phải báo vị trí; không bind nhầm cột theo index mới |
| DB04 | Null, chuỗi rỗng và lỗi ô có diagnostic phù hợp | Formula error/cached value thiếu, dòng rỗng và field bắt buộc thiếu không thành payload hợp lệ giả |
| DB05 | Refresh/relink atomic và có cancellation | File khóa/xóa/hỏng, đọc dở, cancel hoặc refresh A chậm hơn B: không publish dữ liệu từng phần hay kết quả cũ đè mới |
| DB06 | Snapshot giữ source identity và schema | File thay đổi sau preview phải invalidate Ready; mtime/đường dẫn giống nhau không đủ chứng minh nội dung giống |
| DB07 | Selection không phụ thuộc vị trí bảng sau sort/filter | Giữ record identity trong snapshot và source row; hai dòng cùng giá trị vẫn là hai record khác nhau |
| DB08 | Copies có giới hạn kiểm tra được | Số nguyên dương; chặn rỗng, âm, thập phân, overflow và tổng vượt giới hạn triển khai; không âm thầm cắt batch |
| DB09 | Unlink/relink không phá thiết kế | Unlink giữ binding authored để map lại; nguồn chưa xác minh không được in như dữ liệu mới |
| DB10 | Lỗi của một dòng đã chọn chặn toàn batch trước dispatch | Preflight toàn bộ record được chọn; báo field/object/source row; không chỉ kiểm tra trang preview đang nhìn |

Policy import phải ghi rõ raw value, displayed value hoặc phép biến đổi được chọn. Với ô số Excel đã mất số 0 đầu hoặc mất độ chính xác, không thể khôi phục từ phỏng đoán: báo người dùng sửa nguồn/định dạng phù hợp. Không chạy macro hoặc tự tính lại workbook bằng engine có semantics khác Excel. Giá trị công thức lưu sẵn phải được nhận diện; không đủ bằng chứng dùng cho mã bắt buộc thì chặn và yêu cầu cập nhật nguồn.

Đọc nguồn theo hướng read-only. Không tự ghi trở lại Excel/CSV khi chọn dòng hoặc in. Snapshot tốt cũ có thể giữ để người dùng xem khi refresh lỗi, nhưng trạng thái phải là stale/failed và không Ready. Lỗi phải có mã, file/sheet/row/field liên quan và cách sửa; không đưa toàn bộ dữ liệu nhạy cảm vào log.

## 5. Hợp đồng barcode 1D/2D

| ID | Bắt buộc | Bằng chứng |
| --- | --- | --- |
| B01 | Validate payload đúng symbology trước encode | Valid/invalid, ký tự không hỗ trợ, độ dài, check digit; đối chiếu payload decode |
| B02 | Không tự thêm/bỏ/đổi ký tự ngoài policy được chọn | Phân biệt raw input, encoded payload, HRI; leading zero và separator có fixture riêng |
| B03 | GS1 có semantics riêng | AI/độ dài/check digit/FNC1 và separator theo contract đã hỗ trợ; không biến chuỗi có ngoặc thành GS1 bằng phỏng đoán |
| B04 | Module theo effective DPI, quiet zone được bảo vệ | 203/300/600 DPI và non-square DPI nếu tuyên bố hỗ trợ; frame quá nhỏ phải chặn thay vì bóp tùy ý |
| B05 | Hình học không làm hỏng symbol | 1D giữ module/ratio; 2D giữ module vuông; rotation, HRI, clipping và mép printable được kiểm tra chung |
| B06 | Designer/preview/print cùng policy encode/layout | Cùng snapshot và output plan cho cùng payload/geometry; không kéo giãn bitmap preview để in |
| B07 | Chỉ công bố loại mã đã qua nghiệm thu | Danh sách hỗ trợ lấy từ runtime, lập matrix từng loại và giới hạn; loại chưa đủ evidence không được quảng bá production-ready |

Mọi loại barcode đang cung cấp trong sản phẩm phải có positive/negative fixtures, decode round-trip và kiểm tra geometry tại output DPI. Decode thành công trên ảnh số chỉ là bằng chứng phần mềm. Khả năng quét/grade trên tem thật cần máy in, media, ribbon nếu có, tốc độ/darkness, scanner hoặc verifier, payload kỳ vọng, mẫu tem và kết quả đo được lưu cùng nhau. Không dùng một ngưỡng X-dimension/grade chung cho mọi symbology và mọi ứng dụng.

## 6. Hợp đồng Print

| ID | Bắt buộc | Nghiệm thu |
| --- | --- | --- |
| P01 | Đúng queue được chọn, không fallback | Queue thiếu/đổi driver/offline/paused/unknown chặn với lý do |
| P02 | Capability thật tách khỏi stock preset | Media, DPI, orientation, imageable area có evidence; preset không chứng minh driver hỗ trợ |
| P03 | Ready thuộc một prepared snapshot | Thay document/data/quantity/profile/ticket làm stale; async result cũ không khôi phục Ready |
| P04 | Sequence có record/copy identity | A×2, B×1 → A1,A2,B1; preview và manifest/dispatch cùng thứ tự và số lượng |
| P05 | Kiểm tra lại sát thời điểm gửi | Resource/source/queue/ticket/media drift chặn trước spool, không lặng lẽ rebuild nội dung khác |
| P06 | Chống gửi trùng ở cấp ứng dụng | Double-click đồng thời không tạo hai dispatch; outcome mơ hồ sau crash/timeout không tự retry |
| P07 | Kết quả trung thực và phục hồi có căn cứ | Canceled, Failed, SpoolAccepted, Unknown và DeviceConfirmed khác nhau; spool accepted không có nghĩa tem đã ra |
| P08 | Audit hỗ trợ truy vết | Job ID, document/data/output fingerprints, record/copy mapping, thời gian, queue, outcome và diagnostic; giữ evidence trước dispatch theo contract hiện có |

Một label trên mỗi feed/page. Không override safety bằng `IGNOREERROR`, không bỏ qua dòng lỗi để tiếp tục in mà không tạo một selection mới được người dùng kiểm tra. Lỗi log phụ trợ không được làm giả thất bại rồi tự gửi lại job đã spool; lỗi lưu evidence bắt buộc trước dispatch phải chặn trước khi gửi.

Không hứa exactly-once vật lý từ Windows spooler. Khi không biết job đã được máy nhận hay chưa, chuyển sang đối soát thủ công; reprint là thao tác rõ ràng, có liên kết job cũ và chạy lại safety checks.

## 7. Cổng chất lượng và hồ sơ nghiệm thu

“Không được phép bug” được thực thi bằng chính sách không phát hành khi còn lỗi đã biết làm sai dữ liệu/barcode, mất thiết kế, in trùng ngoài ý muốn hoặc báo thành công sai. Không coi số test xanh là chứng minh phần mềm không thể có lỗi.

Mỗi ID ở trên cần hồ sơ: test/fixture cụ thể, input và expected result, commit hoặc worktree diff identity, lệnh và kết quả, thời điểm, môi trường, trạng thái `Pass / Fail / Not verified`. File test tồn tại chưa phải Pass. `Not verified` không đóng gate. Không đổi tên gate để né lỗi.

Các điểm khởi đầu có thật trong source tree, chưa được chạy lại trong lần soạn quy chuẩn:

| Nhóm | Test hiện có để mở rộng/đối chiếu | Evidence còn phải bổ sung |
| --- | --- | --- |
| Designer | `ResizeGeometryContractTests`, `GroupResizeGeometryContractTests`, `SceneCompilerTests`, regression harness | WPF gesture, focus/capture, undo/redo và round-trip trên fixture thực |
| Excel/CSV | `ExcelDataServiceReliabilityTests`, `DataConnectorContractTests`, `FileSourceIdentityStaleProtectTests` | Trace DB01–DB10 tới từng test, CSV/Excel edge cases và race/failure injection |
| Barcode | `BarcodeCheckDigitContractTests`, `DeviceBarcodeLayoutTests`, `MatrixSquareModuleFitTests`, `ThermalRasterGoldenContractTests` | Matrix từng symbology, decode và tem in thật |
| Print | `PrintPreviewContractsTests`, `DispatchRevalidationContractTests`, `PrinterMediaResolverTests`, `PrintJobStateMachineProtectTests` | WPF session integration, driver drift, crash/timeout và chống dispatch trùng |

Tên test ở bảng thuộc `src/ANLAbel.UnitTests/`; harness ở `src/ANLAbel.Tests/`. Không suy ra rằng các file này đã bao phủ hết ID tương ứng.

Cổng tự động chuẩn theo [quality loop](AUTOMATED_QUALITY_LOOP.md):

```powershell
dotnet build ANLAbel.slnx --no-restore
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Mutation
```

Mutation áp dụng khi đổi label-safety, giữ ngưỡng hiện hành 90%; không dùng mutation thay kiểm tra tích hợp hay tem thật. Các gate Text/TextBox sau phải giữ nguyên:

- `Text stays free while TextBox stays bounded`
- `Text shrink-frame compresses glyphs`
- `text box does not resize object from text content`
- `text box reflows to fit frame when user resizes`
- `normal resize capture release does not cancel gesture`
- `new text box uses compact label-aware frame`
- `designer preview row keeps object geometry`

Nghiệm thu tương tác: chuỗi tạo/sửa → bind → đổi row → save/reopen → preview → cancel/prepare lại; kiểm tra zoom 50/100/200%, Windows scaling 100/125/150%, object xoay và nhóm. Dữ liệu thử gồm 1/1.000/10.000 dòng để đo độ trễ, bộ nhớ, cancellation và tránh UI treo; các mức này là tải kiểm chứng mục tiêu, không phải tuyên bố capacity đã đạt. Phải công bố giới hạn được đo trước phát hành.

Nghiệm thu máy thật theo từng cấu hình được hỗ trợ: queue/driver/version/DPI/media, mẫu nhỏ và sát biên, barcode 1D/2D, nhiều dòng/copies, pause/offline/hết giấy, thay ticket sau preview, restart spooler và outcome chưa xác định. Không tự gửi job production để lấy evidence. Hồ sơ phải phân biệt capability observation, spool evidence và physical verification.

## 8. Thứ tự triển khai và điều kiện hoàn tất

1. **Đối chiếu hiện trạng:** map từng ID tới test/runtime; đánh dấu thiếu, không chép số pass lịch sử. Chốt fixture tổng hợp dùng cho cả designer, dữ liệu và print.
2. **Designer + dữ liệu trước:** xử lý các gap D01–D07 và DB01–DB10 theo từng lỗi tái hiện được; thêm test đúng lỗi, sửa nhỏ, không rewrite protected Text/TextBox.
3. **Barcode xuyên suốt:** chạy B01–B07 trên dữ liệu fixture và mọi symbology hiện cung cấp; lỗi payload/geometry chặn bước in. Không đợi tới cuối mới kiểm tra barcode.
4. **Khép preview → dispatch:** hoàn tất P01–P08 qua đường hiện có; tách session khỏi mutable document và giữ sequence identity khi evidence cho thấy còn thiếu.
5. **Nghiệm thu vận hành:** Fast/mutation phù hợp, WPF, fault injection và matrix máy thật; chỉ đóng ID có evidence tương ứng. UI mới theo handoff hiện hành, không mở thêm product surface.

Bộ quy chuẩn hoàn tất khi phạm vi, ownership, invariants, acceptance và thứ tự triển khai đã rõ và các tài liệu dẫn đường thống nhất. Runtime chỉ được công nhận ổn định sau một đợt audit riêng chứng minh từng ID; việc viết xong tài liệu không đóng các gate runtime hoặc vật lý.
