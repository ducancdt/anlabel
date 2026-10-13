# Kiểm toán nhất quán tài liệu (DOCS_CONSISTENCY_AUDIT)

> Ngày kiểm toán: 2026-09-14. Phạm vi: READ-ONLY. Chỉ file này được tạo mới.
> Phiên bản chuẩn (canonical): `eng/Version.props` → `<ANLAbelReleaseVersion>` = **0.287**.

## Tổng quan số liệu

- Số file `.md` đã quét (dưới `docs/` và `docs/reinvention/`): **112**
- Số liên kết cục bộ đã kiểm tra (bỏ qua `http(s)://`, `mailto:`, và neo `#`): **1077**
- Số liên kết hỏng (target không tồn tại trên đĩa): **0**
- Số điểm lệch phiên bản (so với 0.287): **0**

---

## 1. Version drift (lệch phiên bản)

Đã rà toàn bộ chuỗi phiên bản dạng `0.2xx`, `v0.2xx`, `Now (0.xxx)` và các câu khai báo "current/public version" trong các file trọng điểm được yêu cầu. **Tất cả các khai báo "phiên bản hiện hành" đều khớp giá trị chuẩn 0.287.**

| File:line | Ngữ cảnh khai báo hiện hành | Giá trị trong doc | Chuẩn (Version.props) | Trạng thái |
|---|---|---|---|---|
| docs/reinvention/07-execution-plan.md:36 | `## Now (0.287)` | 0.287 | 0.287 | OK |
| docs/reinvention/07-execution-plan.md:38 | `Public version 0.287.` | 0.287 | 0.287 | OK |
| docs/reinvention/11-verification-checkpoint-2026-08-13.md:15 | `0.287 is canonical in eng/Version.props` | 0.287 | 0.287 | OK |
| docs/reinvention/MEMORY.md:4 | `- Version: 0.287.` | 0.287 | 0.287 | OK |
| MASTER_PLAN.md:5 | `product display v0.287` | 0.287 | 0.287 | OK |
| PLAN.md:1254 | `eng/Version.props la nguon public version duy nhat` (không hard-code số) | — | 0.287 | OK |
| docs/VERSIONING.md:3 | `The current public version is 0.287.` | 0.287 | 0.287 | OK |
| docs/README.md | Không khai báo số phiên bản | — | 0.287 | OK |
| docs/architecture.md | Không khai báo số phiên bản | — | 0.287 | OK |
| docs/CORE_SOFTWARE_STANDARD.md | Không khai báo số phiên bản | — | 0.287 | OK |
| docs/CORE_STANDARD_EVIDENCE_AUDIT.md:145,151,155 | Increment mới nhất `0.287` | 0.287 | 0.287 | OK |
| docs/AUTOMATED_QUALITY_LOOP.md:8 | `Current public version 0.287` | 0.287 | 0.287 | OK |
| docs/audit-2026-07-02.md:13 | `public version 0.287 is canonical` | 0.287 | 0.287 | OK |

**Kết luận: KHÔNG có điểm lệch phiên bản (0 finding).**

Ghi chú: Các số phiên bản cũ khác xuất hiện trong tài liệu (ví dụ `0.057`–`0.283` trong `MASTER_PLAN.md`/`PLAN.md`/`CORE_STANDARD_EVIDENCE_AUDIT.md`/`PRINT_PRINTER_PAPER_FAIL_CLOSED.md`, hoặc `v0.185`/`v0.229`/`v0.267` trong các handoff) là bản ghi lịch sử của từng increment/quyết định, không phải khai báo "phiên bản hiện hành", nên không tính là lệch.

---

## 2. Broken relative links (liên kết cục bộ hỏng)

Đã kiểm tra 1077 liên kết cục bộ; mọi target đều tồn tại trên đĩa (đường dẫn tính tương đối theo file chứa liên kết). **Không có liên kết nào trỏ tới target thiếu (0 finding).**

| Linking file:line | Target | Trạng thái |
|---|---|---|
| — | — | Không có liên kết hỏng |

### Ghi chú (không tính là hỏng — cần lưu ý phong cách)

| Linking file:line | Target | Ghi chú |
|---|---|---|
| docs/NICELABEL_JOB_COMMAND_ARCHITECTURE.md:198 | `file:///H:/00_REPOS_PROJECTS/ANLABEL/PLAN.md` | Dùng URI tuyệt đối `file://` cứng theo máy. File đích **có tồn tại** (`H:/00_REPOS_PROJECTS/ANLABEL/PLAN.md`), nên không hỏng; nhưng nên đổi thành liên kết tương đối `../PLAN.md` cho khả chuyển. |
