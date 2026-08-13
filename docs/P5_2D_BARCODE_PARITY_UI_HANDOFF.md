# P5 2D barcode parity UI handoff

**Status:** pre-implementation handoff; design and contract review required (2026-08-13)
**Parent spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P5
**Competitive matrix:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M16/M17
**UI/UX specification:** [`P5_2D_BARCODE_PARITY_UI_SPEC.md`](P5_2D_BARCODE_PARITY_UI_SPEC.md)
**Owner decision packet:** [`P5_2D_BARCODE_PARITY_DECISION_PACKET.md`](P5_2D_BARCODE_PARITY_DECISION_PACKET.md)
**Figma rule:** use the selected-Properties shell in [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md); no barcode-specific 2D frame is recorded.

## 1. Operator task

An operator authoring a QR Code or Data Matrix must be able to:

1. choose the 2D standard and see only controls whose meaning is supported by that standard and the active renderer;
2. distinguish data-driven sizing from an explicitly fixed symbol/module policy;
3. understand symbol version/size, error-correction terminology, quiet-zone modules, source DPI and effective dots at print DPI;
4. repair capacity, undersized-frame, invalid-binding or sub-2-dot warnings before preview/print.

QR and Data Matrix are related matrix codes, but their version/size and error-correction vocabularies are not interchangeable. P5 must make that distinction visible instead of presenting QR-named controls as if they were Data Matrix semantics.

## 2. Current source evidence

