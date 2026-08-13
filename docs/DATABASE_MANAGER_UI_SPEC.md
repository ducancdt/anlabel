# ANLAbel — Database Manager UI/UX specification

**Status:** documentation-only runtime/design contract; implementation exists but click-through evidence is open (2026-08-13)
**Handoff:** [`DATABASE_MANAGER_UI_HANDOFF.md`](DATABASE_MANAGER_UI_HANDOFF.md)
**Module plan:** [`database-manager-module-plan.md`](database-manager-module-plan.md)
**Figma reference:** panels file `kqyNBI0DgRHnPzJTDBIui5`, shell `8:2`; no dedicated Manager frame exists

This document turns the existing Database Manager handoff and WPF dialog into a measurable UX contract. It does not redesign the window, change shared-source or Unlink semantics, or claim that the current runtime has been manually verified.

## 1. Operator outcome

From one owner window, an operator should be able to:

1. see shared data sources and add the current template's link;
2. select a source and inspect its name, file, sheet, header row and current-template usage;
3. load sheets, test the connection and preview rows using the current values;
4. relink a missing file or use the source for the current template explicitly;
5. remove a source only after seeing the fallback consequence;
6. open a separate guarded cleanup flow for missing, long-unused sources.

The per-template **Unlink Excel** action remains a separate escape hatch. Database Manager must not mutate binding expressions, label geometry or authored Text/TextBox behavior as a side effect of source management.

## 2. Current WPF evidence and ownership

