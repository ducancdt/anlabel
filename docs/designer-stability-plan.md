# Plan: Khắc phục object tự nhảy vị trí/kích thước trong Designer

**Ngày:** 2026-07-02 · **Trạng thái:** Đã điều tra xong nguyên nhân, chưa sửa (chờ duyệt plan)

## Triệu chứng người dùng báo

Object trên canvas thiết kế tự "nhảy lung tung" (đổi vị trí/kích thước) dù không hề rê chuột hay kéo thả.

## Nguyên nhân gốc tìm được (đọc code `src/ANLAbel.App/Controls/LabelDesignerCanvas.cs`)

Vấn đề cốt lõi: **đường render (vẽ lại canvas) đang GHI ngược vào model** (`LabelObject.WidthMm/HeightMm`). Render đáng lẽ chỉ đọc model; hiện có 3 chỗ trong `UpdateObjectElement()` sửa model ngay trong lúc vẽ:

1. **Text auto-fit** — `FitTextObjectToContent()` (dòng ~1609): mỗi lần vẽ lại, object `Text` bị đo lại theo nội dung hiển thị và bị ghi đè `WidthMm/HeightMm`. Nội dung hiển thị lấy từ `GetDisplayText()` → resolve `BindingExpression` theo `PreviewRow` (row Excel đang chọn). Hệ quả: **chỉ cần click sang row Excel khác là hàng loạt text object đổi kích thước** — không đụng chuột vẫn "nhảy".
2. **Matrix barcode ép vuông** (dòng ~354-362): mỗi lần vẽ, nếu QR/DataMatrix có `WidthMm != HeightMm` thì bị ép cả hai về `min(w,h)` rồi `return` — chờ PropertyChanged gọi vẽ lại lần nữa. Người dùng chỉnh Width trong Properties panel thì Height "tự nhảy" theo.
3. **QR auto-size theo data** — `TryApplyMatrixAutoSize()` (dòng ~1702): khi đổi PreviewRow (`OnPreviewRowChanged` gọi với `allowMatrixAutoSize: true`), QR được tính lại size theo độ dài data của row mới và ghi đè `WidthMm/HeightMm`. Đổi row → QR to/nhỏ lại.

Các yếu tố khuếch đại:

4. **Auto-fit clamp theo mép tem dùng vị trí X/Y**: `fitWidthMm = Math.Min(Template.WidthMm - item.XMm, ...)` — object gần mép phải/dưới bị bóp nhỏ lại mỗi lần re-fit; khi nội dung đổi thì nở ra lại → dao động kích thước.
5. **Rotation làm size-change biến thành position-jump**: `ApplyObjectRotation` dùng `RenderTransform` tâm (0.5, 0.5). Khi auto-fit đổi Width/Height của object đang xoay 90°/270°, tâm xoay dời đi → hình vẽ trên canvas dịch chỗ dù `XMm/YMm` không đổi. Auto-fit cũng đo text theo trục chưa xoay rồi clamp theo chiều ngang tem — sai với object xoay dọc.
6. **Vòng lặp PropertyChanged**: mutation trong lúc render kích `ObjectOnPropertyChanged` → `UpdateObjectElement` lần nữa. Hiện chỉ chặn bằng ngưỡng 0.05 mm và `_matrixAutoSizingObjects`; nếu phép fit không hội tụ (clamp qua lại giữa 2 giá trị) sẽ rung lắc liên tục.
7. **Undo/Redo bị bẩn**: history dùng snapshot template; vừa restore snapshot xong thì render auto-fit lại sửa model ngay → trạng thái sau Undo khác snapshot, cảm giác "Undo xong object vẫn nhảy".

Nguyên nhân bổ sung phía input/UX (điều tra lần 2 cùng ngày, cùng file `LabelDesignerCanvas.cs`):

8. **Snap 3mm quá mạnh và không tắt được**: `SnapThresholdMm = 3.0` (dòng ~79); khi kéo, object bị hút về 9 cặp cạnh/tâm × mọi object khác + tâm canvas (`ComputeAlignmentSnap`, dòng ~1878). Với tem nhỏ 30×15mm, 3mm là 10-20% chiều tem → kéo tinh chỉnh gần như không thể, cảm giác "giật/nhảy" khi kéo. Không có phím Alt để tạm tắt, không có setting bật/tắt.
9. **Phím mũi tên di chuyển object khi canvas còn focus**: `TryMoveSelectionWithArrowKey` (dòng ~1244): canvas `Focusable=true` và tự `Focus()` khi click; sau đó bấm mũi tên (kể cả khi người dùng tưởng đang cuộn màn hình) là object di chuyển 1mm, **Shift = 10mm** — object "tự đi" không do chuột.
10. **Không xử lý `LostMouseCapture`**: drag chỉ kết thúc trong `PreviewMouseLeftButtonUp` (dòng ~293). Nếu đang kéo mà capture bị mất (Alt+Tab, dialog/popup bật lên) thì `_dragObject`/`_dragStart` còn nguyên; lần rê chuột sau (giữ nút trái) trên element đó tiếp tục kéo theo delta tính từ `_dragStart` cũ → object **teleport** đột ngột. Guides căn chỉnh cũng không được ẩn.
11. **Clamp kéo đơn lẻ ≠ kéo nhóm**: kéo 1 object chỉ chặn `Math.Max(0, ...)` phía trái/trên (dòng ~284), kéo nhóm và kéo Line thì clamp đủ 4 phía — hành vi không nhất quán, khó đoán.

