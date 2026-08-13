# ANLAbel — P5 2D barcode parity UI/UX specification

**Status:** documentation-only, pre-implementation UI/UX contract (2026-08-13)
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P5
**Handoff:** [`P5_2D_BARCODE_PARITY_UI_HANDOFF.md`](P5_2D_BARCODE_PARITY_UI_HANDOFF.md)
**Owner decision packet:** [`P5_2D_BARCODE_PARITY_DECISION_PACKET.md`](P5_2D_BARCODE_PARITY_DECISION_PACKET.md)
**Research gaps:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M16/M17
**Figma boundary:** selected-Properties shell from `18:69` / `13:2`; no P5-specific barcode frame is present

This document defines a standard-aware 2D authoring surface. It preserves the shipped QR capacity/module policy, gives Data Matrix a first-class path only where the renderer supports it, and makes unsupported semantics explicit. It does not add model fields, edit Figma, change barcode rendering or claim P5 complete.

## 1. Product outcome

The operator should be able to answer four questions without opening a separate manual:

1. Which 2D standard is selected?
2. Is its size determined by data or by an explicit symbol/module choice?
3. What version/size, ECC/EC, quiet zone and effective print dots will be used?
4. Is the current data/frame/renderer combination safe to preview and print?

The panel must not make QR and Data Matrix appear to share a version or ECC model when the engine does not support that equivalence.

## 2. Existing UI and Figma evidence

The current card in [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1878) is visible for Code 128, QR Code and Data Matrix. It currently places QR mode, EC level, quiet-zone modules, DPI and fixed `Version`/`Module px` controls in one broad card. That is the primary P5 copy/visibility risk.

The current model and renderer evidence is:

| Area | Evidence | Design implication |
| --- | --- | --- |
| QR state | `QrSizingMode`, `QrErrorCorrection`, `QrFixedVersion`, `QrModuleSizePx`, `QrQuietZoneModules`, `QrDpi`; QR capacity is checked for fixed version | Keep exact QR behavior; improve copy and state visibility only in this docs slice |
| Data Matrix state | `ObjectType.DataMatrix` and ZXing rendering exist, but no typed DM size/EC fields are present | Do not invent a version/EC contract in XAML |
| Matrix physical policy | `ValidateBarcodeModuleSizeAtPrintDpi` checks fixed matrix modules against print DPI and the shared `IsSquare2DCodeLike()` predicate | Reuse only the physical module rule that is actually standard-neutral |
| Figma shell | Panels node `18:69`: `300 × 700`, selected Properties, `284 DIP` content cards; node `13:2` is another `300 × 700` compact Properties shell | Shell spacing/status language only; no barcode-specific meaning or state |

## 3. Standard-aware control contract

| Control | QR Code | Data Matrix | Visibility/ownership rule |
| --- | --- | --- | --- |
| Standard | `QR Code` | `Data Matrix` | Always visible for a 2D object |
| Sizing mode | `Auto size by data`; `Fixed symbol version + module` | `Automatic`; approved fixed size/module only if supported | Do not use a QR enum label for DM |
| Symbol version/size | `Symbol version 1–40` with module-count hint | Renderer-supported size name/count; otherwise disabled diagnostic | Never bind a DM size selector to `QrFixedVersion` |
| Error correction/EC | `L/M/Q/H`; existing QR mapping | Only an engine-supported DM EC policy; otherwise `Not available` | QR EC list is hidden for DM |
| Module size | Module px, source DPI and effective print dots | Same physical readout when fixed module is supported | Shared calculation, standard-specific validation |
| Quiet zone | Logical modules | Logical modules | Preserve authored value; use one render/preflight option |
| Capacity/fit | Exact QR byte-mode capacity and frame checks | Renderer-supported fit/size result; no QR capacity claim | Status must identify the standard and source |

## 4. Host-neutral wireframe

```text
2D Barcode
Standard              [QR Code ▼]
Application profile   [General ▼]

Sizing mode           [Auto size by data ▼]
Symbol version        [Auto / Version 3 ...]
Error correction      [M - Medium ▼]
Module                [6 px ▼]   Source DPI [300]
Effective module      [2.0 dots × 2.0 dots @ 100 DPI]
Quiet zone            [4 modules]

Capacity / fit        [42 / 100 bytes · Fits]
Print preflight       [Ready]
```

Data Matrix changes the standard-specific rows to:

```text
Standard              [Data Matrix ▼]
Sizing mode           [Automatic ▼]
Data Matrix size      [Automatic / supported size ▼]
Error correction/EC   [Not available in this renderer]
Module / source DPI   [6 px] [300]
Effective dots        [2.0 × 2.0 @ print DPI]
Quiet zone            [4 modules]
Renderer support      [Automatic path supported]
Print preflight       [Ready / warning / blocked]
```

The second wireframe is intentionally honest: a disabled/unavailable EC row is preferable to a QR-shaped control whose value has no effect.

## 5. State and severity contract

| State | Required UI | Severity | Repair |
| --- | --- | --- | --- |
| Empty/unbound | `Waiting for data`; no stale capacity or fit | Informational | Enter data or repair binding |
| QR auto-size valid | Resolved symbol version, ECC, QZ, effective dots and `Fits` | Ready | Continue |
| QR fixed capacity overflow | UTF-8 bytes, selected version/ECC, capacity and action | Block row | Increase version, reduce data or use auto-size |
| QR frame too small | Required size, authored size and action | Block row | Resize frame or change sizing policy |
| DM automatic supported | Renderer support, QZ/module/DPI readout and fit | Ready | Continue |
| DM fixed size unsupported | Disabled explanation and automatic fallback | Warning/blocked edit | Keep automatic or cancel edit |
| Fixed matrix sub-2-dot | Effective X/Y dots, plan DPI/source DPI and repair | Block/warn per existing policy | Increase module/source DPI or use automatic sizing |
| Invalid binding/stale result | Exact field/formula/refresh reason; status not green | Block affected row | Repair and re-evaluate |

