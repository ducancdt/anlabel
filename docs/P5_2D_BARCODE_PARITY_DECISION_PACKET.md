# P5 QR / Data Matrix parity owner decision packet

**Status:** documentation-only decision packet; owner sign-off required before implementation
**Date:** 2026-08-13
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P5
**UI/UX handoff:** [`P5_2D_BARCODE_PARITY_UI_HANDOFF.md`](P5_2D_BARCODE_PARITY_UI_HANDOFF.md)
**UI/UX specification:** [`P5_2D_BARCODE_PARITY_UI_SPEC.md`](P5_2D_BARCODE_PARITY_UI_SPEC.md)
**Program index:** [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md)

This packet turns the open P5 questions into a bounded owner decision. It separates the shipped QR contract from the currently unsupported or ambiguous Data Matrix controls, recommends the smallest honest UI state, and lists the renderer/preflight evidence required before implementation. It does not add a model field, change matrix rendering, edit Figma, or claim P5 complete.

## 1. Decision requested

Approve or amend these seven decisions before implementation:

1. QR visible copy and the compatibility boundary for existing saved values;
2. Data Matrix sizing vocabulary and whether a fixed-size path is supported;
3. Data Matrix error-correction vocabulary and whether any selector is actionable;
4. standard-specific model/persistence fields versus the current QR-named fields;
5. matrix module/source-DPI/effective-dot and quiet-zone ownership;
6. unsupported-control treatment (hidden or disabled diagnostic with automatic fallback);
7. WPF/Figma/runtime and renderer-probe ownership.

The recommended bounded option is **preserve QR exactly, expose Data Matrix automatic sizing only until a renderer-backed size/EC contract exists, and show unsupported DM controls as disabled diagnostics with a safe automatic path**. This is a recommendation, not approval.

## 2. Source and design evidence

