# P4 barcode ratio / density / physical quiet-zone owner decision packet

**Status:** Code 39 software slice implemented; runtime scale evidence pending
**Date:** 2026-08-14
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P4
**UI/UX handoff:** [`P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md)
**UI/UX specification:** [`P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md)
**Program index:** [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md)

This packet turns the open P4 questions into a bounded owner decision. It records what the current source, tests and read-only Figma metadata prove, recommends a safe first slice, and names the evidence required before adding ratio or physical quiet-zone UI. It does not add a model field, change renderer behavior, edit Figma, or claim P4 complete.

## 1. Decision requested

Approve or amend these six decisions before implementation:

1. first ratio-enabled symbology and legal set;
2. ratio representation (named preset, numeric value, or both);
3. density copy and whether a numeric density value is warranted;
4. quiet-zone side/total convention and renderer meaning;
5. physical-QZ warning threshold/severity and legacy-X treatment;
6. WPF/Figma/runtime evidence ownership.

The approved bounded option is **Code 39 first**, with a typed legal ratio policy, no independent density input, and a per-side physical quiet-zone readout. The existing ZXing writer does not expose a ratio input, so implementation must add a tested Code 39 geometry seam; a XAML-only selector is explicitly disallowed.

## 1.1 Research record (2026-08-14)

- ISO identifies ISO/IEC 16388:2023 as the current Code 39 symbology specification. Its public catalogue describes it as covering dimensions and tolerances.
- The accessible USS-39 specification states that wide:narrow ratio is 2.0:1–3.0:1 when X is at least 0.508 mm, and 2.2:1–3.0:1 below that X; it requires a quiet zone of at least 10X or 2.54 mm, whichever is greater, on both leading and trailing sides.
- ZXing's `Code39Writer` has no ratio encode hint. Therefore P4 must not present a ratio that does not reach the rendered vector.

