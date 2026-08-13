# Designer shell, frequency-first panels and Excel verification owner decision packet

**Status:** documentation-only owner gate; no shell-width change, tab rename, Figma write, or Text/TextBox change is authorized by this packet (2026-08-13)
**Shell research:** [`NICELABEL_DESIGNER_SHELL_RESEARCH.md`](NICELABEL_DESIGNER_SHELL_RESEARCH.md)
**Panel design note:** [`industrial-panel-design.md`](industrial-panel-design.md)
**Figma handoff template:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Excel/data history:** [`database-plan.md`](database-plan.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

The remaining non-Control-Center UI review questions are coupled to the same operator path: the MainWindow shell hosts the frequency-first Workspace and Properties panels, and the Properties panel carries the Excel-link trust state. This packet makes the evidence and owner choices explicit without treating a Figma measurement as a runtime measurement.

The packet covers:

- shell region ownership and the existing `Shell.*` accessibility surface;
- the working `268 DIP` Workspace / `280 DIP` Properties baseline versus the rounded `300/300 DIP` exploration frames;
- the `Advanced` versus historical `More` third-tab label;
- the five-state Excel link verification card and its stale-data boundary;
- target-scale, keyboard/focus, scroll and regression closure.

It does not add a shell host, change MainWindow XAML, create a Properties redesign, add a Database Manager Figma frame, or alter any Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or print invariant. A blank owner row keeps the affected decision open.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Shell host and region ownership | Keep the existing MainWindow as the single designer-shell host. Preserve the R1-R7 region map and current `Shell.QuickAccess`, `Shell.Ribbon`, `Shell.Toolbox`, `Shell.Workspace`, `Shell.Canvas`, `Shell.Properties` and `Shell.Status` IDs; treat `Shell.LeftColumn` as the existing structural helper. | Confirm one host, one owner for each region action, and the accessibility owner for any new child IDs. |
| D2. Panel width baseline | Keep WPF `268 DIP` Workspace and `280 DIP` Properties as the working product baseline. The Figma `300/300` panels are a rounded visual reference; the full-shell `R3/R4` and `R6` frames include outer regions, margins and splitters and are not a direct runtime width prescription. | Approve or reject the baseline only after a target-scale screenshot/UIA measurement at the supported minimum window and display scales. |
| D3. Frequency-first disclosure | Keep `Layers` and `Data` as real Workspace tabs with `Data` selected by default, keep secondary data settings collapsed, keep Properties docked/available, and give each panel one intentional vertical scroll owner. | Confirm the default tab, narrow-window behavior and scroll/focus owner before any layout refactor. |
| D4. Properties third label | Keep the operator-facing label `Advanced` and its accessible name `Advanced object properties`. Figma `Properties tab / More` (`18:88`) is an earlier visual variant; it is not a reason to rename the shipped surface. | If a rename is desired, choose one final label and update visual copy, `AutomationProperties.Name`, keyboard acceptance and regression names atomically. |
| D5. Excel state machine | Use exactly `NotLinked`, `Checking`, `Verified`, `Stale` and `Failed`. `Verified` is issued only after the workbook, selected sheet and header can be read; stale or failed data is never presented as current success. | Name the product/implementation owner for state copy, transition policy and print/preflight wording. |
| D6. Freshness and action ownership | `MainViewModel` owns the state, verification, freshness watcher and command semantics; `MainWindow` renders the card. `DatabaseManagerWindow` remains the owner of shared-source Test/Preview/Relink/Use/Remove/Cleanup. A file change marks `Stale`; it does not silently reload rows. | Confirm the command owner and whether any cross-link from the Properties card should deep-link to Manager without duplicating mutations. |
| D7. Verification evidence | Show source file/sheet, verified column/row counts and check time only for the current successful snapshot. `Checking`, `Stale` and `Failed` retain repair copy and never reuse old green evidence. | Approve the exact evidence fields, redaction policy and the runtime artifact path for each state. |
| D8. Figma routing | Reuse shell `2:2`, panels `13:2`/`18:69`, and Excel component `22:82` as read-only state references. If a question is not answered by those nodes, record WPF reuse or request the smallest state-specific reference; do not create a duplicate file. | Name the design owner for a missing state and the evidence owner for the corresponding WPF screenshot/UIA run. |
| D9. Runtime/accessibility gate | Measure `1024 x 600` at `100%`, `125%` and `150%` (or record an environment exception), verify keyboard/focus order and absence of default horizontal scrolling, then attach state screenshots/UIA evidence. | Name App/UIA, runtime screenshot and QA owners; record the target artifact paths. |
| D10. Closure and regression ownership | Convert the fixtures below into named Core/App/runtime gates and a clean implementation checkpoint. Documentation alone does not close the shell, panel or Excel state matrix. | Fill the owner/sign-off table and link the closing commit, screenshots, UIA measurements and regression output. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`NICELABEL_DESIGNER_SHELL_RESEARCH.md`](NICELABEL_DESIGNER_SHELL_RESEARCH.md#L32-L79) | The shell is mapped into R1 Quick Access, R2 Ribbon, R3 Toolbox, R4 Workspace, R5 Design Surface, R6 Object Properties and R7 Status Bar; the research records the existing `Shell.*` IDs and the protected Text/TextBox boundary. | It is research and Figma metadata, not a shipped runtime screenshot or a permission to redesign the shell. |
| [`industrial-panel-design.md`](industrial-panel-design.md#L52-L106) | The product note records the `268/280` working widths, frequency-first tabs, one-scroll-owner rule, `Advanced` vocabulary, scale checks and protected Text/TextBox boundary. | It does not resolve the competing Figma `300/300` measurement without target-scale runtime evidence. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L616-L636) | The current grid uses `268` for the left panel and `280` for Properties, with 6 DIP splitter columns and existing shell accessibility regions. | Width converters and XAML alone do not prove usable layout at every display scale. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L694-L752) | Workspace has real `Layers` and `Data` tabs, with the current selection on `Data` and an intentional `ScrollViewer` per tab content. | It does not prove keyboard traversal, clipping or scroll behavior at the minimum target window. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1079-L1095) and [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1206-L1238) | Properties is a docked `Shell.Properties` panel with `Label`, `Layout` and `Advanced` tabs; the current accessible names are explicit. | It does not justify renaming the third tab to match an earlier visual variant. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1425-L1483) | The Properties content card already renders state-specific colors, icon, title, detail, action and trust text for the Excel verification state. | XAML triggers do not prove that the underlying verification is fresh or that every failure clears prior evidence. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L35-L42) | The state enum is exactly `NotLinked`, `Checking`, `Verified`, `Stale`, `Failed`. | The enum alone does not specify owner copy, visual treatment or runtime closure. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L622-L709) | Source/freshness text, state titles/details/actions/trust copy and stale transition are centralized in one view-model owner. | It does not replace a click-through or UI Automation run. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1572-L1647) | Verification enters `Checking`, refreshes a changed snapshot, tests workbook/sheet/header, then marks `Verified` or `Failed`; verified counts/time are recorded. | It does not prove display-scale layout, focus order or a printer result. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1670-L1765) and [`database-plan.md`](database-plan.md#L46-L69) | The file watcher debounces write changes and marks data stale without auto-reloading; existing data remains until an explicit update. | A watcher is a notice boundary, not a substitute for a user-confirmed refresh or a full data-source Manager workflow. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1952-L1979) | Explicit Unlink clears the data snapshot and returns verification to `NotLinked`; shared-source removal remains a separate Manager concern. | It does not authorize a new cross-surface mutation or a second unlink path. |
| [`AGENTS.md`](../AGENTS.md) | Text/TextBox behavior is a protected product contract and may not be changed incidentally. | It does not choose shell widths, labels or Excel card copy. |

## Shell ownership map

The map below keeps the visual region names, WPF automation regions and local action owner together. It is a handoff boundary, not a request to add new regions.

| Figma region | Node | WPF region / ID | Primary owner |
| --- | --- | --- | --- |
| R1 Quick Access | `2:3` | `Shell.QuickAccess` | `MainWindow` file/new/save/undo/redo/revision actions |
| R2 Ribbon | `2:23` | `Shell.Ribbon` | `MainWindow` import/update/print/workspace commands |
| R3 Toolbox | `2:80` | `Shell.Toolbox` | `MainWindow` object insertion and toolbox visibility |
| R4 Workspace | `2:109` | `Shell.Workspace` | `MainWindow` Layers/Data task switch and row/context display |
| R5 Design Surface | `2:123` | `Shell.Canvas` | `LabelDesignerCanvas` selection and design interaction |
| R6 Object Properties | `2:132` | `Shell.Properties` | `MainWindow` selected-object content, layout and advanced tabs |
| R7 Status Bar | `2:170` | `Shell.Status` | `MainWindow` status, printer and zoom feedback |

`Shell.LeftColumn` is a structural grouping for the toolbox/workspace pair; it is not a second action owner. The shell remains one WPF host even when a panel is hidden or collapsed.

## Width, density and label resolution

The competing dimensions are explainable but not interchangeable:

| Reference | Metadata / source | Safe interpretation |
| --- | --- | --- |
| MainWindow working baseline | left column `268 DIP`, splitter `6 DIP`, Properties splitter `6 DIP`, Properties column `280 DIP` | Current product baseline for implementation and acceptance until measured otherwise. |
| Full shell Figma `2:2` | `1440 x 900`; R3/R4 left body `268` wide; R6 outer region `292` wide with a `274` card | Visual shell composition; outer region includes margins and does not map one-to-one to the grid column. |
| Frequency-first panel `13:2` | `300 x 700`; inner cards `276` wide | Compact selected-object reference and TextBox contract language. |
| Properties tabs `18:69` | `300 x 700`; header `48`, tabs `38`, content `546` | Earlier tabbed visual variant; its third tab instance is named `More`. |

**Working resolution:** preserve `268/280` and `Advanced` in the current product notes and source. A future change must provide a target window, display scale, measured usable width, screenshot/UIA evidence, an owner decision and synchronized acceptance names. Metadata alone cannot trigger the change.

## Excel link verification contract

### State matrix

| State | Required visible evidence | Primary action | Boundary |
| --- | --- | --- | --- |
| `NotLinked` | No source claim; clear link/import affordance and copy that a workbook must be linked before row data is used. | `Link Excel...` (current handler opens the import flow). | No cached success, row count or verified timestamp is shown as current. |
| `Checking` | Explicit in-progress title and a wait-safe action; source context remains visible when available. | `Checking...` / wait for the current operation. | Do not certify the snapshot; current copy says printing remains blocked while the check runs. |
| `Verified` | Current workbook and sheet, verified column count, row count and check time. | `Recheck Excel link`. | Green/current trust is allowed only after workbook open plus selected sheet/header validation; the snapshot fingerprint must still be current. |
| `Stale` | The workbook changed after the last read; old rows are visibly not current. | `Update & verify`. | Do not silently reload; printing remains blocked until an explicit refresh/verification path completes. |
| `Failed` | Repairable failure detail, including file/sheet/header context when available. | `Recheck Excel link` or `Relink Excel...` when the path/sheet is broken. | Never retain a green verified claim or hide the failure behind old row data. |

`IsExcelLinkBroken` is an orthogonal broken-path detail used by the existing Relink action; it is not a sixth verification state. Explicit Unlink returns to `NotLinked`. A successful import/refresh publishes a new snapshot and records the verified counts/time; a changed write time moves the visible state to `Stale` without replacing rows underneath an active design/print session.

### Transition record

```text
NotLinked --Link/Import--> Checking --valid workbook/sheet/header--> Verified
                                      |                              |
                                      +--read failure--> Failed      +--file write change--> Stale
                                                                         |
                                      Relink / Update & verify ----------+
                                                      -> Checking -> Verified or Failed
Verified / Stale / Failed --explicit Unlink--> NotLinked
```

The transition diagram describes the existing owner boundary and the evidence to test; it does not authorize a new asynchronous workflow or automatic reload.

## Read-only Figma metadata boundary

Metadata was checked read-only on 2026-08-13. The following entries are visual evidence only:

| File | Node | Measured metadata | Safe reuse | Missing / unresolved state |
| --- | --- | --- | --- | --- |
| NiceLabel shell recreation (`zdN71qfzrYV6pPt1b2FRRc`) | `2:2` | `ANLAbel - Full Shell v1`, `1440 x 900` | Region map, chrome hierarchy and role vocabulary | No runtime scale, WPF theme or Excel verification behavior |
| Panels exploration (`kqyNBI0DgRHnPzJTDBIui5`) | `13:2` | Selected Properties `300 x 700`; compact cards and explicit TextBox wrap/clip language | Density, selected-object summary and compact card order | No WPF automation measurement; TextBox copy remains protected |
| Panels exploration (`kqyNBI0DgRHnPzJTDBIui5`) | `18:69` | Properties tabs `300 x 700`; Label/Layout/More visual variant | Tab spacing and content hierarchy | `More` versus product `Advanced` remains an owner choice, with `Advanced` as current baseline |
| Panels exploration (`kqyNBI0DgRHnPzJTDBIui5`) | `22:82` | `Excel Link Verification` component `620 x 455`; symbols `State=Not linked`, `Checking`, `Verified`, `Stale`, `Failed` | Five-state visual vocabulary | No WPF runtime proof, no Database Manager frame and no license/server claim |

**Figma rule:** do not call a design context, write to Figma or create a duplicate frame for this docs-only decision. If the owner requires a visual change, first name the missing state, then record the smallest reference and map it to the existing WPF action owner.

## Accessibility, keyboard and scroll gate

Existing IDs remain the stable shell anchors:

| Surface | Current ID/name | Proposed child evidence (not implemented by this packet) |
| --- | --- | --- |
| Shell | `Shell.QuickAccess`, `Shell.Ribbon`, `Shell.LeftColumn`, `Shell.Toolbox`, `Shell.Workspace`, `Shell.Canvas`, `Shell.Properties`, `Shell.Status` | None unless a new child action has no existing accessible name. |
| Workspace tabs | `Shell.Workspace` / tab headers `Layers`, `Data` | `Shell.Workspace.LayersTab`, `Shell.Workspace.DataTab` if UIA cannot identify the existing headers reliably. |
| Properties tabs | `PropertiesModeTabs`, `Properties sections`; `Advanced object properties` | Keep the `Advanced` label and name together; do not introduce a `More` alias in acceptance. |
| Excel card | Existing bindings `ExcelLinkVerificationState`, title/detail/action/trust | `Shell.Properties.ExcelLinkVerification`, `.Action`, `.Trust` only if a runtime UIA run demonstrates the need. |

Closure evidence must demonstrate:

1. keyboard order from shell chrome to Workspace/Properties and back without a focus trap;
2. visible focus on tabs, action buttons and the verification card at every supported scale;
3. one intentional vertical scroll owner per panel and no default horizontal scroll at `1024 x 600`;
4. state-specific accessible names that do not rely on color alone; and
5. unchanged Text/TextBox behavior when the selected object is `Text` or `TextBox`.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `MainShell_R1_R7_automation_regions` | All existing shell IDs map to the documented Figma regions and remain discoverable. | UIA tree plus the existing shell regression. |
| `MainShell_268_280_baseline_at_target_scales` | Working widths remain usable at `1024 x 600`, `100%`, `125%` and `150%` with no default horizontal scroll. | Runtime screenshots and measured bounds; owner exception if an environment cannot run a scale. |
| `PropertiesTabs_keep_Advanced_label` | The visible third tab and accessible name remain `Advanced`; no `More` alias leaks into acceptance. | UIA names, keyboard traversal and screenshot. |
| `ExcelVerification_NotLinked_action` | A new/unlinked template shows link/import affordance and no current success evidence. | Properties screenshot/UIA and view-model assertion. |
| `ExcelVerification_Checking_is_not_green` | Verification shows in-progress copy and does not certify data while the operation is running. | Controlled async fixture and UIA state capture. |
| `ExcelVerification_Verified_has_current_evidence` | Workbook/sheet, row/column counts and check time match the current successful snapshot. | Temporary workbook, deterministic service result and screenshot. |
| `ExcelVerification_FileChange_marks_Stale_without_reload` | A write-time change marks stale, keeps rows unchanged until explicit update and blocks stale use. | File watcher fixture, row identity assertion and runtime copy. |
| `ExcelVerification_Failed_clears_success_claim` | Missing file/sheet/header or invalid workbook shows repair detail and no old green evidence. | Failure matrix with UIA and view-model assertions. |
| `ExcelVerification_Unlink_resets_NotLinked` | Explicit Unlink clears the snapshot and returns to `NotLinked`; shared-source Manager state is untouched. | Before/after template and registry assertions. |
| `Protected_TextTextBox_contract_unchanged` | The named Text/TextBox industrial gates remain green; shell/panel work does not change sizing, wrap, clip, padding, resize or print parity. | Required repository regression suite if code changes are later approved. |

## No-go list

- Do not change `268/280` because a Figma panel reports `300/300`; first obtain target-scale runtime measurements and owner sign-off.
- Do not rename `Advanced` to `More` (or expose both labels) without synchronizing visible copy, accessibility names, keyboard acceptance and regression identifiers.
- Do not add a second shell host, duplicate an Excel verification card in Database Manager, or move shared-source mutation into the Properties card.
- Do not mark stale, failed or checking data as verified from cached rows; do not silently reload a workbook after a watcher event.
- Do not infer a Database Manager design from component `22:82`; the current Manager remains a separate WPF workflow with its own handoff/spec and action owners.
- Do not use this shell/panel work to alter Text/TextBox ownership, sizing, wrapping, clipping, padding defaults, resize lifecycle, selection handles, overflow or print parity.
- Do not write to or create a Figma file merely to fill an unresolved state, and do not claim runtime closure from metadata alone.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the decision open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Shell host and region ownership | `TBD` | `TBD` | `TBD` |  |
| D2. Panel width baseline and scale exception | `TBD` | `TBD` | `TBD` |  |
| D3. Frequency-first tabs/disclosure/scroll | `TBD` | `TBD` | `TBD` |  |
| D4. Properties third label | `TBD` | `TBD` | `TBD` |  |
| D5. Excel five-state semantics | `TBD` | `TBD` | `TBD` |  |
| D6. Freshness and action ownership | `TBD` | `TBD` | `TBD` |  |
| D7. Verification evidence/redaction | `TBD` | `TBD` | `TBD` |  |
| D8. Figma route/missing-state policy | `TBD` | `TBD` | `TBD` |  |
| D9. Runtime/UIA/keyboard evidence | `TBD` | `TBD` | `TBD` |  |
| D10. Closure/regression ownership | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the shell/panel/Excel slice may move from documentation review to implementation or runtime closure only after the applicable D1-D10 rows are filled, the `268/280` versus `300/300` question is resolved with target-scale evidence, the final tab label is synchronized across UIA and acceptance, each Excel state has a deterministic fixture and screenshot, and a clean implementation checkpoint links the results. Until then this is an open owner contract; it makes no release, Figma-edit, print-certification or physical-output claim.