| Surface | Current evidence | P5 gap |
| --- | --- | --- |
| Barcode Properties | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1878) shows one card for Code 128, QR Code and Data Matrix. It currently exposes QR mode, QR EC, quiet-zone modules, DPI and a fixed `Version`/`Module px` group across the matrix-object path. | The card needs standard-aware copy and visibility. A Data Matrix operator must not be given a control labelled as QR version or QR EC unless the renderer contract explicitly gives it the same meaning. |
| Model/persistence | [`LabelObject.cs`](../src/ANLAbel.Core/Models/LabelObject.cs#L205) persists `QrSizingMode`, `QrErrorCorrection`, `QrFixedVersion`, `QrModuleSizePx`, `QrQuietZoneModules` and `QrDpi`; [`LabelObjectCloner.cs`](../src/ANLAbel.Core/Models/LabelObjectCloner.cs) and `DocumentSnapshot` carry those fields. | There is no first-class Data Matrix size/EC field. Any new DM fields require explicit save/load/clone/snapshot defaults and legacy compatibility. |
| Renderer | [`ZxingBarcodeRenderer.cs`](../src/ANLAbel.Barcode/Renderers/ZxingBarcodeRenderer.cs) applies `ErrorCorrection` only to QR. Data Matrix receives a shape hint; it does not receive a QR EC value or a user-selected symbol-size constraint. | Do not promise a DM EC/size selector until the engine and renderer expose a supported contract. Unsupported controls must be hidden or marked unavailable with a reason. |
| Print preflight | [`PrintPreflightValidator.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs) shares the fixed matrix module warning and checks QR fixed-version byte capacity. `IsSquare2DCodeLike()` includes Data Matrix, but capacity/version validation is QR-specific. | Reuse the physical module policy for DM only when its module/source-DPI semantics are confirmed; add DM size/EC/capacity checks only with a named engine contract. |
| Figma | Read-only metadata for panels file `kqyNBI0DgRHnPzJTDBIui5`, node `18:69`, reports a `300 × 700` selected-Properties shell with `284 DIP` content cards. The page has no barcode 2D state. | Use the shell for grouping and density only. A Figma frame alone cannot prove QR/DM behavior or print-DPI evidence. |

## 3. Proposed contract boundary

| Concern | Proposed boundary | Must not do |
| --- | --- | --- |
| QR naming | Keep the existing `QrCapacityTable`, fixed/auto sizing and ECC behavior. Rename the visible fixed control from generic `Version` to `Symbol version` or `QR symbol version` after owner approval. | Do not change QR capacity math or silently reinterpret existing saved values. |
| Data Matrix sizing | Expose only a renderer-supported data-driven or fixed symbol/module policy. If the engine cannot author a requested DM size, show `Not available in this renderer` and leave the data-driven path explicit. | Do not reuse `QrFixedVersion` as a fake DM size or show a no-op selector. |
| Error correction | Show QR `L/M/Q/H` only when Standard = QR. Show a Data Matrix EC control only after the renderer defines a supported value, mapping and preflight rule. | Do not label a QR EC value as `DM EC` or imply that ZXing's current DM path accepts it. |
| Module/DPI | For a fixed matrix module, report `module px × print DPI / source DPI` as effective X/Y dots. Keep the shared sub-2-dot risk policy and its source-DPI explanation. | Do not silently stretch a fixed module into an undersized printer frame or report authored px as printer dots without conversion. |
| Quiet zone | Keep quiet zone as logical modules, with the same render/preflight option. Any future physical-mm readout must name its convention and shared resolution. | Do not migrate or overwrite authored quiet zones, or mix linear P4 side/total language into this 2D control. |
| Frame/legacy behavior | Preserve existing QR authoring and frame policies. New DM fields, if approved, use safe defaults; old `.anlabel` files retain their visual geometry. | Do not add a second content-driven frame mutation path merely to make DM controls appear equivalent to QR. |
| Printer path | Continue the graphic ZXing path for this phase. | Do not claim native 2D printer commands, verifier grade or certification. |

## 4. State matrix

| State | Visible evidence | Safe next action | Print rule |
| --- | --- | --- | --- |
| No source / empty data | Standard and applicable controls remain visible; capacity/fit reads `Waiting for data`; no stale result | Enter data or repair the binding | Do not treat an empty preview as a valid symbol |
| QR auto-size, valid data | `QR symbol version` (resolved), ECC, module/QZ, source DPI and fit status | Inspect or edit data/ECC/QZ | Continue normal QR preflight |
| QR fixed symbol/module, valid capacity | Fixed mode, symbol version, ECC, module px, effective dots and capacity `fits` | Continue or choose a larger version/module | Block only on the existing capacity/frame/module rules |
| QR fixed capacity overflow | Byte count, selected symbol version/ECC, capacity and repair hint | Increase version, lower data or choose auto-size | Block the affected row; do not truncate data |
| Data Matrix supported data-driven path | Standard-aware `Data Matrix size: automatic` and module/QZ/DPI readouts | Keep automatic or choose an approved supported size | Use the renderer's actual result; no QR capacity claim |
| Data Matrix requested control unsupported | Clear `Not available in this renderer` explanation and the safe automatic fallback | Keep automatic or cancel the unsupported edit | Do not print as if the requested size/EC was applied |
| Fixed matrix sub-2-dot risk | Effective X/Y dots, printer DPI, source DPI and repair hint | Increase module px, align source DPI or use automatic sizing | Fail/warn using the shared plan-DPI policy |
| Authored frame too small / invalid binding / stale unknown | Specific frame, binding or evidence status; no fabricated capacity | Resize/repair binding/reload data and revalidate | Block only with an actionable reason; never hide stale state as valid |

## 5. First-pass host-neutral wireframe

```text
[2D Barcode]
[Standard: QR Code | Data Matrix]
[Application profile]

[Sizing mode: Auto size by data | Fixed symbol/module]
[Symbol version / Data Matrix size]   [Error correction / EC]
[Module px] [Source DPI] [Effective dots @ print DPI]
[Quiet zone (modules)]

[Capacity / fit / renderer support status]
[Print preflight: valid | warning | blocked + repair]
```

The labels in the third row are standard-specific: QR uses `Symbol version` and `Error correction`; Data Matrix uses an approved `size`/`EC` vocabulary only when the renderer supports it. For an unsupported Data Matrix control, retain the row as a diagnostic, not as an editable QR-shaped control.

## 6. Interaction and persistence rules

1. Changing Standard refreshes applicability before editing; QR-only values must not be silently applied as Data Matrix semantics.
2. Changing QR sizing mode, symbol version, ECC, module or quiet zone recomputes the existing QR capacity/geometry result from the shared path.
3. Changing a Data Matrix field is allowed only when the field is backed by an explicit engine/renderer contract; otherwise the UI explains why it is unavailable.
4. Source DPI and selected printer plan DPI are always shown together when effective dots are reported. Designer, preview and print diagnostics use the same conversion and severity.
5. Binding, empty-data and stale/unknown states invalidate the displayed fit/capacity result until re-evaluated. No stale green result survives a source change.
6. New Data Matrix fields, if approved, must survive save/load/clone/document snapshot; legacy files preserve authored geometry and existing QR fields.

## 7. Proposed AutomationIds

These IDs are proposals until the owner selects the host and runtime implementation:

| Region/control | Proposed `AutomationId` | Accessible name |
| --- | --- | --- |
| 2D barcode card | `Barcode2D.Properties.Card` | 2D barcode properties |
| Standard | `Barcode2D.Properties.Standard` | 2D barcode standard |
| Sizing mode | `Barcode2D.Properties.SizingMode` | 2D barcode sizing mode |
| Symbol version/size | `Barcode2D.Properties.SymbolVersionOrSize` | Symbol version or Data Matrix size |
| Error correction/EC | `Barcode2D.Properties.ErrorCorrection` | Error correction or EC |
| Module size | `Barcode2D.Properties.ModuleSize` | Module size in pixels |
| Effective module | `Barcode2D.Properties.EffectiveModule` | Effective module at print DPI |
| Quiet zone | `Barcode2D.Properties.QuietZone` | Quiet zone in modules |
| Capacity/fit | `Barcode2D.Properties.CapacityStatus` | Capacity and fit status |
| Renderer support | `Barcode2D.Properties.RendererSupport` | Renderer support status |
| Validation | `Barcode2D.Properties.Validation` | 2D barcode print preflight |

Keyboard order should remain Standard → Application profile → Sizing mode → standard-specific size/EC → Module/DPI → Quiet zone → status/repair. Disabled controls must expose the reason and a safe alternative.

## 8. Figma routing and runtime evidence

The read-only metadata check for panels node `18:69` is sufficient for this documentation slice: selected Properties `300 × 700`, content cards `284 DIP`, compact tabbed shell. No barcode-specific QR/DM frame is currently recorded. Reuse that shell only after an owner approves the language and density; request a state-specific node only if the shell cannot answer a concrete design question.

Before P5 implementation closure, capture runtime screenshots/UI Automation at `1024 × 600`, `100%`, `125%` and `150%` for at least:

- QR auto-size and fixed-capacity success/overflow;
- Data Matrix supported automatic path and unsupported-control explanation;
- fixed-module sub-2-dot warning and undersized-frame repair;
- empty/invalid/stale binding states.

The evidence must record measured Properties bounds, focus/keyboard order, wrapped repair text and the exact print-plan DPI. Figma metadata is visual input, not runtime or physical-output proof.

## 9. Owner decisions before coding

1. Confirm the QR visible copy (`Symbol version` versus `QR symbol version`).
2. Confirm which Data Matrix size/EC values the selected renderer can actually encode and preflight.
3. Decide whether unsupported DM controls are hidden or shown as disabled diagnostics.
4. Approve reuse of Figma `18:69`/`13:2`, or provide a state-specific 2D Properties node.
5. Assign runtime screenshot/UI Automation and save/load/clone regression owners.

Until these decisions and the named gates are recorded, P5 remains a design/contract handoff and not an implemented 2D parity feature.
