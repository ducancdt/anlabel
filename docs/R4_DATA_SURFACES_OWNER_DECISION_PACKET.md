# R4 data surfaces owner decision packet

**Status:** documentation-only owner gate; no transform editor, data-source registry rewrite, Figma write, or Text/TextBox change is authorized by this packet (2026-08-13)
**Execution plan:** [`reinvention/07-execution-plan.md`](reinvention/07-execution-plan.md) §R4.1–R4.4
**Data Workspace handoff:** [`R4_DATA_WORKSPACE_UI_HANDOFF.md`](R4_DATA_WORKSPACE_UI_HANDOFF.md)
**Data Workspace specification:** [`R4_DATA_WORKSPACE_UI_SPEC.md`](R4_DATA_WORKSPACE_UI_SPEC.md)
**Database Manager handoff:** [`DATABASE_MANAGER_UI_HANDOFF.md`](DATABASE_MANAGER_UI_HANDOFF.md)
**Database Manager specification:** [`DATABASE_MANAGER_UI_SPEC.md`](DATABASE_MANAGER_UI_SPEC.md)
**Concrete Manager/Cleanup owner packet:** [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](DATABASE_MANAGER_UI_DECISION_PACKET.md)
**Concrete Data Workspace owner packet:** [`R4_DATA_WORKSPACE_UI_DECISION_PACKET.md`](R4_DATA_WORKSPACE_UI_DECISION_PACKET.md)
**Module history:** [`database-manager-module-plan.md`](database-manager-module-plan.md)
**Figma routing template:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

The repository has two related but deliberately separate data surfaces:

The concrete WPF Manager/Cleanup action and runtime owner is bounded in [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](DATABASE_MANAGER_UI_DECISION_PACKET.md), while the Data-tab authoring/diagnostics owner gate is bounded in [`R4_DATA_WORKSPACE_UI_DECISION_PACKET.md`](R4_DATA_WORKSPACE_UI_DECISION_PACKET.md). This cross-surface packet remains authoritative for the shared source/connector identity and Manager-versus-Workspace boundary.

```text
Excel/CSV import or shared-source selection
        -> one DataSource identity + one immutable connector snapshot
        +--------------------------------------------------------------+
        |                                                              |
        v                                                              v
Data tab / R4 Workspace                                  Database Manager dialog
draft transform + sample + lineage                       registry CRUD + Test/Preview
fail-closed preview/print                                Relink/Use/Remove/Cleanup
        |                                                              |
        +---------------- existing binding/preview/print spine --------+
```

This packet turns the shared boundary into an owner-review record. It does not combine the two hosts, add a second source store, move source CRUD into the Data tab, or make the Manager a transform editor. Existing per-template **Unlink Excel** remains separate from shared-source management. Existing Text/TextBox ownership, sizing, wrapping, clipping, padding, overflow and print parity remain protected.