No state may show a green `Ready` status while its source data, renderer support or plan DPI is unknown.

## 6. Interaction, copy and persistence rules

1. `QR mode` becomes `Sizing mode`; `Version` becomes `Symbol version` (or `QR symbol version` after owner decision). `EC level` is shown only for QR.
2. Changing Standard recalculates control applicability before the next edit. Existing saved values are preserved for round-trip safety but are not presented as active semantics for the other standard.
3. A Data Matrix size/EC control is editable only when the selected renderer and preflight contract support it. Otherwise the disabled copy names the missing capability and offers the automatic path.
4. Fixed-module effective dots use `modulePx × printDpi / sourceDpi` on both axes. The UI, designer warning, preview and print preflight use the same result.
5. Quiet-zone edits change only logical margin and validation/readouts. They do not silently resize an authored frame or change HRI geometry.
6. If new DM fields are approved, save/load/clone/document snapshot preserve them with safe defaults. Existing templates retain their visual geometry and QR behavior.
7. Capacity and fit messages include the standard, data state and repair action. Do not call an automatic Data Matrix result a QR capacity result.

## 7. Proposed AutomationIds and accessibility

| Region/control | Proposed `AutomationId` | Accessible name |
| --- | --- | --- |
| 2D card | `Barcode2D.Properties.Card` | 2D barcode properties |
| Standard | `Barcode2D.Properties.Standard` | 2D barcode standard |
| Sizing mode | `Barcode2D.Properties.SizingMode` | Sizing mode |
| Symbol version/size | `Barcode2D.Properties.SymbolVersionOrSize` | Symbol version or Data Matrix size |
| Error correction/EC | `Barcode2D.Properties.ErrorCorrection` | Error correction or EC |
| Module px | `Barcode2D.Properties.ModuleSize` | Module size in pixels |
| Source DPI | `Barcode2D.Properties.SourceDpi` | Source DPI |
| Effective dots | `Barcode2D.Properties.EffectiveModule` | Effective module at print DPI |
| Quiet zone | `Barcode2D.Properties.QuietZone` | Quiet zone modules |
| Capacity/fit | `Barcode2D.Properties.CapacityStatus` | Capacity and fit |
| Renderer support | `Barcode2D.Properties.RendererSupport` | Renderer support |
| Validation | `Barcode2D.Properties.Validation` | 2D barcode print preflight |

Keyboard order: Standard → Application profile → Sizing mode → standard-specific size/EC → Module → Source DPI → Quiet zone → capacity/support → validation. Every disabled field needs an accessible explanation and a safe alternative.

## 8. Responsive and runtime evidence

| Target | Required behavior | Evidence |
| --- | --- | --- |
| `1280 × 800` | Keep standard-specific rows compact; long capacity/repair copy wraps inside the Properties card | QR fixed/auto and DM automatic screenshots/UI Automation |
| `1024 × 600` | Stack size/EC and effective-dot readouts without clipping the repair action | QR overflow, DM unsupported-control and sub-2-dot states |
| `100%`, `125%`, `150%` | Preserve focus order, visible disabled explanation and measured card bounds | Record scale, bounds, keyboard/focus and wrapped text |

Runtime evidence must include the exact printer plan DPI and a controlled payload. It must demonstrate that the displayed effective dots and preflight message agree. Figma `18:69`/`13:2` metadata is not a substitute for this evidence.

## 9. Implementation acceptance gates

P5 may be marked implemented only when all of the following are true:

1. QR copy and state visibility distinguish `Symbol version`, ECC, capacity and module/DPI without changing `QrCapacityTable` behavior.
2. Data Matrix has a documented supported sizing/EC contract, or the UI clearly exposes the automatic path and unavailable controls without fake parity.
3. Fixed QR and DM modules report effective dots at plan DPI and retain the sub-2-dot regression gate.
4. DM save/load/clone/snapshot fields, if added, round-trip with safe defaults; old templates preserve geometry.
5. Empty, invalid, stale, overflow, unsupported and undersized states have actionable regression fixtures.
6. Designer, preview and print use the same matrix module/QZ policy; P0–P4 gates and QR gates remain green.
7. Runtime evidence covers target scales and approved Figma routing; no Figma edit is implied by this spec.
8. Protected Text/TextBox behavior remains unchanged.
9. No native-printer, physical-verifier, GS1-certification or full-2D-catalog claim is added.

Suggested verification remains:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 10. Explicit non-goals

- a full Data Matrix catalogue or Aztec/MaxiCode/PDF417 parity;
- pretending QR version/ECC fields are Data Matrix controls;
- changing the existing QR capacity table or silently migrating legacy templates;
- native 2D printer commands, physical verifier grade or certification;
- automatic frame resizing solely to make a 2D control appear valid;
- a new Figma frame solely to satisfy this document;
- any Text/TextBox ownership, sizing, wrapping, clipping, padding, resize or print-contract change.

Until the owner records the renderer-supported Data Matrix size/EC vocabulary, unsupported-control treatment, QR copy choice, Figma routing and runtime evidence owner, P5 remains a UI/UX specification.
