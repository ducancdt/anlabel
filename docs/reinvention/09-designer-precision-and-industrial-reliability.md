# Designer Precision và Industrial Reliability

Trạng thái: **Approved planning baseline — implementation checkpoint v0.186; operator support export + GS1 industrial AI subset; hardware gates open**  
Cập nhật: **2026-08-11**  
Phạm vi: bắt điểm, căn chỉnh, text layout, ruler/guide, độ xác định của scene, preflight, media/printer profile, job lifecycle và kiểm thử máy in thật.

Tài liệu này là phần chi tiết bắt buộc của [kế hoạch thực thi tổng](07-execution-plan.md), không phải danh sách ý tưởng UI. Mọi work item ở đây phải giữ các invariant trong [RULES.md](RULES.md), đạt gate tương ứng trong [QUALITY-GATES.md](QUALITY-GATES.md), và không được tuyên bố hỗ trợ máy in nếu chưa có evidence vật lý.

> Latest implementation checkpoint: v0.186 (Print Center redacted support-evidence export from durable jobs; GS1 industrial AI subset for weight/GLN/company-internal fields). Physical-device certification is not claimed.

## Implementation checkpoint v0.185 (product-wired software-track)

Post-v0.181 software fixtures are closed **and wired into shipped paths**:

- **D1 / DP-129:** `LabelDesignerCanvas` calls only `SnapPathMatrixContract.Choose` / `ApplyHysteresis` for single/group/resize/draw.
- **D2 software soak:** 500 mixed objects, multi-select/key identity across 25/100/400% zoom, resize-cancel restore (not real mouse-capture telemetry).
- **D4 / IR-131:** plans retain `EffectiveOutputContract`; last-mile revalidation uses field-level `Evaluate` so DPI/media/ticket/imageable drift is named before `PrintDocument`.
- **IR-130:** `PrintService.AttachSupportEvidence` on batch and calibration spool-accept writes redacted support JSON/fingerprint onto `PrintJobResult`.
- **D7 / IR-134:** async command re-entry guard remains regression-tested.
- **D8:** release metadata synchronized at product version `0.185`.

Evidence: 133 application checks, 305 xUnit tests, zero-warning Release build. This is **not** physical certification.

Hardware-only remainders: baseline pointer/display traces; real mouse-capture group/resize telemetry; installed-font visual measurements; live driver ticket/imageable coercion; spooler restart/hot-unplug; thermal-driver goldens; vendor scanner/verifier adapters; physical printer/scanner records.

## Implementation checkpoint v0.184 (contracts introduced)

Core contracts and unit fixtures for the matrix, dispatch revalidation and support redaction were introduced; product wiring completed in v0.185.

## Implementation checkpoint v0.181

One design-time `PrintRenderPlan` is now exercised against three bound rows with different text and QR payload lengths. Preflight, preview metadata and compiled-scene geometry must remain identical for every row, while the mutable authoring model's document hash stays unchanged. Designer style edits now rebuild the text visual immediately (font, alignment, padding and overflow policy) rather than only moving its host, removing a stale-canvas WYSIWYG seam. Static text preview/print now uses the same physical left-padding origin as the designer fast path, closing a legacy `+2 DIP` drift for padded labels. Preview effective PrintTicket/Capabilities preparation now runs on a dedicated STA before preflight, so driver capability queries cannot block the WPF dispatcher. AutoFit geometry now measures the same physical edge padding used by preview/print, so configured insets cannot be lost when text owns its frame. Single-line dragging now uses the shared object/grid snap candidates and guide overlay, then clamps the complete stroke hull to the label bounds; line movement can no longer bypass alignment or place a thick stroke outside the authored stock. Interactive print now records the queue shown before the dialog and rejects an unchanged Windows-default queue when no saved industrial queue exists, while a deliberately changed queue proceeds through canonical lookup and effective-ticket validation. Preview/Quick Print/Calibration serialize their inputs before running the synchronous driver call on a dedicated STA worker; pre-start cancellation and a Preview busy gate prevent UI freeze and duplicate dispatch. Printer Setup now restores the saved non-first catalog stock after its category callback repopulates the list, and edited dimensions remain an explicit custom stock; this prevents a reopened industrial label from silently changing paper identity. The release gate additionally rejects drift between the project version, app/help titles and commercial/trial installer metadata. This closes the software fixtures requested after v0.180; it does not claim that a Windows spooler, thermal driver or physical scanner produced verified media. Evidence is 125 application checks, 284 xUnit tests and a zero-warning Release build.

Calibration now uses the same input-snapshot and dedicated-STA dispatch boundary as Preview and Quick Print after the operator explicitly selects a queue; the UI remains responsive while the driver call runs, and cancellation is honored before worker start.

**Superseded software portion:** v0.184 closes the pure-contract software fixtures for D1/DP-129, D4/IR-131, D7/IR-134 and support redaction. Hardware evidence for D1–D6 remains required before production certification.

## Implementation checkpoint v0.180

Fixed QR validation now uses the actual version/correction byte-capacity table rather than estimating capacity from the authored rectangle area. A version-1/M symbol with more than its exact 14-byte byte-mode capacity is blocked even when the object frame is physically large enough; the diagnostic includes the selected version, correction level and capacity. This keeps data correctness separate from visual frame size and stops invalid symbols before spool. Evidence is 113 application checks, 284 xUnit tests and a zero-warning Release build; scanner/driver evidence remains open.

## Implementation checkpoint v0.179

Before dispatch, each resolved data row now passes its QR/DataMatrix geometry through the shared contract. A required target size larger than the authored frame is a blocking issue for AutoSizeByData and fixed module/version modes; the renderer is no longer allowed to make an apparently valid but physically undersized symbol by squeezing it into the object rectangle. The template remains immutable during row validation, so one oversized row cannot silently change subsequent labels. Evidence is 113 application checks, 284 xUnit tests and a zero-warning Release build; verifier/driver evidence remains open.

## Implementation checkpoint v0.178

`QrObjectGeometryContract` is now the single pure geometry decision used by `LabelObject`, `LabelDesignerCanvas` and `MainViewModel`. It centralizes square 2D detection, fixed-version/module sizing, resolved-row auto-sizing, maximum available label-space clamping and the 0.05 mm change tolerance. An unresolved bound value does not mutate the authored frame; a fixed-size code can still resolve without row data. This removes formula drift between model setters, preview-row refresh and canvas property notifications. Evidence is 111 application checks, 284 xUnit tests and a zero-warning Release build; physical output remains unverified.

## Implementation checkpoint v0.177

When Escape, lost mouse capture or an adorner cancellation restores the captured physical geometry, the canvas now recalculates its workspace extent before notifying the view model. This prevents a previously overflowed object from leaving stale scroll/selection bounds after a canceled gesture. The same boundary rule is used by successful commits, while document history still records no canceled edit. Evidence remains software-only: 111 application checks, 279 xUnit tests and a zero-warning Release build.

## Implementation checkpoint v0.176

Designer pointer work now avoids unnecessary content work. `LabelDesignerCanvas` routes X/Y/line-endpoint changes through a transform-only host update, so a move does not rerasterize an unchanged barcode/image or recreate text visuals. During active move/resize the canvas extent is not scanned for every property notification; it is reconciled once at gesture completion. `MainViewModel.RecordTemplateChange` checks the explicit gesture transaction before `CaptureTemplateSnapshot`, removing full-template JSON serialization from every pointer tick while preserving exact start/final undo and cancel semantics. Evidence is 111 application checks, 279 xUnit tests and a zero-warning Release build.

This is a software responsiveness improvement, not a hardware claim. Baseline-workstation pointer p95/max traces and long mixed-object soak remain required before declaring the editor production-grade.

## Implementation checkpoint v0.175

The SDK-neutral physical verifier seam now has operational guardrails, not only a type contract. `PhysicalVerifierAdapterOptions` enforces a finite 30-second default timeout (capped at five minutes), links caller cancellation and maps an unresponsive adapter to `adapter-timeout`. Caller cancellation remains observable as cancellation. The adapter retains one in-flight task until it finishes; a second call receives `adapter-busy`, preventing overlapping reads when a vendor SDK ignores cancellation. Evidence is 111 application checks, 279 xUnit tests and a zero-warning Release build.

This is still software evidence. Each vendor adapter must demonstrate cancellation behavior, transport/disconnect handling and real scanner/verifier semantics before a physical job can be certified.

## Implementation checkpoint v0.174

Vendor scanner/verifier integrations now have a common async boundary. The adapter returns no raw payload: only adapter/version, correlation token, method/outcome, canonical observed digest, device ID, grade and timestamp. Mapping validates identity/method/correlation and requires a SHA-256 digest for barcode verification before producing durable evidence. A thermal-golden request is checked against the manifest/profile/frame before the adapter is invoked. Evidence is 111 application checks, 276 xUnit tests and a zero-warning Release build.

This remains an SDK-neutral seam. Real vendor payload semantics, transport faults, driver/firmware/media capture and physical printer certification remain open.

## Implementation checkpoint v0.173

Thermal raster evidence now has a fail-closed physical-context contract. A `ThermalRasterProfile` fingerprints the exact queue, driver/version, firmware, media, ribbon, calibration, DPI and output geometry; a `ThermalRasterGoldenBinding` combines that profile fingerprint with the exact `RasterGoldenIdentity`. The evidence is carried through plan, preview page, result, manifest v2 and operation log; changing any profile or frame component invalidates the binding. Legacy manifest v1 fingerprints remain readable. Evidence is 111 application checks, 270 xUnit tests and a zero-warning Release build.

This is still software evidence. Real driver/firmware/media capture, thermal-device frame comparison, scanner/verifier SDK mapping and physical printer certification remain open.

## Implementation checkpoint v0.172

The preview worker now returns a frozen bitmap together with a `RasterGoldenIdentity` calculated on the worker that rendered it. The identity covers the exact Pbgra32 frame (pixel width/height, independent 300 DPI axes, stride, format and bytes); the page cache retains it beside each image, so repeated renders are deterministic and frame-size/content changes are observable. The legacy image-only call remains compatible. Evidence is 111 application checks, 262 xUnit tests and a zero-warning Release build.

This is a preview/software evidence boundary, not thermal-driver or physical-media certification. Driver-specific golden fixtures, scanner/verifier SDK mapping and physical printer/media tests remain open.

## Implementation checkpoint v0.171

Barcode-verifier jobs now carry a versioned expectation rather than a bare opaque digest. The expectation binds symbology, application profile and the canonical payload (including the shared GS1 normalization), then requires an explicit ANSI, ISO/IEC 15415 or ISO/IEC 15416 minimum grade. The physical verifier coordinator checks that the adapter returned the reviewed content fingerprint and a grade meeting policy; mismatches, unknown/vendor grades and below-threshold results remain unverified. Evidence is 110 application checks, 262 xUnit tests and a zero-warning Release build.

This is the software policy seam for a future SDK, not a verifier certification. Adapter payload mapping, device-specific grade semantics, thermal-driver golden rasters and physical printer/media evidence remain open.

## Implementation checkpoint v0.170

The designer/print plan now has two additional evidence boundaries. `IPhysicalOutputVerifier` and its coordinator keep scanner/barcode-verifier integration outside Core while making invalid requests, missing evidence and adapter faults fail closed. `RasterGoldenContract` fingerprints the exact device frame with a revisioned SHA-256 over geometry, independent DPI axes, stride, format and bytes; a preview or thermal-driver fixture therefore cannot silently pass after a DPI/row-layout/pixel change. Evidence is 110 application checks, 257 xUnit tests and a zero-warning Release build.

This closes a software seam, not a hardware claim. A production adapter, symbology payload/grade policy, thermal-driver fixture set and physical printer/media certification remain open before `Completed` can represent verified media output.

## Implementation checkpoint v0.169

`Completed` is now a guarded lifecycle state, not a synonym for `PrintDocument` returning or the Windows queue reporting `Completed`. The transition requires hash-verified `PhysicalOutputVerificationEvidence` from a scanner or barcode verifier, bound to the exact job ID and manifest fingerprint, with expected and observed content fingerprints equal and a device identity present. Visual inspection remains an audit signal only. The event store carries the evidence fingerprint through its hash chain and rejects tampered evidence during replay. Evidence is 110 application checks, 250 xUnit tests and a zero-warning Release build.

The contract is intentionally hardware-neutral. A production adapter still needs to map real scanner/verifier payloads, symbology and grade thresholds into the expected/observed fingerprints; thermal raster golden tests and on-device certification remain open.

## Implementation checkpoint v0.168

Accepted submissions now carry a value-only pre-dispatch queue snapshot. Preview and quick print can poll a fresh queue snapshot on a worker for a bounded window after `PrintDocument` returns, which covers drivers that publish jobs asynchronously without freezing the designer. The same unique/name-match resolver remains fail-closed: no snapshot, duplicate candidate or inaccessible queue means no spool identity and no unsafe monitor/retry path. Evidence is 110 application checks, 244 xUnit tests and a zero-warning Release build.

This improves correlation evidence only; it still cannot prove that a label reached media. Verifier/scanner integration, thermal-driver golden rasters and physical-printer certification remain open.

## Implementation checkpoint v0.167

The dispatch seam no longer chooses the newest visible spool identifier by default. A value-only before/after resolver correlates a job only when the post-dispatch candidate is unique, or when an exact job-name match leaves one candidate among concurrent submissions. A missing snapshot, duplicate candidate or duplicate name match yields no identity and therefore no unsafe queue monitor/retry path. Core fixtures cover the ambiguity and missing-evidence cases. Evidence is 110 application checks, 242 xUnit tests and a zero-warning Release build.

The remaining identity seam is bounded delayed capture: some drivers publish the job asynchronously after `PrintDocument` returns, so a future adapter must wait off the UI thread and still refuse ambiguous matches. Verifier/scanner integration, thermal-driver golden rasters and physical-printer certification remain open.

## Implementation checkpoint v0.166

Recovery no longer treats a printer/driver fault as a generic queue observation. Offline, paper-out, user intervention, blocked, paused, retained and explicit driver-error states are classified as terminal for the current reconciliation attempt, whether they arrive from the live spool reader or from a replayed event log. Print Center receives an operator-decision result with physical output still unverified; no code path turns these states into an automatic retry. The fault matrix is covered by the xUnit suite. Evidence is 110 application checks, 237 xUnit tests and a zero-warning Release build.

The next reliability seam is delayed or ambiguous spool-job identity capture: a queue adapter must never associate a newly visible job with this submission unless the identity is unique and attributable. Verifier/scanner integration, thermal-driver golden rasters and physical-printer certification remain open.

## Implementation checkpoint v0.165

The effective print plan now derives a value-only `DeviceRenderGeometry` from physical label dimensions, imageable-area DIP values and independent printer `dpiX/dpiY`. Label and printable bounds are recorded as integer dots and carried through preview pages and the output-contract fingerprint. Barcode bitmap rendering uses the non-square capability when available; square-only providers are normalized to the target dot frame with nearest-neighbour pixels before WPF scaling. Tests cover 203/300/305/600/609-style axes, non-square 305×609 output, invalid printable-area fail-closed behavior and exact plan/preview geometry identity. Evidence is 110 application checks, 223 xUnit tests and a zero-warning Release build. Thermal-driver golden rasters, verifier scans, queue fault injection and physical-printer certification remain open.

## Implementation checkpoint v0.164

Save and rollback now preserve exact prior bytes in a bounded `.revisions` archive beside the `.anlabel` file. Each snapshot has a SHA-256 content hash, UTC timestamp and reason, while `audit.jsonl` records the event without copying label payloads into the log. The revision UI validates primary, `.bak` and archive files independently; only a valid managed backup or local archive can be selected for an explicit rollback. Retention is limited to eight snapshots and cleanup is restricted to that derived archive folder. Software evidence is 109 application checks, 220 xUnit tests and a zero-warning Release build. This remains document-level evidence: thermal driver/raster parity, queue/spooler behavior, verifier scans and physical-printer certification remain open.

## Implementation checkpoint v0.163

The revision window now compares two valid document snapshots before rollback. It surfaces physical size, design/printer DPI, queue/paper/media/feed, offsets/scales, object counts, guides, linked-data summary and full `DocumentHash` values. Invalid or future-schema sides are explicitly non-comparable rather than being reduced to a misleading “no changes” result. The application matrix is 109 checks, the Core/printing xUnit suite is 220, and Release builds remain warning-free. Multi-revision retention/audit, power-loss injection, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.

## Implementation checkpoint v0.162

The committed template now has an operator-facing revision surface. The File ribbon opens a history window that validates the primary and `.bak` independently and exposes diagnostics for missing, malformed and future-schema files. Only a valid managed backup can be selected; rollback asks for confirmation, restores the exact validated bytes atomically and retains the previous primary in the new backup slot. Cancellation before the commit boundary leaves both artifacts byte-for-byte unchanged. The application matrix is 109 checks, the Core/printing xUnit suite is 220, and Release builds remain warning-free. Multi-revision retention/diff review, power-loss injection, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.

## Implementation checkpoint v0.161

Template persistence now has a recovery contract suitable for an industrial design workstation. Every replacement save preserves the prior valid document as `*.anlabel.bak` via a same-directory durable copy before committing the new primary. Opening malformed JSON reports `ProjectLoadResult.RecoveredFromBackup`, identifies the backup path and forces Save As after the linked-data restore; the original damaged file is not silently overwritten. A corrupt backup produces a combined diagnostic, while future or incompatible schema markers fail closed and cannot be replaced by an older backup. A rejected open is parsed before the current document is detached, preserving unsaved work. The application matrix is 108 checks, the Core/printing xUnit suite is 220, and Release builds remain warning-free. Physical crash/power-loss testing, driver/raster parity, queue/spooler, verifier and printer certification remain open.

## Implementation checkpoint v0.160

`.anlabel` persistence now has an explicit envelope (`format`, `schemaVersion`, `template`). Save remains temporary-file/write-through/rename based; cancellation cleans the temporary artifact and preserves the previous committed bytes. Load supports the raw `LabelTemplate` format used by older files as a deliberate migration path, but rejects future schema numbers, wrong format markers, malformed JSON and non-object roots instead of silently dropping industrial label fields. Evidence is 106 application checks, 220 xUnit tests and a zero-warning Release build; operator backup/recovery UI, upgrade/rollback, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.

