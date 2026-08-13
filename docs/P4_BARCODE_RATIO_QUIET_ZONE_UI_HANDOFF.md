# P4 barcode ratio, density and physical quiet-zone UI handoff

**Status:** pre-implementation handoff; design and contract review required (2026-08-13)
**Parent spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P4
**Competitive matrix:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M3/M4/M14
**UI/UX specification:** [`P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md)
**Figma rule:** use the selected-Properties references in [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md); no barcode-specific P4 frame is recorded.

## 1. Operator task

An operator authoring a supported linear barcode must be able to:

1. choose a legal wide/narrow ratio when that symbology supports one;
2. understand the implied density as a read-only consequence of effective X-dimension and ratio;
3. see the quiet-zone requirement in physical millimetres at the current print DPI;
4. repair a ratio, X-dimension, quiet-zone or GS1 industrial warning before preview/print.

The first proposed optional-ratio slice is Code 39, followed by ITF only after its renderer and standards rules are covered. P4 must not expose a control that silently changes QR/Data Matrix geometry or treats density as a third size driver.

## 2. Current source evidence

| Surface | Current evidence | P4 gap |
| --- | --- | --- |
| Barcode Properties | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1878) exposes standard, application profile, quiet-zone modules, HRI, X-dimension, effective-module readout and `SizedFromX`. | No ratio selector, derived density readout or physical quiet-zone readout. |
| Model | [`LabelObject.cs`](../src/ANLAbel.Core/Models/LabelObject.cs#L410) persists X-dimension/width mode; quiet-zone modules are currently held by `QrQuietZoneModules`. | Add typed fields only through an explicit contract; do not overload QR naming for a linear-only ratio or physical measurement. |
| Shared X resolution | `LinearBarcodeModuleContract.Resolve` / `ResolveForObject` quantizes authored X to whole printer dots at print DPI. | Physical quiet zone must consume this result, not a second mm/dot formula in XAML. |
| Renderer | `IBarcodeRenderer`/ZXing count and render logical modules; current UI catalog includes Code 39 and ITF but the linear preflight path remains bounded. | Confirm per-symbology ratio support before exposing a selector; unsupported standards stay explicit. |
| Preflight | `PrintPreflightValidator` already validates module size and GS1 application geometry. | Add ratio legality and physical quiet-zone diagnostics only after Core/renderer contracts exist. |

## 3. Proposed contract boundary

| Concern | Proposed boundary | Must not do |
| --- | --- | --- |
| Ratio | Typed per-symbology ratio, with a legal set and a documented default; first candidate Code 39 | Do not expose one ratio value for every standard or silently clamp without status. |
| Density | Read-only derived presentation from effective X, ratio and symbol structure | Do not make density a third independent width/scale input. |
| Quiet zone | Physical width = quiet-zone modules × effective module width; show source DPI/effective X | Do not claim a physical measurement from authored modules alone or alter frame geometry automatically. |
| GS1 minimum | Use the existing application-profile/industrial preflight policy and name the required/observed mm | Do not convert a warning into certification or silently reduce the quiet zone. |
| Legacy files | Missing P4 fields use safe legacy defaults; authored width, quiet-zone modules and geometry remain unchanged | Do not migrate all existing barcodes to a new ratio or resize old frames. |
| 2D codes | Keep QR/Data Matrix module/ECC/quiet-zone paths separate | Do not show linear ratio/density controls for square 2D codes. |

## 4. State matrix

| State | Visible evidence | Safe action | Print rule |
| --- | --- | --- | --- |
| Linear standard, ratio not supported | Ratio control hidden/disabled with reason; existing X/QZ readout remains | Choose another supported standard or edit X/QZ | Continue normal preflight |
| Code 39/approved ratio standard | Legal ratio selector, effective X, derived density and physical QZ | Choose a legal ratio or edit X/QZ explicitly | Recompute one shared plan before preview/print |
| Invalid ratio value | Policy/renderer reason and legal choices | Select a legal value or restore default | Block until resolved |
| X-dimension legacy/zero | Explicit legacy/frame-owned notice; physical QZ is unavailable or frame-estimated per contract | Set authored X or keep legacy with honest status | Never invent a certified physical QZ |
| Effective X resolved | mm, mil/dots, print DPI and QZ mm are shown from one resolution | Inspect or change authored X | Preflight uses identical values |
| QZ below industrial minimum | Observed mm/modules, required threshold/profile and repair hint | Increase QZ/X or change profile deliberately | Block/warn according to existing fail-closed severity; no silent shrink |
| Ratio changes symbol geometry | Before/after derived module/width evidence | Review preview; preserve frame ownership unless `SizedFromX` is explicit | Vector/module pattern must reflect the selected ratio |
| GS1 profile | Required/observed QZ basis and application profile visible | Repair data/geometry | No “GS1 certified” or physical-grade claim |

## 5. Figma routing and acceptance

Use the selected-Properties shell references (`18:69` / `13:2`) only for grouping and compact status language. No dedicated ratio/density/physical-QZ state frame is required before design review; the owner may approve WPF reuse or request a state-specific node. Runtime screenshot/UI Automation at target sizes is the acceptance artifact.

Required gates before P4 implementation closure:

- ratio policy is covered for at least one optional-ratio symbology with legal/illegal fixtures;
- changing ratio changes controlled encoded geometry/module evidence;
- density is derived and cannot independently resize a symbol;
- physical QZ uses `effective X × modules` from the same print-DPI resolution as preflight;
- legacy `FrameOwned`, P1 X-dimension and P2 HRI gates stay green;
- QR/Data Matrix and protected Text/TextBox contracts remain unchanged;
- runtime evidence covers supported, unsupported, invalid and GS1-low-QZ states at `1024 × 600`, `100%`, `125%` and `150%` (or a documented exception);
- no physical verifier, native-printer or certification claim is made.

## 6. Owner decisions before coding

1. Confirm Code 39 as the first ratio symbology, or name the alternative and its legal set.
2. Confirm whether ratio is authored as a named preset, numeric value or both.
3. Approve density copy/units and the physical-QZ warning threshold/source.
4. Approve reuse of `18:69`/`13:2` or provide a state-specific Figma node.
5. Assign runtime screenshot/UI Automation ownership and regression fixture names.

Until these decisions are recorded, P4 remains a design/contract handoff and not an implemented barcode feature.