The recommendations below are bounded options, not approvals. A blank sign-off row keeps the relevant slice open.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Canonical source identity and read model | Keep `DataSource.Id` in the machine-wide registry as the shared-source identity; keep `Template.DatabaseConfig.DataSourceId` as the template reference; expose the imported table through `DataTableDataConnector` as the immutable typed snapshot while `ExcelDataView` remains the WPF compatibility view. | Name the source/connector authority, migration/version rule and who owns a refresh replacement. |
| D2. Surface and action ownership | MainWindow Data tab owns R4 transform drafts, sample/lineage diagnostics and binding links. `DatabaseManagerWindow` owns shared-source list/detail, sheet/header editing, Test/Preview, Relink, Use, Remove and Cleanup. `MainViewModel`, `DataSourceRegistry` and `ExcelDataService` remain the mutation/I/O owners. | Confirm host names, navigation route, one command owner per action and stable AutomationIds. |
| D3. Delivery order | Close the existing Manager runtime matrix against the shared-source contract, then add the first R4 authoring slice on the same connector snapshot. Neither surface may wait for a new connector family, filter builder or graph canvas. | Approve Manager-first versus a coordinated implementation and name the release milestone for each slice. |
| D4. Workspace draft/commit contract | Start with one bounded derived field using the supported Formula AST (`FIELD`, `CONCAT` and already-supported functions). Draft edits are local; Apply validates the whole definition atomically, updates the document fingerprint only on commit, and leaves the last valid commit intact on failure. | Approve formula vocabulary, operator copy, output-name rules, sample-row default and draft/Apply/Cancel wording. |
| D5. Manager request identity and async behavior | Every Load sheets/Test/Preview operation captures source ID, file path, sheet and header-row values. Busy controls prevent duplicate starts; a result for an old selection or field snapshot is discarded or labeled stale. Existing Excel timeouts/cancellation remain the recovery boundary. | Approve cancellation/timeout copy, stale-result handling and whether a visible operation ID is needed. |
| D6. Shared-source mutation and safety | Preserve current Add-current deduplication, Use-by-ID import, Relink validation, usage-aware Remove confirmation and explicit Cleanup selection. Header rows normalize to `>=1`; edits persist through `DataSourceRegistry`; Manager Preview never silently binds the template. | Approve operator copy and confirmation text without changing fallback semantics or per-template Unlink. |
| D7. Cross-surface freshness and transform blocking | Source stale/failed, transform-invalid and binding-invalid are distinct states. Preview/Current Row/All Rows use the same transform evaluator; a transform error never falls back to raw dispatch. A source refresh replaces the connector snapshot through the existing owner. | Name the status/read-model owner and approve the selected-row refresh behavior and error severity vocabulary. |
| D8. Figma route | Reuse panels `8:2`/`9:2`/`9:3`/`9:16`/`9:27`/`9:35` for shell/card language only. The Manager remains the current WPF `900 × 620` dialog; no dedicated Manager frame exists. Do not edit or duplicate Figma to fill missing transform/Manager states. | Explicitly approve WPF reuse, or identify the smallest state-specific node before a visual redesign. |
| D9. Accessibility and runtime evidence | Use the proposed IDs in the two specs, keep one intentional scroll owner per surface, and capture `1024 × 600`, `100%`, `125%`, `150%` evidence (or a recorded environment exception) for both surfaces. | Name App/UI Automation, keyboard/focus and screenshot owners and artifact paths. |
| D10. Closure and regression ownership | Convert the fixtures in this packet into named Core/App/Data/runtime gates and a clean implementation commit. Existing Excel/CSV, binding, print-preflight, barcode and protected Text/TextBox gates stay in scope. | Name the implementation, QA, runtime/UIA and product owners; record the commit that closes each slice. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`DataConnectorContracts.cs`](../src/ANLAbel.Core/Data/DataConnectorContracts.cs#L5-L87) | Connector descriptors, typed schema/records, paging and cancellation-safe reads are UI-free contracts. | It does not choose a WPF host, provide credentials or replace the existing Excel import. |
| [`DataTableDataConnector.cs`](../src/ANLAbel.Data/DataTableDataConnector.cs#L7-L58) | The imported `DataTable` is captured as immutable schema/record pages; the adapter is read-only and deterministic for its lifetime. | It does not refresh itself; a refresh must publish a replacement through the existing owner. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L303-L379) | `ExcelDataView` remains the WPF view, `DataConnector` is the typed snapshot, and `DataTransformError`/`PreviewRow` are surfaced properties. | Existing properties are not a complete transform authoring command model or a runtime UI proof. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1450-L1505) | Import loads through `ExcelDataService`, publishes the connector, keeps the authored header row and synchronizes fields. | It does not authorize a second import/parser path. |
| [`DataTransformPipeline.cs`](../src/ANLAbel.Core/Data/DataTransformPipeline.cs#L8-L147) | Transform definitions are named, topologically evaluated, lineage-producing and fail-closed on duplicates, parse errors and cycles. | It does not define operator copy, draft persistence or a WPF editor. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L4291-L4324) | Preview rows use the same transform pipeline and publish a diagnostic on failure. | The current UI does not yet expose draft/lineage repair controls. |
| [`DataSource.cs`](../src/ANLAbel.Core/Models/DataSource.cs#L5-L75) and [`DataSourceRegistry.cs`](../src/ANLAbel.Data/DataSourceRegistry.cs#L9-L167) | Shared sources have stable IDs, file/sheet/header values, usage history and versioned atomic registry persistence. | Registry fields do not prove that a Manager click-through or multi-template scan was run. |
| [`DatabaseManagerWindow.xaml`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L1-L116) and [`DatabaseManagerWindow.xaml.cs`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L39-L281) | The existing WPF dialog has list/detail, field persistence, async sheet/Test/Preview, usage warning, Relink/Use/Remove and guarded Cleanup paths. | It does not provide stable AutomationIds or prove target-scale runtime behavior. |
| [`ExcelDataService.cs`](../src/ANLAbel.Data/Excel/ExcelDataService.cs#L58-L118) | Sheet reads are asynchronous/cancelable, bounded by local/network timeouts, and Test Connection returns an explicit result. | A wait cursor alone is not cancellation, duplicate-action protection or stale-result rejection. |
| [`DataSourceCleanupWindow.xaml.cs`](../src/ANLAbel.App/DataSourceCleanupWindow.xaml.cs#L7-L73) | Cleanup acts only on supplied missing/unused candidates and requires explicit selection plus confirmation. | It does not establish a broader retention or deletion policy. |

## Surface ownership contract

| Concern | Data Workspace / Data tab | Database Manager | Single authority |
| --- | --- | --- | --- |
| Source identity/context | Read-only current source/connector context | Selects and edits shared registry entries | `DataSourceRegistry` + `Template.DatabaseConfig.DataSourceId` |
| Schema/records | Field list and selected sample page | Read-only connection/preview evidence | `ExcelDataService` → `DataTableDataConnector` |
| Transform definitions | Draft list/editor, Apply/Cancel/Remove and lineage | Not shown or edited | `LabelTemplate.DataTransforms` + `DataTransformPipeline` |
| Source CRUD | Deep-link to Manager | Add, Name, Sheet, Header, Relink, Remove, Cleanup | `MainViewModel`/`DataSourceRegistry` |
| Preview semantics | Derived sample used by binding/print | Raw source preview before Use; no implicit bind | Existing import/preview spine; no second evaluator |
| Print/dispatch guard | Reports binding/transform block | Does not dispatch or alter label objects | Existing preflight/print owner |
| Per-template Unlink | Separate Data-panel action | Never substituted by shared Remove | Existing `UnlinkExcel` contract |

The Manager may update a source that the current template uses, but the current template only reloads through `UseDataSourceAsync`/the existing import owner. The Workspace may display the source fingerprint and freshness, but it must not edit registry paths or bypass Manager confirmation.

## Figma metadata boundary (read-only)

Metadata was checked read-only on 2026-08-13 in panels file `kqyNBI0DgRHnPzJTDBIui5`. The values below are design evidence, not runtime measurements or product data.

| Node | Measured metadata | Safe use | Missing state |
| --- | --- | --- | --- |
| `8:2` | `664 × 788`, `Workspace + Properties` panel pair | Shell, header and panel vocabulary | No data workflow behavior |
| `8:5` / `8:15` | Workspace panel `300 × 700`; tabs `300 × 42` | Real Layers/Data task switch language | No transform task tab |
| `9:2` | Data tab content `300 × 610` | Data surface container | No transform editor or Manager route |
| `9:3` | Empty source card `276 × 142` | No-source/import state | No linked-source variants |
| `9:16` | Current data context `276 × 102` | Workbook/preview-row context | No schema/lineage detail |
| `9:27` | Collapsed settings `276 × 62` | Secondary disclosure for tracking/copies/transforms/shared sources | No authoring controls |
| `9:35` | Binding checks `276 × 42` | Compact diagnostic anchor | No transform-specific errors |
| Page `0:1` top-level | Frames `1:2`, `4:2`, `8:2`, `13:2`, `18:69`, `22:82` | Confirms the current inventory | No dedicated Database Manager frame |

**Routing rule:** use WPF `268/280` Data/Properties panel widths as the current product baseline and the Manager's authored `900 × 620` / `760 × 480` minimum as its separate baseline. A visual redesign requires a named state-specific node and owner decision; this packet authorizes no `get_design_context`, Figma write, new file or duplicated frame.

## State and failure matrix

| Surface/state | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| No source linked | `No data linked`, import action and no fake sample | Import or remain data-free | No transform preview or false green state |
| Source linked, no transforms | Source ID/name, schema/row context, selected sample and Add transform | Add a local draft or bind existing fields | Existing bindings remain unchanged |
| Workspace draft valid | Output name, normalized formula, sample value and lineage | Validate then Apply atomically | Draft cannot affect preview/dispatch before Apply |
| Workspace parse/missing/duplicate/cycle | Exact output, source field/cycle participants and repair text | Edit, remove or cancel draft | No raw fallback; preview/print remains blocked while effective definition is invalid |
| Source stale/failed | Timestamp/reason plus Refresh/Relink action | Refresh or relink through existing owner | Cached values never become a green current state |
| Manager no source selected | List, Save current link, Cleanup and selection explanation | Select/add or close | Detail fields and old preview are cleared |
| Manager request running | Operation/source snapshot, busy state and guarded action | Wait/cancel/retry using current snapshot | No duplicate write/read and no stale result overwrite |
| Manager connection/preview failed | File/sheet/header context, error and repair action; grid cleared on failure | Relink, edit, retry or close | Never show success or previous source's rows |
| Manager source used by current template | Explicit `Yes` usage and fallback warning | Confirm Use/Remove deliberately | Cancel leaves registry and template reference unchanged |
| Cleanup candidates | Missing path plus last-used/never-used evidence and checkboxes | Select then confirm once | Never delete unchecked candidates; no empty destructive dialog |
| Cross-surface refresh | New connector/source fingerprint and selected-row policy | Re-select/refresh through owner | Old sample/lineage cannot masquerade as current data |

## Proposed AutomationIds and keyboard paths

These IDs consolidate the proposals in the two UI specs. They are not current runtime IDs until the owner adds and verifies them.

| Surface | Region/control | Proposed `AutomationId` | Accessible name |
| --- | --- | --- | --- |
| Workspace | Root/context | `DataWorkspace.Root` / `DataWorkspace.SourceSummary` | Data workspace / Data source context |
| Workspace | Field/transform list | `DataWorkspace.SourceFieldList` / `DataWorkspace.TransformList` | Source fields / Committed transforms |
| Workspace | Editor | `DataWorkspace.TransformOutputName` / `DataWorkspace.TransformFormula` | Derived output name / Transform formula |
| Workspace | Result/lineage | `DataWorkspace.TransformResult` / `DataWorkspace.TransformLineage` | Sample transform result / Transform inputs and lineage |
| Workspace | Actions/status | `DataWorkspace.ValidateTransform` / `DataWorkspace.ApplyTransform` / `DataWorkspace.CancelTransform` / `DataWorkspace.Diagnostics` | Validate / Apply / Cancel / Data diagnostics |
| Manager | Root/list/detail | `DataSources.Manager.Root` / `DataSources.Manager.SourceList` / `DataSources.Manager.Detail` | Database Manager / Shared data sources / Selected data source details |
| Manager | Registry actions | `DataSources.Manager.AddCurrent` / `DataSources.Manager.Relink` / `DataSources.Manager.UseCurrent` / `DataSources.Manager.Remove` | Save current template data source / Relink data source file / Use for current template / Remove shared data source |
| Manager | Connection/preview | `DataSources.Manager.Sheet` / `DataSources.Manager.LoadSheets` / `DataSources.Manager.HeaderRow` / `DataSources.Manager.TestConnection` / `DataSources.Manager.Preview` | Worksheet name / Load worksheets / Header row number / Test data source connection / Preview data |
| Manager | Evidence | `DataSources.Manager.Status` / `DataSources.Manager.PreviewGrid` / `DataSources.Manager.Usage` | Data source status / Data preview / Current template usage |
| Cleanup | List/action | `DataSources.Cleanup.List` / `DataSources.Cleanup.RemoveSelected` | Orphaned data sources / Remove selected sources |

Workspace keyboard order is source/context → field list → sample row → transform list → editor → Validate/Apply/Cancel → diagnostics/binding checks. Manager order is source list/context → detail fields → Test/Preview → status/grid → Use/Remove → Close; Cleanup keeps candidate list → Remove Selected → Close. Each surface has one intentional scroll owner at narrow sizes.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `Workspace_NoSource_hasNoFalsePreview` | No-source card and Import action; no transform result | Runtime screenshot/UIA and no fabricated row |
| `Workspace_Apply_persistsDefinitionAndFingerprint` | Valid definition survives save/load/clone and changes document identity only after Apply | Core/App regression and snapshot evidence |
| `Workspace_InvalidDraft_keepsLastCommittedValue` | Parse/missing/duplicate/cycle names repair target; prior commit remains active | Diagnostic text plus blocked preview/print |
| `Workspace_Lineage_matchesEvaluator` | Sample value and `output ← input fields` come from `DataTransformPipeline` | Deterministic lineage assertion |
| `Workspace_AllRows_blocksOnFirstFailure` | Later valid rows cannot clear an earlier transform error | Batch diagnostic and no raw dispatch |
| `Manager_EmptyList_andAddCurrent` | Empty detail is non-stale; Save current link deduplicates and selects source | Click-through/UIA and registry assertion |
| `Manager_FieldEdits_persistAfterReload` | Name/sheet/header values persist; invalid header visibly normalizes to `1` | Registry reload and visible status |
| `Manager_RequestSnapshot_rejectsStaleResult` | Switching source or fields prevents old Test/Preview result from overwriting current state | Async fixture with source ID/path/sheet/header evidence |
| `Manager_TestConnection_failureMatrix` | Missing file/sheet/header/lock/cancel are repairable, non-green states | Runtime/UIA and Excel service result |
| `Manager_Preview_isReadOnly` | Populated/empty/read-failure states show source context; Preview never changes `DataSourceId` | Template identity before/after |
| `Manager_Remove_currentSource_warnsFallback` | Cancel is no-op; confirm clears only shared ID and preserves per-template fallback path | Confirmation text and registry/template assertion |
| `Manager_Cleanup_requiresExplicitSelection` | No candidates is informational; unchecked rows are untouched; selected rows remove after one confirmation | Child-dialog click-through and registry refresh |
| `CrossSurface_Use_refreshesSameConnector` | Manager Use and normal import publish the same source/connector identity; Workspace sees the replacement snapshot | Source/connector fingerprints and selected-row evidence |
| `Protected_TextTextBox_contract_unchanged` | Existing Text/TextBox named gates remain green | Required repository regression suite |

## No-go list

- Do not create a second data-source registry, transform evaluator, preview row store or dispatch path.
- Do not put shared-source CRUD, Relink, Remove or Cleanup into the R4 transform editor; deep-link to the Manager owner instead.
- Do not put formula authoring, lineage repair or transform Apply into Database Manager.
- Do not let Manager Preview silently bind the current template, or let a stale async result overwrite a new source selection.
- Do not use the linked-file freshness watcher as an automatic reload/print trigger or claim that a wait cursor proves cancellation.
- Do not fall back to raw values after an invalid effective transform, and do not mark stale/failed data as verified from cached rows.
- Do not change per-template Unlink semantics, shared-source fallback behavior, label object content/geometry or any Text/TextBox contract.
- Do not infer a transform editor, Manager dialog, source data, runtime accessibility or release readiness from Figma sample copy or dimensions.
- Do not create or edit a Figma file merely to fill the missing transform/Manager states.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the corresponding slice open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Canonical source identity/read model | `TBD` | `TBD` | `TBD` |  |
| D2. Surface/action ownership | `TBD` | `TBD` | `TBD` |  |
| D3. Delivery order/prerequisites | `TBD` | `TBD` | `TBD` |  |
| D4. Workspace formula/draft contract | `TBD` | `TBD` | `TBD` |  |
| D5. Manager request identity/async | `TBD` | `TBD` | `TBD` |  |
| D6. Manager mutation/cleanup safety | `TBD` | `TBD` | `TBD` |  |
| D7. Freshness/transform blocking | `TBD` | `TBD` | `TBD` |  |
| D8. Figma route and WPF baselines | `TBD` | `TBD` | `TBD` |  |
| D9. Accessibility/runtime evidence | `TBD` | `TBD` | `TBD` |  |
| D10. Closure/regression ownership | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the R4 Data Workspace and Database Manager slices may move from documentation review to implementation/runtime closure only after the applicable D1–D10 rows are filled, one source/connector authority and one action owner per mutation are named, the fixture matrix is converted into regression and target-scale UI evidence, and a clean implementation checkpoint links the results. Until then, these remain open UI/UX/data-surface contracts; this packet makes no release, Figma, typed-connector-parity or physical-output claim.
