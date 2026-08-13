# Database Manager and cleanup UI owner decision packet

**Status:** documentation-only owner gate; the existing WPF Manager/Cleanup workflow remains the source-management owner and no new Manager frame, registry rewrite, Figma edit or Text/TextBox change is authorized (2026-08-13)
**Module plan:** [`database-manager-module-plan.md`](database-manager-module-plan.md)
**UI/UX handoff:** [`DATABASE_MANAGER_UI_HANDOFF.md`](DATABASE_MANAGER_UI_HANDOFF.md)
**UI/UX specification:** [`DATABASE_MANAGER_UI_SPEC.md`](DATABASE_MANAGER_UI_SPEC.md)
**Cross-surface R4 packet:** [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md)
**Figma routing:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and boundary

The repository has a concrete WPF `DatabaseManagerWindow` and a separate `DataSourceCleanupWindow`. They own shared Excel-source selection, testing, preview, relink, use, removal and guarded orphan cleanup. The R4 packet defines the cross-surface source/read-model boundary; this packet makes the concrete Manager/Cleanup interaction owner and its runtime gaps reviewable.

This packet covers:

- the entry point, Manager shell, source list/detail and cleanup child;
- shared-source identity, field persistence and current-template usage evidence;
- async sheet/test/preview operations and stale-result/duplicate-action safety;
- relink, use, remove/fallback and orphan-cleanup confirmation semantics;
- read-only Figma reuse, stable AutomationId proposals and target-scale evidence;
- regression fixtures for registry, usage, header-row and cleanup behavior.

