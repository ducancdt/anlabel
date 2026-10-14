# Architecture Decision Log

Trạng thái: `Proposed`, `Accepted`, `Superseded`, `Rejected`.

## ADR-026 — Chỉ in từ cột có tiêu đề riêng

- Status: Accepted (2026-10-14, kế hoạch 13, đóng DB03 ở mức phần mềm).
- Context:
  - Tiêu đề trống được đặt tên theo vị trí (`Column3`), tiêu đề trùng theo thứ tự (`Part No`, `Part No_2`), và không có thông báo nào.
  - Một liên kết tới tên như vậy chỉ nói cột nằm ở đâu. Thêm, bớt hoặc đảo cột trong tệp có thể làm tem in một cột khác mà không có gì thay đổi trên màn hình. Đảo hai cột cùng tiêu đề thì không thể phát hiện từ dòng tiêu đề.
  - Cột bị đổi tên hay xóa đã chặn in qua kiểm tra “thiếu cột”, nhưng không nói cột đó trước nằm ở đâu.
- Decision:
  - Mỗi cột nhập vào mang vị trí và tiêu đề gốc (`DataColumnOrigin`).
  - Đối tượng hoặc cột số bản đọc một cột có tiêu đề trống hay trùng (trực tiếp hoặc qua trường chuyển đổi) bị chặn in ở Xem trước và In nhanh. Cách sửa: đặt tiêu đề riêng cho cột trong tệp rồi Cập nhật Excel.
  - Bổ sung sau review 2026-10-14: một tên khớp ngang nhau với nhiều tiêu đề gần giống (ví dụ `PartNo` với “Part No” và “Part-No”) cũng chỉ nhận ra được theo thứ tự cột, nên cũng bị chặn, và ANLAbel không tự viết lại liên kết sang cột đầu tiên. Trường được tìm đúng như khi in: công thức được chạy thử (kể cả dấu cách trước ngoặc và tên ghép bằng CONCAT), tên tra trên cả cột lẫn trường chuyển đổi, cột số bản chấp nhận dạng có ngoặc nhọn. In nhanh kiểm tra đúng bảng dữ liệu đã dùng để dựng các dòng in.
  - Sau mỗi lần nhập, dòng trạng thái nêu bằng chữ cái cột: cột đang dùng không còn ở dòng tiêu đề (kèm cột của lần nhập trước), cột đang dùng có tiêu đề trống/trùng, hoặc chỉ nơi tiêu đề trống/trùng khi không dùng tới.
- Không đổi: liên kết vẫn tìm cột theo tên như trước; cột có tiêu đề riêng không bị ảnh hưởng; tiêu đề trống/trùng không được dùng tới chỉ được báo, không chặn.
- Alternatives:
  - Chỉ báo, không chặn: không ngăn được tem in sai cột khi tệp đổi thứ tự cột.
  - Lưu bố cục cột lần trước rồi chỉ chặn khi bố cục đổi: không phát hiện được việc đảo hai cột cùng tiêu đề.
- Consequences: mẫu đang liên kết tới cột như vậy phải sửa tiêu đề trong tệp trước khi in. Khi quyết định, không mẫu thật hay mẫu thư viện nào có liên kết như vậy.
- Evidence required: gate `columns known only by position are reported and cannot print`; unit `LoadSheet_RecordsWhereEachColumnCameFrom`, `LoadCsv_RecordsWhereEachColumnCameFrom`, `ColumnLetters_FollowTheSpreadsheet`; neo ngôn ngữ trong `printer, print, template and data failures read in Vietnamese`.

## ADR-025 — Designer vẽ mã vạch đúng như bản in

- Status: Accepted (2026-10-14, kế hoạch 13, đóng B06 ở mức phần mềm). Thay phần “Designer vẫn render barcode theo `item.QrDpi`” của quy tắc 12 trong `agent.md`; quyết định 2026-07-03 cho Xem trước/In (DPI của `PrintRenderPlan`) giữ nguyên.
- Context:
  - Xem trước và In vẽ mã ở DPI in và chia module theo số chấm nguyên: mã 2D có kích thước khớp nguyên module, đặt giữa khung; mã 1D có số chấm nguyên mỗi module, phần dư thành lề hai bên.
  - Designer vẽ ở `QrDpi` của đối tượng (mặc định 300) rồi kéo giãn ảnh vào khung. Ở máy 203 DPI, designer cho thấy mã to hơn tem in: QR khung 16 mm hiện 13,8 mm trong khi tem có 12,5 mm; Code 128 khung 45 mm hiện 33,9 mm trong khi tem có 25,0 mm.
  - Người vận hành chỉnh khung theo hình trên designer, nên cách cũ trái B06 và dễ làm sai kích thước mã trên tem.
