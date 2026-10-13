# Chính sách dùng tài liệu tham chiếu

Ngày chốt: 2026-09-15. Áp dụng cho kế hoạch, agent, skill, rule và thiết kế ANLAbel.

## Nguồn quyết định

Yêu cầu hiện tại của người dùng và hợp đồng bảo vệ trong [AGENTS.md](../AGENTS.md)
định hướng công việc. [Product contract](LOCAL_LABEL_PRODUCT_CONTRACT.md),
[core standard](CORE_SOFTWARE_STANDARD.md) và [live plan](reinvention/07-execution-plan.md)
chốt phạm vi, tiêu chí nghiệm thu và thứ tự ưu tiên.

`PLAN.md` và `MASTER_PLAN.md` giữ lịch sử. Các roadmap, checkpoint, proposal và
decision packet cũ không tự tạo backlog. Quyết định sản phẩm đã được chấp nhận
phải được đọc cùng cập nhật mới nhất; không dùng một đoạn lịch sử để ghi đè
phạm vi hiện tại. Hợp đồng Text/TextBox trong AGENTS.md vẫn được bảo vệ nguyên vẹn.

## Chọn lọc từ NiceLabel

| Ý tưởng tham khảo | Phần cốt lõi ANLAbel chọn | Giới hạn |
| --- | --- | --- |
| Designer | Thiết kế mm, object rõ ownership, undo/redo, save/reopen ổn định | Không sao chép toàn bộ ribbon hay editor |
| Dữ liệu | Excel/CSV, map field, chọn dòng và copies | Không mở SQL/ODBC, ERP/MES hay scripting theo manual |
| Preview | Đúng dữ liệu, thứ tự và số lượng sẽ in; cùng scene với print | Không xây toàn bộ PowerForms |
| Máy in | Queue rõ ràng, cấu hình driver và khổ tem có nguồn gốc | Preset không là bằng chứng capability; không đoán hỗ trợ |
| Kiểm tra và phục hồi | Lỗi có cách sửa, trạng thái job trung thực, reprint có chủ đích | Không tự retry khi chưa biết kết quả; không cần full Control Center |

Tài liệu `NICELABEL_*`, `BARCODE_NICELABEL_BARTENDER_RESEARCH.md`, manual
`ug-NiceLabel_2019_PowerForms_User_Guide-en.md`, ảnh trích xuất và Figma recreation
là nguồn tham khảo. Tên “architecture”, “parity”, “target” hoặc một checklist
trong các tài liệu này không biến mọi nội dung thành yêu cầu thực thi.

Không cần đọc toàn bộ manual cho mỗi task. Tìm đúng phần liên quan khi nó giúp
giải quyết vấn đề cụ thể. Phân biệt điều nguồn nói, điều ANLAbel đã chọn và điều
runtime thực sự chứng minh được. Một hình Figma không chứng minh in đúng.

## Cách đưa ý tưởng vào kế hoạch

Trước khi đưa một ý tưởng tham khảo vào live plan, ghi ngắn gọn:

1. Người vận hành đang gặp vấn đề gì và bằng chứng hoặc yêu cầu nào xác nhận nó.
2. Hành vi ANLAbel cần đạt, phần chọn từ nguồn và phần không áp dụng.
3. Cách triển khai đơn giản qua module hiện hữu và tác động tới dữ liệu/in.
4. Cách kiểm tra kết quả; giới hạn chưa xác minh và cách phục hồi nếu thất bại.

Đây là tiêu chí quyết định, không phải mẫu hồ sơ bắt buộc cho mọi bug fix.
Tính năng ngoài lõi cần nhu cầu sản phẩm rõ ràng; không suy ra phê duyệt từ
khoảng trống trong bảng so sánh. Không xóa tính năng hoặc dữ liệu hiện hữu chỉ
vì chúng xuất phát từ nghiên cứu cũ.
