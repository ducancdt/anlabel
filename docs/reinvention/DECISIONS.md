# Architecture Decision Log

Trạng thái: `Proposed`, `Accepted`, `Superseded`, `Rejected`.

| ID | Trạng thái | Quyết định | Lý do chính | Điều kiện xem lại |
| --- | --- | --- | --- | --- |
| ADR-001 | Accepted | Local-first là mặc định; server/cloud là optional module | phù hợp xưởng nhỏ, offline, GPL và giảm vận hành | có nhu cầu collaboration đa site được chứng minh |
| ADR-002 | Accepted | Giữ WPF trong R0–R3, không rewrite shell trước khi tách engine | giảm rủi ro, giữ print integration và release được từng bước | compiler/presenter contract ổn định và migration study có prototype |
| ADR-003 | Accepted | Scene compiler platform-neutral là nguồn semantic chung | loại bỏ drift designer/preview/print | chỉ xem lại implementation, không bỏ nguyên tắc one pipeline |
| ADR-004 | Accepted | WPF viewport V2 ưu tiên `FrameworkElement` + `DrawingVisual`/retained visual tree | nhẹ hơn control-per-object, giữ text/print ecosystem hiện tại | benchmark chứng minh Skia/Direct2D tốt hơn và font parity đạt |
| ADR-005 | Accepted | Mọi mutation qua command transaction | undo ổn định, diff/audit được, cấm render mutation | không |
| ADR-006 | Accepted | Tách Design và Print Center | giảm lỗi thao tác và hỗ trợ persona Operator | không; chỉ có thể thay shell presentation |
| ADR-007 | Proposed | Document v2 dùng JSON envelope + schema migrations + resource manifest | tương thích Git, dễ inspect, hỗ trợ portability | prototype phải đo kích thước/load/migration |
| ADR-008 | Proposed | Dùng SQLite cho unified local job/revision/event index; artifact lớn nằm ngoài DB theo content hash | query/recovery tốt hơn CSV/JSONL rời rạc | benchmark, dependency/license và backup strategy |
| ADR-009 | Accepted | Printer profile là versioned snapshot/ref, không phải implicit OS state | reproducibility và preflight | mapping sang driver backend có thể thay đổi |
| ADR-010 | Accepted | Typed data graph thay string binding làm model đích | validation, transform, lineage, connector reuse | compatibility adapter phải giữ template v1 |
| ADR-011 | Proposed | Semantic diff + visual diff đều cần cho revision review | hình giống nhưng data rule khác và ngược lại đều nguy hiểm | chọn engine diff sau prototype |
| ADR-012 | Accepted | Automation có job state machine và idempotency contract | tránh retry in trùng | không |
| ADR-013 | Accepted | Snap V2 dùng immutable scene anchors, screen-space acquire/release hysteresis và stable semantic ranking | cảm giác bắt điểm ổn định theo zoom, không phụ thuộc thứ tự collection | usability/pen/touch evidence có thể đổi threshold, không bỏ hysteresis/determinism |
| ADR-014 | Accepted | Selection tách selected/primary/key; align/distribute là semantic transaction; text tách layout/ink/baseline, persisted `LineHeightPt` (`0 = Auto`, dương = minimum line box) và sizing/overflow explicit | căn hàng/baseline/line spacing đúng, undo được, không render mutation; preview/print/preflight dùng cùng metrics | text shaping implementation có thể đổi sau parity prototype |
| ADR-015 | Accepted | Requested ticket phải merge/validate thành EffectiveOutputContract; DeviceRenderPlan quantize physical scene theo effective dpiX/dpiY và capabilities/imageable area | WPF DIP rounding và DPI catalog gần đúng không bảo đảm geometry; driver có thể coerce media/resolution | backend/native provider có thể thay, invariant effective-contract/device-plan/dispatch hash parity giữ nguyên |
| ADR-016 | Accepted | Missing requested queue fail closed; spool submit không được gọi là physical completion; ambiguous dispatch không auto retry | tránh in nhầm printer/stock, báo success sai và in trùng | chỉ nâng mức certainty khi backend/model có verified device feedback |

### ADR-017 - Effective job evidence and hardware boundary

- Status: Accepted
- Date: 2026-08-10
- Context: A WPF `PrintDocument` return, a queue-completed status and a scanned/verified label are different observations. Driver coercion, paper-out, hot-unplug and physical partial output can occur after dispatch.
- Decision: Persist an immutable manifest/output-contract fingerprint before dispatch; expose evidence levels (`SpoolAccepted`, queue/device acknowledgement, `Completed` only with explicit device evidence, `Unknown`); require operator reconciliation for ambiguous jobs; never auto-retry or claim physical completion from spooler acknowledgement.
- Consequences: UI and logs are deliberately more conservative. Hardware adapters may raise the certainty level only when they provide a correlated device observation; physical printer evidence remains a separate gate.
- Evidence required: queue identity, spool/job identity when available, requested/effective ticket hashes, media/DPI/imageable area, lifecycle events, operator decision and verifier/device evidence.

### ADR-018 - Industrial profile and dot/raster identity

- Status: Accepted
- Date: 2026-08-10
- Context: Industrial devices expose exact and sometimes non-square DPI, stock/sensor/ribbon modes and driver-managed monochrome transforms. Rounding WPF DIP or treating a printer family as one profile can change barcode modules and registration.
- Decision: Keep device-dot quantization and vector barcode layout in a platform-neutral seam; bind output/calibration to printer, driver/firmware, stock/media, DPI X/Y, orientation and raster mode; invalidate the contract on any change. Do not issue vendor commands or claim model support without an allowlist and physical evidence.
- Consequences: A driver with incomplete capabilities is `Unverified` rather than guessed. Barcode/profile/preflight diagnostics become more explicit and may block a job until the operator chooses a compliant profile.
- Evidence required: 203/300/305/600/609 and non-square fixtures, ticket/capabilities/imageable-area capture, X-dimension/quiet-zone/HRI measurements, thermal raster mode and ISO verifier/scanner results.

### ADR-019 - Precision editor coordinate and metric boundary

- Status: Accepted
- Date: 2026-08-10
- Context: Selection, snapping, frame alignment, baseline alignment and visible-ink alignment answer different design questions; implicit optical behavior makes content changes move production geometry.
- Decision: Persist document geometry in millimetres, score interaction in view space, keep selection/key IDs outside the visual tree, and expose layout-bounds/frame, baseline and optical-ink operations as separate commands. Optical alignment is opt-in and WPF glyph geometry is an adapter until a platform-neutral metric hash exists.
- Consequences: Guides/captions remain transient unless explicitly authored; preview/print cannot silently mutate authoring geometry; new metric backends must pass parity fixtures before replacing the adapter.
- Evidence required: zoom/scale/hysteresis, group/resize/rotation, mixed-font/RTL/diacritic/multiline text and one-gesture undo/redo fixtures.

## Mẫu ADR mới

```markdown
### ADR-XXX — Tên quyết định

- Status:
- Date:
- Context:
- Decision:
- Alternatives:
- Consequences:
- Migration/rollback:
- Evidence required:
```

## ADR cần quyết định trong R1

- JSON envelope chi tiết và policy embed/link resource.
- Cách biểu diễn immutable snapshot: records thuần, source-generated serializer hoặc builder/freeze.
- SQLite dependency và backup/retention.
- Text shaping abstraction để designer/print parity và RTL.
- Spatial index: linear scan có threshold trước hay R-tree ngay từ đầu.