- Decision:
  - Designer vẽ mã vạch ở DPI của kế hoạch in (máy in của mẫu → DPI của mẫu → `QrDpi` → 203), cùng thứ tự với `PrintService.CreatePlan`.
  - Mã 2D và PDF417 hiện đúng kích thước đã chia module, đặt giữa khung, không kéo giãn. Mã 1D theo X hiện đúng chiều rộng sản xuất từ mép trái, kể cả khi dòng dữ liệu làm mã rộng hơn khung.
  - `QrDpi` vẫn dùng để tính kích thước module px và tự cỡ của mã 2D như trước.
- Không đổi: bản in và Xem trước không đổi một điểm ảnh nào; dữ liệu mẫu không bị ghi lại.
- Bổ sung sau review 2026-10-14: đổi máy in hoặc DPI thì designer vẽ lại mã vạch; mã 2D cần nhiều chấm hơn khung được vẽ như bản in (mã vuông bị bóp vào khung, PDF417 tràn từ góc trên trái) và kiểm tra trước khi in chặn trường hợp này.
- Giới hạn: designer chỉ biết DPI của mẫu. Khi cài đặt máy in để trình điều khiển giữ thiết lập, hoặc trình điều khiển ép một DPI khác, bản in chạy theo DPI thực tế còn designer vẫn vẽ theo DPI của mẫu. Xem trước luôn vẽ đúng như bản in.
- Consequences: sau khi cập nhật, mã trên designer có thể nhỏ hơn trước. Đó là kích thước tem vẫn in ra từ trước tới nay.
- Evidence required: gate `designer, preview and print draw the same barcode` (QR, Data Matrix, PDF417, Code 128 theo khung và theo X, ở 203/300 DPI; 12/16 ca đỏ khi bỏ bản sửa).

## ADR-024 — Nhập CSV: mỗi dòng phải có đúng số trường của dòng tiêu đề

- Status: Accepted (2026-10-13, kế hoạch 13 GĐ4). Thay cho đề xuất trong [`../CSV_PARSER_FAILURE_POLICY_ADR.md`](../CSV_PARSER_FAILURE_POLICY_ADR.md). Đề xuất đó viết cho `CsvAutomationSourceParser`, mà parser này đã bị gỡ cùng automation thả file ở bước 1.2.
- Context:
  - Đường nhập tương tác (`ExcelDataService.LoadCsv`) báo lỗi khi một dòng có nhiều trường hơn tiêu đề, nhưng lại âm thầm thêm ô rỗng khi dòng có ít trường hơn.
  - Từ dữ liệu không phân biệt được hai trường hợp: thiếu ô cuối, hay thiếu một ô ở giữa. Nếu thiếu ô ở giữa, mọi giá trị sau đó dịch sang cột bên trái. Ví dụ `Code,Lot,Qty` với dòng `A2,3` sẽ in số lượng vào ô số lô.
- Decision:
  - Dòng có số trường khác số trường của tiêu đề (nhiều hơn hay ít hơn) sẽ chặn việc nhập. Thông báo nêu số dòng, số trường của dòng và số trường của tiêu đề, kèm cách sửa: ô trống vẫn cần dấu phân cách.
  - Dòng rỗng hoàn toàn được bỏ qua như trước.
- Alternatives:
  - Đệm ô rỗng (cách cũ): có thể in sai cột mà không ai biết.
  - Bỏ dòng lỗi rồi nhập tiếp: thiếu tem mà không có điểm dừng, trái DB10.
- Consequences:
  - Tệp CSV do Excel hoặc phần mềm ERP xuất luôn ghi đủ dấu phân cách nên không bị ảnh hưởng.
  - Tệp viết tay mà thiếu ô cuối phải sửa trước khi nhập.
- Evidence required: unit `A_csv_record_with_fewer_fields_than_the_header_is_rejected_with_its_row`, `Empty_lines_and_explicit_empty_fields_still_import`. Gate `data failures name the file, row and column`.