Sources: [ISO/IEC 16388:2023 catalogue](https://www.iso.org/es/contents/data/standard/07/77/77799.html), [USS-39 specification](https://www.expresscorp.com/wp-content/uploads/2023/02/USS-39.pdf), [ZXing Code39Writer](https://github.com/zxing/zxing/blob/master/core/src/main/java/com/google/zxing/oned/Code39Writer.java).

## 2. Source and evidence boundary

| Evidence | What is true today | Consequence for P4 |
| --- | --- | --- |
| Properties surface | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1878) exposes `Quiet zone (modules)`, HRI, X-dimension, `SizedFromX`, effective-module readout and validation. | There is no ratio selector, density readout or physical-QZ readout to preserve as an existing contract. |
| Object model/snapshot | [`LabelObject`](../src/ANLAbel.Core/Models/LabelObject.cs#L410) and [`DocumentSnapshot`](../src/ANLAbel.Core/Scene/DocumentSnapshot.cs#L225) persist X-dimension, width mode and `QrQuietZoneModules`; no ratio or physical-QZ field exists. | New fields need explicit Core types plus clone/save/load/snapshot/migration coverage. Do not rename or migrate the existing QR-prefixed quiet-zone property in place. |
| Profile quiet-zone policy | [`BarcodeApplicationContract.GetRequiredQuietZoneModules`](../src/ANLAbel.Core/Barcode/BarcodeApplicationContract.cs#L26) returns `0` for General, `10` for linear Industrial/GS1, `1` for Data Matrix GS1 and `4` for other matrix GS1; validation compares authored modules with that requirement. | Existing profile severity and module requirement are authoritative until an owner approves a physical-mm presentation. This is not an ISO/ANSI verifier claim. |
| X resolution | [`LinearBarcodeModuleContract`](../src/ANLAbel.Core/Barcode/LinearBarcodeModuleContract.cs) quantizes authored X to whole dots at plan DPI and can estimate from the frame when X is zero; explicit `SizedFromX` is the only shipped width-owning path. | Physical QZ must consume the same resolved X/DPI result; ratio or density must not create a second width mutation path. |
| Renderer options | [`BarcodeRenderOptions`](../src/ANLAbel.Barcode/Options/BarcodeRenderOptions.cs) carries quiet-zone modules, QR error correction and GS1; [`ZxingBarcodeRenderer`](../src/ANLAbel.Barcode/Renderers/ZxingBarcodeRenderer.cs) maps quiet zone to the ZXing margin hint but has no ratio option. | Ratio support and margin side semantics require a renderer/standards probe before exposing an actionable control. |
| Renderer seam | [`IBarcodeRenderer`](../src/ANLAbel.Barcode/Renderers/IBarcodeRenderer.cs) exposes render, validate and logical-module count, not ratio metadata or physical-QZ resolution. | A future contract must supply one ratio/module/QZ result to designer, preview, preflight and print; do not calculate a UI-only approximation. |
| Existing tests | Unit/application tests cover profile module requirements, X quantization, logical module count, `SizedFromX` and legacy `FrameOwned`; no ratio or physical-QZ test exists. | P4 fixtures are new gates; keep all P0–P2 and protected Text/TextBox gates unchanged. |
| Figma panels | Read-only metadata for panels file `kqyNBI0DgRHnPzJTDBIui5`, Page `0:1`, shows selected Properties `13:2` and tabbed Properties `18:69`, each `300 × 700`; the only barcode hint is ribbon text `1:8`. | Reuse is a spacing/grouping decision only. There is no P4 ratio, density, QZ or disabled/error state to treat as a design specification. |

## 3. Owner decision matrix

### D1 — First ratio-enabled symbology

| Option | Evidence/benefit | Missing proof or risk | Recommendation |
| --- | --- | --- | --- |
| **Code 39** | Already the first proposed P4 slice; present in the catalog and renderer; isolates ratio work from QR/Data Matrix and GS1 AI parsing. | Current ZXing path has no ratio option; the exact legal set and output effect need a standards/renderer fixture. | **Recommended first slice.** |
| ITF | Present in the catalog and relevant to industrial labels. | Ratio, bearer-bar and check-digit interactions are not represented; a generic Code 39 decision must not be copied to ITF. | Defer until its own legal set and fixtures exist. |
| All linear standards | Broad apparent coverage. | Would expose no-op or incorrect controls for standards whose ratio is engine-owned or not supported by the current renderer. | Not ready for P4. |

**Owner record:** `Selected symbology: Code 39 only`  **Owner/date:** Product owner / 2026-08-14

### D2 — Ratio representation

| Option | Operator behavior | Risk | Recommendation |
| --- | --- | --- | --- |
| Named legal preset(s) | Choose only values the selected standard/renderer accepts; the UI can explain `Standard`, `Wide` or equivalent only after the legal labels are confirmed. | Requires a standards-backed label/value table; avoid inventing names or values in XAML. | **Recommended first UI.** |
| Numeric value only | Direct numeric authoring with validation. | Easy to enter an illegal value; freeform rounding/clamping can change encoded geometry without a clear state. | Defer unless the standards source requires numeric authoring. |
| Preset + advanced numeric override | Covers both workflows. | Larger contract, migration and accessibility surface; not justified before one standard is proven. | Defer to a later slice. |

The persisted value should be typed and per object, with a documented legacy-safe default that preserves the current engine behavior. Invalid values must fail closed or remain unapplied; never silently clamp to a different ratio.

**Owner record:** `Representation: named presets 2.2:1, 2.5:1, 3.0:1; 2.0:1 only when effective X is at least 0.508 mm; invalid states fail closed.`  **Owner/date:** Product owner / 2026-08-14

### D3 — Density and effective-X presentation

Recommend keeping density **read-only** and subordinate to the existing effective-X line. The first UI should always show source DPI, effective X in mm/mil/dots and the selected ratio when the contract is resolved. A numeric density value is optional and must not become a third size input; the owner must approve its formula, units and copy from a standards/renderer source.

Safe default copy: `Derived density` plus a tooltip explaining that it is derived from the resolved X/ratio/symbol structure. If the renderer cannot provide a trustworthy ratio-aware density, show `Density unavailable` rather than a frame-derived guess.

**Owner record:** `Density is deferred from the first code slice: show existing effective-X data only, never a third input or an unverified density calculation.`  **Owner/date:** Product owner / 2026-08-14

### D4 — Quiet-zone convention

The owner must choose one convention and use it everywhere:

| Convention | Meaning | Required evidence |
| --- | --- | --- |
| Per side | `quietZoneModules × effectiveModuleWidthMm` on each left/right margin; total width may be shown as a secondary derived value. | Renderer probe confirms ZXing margin semantics and the selected standard's requirement is expressed per side. |
| Total | The displayed physical value covers both sides together. | Core/renderer contract converts the engine margin to a total without double-counting. |

**Recommended:** user-facing **per side**, with `modules`, `mm`, effective X and DPI visible together; retain a separate total only if it prevents operator ambiguity. The recommendation must not be implemented until the renderer probe and standards source are attached.

General profile remains `no profile minimum`; Industrial/GS1 linear profiles retain the existing required `10` logical modules. A physical value is a measurement/readout from the shared print plan, not certification.

**Owner record:** `Per side. Physical requirement is max(10 × effective X, 2.54 mm) per side for the Code 39 P4 slice; renderer geometry must carry the same margin convention.`  **Owner/date:** Product owner / 2026-08-14

### D5 — Threshold, severity and legacy X=0

| State | Recommended behavior | Must not claim |
| --- | --- | --- |
| Explicit X > 0 | Quantize through `LinearBarcodeModuleContract.Resolve`; compute physical QZ from the effective result and show plan DPI. | Do not use authored X before quantization or a second UI formula. |
| Legacy X = 0 / `FrameOwned` | Preserve frame and quiet-zone modules; show physical QZ as `Unresolved (legacy frame estimate)` or an equally honest state until the owner approves an estimate label. | Do not call the result verified physical measurement. |
| Below existing profile modules | Reuse `BarcodeApplicationContract`'s profile requirement and current fail-closed behavior. | Do not silently raise/lower modules or change GS1 policy. |
| Physical QZ below a new threshold | Add a typed severity only after the owner names the threshold/source; show observed, required and repair action. | Do not invent ISO/ANSI grade or force `Print anyway`. |

**Owner record:** `Below the Code 39 physical minimum blocks print preflight; legacy X=0 remains unresolved and preserves existing geometry.`  **Owner/date:** Product owner / 2026-08-14

### D6 — UI, Figma and runtime ownership

| Concern | Bounded recommendation | Required owner evidence |
| --- | --- | --- |
| WPF surface | Extend the existing Barcode Properties card with a linear-only section: ratio → density/effective X → physical QZ → validation. | Named App/MainWindow owner, AutomationIds and keyboard order. |
| Figma reference | Reuse `18:69` and `13:2` for compact grouping/status language only. | Explicit reuse approval, or a smallest state-specific reference for supported/unsupported/invalid/low-QZ states. |
| Figma write | None for this packet; no new frame is needed merely to document a gap. | A later write requires an explicit UI task and Figma write review. |
| Runtime evidence | Capture supported, unsupported, invalid-ratio, legacy-X and GS1-low-QZ states at `1024 × 600`, `100%`, `125%`, `150%` (or document an exception). | Named screenshot/UIA owner, focus/keyboard/scroll result and artifact paths. |
| Physical claim | Keep software readout, preflight, thermal-golden and verifier evidence separate. | No P4 approval may claim physical verification or printer certification. |

**Owner record:** `WPF owner: ANLAbel implementation  Figma route: reuse 18:69 shell only  Runtime owner: pending available Windows automation  Date: 2026-08-14`

## 4. Implementation-ready fixture matrix

These fixtures are required names/assertions, not authorization to code before D1–D6 are signed:

| Fixture | State | Required assertion |
| --- | --- | --- |
| `Code39_ratio_legal_changes_geometry` | Approved legal ratio | Controlled payload shows a changed encoded module/vector or logical-width result; no silent frame mutation under `FrameOwned`. |
| `Code39_ratio_illegal_fails_closed` | Illegal/unsupported ratio | UI and preflight name the legal choices; renderer is not called with a silently clamped value. |
| `Linear_density_is_read_only` | Supported ratio + X | Density/effective readout changes when X/DPI/ratio changes but has no setter or independent width path. |
| `Physical_qz_per_side_matches_plan` | Explicit X, known DPI and QZ modules | Readout equals the approved side/total convention from the same quantized X used by preflight. |
| `Legacy_frame_owned_qz_is_honest` | X = 0 / `FrameOwned` | Authored width/modules remain unchanged; UI does not label the frame-derived result as a verified physical measurement. |
| `Industrial_qz_threshold_reuses_profile` | Industrial/GS1 linear profile | Required logical modules and severity match `BarcodeApplicationContract`; no silent reduction or certification claim. |
| `SizedFromX_ratio_does_not_duplicate_width_owner` | Explicit `SizedFromX` | First Code 39 slice fails closed because the existing module-count seam is integer-only; it must not invent a fractional-ratio width. |
| `BarcodeP4_clone_save_legacy_round_trip` | New and legacy objects | Ratio/physical-QZ fields round-trip after approval; missing fields preserve authored geometry and current quiet-zone semantics. |
| `P4_non_linear_controls_are_not_actionable` | QR/Data Matrix/Aztec/PDF417 | Ratio/density controls are hidden/disabled with a reason; square-module/ECC paths are unchanged. |

## 5. Ready / not-ready gates

P4 is **ready for implementation** only after:

- D1–D6 have owner/date entries and the handoff/spec reflect any amendments;
- a standards/renderer probe proves the selected ratio legal set and the quiet-zone margin convention;
- one Core/Printing result owns ratio, logical modules, effective X, physical QZ and severity for designer, preview, preflight and print;
- density is explicitly read-only and cannot mutate width or frame geometry;
- legacy X=0, `FrameOwned`, `SizedFromX`, GS1 profile and 2D unavailable states are named;
- AutomationIds, keyboard order, target-scale runtime owner and fixture artifacts are assigned;
- P0–P2 barcode gates and protected Text/TextBox gates remain in the regression scope.

P4 remains **not ready** when ratio values are invented in XAML, when per-side and total QZ conventions differ between surfaces, when X=0 is presented as verified physical data, when density is a hidden width driver, or when generic Figma shells are treated as shipped barcode states.

## 6. Sign-off

| Decision | Owner | Date | Approved / amended | Evidence link |
| --- | --- | --- | --- | --- |
| D1 first ratio symbology/legal set | Product owner | 2026-08-14 | Code 39, conditional legal set | §1.1 |
| D2 ratio representation | Product owner | 2026-08-14 | named presets; no silent clamp | §3 |
| D3 density copy/units/formula | Product owner | 2026-08-14 | deferred; effective-X remains read-only | §3 |
| D4 quiet-zone side/total convention | Product owner | 2026-08-14 | per side; max(10X, 2.54 mm) | §3 |
| D5 threshold/severity/legacy X=0 | Product owner | 2026-08-14 | block below physical minimum; legacy unresolved | §3 |
| D6 UI/Figma/runtime ownership | Product owner | 2026-08-14 | WPF reuse; runtime evidence pending automation | §3 |

Until this table is completed, this packet is a review aid only. The next safe action is to obtain the standards/renderer probe and owner decisions; no Text/TextBox contract change is involved.
