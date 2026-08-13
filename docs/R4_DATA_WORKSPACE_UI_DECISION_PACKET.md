# R4 Data Workspace authoring and diagnostics UI owner decision packet

**Status:** documentation-only owner gate; the existing WPF Data tab remains the source/row/binding-check host, while transform authoring, sample results and lineage remain an explicit design gap (2026-08-13)
**UI/UX handoff:** [`R4_DATA_WORKSPACE_UI_HANDOFF.md`](R4_DATA_WORKSPACE_UI_HANDOFF.md)
**UI/UX specification:** [`R4_DATA_WORKSPACE_UI_SPEC.md`](R4_DATA_WORKSPACE_UI_SPEC.md)
**Cross-surface R4 packet:** [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md)
**Shared-source owner packet:** [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](DATABASE_MANAGER_UI_DECISION_PACKET.md)
**Figma routing:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and boundary

The current Data tab already owns the linked Excel/CSV summary, row selection, freshness/link warnings, read-only row grid, binding checks and a compact read-only transform list. The typed source/transform pipeline can parse formulas, order dependencies, evaluate a sample record and return lineage, but there is no WPF editor for drafting, validating, applying or repairing a transform. This packet turns that gap into an owner contract before implementation.

This packet covers:

- source context, selected/sample row and freshness state in the Data tab;
- a first bounded transform draft/editor with output name, formula, validation, sample result and lineage;
- atomic Apply/Cancel semantics and persistence through `LabelTemplate.DataTransforms`;
- parse, evaluation, missing-field, duplicate-output, dependency-cycle and stale-source diagnostics;
- binding-check navigation, UIA IDs, target-scale evidence and regression fixtures;
- read-only Figma reuse without inventing a transform frame.

