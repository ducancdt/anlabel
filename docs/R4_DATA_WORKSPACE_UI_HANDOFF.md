# R4 Data Workspace authoring and diagnostics UI handoff

**Status:** pre-implementation handoff; design review required
**Parent plan:** [`docs/reinvention/07-execution-plan.md`](reinvention/07-execution-plan.md) section R4.4
**Current product sequence:** [`PLAN.md`](../PLAN.md) transform persistence/preview/dispatch entries through 0.211
**UI/UX specification:** [`R4_DATA_WORKSPACE_UI_SPEC.md`](R4_DATA_WORKSPACE_UI_SPEC.md)
**Owner decision packet:** [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md)
**Figma reference:** panels file [`ANLAbel UI exploration`](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), shell `8:2`, Data tab `9:2`

## 1. First operator task

An operator with an imported Excel/CSV source must be able to create one derived field and understand whether it is safe to use on a label:

1. choose a source field or fields;
2. name the derived output (for example `PrintName`);
3. author a formula such as `CONCAT(FIELD("PartNo"), "-", FIELD("Lot"))`;
4. inspect one sample value and its input-field lineage;
5. repair a parse error, missing field, duplicate output, or dependency cycle before preview/print.

The first slice is an authoring and diagnostics surface. It is not a new connector engine and does not change object geometry.

## 2. Current source evidence