## ADR-023 — Chỉ lệnh in chưa rõ kết quả mới cần người vận hành đối soát

- Status: Accepted (2026-10-13, kế hoạch 13 QĐ-10). Bổ sung ADR-016 và ADR-017, không thay thế.
- Context:
  - Chỉ Completed, Failed và Cancelled là trạng thái kết thúc, và Completed cần xác minh vật lý. Vì vậy mọi lệnh in thành công (`SpoolAccepted`, hoặc `QueueObserved` với hàng đợi `Retained`) đều nằm mãi trong “Xem lại N lệnh in”.
  - Log thật của máy vận hành: 6/6 lệnh thành công nằm trong danh sách.
  - Cảnh báo in trùng ở Xem trước chỉ đánh dấu dòng khi có xác minh vật lý nên không bao giờ bật.
  - ADR-017 chỉ yêu cầu đối soát các lệnh mơ hồ; cách làm cũ đã áp dụng rộng hơn quyết định đó.
- Decision:
  - **Danh sách cần đối soát chỉ gồm:**
    - `Dispatching` (dừng giữa lúc gửi) và `Unknown`;
    - `SpoolAccepted`/`QueueObserved` khi hàng đợi báo lỗi hoặc trạng thái mơ hồ: Error, Offline, PaperOut, UserIntervention, Blocked, Paused, Deleted, NotFound, Unknown;
    - mọi lệnh có chuỗi thao tác của người vận hành (ghi nhận, yêu cầu in lại, duyệt in lại).
  - **Không liệt kê:**
    - `SpoolAccepted`/`QueueObserved` bình thường (Pending, Spooling, Printing, Completed, Retained);
    - lệnh chưa từng gửi (Created, Preparing, PreflightPassed). Bằng chứng trước khi gửi là bắt buộc, nên lệnh chưa có `Dispatching` thì chưa được gửi.
  - Dòng đã được hàng đợi nhận được đánh dấu “đã gửi”, để in lại cùng dòng trong phiên sẽ hỏi lại vì có thể tạo tem trùng.
- Không đổi:
  - Spool accepted không có nghĩa là tem đã ra; Completed vẫn cần bằng chứng thiết bị.
  - Không tự in lại; in lại vẫn là thao tác có chủ đích.
  - Máy trạng thái, bằng chứng job và manifest giữ nguyên.
- Alternatives:
  - Giữ danh sách mọi lệnh in: cảnh báo thành nhiễu và bị bỏ qua, kể cả cảnh báo thật.
  - Tự ghi nhận mọi lệnh: tạo bằng chứng giả.
- Consequences: chip trạng thái và Trung tâm in chỉ đếm lệnh chưa rõ kết quả. Lịch sử in vẫn giữ mọi lệnh.
- Evidence required:
  - Unit `NormalOrNeverSentJobsAreNotReviewCandidates`, `UncertainJobsStayReviewCandidates`, `OpenReprintRequestStaysAReviewCandidateEvenAfterANormalOutcome`, `PersistedQueueFaultStateRequiresOperatorDecision` (gồm Deleted, NotFound), `AmbiguousQueueEvidenceStillRequiresOperatorDecision`.
  - Áp dụng lên log thật: 6 lệnh `Retained` không còn bị liệt kê; 4 lệnh Failed vẫn là trạng thái kết thúc.

## ADR-022 — Cải tổ tập trung lõi: khóa nền, gọn phạm vi, ổn định trước tính năng

- Status: Accepted (2026-10-13: chủ sản phẩm chỉ thị thực thi theo phương án đề xuất)
- Date: 2026-10-13
- Context: Khảo sát ngày 2026-10-13 ([`13-ke-hoach-cai-to-loi.md`](13-ke-hoach-cai-to-loi.md) §2) cho thấy:
  - Worktree có 184 mục chưa commit. Bản cài là 0.266 trong khi nguồn là 0.306; 0.289–0.305 không có changelog.
  - Tạo mới, Mở và Đóng không hỏi lưu. Không có bắt lỗi toàn cục, không tự lưu. 8 lệnh bất đồng bộ chỉ ghi lỗi vào `Debug`.
  - Nhật ký job có thể chặn mọi lệnh in. Mọi lệnh in thành công vẫn nằm trong "Xem lại".
  - 9 cửa sổ không còn đường mở; khoảng 5.100 dòng automation/JOB chỉ test gọi tới.
  - AGY dispatcher lấy việc từ tài liệu lịch sử và ngoài phạm vi.
  - 32/32 ID của quy chuẩn lõi đang *Not verified*.

  ADR-021 đã chốt hướng "tin cậy, đơn giản", nhưng quy trình làm việc chưa đưa hướng đó vào thực tế.
