# Danh mục tính năng và ưu tiên

## 1. Cách chấm

- **Impact:** giảm lỗi vật lý, tăng tốc workflow, mở rộng sản phẩm.
- **Foundation:** feature khác có phụ thuộc không.
- **Risk reduction:** giảm nợ kiến trúc/production risk.
- **Evidence:** có use case/competitor/hardware evidence chưa.
- **Effort:** S/M/L/XL tương đối, không phải cam kết thời gian.

Priority:

- P0: bắt buộc cho ANLAbel Next đáng tin.
- P1: tạo chiều sâu cạnh tranh sau foundation.
- P2: optional/enterprise/ecosystem.
- Park: chưa làm nếu chưa có evidence.

## 2. P0 — Foundation và workflow cốt lõi

| Capability | Impact | Effort | Dependency | Lý do |
| --- | --- | --- | --- | --- |
| Document schema v2 + migration | rất cao | L | fixtures | portability/revision/extension |
| Immutable snapshots | rất cao | M | document v2 | deterministic compiler/jobs |
| Scene compiler | rất cao | XL | snapshots | one semantic pipeline |
| Command transactions + delta undo | rất cao | L | document | editor stability/diff |
| Viewport V2 | rất cao | XL | compiler/commands | performance và tách UI |
| Selection/key object + Snap Engine V2 | rất cao | L | viewport/commands/scene anchors | bắt điểm ổn định theo zoom, group/resize cùng semantics |
| Frame align + center/gap distribute | cao | M | selection/commands | thao tác thiết kế nền tảng, deterministic/undo được |
| Shared text layout + vertical/baseline align | rất cao | L | compiler/text metrics | loại drift designer/print và căn chữ đúng giữa font/cỡ khác nhau |
| Explicit text sizing/overflow policy | rất cao | M | text layout/data | chặn tem mất/tràn dữ liệu biến đổi |
| Unified preflight | rất cao | L | compiler/profile/data | fail closed |
| Unified preview/print RenderPlan | rất cao | L | compiler | WYSIWYG thật |
| Printer profile v2 | rất cao | L | print core | reproducibility/calibration |
| Device-dot quantizer + ticket reconciliation | rất cao | L | profile/RenderPlan | đúng 203/300/600 DPI và driver-coerced settings |
| Job manifest/state/event store | rất cao | XL | RenderPlan/profile | recovery/audit/idempotency |
| Print Center/operator mode | rất cao | L | job core | giảm lỗi production |
| Responsive shell/inspector | cao | L | command/property contracts | giải quyết UI debt |
| Light/dark/HC/localization/a11y base | cao | L | shell | quality baseline |
| Typed data contracts + Excel adapter | rất cao | L | snapshots | thoát Excel coupling mà giữ compatibility |
| Golden scene/render tests | rất cao | L | fixtures/compiler | proof of parity |

## 3. P1 — Chiều sâu sản phẩm

| Capability | Impact | Effort | Dependency |
| --- | --- | --- | --- |
| Layers/group hierarchy | cao | M | editor v2 |
| Oriented/optical align + smart layout precision | trung bình/cao | M | Designer Precision gates |
| Anchors/constraints | cao | L | scene layout |
| Stack/Grid/Table/RepeatRegion | rất cao | XL | layout engine |
| Components/symbols | cao | L | document/resources |
| Typed variable/transform graph | rất cao | XL | data contracts |
| Data filter/sort/query/alias | cao | L | connectors |
| Generated operator forms | cao | L | variables/Print Center |
| Serialization reservation/reconcile | rất cao | XL | job store |
| GS1 structured assistant | cao | L | barcode/data |
| Resource/font portability diagnostics | cao | M | resource manifest/compiler |
| Calibration Assistant | cao | M | profiles/hardware fixtures |
| Local revisions/publish | cao | L | repository |
| Semantic + visual diff | cao | L | revisions/compiler |
| History/reprint dashboard | cao | M | job store |
| CLI/headless validation/render | cao | M | compiler/print core |
| Autosave/crash recovery | cao | M | snapshots/repository |

## 4. P2 — Local label depth

| Capability | Value condition | Effort |
| --- | --- | --- |
| Automation DSL/host | ERP/WMS integration demand | XL |
| Local file-drop automation | repeatable CSV/JSON folder workflow | L |
| Batch manifest/recovery depth | large local production runs | L |
| Template package import/export | portable local deployment | M |
| Additional local data adapters | proven label-data need | M |
| Multi-artboard/page templates | document use case | L |
| Phrase library/multilingual layers | multilingual production need | L |
| Analytics/material consumption | structured events + business need | L |

## 5. Park — không làm chỉ vì đối thủ có

- 10.000 printer drivers tự viết;
- general-purpose scripting bên trong document;
- cloud SaaS, web/browser app, login, sync và remote control plane;
- full PowerForms clone;
- RFID/card/magnetic encoding;
- arbitrary native printer-language optimization;
- office-print-first page layout;
- per-printer licensing complexity;
- marketplace/plugin execution không có sandbox/trust model.

## 6. UX capability map

| User need | Surface đích | P0/P1 |
| --- | --- | --- |
| Tìm/tạo/mở tài liệu | Home/Library | P0 |
| Thêm/chọn/sắp xếp object | Design | P0/P1 |
| Xem layers/components | Design left rail | P1 |
| Chỉnh property | Inspector | P0 |
| Xem lỗi toàn tài liệu | Problems | P0 |
| Kết nối/test/map data | Data | P0/P1 |
| Duyệt record/sample | Data/Print | P0 |
| Chọn profile/media/calibration | Print/Admin | P0/P1 |
| In an toàn | Print Center | P0 |
| Theo dõi/reprint | Monitor | P0/P1 |
| Revision/publish | Library | P1 |
| Tạo integration | Automate | P2 |

## 7. Dependency traps cần tránh

- Không làm shell đẹp trước command/property contracts; sẽ tái tạo coupling cũ.
- Không làm forms trước typed variables và Print Center.
- Không làm automation trước immutable job snapshot/idempotency.
- Không làm cloud sync, web app, login hoặc remote gateway.
- Không làm native printer adapter trước profile/capability/preflight/hardware lab.
- Không làm visual diff trước deterministic scene compiler.
- Không làm components trước stable IDs/resource references/commands.

## 8. Product bets sáng tạo

### Explainable Label Compiler

Cho phép người dùng click “Why?” trên output/problem để thấy variable, transform, condition, layout, font và barcode decision. Đây là khác biệt mạnh so với suite legacy.

### Proof Package

Một file package chứa revision hash, generic/sample fixture, rendered proofs ở 203/300/600 DPI, preflight report, resource manifest và optional signature. QA có thể review không cần source data thật.

### Printer Evidence Passport

Mỗi profile lưu model/driver/firmware/DPI/media/calibration/test date/results. UI phân biệt Verified tại site này, Driver-reported và Unknown.

### Safe Reprint Lineage

Reprint không phải “in lại file”; nó là child job có reason, operator, selected items, quantity, target profile và link về job gốc.

### Progressive Operator Form

ANLAbel tự sinh form an toàn từ prompted variables/schema, sau đó cho tùy biến layout. Người dùng có giá trị ngay mà không cần full form designer.

### Template Health Score

Score giải thích được dựa trên portable resources, sample coverage, binding validity, profile verification, barcode module, overflow và revision state; không biến thành điểm số marketing mơ hồ.