| Surface | Current evidence | UI gap |
| --- | --- | --- |
| Typed connector | [`DataConnectorContracts.cs`](../src/ANLAbel.Core/Data/DataConnectorContracts.cs) defines connector descriptors, typed schema/records, paging and cancellation-safe reads. [`MainViewModel.DataConnector`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L321) publishes the immutable import snapshot beside the legacy `DataView`. | No workspace surface exposes schema, sample records, paging state, or a safe connector action model. |
| Transform model | [`LabelTemplate.DataTransforms`](../src/ANLAbel.Core/Models/LabelTemplate.cs#L81) persists typed `DataTransformDefinition` values; [`MainViewModel.DataTransforms`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L220) exposes them read-only to the current view. | No add/edit/remove/reorder commands or draft editor are present. |
| Evaluation and lineage | [`DataTransformPipeline`](../src/ANLAbel.Core/Data/DataTransformPipeline.cs#L8) detects duplicate outputs, formula errors and dependency cycles, then returns a sample `DataRecord` plus `DataTransformLineage`. | Errors are available to the ViewModel but there is no field-level repair UI or lineage explanation. |
| Preview/dispatch guard | [`MainViewModel.DataTransformError`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L327) is surfaced and current print paths block when it is non-empty. The transform preview path is [`CreatePreviewRow`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L4309). | The operator cannot see which draft caused the block or which source field is missing. |
| Identity and persistence | [`DocumentSnapshot.DataTransformFingerprint`](../src/ANLAbel.Core/Scene/DocumentSnapshot.cs#L38) and [`LabelTemplate` snapshot capture](../src/ANLAbel.Core/Scene/DocumentSnapshot.cs#L77) make transform changes part of document identity. | Save/load/clone UI coverage for authored transforms must be added with the feature. |
| Current WPF Data tab | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L745) has a Data tab. The current-data card exposes link status and Update/Unlink actions at [`#L843`](../src/ANLAbel.App/MainWindow.xaml#L843); the transform list is currently collapsed/hidden at [`#L828`](../src/ANLAbel.App/MainWindow.xaml#L828). | No transform editor, sample table, mapping/lineage view, filter/sort controls, or explicit diagnostics action exists. |

## 3. Proposed product boundary

### In scope for the first UI slice

- a Data tab source summary that preserves the existing Excel/CSV link, stale, failed and refresh actions;
- an explicit **Transforms** section with `Add transform`, output field name, formula text, sample result and remove/edit actions;
- a compact sample-record preview (one selected row is sufficient for the first slice);
- field-level diagnostics for invalid formula, missing field, duplicate output and dependency cycle;
- lineage text or a small list showing `output <- input fields`;
- a binding-check link from a transform/output error to the affected label object or binding diagnostic;
- draft validation before writing `LabelTemplate.DataTransforms` and a fail-closed status when the committed definition is invalid.

### Deliberately deferred

- ODBC/SQL/HTTP connectors, credential/secret authoring and a new connection wizard;
- full filter/sort builder, paging controls and a multi-node graph editor;
- prompt/database/counter variables and a general-purpose expression designer;
- automatic migration that changes the meaning of legacy `{Field}` or Formula bindings;
- any change to Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle or print parity.

## 4. UI state contract

| State | Required visible content | Safe action |
| --- | --- | --- |
| No source linked | `No data linked`, short explanation and `Import Excel / CSV` | Import or keep the label data-free; do not show a false preview. |
| Source linked, no transforms | Workbook/sheet, row count/freshness, selected sample row and `Add transform` | Add a derived field or continue binding source fields. |
| Transform draft valid | Output name, formula, sample value, input fields and neutral status | Apply/save the definition; preview uses the same transformed row. |
| Formula parse or evaluation error | Error beside the draft plus the formula location/message when available | Edit the draft; keep the last valid committed definition until Apply succeeds. |
| Duplicate output | Both conflicting output names and a repair instruction | Rename or remove one definition; never silently overwrite. |
| Dependency cycle | Names participating in the cycle and a repair instruction | Break the cycle; preview/print stays blocked while unresolved. |
| Source stale/failed | Existing stale/failed evidence and Update/Relink action | Refresh or relink first; do not mark the transform green from cached data. |
| Binding issue | Affected object/binding summary and link to the object diagnostic | Repair the source/output mapping; do not dispatch raw fallback values. |

The frequency order should remain: source/context -> sample row -> transforms -> lineage/diagnostics -> binding checks. `Data settings` can stay collapsed for secondary tracking/copies/shared-source options.

## 5. Figma evidence and routing

The read-only metadata for panels file `kqyNBI0DgRHnPzJTDBIui5` and Data tab content `9:2` was checked on 2026-08-13:

| Node | Name | Size | Reusable intent |
| --- | --- | --- | --- |
| `8:2` | `ANLAbel - Frequency-first Panels v0.198` | `664 x 788` | Workspace + Properties shell; keep the existing panel language. |
| `8:15` | `Workspace tabs` | `300 x 42` | Real `Layers` / `Data` task switch. |
| `9:2` | `Data tab content` | `300 x 610` | Data surface container. |
| `9:3` | `Data source / Empty` | `276 x 142` | No-source summary and Import action. |
| `9:16` | `Current data context` | `276 x 102` | Workbook and preview-row context. |
| `9:27` | `Data settings / Collapsed` | `276 x 62` | Secondary settings disclosure; includes the transform concept in its hint. |
| `9:35` | `Binding checks / Clear` | `276 x 42` | Existing diagnostic anchor. |

There is no transform editor, sample table, lineage list, or invalid-state variant in the checked Figma page. **Interim routing:** reuse the `9:2` shell and card language, but require either a state-specific Figma node or an explicit owner-approved WPF-reuse decision before implementing new controls. Do not widen the WPF `268/280` panels from the Figma `300/300` reference alone, and do not edit Figma merely to fill the missing states.

The runtime artifact—not the Figma frame—is acceptance: screenshot/UI Automation at `1024x600`, `100%`, `125%` and `150%` covering no-source, valid transform, invalid formula/cycle and stale/failed source states.

## 6. Regression and acceptance gates

Required software coverage for this slice:

- add/edit/remove/round-trip persistence of `DataTransformDefinition` values;
- valid sample evaluation displays the transformed value and input-field lineage;
- duplicate output, invalid formula, missing field and dependency cycle fail closed with actionable diagnostics;
- preview, Current Row and All Rows use the same transformed values; no raw fallback is dispatched after a transform error;
- transform fingerprint changes document identity and survives save/load/clone;
- existing Excel link verification, binding, print preflight, barcode and protected Text/TextBox gates remain green;
- runtime screenshot or UI Automation covers the state matrix above at the target window/display scales.

Suggested commands remain:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 7. Ownership and ready gate

The cross-surface source/read-model, host ownership, draft/commit, async freshness, Figma routing and runtime evidence decisions are consolidated in [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md). That packet keeps Data Workspace authoring in the Data tab and shared-source CRUD in `DatabaseManagerWindow`; it is documentation-only and does not authorize a transform editor or Figma edit.

This handoff is **not ready for implementation** until the owner records:

1. whether the first release is transform authoring only or also schema/filter/sort work;
2. the operator-facing formula language/copy and the default sample-row behavior;
3. reuse of Figma `9:2` shell versus a state-specific reference for the transform/error cards;
4. runtime screenshot/UI Automation ownership and stable AutomationIds;
5. the named regression additions and the clean implementation commit that owns them.

The slice may be marked ready only when those decisions, runtime evidence and regression coverage are attached. Software transform support is not a claim of typed connector parity, external database support or physical print verification.
