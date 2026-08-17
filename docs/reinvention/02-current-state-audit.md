# Audit ANLAbel hiện tại

Ngày audit: **2026-08-09**  
Phạm vi: source, XAML, models, data, barcode, printing, persistence, tests, release scripts và tài liệu. Audit này là read-only đối với production code; worktree hiện có thay đổi UI/icon của người dùng và phải được bảo toàn.

## 1. Kết luận

ANLAbel không còn là prototype Phase 1. Nền hiện tại đã có nhiều phần khó và đúng hướng:

- geometry theo millimeter;
- object model và template JSON;
- Excel streaming, binding/formula, stale detection;
- barcode 1D/2D, QR sizing, print-DPI preflight;
- renderer in tách khỏi screen preview;
- print history/operation log;
- generic embedded template library;
- regression/unit tests đáng kể;
- packaging/release thực tế.

Nhưng quy mô đã vượt cấu trúc ban đầu. Ba hotspot đang gánh quá nhiều trách nhiệm:

| File | Dòng | Trách nhiệm bị gom |
| --- | ---: | --- |
| `src/ANLAbel.App/ViewModels/MainViewModel.cs` | 3.359 | document, commands, undo, Excel, data sources, bindings, print, dialogs state, watcher |
| `src/ANLAbel.App/Controls/LabelDesignerCanvas.cs` | 2.356 | renderer, control creation, gestures, hit test, selection, copy, snap, QR/text auto-size |
| `src/ANLAbel.App/MainWindow.xaml` | 1.250 | shell, ribbon, toolbox, tree, data, inspector, properties, dialogs state |

`PrintPreviewWindow.xaml.cs` cũng đã 1.060 dòng; `HelpWindow.xaml.cs` 922 dòng. Thêm tính năng trực tiếp vào các file này sẽ tăng lỗi liên vùng.

## 2. Module inventory

| Module | Có gì | Đánh giá |
| --- | --- | --- |
| `ANLAbel.Core` | mutable models, enums, geometry, formula/binding, QR sizing, licensing | domain có nền nhưng model lẫn auto-behavior và observable state |
| `ANLAbel.Project` | JSON save/load `.anlabel` | gọn nhưng thiếu envelope/schema/migration/atomic save |
| `ANLAbel.Data` | ExcelDataReader, registry, preferences, data/print logs, Excel export | Excel path khá sâu; connector abstraction và event store chưa có |
| `ANLAbel.Barcode` | ZXing renderer/vector data | useful seam; GS1/typed barcode data/native backends chưa có |
| `ANLAbel.Printing` | discovery, profiles, preflight, render plan, visual renderer, print service | nền tốt; job lifecycle/headless queue/parity còn thiếu |
| `ANLAbel.App` | WPF shell, dialogs, ViewModels, designer, template gallery | feature-rich nhưng monolithic và hard-coded UI |
| Tests | 51 custom checks + 45 xUnit baseline gần nhất | tốt cho lịch sử dự án, thiếu compiler/golden/UI/job fault suites |

## 3. Điểm mạnh cần giữ

### Physical units

`LabelTemplate` và `LabelObject` lưu X/Y/W/H bằng mm; `MmConverter` tách screen DIP và printer dots. Đây là invariant đúng cho sản phẩm công nghiệp.

### Model/print separation bước đầu

`ANLAbel.Printing/RenderPipeline/LabelVisualRenderer.cs` render từ model thay vì chụp canvas. `PrintRenderPlan` đã chứa DPI, media, feed, offset và scale. Không được đánh mất khi redesign.

### Preflight

`PrintPreflightValidator` kiểm tra missing binding, bounds, text overflow, barcode validity và module size theo print DPI. Đây là hạt nhân để nâng thành explainable rule engine.

### Data reliability work

`ExcelDataService` dùng ExcelDataReader, background Task, cancellation/timeout và format adapter. `MainViewModel` có link recovery, watcher/stale state, key tracking và shared data source registry.

### Logs và tests

CSV append-only print history, JSONL operation logs, export Excel, application regression runner và xUnit tạo nền evidence tốt hơn nhiều app cùng quy mô.

## 4. Critical stabilization findings

Các mục này không chờ “reinvention hoàn tất”. Chúng nên thành lane S0 có test bảo vệ.

### S0-01 — Ba nơi cùng thay geometry QR/matrix

Evidence:

