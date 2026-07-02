# Plan: Siết độ tin cậy phần In & Preview

**Ngày:** 2026-07-02 · **Trạng thái:** Đề xuất, chờ duyệt · Theo định hướng chủ dự án: "phần in và preview cũng siết kỹ đảm bảo tin cậy"

Nguyên tắc xuyên suốt (đúng rule 4-6 `agent.md`): sản phẩm phục vụ máy in tem nhãn công nghiệp (Zebra, TSC, Godex, SATO...) — mọi quyết định phải theo hành vi thật của driver các dòng máy này, không theo máy in văn phòng.

## 1. Hiện trạng (đã có nền tốt)

- Pipeline vector riêng: `LabelVisualRenderer.Render(template, row, plan)` (`src/ANLAbel.Printing/RenderPipeline/`) dùng chung `MmConverter` + `TextBoxOverflowDetector` với designer canvas → cùng một nguồn đo đạc.
- `PrintRenderPlan` mang offset/DPI/media/gap/feed/rotate — có in **calibration page** để cân chỉnh.
- `PrintPreviewWindow` đã có: preflight chặn in (`preflight.ToUserMessage()` → "Print blocked"), chọn dòng bằng checkbox, ghi print history (`WritePrintHistoryAsync`), bắt lỗi hiển thị MessageBox.
- In chạy nền (`Task.Run` cho print job — theo MASTER_PLAN mục 7).

## 2. Rủi ro / lỗ hổng tin cậy cần siết

| # | Rủi ro | Vì sao đáng lo |
|---|--------|----------------|
| R1 | **WYSIWYG không được kiểm chứng tự động**: designer canvas, preview và bản in dùng chung bộ đo nhưng không có test khẳng định "cùng template + cùng row → cùng hình học" giữa 3 đường render | Sai lệch chỉ bị phát hiện khi khách in thật — tốn tem, mất niềm tin |
| R2 | **Dữ liệu lúc in ≠ dữ liệu đang xem**: nếu Excel đổi sau lần import cuối, preview/in dùng bản cũ trong RAM mà không cảnh báo | In sai hàng loạt do dữ liệu cũ |
| R3 | **Thiếu cột / binding hỏng lúc in**: preflight hiện chặn một số lỗi, nhưng cần bảo đảm *mọi* object bind cột thiếu đều bị chặn hoặc cảnh báo rõ trước khi ra máy in (nối với TC1 của `database-plan.md`) | Tem in ra placeholder/rỗng |
| R4 | **Khổ giấy/driver**: driver Seagull/BarTender và driver hãng trả paper size khác nhau; nếu plan lấy sai khổ → tem lệch/cắt (rule 5) | Lệch tem là lỗi khách thấy ngay |
| R5 | **Barcode/QR chất lượng in**: render bitmap theo DPI object (`QrDpi`) — nếu DPI object ≠ DPI máy in thật, module bị scale không nguyên pixel → mã khó quét | Mã không quét được = lỗi nghiêm trọng nhất của tem công nghiệp |
| R6 | **In hàng loạt (nhiều dòng) không có resume/report**: nếu lỗi ở dòng thứ k trong N dòng, người dùng cần biết đã in đến đâu, dòng nào hỏng | In lại cả lô gây trùng tem |
| R7 | Print history ghi bằng `WritePrintHistoryAsync` nhưng nếu ghi lỗi chỉ hiện Warning — cần chuẩn hoá log để truy vết | Không truy vết được lô in sai |

## 3. Kế hoạch siết (theo thứ tự đề xuất)

### Đợt 1 — Preflight & dữ liệu tươi (gắn chặt với database-plan GĐ TC)