## Implementation checkpoint v0.159

Resize cancellation now has one restore invariant across the UI routes that can interrupt a gesture. The single-object adorner captures the complete physical frame at start, and Thumb-level loss, canvas-level loss, canceled drag completion or adorner teardown restore that frame before the edit gesture is canceled. The adorner is idempotent, so the same routed event cannot double-close undo history. Group resize continues to restore every selected member from its start snapshot. Evidence is 105 application checks, 220 xUnit tests and a zero-warning Release build; real pointer-capture traces, display measurements, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.

## Implementation checkpoint v0.158

Resize handles now carry their semantic edge and keyboard modifier state into a WPF-free geometry contract. Shift preserves the authored physical width/height ratio, including side handles; Ctrl expands or contracts around the original centre; and the same result is used for both single-object and multi-selection group resize before the existing snap, minimum-size and artboard guards. Alt remains the explicit way to bypass pointer snapping. Evidence is 104 application checks, 220 xUnit tests and a zero-warning Release build; real pointer-capture traces, display measurements, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.

## Implementation checkpoint v0.152

`TextSizingMode.ScaleWidth` now gives industrial text frames a deterministic remediation choice when changing font size would violate the authored typography. The shared layout path preserves the authored point size and line height, computes a bounded horizontal factor with a 0.5 minimum, and anchors the transform to the resolved left/center/right alignment. Designer, preview, print and preflight reuse the same line metrics, visible-ink bounds and scene identity. Values below the supported factor remain a blocking `Error` instead of being silently distorted. Software evidence is 99 application checks and 214 xUnit tests; display-scale pointer measurements, driver raster parity, spooler fault fixtures, verifier scans and physical-printer evidence remain open.

## Implementation checkpoint v0.153

`PointerFrameTelemetry` now records one bounded timing sample after each canvas drag preview frame, tagged with normalized zoom and display `PixelsPerDip`. Its snapshot API computes average, P95 and maximum outside the pointer event, supports per-zoom/display filtering, and reports whether the 16.667 ms frame budget is met. `LabelDesignerCanvas.PointerTelemetry` is observational and does not alter document coordinates, snap hysteresis or undo grouping. Evidence is 100 application checks, 217 xUnit tests and a zero-warning Release build; rendering the diagnostic overlay, real display-scale measurements, driver raster parity, verifier scans and physical-device certification remain open.

## Implementation checkpoint v0.154

The canvas now exposes `ShowPointerTelemetry` through its context menu. When enabled, the diagnostic overlay displays P95/max drag-frame timing, ring utilization, normalized zoom and display `PixelsPerDip`; it updates only after a recorded preview frame and never becomes part of the saved or printable scene. The default remains off. Evidence is 101 application checks, 217 xUnit tests and a zero-warning Release build; representative display-scale traces, driver raster parity, queue fault fixtures, verifier scans and physical-printer evidence remain open.

## Implementation checkpoint v0.155

Designer text rendering, overflow detection, baseline placement and optical-ink measurement now use the actual loaded visual's `PixelsPerDip`, closing the monitor-scale seam between what the author sees and the measured text. Persisted auto-size deliberately remains model-independent at PPD 1.0, so changing Windows display scaling cannot silently change saved label geometry. An STA identity fixture proves text layouts measured at 1.0 and 2.0 PPD remain distinct. Evidence is 102 application checks, 217 xUnit tests and a zero-warning Release build; representative display traces, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.

## Implementation checkpoint v0.156

The canvas now treats the primary selected object as an explicit key object without sacrificing the rest of a multi-selection. Clicking an already-selected peer changes the key outline and `SelectedObject` while retaining every selected object; Ctrl-click removal and additive selection remain predictable. `SetKeyObject` rejects objects outside the current template or selection, and the existing align/distribute/baseline/optical commands therefore operate on a stable reference. Evidence is 103 application checks, 217 xUnit tests and a zero-warning Release build; real pointer capture, display traces, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.

## Implementation checkpoint v0.157

Drawing line/rectangle/ellipse endpoints now use the same semantic snap selector as object move and resize. Visible object edges/centers, artboard edges/center, persistent ruler guides and the physical grid all share the zoom-normalized acquire/release budget, hysteresis lock and guide captions. Alt bypasses pointer snapping, while typed dimensions remain exact and do not inherit a nearby snap target. Evidence is 104 application checks, 217 xUnit tests and a zero-warning Release build; real mouse-capture/cancel traces, display measurements, driver/raster parity, queue/spooler, verifier and physical-printer evidence remain open.
> Latest implementation checkpoint: v0.115 (font/glyph coverage preflight); installed-font and physical-device evidence remain open.
> Latest implementation checkpoint: v0.116 (effective PrintTicket bound to preview manifest); driver/hardware and installed-font evidence remain open.
> Latest implementation checkpoint: v0.117 (injectable font catalog + international Unicode matrix); installed-font and physical-device evidence remain open.
> Latest implementation checkpoint: v0.118 (bounded preview dimension validation + 300-cycle long-soak); baseline-workstation, driver and physical-device evidence remain open.
> Latest implementation checkpoint: v0.119 (ruler offset synchronization + spool-monitor deadline race fix); visual ruler drift and physical queue/device evidence remain open.
> Latest implementation checkpoint: v0.120 (bounded image decode/effective-PPI preflight + version alignment); visual drift, baseline-workstation, driver and physical-device evidence remain open.
> Latest implementation checkpoint: v0.124 (text-baseline snapping + shared Justify alignment); platform-neutral glyph metrics, verifier, dither, driver and physical-device evidence remain open.
> Latest implementation checkpoint: v0.125 (physical-mm grid contract + overlap-safe smart-spacing candidates); optical glyph metrics, verifier, dither, driver and physical-device evidence remain open.
> Latest implementation checkpoint: v0.132 (multi-selection resize hull with transformed-object scaling); platform-neutral glyph shaping, verifier, dither, driver and physical-device evidence remain open.
> Latest implementation checkpoint: v0.138 (generation-scoped canvas refresh and incremental object reconciliation); display-scale, driver, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.139 (physical text-padding/layout-frame contract); per-edge overflow policy, driver, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.141 (edge-aware physical text-frame contract); preview-vs-dispatch plan identity; explicit overflow policy, driver, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.142 (horizontal fixed-frame glyph overflow detection); explicit remediation modes, driver, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.143 (zoom-normalized snap tolerance and inclusive acquire boundary); display-scale, driver, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.144 (compiled text-frame parity and explicit overflow policy); ellipsis/shrink, driver, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.145 (last-mile dispatch contract revalidation); queue/spooler faults, driver raster, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.146 (rotated group-move transformed hull parity); pointer telemetry, driver raster, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.147 (rotated-object preflight transformed-hull parity); pointer telemetry, driver raster, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.148 (line stroke bounds parity across designer and preflight); pointer telemetry, driver raster, verifier and physical-device evidence remain open.
> Latest implementation checkpoint: v0.149 (quick-print effective-contract ordering and explicit queue dispatch); pointer telemetry, driver raster, verifier, queue/spooler and physical-device evidence remain open.
> Latest implementation checkpoint: v0.150 (explicit ellipsis text remediation across bounded layout paths); pointer telemetry, driver raster, verifier, queue/spooler and physical-device evidence remain open.
> Latest implementation checkpoint: v0.151 (bounded ShrinkFont remediation and zoom-stable snap regression); ScaleWidth, pointer telemetry, driver raster, verifier, queue/spooler and physical-device evidence remain open.

## 1. Outcome cần đạt

### Designer Precision

Người thiết kế phải có thể:

- kéo, resize và nudge object mà không rung, nhảy mục tiêu hoặc phụ thuộc mức zoom;
- biết rõ đang snap vào cạnh, tâm, baseline, guide, grid, artboard hay khoảng cách đều;
- chọn một **key object** làm mốc rồi align/distribute cả nhóm bằng một command undo được;
- căn text theo khung, theo chiều dọc và theo baseline giữa các font/cỡ chữ khác nhau;
- chọn rõ text box là `HugContent`, `FixedWidthAutoHeight` hay `FixedFrame`, và chọn policy overflow;
- nhập tọa độ/kích thước vật lý chính xác; thao tác chuột chỉ là một cách sửa cùng document geometry;
- nhận cùng kết quả scene sau save/load, undo/redo, preview và print.

### Industrial Reliability

Người vận hành phải có thể:

- biết template, dữ liệu, media, printer profile, DPI và printable bounds nào tạo ra job;
- được chặn trước khi in nếu text overflow, barcode sai, font thiếu, dữ liệu cũ hoặc media/profile không khớp;
- phân biệt `spool accepted`, `printer acknowledged`, `completed`, `failed` và `unknown` thay vì coi “gửi xong” là “đã in xong”;
- khôi phục sau crash/spooler restart mà không tự động in trùng;
- dùng profile/calibration riêng theo printer + stock, với evidence đo thực tế;
- kiểm tra cùng một template ở 203/300/600 DPI và trên gap/black-mark/continuous media.

## 2. Evidence nghiên cứu và cách diễn giải

### Fact từ nguồn chính thức