It does not add a connector wizard, ODBC/SQL/HTTP or secret management, filter/sort/paging, arbitrary code execution, a graph editor, legacy-binding migration, a new Manager screen, Figma edits, or any Text/TextBox ownership, sizing, wrapping, clipping, padding, resize, overflow or print-parity change. Shared-source CRUD remains in [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](DATABASE_MANAGER_UI_DECISION_PACKET.md).

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Host and surface boundary | Keep `MainWindow` Data tab as the authoring/diagnostics host. Keep shared source identity, relink, use, remove and cleanup in the Database Manager; do not duplicate registry CRUD here. | Confirm one owner for source context, transform draft state and each committed-template mutation. |
| D2. First formula scope | Start with the existing bounded AST/evaluator vocabulary (`FIELD("Name")`, `CONCAT(...)`) and typed `DataTransformDefinition(Name, Formula)`. Treat unsupported syntax as a visible validation error. | Approve the initial function/argument list and copy for unsupported or empty formulas. |
| D3. Source/sample context | Show linked source, fields, freshness/link status and the selected/sample row used for validation. A missing source or row is a first-class state, not an empty success. | Choose sample-row selection and whether a draft may validate against a manually chosen row only or the current preview row. |
| D4. Draft and atomic Apply | Keep edits in a draft model. Validate name/formula/dependencies and evaluate the sample before Apply; Apply replaces the committed transform collection atomically, while Cancel discards the draft. | Approve duplicate-name policy, ordering UI and whether Apply requires a fresh source snapshot. |
| D5. Result and lineage ownership | `DataTransformPipeline.Evaluate` remains the calculation authority. The Data tab presents per-transform result, input-field lineage and actionable diagnostics; it must not reimplement formula evaluation in code-behind. | Name the owner for diagnostic copy and the minimum lineage detail needed for repair. |
| D6. Invalid and stale fail-closed | Parse/evaluation errors, missing fields, duplicate outputs, dependency cycles and stale/failed source status keep Apply/print unavailable and never present a raw-source result as a successful transformed preview. | Approve severity, retry/repair actions and whether an invalid draft can be saved as an explicitly disabled draft (recommended: no committed invalid transform). |
| D7. Binding-check route | Existing binding checks remain the navigation surface for object field issues. Selecting an issue may select/focus the object, but transform diagnostics must not mutate object geometry or Text/TextBox policies. | Approve the link from a missing transformed field to its transform row and the reverse link from a transform input to affected bindings. |
| D8. Persistence and identity | `LabelTemplate.DataTransforms` is the committed source of truth; `DocumentSnapshot.DataTransformFingerprint` must change with authored definitions and survive save/load/clone. | Approve unsaved-draft close behavior and the visible “saved/reloaded” confirmation. |
| D9. Figma and accessibility route | Reuse only the existing `8:2`/`9:2` shell/card vocabulary read-only. Use proposed stable UIA IDs in WPF; request a smallest state-specific Figma reference only if a later owner needs a concrete transform state. | Name the design/UIA owner and decide whether a transform-editor state is required before implementation. |
| D10. Closure and regression | Close only with click-through/UIA at target scales, transform fixtures, save/clone evidence and clean implementation output. This packet adds no code/test result. | Fill sign-off rows and attach the implementation/runner evidence at closure. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L845-L862) | The current Data tab has a linked-source/current-row card with relink, refresh and unlink actions. | It does not prove a transform authoring host or target-scale layout. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L891-L911) | Excel rows are a read-only `DataGrid` bound to `ExcelDataView`; selection is available for preview context. | It does not prove transformed sample columns, paging or lineage. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L912-L963) | Data settings exposes tracking/copies/shared sources and lists `DataTransforms` as `Name = Formula`; the list has no Add/Edit/Validate/Apply/Cancel controls. | A read-only list is not an editor, draft boundary or commit guarantee. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L964-L999) | Binding checks already have a compact status card, issue list and selection command. | The current card does not explain transform lineage or repair a formula. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L220-L220) | `DataTransforms` exposes `Template.DataTransforms` as a read-only enumerable to the view. | It does not provide draft commands or atomic editing. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L321-L339) | `DataConnector`, `DataTransformError` and `HasDataTransformError` are observable read-model properties. | A status property does not establish per-transform diagnostics or a safe Apply transaction. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L2516-L2530) and [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L2575-L2609) | Current-row and all-row print paths block when a transform error is present or encountered. | The source does not prove an authoring UI, retry policy or all-row partial-result presentation. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L4291-L4323) | Preview builds a source dictionary and evaluates committed transforms; on invalid evaluation it records the error but returns the source dictionary. | Returning source on error is not a safe transformed-preview contract; the future UI must label the state invalid and prevent raw fallback from appearing successful. |
| [`DataTransformPipeline.cs`](../src/ANLAbel.Core/Data/DataTransformPipeline.cs#L28-L46) | The pipeline computes a fingerprint and evaluates typed definitions against a `DataRecord`. | It does not provide WPF draft state, row selection or visual diagnostics. |
| [`DataTransformPipeline.cs`](../src/ANLAbel.Core/Data/DataTransformPipeline.cs#L53-L117) | Duplicate names, parse/evaluation errors and ordered results/lineage are calculated centrally; invalid results return errors. | It does not define copy, severity, repair controls or a commit transaction. |
| [`DataTransformPipeline.cs`](../src/ANLAbel.Core/Data/DataTransformPipeline.cs#L120-L164), [`FormulaEngine.cs`](../src/ANLAbel.Core/Expressions/Formulas/FormulaEngine.cs#L3-L18) and [`FormulaEvaluator.cs`](../src/ANLAbel.Core/Expressions/Formulas/FormulaEvaluator.cs#L19-L66) | Dependencies are topologically ordered, cycles are diagnosed, and the bounded `FIELD`/`CONCAT` evaluator is the calculation boundary. | It does not authorize arbitrary expressions, network access or hidden variables. |
| [`DataConnectorContracts.cs`](../src/ANLAbel.Core/Data/DataConnectorContracts.cs#L6-L91) | Typed connector descriptors/records and paging/cancellation contracts provide a stable read model beside legacy `DataView`. | The first UI slice does not need to expose every connector capability. |
| [`DocumentSnapshot.cs`](../src/ANLAbel.Core/Scene/DocumentSnapshot.cs#L18-L77) and [`LabelTemplate.cs`](../src/ANLAbel.Core/Models/LabelTemplate.cs#L76-L84) | Transform definitions are part of the template and their fingerprint contributes to document identity. | A fingerprint alone does not prove save/load/clone UI evidence. |
| [`R4_DATA_WORKSPACE_UI_SPEC.md`](R4_DATA_WORKSPACE_UI_SPEC.md) | The proposed wireframe, state matrix, UIA IDs and acceptance gates already define a bounded docs-only vertical slice. | The spec is not owner sign-off and does not add implementation. |

## Surface and action ownership

| Surface/action | Current/proposed owner | Safe action | Boundary |
| --- | --- | --- | --- |
| Data tab host/source summary | `MainWindow` + `MainViewModel` | Show source ID/display path, field summary, current/sample row and freshness/link state | Do not mutate shared registry or object geometry. |
| Source fields/row context | `IDataConnector`/`DataView` read model | Choose a sample row and expose source fields used by formula validation | No connector-specific editor, secrets or network setup in this slice. |
| Transform list | Data Workspace view model | Add/select/reorder/remove draft entries; show committed versus draft state | Do not write `Template.DataTransforms` until atomic Apply. |
| Transform editor | Proposed Data Workspace draft model | Edit output name and bounded formula; validate inline | No arbitrary code, hidden variables or raw string eval in code-behind. |
| Sample result/lineage | `DataTransformPipeline` + Data Workspace presentation | Show transformed value, input fields and dependency chain for the selected sample | Never show an invalid raw source as a successful transformed value. |
| Diagnostics | Data Workspace | Repair parse/missing/duplicate/cycle/stale errors; keep Apply disabled while invalid | Do not suppress or downgrade `DataTransformError`. |
| Binding checks | Existing `MainViewModel.BindingIssues`/selection route | Link issue to object and affected transform/input field | No Text/TextBox policy, frame, wrapping or binding ownership change. |
| Apply/Cancel | Proposed draft transaction | Apply all valid definitions once; Cancel restores committed list | No partial collection mutation or implicit save on every keystroke. |
| Shared-source CRUD | `DatabaseManagerWindow`/`DataSourceRegistry` | Relink, Use, Remove and orphan cleanup through Manager | Do not duplicate these actions in transform editor. |
| Preview/print | Existing preview/print paths | Consume the committed transformed read model and block invalid/stale data | No raw fallback, partial all-row print or physical-output claim. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| No source linked | Empty-source/import card and disabled transform actions | Import/link through existing source flow | No formula validation against an unknown schema. |
| Source linked, no transforms | Source/fields/row context and “No transforms” | Add first draft or inspect bindings | Keep committed preview equal to source only when no transform exists. |
| Source linked, no sample row | Source fields plus explicit “No sample row” | Select a row or repair empty source | No green sample result; Apply policy follows the approved no-row rule. |
| Draft editing | Draft badge, output name/formula fields and Cancel | Edit, reorder, remove or Validate | Committed template and fingerprint remain unchanged. |
| Draft valid | Valid status, sample result and lineage | Apply atomically | Apply is one transaction; no intermediate committed list. |
| Parse/evaluation error | Error copy attached to formula/result with repair hint | Edit/revalidate or Cancel | Apply/print disabled; no raw fallback shown as transformed. |
| Missing input field | Named missing field and source-field repair route | Rename/repair source or formula | Keep definition uncommitted and diagnostics visible. |
| Duplicate output | Both conflicting definitions named | Rename/remove one, then revalidate | No order-dependent overwrite or partial Apply. |
| Dependency cycle | Cycle members listed in dependency order | Break the cycle or Cancel | No evaluation, Apply or print. |
| Source stale/failed | Freshness/link warning remains next to sample context | Refresh/relink through source owner | Do not validate or print against a stale snapshot without explicit approved policy. |
| Binding issue | Existing binding-check summary plus object/transform link | Select object, repair field/formula | No geometry or protected Text/TextBox mutation. |
| Apply succeeded | Committed list, refreshed sample and changed document state | Continue preview/save/print | Fingerprint and save state must reflect the exact definitions. |
| Apply failed | Actionable persistence/validation error, draft retained | Repair/retry/Cancel | Keep prior committed transforms and fingerprint unchanged. |
| Save/load/clone mismatch | Visible fingerprint or transform-count mismatch | Reopen/repair through document owner | Never silently drop or reorder authored transforms. |
| All-row partial failure | Row index/source context and aggregate diagnostic | Repair/cancel; do not print | No partial output or raw rows presented as complete. |

## Figma metadata boundary

Read-only metadata was rechecked on 2026-08-13 for the existing [ANLAbel UI exploration file](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), Page `0:1`, node `9:2`:

| Node | Metadata | Safe reuse | Missing transform state |
| --- | --- | --- | --- |
| `8:2` | Frequency-first panel shell, `664 x 788` | Workspace/card spacing and visual language | No transform editor or lineage panel |
| `8:5` | Workspace panel, `300 x 700` | Data-panel context only | No authoring transaction or diagnostics region |
| `9:2` | Data tab content, `300 x 610` | Existing Data hierarchy | No Add/Edit/Apply/Cancel transform flow |
| `9:3` | Empty source, `276 x 142` | No-source/import copy | No schema/sample validation state |
| `9:16` | Current data context, `276 x 102` | Workbook/preview-row summary | No sample result or freshness repair controls |
| `9:27` | Collapsed data settings, `276 x 62` | Disclosure pattern for secondary settings | No transform list/editor/lineage |
| `9:35` | Binding checks clear, `276 x 42` | Compact diagnostics status | No formula or dependency diagnostics |
| `22:82` | Excel Link Verification symbols, `620 x 455` | Link-health vocabulary only | Not a Data Workspace authoring screen |

**Routing decision:** reuse `8:2`/`9:2` only as read-only shell evidence and keep the current WPF Data tab as the implementation baseline. Do not widen the WPF panel from Figma dimensions, call `get_design_context`, create a transform frame or edit Figma for this docs-only packet. If implementation needs a concrete visual state, the owner must name the state and request the smallest state-specific Figma reference first.

## Accessibility and responsive gate

The current Data-tab XAML does not assign stable `AutomationProperties.AutomationId` values to the proposed transform controls. The following IDs are proposals, aligned with [`R4_DATA_WORKSPACE_UI_SPEC.md`](R4_DATA_WORKSPACE_UI_SPEC.md):

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Workspace root | `DataWorkspace.Root` | Data workspace |
| Source summary | `DataWorkspace.SourceSummary` | Linked data source |
| Source fields | `DataWorkspace.SourceFieldList` | Source fields |
| Sample context | `DataWorkspace.SampleContext` | Sample row |
| Transform list | `DataWorkspace.TransformList` | Data transforms |
| Add | `DataWorkspace.AddTransform` | Add transform |
| Output name/formula | `DataWorkspace.TransformOutputName`, `DataWorkspace.TransformFormula` | Output field name / Formula |
| Result/lineage | `DataWorkspace.TransformResult`, `DataWorkspace.TransformLineage` | Sample result / Field lineage |
| Validate/Apply/Cancel | `DataWorkspace.ValidateTransform`, `DataWorkspace.ApplyTransform`, `DataWorkspace.CancelTransform` | Validate / Apply / Cancel transform |
| Diagnostics/bindings | `DataWorkspace.Diagnostics`, `DataWorkspace.BindingChecks` | Transform diagnostics / Binding checks |

Runtime evidence must cover `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception), keyboard order through source context → transform list → editor → result/lineage → diagnostics → Apply/Cancel, visible focus after validation and Cancel, one intentional scroll owner, and no horizontal clipping of formula/error copy. The target-scale pass must also re-run the protected Text/TextBox gates when implementation changes touch the Data tab host.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `DataWorkspace_DraftDoesNotMutateCommitted` | Typing/reordering/removing a draft leaves `Template.DataTransforms` and fingerprint unchanged until Apply. | View-model/runner assertion and UIA draft badge. |
| `DataWorkspace_ApplyIsAtomic` | Valid definitions replace the committed collection once; invalid definitions leave the prior collection intact. | Before/after collection and status capture. |
| `DataWorkspace_SampleAndLineage` | A selected sample shows transformed value and exact input/dependency lineage. | Pipeline result plus UIA/sample screenshot. |
| `DataWorkspace_DuplicateOutputFailsClosed` | Duplicate output names keep Apply/print unavailable and identify both entries. | Error state and no-partial-commit assertion. |
| `DataWorkspace_ParseMissingAndCycleDiagnostics` | Parse, missing-field and dependency-cycle errors identify repair targets and produce no transformed result. | Formula fixtures and diagnostic copy. |
| `DataWorkspace_NoRawFallbackOnInvalid` | Invalid evaluation never appears as a successful raw-source preview or printable row. | Preview/print guard assertion; current `CreatePreviewRow` behavior must be resolved explicitly. |
| `DataWorkspace_FreshnessBlocksEvaluation` | Stale/failed source remains visibly invalid until refresh/relink policy succeeds. | Controlled stale connector fixture. |
| `DataWorkspace_BindingIssueLink` | Binding issue selects the object and links the missing field/transform without geometry mutation. | UIA focus route and protected-contract assertion. |
| `DataWorkspace_TransformFingerprintRoundTrip` | Save/load/clone preserves definitions, order and `DataTransformFingerprint`. | Document snapshot and persistence fixture. |
| `DataWorkspace_PreviewCurrentAllRowsParity` | Preview, Current Row and All Rows consume the same valid transformed values; any invalid row blocks complete print. | Runner output and row-index diagnostics. |
| `Protected_TextTextBox_contract_unchanged` | Data Workspace changes do not alter Text/TextBox ownership, frame geometry, wrap/clip, padding, resize or print parity. | Protected regression suite after any implementation change. |

## No-go list

- Do not treat the current read-only `Name = Formula` list as evidence that authoring already exists.
- Do not place shared-source registry CRUD, relink, Use, Remove or cleanup inside the transform draft owner.
- Do not allow arbitrary code, network access, credentials, hidden variables or connector-specific wizards in the first formula slice.
- Do not display a raw source dictionary as a successful transformed result when evaluation is invalid.
- Do not partially Apply a list with duplicate names, missing inputs, parse errors or cycles; do not print partial all-row output.
- Do not mutate object bindings, label geometry or the protected Text/TextBox contract from transform diagnostics or binding links.
- Do not infer a transform editor, sample table or lineage graph from Figma `9:2` metadata; no Figma write is implied.
- Do not claim Preview, fingerprint persistence or UIA proposals are runtime or physical-output evidence until fixtures and target-scale checks exist.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the packet open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Host/source/transform boundary | `TBD` | `TBD` | `TBD` |  |
| D2. Formula vocabulary and unsupported syntax | `TBD` | `TBD` | `TBD` |  |
| D3. Sample-row/context policy | `TBD` | `TBD` | `TBD` |  |
| D4. Draft/atomic Apply/Cancel | `TBD` | `TBD` | `TBD` |  |
| D5. Result/lineage/diagnostic owner | `TBD` | `TBD` | `TBD` |  |
| D6. Invalid/stale fail-closed policy | `TBD` | `TBD` | `TBD` |  |
| D7. Binding-check navigation boundary | `TBD` | `TBD` | `TBD` |  |
| D8. Persistence/fingerprint round-trip | `TBD` | `TBD` | `TBD` |  |
| D9. Figma route/AutomationIds | `TBD` | `TBD` | `TBD` |  |
| D10. Runtime/UIA/regression closure | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the Data Workspace authoring/diagnostics slice may move from documentation review to implementation/release evidence only after D1-D10 are filled, the Manager/Workspace boundary is explicit, valid and invalid fixtures pass, transform definitions round-trip with their fingerprint, target-scale UIA/screenshots are attached, and the current Text/TextBox/print contract remains unchanged. Until then this is an open Data Workspace UI contract and makes no Figma, print, physical-output or release claim.
