# Database Manager UI/UX handoff

**Status:** implementation exists; runtime click-through and design ownership are still open
**Parent plan:** [`database-manager-module-plan.md`](database-manager-module-plan.md) (M1-M3 history)
**Cross-surface handoff:** [`10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**UI/UX specification:** [`DATABASE_MANAGER_UI_SPEC.md`](DATABASE_MANAGER_UI_SPEC.md)
**Owner decision packet:** [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md)
**Figma reference:** panels file [`ANLAbel UI exploration`](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), Page `0:1`

## 1. Operator task

An operator must be able to manage a shared Excel source from one owner window without hunting across the designer:

1. open **Manage Data Sources...** from the Data panel;
2. select or save a shared source;
3. edit the display name, sheet and header row, then test the connection;
4. preview rows before using the source for the current template;
5. relink a missing file, remove a source with an explicit usage warning, or clean up orphaned sources in a separate guarded flow.

The per-template **Unlink Excel** action remains a separate escape hatch. It must not silently mutate an object's binding expression or label geometry.

## 2. Current implementation evidence

| Surface | Evidence in the current checkout | Acceptance boundary |
| --- | --- | --- |
| Entry point | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L934) exposes `Manage Data Sources...`; [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L566) opens `DatabaseManagerWindow` as an owned dialog. | Runtime click-through must prove the entry is reachable from the Data panel at the target window sizes. |
| Manager shell | [`DatabaseManagerWindow.xaml`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L1) defines a `900 x 620` window (`760 x 480` minimum), a `260 DIP` source list and a scrollable detail column. | Screenshot/automation must prove no clipping or hidden primary action at `1024 x 600`, 100%, 125% and 150%. |
| Source actions | The detail surface includes name, read-only file path, Relink, editable Sheet, Load sheets, Header row, Test Connection, Preview data, Use for current template and Remove at [`DatabaseManagerWindow.xaml#L62`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L62). | Each action needs a success, failure or guarded-confirmation state; no action may report success from stale cached data. |
| Async I/O | [`DatabaseManagerWindow.xaml.cs`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L129) loads sheets, `#L157` tests the connection and `#L178` loads the preview through `ExcelDataService`. | Checking state, cancellation/timeout behavior and a visible failure repair path must be evidenced. |
| Usage/removal safety | Current-template usage is shown at [`DatabaseManagerWindow.xaml#L99`](../src/ANLAbel.App/DatabaseManagerWindow.xaml#L99); Remove confirmation is implemented at [`DatabaseManagerWindow.xaml.cs#L232`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L232). | The warning must distinguish the open template from other references and preserve the documented fallback behavior. |
| Cleanup | `Clean up...` filters missing files unused for 30+ days at [`DatabaseManagerWindow.xaml.cs#L264`](../src/ANLAbel.App/DatabaseManagerWindow.xaml.cs#L264); the guarded bulk-removal window is [`DataSourceCleanupWindow.xaml`](../src/ANLAbel.App/DataSourceCleanupWindow.xaml#L1). | Empty candidate, unchecked selection, confirmation and successful removal states need runtime evidence. |
| Existing contract tests | The plan records M1-M3 software tests and build/smoke evidence, but also explicitly says the Manager click-through has not been manually verified. | Do not promote “M2/M3 done” to a product UX or release claim until the runtime matrix below is attached. |

## 3. UI state matrix

| State | Required visible content | Safe action |
| --- | --- | --- |
| Manager, no source selected | Source list, Save current link, Clean up; detail pane explains selection is required | Select a source or close; do not show stale detail values. |
| Source selected, not used by current template | Name/path/sheet/header; `Used by ...: No`; actions available | Test, preview, relink, use or remove. |
| Source selected and used | Same detail plus explicit `Yes` usage evidence | Remove requires a warning that the current template falls back to its own path. |
| Checking sheets/connection/preview | Busy indication and disabled duplicate action | Wait or cancel; do not allow conflicting writes. |
| Connection verified | Green/neutral result with file, sheet, header and row/column evidence | Preview or Use; result must correspond to the current fields. |
| Connection failed or stale file | Error severity, reason and repair action (`Relink`, edit sheet/header, retry) | Keep the source unverified; never turn failure into a green state. |
| Preview loaded | Read-only rows/columns, count summary and source context | Inspect, then Use or close; preview must not silently bind the template. |
| Remove confirmation | Source name, current-template usage note and fallback consequence | Cancel leaves registry unchanged; confirm removes only the chosen source. |
| Cleanup, no candidates | Informational message and no empty destructive dialog | Close. |
| Cleanup candidates | Checkbox list with missing path and last-used/never-used evidence | Select, confirm once, remove only checked candidates. |

Keep the primary order: source list/context -> connection details -> Test/Preview evidence -> Use/Remove. `Clean up...` is maintenance, not a primary path for normal linking.

## 4. Figma evidence and routing

A fresh read-only metadata scan of panels Page `0:1` on 2026-08-13 found exactly these top-level frames: `1:2`, `4:2`, `8:2`, `13:2`, `18:69` and `22:82`. There is no Database Manager frame or state-specific source-management component.

The existing `8:2` panel reference supplies reusable language for a `300 DIP` Workspace/Data shell, but it does not answer the Manager's list/detail, test, preview, remove or cleanup states. The current WPF Manager is a separate `900 x 620` dialog and should remain the working product baseline until an owner chooses a visual redesign.

**Interim routing decision:** reuse the existing shell/card vocabulary from Figma `8:2` and the current WPF Manager information architecture; do not create or edit Figma merely to make the inventory complete. If the owner selects a visual redesign, create/locate one state-specific reference covering the matrix above before changing the WPF surface. Record that decision in [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md).

The acceptance artifact is a runtime screenshot or UI Automation measurement, not a Figma frame. Figma metadata alone does not prove dialog reachability, async behavior, file-lock handling or safe removal.

## 5. Regression and runtime gates

Required before calling the Manager UX slice closed:

- click-through from the main Data panel into the owned Manager dialog;
- source selection and empty-selection state;
- edit name/sheet/header and persistence after reload;
- Test Connection: valid file/sheet/header, missing file, missing sheet and invalid header;
- Preview data: populated rows, empty sheet and read/lock failure;
- Relink success/failure and `Used by current template` refresh;
- Use for current template preserves the source identity and reloads the expected row/schema;
- Remove cancel/confirm, current-template warning and registry persistence;
- Cleanup no-candidate, unchecked, cancel and bulk-confirm paths;
- runtime screenshot/UI Automation at `1024x600`, `100%`, `125%` and `150%` (or a recorded environment exception);
- existing M1-M3 application/unit tests, Excel verification states, Data Workspace transform gates and protected Text/TextBox gates remain green.

Suggested commands remain:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 6. Owner decisions before a UI change

The shared source/read-model, Manager-versus-Workspace action boundary, async request identity, Figma reuse rule and target-scale evidence gate are consolidated in [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md). It keeps `DatabaseManagerWindow` as the shared-source owner and does not authorize a new Manager frame or a transform editor in this dialog.

1. Confirm whether the current WPF information architecture is accepted for the first smoke-test pass or whether a new Figma Manager frame is required.
2. Confirm operator copy for `Test Connection`, `Preview data...`, `Use for current template`, `Remove...` and `Clean up...`.
3. Assign the runtime screenshot/UI Automation owner and stable AutomationIds for the dialog and state matrix.
4. Keep shared-source removal semantics and the per-template Unlink contract unchanged unless explicitly reopened with matching docs and tests.

Until these decisions and runtime evidence exist, this document is a handoff—not a claim that Database Manager UX is verified or release-ready.