## Kế hoạch khắc phục (đề xuất, theo thứ tự)

### Bước 1 — Nguyên tắc bất biến: render không được ghi model
- Tách mọi mutation (auto-fit text, ép vuông matrix, QR auto-size) ra khỏi `UpdateObjectElement`.
- Render chỉ **đọc** `XMm/YMm/WidthMm/HeightMm` và vẽ; nếu nội dung vượt khung → cứ vẽ tràn (Text) hoặc clip (TextBox) như luật hiện có, kèm cảnh báo preflight như print pipeline đang làm.

### Bước 2 — Auto-fit text chỉ chạy tại thời điểm người dùng chủ động thay đổi
- Chạy fit khi: người dùng sửa `Text`, đổi font/size/style, đổi `BindingExpression`, hoặc tạo object mới. KHÔNG chạy khi đổi PreviewRow.
- Với text bind Excel: đo theo **sample/row hiện tại một lần lúc gán binding**, sau đó giữ nguyên khung; nếu row khác làm text dài hơn khung → hiển thị tràn + báo preflight (đúng triết lý NiceLabel: khung do người thiết kế quyết định), không tự resize.
- Bỏ clamp `Template.WidthMm - item.XMm` trong phép fit (giữ object trong tem là việc của move/preflight, không phải của phép đo chữ).

### Bước 3 — Matrix barcode: ép vuông + auto-size thành hành động có chủ đích
- Ép vuông chỉ khi người dùng vừa sửa Width hoặc Height trong Properties (điều chỉnh chiều còn lại 1 lần, có ghi history), không phải mỗi lần render.
- QR auto-size theo data chỉ chạy khi `QrSizingMode == AutoSizeByData` VÀ data nguồn thật sự đổi (so sánh chuỗi data cũ/mới), thực hiện qua một luồng riêng có suppress-reentry rõ ràng, và cũng ghi 1 bước history.

### Bước 4 — Rotation
- Khi buộc phải đổi kích thước object đang xoay (bước 2/3), bù `XMm/YMm` để **tâm hình giữ nguyên**, tránh jump thị giác.
- Phép đo fit phải tính theo trục sau xoay khi clamp trong tem (hoặc đơn giản là bỏ clamp như bước 2).

### Bước 5 — Undo/Redo và kiểm chứng
- Sau khi render hết ghi model, snapshot Undo/Redo tự sạch. Thêm test:
  - `render pass does not mutate template` — serialize template, gọi vòng update/render offscreen, serialize lại, so sánh bằng nhau.
  - `preview row change keeps object geometry` — đổi PreviewRow, assert X/Y/W/H mọi object không đổi.
  - Test hiện có về barcode/preflight phải vẫn PASS.

### Bước 5b — An toàn input & snap (bổ sung cho nguyên nhân 8-11)
- Thêm handler `LostMouseCapture` trên element và canvas: clear `_dragObject`/`_groupDragStarts`, ẩn alignment guides, kết thúc marquee. Thêm **Esc** khi đang kéo = huỷ, trả object về `_startXMm/_startYMm`.
- Giảm `SnapThresholdMm` 3.0 → ~1.0mm, hoặc tính theo màn hình (`DipToMm(6px)/Zoom` — zoom to thì ngưỡng mm nhỏ lại, tinh chỉnh chính xác hơn). Giữ **Alt** để tạm tắt snap khi kéo; thêm toggle "Snap to objects" trên ribbon (lưu preferences).
- Phím mũi tên: giữ tính năng nudge (chuẩn label designer) nhưng hiển thị vị trí mới trên status bar để người dùng hiểu vì sao object di chuyển, và đảm bảo có undo step.
- Thống nhất clamp kéo đơn lẻ giống kéo nhóm (đề xuất: clamp đủ 4 phía vì object ra ngoài tem không in được).

### Bước 6 — Chạy thử thực tế
- Build, chạy app, mở template 06/12 (nhiều text bind + QR), click qua lại các row Excel, xoay object 90° rồi đổi row — xác nhận không còn nhảy. Deploy bằng `deploy-desktop.ps1` cho bản cài.

## Mức độ rủi ro

Thay đổi hành vi auto-fit là thay đổi UX có chủ đích (text sẽ không còn tự co giãn theo row Excel). Cần xác nhận với người dùng trước khi làm Bước 2 — nếu vẫn muốn auto-fit theo row, phương án thay thế là fit **chỉ phần hiển thị** (visual) mà không ghi vào model, giữ `WidthMm/HeightMm` trong file `.anlabel` cố định.