- Decision:
  - `13-ke-hoach-cai-to-loi.md` là chương trình hiện hành của live plan. Checklist §9 của nó là nguồn việc duy nhất cho người, agent và dispatcher.
  - Mỗi lát công việc chỉ thuộc một trong bốn loại: sửa lỗi lõi tái hiện được, gỡ phức tạp ngoài lõi, đóng một ID bằng test/fixture, chẩn đoán vận hành.
  - Thứ tự: khóa nền → gọn phạm vi → vòng đời file → Designer và Excel/CSV → mã vạch, xem trước và in → nghiệm thu thật.
  - Các thay đổi phạm vi, version, dispatcher và chính sách cảnh báo in chỉ có hiệu lực theo từng QĐ-1…QĐ-12 sau khi chủ sản phẩm duyệt.
- Alternatives:
  - Tiếp tục sửa lỗi lẻ trên worktree chưa bảo toàn: rủi ro mất việc và không truy vết được.
  - Viết lại designer hoặc shell: rủi ro cao và trái ADR-002.
  - Chỉ ẩn tính năng ngoài lõi: vẫn tốn công bảo trì, dịch và test.
- Consequences:
  - Tạm dừng mọi tính năng mới trong thời gian chương trình chạy.
  - Một số cửa sổ và mã ngoài lõi rời bản phát hành (nếu duyệt), và khôi phục được qua git.
  - Thêm tiêu chí vòng đời file F01–F07 vào quy chuẩn lõi (nếu duyệt QĐ-9).
- Migration/rollback:
  - Mỗi lần gỡ là một commit riêng, có thể revert.
  - Không đổi schema `.anlabel`, không đổi hợp đồng Text/TextBox, không nới cổng an toàn in.
  - Mẫu cũ phải mở, lưu và xem trước như trước (gate round-trip).
  - Thay đổi chính sách runtime (QĐ-10, QĐ-12) cần ca tái hiện, lý do an toàn và regression riêng.
- Evidence required:
  - Các chỉ số ở §6 của kế hoạch.
  - Fast xanh sau mỗi lát.
  - Round-trip toàn bộ mẫu thư viện và mẫu thật.
  - Dispatcher dry-run chỉ liệt kê việc của §9.
  - Chạy test không đổi file nào trong thư mục dữ liệu thật của người dùng.

## ADR-021 — Tin cậy công nghiệp, thao tác đơn giản, tham chiếu chọn lọc

- Status: Accepted
- Date: 2026-09-15
- Context: Người dùng xác nhận độ tin cậy cao và dễ dùng luôn là mục đích;
  kế hoạch cạnh tranh và rule lịch sử có thể làm lệch sang sao chép tính năng.
- Decision: Product contract và core standard là phạm vi hiện hành. NiceLabel
  chỉ là tham chiếu theo `../REFERENCE_POLICY.md`. Ưu tiên lỗi vận hành lõi,
  dữ liệu/đầu ra đúng, phục hồi rõ ràng; không đo chất lượng bằng số tính năng
  hoặc số lần chặn in. Thiếu metadata không thiết yếu cần phân loại, không tự
  suy ra lỗi chặn; không bỏ kiểm tra an toàn hay retry kết quả chưa rõ.
- Supersedes: phần mở rộng cloud/server mặc định của ADR-001; các đề xuất
  SQLite/typed data graph/Control Center không còn là backlog mặc định.
  ADR-020 và rule 11 trong `agent.md` không còn cấm đọc capability driver:
  dùng adapter hiện có, kiểm tra nguồn gốc/đơn vị và giữ preset tách capability.
  Giữ nguyên named queue, Text/TextBox, chống in trùng và bằng chứng đầu ra.
- Alternatives: full NiceLabel parity hoặc chặn mọi khoảng trống metadata đều
  không đáp ứng mục tiêu đơn giản và tin cậy.