1. **Preflight đầy đủ trước khi in**, chạy cho *từng dòng* sẽ in: (a) mọi cột mà template cần đều có trong data; (b) mọi barcode/QR render được với data của dòng đó (đã có `ValidateData`/`RenderBarcode` — gom về preflight thay vì lỗi giữa chừng); (c) object nào tràn khỏi tem → cảnh báo kèm tên object + dòng. Kết quả preflight hiển thị dạng danh sách, cho phép "bỏ qua dòng lỗi, in các dòng còn lại" hoặc "huỷ".
2. **Kiểm tra dữ liệu tươi trước khi in**: so `LastWriteTimeUtc` của file Excel với lúc import; nếu file đã đổi → hỏi "Dữ liệu Excel đã thay đổi sau lần cập nhật cuối. Cập nhật lại trước khi in?" (Update / In với dữ liệu đang xem / Huỷ).
3. **Chuẩn hoá print log**: mỗi lệnh in ghi 1 dòng JSON vào `%LocalAppData%\ANLAbel\logs\print-history`: template, máy in, khổ, DPI, số dòng chọn/in thành công/lỗi, hash dữ liệu từng dòng (ngắn), thời gian. Lỗi ghi log không được chặn việc in (giữ hành vi Warning hiện tại) nhưng phải retry 1 lần.

### Đợt 2 — WYSIWYG kiểm chứng được

4. **Test "3 đường render cùng hình học"**: với 2-3 template chuẩn (text bind + barcode + QR + line/rect, có rotation), render offscreen qua (a) designer canvas logic, (b) `LabelVisualRenderer` preview, (c) `LabelVisualRenderer` print plan → so bounds từng object (tolerance ≤ 0.1mm). Chạy trong `ANLAbel.Tests`.
5. ✅ **Test round-trip đơn vị đo** (v0.063): `MmConverterRoundTripTests` khóa mm→DIP→mm và mm→printer dots→mm với DPI 203/300/600 trên các kích thước tem đại diện 0,5–150mm; sai số tích luỹ ≤ 0,05mm. `MmToPrinterDots`/`PrinterDotsToMm` giờ fail-fast khi DPI ≤ 0 thay vì trả kết quả vô nghĩa.
6. **Preview phải dùng đúng `PrintRenderPlan` sẽ in** (offset, rotate 180, margin driver): thêm chỉ báo trên preview "Plan: 203 DPI · offset 1.0/0.5mm · rotated" để người dùng thấy preview đang mô phỏng đúng cấu hình in.

### Đợt 3 — Driver & chất lượng barcode

7. **Ma trận khổ giấy theo driver** (rule 5): kiểm tra thứ tự lấy khổ: DEVMODE/driver preferences → `PrinterSettings.PaperSizes` → nhập tay; log lại nguồn nào được dùng; cảnh báo nếu khổ driver ≠ khổ template quá tolerance (ví dụ > 1mm).
8. **Khớp DPI barcode với DPI máy in**: khi in, nếu `QrDpi` của object ≠ DPI của plan → tự render lại barcode ở DPI plan (không scale bitmap); cảnh báo trong preflight nếu module size sau quy đổi < 2 dot (ngưỡng quét được thực tế với máy 203dpi).
9. **Checklist in thử thực tế** (thủ công, mỗi release): in calibration + 1 template chuẩn trên ít nhất 1 máy Zebra/TSC thật hoặc driver ảo Seagull; quét thử barcode/QR bằng điện thoại + máy quét công nghiệp nếu có.

### Đợt 4 — In hàng loạt tin cậy

10. **Báo cáo sau lô in**: dialog tổng kết "Đã in N/M dòng; lỗi ở dòng: ...; đã ghi log tại ...". Cho phép in lại riêng các dòng lỗi.
11. **Chống trùng tem**: option đánh dấu các dòng đã in (cột trạng thái trong DataGrid + lưu vào print log), cảnh báo khi in lại dòng đã in.

## 4. Tiêu chí nghiệm thu

- Không thể bấm In khi có binding thiếu cột mà không thấy cảnh báo liệt kê rõ object/dòng nào.
- Sửa file Excel bên ngoài rồi bấm In → luôn có câu hỏi xác nhận dữ liệu cũ/mới.
- Test WYSIWYG 3 đường render + round-trip DPI chạy trong CI/`ANLAbel.Tests`, PASS ở 203/300/600 DPI.
- Mỗi lệnh in đều truy vết được qua log: ai in template nào, dữ liệu nào, máy nào, kết quả gì.
- Mỗi đợt: build PASS, test PASS, bump version hiển thị (rule 1-3 `agent.md`), cập nhật `MASTER_PLAN.md`.