- Loftware Desktop Designer có align left/center/right/top/middle/bottom và distribute theo khoảng cách ngang/dọc; tùy cấu hình, object đầu tiên hoặc object biên/lớn nhất đóng vai trò tham chiếu. Nguồn: [Loftware — Align](https://help.loftware.com/cloud/Designer/Workspace-Overview/Tabs-and-Ribbons/Home-Tab/Align.html).
- Loftware phân biệt snap-to-object, snap-to-grid và free move; khi object align, đường dẫn hướng xuất hiện. Ruler, gridline, snapline và resize handle là visual aids riêng. Nguồn: [Design Surface Context Menu](https://help.loftware.com/cloud/Designer/Workspace-Overview/Context-Menus/Design-Surface-Context-Menu.html), [Visual Aid Elements](https://help.loftware.com/cloud/Designer/Workspace-Overview/Design-Surface/Visual-Aid-Elements.html).
- Loftware mô tả Shift để khóa hướng kéo, Ctrl+Arrow để tinh chỉnh vị trí và Shift+click để thêm object vào selection. Nguồn: [Efficient Use of Keyboard and Mouse](https://help.loftware.com/cloud/Designer/Introduction/Keyboard-and-Mouse-Support/Efficient-Use-of-Keyboard-and-Mouse.html).
- BarTender dùng **master selected object** làm mốc khi align nhiều object; từ ba object trở lên mới có distribute. Snap có thể dựa trên object, ruler, grid, biên và center line của template/form. Nguồn: [BarTender — Aligning Objects](https://help.seagullscientific.com/11.8/en/Content/Aligning_Objects.htm), [BarTender Document Options](https://help.seagullscientific.com/11.4/en/Content/HIDD_FORMAT_OPTIONS.htm).
- BarTender áp snap khi move và resize; click lại một object đang nằm trong selection có thể đổi master mà không bỏ selection. Nguồn: [BarTender — Object Snapping](https://help.seagullscientific.com/10.1/en/content/SnapBehavior.html), [Master Selected Object](https://help.seagullscientific.com/11.8/en/Content/Objects_MasterSelected.htm).
- Loftware phân biệt Text tự lấy kích thước theo nội dung và Text Box có frame/fit behavior; BarTender cũng cung cấp auto-fit với giới hạn font/scale/spacing. Nguồn: [Loftware — Text](https://help.loftware.com/cloud/Designer/Label/Label-Objects/Text.html), [Loftware — Text Box](https://help.loftware.com/cloud/Designer/Label/Label-Objects/Text-Box.html), [BarTender — Auto Fit](https://help.seagullscientific.com/11.5/en/Content/Auto_Fit_PropertyPage_text.htm).
- WPF `FormattedText` cung cấp `Baseline`, `Height`, `Extent`, `MaxTextWidth`, `MaxTextHeight` và low-level text layout. WPF `TextFormatter` tạo `TextLine` và hỗ trợ paragraph direction, runs và international layout. Nguồn: [Microsoft — FormattedText](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.formattedtext), [Microsoft — Advanced Text Formatting](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/advanced-text-formatting), [Microsoft — WPF bidirectional features](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/bidirectional-features-in-wpf-overview).
- Unicode UAX #9 là chuẩn normative cho việc resolve thứ tự hiển thị của đoạn bidi; `TextDirectionMode.Auto` của ANLAbel chỉ chọn paragraph base direction từ strong letter rồi để WPF xử lý mixed runs, không đảo chuỗi dữ liệu. Nguồn: [Unicode UAX #9](https://www.unicode.org/reports/tr9/).
- WPF dùng device-independent units và có thể gặp cạnh mờ khi rơi giữa device pixel; layout rounding là khái niệm dành cho UI screen. Nó không thay thế việc quantize theo printer dots. Nguồn: [Microsoft — WPF Layout](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/layout).
- Zebra yêu cầu media sensor/calibration phù hợp với gap, black mark/notch hay continuous media; sensor profile được dùng để chẩn đoán khi printer nhận sai gap. TSC cũng yêu cầu chọn loại sensor và calibrate khi đổi media; manual calibration là fallback khi auto calibration không thành công. Nguồn: [Zebra — Manually Calibrating Media](https://docs.zebra.com/us/en/printers/desktop/zd410-desktop-printer-ug/tools/activating-advanced-mode/manually-calibrating-media.html), [Zebra — Sensor Profile](https://docs.zebra.com/content/tcm/us/en/printers/desktop/zd410-desktop-printer-ug/tools/printer-diagnostics/sensor-profile.html), [TSC MX241P manual](https://fs.tscprinters.com/system/files/31-1510003-00_mx241p_user-manual_en_a.pdf).
- Windows `PrintCapabilities`/`PrintTicket` cho phép hỏi khả năng printer và tránh option không được hỗ trợ; trạng thái spooler có `offline`, `paper out`, `error`, `user intervention`, v.v. Nhưng `JOB_STATUS_COMPLETE` có thể chỉ có nghĩa job đã gửi tới printer, chưa chứng minh vật lý đã in. Nguồn: [Microsoft — WPF Printing Overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/documents/printing-overview), [Microsoft — JOB_INFO_1](https://learn.microsoft.com/en-us/windows/win32/printdocs/job-info-1).
- Microsoft `MergeAndValidatePrintTicket` trả effective ticket và conflict status sau khi driver kiểm tra; `PageImageableArea` mô tả origin/extent nếu driver cung cấp. Nguồn: [MergeAndValidatePrintTicket](https://learn.microsoft.com/en-us/dotnet/api/system.printing.printqueue.mergeandvalidateprintticket?view=windowsdesktop-10.0), [PageImageableArea](https://learn.microsoft.com/en-us/dotnet/api/system.printing.printcapabilities.pageimageablearea?view=windowsdesktop-10.0).
- BarTender phân biệt trạng thái chỉ “sent” với trạng thái có printing verification; đây là bằng chứng rằng submit và verified physical outcome là hai mức khác nhau. Nguồn: [BarTender — LastStatus](https://help.seagullscientific.com/11.5/en/Subsystems/BTXML/Content/LastStatus_Tag.htm).
- GS1 verification kiểm tra cả chất lượng in, cấu trúc symbol, X-dimension/quiet zone, data content/format và application requirements; “scan được một lần” không tương đương verified. Nguồn: [GS1 Barcode Verification Guideline](https://ref.gs1.org/guidelines/barcode-verification/), [GS1 1D Verification Process](https://www.gs1.org/docs/barcodes/GS1_Bar_Code_Verification.pdf).
- DPI thiết bị thực tế không chỉ có 203/300/600; ví dụ SATO công bố 203/305/609 DPI trên dòng CL4NX Plus. Nguồn: [SATO — Resolution specification](https://www.manual.sato-global.com/printer/clnxplus/main/main_GUID-92941A3B-53D8-43A2-9A81-0AAFDC0953C2.html).

### Recommendation của ANLAbel

Các threshold, priority, keyboard semantics, work-item order và performance budget bên dưới là quyết định thiết kế của ANLAbel dựa trên evidence trên. Chúng không phải tuyên bố rằng NiceLabel, BarTender, Microsoft, Zebra, TSC hoặc GS1 dùng cùng thuật toán.

## 3. Audit hành vi hiện tại

> **Recovery continuation v0.101 (2026-08-09):** the durable print event store now returns one latest valid event per job. A fail-closed classifier labels non-terminal tails as queue reconciliation, explicit operator decision or event-log repair; terminal jobs are excluded and automatic retry is never allowed. MainWindow surfaces a read-only warning/review dialog after startup. Current regression evidence is 69 application checks and 93 xUnit tests; explicit queue re-query/reprint lineage and physical printer verification remain open.

> **Queue re-query continuation v0.102 (2026-08-09):** a recovery candidate with a captured printer/job identity can be queried through the bounded status reader. The result is appended as QueueObserved evidence, while timeout, identity mismatch and terminal queue states remain operator-decision states. MainWindow offers the query action but never retries, marks physical output or changes row-level printed truth. Current regression evidence is 69 application checks and 93 xUnit tests; explicit acknowledge/void/reprint lineage and physical printer verification remain open.

> **Operator-lineage continuation v0.103 (2026-08-09):** `PrintJobOperatorActionService` now appends explicit acknowledge, void and reprint-request events to the same hash-chained store. Void moves only the durable state to `Cancelled`; a reprint request creates a linked `Created` child and never prepares or dispatches it. MainWindow offers the decisions after queue evidence is exhausted, and the machine log carries actor/related-job fields. Legacy pre-lineage hashes remain readable; provider faults during spooler restart fail closed as `Unknown`. Current regression evidence is 69 application checks and 100 xUnit tests; child approval/dispatch, hot-unplug fixtures, platform-neutral resource parity and physical printer verification remain open.

> **Queue-safety continuation v0.104 (2026-08-09):** direct print calls with a blank queue now return a failed evidence result and explicitly refuse Windows-default substitution. Queue/driver failures are surfaced as actionable errors, while the interactive dialog still requires a human queue choice. Current regression evidence is 70 application checks and 100 xUnit tests; a real missing-named-queue resolver fixture, hot-unplug evidence and physical printer verification remain open.

### Implementation checkpoint v0.110 (2026-08-10)

> **Reprint approval/dispatch guard continuation v0.110 (2026-08-10):** linked child jobs can now record a same-state `ReprintApproved` event only when the exact captured `PrintJobManifest` is presented. The MainViewModel dispatch API accepts an explicit current row set, rebuilds the current template/data manifest and blocks before `Preparing` on any path, queue, DPI, design hash, count or row-digest mismatch. The API never auto-dispatches after approval; the recovery dialog can open Print Preview with the approved child so the operator selects the source subset explicitly. Regression evidence is 72 application checks and 112 xUnit tests.

Open after v0.110: evolve the recovery dialog into a dedicated Print Center/recovery workspace, then complete spooler-restart/hot-unplug fixtures, platform glyph/fallback observations, long-soak memory evidence and physical printer verification.

### Implementation checkpoint v0.111 (2026-08-10)

> **Print Center/recovery workspace continuation v0.111 (2026-08-10):** `PrintCenterWindow` consolidates recovery candidates, durable lifecycle state, queue/spool evidence and manifest validity into one operator surface. Reconcile, acknowledge, void, linked reprint request, manifest approval and guarded preview are explicit actions; there is no automatic retry or dispatch. Opening an approved preview preserves the child job ID and exact manifest so any changed template, queue, DPI or source-row set is rejected before preparation. Regression evidence is a Release build with 0 warnings/errors, 72 application checks, 112 xUnit tests and a responsive app smoke run.

Open after v0.111: add keyboard/scanner workflow evidence for IR-207, spooler-restart/hot-unplug fixtures, platform glyph/fallback observations, long-soak memory evidence and physical printer verification.

### Implementation checkpoint v0.112 (2026-08-10)

> **Scanner/keyboard Print Center continuation v0.112 (2026-08-10):** `PrintRecoveryCandidateFilter` provides an order-preserving, pure search contract. Exact durable job IDs win over partial matches across printer, spool, queue and manifest fields. The window focuses the scan field on load, supports F5 refresh, Escape focus reset and Enter selection; only explicit Ctrl+Enter can open a manifest-guarded approved preview, and no keyboard action dispatches, retries or voids a job. Regression evidence is a Release build with 0 warnings/errors, 72 application checks and 115 xUnit tests.

Open after v0.112: verify the workflow with a real keyboard/scanner station, then complete spooler-restart/hot-unplug fixtures, platform glyph/fallback observations, long-soak memory evidence and physical printer verification.

### Implementation checkpoint v0.113 (2026-08-10)

> **Bounded text-frame continuation v0.113 (2026-08-10):** `ObjectStyle.TextSizing` persists `AutoFit` (legacy-compatible measured static text) versus `FixedFrame` (authored bounds are authoritative). The fixed-frame path shares the same layout contract across designer, preview and print, clips glyphs to the text object and reports overflow during preflight instead of allowing the outer object/visual to drift. Clone and immutable scene identity capture the mode, with regression evidence of 73 application checks and 115 xUnit tests on a zero-warning Release build.

Open after v0.113: verify mixed-font/RTL/glyph fallback on representative installed-font sets, add install/remove-font and long-soak memory evidence, and complete physical printer/spooler verification.

### Implementation checkpoint v0.114 (2026-08-10)

> **Industrial fault-contract continuation v0.114 (2026-08-10):** `SpoolJobMonitoringTests` now covers terminal offline, paper/media unavailable and operator-intervention observations, plus the existing reader-fault path used for spooler restart/hot-unplug simulation. Every result remains queue evidence only (`PhysicalOutputVerified=false`) and the monitor stops at explicit operator review; no fixture authorizes automatic retry. Regression evidence is a zero-warning Release build with 73 application checks and 118 xUnit tests. These are injectable fault fixtures, not physical-device certification.

Open after v0.114: run the matrix against real USB/network disconnect and spooler restart, then complete mixed-font/RTL/glyph fallback, long-soak memory and physical printer verification.

### Implementation checkpoint v0.115 (2026-08-10)

> **Font/glyph coverage continuation v0.115 (2026-08-10):** `TextFontObservation` records the requested and resolved family plus glyph-map coverage for each row value. If the requested family is installed but lacks a code point, preflight emits a blocking row diagnostic with a bounded `U+....` summary; this prevents silent OS fallback from changing the visible/printed symbol. Missing-family behavior remains the existing deterministic fallback diagnostic. Regression evidence is a zero-warning Release build with 74 application checks and 118 xUnit tests.

Open after v0.115: collect representative installed-font observations for Latin/RTL/CJK/emoji policy, add install/remove-font fixtures, long-soak memory evidence and physical printer verification.

### Implementation checkpoint v0.116 (2026-08-10)

> **Effective output-contract continuation v0.116 (2026-08-10):** Print Preview now resolves the selected queue's effective `PrintTicket` before it creates the immutable manifest. Preflight consumes the effective DPI; reported driver media coercion is rejected; the manifest records the output-contract fingerprint; and dispatch compares that fingerprint again immediately before `PrintDocument`. A changed or missing prepared hash fails closed, while direct callers without a prepared expectation retain the explicit legacy path. Regression evidence is a zero-warning Release build with 74 application checks and 125 xUnit tests.

Open after v0.116: exercise driver media/DPI coercion on representative queues, real USB/network disconnect and spooler restart, then complete installed-font matrix, long-soak memory and physical-printer verification.

### Implementation checkpoint v0.117 (2026-08-10)

> **International font-policy continuation v0.117 (2026-08-10):** font availability/fallback resolution now accepts an injectable family catalog, matches names case-insensitively and returns a stable installed spelling. The application regression matrix exercises install/remove behavior plus combining marks, Hebrew RTL, CJK, emoji surrogate pairs, an unassigned scalar and grapheme-safe segmentation. This is deterministic policy evidence, not proof that every target workstation has the same fonts or glyph metrics. Regression evidence is a zero-warning Release build with 75 application checks and 125 xUnit tests.

Open after v0.117: collect observations on representative installed font sets and real printer output, then complete OS-level install/remove fixtures, long-soak memory and physical-printer verification.

### Implementation checkpoint v0.118 (2026-08-10)

> **Preview safety/long-soak continuation v0.118 (2026-08-10):** `PreviewRasterizer` rejects non-finite, non-positive or unsafe raster dimensions before queueing a request. A 300-cycle navigation soak reuses one STA worker, records cancellation, leaves no pending request and measures private-memory growth; the observed run completed 282 renders, canceled 1 and measured a 0.0 MB private-memory delta. Combined with the existing 10k burst/cache regressions, this is local software evidence for bounded preview behavior, not physical-printer certification or proof on the 4 GB baseline workstation. Regression evidence is a zero-warning Release build with 75 application checks and 125 xUnit tests.

Open after v0.118: repeat the soak on the baseline 4 GB machine with 203/300/600-DPI label profiles and real page-navigation traces; finish OS font install/remove, driver PrintTicket/media coercion, spooler restart/hot-unplug and physical-printer verification.

### Implementation checkpoint v0.119 (2026-08-10)

> **Viewport/queue continuation v0.119 (2026-08-10):** the horizontal and vertical rulers now mirror the artboard `DesignerScrollViewer` offsets after scroll, zoom extent changes and canvas resize through one guarded synchronization method. `SpoolJobMonitor` also gives an already-completed provider task priority over its deadline task, preventing a synchronous thermal-driver adapter from dropping the final terminal observation under scheduler pressure. Regression evidence is a zero-warning Release build with 75 application checks and 125 xUnit tests; five consecutive full xUnit runs passed. The ruler drift gate still needs measured display-scale evidence, and queue observations remain non-physical evidence.

Open after v0.119: measure ruler/artboard/overlay drift at 25/100/400% zoom and representative DPI scales; repeat preview soak on the baseline workstation, then finish installed-font, PrintTicket/media, spooler restart/hot-unplug and physical-printer verification.

### Implementation checkpoint v0.120 (2026-08-10)

> **Raster/image continuation v0.120 (2026-08-10):** `ImageResolutionContract` derives effective X/Y PPI from decoded pixel dimensions and the physical object frame. `PrintPreflightValidator` now fails closed for absent or malformed embedded bitmaps, encoded payloads above 64 MB and decoded images above 64 megapixels, then blocks any source axis below the effective printer grid. This avoids silent interpolation on industrial labels and bounds decompression-bomb memory risk. Application fixtures cover corrupt, undersampled and adequate images; Core xUnit fixtures cover the PPI contract and device-grid decision. Regression evidence is a zero-warning Release build with 78 application checks and 130 xUnit tests; app smoke is responsive at title v0.120.

Open after v0.120: validate alpha/color-profile/1-bpp conversion and renderer byte parity on real Zebra/TSC/Godex devices; measure ruler/artboard/overlay drift at 25/100/400% and multiple display scales; repeat preview soak on the 4 GB workstation; and close driver media/DPI coercion, spooler restart/hot-unplug and physical-output verification.

### Implementation checkpoint v0.121 (2026-08-10)

> **Dot-grid barcode continuation v0.121 (2026-08-10):** `DeviceBarcodeLayout` owns the immutable printer-dot geometry for vector barcode dark runs. Effective X/Y DPI are applied independently to frame edges and module boundaries; collapsed/out-of-frame runs are clamped before WPF painting. `LabelVisualRenderer` consumes that layout rather than performing a second DIP rounding algorithm. Unit fixtures cover 203/300/305/600/609 plus 305×609 non-square DPI, and an application fixture proves renderer geometry parity. Regression evidence is a zero-warning Release build with 79 application checks and 137 xUnit tests; app smoke is responsive at title v0.121.

Open after v0.121: barcode application profile/quiet-zone/HRI/GS1 verifier results, real thermal raster parity, image 1-bpp/dither/color-profile parity, display-scale ruler measurements, baseline-workstation soak, driver coercion, spooler restart/hot-unplug and physical-output verification.

### Implementation checkpoint v0.122 (2026-08-10)

> **Application-profile barcode continuation v0.122 (2026-08-10):** `BarcodeApplicationProfile` is an explicit persisted policy (`General`, `Industrial`, `Gs1`) carried through clone, immutable scene snapshot/hash, designer controls and preview/print renderer options. Industrial/GS1 profiles fail preflight when the configured quiet zone is below the policy threshold (10 modules for linear symbols; 4 QR / 1 Data Matrix for GS1) or a requested HRI font falls outside 5–20 pt. GS1 accepts the unambiguous `(AI)value` authoring form, validates common GTIN/SSCC check digits and date/variable-field boundaries, and normalizes variable fields to ASCII GS separators before ZXing `GS1_FORMAT`/FNC1 encoding. Regression evidence is a zero-warning Release build with 80 application checks and 143 xUnit tests. This remains software preflight evidence, not a verifier grade: complete AI registry, HRI text sub-layout, fixed-version encoder capacity, device-raster parity and physical Zebra/TSC/Godex runs remain open.

### Implementation checkpoint v0.123 (2026-08-10)

> **Measured HRI sub-layout continuation v0.123 (2026-08-10):** linear-barcode HRI is now represented by the platform-neutral `BarcodeHriLayoutContract` and measured by one WPF adapter shared by designer, preview, print and preflight. The contract reserves a deterministic symbol strip, gap and HRI strip; text width/font/frame failures are blocking diagnostics, and the renderer clips only to that validated strip. QR/Data Matrix leave HRI disabled and preserve their full symbol frame. Regression evidence is a zero-warning Release build with 81 application checks and 148 xUnit tests; app smoke is responsive at title v0.123. This closes the software layout-consistency gate, not verifier grade, driver raster parity or physical-device certification.

Open after v0.123: complete GS1 AI registry/FNC1 edge vectors and fixed-version encoder capacity checks, compare raster/vector output on actual Zebra/TSC/Godex devices, complete image alpha/color-profile/1-bpp/dither parity, and retain the driver/spooler/physical-output gates from v0.120.

### Implementation checkpoint v0.124 (2026-08-10)

> **Text-anchor precision continuation v0.124 (2026-08-10):** the designer snap engine now adds first-baseline anchors for Text/TextBox objects in single and group moves. Baselines are measured through the shared WPF layout metrics and use the same 6/10-DIP acquire/release hysteresis as object edges and centers; bypassing with Alt explicitly releases the target lock. `TextAlignmentMode.Justify` is persisted and bounded through the same designer/preview/print/preflight text path, while legacy left-aligned auto-fit text remains intentionally unbounded. Regression evidence is a zero-warning Release build with 82 application checks and 148 xUnit tests; app smoke is responsive at title v0.124.

Open after v0.124: platform-neutral glyph metrics and optical alignment, smart-spacing/grid/guide contracts, complete GS1 AI registry/FNC1 edge vectors and fixed-version capacity, driver raster parity and physical-device certification.

### Implementation checkpoint v0.125 (2026-08-10)

> **Grid and equal-spacing continuation v0.125 (2026-08-10):** `SnapGridContract` now owns the physical millimetre step used by both visible grid lines and pointer candidates. The step is bounded to 0.25–20 mm, persisted independently of a template, selectable from the canvas context menu and rounded deterministically; Alt bypasses the acquisition path without changing authored geometry. `SmartSpacingContract` evaluates sorted visible/non-moving intervals in O(n log n), keeps the furthest trailing edge of an overlap run so nested objects cannot create a false gap, and returns before/after placements that preserve the measured gap. Single and group moves use the same spacing candidates; resize-edge snap uses the same grid contract, with semantic edge/center/baseline anchors ranked above grid and spacing. Regression evidence is a zero-warning Release build with 83 application checks and 156 xUnit tests; app smoke is responsive at title v0.125. This closes a deterministic software slice only; visual guide annotations, platform-neutral glyph metrics/optical alignment, barcode verifier grade, thermal raster parity and physical-device certification remain open.

Open after v0.125: expose measured spacing/grid candidates as non-overlapping guide labels, add optical alignment and platform-neutral glyph metrics for mixed fonts/RTL/combining text, complete GS1 AI/FNC1 and fixed-version capacity vectors, and collect effective-ticket/raster/verifier evidence on representative Zebra/TSC/Godex hardware.

### Implementation checkpoint v0.126 (2026-08-10)

> **Guide explanation and explicit optical-ink continuation v0.126 (2026-08-10):** grid snap is now evaluated independently from object/baseline/spacing snap, so a user can use a physical grid without silently re-enabling object matches. The transient guide overlay carries the same candidate explanation used by the snap selector: axis position, `grid … mm`, `baseline`, `center`, `edge` or the measured `gap … mm`; the overlay is discarded on release, bypass or rebuild and is not part of the printable scene. `OpticalAlignmentContract` is a WPF-free translation contract over visible-ink bounds, while `TextBoxOverflowDetector.GetInkBoundsDip` uses the current WPF `BuildGeometry` adapter for normal and explicit-line-height text. The Properties panel exposes a one-shot, undo-bracketed “Optical ink center” action; frame and baseline alignment remain the safe defaults and no persisted template geometry changes unless the user invokes the command. Regression evidence is a zero-warning Release build with 84 application checks and 160 xUnit tests; app smoke is responsive at title v0.126. This is an explicit software/adapter slice, not platform-neutral glyph-shaping or physical-label evidence.

Open after v0.126: persist author-created guides as design metadata, produce a stable platform-neutral glyph/ink metrics hash consumed by preview/print/preflight, add mixed-font/italic/diacritic optical golden fixtures, complete GS1 AI/FNC1 and fixed-version capacity vectors, and collect effective-ticket/raster/verifier evidence on representative Zebra/TSC/Godex hardware.

### Implementation checkpoint v0.128 (2026-08-10)

> **Persistent guide authoring continuation v0.128:** guides are now persisted as authoring metadata with stable IDs, vertical/horizontal orientation, physical-mm position, lock and visibility state. Ruler drags use one cancelable edit gesture; the canvas overlay is reconciled by ID, and the design-guide context menu supports add, lock/unlock, delete and clear. Guides are excluded from printable scene compilation and from `SceneHash`, while `DocumentHash` changes so save/history remains truthful. Regression evidence is a zero-warning Release build with 85 application checks and 164 xUnit tests; app smoke is responsive at title v0.128. This closes the software guide contract, not ruler pixel measurements, glyph shaping parity, or physical printer/verifier evidence.

Open after v0.128: platform-neutral glyph/ink metrics identity, transformed/group/resize and keyboard interaction contracts, GS1 fixed-version/verifier fixtures, effective driver ticket and imageable bounds, raster/font/image/stock fingerprints, and representative physical-device evidence.

### Implementation checkpoint v0.129 (2026-08-10)

> **Keyboard precision continuation v0.129:** the canvas now applies a pure physical nudge contract (`0.1 mm` standard, `1 mm` Shift/coarse, `0.01 mm` Alt/fine) without pointer snap. Repeated arrow keys share one cancelable history gesture; Escape or focus loss restores the exact pre-nudge geometry, locked selection fails closed, and routed TextBox/ComboBox editor focus is protected. Regression evidence is a zero-warning Release build with 86 application checks and 168 xUnit tests; app smoke is responsive at title v0.129. This closes the software keyboard-precision slice, not monitor-scale measurement, glyph shaping parity, or physical printer evidence.

Open after v0.129: platform-neutral glyph/ink metrics identity, transformed/group/resize snap parity, mixed-script optical golden fixtures, GS1 fixed-version/verifier vectors, effective driver ticket/imageable bounds, raster/font/image/stock fingerprints, and representative physical-device evidence.

### Implementation checkpoint v0.130 (2026-08-10)

> **Shared transformed-bounds continuation v0.130:** `TransformedBoundsContract` centralizes document-space rectangle bounds for 0°/90°/180°/270° rotations around the authored frame center. `LabelArrangeEngine` and canvas snap target/aggregate paths now consume it, so rotated peers participate consistently in single/group alignment, spacing and resize-target candidates. Rotated source-handle resize mapping remains explicitly open. Regression evidence is a zero-warning Release build with 86 application checks and 174 xUnit tests; app smoke is responsive at title v0.130. This closes one deterministic geometry seam, not full transformed resize UX or physical printer evidence.

Open after v0.130: rotated source-edge/group-resize mapping, platform-neutral glyph/ink metrics identity, mixed-script optical fixtures, GS1 fixed-version/verifier vectors, effective driver ticket/imageable bounds, raster/font/image/stock fingerprints, and representative physical-device evidence.

### Implementation checkpoint v0.131 (2026-08-10)

> **Rotated resize-edge continuation v0.131:** `ResizeGeometryContract` maps authored edges to world edges for cardinal rotations and solves the local dimension against the snapped world target while preserving the opposite edge. Canvas resize now uses the same contract for both axes and routes transformed snap guides to the correct X/Y overlay. Regression evidence is a zero-warning Release build with 86 application checks and 182 xUnit tests; app smoke is responsive at title v0.131. This closes rotated single-object edge mapping, not multi-selection/group resize UX or physical printer evidence.

Open after v0.131: group-resize UX/invariants, platform-neutral glyph/ink metrics identity, mixed-script optical fixtures, GS1 fixed-version/verifier vectors, effective driver ticket/imageable bounds, raster/font/image/stock fingerprints, and representative physical-device evidence.

### Implementation checkpoint v0.132 (2026-08-10)

> **Multi-selection resize continuation v0.132:** `SelectionResizeAdorner` can now be hosted on the canvas with a bounds provider, so a multi-selection has one visible eight-handle hull instead of silently losing resize affordances. `GroupResizeGeometryContract` keeps the opposite hull edge fixed, enforces a physical minimum, clamps to the artboard and maps each member through display-space bounds; cardinally rotated objects swap authored dimensions back after scaling, while line endpoints are mapped directly. Group resize snapping reuses semantic object/grid priorities, hysteresis and transient guides, excluding selected members from candidates. Frame/key/canvas arrange, distribution and text-baseline commands now use one explicit undo transaction and cancel cleanly when no geometry changes. Release build and app checks remain clean at 86/86; xUnit is 190/190 and the v0.132 smoke window is responsive. This closes the software group-resize contract slice, not pointer telemetry, renderer metric identity or physical printer evidence.

Open after v0.132: pointer-frame/undo invariants for group handles, platform-neutral glyph/ink metrics identity, mixed-script optical fixtures, GS1 fixed-version/verifier vectors, effective driver ticket/imageable bounds, raster/font/image/stock fingerprints, and representative physical-device evidence.

### Implementation checkpoint v0.133 (2026-08-10)

> **Text-layout identity continuation v0.133:** `TextLayoutIdentityContract` creates a stable value-only fingerprint from normalized text, resource/style fingerprint, resolved RTL/LTR direction, frame and DIP scale, line count/height, baseline, ink extent, vertical offset and overflow. `TextBoxOverflowDetector` attaches it to both explicit line-height and regular `FormattedText` measurements, so designer baseline/optical alignment, preview, print and preflight share one measurable path. `FormattedText` construction is now locale-invariant. Mixed RTL/combining/emoji/multiline and line-height-change fixtures pass; app checks are 87/87, xUnit 192/192, and the v0.133 smoke window is responsive. This is an identity/evidence gate, not a claim that different installed fonts or native shaping engines produce identical glyphs.

Open after v0.133: approved cross-machine font/glyph/ink parity, text overflow golden fixtures, GS1 fixed-version/verifier vectors, effective driver ticket/imageable bounds, raster/font/image/stock fingerprints, and representative physical-device evidence.

### Implementation checkpoint v0.134 (2026-08-10)

> **Effective-ticket guard continuation v0.134:** prepared dispatch now calls `PrintContractGuard.Matches(expectedHash, actualHash, actualTicketVerified)`. When a preview/reprint supplies an expected output contract, an empty ticket XML hash or `OutputContractTicketVerified=false` fails closed even if a synthetic output-contract fingerprint happens to match; direct legacy calls with no expected hash remain unchanged. The check runs after the queue is re-merged immediately before scene/preflight/paginator work. Regression evidence is 87/87 app checks, 193/193 xUnit, zero-warning Release build and responsive v0.134 smoke. This prevents an unverified driver contract from authorizing a prepared job, but does not certify imageable-area, spooler or physical-label behavior.

Open after v0.134: effective resolution/imageable-area evidence, queue/spooler fault fixtures, barcode/font/image/media fingerprints, durable recovery and representative physical-device evidence.

### Implementation checkpoint v0.135 (2026-08-10)

> **Spool identity continuation v0.135:** `PrintJobResult.HasSpoolIdentity` is now an explicit evidence boundary. A spool-accepted submission with no positive job ID remains accepted, but its user-facing status says that queue correlation is unavailable and that automatic retry is unsafe until the operator reconciles the queue/device. The Print Preview duplicate-label indicator is stricter: rows become `IsPrinted` only after a device-confirmed terminal outcome; spool acceptance and queue observation never claim physical completion. Regression evidence is 88/88 app checks, 193/193 xUnit, zero-warning Release build and responsive v0.135 smoke. This improves operator truthfulness without claiming physical output or silently converting queue evidence into device evidence.

Open after v0.135: effective resolution/imageable-area evidence, queue/spooler fault fixtures with real driver adapters, barcode/font/image/media fingerprints, durable recovery and representative physical-device evidence.

### Implementation checkpoint v0.136 (2026-08-10)

> **Imageable-area/DPI continuation v0.136:** `PrintableAreaContract` now treats a driver imageable rectangle as evidence only when its origin/extent are finite and positive and its right/bottom edges fit inside the effective media (with a bounded 1 DIP conversion tolerance). Invalid capability geometry fails before prepared dispatch; missing `PageImageableArea` remains explicitly unverified rather than being replaced by guessed margins. `EffectiveDpiContract` also rejects non-positive or unsupported resolutions while preserving non-square 305×609 behavior. Regression evidence is 88/88 app checks, 206/206 xUnit, zero-warning Release build and responsive v0.136 smoke. This closes the software validation seam, not the real driver/stock/verifier gate.

Open after v0.136: real-driver imageable-area/PrintCapabilities fixtures, queue/spooler fault adapters, barcode/font/image/media fingerprints, durable recovery and representative physical-device evidence.

### Implementation checkpoint v0.137 (2026-08-10)

> **Image raster/identity continuation v0.137:** the image lane now has one explicit, versioned contract. Every embedded image records its payload fingerprint, decoder-observed pixel dimensions and a persisted raster mode: `DriverManaged` preserves colour/alpha for the driver, while threshold and 4×4 ordered dither apply the same white-alpha compositing and BT.709 luma transform in the designer, preview and print presenter. Preflight uses that same decoder/transform, rejects unsupported policy, corrupt/oversized payloads and stale stored dimensions, and still enforces effective source PPI against the resolved X/Y printer grid. The aggregate image identity flows through immutable scene snapshots, `PrintRenderPlan`, manifests, results and operation logs; legacy templates are hydrated only when dimensions are absent, never when a non-zero value conflicts. Regression evidence is 89/89 app checks, 209/209 xUnit, zero-warning Release build and deterministic byte/mode/fault fixtures.

This closes the software transform/identity seam but does **not** claim thermal-driver parity, colour-profile fidelity, ISO verifier grade, stock/ribbon calibration or physical output. The next gate is a model/driver/firmware matrix with captured PrintTicket/capabilities, application-vs-driver raster bytes, scanner/verifier result and operator evidence.

### Implementation checkpoint v0.138 (2026-08-10)

> **Canvas identity/refresh continuation v0.138:** zoom is now a viewport-only refresh that updates existing object hosts, persistent guides, alignment guides and selection adorners in place. Observable collection `Add`/`Remove`/`Replace`/`Move` events reconcile only the affected visual; stable IDs preserve the selected key object across replacement, while Reset keeps the explicit rebuild fallback. The regression fixture proves host-reference stability through zoom, add/remove and stable-ID replacement. Evidence is 90/90 application checks, 209/209 xUnit and a zero-warning Release build with responsive v0.138 smoke.

This closes the software selection/host identity seam but does **not** close display-scale visual measurement, platform-neutral glyph shaping, driver raster/imageable-area, verifier, queue/spooler or physical-device gates.

### Implementation checkpoint v0.139 (2026-08-10)

> **Text frame/padding continuation v0.139:** `ObjectStyle.TextPaddingMm` is now an explicit persisted physical contract, clamped to 0–20 mm. `TextBoxOverflowDetector` converts the value once at the shared layout boundary and feeds the same padded content rectangle to wrapping, WPF `MaxTextWidth`/`MaxTextHeight`, horizontal origin, vertical alignment and overflow diagnostics. Designer, preview, print, preflight, optical bounds and baseline calculations consume that seam; scene snapshots/hashes and cloning retain it, and the Text Style card exposes the value. Zero padding preserves the legacy 2-DIP static-text inset and zero TextBox inset.

Evidence is 91/91 application checks, 209/209 xUnit, a zero-warning Release build and responsive v0.139 smoke. This closes the hidden-padding drift seam but does **not** claim per-edge padding, platform-neutral glyph shaping, display-scale measurement, thermal-driver raster parity, verifier grade, queue/spooler or physical-device certification.

### Implementation checkpoint v0.140 (2026-08-10)

> **Edge-aware text-frame continuation v0.140:** the persisted text frame now carries independent left/right/top/bottom padding in physical millimetres. The uniform `TextPaddingMm` shorthand sets all four edges for legacy authoring; a non-uniform edit projects the shorthand to zero while preserving the edge values. The shared WPF layout seam converts each edge to DIP and feeds the same asymmetric content rectangle to wrapping, `MaxTextWidth`/`MaxTextHeight`, horizontal origin, vertical alignment, optical alignment, baseline calculations and preflight. Cloning, immutable snapshots, scene hashes and the Text Style card retain/expose all edges.

Evidence is 91/91 application checks, 209/209 xUnit, a zero-warning Release build and responsive v0.140 smoke. This closes asymmetric hidden-padding drift but does **not** claim explicit overflow modes, platform-neutral glyph shaping, display-scale measurement, thermal-driver raster parity, verifier grade, queue/spooler or physical-device certification.

### Implementation checkpoint v0.141 (2026-08-10)

> **Effective preview/print contract continuation v0.141:** preview page creation and frozen drawing snapshots now accept the already-resolved `PrintRenderPlan`. The asynchronous Print Preview path resolves/caches the selected queue's effective PrintTicket plan, validates rows against that same plan, and passes it into page metadata and rasterization. Preview metadata retains output-contract hash, ticket evidence, effective DPI and imageable-area verification. If queue/ticket validation is unavailable, preview remains usable as design-only but a blocking preflight issue is recorded so the operator cannot mistake a design-only preview for production-ready output.

Evidence is 91/91 application checks, 209/209 xUnit, a zero-warning Release build and responsive v0.141 smoke; preview-page/drawing fixtures prove effective-plan identity and DPI propagation. This closes the preview-vs-dispatch plan drift seam but does **not** claim explicit overflow modes, platform-neutral glyph shaping, display-scale measurement, thermal-driver raster parity, verifier grade, queue/spooler or physical-device certification.

### Implementation checkpoint v0.142 (2026-08-10)

> **Horizontal fixed-frame overflow continuation v0.142:** the shared text detector now measures the natural width of every complete wrapped line before applying the bounded WPF width. Grapheme-safe wrapping deliberately keeps an indivisible grapheme intact; when that grapheme is wider than the content frame, the layout is marked overflowing even if there is enough vertical room. The same 0.2 DIP tolerance and result flow through designer diagnostics, preview/print layout and production preflight, while empty lines do not create false width errors.

Evidence is 91/91 application checks, 209/209 xUnit and a zero-warning Release build; the fixed-frame regression covers both a regular long value and a single wide grapheme. This closes a silent horizontal clipping seam but does **not** claim automatic shrink/remediation modes, platform-neutral glyph shaping, display-scale measurement, thermal-driver raster parity, verifier grade, queue/spooler or physical-device certification.

### Implementation checkpoint v0.143 (2026-08-10)

> **Zoom-normalized snap continuation v0.143:** `SnapToleranceContract` owns the screen-DIP to document-mm conversion for acquire/release tolerance. Zoom is bounded to 0.25–4.0 (non-finite input uses 1.0), and `SnapCandidateSelector` accepts a candidate exactly on the acquire boundary. Canvas single-object, group and resize paths consume the same contract while retaining semantic priority and hysteresis.

Evidence is 91/91 application checks, 210/210 xUnit and a zero-warning Release build. The contract regression covers 25%, 100%, 400%, invalid zoom/tolerance and the inclusive boundary. This closes a tolerance-consistency/dead-zone seam but does **not** claim display-scale visual measurement, platform-neutral glyph shaping, thermal-driver raster parity, verifier grade, queue/spooler or physical-device certification.

### Implementation checkpoint v0.144 (2026-08-10)

> **Compiled text-frame parity and explicit overflow policy v0.144:** the compiled scene presenter now hydrates TextSizing, TextOverflow and every persisted physical padding edge into the WPF render object. `TextOverflowMode.Error` remains the safe default; `Clip` and `AllowOverflow` are explicit author choices preserved through JSON, cloning, scene hash and the designer/preview/print/preflight path. Error mode blocks and paints a diagnostic frame, Clip keeps the bounded clip without blocking, and AllowOverflow leaves the object frame while retaining the design-label bounds check.

Evidence is 92/92 application checks, 210/210 xUnit and a zero-warning Release build; a rendered compiled-scene fixture catches the prior policy-loss regression, while save/load, clone and scene-hash fixtures cover the new persisted field. This closes the silent snapshot-presenter drift seam but does **not** claim ellipsis/shrink/scale remediation, display-scale measurement, platform-neutral glyph shaping, thermal-driver raster parity, verifier grade, queue/spooler or physical-device certification.

### Implementation checkpoint v0.145 (2026-08-10)

> **Last-mile dispatch contract revalidation v0.145:** after preflight, `PrintService` re-reads the selected queue's effective `PrintTicket`, capabilities and imageable-area contract immediately before `PrintDocument`. `PrintContractGuard.MatchesDispatchSnapshot` requires stable document identity, output-contract fingerprint and ticket-evidence state between preparation and dispatch; failures stop before spool submission and explicitly state that no label was submitted. Calibration uses the same guard.

Evidence is 92/92 application checks, 211/211 xUnit and a zero-warning Release build. The regression matrix covers unchanged snapshots, design/output/evidence drift and missing fingerprints. This closes the software time-of-check/time-of-use seam but does **not** claim queue hot-unplug recovery, driver-specific raster parity, verifier grade or physical-device certification.

The v0.145 research-to-plan bridge is recorded in [07-execution-plan.md](07-execution-plan.md#latest-audit-addendum-v0145--verification-work-not-a-hardware-claim) as DP-129..131 and IR-131..134. It turns the latest review into explicit acceptance work for snap-path parity, selection/undo transactions, bound-text WYSIWYG, queue fault/reconciliation, device-dot/raster parity and crash-safe persistence/async orchestration; hardware and verifier gates remain separate.

### Implementation checkpoint v0.146 (2026-08-10)

> **Rotated group-move transformed hull parity v0.146:** group drag now uses `TransformedBoundsContract` for rotated members when calculating the selection hull. Snap candidates, canvas clamping and guide positions consequently use the same document-space bounds as rendered selection/arrange/resize paths instead of authored width/height axes.

Evidence is 93/93 application checks, 211/211 xUnit and a zero-warning Release build; the STA fixture verifies a 20×6 mm object rotated 90° contributes a 6×20 mm hull and that the peer bounds are retained. This closes one software geometry inconsistency but does **not** claim pointer telemetry, display-scale overlay accuracy, driver raster parity, verifier grade or physical-device certification.

### Implementation checkpoint v0.147 (2026-08-10)

> **Rotated-object preflight transformed-hull parity v0.147:** `PrintPreflightValidator` now uses `TransformedBoundsContract` for non-line object bounds, matching the rendered 90°/270° hull used by designer, preview and print. A rectangle can no longer pass the authored-frame check while its rotated visual hull crosses the design-label edge.

Evidence is 93/93 application checks, 211/211 xUnit and a zero-warning Release build; the regression fixture covers an authored-in-bounds rectangle whose 90° hull is outside the label and confirms preflight blocks it. This closes one software preflight geometry inconsistency but does **not** claim pointer telemetry, driver raster parity, verifier grade or physical-device certification.

### Implementation checkpoint v0.148 (2026-08-10)

> **Line stroke bounds parity v0.148:** `LineBoundsContract` centralizes line endpoint and half-stroke safety bounds. Designer group geometry, arrange, compiled-scene visual anchors and print preflight now agree on the visible line hull, while the authored endpoint rectangle remains the layout frame.

Evidence is 94/94 application checks, 214/214 xUnit and a zero-warning Release build. The fixture covers a 0.4 mm line whose endpoint frame reaches 29.9 mm on a 30 mm label and confirms both designer and preflight observe a 30.1 mm right edge. This closes one software stroke-geometry inconsistency but does **not** claim pointer telemetry, driver raster parity, verifier grade or physical-device certification.

### Implementation checkpoint v0.149 (2026-08-10)

> **Quick-print effective-contract ordering v0.149:** tracked quick print now resolves the saved named queue and its effective PrintTicket before creating the manifest or writing `Created → Preparing → PreflightPassed`. Scene compilation and row preflight run against that same effective plan, and the final dispatch uses the explicit queue plus the prepared output-contract hash. Approved reprints rebuild this physical contract before comparing manifests, so a design-only identity cannot approve a different stock, DPI or driver configuration.

Evidence is 95/95 application checks, 214/214 xUnit and a zero-warning Release build. The missing named-queue fixture proves the quick-print path stops before any durable preparation transition and keeps the actionable “queue no longer installed/default queue disabled” diagnostic. This closes the software quick-print identity/order seam but does **not** claim queue hot-unplug recovery, driver-specific raster parity, verifier grade or physical-device certification.

### Implementation checkpoint v0.150 (2026-08-10)

> **Explicit ellipsis remediation v0.150:** bounded Text/TextBox objects may now select `TextOverflowMode.Ellipsis`. The designer/preview/print one-pass WPF path enables character ellipsis inside the authored frame; explicit line-height layouts use grapheme-safe truncation, physical line-count limits and a final visible `…`. Preflight accepts the policy only because it is explicit; the default `Error` policy remains blocking.

Evidence is 96/96 application checks, 214/214 xUnit and a zero-warning Release build. This closes the missing ellipsis-policy seam but does **not** claim platform-neutral glyph shaping, display-scale measurements, thermal-driver raster parity, verifier grade or physical-device certification.

### Implementation checkpoint v0.151 (2026-08-10)

> **Bounded ShrinkFont and snap evidence v0.151:** `TextSizingMode.ShrinkFont` keeps the authored Text/TextBox frame fixed and resolves a deterministic effective font size by bounded binary search, never writing back to the authored `FontSizePt`. The minimum is 4 pt; if the value is still too large, the selected `Error`/`Ellipsis` overflow policy remains authoritative. Designer, preview, print and preflight use the same line-layout path. The WPF natural-line-height adapter now uses measured per-line height when `FormattedText.LineHeight` is zero, preventing false vertical-fit acceptance.

Evidence is 98/98 application checks, 214/214 xUnit and a zero-warning Release build. The snap fixture covers 25–400% zoom, semantic priority and acquire/release hysteresis. This closes a bounded text-fitting and software snap-evidence slice but does **not** claim ScaleWidth raster parity, pointer overlay pixel measurements, platform-neutral glyph shaping, thermal-driver raster parity, verifier grade or physical-device certification.

### Research checkpoint v0.127 (2026-08-10)

> **Precision and industrial reliability research continuation v0.127:** the latest audit is now represented as DP-120..128 and IR-120..130 in the [total execution plan](07-execution-plan.md#industrial-reliability-research-addendum-v0127-2026-08-10). The designer lane covers view-space acquire/release thresholds, transformed/group/resize bounds, stable selection/key identity, deterministic align/distribute transactions, explicit layout/ink/baseline separation, persistent guides, keyboard precision and pointer-frame budgets. The industrial lane covers exact queue identity, evidence-level spool outcomes, effective PrintTicket/imageable bounds, 203/300/305/600/609 and non-square dot geometry, barcode profile/verifier rules, font/image/raster fingerprints, stock/sensor/calibration identity, bounded 10k preview, crash-safe job lineage and redacted support bundles.

This checkpoint only records researched facts, official references and measurable exit criteria. It does not claim physical Zebra/TSC/Godex output, ISO verifier grade, driver-raster parity or a production/commercial certification. Those remain G7/hardware gates and must include exact model, driver/firmware, stock/ribbon, effective DPI, ticket/imageable area, calibration and operator/verifier evidence.

### Research addendum from the latest code audit

The detailed delivery table is in [07-execution-plan.md](07-execution-plan.md#audit-driven-plan-addendum-research-review-v0120). It records the remaining designer and industrial risks as `DP-113..119` and `IR-113..119`: one QR geometry owner, generation-scoped canvas reconciliation, explicit text padding/justify/baseline policy, bounded physical-mm grid/equal-spacing semantics, truthful guide explanations, explicit optical ink alignment, printer-dot barcode quantization, versioned image/1-bpp/dither behavior, truthful spool/physical recovery, atomic persistence plus typed async commands, effective PrintTicket invalidation, and baseline-workstation/verifier evidence. These items are the next plan gates; the current software grid/ink/PPI gates are necessary but do not certify thermal raster parity or a physical label.

### Implementation checkpoint v0.109 (2026-08-10)

> **Print-manifest identity continuation v0.109 (2026-08-10):** `PrintJobManifest` creates an immutable metadata contract for each approved dispatch. It canonicalizes template/path/mode/queue, label dimensions, effective DPI, label/source-row counts and document/text-resource/scene/output-contract hashes; selected row dictionaries are reduced to an order-sensitive `RowsFingerprint` without retaining raw label values. Quick Print and Print Preview carry the manifest fingerprint through preparation, preflight, dispatch, queue observation, recovery candidates, JSONL logs and linked reprint children. The current hash chain accepts pre-manifest v0.108 and older event forms. Regression evidence is 72 application checks and 110 xUnit tests.

Open after v0.109: explicit child-job approval/dispatch must compare the manifest against the current template/data before submission; spooler-restart/hot-unplug fixtures, platform glyph/fallback observations, long-soak memory evidence and physical printer verification remain open.

### Implementation checkpoint v0.108 (2026-08-10)

> **Text-resource identity continuation v0.108 (2026-08-10):** `TextResourceContract` emits a deterministic fingerprint for the requested font family (canonicalized for whitespace/case), weight/style, paragraph direction, line-height and the documented Arial fallback policy. `DocumentSnapshot` carries per-style and aggregate identities; `PrintRenderPlan`, preview pages, print results, durable lifecycle events and JSONL job logs carry the same fingerprint. This is an identity/evidence seam only: installed-font availability, platform-neutral glyph metrics, licensing/embedding and physical output remain separate gates. Regression evidence is 72 application checks and 106 xUnit tests.

Open after v0.108: platform glyph/fallback observations and install/remove-font fixtures, explicit child-job approval/dispatch, spooler-restart/hot-unplug fixtures, long-soak memory evidence and physical printer verification.

### Implementation checkpoint v0.107 (2026-08-10)

> **Copy/paste fidelity continuation v0.107 (2026-08-10):** `LabelObjectCloner` is now a Core utility used by the canvas clipboard path. It copies every persisted object/style/resource field, including `TextDirection`, `VerticalAlignment`, `LineHeightPt`, barcode/HRI options and embedded image data, while assigning a fresh ID. Deep-copy regressions prove that later source-style/resource edits cannot mutate the clone and that bound QR geometry survives the type auto-size hook. Current regression evidence is 72 application checks and 102 xUnit tests; platform-neutral glyph/resource parity and physical printer verification remain open.

### Implementation checkpoint v0.106 (2026-08-09)

> **UI queue-availability continuation v0.106 (2026-08-09):** `MainViewModel.RefreshPrinterQueueStatusAsync` resolves the saved named queue off the WPF dispatcher and applies the result only if the template still references the same queue. Startup, new/open/library load and Printer Setup all refresh the evidence. MainWindow exposes an actionable status-bar warning/tooltip and a confirmation action that reopens Printer Setup; it does not auto-select a default queue. One application regression covers the view-model warning contract. Current regression evidence is 72 application checks and 100 xUnit tests; spooler-restart/hot-unplug evidence, child approval/dispatch and physical printer verification remain open.

> **Named-queue continuation v0.105 (2026-08-09):** `IPrinterQueueLookup` now resolves a saved queue before WPF dialog/dispatch; `WindowsPrinterQueueLookup` verifies the canonical name and an injected missing-queue fixture proves the disappeared-queue path returns `Failed` without default substitution. Current regression evidence is 71 application checks and 100 xUnit tests; live UI availability refresh, hot-unplug evidence and physical printer verification remain open.

> **Checkpoint update v0.099 (2026-08-09):** The rows below are the original audit baseline. The current implementation now has zoom-aware acquire/release tolerance with hysteresis and semantic priority on single/group move and resize-edge snap, selection-frame/key-object/canvas arrange commands, baseline alignment, lazy preview metadata with an 8-page LRU, a virtualized/recycling tracking-row list, explicit commit/cancel history for WPF move/resize/draw gestures, cancelable/progressive async preflight, a bounded frozen-drawing preview raster pipeline with one reusable STA worker and newest-request coalescing, a 10,000-request burst regression proving one pending slot and worker reuse, a companion 10,000-page/8-image bounded-cache stress recording process-memory delta and pre-start cancellation latency, deterministic print-operation log flushing for test/shutdown barriers, best-effort spool job identity capture, a shared device-dot quantizer with square/non-square DPI golden tests, an immutable effective-output-contract fingerprint that flows into the effective render plan and job log, a WPF-free immutable `DocumentSnapshot`/`SceneCompiler` geometry seam with order-stable hashes and typed diagnostics, scene identity carried through preview/effective-print plans and JSONL job logs with invalid-scene fail-closed dispatch, a compiled-scene presenter rendering immutable nodes/snapshot resources with a tested geometry-parity and post-plan-mutation guard, a 10,000-page metadata regression proving no eager bitmap allocation, atomic template save, versioned data-source registry migration, shared designer/print text-bound policy, persisted Auto/LTR/RTL paragraph direction, persisted Auto/fixed line-height with two-line parity/overflow fixtures, grapheme-safe wrapping, a shared `TextLayoutMetrics` record for line-height/ink/baseline/vertical-offset diagnostics, and missing-font preflight/fallback diagnostics. Remaining gaps are platform-neutral text shaping/metrics/resource parity, full command/session extraction, long-soak/peak-RAM and post-cancel cleanup evidence beyond the bounded local run, and physical-device verification.

> **Queue-monitor/state continuation (2026-08-09):** `SpoolJobMonitor` now polls an injected status reader with timeout/cancellation and fail-closed printer/job identity checks. The Windows adapter maps queue flags to explicit spooling/printing/paused/paper-out/offline/error/completed/deleted observations and page counters where available. Print Preview and the main-window quick-print commands show/carry the final queue observation and record it in the JSONL operation log; `PrintJobStateStore` also persists preparation, preflight, dispatch, spool and queue-observation transitions under a stable job ID with sequence/hash recovery. `Completed`, `Printed`, `Deleted`, and `NotFound` remain spool evidence only; the monitor never sets physical-output verification. Current regression evidence is 72 application checks and 102 xUnit tests; child approval/dispatch, hot-unplug fixtures and physical printer verification remain open.

| Vùng | Evidence code hiện tại | Gap/risk |
| --- | --- | --- |
| Snap threshold | `LabelDesignerCanvas.cs:91-94`, `2104-2213` | cố định `1.0 mm`; cảm giác bắt điểm thay đổi theo zoom; không có acquire/release hysteresis hay target lock |
| Snap target | `LabelDesignerCanvas.cs:2118-2181` | chỉ cạnh/tâm object và tâm canvas; chưa có artboard edge, margin, guide kéo từ ruler, baseline, smart spacing hoặc target priority |
| Multi-select | `LabelDesignerCanvas.cs:249-282`, `1166-1248` | marquee dùng intersect; primary selection lấy Z cao nhất; chưa có last-selected/key-object rõ; Shift-click additive chưa có contract |
| Selection lifetime | `LabelDesignerCanvas.cs:209-243`, `783-816` | zoom/collection rebuild xóa tập chọn nội bộ; có thể làm adorner và keyboard state lệch, multi-paste chỉ còn selection cuối |
| Group move | `LabelDesignerCanvas.cs:294-297`, `1478-1514` | group move không đi qua snap engine; object locked bị bỏ khỏi start map nhưng UX không giải thích partial selection |
| Resize | `SelectionResizeAdorner.cs:12-52`, `LabelDesignerCanvas.cs:2054-2098` | handle cố định 8 DIP; resize mutate liên tục, không snap, chưa có aspect/center modifier và transaction session hoàn chỉnh |
| Grid | `LabelDesignerCanvas.cs:193-203`, `1055-1133`, `1690-1702` | grid 1 mm hard-coded; chỉ công cụ vẽ snap grid; move/resize không dùng grid; display và snap spacing chưa tách |
| Coordinate precision | `LabelObject.cs:64-88` | setter geometry round sớm đến 0.01 mm; không biểu diễn chính xác một printer-dot step chung cho 203/300/600 DPI và có thể tích lũy sai số nudge |
| Guide | `LabelDesignerCanvas.cs:2215-2298` | tối đa một line mỗi trục, chỉ vẽ target đầu tiên; không có label khoảng cách, persistent guide hay equal-spacing guide |
| Nudge | `LabelDesignerCanvas.cs:1440-1475` | Arrow = 1 mm, Shift = 10 mm; không có fine step và preference; mỗi tick sửa model trực tiếp |
| Align/distribute | `MainWindow.xaml:943-968` | mới có rotation và z-order; chưa có align, distribute, key object hoặc reference mode |
| Text properties | `ObjectStyle.cs:8-55`, `MainWindow.xaml:1102-1162` | baseline audit chỉ left/center/right; v0.099 đã có vertical align, direction và persisted `LineHeightPt` (`0 = Auto`, dương = minimum line box); justify/padding/sizing mode vẫn là R2 follow-up |
| Text layout | `TextBoxOverflowDetector.cs:17-178`, `TextLayoutResult.cs` | v0.099 dùng grapheme-safe wrapping, shared baseline/ink/vertical metrics và explicit line-by-line line-height; platform-neutral bidi/fallback/glyph parity và full padding/justify contract vẫn mở |
| Text auto bounds | `LabelDesignerCanvas.cs:1808-1844` | có explicit fit sau edit nhưng sizing mode chưa persisted/giải thích; data động dễ làm ý nghĩa frame mơ hồ |
| Viewport/print | `LabelDesignerCanvas.cs:1723-1768`, `LabelVisualRenderer.cs:178-208`, `TextLayoutResult.cs` | WPF paths now share the explicit text layout result and vertical offset; remaining gap is one platform-neutral compiled scene rather than duplicated UI/print policy |
| Bound static text | `LabelDesignerCanvas.cs:2033-2080`; `LabelVisualRenderer.cs:180-216`; `TextBoxOverflowDetector.cs` | Historical drift is fixed in v0.099 by one shared bound/vertical-offset policy; remaining gap is platform-neutral metrics plus RTL/fallback/ink-baseline fixture coverage |
| Ruler scroll | `MainWindow.xaml:751-785` | ruler và artboard nằm trong ba `ScrollViewer` riêng, chưa bind/sync offset; ruler có thể lệch khi pan/scroll |
| Undo | `MainViewModel.cs:2350-2368`, `2963-3055` | serialize toàn document; timer 300 ms không reset theo tick nên có thể cắt drag dài, gộp hai action và lưu trạng thái trung gian của gesture đã cancel |
| Printer selection | `PrintService.cs:146-174`, `PrinterQueueLookup.cs`, `MainViewModel.cs`, `MainWindow.xaml(.cs)`, v0.106 lookup/status seam | blank and disappeared named queues fail closed before dialog/dispatch; the UI warns and opens Printer Setup for repair; remaining gap is physical queue evidence |
| Print outcome | `PrintPreviewWindow.xaml.cs:315-337` | sau `PrintDocument` lập tức log success, đánh dấu row printed và báo complete; chưa có spool job identity/status hay device evidence |
| Ticket/capability parity | `PrintPreviewWindow.xaml.cs:206-337`; `PrintService.cs:75-174` | preview/preflight tạo plan trước ticket thực; chưa chứng minh ticket sau driver coercion, DPI/media và `PageImageableArea` giống plan |
| Preview scale | `PrintPreviewWindow.xaml.cs:692-799`, `962-970` | eager raster mọi page ở 300 DPI trên UI thread; batch lớn có nguy cơ OOM/freeze |
| Calibration error | `PrintService.cs:106-130` | catch toàn bộ và trả `false`; thiếu error context/remediation, dễ làm người dùng tưởng calibration đã chạy |
| Device discovery | `PrinterDiscoveryService.cs:6-33`; printer setup UI | paper catalog/DPI choices mang tính generic; chưa phải capability/effective DPI X/Y đã xác minh của từng queue |
| Barcode completeness | renderer/preflight/options hiện tại | module warning chung chưa thay application profile; fixed QR version/HRI/quiet-zone collision/GS1 AI chưa có contract end-to-end |
| Font/image fidelity | text/image render + preflight hiện tại | missing text family now resolves to deterministic fallback and is reported/blocking in preflight; exact font fingerprint/glyph/embedding evidence and image effective-PPI/monochrome-dither contract remain open |

Audit trên là baseline lập kế hoạch; line có thể dịch sau refactor. Work item phải dùng symbol/test ID, không phụ thuộc line number lâu dài.

## 4. Mô hình hình học chuẩn

### 4.1 Các không gian tọa độ

Phải phân biệt bốn không gian:

1. `DocumentSpace`: đơn vị canonical vật lý, persisted; hiện tương thích millimeter, V2 có thể dùng fixed-point micrometre nội bộ.
2. `SceneSpace`: geometry đã resolve layout, vẫn là physical unit, immutable.
3. `ViewportSpace`: DIP sau pan/zoom, chỉ dùng hit tolerance và vẽ overlay.
4. `DeviceSpace`: integer printer dots theo DPI/profile, chỉ xuất hiện khi compile `DeviceRenderPlan`.

Không lưu screen pixel vào document. Không dùng WPF layout rounding để quyết định barcode module hoặc vị trí in.

```text
Document command
    ↓
DocumentSnapshot (physical)
    ↓
SceneCompiler
    ↓
ResolvedScene (physical + text metrics + anchors)
    ├── ViewTransform → viewport DIP
    └── DotQuantizer(profile) → integer device plan
```

### 4.2 Bounds và anchors

Mỗi resolved node cung cấp:

- `LayoutBounds`: frame dùng cho move/resize/align mặc định;
- `InkBounds`: pixel/glyph/stroke thực sự được vẽ, dùng optical preview và diagnostics;
- `HitGeometry`: geometry/tolerance để select;
- `SnapAnchors`: left, center-x, right, top, center-y, bottom và anchors đặc thù;
- `TextMetrics`: first/last baseline, ascender, descender, line boxes nếu là text;
- `DeviceCriticalGeometry`: barcode modules, quiet zone, hairline, cut/safe region.

Align command mặc định dùng `LayoutBounds` vì deterministic và không đổi khi glyph có overhang. `InkBounds` chỉ được dùng khi người dùng chọn “Optical align”; mode này phải hiện rõ và có golden test.

## 5. Snap Engine V2

### 5.1 Contract

```csharp
SnapResult Evaluate(SnapRequest request);

SnapRequest =
  sessionId, operationKind, movingNodeIds,
  proposedTransform, sceneSnapshot,
  viewTransform, pointerType, modifiers,
  activeTargetLock, preferences;

SnapResult =
  adjustedTransform, xMatch?, yMatch?,
  guides[], distanceLabels[], explanation,
  nextTargetLock;
```

Service pure theo request; nó không sửa document, selection hoặc WPF element. Pointer move chỉ cập nhật transient transform overlay; pointer up tạo đúng một `TransformNodesCommand`.

### 5.2 Nguồn bắt điểm

| Nhóm | Anchors/behavior | Phase |
| --- | --- | --- |
| Explicit guides | guide ngang/dọc kéo từ ruler, guide khóa, guide theo stock | DP1 |
| Artboard | left/center/right/top/middle/bottom | DP1 |
| Safe/printable area | margin, printable bounds, quiet/cut safe boundary | DP1/IR1 |
| Object geometry | layout edges/centers; line endpoints/midpoint | DP1 |
| Text | first baseline mặc định; optional last baseline; frame anchors | DP2 |
| Grid | configurable snap step độc lập display grid | DP1 |
| Smart spacing | khoảng cách bằng nhau giữa 2+ object, gap labels | DP2 |
| Container tracks | row/column/stack/grid track, padding | R6 |
| Barcode critical | quiet-zone edge/module origin ở diagnostic mode | IR2, không bật mặc định cho generic objects |

Hidden node và non-interactive guide không tạo candidate. Locked node có thể làm target nhưng không được di chuyển. Candidate từ chính moving selection bị loại.

### 5.3 Tolerance theo zoom

Một ngưỡng vật lý cố định làm snap quá yếu khi zoom nhỏ và quá “dính” khi zoom lớn. Candidate eligibility phải được tính trong ViewportSpace; delta commit vẫn là DocumentSpace:

```text
mouseAcquire = 6 DIP
mouseRelease = 10 DIP
switchMargin = 2 DIP

mmPerDip = 25.4 / (96 × zoom)
acquireMm = mouseAcquire × mmPerDip
releaseMm = mouseRelease × mmPerDip
```

Các số trên là default đề xuất và phải usability-test ở 25/50/100/200/400/800%. Pen/touch hoặc accessibility profile có thể dùng tolerance lớn hơn; preference lưu ngoài document. Nếu micro-label study chứng minh cần `maxPhysicalSnapDistance`, nó phải là preference/profile explicit và acceptance matrix phải thừa nhận rằng screen tolerance khi đó bị giới hạn; không âm thầm clamp.

### 5.4 Hysteresis và target lock

- Khi candidate thắng, `SnapSession` khóa `targetNodeId + sourceAnchor + targetAnchor + axis`.
- Giữ target cho đến khi raw pointer vượt `releaseMm`, candidate biến mất hoặc modifier bypass bật.
- Candidate mới không được “cướp” target chỉ vì gần hơn rất ít; phải thắng ít nhất `switchMargin` hoặc target cũ đã release.
- X và Y có lock độc lập; overlay nói rõ nếu hai trục lấy hai target khác nhau.
- Tie-break deterministic: priority → khoảng cách screen → semantic compatibility → target stable ID → anchor enum.

Hysteresis là acceptance criterion bắt buộc: rê pointer ±1 DIP quanh ranh giới không được làm guide nhấp nháy giữa hai object.

### 5.5 Priority đề xuất

| Priority | Candidate |
| ---: | --- |
| 100 | explicit locked guide hoặc key-object anchor được người dùng yêu cầu |
| 95 | printable/safe boundary đang hiển thị |
| 90 | artboard edge/center |
| 85 | cùng loại anchor của object: left-left, center-center, baseline-baseline |
| 80 | cross-edge hợp lý: right-left, bottom-top |
| 75 | smart equal spacing |
| 65 | grid |
| 50 | cross-anchor ít rõ nghĩa, chỉ bật qua preference |

Không tạo 9×9 cặp edge/center ngang dọc như một tập ngang giá. Ví dụ `left → other center` không nên thắng `left → other left` chỉ vì gần hơn 0.01 mm nếu người dùng đang nối một hàng object.

### 5.6 Selection hull

- Một object: anchors lấy từ resolved geometry của object.
- Multi-selection: moving source là hull của cả selection; internal offsets không đổi.
- Group: dùng group local transform; không flatten children trong gesture.
- Rotation: DP1 dùng projection bounds theo trục document; DP3 thêm oriented anchors/projection cho rotation tùy ý.
- Line: endpoints và midpoint là anchors thật; không giả line là rectangle có width/height dương.

### 5.7 Modifier và keyboard contract

| Input | Contract |
| --- | --- |
| `Alt` trong move/resize | bypass snap tạm thời; thả Alt đánh giá lại candidate nhưng không teleport nếu pointer chưa move |
| `Shift` trong move | khóa trục dominant sau dead-zone; snap chỉ chạy trên trục cho phép |
| `Shift` trong resize corner | giữ aspect ratio từ geometry lúc bắt đầu gesture |
| `Alt` trong resize | resize đối xứng quanh center; nếu xung đột accessibility/menu phải có setting thay thế |
| Arrow | nudge theo `fineStep`, default đề xuất 0.1 mm |
| Shift+Arrow | 10× `fineStep`, default 1 mm |
| Ctrl+Arrow | precision step, default 0.01 mm; không chạy khi focus ở property/text editor |
| Dot-exact mode | optional step `25.4 / targetDpi` mm tính từ gesture start, không cộng trên giá trị đã round |
| Numeric fields | giá trị vật lý authoritative; Enter tạo một command có validation |

Không thay shortcut hiện tại trong hotfix S0. Shortcut mới chỉ bật cùng Viewport V2, có shortcut overlay và test keyboard-only.

### 5.8 Guides và feedback

Overlay phải hiển thị tối thiểu:

- line theo toàn vùng liên quan, không chỉ một đoạn tùy ý;
- badge `X 12.50 mm`, `baseline`, `center`, hoặc `gap 3.00 mm`;
- màu/kiểu line khác nhau cho explicit guide, object alignment, smart spacing và safe area;
- key object có outline khác primary selection;
- khi snap bị bypass, status text nói `Snap bypassed (Alt)`;
- screen reader announcement được debounce, không đọc mọi pointer tick.

Guide kéo từ ruler là design metadata, persisted riêng khỏi printable scene. Cursor/hover guides và candidate line là transient session state, không persisted.

### 5.9 Pseudocode chọn candidate

```text
raw = proposed transform in DocumentSpace
sourceAnchors = resolve moving selection anchors(raw)
targets = spatialIndex.query(expand(sourceHull, acquireMm))
candidates = generateCompatiblePairs(sourceAnchors, targets, operation)
candidates += grid/artboard/guide/safeArea/spacing candidates

for candidate in candidates:
    candidate.screenDistance = toViewportDistance(candidate.delta)
    candidate.score = (
        semanticPriority,
        activeLockBonus,
        -screenDistance,
        stableTieBreak)

winnerX = chooseAxis(X, candidates, existingLockX, acquire, release)
winnerY = chooseAxis(Y, candidates, existingLockY, acquire, release)
return raw adjusted by winner deltas + explanation + next locks
```

Snap engine không được gọi `Template.Objects` mutable trực tiếp; request dùng immutable scene/anchor snapshot của frame hiện tại.

## 6. Align, distribute và smart spacing

### 6.1 Selection roles

Selection V2 có ba khái niệm không được nhập làm một:

- `SelectedNodeIds`: tập object sẽ chịu command;
- `PrimaryNodeId`: object inspector đang mô tả và là anchor cho keyboard traversal;
- `KeyNodeId`: object mốc align/distribute, mặc định là object được click/chọn trực tiếp sau cùng.

Key object được vẽ bằng outline đậm/khác màu. Marquee tự nó không được chọn key ngẫu nhiên theo Z-index; sau marquee, primary có thể là node Z cao nhất để inspector hoạt động, nhưng align command mặc định dùng `SelectionBounds` cho đến khi người dùng click một node làm key.

Shift-click thêm node và đặt làm key; Ctrl-click toggle membership. Click lại một node đã chọn đặt node đó làm key mà không xóa selection. Marquee mặc định `Contain` để giảm chọn nhầm, có mode `Intersect` explicit và modifier-add; hidden node không được chọn. `Esc` đi theo bậc: hủy gesture → bỏ key/reference mode → clear selection.

### 6.2 Reference modes

Mỗi align command nhận reference explicit:

1. `KeyObject` — mặc định khi có key;
2. `SelectionBounds` — dùng min/max/center của toàn selection;
3. `Artboard` — căn một hoặc nhiều object với label;
4. `PrintableArea` — căn với vùng in của profile hiện tại;
5. `SafeArea` — căn với margin/safe guide;
6. `LastObject` chỉ là compatibility option nếu product study chứng minh cần, không dùng state ngầm.

Command bar phải hiện reference đang dùng; không thay đổi ngầm theo số object.

### 6.3 Command matrix

| Command | Số node tối thiểu | Result |
| --- | ---: | --- |
| Align left/center/right | 1 với artboard; 2 với key/selection | dịch X, giữ Y và size |
| Align top/middle/bottom | 1 với artboard; 2 với key/selection | dịch Y, giữ X và size |
| Align first baseline | 2 text-capable nodes | dịch Y để first baseline bằng nhau |
| Align last baseline | 2 multiline text nodes | optional DP3; dịch Y theo last baseline |
| Distribute centers X/Y | 3 | center đầu/cuối cố định, center giữa cách đều |
| Distribute gaps X/Y | 3 | outer bounds cố định, khoảng trống giữa layout bounds bằng nhau |
| Set exact gap X/Y | 2 | xếp theo spatial order với gap nhập mm |
| Pack X/Y | 2 | gap = 0 hoặc token spacing được chọn |
| Match width/height/size | 2 | DP3; key object cung cấp size, qua resize policy của từng node |

`Distribute gaps` và `distribute centers` không phải cùng command. Với object khác kích thước, hai kết quả khác nhau và UI phải gọi đúng tên.

### 6.4 Quy tắc deterministic

- Sort theo `LayoutBounds.Left/Top`, sau đó stable node ID; không dùng collection order tình cờ.
- Outer/key anchors tính từ immutable before-state, không đọc geometry đã mutate giữa vòng lặp.
- Kết quả được tính toàn bộ rồi apply một `TransformNodesCommand` duy nhất.
- Command chứa before/after transform của từng node; undo khôi phục document hash chính xác.
- Coordinate sau command được canonicalize một lần; không round lặp qua DIP.
- Baseline dùng metric từ cùng `TextLayoutResult` mà viewport/preview/print dùng.
- Nếu total gap âm do objects overlap, `Distribute gaps` vẫn cho gap âm có giải thích; không tự resize hoặc reorder.
- Rotated object DP1 dùng axis-aligned layout projection; inspector ghi rõ. Oriented distribution chỉ thêm sau khi có UX/test.

### 6.5 Locked, hidden, group và container

- Hidden node không nằm trong selection command.
- Locked node chỉ được làm key/reference; nếu một locked node khác cũng nằm trong moving set, command fail-fast với message nêu tên/thành phần bị khóa. Không “skip im lặng”.
- Group mặc định là một node. Người dùng phải enter group để align children.
- Child trong auto-layout/container không được sửa X/Y tuyệt đối; align command chuyển thành container alignment property hoặc bị disable với remediation.
- Object nằm ngoài printable bounds được phép trong authoring nhưng problems/preflight phải hiện; align-to-printable giúp sửa nhanh.

### 6.6 Smart spacing khi kéo

DP2 tạo candidate khi moving hull có thể nằm:

- giữa hai object với hai gap bằng nhau;
- trước/sau một chuỗi có common gap;
- thẳng hàng với row/column hiện có.

Spacing candidate chỉ sinh khi ít nhất hai relationship cùng trục đủ bằng chứng. Badge hiển thị gap theo mm. Nó có priority thấp hơn explicit guide/artboard/key anchor nhưng cao hơn grid khi người dùng đã ở trong một row/column rõ.

## 7. Text layout và căn chữ

### 7.1 Bốn khái niệm bắt buộc tách riêng

1. **Object alignment**: vị trí frame text so với object khác/artboard.
2. **Content alignment**: left/center/right/justify và top/middle/bottom bên trong frame.
3. **Baseline alignment**: đường đặt glyph của dòng đầu/cuối giữa các text object.
4. **Optical alignment**: căn theo phần mực nhìn thấy (`InkBounds`), chỉ là mode chủ động.

Nếu chỉ dùng bounding rectangle để “căn text”, chữ `A`, `g`, font 8 pt và font 24 pt có thể nhìn lệch dù top/bottom frame bằng nhau. Baseline phải là metric chính thức của text engine, không là `Height × constant`.

### 7.2 Text model đích

```text
TextNode
├── content/binding/culture/direction
├── font family/weight/style/stretch/fallback policy
├── font size/line height/letter spacing/OpenType options
├── frame: width mode, height mode, min/max, padding
├── horizontal: left/center/right/justify
├── vertical: top/middle/bottom
├── wrapping: none/word/character/word-with-character-fallback
├── overflow: error/clip/ellipsis/shrink-to-fit/continue
└── diagnostics policy: author-only/warning/block-print
```

V2 serialization phải có defaults tương thích v1: horizontal lấy `ObjectStyle.Alignment`; vertical của `Text` hiện tại map `Middle`, `TextBox` map `Top`; direction `Auto`; wrapping của `TextBox` map gần nhất với hành vi cũ nhưng migration report phải ghi thay đổi.

### 7.3 Sizing modes

| Mode | Geometry source | Dùng khi | Rủi ro và policy |
| --- | --- | --- | --- |
| `HugContent` | content + padding tạo resolved W/H | text tĩnh ngắn, caption | data đổi làm resolved bounds đổi; phải có min/max và không persist ngược trong render |
| `FixedWidthAutoHeight` | W persisted, H resolve từ wrapped lines | địa chỉ/mô tả | thay record làm H đổi; container/overflow zone phải kiểm soát |
| `FixedFrame` | W/H persisted | template công nghiệp/data biến đổi | an toàn nhất khi kết hợp `ErrorOnOverflow` và validate mọi record |
| `FixedFrameShrinkToFit` | frame cố định, compiler chọn font size trong min/max | field có giới hạn chặt | phải hiển thị actual resolved size; block nếu dưới min/readability rule |
| `ApplyMeasuredSize` | command lấy current resolved bounds và chuyển sang fixed | người dùng muốn “đóng băng” kết quả | là explicit command/undo step, không là render side effect |

Không có mode “auto-size bí mật”. Mọi mode hiển thị trong inspector và problems panel.

Quy tắc cho `HugContent`:

- trailing whitespace không làm frame nhìn thấy rộng thêm trừ khi policy preserve-space được bật;
- italic/diacritic/underline overhang vẫn phải nằm trong ink/clip safety;
- padding là property explicit, default 0; không cộng hằng số `4 DIP + 0.6 mm` ngầm;
- đổi màu/alignment không làm resize nếu metrics không đổi;
- khi resolved size đổi, anchor point 3×3 được giữ để object không trôi ngoài ý muốn;
- với bound data, resolved bounds có thể đổi theo row nhưng persisted geometry không được ghi ngược từ render.

### 7.4 Overflow policies

- `Error`: scene vẫn vẽ diagnostic; preflight block theo severity. Đây là default đề xuất cho `FixedFrame` bind dữ liệu sản xuất.
- `Clip`: cắt đúng frame; warning mặc định vì mất dữ liệu có thể im lặng.
- `Ellipsis`: phù hợp UI/human text, không dùng mặc định cho mã, lot, serial, UDI hoặc dữ liệu bắt buộc.
- `ShrinkToFit`: binary-search/candidate font size deterministic, có `minFontSizePt`; không thay đổi style persisted.
- `Continue`: chỉ hợp lệ trong flow/container/page layout về sau.

Overflow phải kiểm tra trên toàn record set trước job hoặc theo policy sampling rõ ràng. Không chỉ kiểm tra preview row đang nhìn.

### 7.5 Measurement pipeline

```text
Resolve data + culture + direction
  → choose font/fallback and report substitutions
  → shape glyph runs
  → line break with width/wrap policy
  → compute line boxes, first/last baseline, ink/layout bounds
  → apply horizontal/vertical alignment and padding
  → apply overflow policy
  → emit TextLayoutResult + diagnostics + stable metrics hash
```

`TextLayoutResult` là input chung cho viewport, preview và print adapter. Viewport không tự tạo một `FormattedText` khác với print. Nếu WPF `FormattedText` được dùng trong giai đoạn đầu, wrapper phải truyền cùng culture, direction, typeface, pixels-per-DIP strategy và expose `Baseline`; về dài hạn `TextFormatter`/glyph run abstraction được đánh giá trong ADR text shaping.

### 7.6 Baseline semantics

- Single-line text: `firstBaseline == lastBaseline`.
- Multi-line: align command mặc định dùng `firstBaseline`; command last-baseline là riêng.
- Vertical `Middle` không thay baseline metric; nó chỉ đổi origin của block text trong frame.
- Baseline snap chỉ sinh giữa text-capable anchors; không snap baseline vào cạnh rectangle trừ khi user tạo explicit guide.
- Missing font/fallback làm metrics hash đổi và invalidates scene/preflight.
- Baseline equality trong SceneSpace: sai số tối đa `0.01 mm`; sau device quantization cho phép chênh tối đa một dot nếu không thể biểu diễn chính xác.

### 7.7 International text

- `FlowDirection.Auto` suy theo culture/content nhưng resolved direction phải nằm trong scene hash.
- Hỗ trợ LTR, RTL, bidi, combining marks, surrogate pairs, emoji/fallback, CJK line break và Vietnamese diacritics trong fixtures.
- Wrap không được cắt UTF-16 code unit mù hoặc phá grapheme cluster.
- Font fallback phải report family thực sự dùng; không âm thầm thay font giữa preview và print.
- `Justify` chỉ áp dụng paragraph phù hợp; last line policy explicit.

### 7.8 Text precision acceptance

- Thay preview row không mutate `X/Y/W/H`, style hoặc document hash.
- Hai text khác cỡ/font align baseline rồi save/load vẫn cùng baseline.
- Vertical top/middle/bottom khớp viewport/preview/print scene.
- `HugContent` resolve deterministic 100 lần; `ApplyMeasuredSize` tạo đúng một undo step.
- Overflow fixture chạy ở 203/300/600 DPI, English/Vietnamese/Arabic/CJK và string dài hơn 40%.
- Missing font làm preflight diagnostic có remediation, không crash hoặc fallback im lặng.

## 8. Ruler, grid và guide system

### 8.1 Một viewport transform duy nhất

Ruler ngang, ruler dọc, artboard, selection overlay và guide overlay phải đọc cùng `ViewportTransform { zoom, panX, panY, dpiScale }`. Không duy trì ba scroll offset độc lập.

Acceptance:

- scroll/pan 1,000 lần ở 25–400% zoom không làm tick `0 mm` lệch artboard quá 1 screen DIP;
- zoom-around-pointer giữ document coordinate dưới pointer ổn định;
- đổi Windows scale 100/125/150/200% không đổi document geometry;
- ruler tick density thích ứng zoom nhưng snap grid step không tự đổi nếu preference không đổi.

### 8.2 Grid preferences

Tách riêng:

- display grid on/off;
- major/minor spacing;
- snap-to-grid on/off;
- snap spacing;
- origin;
- unit display mm/inch;
- adaptive visual density.

Document vẫn canonical physical unit; đổi display sang inch không convert rồi ghi đè geometry nhiều lần.

### 8.3 Persistent guides

Người dùng kéo guide từ ruler, nhập tọa độ, lock/unlock, rename/color, duplicate và delete. Guide có thể:

- document-level;
- artboard-level;
- stock/profile-derived read-only guide;
- printable/safe/cut area derived guide.

Guide persisted trong design metadata và bị loại khỏi print scene. Printer-derived guide mang profile version; đổi profile làm guide derived invalid/recompute và chạy preflight.

### 8.4 Problems integration

Click problem `outside printable area`, `text overflow`, `barcode quiet zone` phải:

1. chuyển về Design workspace;
2. select object;
3. zoom/pan đưa object vào view;
4. bật overlay liên quan;
5. đề xuất command sửa, không tự mutate.

## 9. Industrial reliability contract

### 9.1 Nguyên tắc “same intent, explicit device result”

ANLAbel cần hai artifact bất biến:

- `ResolvedScene`: ý nghĩa hình học/nội dung vật lý sau data/layout, độc lập màn hình;
- `DeviceRenderPlan`: scene đã resolve theo printer profile, DPI, printable bounds, media, native/raster capability và quantize sang device dots.

Viewport/preview có thể mô phỏng DeviceRenderPlan, nhưng document không được chứa driver state ngầm. Cùng manifest đầu vào phải cho cùng scene hash và device-plan hash.

### 9.2 Printer + stock profile

Profile versioned tối thiểu chứa:

```text
PrinterProfile
├── profileId/version/displayName
├── queue binding + observed driver/firmware metadata
├── effective dpiX/dpiY + source (driver/profile/measured) + verified state
├── printable bounds/origin/orientation
├── capability claims + evidence status
├── stockId/version
│   ├── width/height/pitch/gap or mark geometry
│   ├── media mode: gap/mark/continuous
│   ├── sensor type/position note
│   └── thermal mode/material/ribbon note
├── calibration correction: offsetX/Y, optional scale with evidence
├── safe/cut/peel zones
└── evidence links/date/operator
```

Speed, darkness, cutter, peel, RFID và native-language settings chỉ thêm khi capability provider + hardware evidence xác nhận. Không copy toàn bộ DEVMODE blob vào `.anlabel`. Không giả định `dpiX == dpiY`; catalog/golden tối thiểu phải nhận biết 203/300/305/600/609 DPI và lưu đúng giá trị effective mà driver trả về.

Mỗi lần chuẩn bị output phải tạo một `EffectiveOutputContract` theo đúng một chiều:

```text
Requested PrinterProfile + Requested PrintTicket
  → PrintQueue.MergeAndValidatePrintTicket
  → EffectiveTicket + ConflictStatus
  → PrintQueue.GetPrintCapabilities(EffectiveTicket)
  → effective dpiX/dpiY + media + PageImageableArea/origin
  → EffectiveOutputContract hash
  → DeviceRenderPlan + preflight + device preview
  → dispatch chính artifact/hash đã duyệt
```

Nếu driver coerce media, orientation, resolution hoặc printable area, mọi preview/preflight/device plan cũ bị invalid. Capability thiếu hoặc không đáng tin cậy phải hiện `Unverified` và áp policy explicit; không được ngầm coi toàn bộ physical label là printable.

### 9.3 Media and calibration workflow

```text
Select verified printer profile
  → select stock/media profile
  → compare loaded/declared media mode
  → show vendor-neutral calibration checklist
  → optional vendor-specific action only with supported provider
  → print calibration pattern
  → user measures X/Y/size and records evidence
  → calculate proposed correction
  → user confirms profile revision
  → rerun preflight + proof label
```

ANLAbel không được tự gửi lệnh calibration hoặc sửa driver/DEVMODE trong nền. Giai đoạn đầu chỉ hướng dẫn và ghi kết quả. Mọi provider có write capability cần explicit confirmation, allowlist model/firmware và test máy thật. Lỗi/cancel calibration phải trả diagnostic có printer/stock/profile context và remediation; cấm nuốt exception rồi để UI trông như đã hoàn tất.

Calibrate lại khi:

- đổi gap ↔ black mark ↔ continuous;
- đổi kích thước/độ dày/màu liner, preprinted media hoặc sensor position;
- thay printhead/DPI/profile/driver/firmware;
- sensor profile cho thấy gap/mark không ổn định;
- proof label vượt tolerance.

### 9.4 Quantization sang printer dots

Với `dotsPerMmX = dpiX / 25.4` và `dotsPerMmY = dpiY / 25.4`, mỗi trục quantize boundary đúng một lần:

```text
dotLeft  = round(xMm × dotsPerMm, deterministic midpoint rule)
dotRight = round((xMm + widthMm) × dotsPerMm, same rule)
dotWidth = dotRight - dotLeft
```

Không tính `dotWidth = round(widthMm × dotsPerMm)` độc lập nếu điều đó làm cạnh phải drift so với object kế bên. Với chuỗi modules/cells, quantize shared boundaries/prefix positions để tổng width được bảo toàn.

Barcode có luật riêng:

- X-dimension/module phải là số dot nguyên;
- tất cả modules của cùng symbol dùng cùng module-dot count;
- quiet zone tính theo symbology sau quantization;
- kiểm tra quiet zone với biên printable và mọi object nền/lân cận có thể xâm lấn, không chỉ kiểm tra frame của chính barcode;
- nếu frame không chứa được integer modules + quiet zone, preflight block thay vì stretch phi tuyến;
- HRI dùng text sub-layout và font/fallback đã resolve;
- fixed QR/Data Matrix version phải được truyền thật vào encoder và kiểm tra capacity từ encoder, không dùng heuristic diện tích;
- GS1 AI, check digit, FNC1, HRI và X-dimension dùng application profile/version explicit; “scan được” không thay verification;
- preview có `Device pixel preview` để xem chính xác dot grid ở DPI mục tiêu.

### 9.5 Printable bounds và orientation

- Label physical size, printable bounds, print origin và media feed orientation là các giá trị riêng.
- Scene object có thể nằm ngoài printable bounds khi thiết kế, nhưng job preflight phải phân loại clipped/error theo policy.
- Đổi orientation không được “đảo width/height” ở nhiều layer. Một geometry resolver duy nhất tạo oriented scene/profile view.
- Manual/custom label size luôn thắng preset khi mode manual được chọn; selection stale trong list không được ghi đè.
- Driver-reported capability là observation có timestamp, không là chân lý nếu profile chưa verified trên hardware.

### 9.6 Font và text fidelity

Preflight theo job profile phải kiểm tra:

- font family/style có tồn tại và có embed/render path phù hợp;
- fallback thực tế có đổi metrics/line break không;
- text overflow trên mọi record/copy/serial value liên quan;
- minimum font/readability rule do template/organization cấu hình;
- bidi/culture/number formatting đã resolve;
- native printer font, nếu về sau dùng, có metric parity fixture riêng; không thay WPF font chỉ vì trùng tên.

Raster hóa text là compatibility fallback có version/policy, không được âm thầm. DeviceRenderPlan ghi rõ object nào vector glyph, raster hay printer-native. Production manifest ghi exact font fingerprint, glyph/fallback result và license/embedding evidence phù hợp; missing family/glyph phải block hoặc yêu cầu fallback đã duyệt.

Ảnh và text raster dùng pipeline nhiệt explicit: decode một lần theo content hash, kiểm tra corrupt/alpha/effective PPI, chuyển monochrome/1bpp bằng dither mode đã version hóa hoặc ghi rõ `driver-managed`. Preview mô phỏng đúng mode đó; không để driver tùy ý đổi kết quả mà output contract vẫn mang cùng hash.

### 9.7 Barcode quality

Software preflight xác nhận cấu trúc và geometry, nhưng không tự gọi output “GS1 verified”. Chương trình verification gồm:

- data/AI/check digit/symbology;
- X-dimension/module dots;
- quiet zone và symbol height;
- contrast/material/darkness/speed bằng test vật lý;
- verifier grade khi có thiết bị; scanner smoke không thay verifier;
- evidence gắn printer, media, ribbon, DPI, speed/darkness, template/device-plan hash.

Mỗi supported barcode có golden vectors ở 203/300/305/600/609 DPI, cả `dpiX != dpiY` khi provider báo, và near-boundary cases: đúng vừa frame, thiếu một dot, quiet-zone collision, fixed-version capacity, HRI, GS1 data, long/empty/invalid data, rotation và edge of printable area.

## 10. Preflight fail-closed

### 10.1 Manifest đầu vào

Preflight nhận immutable `JobManifestDraft`:

- document/revision/schema hash;
- sample/selected/all-record data snapshot + freshness hash;
- variable/counter/serial reservation state;
- printer profile + stock version;
- target DPI/capabilities/calibration;
- quantity/copies/record ordering;
- resolved scene/device-plan hashes;
- user/role/override policy.

Nó không đọc selection/preview row hiện thời của MainWindow như state ngầm.

### 10.2 Blocking conditions tối thiểu

- schema/migration/resource lỗi;
- missing/stale data hoặc field/type/transform error;
- text/barcode nằm ngoài printable bounds theo block policy;
- text overflow `Error`, shrink dưới minimum, missing/fallback font không được duyệt;
- barcode invalid, module/quiet zone không thể biểu diễn ở target DPI;
- image missing/corrupt/không decode;
- profile/stock/DPI đổi sau compile;
- printer queue missing/offline/paper-out khi status đáng tin cậy;
- media mode mismatch do user/profile khai báo;
- serial/counter reservation conflict;
- quá memory/page/record budget mà không thể stream/virtualize.

Warning override cần reason và audit; lỗi cấu trúc barcode, stale manifest hash hoặc duplicate-risk không được override nếu policy không cho phép.

### 10.3 Invalidation graph

| Thay đổi | Invalidate |
| --- | --- |
| content/data/culture/font | text layout → scene → device plan → preflight |
| geometry/align/snap | scene bounds → device plan → preflight |
| DPI/profile/printable bounds | device plan + device-specific diagnostics |
| media/stock/sensor/calibration | safe bounds + device plan + hardware readiness |
| quantity/record order/serial | manifest/preflight/job reservation |
| display zoom/theme | viewport only; không đổi scene/job hash |

Async result cũ phải mang generation ID/hash và bị bỏ nếu input đã đổi; không được overwrite diagnostics mới.

## 11. Job lifecycle và chống in trùng

### 11.1 State machine đề xuất

```text
Draft
  → Validating
  → Ready
  → Queued
  → Rendering
  → Spooling
  → SpoolAccepted
  → DeviceAcknowledged?
  → Completed | Failed | Cancelled | Unknown
```

`DeviceAcknowledged` chỉ tồn tại khi backend/model cung cấp feedback đáng tin và evidence xác nhận. Generic Windows path thường có thể dừng ở `SpoolAccepted` rồi `CompletedBySpooler`; UI phải diễn đạt đúng, không biến nó thành “nhãn chắc chắn đã ra”.

### 11.2 Event và recovery

- Mỗi transition append event trước/sau side effect theo protocol được ADR hóa.
- Job có correlation ID và idempotency key, nhưng idempotency key không tự làm printer idempotent.
- Sau crash/restart, job `Rendering` có thể retry nếu chưa dispatch; job `Spooling/SpoolAccepted/Unknown` không auto retry.
- Reprint luôn tạo job mới, link parent, reason và resolved input mới/copy rõ ràng.
- Cancel sau spool có thể cho kết quả `Unknown`; UI không hứa printer đã dừng.
- Serial/counter commit policy phân biệt `reserve`, `spool accepted`, `device confirmed`; reconciliation là workflow riêng.

### 11.3 Queue behavior

- Ordering key ít nhất theo printer profile và serial namespace.
- Chỉ một dispatcher sở hữu ordering của cùng key.
- Backpressure giới hạn rendered pages/images trong RAM.
- Preview virtualize/lazy render; không eager render toàn bộ 300 DPI trên UI thread.
- User có thể đóng Print Center mà job/background state vẫn nhất quán.

## 12. Performance, resilience và observability

### 12.1 Provisional budgets

Các budget này là target để benchmark; R1/R2 có thể điều chỉnh bằng evidence/ADR, không được bỏ im lặng.

| Scenario | Target |
| --- | --- |
| `SnapEngine.Evaluate`, 500 simple nodes | p95 đề xuất ≤ 2 ms trên máy baseline |
| Pointer move + snap overlay, 500 simple nodes | total p95 ≤ 16.7 ms; không full rebuild/serialize/raster content |
| Nudge/align command 100 nodes | UI feedback ≤ 50 ms; một undo transaction |
| Pan/zoom 500 nodes | không mutate document; không allocation tăng không giới hạn |
| Incremental text update một node | scene update p95 ≤ 50 ms, stale result cancel |
| Open first preview page | ≤ 1 s cho template baseline; remaining pages lazy |
| 10,000 label instances synthetic | initial preview tăng RAM mục tiêu < 300 MB; LRU chỉ giữ 5–10 trang; không materialize bitmap 300 DPI cho tất cả |
| Cancel long preflight/preview | UI acknowledge ≤ 200 ms; worker dừng và cleanup mục tiêu < 1 s |
| Document compile repeat 100 lần | same scene hash, document hash unchanged |
| Drag liên tục 60 giây | overlay/event/memory không tăng liên tục; không leak capture/handler |
| Add/remove 1.000 vòng | removed nodes/template cũ collect được; không còn event subscription |

Máy baseline, data fixture và measurement method phải nằm trong evidence; không so benchmark từ máy khác mà không ghi cấu hình.

### 12.1.1 Evidence reproducible hiện tại (v0.099)

Hai application regressions giữ cùng một contract với các budget ở trên:

- `preview raster coalesces a 10k request burst` phát 10.000 request trên một worker STA dùng chung; assertion giữ newest result frozen, có supersession, pending-slot high-water bằng `1`, worker không tăng và queue rỗng sau cleanup.
- `preview 10k stress stays within memory and cancel budget` tạo 10.000 metadata entries, giữ 8 bitmap theo cache policy, ghi managed/working/private process deltas và ước lượng footprint bitmap. Lần chạy baseline ngày 2026-08-09 ghi `estimatedBitmapMB=40.9`, `measuredDeltaMB=0.0` (counters OS đã commit trước đó, không được diễn giải thành “zero allocation”), và pre-start cancellation `0.1 ms`; test vẫn enforce `<300 MB` và `<1 s`.

Kết quả này chứng minh queue/cache policy và đường hủy trong process hiện tại; chưa đóng long-soak, peak-RAM sau warm-up trên máy 4 GB, native render call không preemptible, hoặc cleanup sau cancel đang chạy. Các evidence đó phải bổ sung trước khi đổi trạng thái IR-004 từ mitigation sang certified.

### 12.2 Fault injection

Phải test:

- mouse capture mất, Alt+Tab, popup mở, Esc giữa move/resize/align preview;
- rapid zoom/pan/preview-row change và compile result về sai thứ tự;
- font bị gỡ sau khi mở document;
- image/data file bị đổi, khóa, xóa, corrupt hoặc network timeout;
- disk full/permission denied khi save/job event append;
- printer rename/offline/paper out/cover open/user intervention;
- USB rút giữa spool, network disconnect, spooler restart;
- driver dialog treo/không trả, process close/crash/power loss;
- profile/stock revision đổi trong khi Print Center đang mở;
- user bấm Print/Enter nhiều lần hoặc scanner gửi duplicate event.

### 12.3 Telemetry/log cục bộ

Log structured nhưng không ghi label values mặc định:

- command/scene/job correlation IDs;
- duration, cache hit/miss, node/page/record counts;
- diagnostic codes, transition, retry/reprint reason;
- profile/version/hash và anonymized environment;
- exception chain đã redact path/value/secret.

Diagnostics export phải cho user preview/redact trước khi chia sẻ.

## 13. Ma trận kiểm thử Designer Precision

### 13.1 Snap matrix

| Dimension | Cases bắt buộc |
| --- | --- |
| Zoom | 25, 50, 100, 200, 400, 800% |
| Windows scale | 100, 125, 150, 200% |
| Source | single, multi-select hull, group, line, rotated, text baseline |
| Target | object edge/center, artboard, safe area, guide, grid, spacing |
| Operation | move, resize 8 handles, draw, nudge, numeric commit |
| Modifier | none, Alt bypass, Shift axis/aspect, lost capture, Esc |
| Conflict | equal distance, priority conflict, two targets near nhau, target removed/hidden/locked |
| Edge | object at each artboard corner, outside printable area, sub-mm objects |

Assertions:

- screen acquire error ≤ 1 DIP so với tolerance contract;
- target không switch trong release zone;
- same input/order-independent scene cho same stable IDs;
- no mutation until gesture commit;
- commit tạo một undo step; cancel giữ document hash;
- guides/explanation khớp actual applied delta.

### 13.2 Align/distribute matrix

- 2/3/100 objects, same/different size, overlap, negative gap;
- reference key/selection/artboard/printable/safe;
- locked key, locked moving node, hidden node, group/container child;
- rotated/line/text objects;
- baseline khác font/weight/size/direction;
- save/load, undo/redo, unit mm/inch, 203/300/600 device quantization;
- order collection shuffled nhưng stable IDs/geometry giống nhau.

### 13.3 Text matrix

- sizing mode × overflow policy × horizontal × vertical alignment;
- no-wrap/word/character fallback; very long unbroken token;
- empty/null/whitespace/trailing spaces/newlines;
- English, Vietnamese, Arabic RTL, Hebrew bidi, CJK, combining marks, surrogate pairs;
- missing font/fallback/italic overhang/underline;
- 8/10/24/72 pt, mixed baseline, non-default line height;
- 203/300/600 DPI, rotation and printable-edge clip;
- 1/100/10,000 records với worst-case string.

## 14. Ma trận phần cứng công nghiệp

Mỗi claim release phải nêu đúng matrix đã chạy, không ghi chung “hỗ trợ Zebra/TSC/SATO…” nếu mới test một model.

| Axis | Minimum coverage trước stable industrial claim |
| --- | --- |
| Vendor/form factor | pairwise Zebra/TSC/SATO; ít nhất desktop + industrial cho mỗi hãng trước claim tương ứng |
| DPI | exact 203/300/305/600/609 theo model; thêm non-square X/Y nếu driver báo; không làm tròn 305→300 hay 609→600 |
| Media | gap/die-cut; black mark; notch; continuous nếu product claim |
| Thermal | direct thermal và thermal transfer/ribbon theo claim |
| Connection | USB và TCP/IP; shared queue nếu khách hàng dùng |
| Size/orientation | 20–30 mm, 100×50, 102×152, tem hẹp/dài; portrait/landscape/180°/feed direction |
| Path | Windows driver; native language chỉ khi provider riêng được duyệt |
| Content | text small/large, line/shape, 1D, QR, Data Matrix, image |
| Volume | proof 1/10/100; endurance 500–1.000/model-stock; soak 8 giờ và synthetic/job batch 10.000 |
| Fault | paper out, cover open, offline/hot unplug, cancel, spooler restart |
| Measurement | X/Y offset, outer size, pitch/feed drift, scan; verifier grade khi có verifier |

Evidence record dùng format G7 và thêm:

- stock lot/material/ribbon anonymized;
- speed/darkness/print mode;
- sensor/calibration method;
- first/middle/last sample measurement trong soak;
- photos/scans/verifier report hoặc ghi rõ không có verifier;
- expected vs actual state semantics.

## 15. Chương trình triển khai Designer Precision

Không triển khai command UI trước khi command/scene seam tồn tại. Các ID dưới đây được tham chiếu từ [07-execution-plan.md](07-execution-plan.md).

### DP0 — Characterization và S0 safety

| ID | Outcome | Deliverable | Acceptance |
| --- | --- | --- | --- |
| DP-001 | Đóng băng hành vi hiện tại | fixtures drag/group/resize/nudge/zoom/selection/text | test mô tả đúng current behavior và known gaps, không “sửa test cho xanh” |
| DP-002 | Không mất selection khi zoom/rebuild | regression cho zoom, collection add/paste, preview-row | stable selected IDs/key sau visual invalidation; không dựa object element reference |
| DP-003 | Undo không trộn/cắt gesture | characterization timer 300 ms + cancellation | long drag không thành nhiều undo; action kế tiếp không merge; Esc không resurrect qua Undo |
| DP-004 | Text preview/print drift được nhìn thấy | parity fixtures cho static/bound Text và TextBox | mismatch có diagnostic; document hash không đổi khi đổi preview row |
| DP-005 | Không giữ object/template cũ và không rebuild thừa | event subscription/invalidation characterization | add/remove 1.000 vòng collect được; move không raster barcode/serialize toàn document mỗi tick |

DP0 có thể chạy trong S0 nhưng fix kiến trúc lớn phải chờ command/scene seam; không mở rộng mega-canvas bằng nhiều branch tạm nếu không cần để chặn data/output bug.

### DP1 — Geometry, command và snap foundation

| ID | Outcome | Deliverable | Acceptance |
| --- | --- | --- | --- |
| DP-101 | Geometry vocabulary chung | `LayoutBounds`, anchors, selection hull, line endpoints | unit tests rotated/line/group; WPF-free |
| DP-102 | Gesture transaction | begin/preview/commit/cancel transform session | một gesture/một command; cancel hash unchanged |
| DP-103 | Snap contract | immutable request/result + candidate/explanation | pure/deterministic tests, no UI/model reference |
| DP-104 | Spatial query seam | linear index có benchmark + replaceable interface | 500 nodes đạt budget hoặc ADR exception |
| DP-105 | Zoom-aware tolerance | acquire/release formula + preference | 25–800% matrix, ≤1 DIP contract error |
| DP-106 | Priority/hysteresis | stable candidate ranking + axis target locks | jitter/conflict/property-based tests |
| DP-107 | Core targets | artboard, object, grid, explicit guide, safe/printable area | overlay khớp applied delta |
| DP-108 | Multi-selection snap | selection hull/group transaction | internal offsets exact; locked policy fail-fast |
| DP-109 | Resize/draw snap | 8 handles, aspect/center/axis modifiers | min/max/artboard bounds, Esc/lost capture |
| DP-110 | Ruler transform | one pan/zoom source cho rulers/artboard/overlay | scroll/zoom/scale drift ≤1 DIP |
| DP-111 | Persistent guides | create/move/lock/delete/serialize metadata | excluded from print scene; undo/migration pass |
| DP-112 | Nudge precision | configurable fine step + 10× modifier | coalescing policy explicit, coordinate status accessible |

Exit DP1:

- move/resize/draw/nudge dùng transform session, không sửa document trong pointer preview;
- snap ổn định ở mọi zoom gate;
- group và resize không còn là path ngoại lệ;
- ruler/guide đúng viewport transform;
- old canvas còn feature flag rollback, nhưng không có hai implementation cùng mutate model.

### DP2 — Align, distribute, baseline và text layout

| ID | Outcome | Deliverable | Acceptance |
| --- | --- | --- | --- |
| DP-201 | Selection roles | primary/key/selected IDs + Shift-click/marquee contract | keyboard/mouse/accessibility tests |
| DP-202 | Align commands | six frame align commands + reference modes | exact command/undo hash; locked/group rules |
| DP-203 | Distribute commands | center/gap/exact-gap X/Y | size-mixed/overlap/order-shuffle tests |
| DP-204 | Smart spacing | row/column equal-gap candidates + badges | no false priority over explicit guide |
| DP-205 | Text model v2 | v0.113 persists `TextSizingMode.AutoFit/FixedFrame`; fixed frames share wrap/overflow/direction/h/v layout and remain stable during data/style edits | v1 migration fixtures, defaults documented, bounded render/preflight parity |
| DP-206 | Shared text layout result | metrics, line boxes, baseline, bounds, diagnostics | viewport/preview/print scene parity |
| DP-207 | Baseline snap/align | first-baseline, optional last-baseline | mixed font/size/RTL, ≤0.01 mm scene tolerance |
| DP-208 | Overflow all records | streaming/cancel/progress diagnostics | worst-record link, no eager bitmap/page render |
| DP-209 | Apply measured size | `AutoFit` remains the default measured mode; switching to `FixedFrame` makes the authored bounds authoritative without render-time mutation | one undo; render never persists bounds; overflow blocks preflight |
| DP-210 | Font fidelity | fallback chain, metrics hash, missing-font preflight | install/remove font fault tests |
| DP-211 | Problems remediation | select/reveal/overlay/suggest command | no auto mutation; keyboard usable |

Exit DP2:

- align/distribute/key-object behavior có tên và reference rõ;
- baseline khác frame top/bottom và dùng shared text metrics;
- mọi text node có sizing/overflow policy explicit;
- preview row không làm document geometry đổi;
- international/worst-record fixtures qua gate.

### DP3 — Advanced precision

- oriented anchors/align cho arbitrary rotation;
- optical align theo ink bounds, off mặc định;
- match size với node-specific resize policy;
- container/grid track anchors;
- spacing tokens/components;
- pen/touch profiles nếu product cần;
- collaborative guide/comment semantics chỉ sau local engine ổn định.

## 16. Chương trình triển khai Industrial Reliability

### IR0 — S0 correctness blockers

| ID | Outcome | Evidence/current risk | Acceptance |
| --- | --- | --- | --- |
| IR-001 | Một owner cho QR/matrix size | mutation đang lặp ở model/ViewModel/canvas | render/row change hash unchanged; explicit command/layout only |
| IR-002 | Clone fidelity | v0.107 `LabelObjectCloner` copies every persisted object/style/resource field and assigns a fresh ID | deep round-trip mọi object property/resource; source and clone style/resource graphs remain independent |
| IR-003 | Manual media size authoritative | preset selection có thể ghi đè manual W/H | manual/preset/orientation state matrix |
| IR-004 | Lazy preview | v0.099 đã chuyển metadata/lazy page + bounded STA raster/LRU; baseline workstation peak-RAM vẫn mở | 10k initial RAM target <300 MB, LRU 5–10 trang, cancel <1 s, progress |
| IR-005 | Async print/preflight | sync work không chặn UI | responsiveness + cancellation tests |
| IR-006 | Dot-space barcode correctness | DIP pixel snap không phải target DPI dots | golden 203/300/305/600/609 và non-square X/Y device geometry |
| IR-007 | Atomic persistence | v0.098 đã có atomic template save + versioned registry migration; fault injection chưa đủ | power-loss/corrupt/locked/disk-full fixtures |
| IR-008 | Command reentry safety | typed async commands/guards đã có ở các luồng chính; cần quét hết command path | typed async command, disable/cancel/error tests |
| IR-009 | Version/release consistency | v0.120 đồng bộ app/help/trial/commercial installer metadata; binary/install evidence vẫn cần | installed binary/source/release metadata match |
| IR-010 | Không fallback printer im lặng | v0.104–v0.106 direct path + pre-dispatch UI status reject blank/default substitution and named-queue disappearance | fail closed hoặc user xác nhận explicit mapping; manifest/log cùng queue identity |
| IR-011 | Không báo “printed” khi mới submit | v0.100+ ghi `SpoolAccepted/QueueObserved` tách khỏi physical output; device verification chưa có | capture spool job identity nếu có; rows không commit sai; operator recovery |
| IR-012 | Ticket/capability reconciliation | v0.116 bind effective ticket/output-contract và block hash drift; driver matrix còn mở | `PrintCapabilities` + validate/merge ticket read-only; mismatch quay lại preflight, không DEVMODE write |
| IR-013 | Calibration failure không bị nuốt | current path catch-all chỉ trả false | cancel/failure có diagnostic, remediation và không tạo profile revision/evidence giả |

IR0 là stop-the-line trước commercial/industrial claim mới.

### IR1 — Profile, device plan và preflight

| ID | Outcome | Deliverable | Acceptance |
| --- | --- | --- | --- |
| IR-101 | Versioned printer/stock profile | schema + evidence/verified state | migration/backup/import/export; no implicit OS state |
| IR-102 | Capability observation | read-only provider + timestamp/status | missing/stale/unsupported behavior; no DEVMODE write |
| IR-103 | Dot quantizer | shared boundary/integer module algorithms theo dpiX/dpiY | property/golden tests 203/300/305/600/609 + non-square |
| IR-104 | DeviceRenderPlan | immutable scene + effective output contract compile | deterministic hash, no WPF UI dependency |
| IR-105 | Printable/safe/media diagnostics | issue codes + remediation | profile/orientation/edge matrix |
| IR-106 | Font/image resolution | v0.108 text-resource identity + v0.115–v0.117 font/glyph gates; v0.120 image decode/64 MB+64 MP/PPI gate; **v0.137 image raster mode/dimension fingerprint and shared transform**; thermal 1-bpp/dither/colour-profile parity remains open | same text-resource/image identity in preview, dispatch and recovery; install/remove-font, stale-dimension, deterministic image-mode and device-raster fixtures |
| IR-107 | Barcode preflight | v0.123 profile/quiet-zone/GS1 software contract plus shared measured linear-HRI layout; fixed-version capacity, collision and verifier evidence remain open | profile + HRI frame fixtures and fail-closed preflight now; complete AI/vector/physical verifier evidence still required |
| IR-108 | All-record preflight | streaming validation | progress/cancel/worst-row links, bounded memory |
| IR-109 | Invalidation graph | generation IDs/content hashes | stale async result never publishes |
| IR-110 | Device pixel preview | exact dots + printable/safe overlay | matches DeviceRenderPlan hash |
| IR-111 | EffectiveOutputContract | requested→validated ticket→capabilities/imageable area artifact | coercion invalidates plan; dispatch hash equals reviewed preview/preflight hash |

Exit IR1:

- profile/media/DPI change invalidates đúng artifacts;
- preview và dispatch tham chiếu cùng DeviceRenderPlan;
- barcode/text/font/resource failure không im lặng;
- generic driver capability luôn ghi verified/unverified.

### IR2 — Durable job core và Operator flow

| ID | Outcome | Deliverable | Acceptance |
| --- | --- | --- | --- |
| IR-201 | Immutable job manifest | v0.109 `PrintJobManifest` captures normalized template/path/mode/queue, dimensions/DPI, counts and document/text/scene/output hashes; row payloads become an order-sensitive digest only | deterministic reload/fingerprint, dictionary-order stability, raw-value redaction |
| IR-202 | Durable job state machine | event store/transitions/recovery | allowed transition/property tests |
| IR-203 | Dispatch ownership | per-printer/serial ordering + backpressure | concurrent submit/order/load tests |
| IR-204 | Duplicate protection | UI debounce + idempotency/reprint contract | double-click/scanner/crash simulations |
| IR-205 | Truthful status | spool/device/unknown semantics | mock port monitor and real printer evidence |
| IR-206 | Cancel/recovery | state-aware cancel/reconcile | spooler restart/hot unplug/power-loss tests |
| IR-207 | Operator Print Center | v0.112 `PrintCenterWindow` adds scanner-friendly exact job-ID filtering, partial evidence search, focus/keyboard handling and guarded preview on top of the v0.111 recovery actions; no implicit retry or dispatch | real keyboard/scanner workflow, no designer access needed |
| IR-208 | Audit/reprint lineage | v0.110 adds durable `ReprintApproved` plus explicit manifest-guarded child dispatch API; v0.109 propagates manifest identity through queue evidence and linked child events | append-only query/export/privacy tests plus exact current data/template match before child dispatch |

### IR3 — Calibration và hardware certification

| ID | Outcome | Deliverable | Acceptance |
| --- | --- | --- | --- |
| IR-301 | Vendor-neutral assistant | checklist/pattern/measurement/profile revision | no hardware write; unit/UX tests |
| IR-302 | Hardware evidence harness | G7 form + artifact naming/hash | reproducible evidence bundle |
| IR-303 | Gap media certification | selected real models | calibration/feed/offset/soak evidence |
| IR-304 | Black-mark certification | selected real models | sensor position/profile/feed evidence |
| IR-305 | Continuous media certification | only if product claims | length/cut/tear behavior evidence |
| IR-306 | Barcode verification program | verifier/scanner/photo policy | claim wording matches evidence |
| IR-307 | Optional native backends | ZPL/TSPL/SBPL provider one-by-one | allowlisted hardware/firmware; rollback driver path |

Không bắt đầu IR-307 chỉ để quảng cáo tốc độ. Driver-based deterministic path và hardware matrix phải ổn trước.

## 17. Dependency và sequencing

```text
S0: DP-001..005 + IR-001..013
       ↓
R1: DocumentSnapshot + SceneCompiler + shared metrics/anchors
       ↓
R2-DP1: command sessions + retained viewport + Snap Engine
       ↓
R2-DP2: key-object align/distribute + text/baseline
       ├───────────────┐
       ↓               ↓
R3 UX shell       R5 IR1 DeviceRenderPlan/profile/preflight
                       ↓
                  R5 IR2 job core/Print Center
                       ↓
                  IR3 hardware certification
```

Không đưa align/distribute xuống R6 nữa: frame align, key object, distribute và baseline là năng lực editor nền tảng của R2. R6 chỉ giữ advanced layout/container/component/oriented precision.

## 18. Release gates dành riêng cho chương trình này

### Gate DP-A — Snap stability

- zoom/scale matrix pass;
- acquire/release/hysteresis pass;
- single/group/resize/draw/nudge pass;
- target explanation đúng;
- gesture cancel/undo hash pass;
- 500-node pointer budget có evidence.

### Gate DP-B — Alignment and text

- key/reference mode rõ;
- align/distribute deterministic/order-independent;
- baseline metrics shared;
- text sizing/overflow persisted và migrated;
- international/missing-font/all-record tests;
- viewport/preview/print scene parity.

### Gate IR-A — Device correctness

- profile/stock/version/verified state;
- dot quantizer 203/300/600;
- printable/media/font/barcode preflight;
- same DeviceRenderPlan hash cho preview/dispatch;
- no unsupported hardware write.

### Gate IR-B — Operational reliability

- durable manifest/state/recovery;
- duplicate/cancel/unknown outcome semantics;
- lazy/streaming memory budgets;
- fault injection pass;
- operator workflow keyboard/scanner pass.

### Gate IR-C — Hardware claim

- G7 evidence hoàn chỉnh theo model/media/DPI/path;
- proof + soak + fault cases;
- measurement and barcode evidence;
- release notes ghi giới hạn chính xác;
- không suy rộng model/hãng chưa test.

## 19. Definition of Done

Phần này chỉ được đánh dấu hoàn tất khi:

1. code không còn geometry mutation từ render/preview/property refresh;
2. snap/align/text dùng immutable scene metrics và semantic commands;
3. mọi gesture/align/distribute là một undo transaction;
4. baseline, vertical alignment, sizing và overflow là persisted explicit policy;
5. viewport/preview/print dùng cùng scene/device semantics;
6. 203/300/600 golden tests và zoom/scale/RTL/missing-font matrix pass;
7. preview/preflight streaming, cancel được và không block UI;
8. job status không nói quá khả năng Windows driver/printer feedback;
9. hardware claim có evidence đúng model/media;
10. memory, ADR, plan, quality gates và release notes đã cập nhật.

Build xanh riêng lẻ không đủ để đóng DP hoặc IR gate.