- `LabelObject.cs:325-363` tự đổi Width/Height theo property;
- `MainViewModel.cs:2527-2577` có logic thứ hai, còn resolve PreviewRow và available size;
- `LabelDesignerCanvas.cs:1928-1967` có logic thứ ba; được gọi trong property/render update path tại khoảng line 555.

Rủi ro:

- ba rule không hoàn toàn giống nhau;
- PreviewRow/canvas refresh có thể tác động geometry;
- undo/history nhận mutation không gắn một user command;
- vi phạm invariant trong `agent.md` nếu canvas path chạy từ update/render.

Hướng:

- chọn một explicit `ResizeMatrixToDataCommand`/layout policy;
- compiler tính required size như diagnostic/layout result;
- chỉ commit geometry khi user/action policy yêu cầu;
- thêm test document hash không đổi khi đổi PreviewRow/render.

### S0-02 — Canvas rebuild lớn và coupling control-per-object

`LabelDesignerCanvas.cs:209-243`, callbacks khoảng `783-816` rebuild object visuals khi template/collection/zoom đổi. Cùng class quản lý selection, drawing, snap, object visuals và property mutation.

Rủi ro: layout churn, stale event handlers, khó incremental render, khó accessibility, khó parity.

Hướng ngắn hạn: instrumentation và tránh rebuild không cần thiết. Hướng đích: Scene Compiler + retained presenter.

### S0-03 — Clone/copy object không đầy đủ

`LabelDesignerCanvas.cs:1349-1389` clone các field cơ bản nhưng không copy đầy đủ QR sizing/error correction/version/module/quiet zone/DPI, image base64, HRI visibility/font và các field tương lai.

Rủi ro: copy/paste tạo object nhìn giống nhưng print khác hoặc mất image/data.

Hướng: central document-node clone/command, test mọi persisted property và stable/new ID policy.

### S0-04 — Print Preview eager render toàn bộ pages ở 300 DPI

`PrintPreviewWindow.xaml.cs:692-755` và `771-796` tạo toàn bộ expanded rows, preflight, preview pages và `RenderTargetBitmap`; `RenderPreviewImage` tại khoảng `962-971` cố định 300 DPI.

Rủi ro:

- memory tăng theo số records × copies × pixel area;
- UI freeze/OOM trên máy 4 GB;
- mọi tick quantity/filter có thể render lại toàn bộ;
- preview DPI không cần thiết cao cho màn hình.

Hướng:

- virtualized/lazy page provider;
- render current/nearby page, cache bounded theo memory;
- preflight streaming/batched/cancellable;
- preview proof và print RenderPlan tách adapter, cùng semantic;
- performance fixture 1/100/1.000 records.

### S0-05 — Preflight/print synchronous trên UI path

`PrintPreviewWindow.xaml.cs` gọi validation/render/print trực tiếp từ handlers; `PrintService.cs:75-104` validate, tạo paginator và gọi print synchronously.

Rủi ro: cửa sổ Not Responding với batch/driver/spooler chậm; re-entry; khó cancel/feedback.

Hướng: async job creation, progress/cancel, background compiler/preflight, dispatch state machine; UI chỉ observe state.

### S0-06 — Barcode vector pixel snapping không theo printer dots

`LabelVisualRenderer.DrawVectorBarcode` tại `LabelVisualRenderer.cs:324-366` nhận `dpi` nhưng snap bằng `Math.Round` trên WPF DIP và không dùng tham số DPI.

Rủi ro: comment nói “printer-pixel boundaries” nhưng đơn vị thực tế là DIP; bar/module có thể không map đúng dots ở 203/300/600 DPI.

Hướng: scene barcode geometry theo physical modules; target adapter map sang device dots hoặc vector contract explicit; golden + physical scan tests.

### S0-07 — Nhập khổ tem thủ công có thể bị bỏ qua

`PrinterSetupWindow.xaml.cs:124-135` luôn ưu tiên `PaperSizesList.SelectedItem`; nếu user sửa Width/Height nhưng list vẫn có selection, `SelectedPaper` vẫn là preset cũ.

Rủi ro: UI cho cảm giác đã nhập custom nhưng job dùng kích thước preset.

Hướng: explicit mode `Preset | Custom`, đổi field custom thì chuyển mode/clear selection, summary/preflight và unit tests.

### S0-08 — Save/registry không atomic, không schema/migration

`ProjectFileService.cs:17-38` dùng `File.Create` rồi serialize trực tiếp. Crash/disk full có thể làm mất file cũ. Template chưa có schema envelope/migration. `DataSourceRegistry` cũng đọc/ghi JSON trực tiếp.