It does not add a transform editor, merge Data Workspace and Manager ownership, change the per-template `Unlink Excel` contract, delete files or print logs, infer a Control Center Manager frame, edit Figma, or alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or print parity. Blank owner rows keep this slice open.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Entry and host ownership | Keep `MainWindow` as the entry owner and `DatabaseManagerWindow` as the shared-source owner. Keep `DataSourceCleanupWindow` as a child maintenance flow; keep per-template Unlink separate. | Confirm the entry/return path and one owner for every registry or template mutation. |
| D2. Source identity and registry authority | `DataSource.Id` is the stable shared identity; `%AppData%\\ANLAbel\\data-sources.json` is the registry authority. Templates reference the ID while retaining their own fallback file/sheet path. | Approve identity/display-name copy and whether registry diagnostics are visible in the first runtime slice. |
| D3. No-selection and selection reset | A source selection clears old test result and preview, hides detail when no source is selected, then loads name/path/sheet/header/usage for the selected ID. | Approve empty-state copy and selection retention after add, relink, remove and cleanup refresh. |
| D4. Field editing and persistence | Name, sheet and header edits persist through the existing source owner; header values normalize to at least `1`. Test/preview must use a captured file/sheet/header snapshot. | Approve save-on-focus-loss versus explicit Apply wording and invalid-header copy. |
| D5. Async request safety | Keep sheet discovery, connection test and preview off the UI thread. Current code shows a wait cursor but does not disable duplicate buttons, cancel, or identify a request; treat stale-result/duplicate-action handling as an open gate. | Choose cancellation/timeout policy, busy controls and request identity before implementation changes. |
| D6. Relink and use | Relink validates that the replacement has at least one sheet, updates the shared source and refreshes the current template only when it references that ID. Use explicitly imports the shared source and records usage. | Approve replacement-file/sheet selection copy and whether a failed Use leaves the current link marked broken. |
| D7. Remove and fallback | Remove requires confirmation with current-template usage and fallback consequence; registry removal clears only `DataSourceId` for the current template and keeps its own path/sheet. | Confirm warning copy, affected-template wording and post-remove selection/status. |
| D8. Orphan cleanup | `Clean up...` admits only missing files unused for 30+ days (or never used), then requires checkbox selection and one bulk confirmation. It is maintenance, not file deletion or retention policy. | Approve threshold/time-zone copy, empty-candidate message and whether the irreversible warning names registry-only removal. |
| D9. Figma and accessibility route | Reuse Figma `8:2`/`9:2` shell/card vocabulary read-only. No Manager frame is needed for the current state questions; stable IDs and target-scale evidence belong to WPF. | Name the design/UIA owner if a concrete missing state later requires the smallest state-specific reference. |
| D10. Closure and regression | Close only with click-through screenshots/UIA at target scales, source fixtures and clean implementation evidence. This packet adds no code/test result. | Fill sign-off rows and link the smoke-test, regression output and closing checkpoint. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L934) and [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L566) | `Manage Data Sources...` opens an owned `DatabaseManagerWindow` from the Data area. | The entry is not runtime-proven at target sizes by source alone. |
| [`DatabaseManagerWindow.xaml`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L1-L18) | The dialog is `900 x 620`, minimum `760 x 480`, centered on its owner and uses app brushes/styles. | Size and styling are not target-scale acceptance evidence. |
| [`DatabaseManagerWindow.xaml`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L19-L51) | The left column owns shared-source list, add-current-link and cleanup entry. | A list does not prove source identity, empty state or post-action selection behavior. |
| [`DatabaseManagerWindow.xaml`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L53-L116) | The detail column owns name/path/relink, sheet/load, header, Test/Preview, usage status, Use and Remove; preview is read-only and scrollable. | Buttons have no stable `AutomationId`, and presence does not prove guarded async behavior. |
| [`DatabaseManagerWindow.xaml.cs`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L18-L74) | Selection resets result/preview and populates detail/usage from the selected source; usage follows the current template's `DataSourceId`. | It does not prove selection persistence after registry mutation. |
| [`DatabaseManagerWindow.xaml.cs`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L76-L127) | Name, sheet and header changes persist through focus/selection handlers; header values normalize invalid input to `1`. | Focus-loss persistence and invalid-input copy need click-through evidence. |
| [`DatabaseManagerWindow.xaml.cs`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L129-L205) | Sheet discovery, connection test and preview call `ExcelDataService` asynchronously and show inline success/failure text. | Current code uses only a wait cursor; it does not cancel, disable duplicate starts or reject a late result for a different source/field snapshot. |
| [`DatabaseManagerWindow.xaml.cs`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L207-L257) | Relink, Use and Remove are explicit actions; removal explains current-template fallback before registry mutation. | A dialog confirmation is not evidence that all affected templates or runtime bindings were reviewed. |
| [`DatabaseManagerWindow.xaml.cs`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L264-L281) | Cleanup filters missing files plus `LastUsedUtc` null/older than 30 days, reports empty candidates and opens the child dialog. | It does not establish a broader retention, file-delete or privacy policy. |
| [`DataSourceCleanupWindow.xaml`](../src/ANLAbel.App/DataSourceCleanupWindow.xaml#L1-L42) and [code-behind](../src/ANLAbel.App/DataSourceCleanupWindow.xaml.cs#L12-L73) | The child lists missing/unused sources with checkboxes and requires selected rows plus one confirmation before registry removal. | It does not prove keyboard access, selection persistence or post-close Manager refresh. |
| [`DataSource.cs`](../src/ANLAbel.Core/Models/DataSource.cs#L10-L75) | Shared IDs, file/sheet/header, last-use time and capped recent-template provenance are model fields; display name falls back to file/sheet. | `RecentTemplates` is evidence for copy/diagnostics, not a complete disk scan of all affected templates. |
| [`DataSourceRegistry.cs`](../src/ANLAbel.Data/DataSourceRegistry.cs#L15-L130) | Registry load supports legacy arrays, versioned documents and future-schema rejection; save uses a temp file and replace. | Atomic save does not prove UI recovery copy for permission/corrupt-registry failures. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L193-L196) and [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1991-L2148) | Commands add idempotently, use/import, remove/fallback, relink and persist shared sources; usage records `LastUsedUtc` and recent templates. | Command code does not prove WPF enabled-state, focus order or stale async-result behavior. |
| [`Program.cs`](../src/ANLAbel.Tests/Program.cs#L6331-L6388), [`Program.cs`](../src/ANLAbel.Tests/Program.cs#L6806-L6840), [`Program.cs`](../src/ANLAbel.Tests/Program.cs#L6916-L6966) and [`Program.cs`](../src/ANLAbel.Tests/Program.cs#L7061-L7115) | Registry CRUD, idempotent add, usage tracking and shared-source relink have named application-runner coverage. | No named test here proves the Manager/Cleanup WPF click-through or target-scale layout. |

## Surface and action ownership

| Surface/action | Current owner | Safe action | Boundary |
| --- | --- | --- | --- |
| Data-panel entry | `MainWindow` | Open one owned Manager dialog | Do not duplicate Manager in Properties/Data Workspace. |
| Shared-source list/add | `DatabaseManagerWindow` + `MainViewModel.AddCurrentAsDataSourceCommand` | Select a stable ID or save the current link idempotently | No duplicate registry rows for the same file/sheet. |
| Name/sheet/header fields | `DatabaseManagerWindow` + `DataSource`/registry | Persist authored source metadata | Do not mutate object bindings or label geometry. |
| Load sheets/Test/Preview | `DatabaseManagerWindow` + `ExcelDataService` | Show result for the current request snapshot | No green state from stale fields, hidden errors or duplicate async operations. |
| Relink | `MainViewModel.RelinkDataSourceAsync` | Select a readable file with at least one sheet, then update shared identity | Failed/canceled dialog leaves the prior registry entry unchanged. |
| Use current template | `MainViewModel.UseDataSourceAsync` | Explicitly set `DataSourceId`, import rows and record usage | Use is not a silent binding or geometry rewrite. |
| Remove | `DatabaseManagerWindow` confirmation + `MainViewModel.RemoveDataSource` | Remove one chosen registry ID and clear current ID while retaining fallback fields | No file deletion, log deletion or implicit unlink of authored object bindings. |
| Orphan cleanup | `DatabaseManagerWindow` + `DataSourceCleanupWindow` | Remove only checked missing/30-day-unused registry entries after one confirmation | Maintenance is not retention policy or physical erasure. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Manager opening | Owned dialog, source list and selection explanation | Wait/select/add or close | Do not show stale detail from a previous source. |
| No source selected | Empty detail marker, Save current link and Clean up | Select or add a source | No Test/Preview/Use/Remove action. |
| Selected source, not used | Name/path/sheet/header and `Used by ...: No` | Test, preview, relink, use or remove | Do not imply current-template mutation until Use succeeds. |
| Selected source, used | Explicit `Yes` usage evidence | Test/preview/use; remove only after warning | Keep the template's fallback path and explain its consequence. |
| Sheet load running | Wait/busy status tied to source | Wait/cancel/retry according to approved policy | Do not start a second load or apply a late list to another source. |
| Connection checking | Operation and source/field snapshot | Wait/retry/repair | Do not show success for a different file, sheet or header. |
| Connection verified | File/sheet/header plus row/column evidence | Preview or Use | Verification is data-source evidence, not print/physical-output evidence. |
| Connection failed/stale | Error severity, reason and Relink/sheet/header repair path | Repair and retry | Keep source unverified; never leave a previous green result visible. |
| Preview loaded | Read-only grid, row/column count and source context | Inspect, then Use or close | Preview never silently binds the template. |
| Preview empty/failed/locked | Explicit zero-row or error status; no stale grid | Repair source/sheet/header or close | Do not show the previous source's preview as current. |
| Remove confirmation | Source name, current-template usage and fallback consequence | Cancel or confirm once | Cancel/close leaves registry and template unchanged. |
| Cleanup no candidates | Informational missing/unused threshold message | Close | Do not show an empty destructive dialog. |
| Cleanup candidates | Checkbox, path and last-used/never-used evidence | Select, confirm once or close | Never remove unchecked rows. |
| Cleanup completed | Child closes and Manager list/selection refreshes | Continue or close | Do not show a removed source as active. |
| Registry load/save failure | Actionable permission/corrupt/future-schema message | Repair/backup/close through explicit owner | Never overwrite an unreadable registry with an empty list. |

## Figma metadata boundary

Read-only metadata was rechecked on 2026-08-13 for the existing [ANLAbel UI exploration file](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), Page `0:1`. The page has no Manager frame; the relevant shell nodes are:

| Node | Metadata | Safe reuse | Missing Manager state |
| --- | --- | --- | --- |
| `8:2` | Frequency-first panel pair, `664 x 788` | Workspace/Data shell spacing and card language | No list/detail dialog, source identity or cleanup flow |
| `8:5` | Workspace panel, `300 x 700` | Data-panel context only | No shared registry owner or Manager action region |
| `9:2` | Data tab content, `300 x 610` | Empty/current/settings/binding-check hierarchy | No Test/Preview/Relink/Remove state machine |
| `9:3` | Empty-source card, `276 x 142` | No-source/import vocabulary | No shared-source list or validation result |
| `9:16` | Current data context card, `276 x 102` | Workbook/preview-row summary language | No sheet/header editor or source usage warning |
| `9:27` | Collapsed data-settings card, `276 x 62` | Secondary disclosure pattern | No cleanup/confirmation state |
| `9:35` | Binding-check card, `276 x 42` | Compact status treatment | No connection/preview diagnostics |
| `22:82` | Excel Link Verification symbols, `620 x 455` | Five-state link vocabulary only | Not a Database Manager design; do not copy it as a registry screen |

**Routing decision:** reuse the shell/card vocabulary from `8:2`/`9:2` only as visual input and keep the existing WPF `900 x 620` Manager plus `560 x 440` cleanup child as the product baseline. Do not call `get_design_context`, create a Manager frame or edit Figma for this documentation-only slice. If a future redesign needs a concrete state, name the state and request the smallest state-specific reference before changing WPF.

## Accessibility and responsive gate

The current Manager/Cleanup XAML has no stable `AutomationProperties.AutomationId`; the following are proposals only:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Manager root | `DataSources.Manager.Root` | Database Manager |
| Source list | `DataSources.Manager.SourceList` | Shared data sources |
| Add current link | `DataSources.Manager.AddCurrent` | Save current Excel link |
| Cleanup entry | `DataSources.Manager.Cleanup` | Clean up orphaned data sources |
| Detail panel | `DataSources.Manager.Detail` | Selected data source details |
| Name | `DataSources.Manager.Name` | Source name |
| File path | `DataSources.Manager.FilePath` | Excel file path |
| Relink | `DataSources.Manager.Relink` | Relink Excel file |
| Sheet/load | `DataSources.Manager.Sheet`, `DataSources.Manager.LoadSheets` | Sheet / Load sheets |
| Header row | `DataSources.Manager.HeaderRow` | Header row |
| Test/Preview | `DataSources.Manager.TestConnection`, `DataSources.Manager.Preview` | Test connection / Preview data |
| Result/status | `DataSources.Manager.Result` | Data-source test and preview status |
| Preview grid | `DataSources.Manager.PreviewGrid` | Preview rows |
| Use/remove/close | `DataSources.Manager.Use`, `DataSources.Manager.Remove`, `DataSources.Manager.Close` | Use for current template / Remove source / Close |
| Cleanup root/list | `DataSources.Cleanup.Root`, `DataSources.Cleanup.List` | Clean up orphaned data sources / Candidates |
| Cleanup actions | `DataSources.Cleanup.RemoveSelected`, `DataSources.Cleanup.Close` | Remove selected / Close |

Runtime evidence must cover `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception), keyboard order from source list through detail/status/preview to Use/Remove/Close, visible focus after relink/confirmation/child return, one scroll owner for detail and one for preview, and no horizontal clipping of required action copy. Async evidence must include a source/field snapshot, duplicate-click behavior, cancellation/timeout policy and late-result rejection.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `DataSources_Manager_EmptySelectionClearsDetail` | Selecting none hides detail and cannot expose a prior source's fields/result/grid. | WPF click-through/UIA evidence. |
| `DataSources_AddCurrentIsIdempotent` | Repeated Save current link reuses the same ID for the same file/sheet. | Existing runner coverage plus source-list screenshot. |
| `DataSources_FieldEditPersistsAndNormalizesHeader` | Name/sheet persist; invalid/zero header becomes `1` before the next operation. | Registry before/after fixture and focus path. |
| `DataSources_TestUsesCurrentSnapshot` | Test result identifies file/sheet/header used; a changed selection cannot receive the old result. | Async fixture and status capture. |
| `DataSources_PreviewFailureClearsOldGrid` | Failed/empty/locked preview hides the previous grid and preserves an actionable error. | Controlled Excel fixture and screenshot. |
| `DataSources_RelinkRequiresReadableSheet` | Cancel/failure/no-sheet leaves the original registry entry; success updates shared identity and current template only when linked. | Relink fixture and registry assertion. |
| `DataSources_UseRecordsUsageAndHeader` | Use imports the selected source, preserves header row and updates `LastUsedUtc`/recent template provenance. | Existing runner coverage plus UIA result. |
| `DataSources_RemoveExplainsFallback` | Cancel leaves registry unchanged; confirm removes only the selected ID and clears current `DataSourceId` while preserving fallback fields. | Operator confirmation and persistence fixture. |
| `DataSources_CleanupEligibilityAndBulkConfirm` | Only missing/30-day-unused sources enter the child; unchecked rows survive; cancel survives; confirmed rows are removed. | Boundary-time fixture and child dialog evidence. |
| `DataSources_RegistryFutureSchemaFailsClosed` | Future schema or corrupt save/load shows repair copy and never replaces data with an empty registry. | Registry fixture and error-state capture. |
| `Protected_TextTextBox_contract_unchanged` | Manager/Cleanup work changes no Text/TextBox ownership, frame geometry, wrap/clip, padding, resize or print parity. | Protected regression suite after any implementation change. |

## No-go list

- Do not infer a Database Manager screen from the generic Figma Data shell or Excel-link verification symbols.
- Do not put formula authoring, transform Apply, lineage repair or connector-specific editing into this Manager packet.
- Do not silently mutate object bindings, label geometry or the protected Text/TextBox contract when using/removing a shared source.
- Do not treat Test Connection, Preview loaded, registry persistence or Figma sample data as print or physical-output evidence.
- Do not start duplicate async operations, apply a late result to a different source/field snapshot or leave stale green status after failure.
- Do not remove files, print logs, templates or data outside the selected registry entries; cleanup is registry maintenance only.
- Do not bypass current-template fallback warnings or introduce a hidden default source/worksheet.
- Do not edit Figma or create a state frame merely to make the documentation appear complete.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the packet open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Entry and host/action ownership | `TBD` | `TBD` | `TBD` |  |
| D2. Source identity/registry authority | `TBD` | `TBD` | `TBD` |  |
| D3. Selection/empty-state behavior | `TBD` | `TBD` | `TBD` |  |
| D4. Field persistence/header policy | `TBD` | `TBD` | `TBD` |  |
| D5. Async snapshot/cancellation/duplicate policy | `TBD` | `TBD` | `TBD` |  |
| D6. Relink/use semantics | `TBD` | `TBD` | `TBD` |  |
| D7. Remove/fallback warning | `TBD` | `TBD` | `TBD` |  |
| D8. Cleanup eligibility/confirmation | `TBD` | `TBD` | `TBD` |  |
| D9. Figma route/AutomationIds | `TBD` | `TBD` | `TBD` |  |
| D10. Runtime/UIA/QA closure | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the Database Manager/Cleanup slice may move from documentation review to implementation/release evidence only after D1-D10 are filled, one source/registry owner exists per mutation, async and registry fixtures pass, target-scale UIA/screenshots are attached, and the current Text/TextBox/Unlink contract remains unchanged. Until then this is an open source-management UI contract and makes no Figma, print, physical-output or release claim.