| Region/action | Current evidence | UX acceptance boundary |
| --- | --- | --- |
| Entry point | [`MainWindow.xaml#L934`](../src/ANLAbel.App/MainWindow.xaml#L934) exposes `Manage Data Sources...`; [`MainWindow.xaml.cs#L566`](../src/ANLAbel.App/MainWindow.xaml.cs#L566) opens an owned `DatabaseManagerWindow`. | The click-through must prove the entry is reachable from the Data panel at target sizes. |
| Manager shell | [`DatabaseManagerWindow.xaml#L1`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L1) declares `900 × 620`, minimum `760 × 480`; left list is `260 DIP`, right detail is scrollable. | Primary actions and source context must remain reachable at `1024 × 600`, 100%, 125% and 150%. |
| Source list | `SourceListBox` is bound to `DataSources`; `AddCurrentAsDataSourceCommand` saves the current link. | Empty list, selection and post-add selection must not show stale detail values. |
| Detail fields | Name, read-only file path, `Relink...`, editable Sheet/`Load sheets`, Header row, `Test Connection`, `Preview data...`. | Each field change must be reflected in the next test/preview and persisted by the existing source owner. |
| Usage/removal | `UsedByCurrentTemplateText` reports current-template use; `Remove_Click` confirms and explains fallback. | Confirmation must distinguish current-template use from other references; cancel leaves the registry unchanged. |
| Cleanup | `CleanUp_Click` selects missing files unused for 30+ days and opens `DataSourceCleanupWindow` (`560 × 440`, minimum `440 × 320`). | Empty candidates, unchecked selection, cancel and bulk confirmation require visible outcomes. |
| Async evidence | Excel service methods run off the UI thread and `TestConnectionAsync` returns an explicit result; the current window shows a wait cursor while loading. | Runtime evidence must prove busy/duplicate-action behavior, failure messaging and recovery; a green result must match the current file/sheet/header. |

The current implementation is the working information architecture. A visual redesign is a separate owner decision, not an inference from the Control Center research shell.

## 3. Read-only Figma routing

Metadata for panels file `kqyNBI0DgRHnPzJTDBIui5` was checked read-only on 2026-08-13. Node `8:2` is a `664 × 788` frequency-first panel pair with a `300 × 700` Workspace/Data panel (`8:5`) and a `300 × 700` Properties panel (`8:6`). Its Data shell contains:

| Figma node | Size / role | Reusable evidence | Missing Manager state |
| --- | --- | --- | --- |
| `8:2` | `664 × 788` panel pair | Shell spacing, header, card and Data/Properties vocabulary | No source list/detail dialog |
| `9:2` | `300 × 610` Data tab content | Empty/current/settings/binding-check card language | No Test/Preview/Relink/Remove flow |
| `9:3` | `276 × 142` empty-source card | No-source/import state | No shared-source registry |
| `9:16` | `276 × 102` current-context card | Workbook/preview-row context | No sheet/header editor |
| `9:27` | `276 × 62` collapsed settings card | Secondary disclosure pattern | No cleanup or confirmation states |
| `9:35` | `276 × 42` binding-check card | Compact status treatment | No connection/preview diagnostics |

**Routing decision:** reuse the existing Figma shell/card vocabulary only as a visual reference, while the current WPF `900 × 620` dialog remains the product baseline. Do not widen the main `268/280` columns, create a Manager frame, or edit Figma merely to fill the evidence gap. Acceptance is a runtime screenshot/UI Automation measurement, not a Figma frame.

## 4. Information architecture

```text
[Database Manager: shared sources                             Close]

[Shared data sources]                 [Selected source detail]
[description]                         Name:        [             ]
[+ Save current link]                 Excel file:  [read-only] [Relink...]
[source list]                         Sheet:       [          ] [Load sheets]
[                                    ] Header row: [   ]
[Clean up...]                         [Test Connection] [Preview data...]
                                      [status / repair evidence]
                                      [read-only preview grid]
                                      [Used by current template: Yes/No]
                                      [Use for current template] [Remove...]

[                                                              Close]
```

The primary order is source context → connection details → Test/Preview evidence → Use/Remove. Cleanup is maintenance and must not compete with normal source selection. A selected source change clears the old status and preview before loading new detail values.

### 4.1 Cleanup child flow

```text
[Clean up orphaned data sources]
[missing path + last-used/never-used evidence]
[ ] Source A   C:\...\missing.xlsx — last used 2026-07-01
[ ] Source B   C:\...\gone.xlsx    — never used
[Remove Selected] [Close]
```

Only sources whose file is missing and whose `LastUsedUtc` is null or older than 30 days may enter this list. Selection, confirmation and deletion are separate states; an empty candidate set is informational, not an empty destructive dialog.

## 5. State and transition matrix

| State | Visible evidence | Safe action | Forbidden implication |
| --- | --- | --- | --- |
| Manager, no source selected | Source list, Save current link, Clean up and selection explanation | Select or add a source; close | Do not show stale detail fields |
| Source selected, not used | Name/path/sheet/header plus `Used by current template: No` | Test, preview, relink, use or remove | Do not imply the current template changed |
| Source selected, used | Same detail plus explicit `Yes` usage evidence | Test/preview/use; removal requires warning | Do not claim unlink preserves shared-source registry automatically |
| Sheet list loading | Busy state on Load sheets and current source identity | Wait/cancel if supported; retry after failure | No duplicate async write or stale sheet list |
| Connection checking | Busy status tied to file/sheet/header snapshot | Wait or retry | Do not show green for a different field set |
| Connection verified | Success message with file, sheet, header, columns and rows | Preview or Use | Verification is not a print or physical-output claim |
| Connection failed/stale | Error severity, reason and Relink/sheet/header repair action | Repair and retry | Never convert failure to a green state |
| Preview loaded | Read-only grid, row/column count and source context | Inspect, then Use or close | Preview must not silently bind the template |
| Preview failed/empty | Error or explicit zero-row state, no stale grid | Repair source/sheet/header | Do not leave the previous source preview visible |
| Remove confirmation | Source name, current-template usage and fallback consequence | Cancel or confirm once | No deletion on dialog close/Cancel |
| Cleanup no candidates | Reason: missing file + unused 30+ days found none | Close | Do not show an empty bulk-delete action |
| Cleanup candidates | Checkboxes, missing paths and last-used/never-used details | Select, confirm once, or close | Never remove unchecked candidates |
| Cleanup completed | Child dialog closes; source list/selection refreshes | Continue or close Manager | Do not report a removed source still as active |

## 6. Async, persistence and safety rules

1. Every Test/Preview/Load-sheets result is scoped to the source ID, file path, sheet and header row used for that request.
2. While an async action is running, its button is disabled or otherwise guarded against duplicate starts; status text names the operation and source.
3. Cancellation, timeout and file-lock failures remain recoverable UI states. The underlying Excel service already distinguishes local/network timeout behavior; the window must not turn an exception into an unexplained close.
4. Editing Name, Sheet or Header row persists through the existing `DataSourceRegistry` owner; invalid header input normalizes visibly and must not silently target another row.
5. `Use for current template` changes only the template's data-source identity and associated preview/binding state. It must not rewrite object content or geometry.
6. Removing a shared source leaves each affected template's documented fallback behavior intact and records the usage warning before confirmation.
7. Cleanup removes only explicitly selected candidates after confirmation; it does not touch the current template's one-off link unless the owner explicitly changes that contract.

## 7. Proposed accessibility and AutomationIds

These IDs are proposals for the runtime owner; they are not current IDs until added and verified:

| Region/control | Proposed `AutomationId` | Accessible name |
| --- | --- | --- |
| Manager root | `DataSources.Manager.Root` | `Database Manager` |
| Source list | `DataSources.Manager.SourceList` | `Shared data sources` |
| Save current link | `DataSources.Manager.AddCurrent` | `Save current template data source` |
| Cleanup | `DataSources.Manager.Cleanup` | `Clean up orphaned data sources` |
| Detail panel | `DataSources.Manager.Detail` | `Selected data source details` |
| Name | `DataSources.Manager.Name` | `Data source name` |
| File path | `DataSources.Manager.FilePath` | `Excel file path` |
| Relink | `DataSources.Manager.Relink` | `Relink data source file` |
| Sheet | `DataSources.Manager.Sheet` | `Worksheet name` |
| Load sheets | `DataSources.Manager.LoadSheets` | `Load worksheets` |
| Header row | `DataSources.Manager.HeaderRow` | `Header row number` |
| Test connection | `DataSources.Manager.TestConnection` | `Test data source connection` |
| Preview | `DataSources.Manager.Preview` | `Preview data` |
| Result/status | `DataSources.Manager.Status` | `Data source status` |
| Preview grid | `DataSources.Manager.PreviewGrid` | `Data preview` |
| Usage evidence | `DataSources.Manager.Usage` | `Current template usage` |
| Use | `DataSources.Manager.UseCurrent` | `Use for current template` |
| Remove | `DataSources.Manager.Remove` | `Remove shared data source` |
| Cleanup list | `DataSources.Cleanup.List` | `Orphaned data sources` |
| Cleanup remove | `DataSources.Cleanup.RemoveSelected` | `Remove selected sources` |

Keyboard order must follow source list/context → details → Test/Preview → status/grid → Use/Remove → Close. Error announcements should include source name and repair action, not only a color change.

## 8. Responsive and runtime acceptance

| Target | Required behavior | Evidence |
| --- | --- | --- |
| `1280 × 800` | Preserve two-column list/detail layout; preview grid may scroll inside its owner | Screenshot/UI Automation for selection, Test, Preview and Use |
| `1024 × 600` | Keep header, selected source, primary actions and status visible; detail scrolls internally without hiding Remove/Use | Screenshot/UI Automation for no-selection, failure and removal warning |
| `100%`, `125%`, `150%` | No clipping of fields/buttons; keyboard focus remains visible | Record window size, scale, focus order and scroll owner |
| Cleanup `560 × 440` / narrow minimum | Candidate evidence and Remove Selected/Close remain visible; list scrolls independently | Screenshot/UI Automation for no-candidate, unchecked and confirmed paths |

## 9. Runtime verification checklist

Before calling the Manager UX slice verified, capture evidence for:

1. Data-panel entry → owned Manager window → close.
2. Empty source list and Save current link.
3. Select source; edit name/sheet/header; reopen and confirm persistence.
4. Load sheets: valid workbook, missing file, locked/read failure and wrong sheet.
5. Test Connection: valid, empty sheet/header, invalid header and stale/missing file.
6. Preview: populated rows, zero rows, read failure and no stale grid after source switch.
7. Relink success/failure and refreshed usage evidence.
8. Use for current template preserves source ID/schema/row behavior without changing label geometry.
9. Remove cancel/confirm, current-template warning and registry refresh.
10. Cleanup no candidates, unchecked selection, cancel and confirmed bulk removal.
11. Target-size/scale screenshots or UI Automation at `1024 × 600`, `100%`, `125%` and `150%` (or documented exception).

Existing M1–M3 tests, Excel verification, R4 transform diagnostics and protected Text/TextBox gates must remain green. A Figma shell, build or unit test does not replace this click-through evidence.

## 10. Explicit non-goals and owner decisions

Non-goals: a new shared-source service, multi-user ACLs, ODBC/SQL/HTTP connectors, cloud synchronization, automatic printing, destructive cleanup without confirmation, a new Figma Manager frame, or any Text/TextBox contract change.

Before a visual or behavioral change, the owner must confirm:

1. the current WPF information architecture versus a redesigned Manager reference;
2. operator copy and stable AutomationIds;
3. cancellation/timeout presentation for async actions;
4. runtime screenshot/UI Automation ownership and clean implementation commit.

Until those decisions and click-through evidence exist, this document remains a verification specification, not a release or UX-complete claim.