| Evidence | What is true today | Consequence for P5 |
| --- | --- | --- |
| Properties card | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1878) shows QR mode, EC level, quiet-zone modules, DPI and fixed `Version`/`Module px` controls for the shared Code 128/QR/Data Matrix card. | Copy and visibility must become standard-aware; a Data Matrix object must not see editable QR semantics. |
| Object model | [`LabelObject`](../src/ANLAbel.Core/Models/LabelObject.cs#L205) persists `QrSizingMode`, `QrErrorCorrection`, `QrFixedVersion`, `QrModuleSizePx`, `QrQuietZoneModules` and `QrDpi` for matrix objects. | Do not bind a new DM selector to `QrFixedVersion` or `QrErrorCorrection`; future DM fields require explicit types and migration defaults. |
| Snapshot/clone | [`DocumentSnapshot`](../src/ANLAbel.Core/Scene/DocumentSnapshot.cs#L225) and [`LabelObjectCloner`](../src/ANLAbel.Core/Models/LabelObjectCloner.cs) carry the existing QR-named fields. | Existing QR values and geometry are data; preserve them byte-for-byte in the compatibility scope. |
| QR sizing contract | [`QrSizingCalculator`](../src/ANLAbel.Core/Barcode/QrSizingCalculator.cs) and [`QrObjectGeometryContract`](../src/ANLAbel.Core/Barcode/QrObjectGeometryContract.cs) calculate QR version/module/capacity and are reached through `IsSquare2DCodeLike()`. | The shared square-code path is not proof of Data Matrix version/size semantics; an explicit standard branch is required before claiming parity. |
| Renderer | [`ZxingBarcodeRenderer`](../src/ANLAbel.Barcode/Renderers/ZxingBarcodeRenderer.cs) maps `ErrorCorrection` only for QR; Data Matrix receives `DATA_MATRIX_SHAPE = FORCE_NONE`. No DM EC or symbol-size option is passed. | A DM EC/size selector is currently unavailable; attach an engine/renderer probe and contract before making it editable. |
| Matrix preflight | [`PrintPreflightValidator`](../src/ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs#L142) shares the fixed-matrix sub-2-dot warning, while fixed-version byte-capacity validation is gated to `ObjectType.QRCode`. | Reuse the standard-neutral physical module warning only after confirming DM module semantics; do not reuse QR capacity messages for DM. |
| Existing tests | QR sizing/capacity, fixed-module DPI and shared matrix warnings are covered; no first-class DM size/EC/capacity contract or unavailable-control UI test exists. | P5 needs explicit QR regression locks and DM supported/unsupported fixtures. |
| Figma panels | Read-only metadata for panels file `kqyNBI0DgRHnPzJTDBIui5`, Page `0:1`, shows selected Properties `13:2` and tabbed Properties `18:69`; neither contains QR/DM, capacity, unsupported or error states. | Reuse is limited to grouping/density. Figma cannot prove standard semantics or print-DPI behavior. |

## 3. Owner decision matrix

### D1 — QR copy and compatibility

| Option | Operator copy | Compatibility risk | Recommendation |
| --- | --- | --- | --- |
| `Symbol version` | Neutral label in the QR-only row; helper names `QR` in the description. | Could be too generic when a future DM size row is adjacent. | **Recommended**, with `QR symbol version` as the fallback if review finds ambiguity. |
| `QR symbol version` | Explicitly names QR in every state. | Longer copy at `1024 × 600`; no semantic risk. | Acceptable alternative. |

Keep `QrSizingMode`, `QrFixedVersion`, `QrErrorCorrection`, `QrModuleSizePx`, `QrQuietZoneModules` and the existing QR capacity table unchanged for legacy files. A copy-only change must not reinterpret saved values or change auto-size/fixed-size geometry.

**Owner record:** `QR copy selected: ____________________`  **Owner/date:** ____________________

### D2 — Data Matrix sizing contract

| Option | Meaning | Evidence required | Recommendation |
| --- | --- | --- | --- |
| Automatic/data-driven only | Renderer chooses a supported symbol size from the data; UI reports the actual result if the engine exposes it. | Probe output, resolved size/module count and preflight fit behavior. | **Recommended current boundary.** |
| Fixed supported size | Operator selects a named DM symbol size; renderer and preflight enforce it. | Complete size table, capacity/fit, save/load and vector/module fixtures. | Defer until contract exists. |
| Reuse QR version | Treat `QrFixedVersion` as DM size. | No valid evidence in current renderer; would be misleading. | **Forbidden.** |

**Owner record:** `DM sizing contract: automatic / fixed supported / amended: __________`  **Owner/date:** ____________________

### D3 — Data Matrix EC/error-correction semantics

The current renderer does not consume `BarcodeRenderOptions.ErrorCorrection` for Data Matrix. Therefore the safe default is `Not available in this renderer` with no editable selector. If an owner selects a future DM EC policy, the packet must be amended with its value vocabulary, engine mapping, preflight rule, capacity behavior and fixtures before XAML changes.

**Owner record:** `DM EC policy/source: ____________________`  **Owner/date:** ____________________

### D4 — Model and persistence boundary

Do not overload QR-named fields. If a DM fixed size/EC path is approved, use explicit per-standard fields (illustrative names only):

- `DataMatrixSizingMode` (`Automatic` / approved fixed mode);
- `DataMatrixSymbolSize` (typed supported size, not a QR version integer);
- `DataMatrixErrorCorrection` only if the renderer exposes a real policy;
- a resolved-size/status value kept diagnostic rather than authored data unless the owner explicitly persists it.

Legacy objects with no DM fields keep automatic/engine-default behavior and all existing geometry. Clone, save/load and document snapshot tests must prove QR fields are unchanged and DM defaults are deterministic.

**Owner record:** `DM field/migration shape approved or deferred: ____________________`  **Owner/date:** ____________________

### D5 — Matrix module, DPI and quiet-zone ownership

| Concern | Bounded rule | Must not do |
| --- | --- | --- |
| Fixed module | Effective dots use `module px × print DPI / source DPI` when a fixed module is explicitly supported. | Do not report authored pixels as printer dots or silently stretch into an undersized frame. |
| Automatic module | Report only renderer-backed size/module evidence; keep the existing auto path authoritative. | Do not invent QR capacity or a DM size from frame dimensions. |
| Quiet zone | Keep logical modules and the shared render/preflight option; any physical-mm readout follows the separately approved P4 convention. | Do not mix P4 linear side/total language into the 2D control or migrate authored margins. |
| Frame ownership | Preserve existing QR/object geometry and `FrameOwned`/explicit sizing rules. | Do not resize a frame solely to make a DM control appear valid. |

**Owner record:** `Matrix module/QZ contract and P4 dependency approved: ____________________`  **Owner/date:** ____________________

### D6 — Unsupported controls and status copy

| State | Recommended UI | Safe action | Print rule |
| --- | --- | --- | --- |
| QR auto/fixed valid | QR-only sizing/version/EC controls, capacity/fit and effective dots | Edit data or approved QR fields | Existing QR preflight remains authoritative |
| QR fixed overflow/undersized | Byte count, version/ECC, required frame/module and repair action | Increase version/module/frame or use auto | Block affected row under existing rules |
| DM automatic supported | `Data Matrix size: Automatic`, renderer-backed module/QZ/DPI status | Keep automatic or edit only approved fields | Use actual renderer result; no QR capacity claim |
| DM fixed/EC unsupported | Disabled diagnostic: `Not available in this renderer`; automatic fallback remains explicit | Keep automatic or cancel unsupported edit | Never print as if requested value applied |
| Non-2D standard | Hide the 2D section | Edit standard-specific linear controls | Existing linear path unchanged |
| Empty/invalid/stale binding | Source/binding status separate from size/EC status | Repair source and revalidate | Block when encoded data is not deterministic |

**Owner record:** `Hidden vs disabled diagnostic and copy approved: ____________________`  **Owner/date:** ____________________

### D7 — UI, Figma and runtime ownership

| Concern | Bounded recommendation | Required owner evidence |
| --- | --- | --- |
| WPF surface | Keep one 2D card, but render standard-specific rows and status; QR and DM must have distinct labels and bindings. | Named App/MainWindow owner, AutomationIds and keyboard order. |
| Figma reference | Reuse `18:69` / `13:2` for grouping and density only. | Explicit reuse approval, or the smallest QR/DM state-specific node if the shell cannot answer a concrete question. |
| Figma write | None for this packet; no new frame is needed to document missing DM semantics. | Any later write requires an explicit UI task and Figma write review. |
| Runtime evidence | Capture QR auto/fixed success/overflow, DM automatic, unsupported-control, invalid binding and sub-2-dot states at `1024 × 600`, `100%`, `125%`, `150%` (or document an exception). | Named screenshot/UIA owner, focus/keyboard/scroll result and artifact paths. |
| Physical claim | Keep software matrix status separate from native-printer/verifier evidence. | No P5 approval may claim physical verification or 2D certification. |

**Owner record:** `WPF owner: __________  Figma route: reuse / state node  Runtime owner: __________  Date: __________`

## 4. Implementation-ready fixture matrix

These fixtures are required names/assertions, not authorization to code before D1–D7 are signed:

| Fixture | State | Required assertion |
| --- | --- | --- |
| `Qr_copy_change_preserves_capacity_and_geometry` | Legacy QR auto/fixed | Visible copy changes only; capacity, module math, saved values and geometry remain identical. |
| `Qr_fixed_overflow_fails_closed` | Fixed version/ECC overflow | Byte count, capacity and repair action are named; data is not truncated or silently auto-switched. |
| `Qr_effective_dots_use_plan_dpi` | Fixed module, source/plan DPI differ | Effective dots match the shared conversion and the sub-2-dot warning remains unchanged. |
| `DataMatrix_automatic_uses_renderer_result` | DM automatic | Status names actual renderer-supported size/module result; no QR capacity claim appears. |
| `DataMatrix_unsupported_size_is_not_actionable` | Requested fixed size without contract | Control is disabled/diagnostic or edit is rejected; renderer receives no silently ignored value. |
| `DataMatrix_unsupported_ec_is_not_qr_ec` | DM with QR EC field selected in legacy UI | QR EC is not labelled or applied as DM EC; safe automatic path remains explicit. |
| `Matrix_quiet_zone_round_trip_preserves_authored_value` | QR and DM legacy objects | Logical QZ and geometry survive clone/save/load/snapshot; no P4 physical convention is mixed in. |
| `DataMatrix_sub_two_dot_warning_is_honest` | Fixed DM module if supported | Warning uses confirmed source/plan DPI semantics; otherwise status says support is unavailable. |
| `P5_non_matrix_controls_are_not_actionable` | Linear barcode selected | 2D controls are hidden and linear behavior remains unchanged. |

## 5. Ready / not-ready gates

P5 is **ready for implementation** only after:

- D1–D7 have owner/date entries and the handoff/spec reflect amendments;
- a renderer probe proves the selected Data Matrix sizing/EC vocabulary or explicitly confirms automatic-only support;
- QR capacity and geometry are locked by existing tests, with copy changes separated from contract changes;
- one standard-aware result owns matrix size/module/QZ/status for designer, preview, preflight and print;
- unsupported controls have explicit accessibility copy and a safe automatic path;
- legacy QR fields/geometry, P0–P4 gates and protected Text/TextBox gates remain in regression scope;
- runtime screenshot/UIA ownership and target-scale artifacts are named;
- no Figma shell is treated as proof of a Data Matrix capability.

P5 remains **not ready** when Data Matrix is given a QR version/EC selector, when unsupported controls silently do nothing, when a QR capacity message is shown for DM, or when automatic frame resizing is introduced solely to make the card appear symmetric.

## 6. Sign-off

| Decision | Owner | Date | Approved / amended | Evidence link |
| --- | --- | --- | --- | --- |
| D1 QR copy/compatibility |  |  |  |  |
| D2 DM sizing contract |  |  |  |  |
| D3 DM EC policy/source |  |  |  |  |
| D4 DM fields/migration |  |  |  |  |
| D5 matrix module/QZ ownership |  |  |  |  |
| D6 unsupported-control state/copy |  |  |  |  |
| D7 UI/Figma/runtime ownership |  |  |  |  |

Until this table is completed, this packet is a review aid only. The next safe action is to run the renderer capability probe and obtain owner decisions; no Text/TextBox contract change is involved.