Hướng: temp file cùng volume → flush → atomic replace + backup; envelope/migrator; corrupt recovery tests.

### S0-09 — Async lambda đi vào `Action` RelayCommand

`MainViewModel.cs:154,163,165,167` truyền `async` lambda cho `RelayCommand` có backing `Action` (`RelayCommand.cs:5-30`). Kết quả là `async void`.

Rủi ro: exception khó quản, command re-entry, CanExecute/busy state không đảm bảo, tests không await được.

Hướng: `AsyncRelayCommand` có Task, cancellation, execution state, error policy và re-entry guard.

### S0-10 — Version/release metadata drift

Current csproj/MainWindow là `0.096`, nhưng:

- `App.xaml.cs:94-107` còn `v0.086` ở trial/licensed title;
- `HelpWindow.xaml.cs:874` còn `v0.053`;
- installer `.iss` còn `0.086` ở AppVersion/output/version info.

Rủi ro: user chạy đúng binary nhưng UI/installer báo bản khác; support/release không truy vết được.

Hướng: single version source generated vào assembly/UI/installer; CI gate search/version consistency.

## 5. Architecture gaps

### Document

Có stable string IDs và JSON dễ đọc, nhưng chưa có:

- schema version/migrator;
- revision/content identity;
- resource/font manifest;
- layers/groups/containers/components;
- separation persisted vs editor/runtime state;
- atomic save/recovery.

### Editor

Có multi-select, marquee, snap, drag/resize, drawing, layer-ish z-order và undo; nhưng:

- interaction/render/mutation lẫn trong canvas;
- undo là JSON snapshot toàn template (`MainViewModel.cs:2343-3077`);
- no typed command transaction;
- clone/copy manual;
- selection accessibility khó;
- no general layer/group/constraint model.

### Render consistency

Designer renderer và print renderer là hai implementation. Shared model giúp gần nhau nhưng không có shared compiled scene/text shaping/layout output để chứng minh parity.

### Data

Excel workflow mạnh so với quy mô, nhưng `DataView`/dictionary/string binding đi xuyên UI/domain. Chưa có typed connector/schema/transform/lineage/secret contract.

`ExcelDataService.LoadSheet` vẫn materialize rows vào `List<string[]>` rồi copy tiếp sang `DataTable`; timeout có thể trả UI trong khi task nền tiếp tục. Đây là lý do R4 phải có paging/stream contract và S0/R1 phải đo memory trên máy 4 GB, không coi `Task.Run` là đủ.

### Print

Có profile/preflight/render/print/log nhưng chưa có durable job state, idempotency, feedback semantics, reprint lineage, profile version/evidence hoặc operator-only workspace.

### UX

- shell/ribbon/toolbox/properties cùng một XAML lớn;
- adaptive behavior chủ yếu giảm kích thước/ẩn panel;
- dialog có fixed dimensions đã gây clipping;
- nhiều hard-coded English/Vietnamese strings;
- nhiều mojibake character icons/text trong source;
- chỉ tìm thấy rất ít `AutomationProperties` so với hàng trăm user-facing controls/strings;
- chưa có dark/high-contrast/pseudo-localization architecture rõ.

Chỉ tìm thấy một `AutomationProperties.Name` rõ trong XAML hiện tại, trong khi có hàng trăm control/string tương tác. Đây là evidence thiếu accessibility coverage, không phải kết luận mọi control đều inaccessible vì WPF control mặc định vẫn cung cấp một phần automation.

### Governance/automation

Template library và logs là nền, nhưng chưa có revision store, publish state, diff, roles, approval, headless queue, CLI/REST hay trigger pipeline.

Print logs hiện có khả năng chứa label content, row data và Excel path; chưa thấy retention/redaction/encryption/cross-process lock. Cần privacy classification trước khi dùng log làm audit store.

### Barcode và object fidelity

Model có `ShowBarcodeText` và font HRI, nhưng renderer paths chưa chứng minh dùng đầy đủ; clone hiện bỏ các field này. Chưa có GS1 AI/FNC1 assistant, structured validation, bearer bar/bar-width reduction hoặc scanner verification workflow. Preflight hiện chủ yếu trả blocking issue, chưa có severity/acknowledgement policy.

### Template library và documentation drift

