# Lưu trữ tài liệu ngoài phạm vi

Chuyển vào đây ngày 2026-10-13 theo kế hoạch 13, mục 1.4 (QĐ-11, ADR-022).

Thư mục này giữ lịch sử thiết kế và quyết định của các bề mặt nằm ngoài phạm vi
lõi:

- Control Center: `CC_*` (tổng quan vận hành, hàng đợi in, thư viện và phê
  duyệt tài liệu, lịch sử/in lại, phân tích, quản trị, automation).
- Không gian dữ liệu và biến đổi dữ liệu: `R4_*`.
- Print method: `P7_*`. Physical verifier: `P8_*`.
- Nghiên cứu NiceLabel về Control Center, JOB và Figma parity.

Các tài liệu trong thư mục này **không tạo việc**. Việc chỉ lấy từ mục 9 của
[`../reinvention/13-ke-hoach-cai-to-loi.md`](../reinvention/13-ke-hoach-cai-to-loi.md).

Một số bề mặt vẫn còn trong app với vai trò hỗ trợ lõi: Trung tâm in (đối soát,
in lại có chủ đích) và Lịch sử in. Packet của chúng ở đây là bản ghi thiết kế cũ,
không phải backlog.

Mã của automation thả file, NiceLabel JOB runner và các cửa sổ Control Center đã
gỡ được giữ ở nhánh git `archive/automation-2026-10`.