- Migration/rollback: chỉ cập nhật hướng dẫn và phân loại tài liệu; không sửa
  runtime, schema, template hoặc thiết lập máy in. Giữ lịch sử để truy vết.
  Bất kỳ thay đổi policy runtime tiếp theo cần evidence và regression riêng.
- Evidence required: các entrypoint cùng trỏ về phạm vi lõi; skill/rule không
  tự mở rộng tính năng; link hợp lệ; protected contract giữ nguyên. Hoàn tất
  tài liệu không có nghĩa đã nghiệm thu độ tin cậy runtime hoặc tem thật.

| ID | Trạng thái | Quyết định | Lý do chính | Điều kiện xem lại |
| --- | --- | --- | --- | --- |
| ADR-001 | Accepted | Local-first là mặc định; server/cloud là optional module | phù hợp xưởng nhỏ, offline, GPL và giảm vận hành | có nhu cầu collaboration đa site được chứng minh |
| ADR-002 | Accepted | Giữ WPF trong R0–R3, không rewrite shell trước khi tách engine | giảm rủi ro, giữ print integration và release được từng bước | compiler/presenter contract ổn định và migration study có prototype |
| ADR-003 | Accepted | Scene compiler platform-neutral là nguồn semantic chung | loại bỏ drift designer/preview/print | chỉ xem lại implementation, không bỏ nguyên tắc one pipeline |
| ADR-004 | Accepted | WPF viewport V2 ưu tiên `FrameworkElement` + `DrawingVisual`/retained visual tree | nhẹ hơn control-per-object, giữ text/print ecosystem hiện tại | benchmark chứng minh Skia/Direct2D tốt hơn và font parity đạt |
| ADR-005 | Accepted | Mọi mutation qua command transaction | undo ổn định, diff/audit được, cấm render mutation | không |
| ADR-006 | Accepted | Tách Design và Print Center | giảm lỗi thao tác và hỗ trợ persona Operator | không; chỉ có thể thay shell presentation |
| ADR-007 | Proposed | Document v2 dùng JSON envelope + schema migrations + resource manifest | tương thích Git, dễ inspect, hỗ trợ portability | prototype phải đo kích thước/load/migration |
| ADR-008 | Superseded (ADR-021; core standard: no SQL/SQLite) | Dùng SQLite cho unified local job/revision/event index; artifact lớn nằm ngoài DB theo content hash | query/recovery tốt hơn CSV/JSONL rời rạc | benchmark, dependency/license và backup strategy |
| ADR-009 | Accepted | Printer profile là versioned snapshot/ref, không phải implicit OS state | reproducibility và preflight | mapping sang driver backend có thể thay đổi |
| ADR-010 | Superseded (ADR-021: typed data graph is not a backlog item) | Typed data graph thay string binding làm model đích | validation, transform, lineage, connector reuse | compatibility adapter phải giữ template v1 |
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

### ADR-020 - Operator label stock, not office paper or driver lists

- Status: Accepted. ADR-021 superseded the ban on reading driver capability: evidence may be read through the existing adapter. The stock list itself stays the mm catalog plus manual entry and is never filled from the driver.
- Date: 2026-08-18
- Context: Industrial queues do not expose a trustworthy paper-size list (`DEVMODE` / `DC_PAPERSIZE` previously returned swapped or mistyped sizes). Mixing A4/Letter into `StandardLabelSizes`, stamping catalog picks as `DriverAutomatic`, and selecting `printers[0]` (Windows default) made khổ giấy and printer choice fail open.
- Decision: Keep the millimetre catalog + typed custom size. Treat both as `PaperSizeSource.Manual`. Reject A4/A3/Letter/Legal as thermal stock in `LabelStockContract` and preflight. Resolve a named queue only; never auto-select the default. Do not reopen driver paper enumeration.
- Alternatives: Read `PageMediaSizeCapability` / `DeviceCapabilitiesW` (rejected; rule 11). Keep A4 in the shipping list (rejected; office-tray mental model).
- Consequences: Existing templates that already store A4 physical stock fail closed at preflight until the operator picks a die size. Legacy `DriverAutomatic` values remain loadable and are not rewritten on open.
- Evidence required: `LabelStockContract` unit tests on the real public methods, `PrintService.ValidateRows` blocking office stock, catalog contains no office sheet, `ResolveNamedQueue` returns null for a blank name.

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