- Embedded library đã có nhiều template và genericization rules tốt, nhưng gallery parse lỗi có thể bị bỏ qua, thumbnail/gallery chưa virtualized và chưa có user/recent/favorite/tag/version semantics.
- `docs/architecture.md` vẫn mô tả Data/Barcode/Printing như placeholder dù production code đã sâu.
- README nói có table object trong khi `ObjectType` hiện chưa có Table; roadmap/docs cần được sinh/kiểm tra từ capability matrix để tránh marketing drift.

### Release/security operations

- `NuGetAudit=false` đang có trong project files; cần có documented dependency audit thay thế hoặc bật trong CI phù hợp.
- Installer hiện unsigned theo README; chưa có SBOM/provenance/checksum/upgrade rollback gate đầy đủ.
- Không thấy global crash boundary/diagnostics export có redaction.

## 6. Maturity score

Scale: 0 chưa có; 1 prototype; 2 usable cơ bản; 3 khá vững; 4 production platform sâu. Đây là gap score dựa trên source, không phải điểm marketing.

| Miền | Điểm | Lý do |
| --- | ---: | --- |
| Physical geometry/model | 3.0 | mm-based, nhiều object/style; schema/layout còn hạn chế |
| Editor interaction | 2.5 | feature khá nhiều; coupling/mutation/undo risk |
| Rendering parity | 2.0 | print renderer riêng; chưa one compiler |
| Barcode | 3.0 | symbologies/QR/preflight tốt; dot mapping/GS1/native depth thiếu |
| Data | 2.5 | Excel reliability/binding sâu; chưa typed graph/connectors |
| Print reliability | 2.5 | preflight/profile/history; chưa job lifecycle/hardware matrix |
| Operator workflow | 1.0 | preview có row selection; chưa Print Center/role split |
| UX/adaptive | 1.5 | usable desktop; shell monolith/HD/accessibility/i18n debt |
| Persistence/revisions | 1.5 | JSON file tốt; no schema/atomic/revision/diff |
| Automation/headless | 0.5 | services có thể reuse nhưng UI-oriented orchestration |
| Governance/security | 0.5 | licensing/logs; no publish/RBAC/audit chain |
| Test/quality | 2.5 | 96 checks baseline; thiếu golden/UI/fault/hardware suites |

## 7. Build/dependency observations

- `ANLAbel.Core` không có external package, là nền tốt để giữ platform-neutral.
- `ANLAbel.Data` có ExcelDataReader và ClosedXML; rule repo cấm quay Excel read path lại ClosedXML. ClosedXML hiện phục vụ export report, cần giữ scope rõ.
- `ANLAbel.Barcode` dùng ZXing.Net.
- App transitive dependency footprint chịu ảnh hưởng Data/Barcode; future headless/core split phải tránh kéo WPF/ClosedXML vào compiler không cần thiết.
- `dotnet list ANLAbel.slnx reference` không hỗ trợ solution target theo cách đã gọi; package audit chạy được. Build workflow chuẩn vẫn là `dotnet build ANLAbel.slnx`.

## 8. Recommended order

1. S0 stabilization issues có nguy cơ mất dữ liệu/in sai/treo.
2. Fixtures + document envelope + compiler shadow mode.
3. Commands/viewport/inspector contracts.
4. Unified preview/print + job core.
5. Shell/data/operator workflows.
6. Advanced layout/revision/automation.

Không nên bắt đầu bằng đổi màu/ribbon hoặc rewrite framework. Các thay đổi đó không giải quyết các nguồn drift ở trên.

## 9. Evidence map

| Claim | Evidence chính |
| --- | --- |
| mutable mm models | `ANLAbel.Core/Models/LabelTemplate.cs`, `LabelObject.cs` |
| snapshot undo | `MainViewModel.cs:2343-3077` |
| canvas responsibilities | `LabelDesignerCanvas.cs` methods `Rebuild`, `UpdateObjectElement`, mouse/selection/snap/render |
| separate print renderer | `ANLAbel.Printing/RenderPipeline/LabelVisualRenderer.cs` |
| preflight | `ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs` |
| Excel async/streaming | `ANLAbel.Data/Excel/ExcelDataService.cs` |
| logs | `ANLAbel.Data/PrintLogs`, `DataLogs` |
| persistence gap | `ANLAbel.Project/SaveLoad/ProjectFileService.cs` |
| UI density/i18n | `MainWindow.xaml`, dialog XAML, `HelpWindow.xaml.cs` |
| tests | `ANLAbel.Tests/Program.cs`, `ANLAbel.UnitTests/` |
