# ANLAbel designer header uniqueness — Figma to WPF handoff

Filled instance of [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md).

## 1. Slice identity

| Field | Required value |
| --- | --- |
| Local label outcome | Designer header shows each command once, with a unique glyph |
| In scope | `Shell.QuickAccess` and `Shell.Ribbon` chrome |
| Out of scope | Toolbox, Properties, canvas Text/TextBox, print pipeline, status-bar zoom (kept as the only zoom placement) |
| Related active plan | `docs/reinvention/07-execution-plan.md` §7.1 item 1 |
| Implementation date | `2026-08-14` |

## 2. Exact Figma authority

| Field | Required value |
| --- | --- |
| Figma file URL | https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc |
| Node ID | `5:2` |
| Node name | `ANLAbel — Header uniqueness v0.229` |
| Created or updated with `@figma` | `Yes` |
| Existing component/tokens reused | Existing shell file `zdN71qfzrYV6pPt1b2FRRc`; Full Shell `2:2` Help group updated to drop ribbon zoom (`2:69`) |

## 3. Required states

| State | Figma node/variant | Runtime source | Primary action |
| --- | --- | --- | --- |
| Empty | N/A — header chrome is always present | `MainWindow` loaded | None |
| Loading/busy | N/A — header commands stay enabled independently of data load | Existing command `CanExecute` | None |
| Ready/success | `5:2` default | Bound commands + click handlers | File / data / print / snap / help |
| Stale | N/A | N/A | N/A |
| Blocked/error | N/A for chrome | Queue warning remains on status bar | Open Printer Setup from status |
| Disabled/read-only | Undo/Redo/print commands disabled by ViewModel | `ICommand.CanExecute` | None |
| Large/long data | Ribbon `Viewbox StretchDirection=DownOnly` | Existing ribbon Viewbox | Horizontal squeeze, no second icon row |

## 4. Layout contract

| Requirement | Decision/evidence |
| --- | --- |
| 1024 x 600 effective fit | Window `MinWidth=1024` `MinHeight=600`; header is two fixed bands (52 + ribbon Viewbox) |
| 1280 x 720 and 1920 x 1080 behavior | Ribbon Viewbox grows only down; QAT stays 52 DIP |
| 100/125/150/200% scale intent | Same bands; STA scale fixtures measure 1024×600, 819×480 and 683×400 logical |
| One explicit scroll owner | Header does not scroll; canvas/workspace remain the content scroll owners |
| Long text (+40%) wrapping/truncation | Ribbon captions stay two short lines; printer chip uses existing binding |
| Empty and maximum-data density | Header independent of row count |
| Keyboard focus order | QAT New→Open→Save→Undo→Redo→Revisions then ribbon left-to-right |
| Visible focus and high contrast | Existing WPF button focus; navy QAT / light ribbon contrast unchanged |

## 5. Figma to WPF mapping

| Figma node/component | WPF control/resource | AutomationId | Accessible name | Data/state source |
| --- | --- | --- | --- | --- |
| R1 Quick Access | `Border` | `Shell.QuickAccess` | Quick access | Chrome |
| New | QAT `Button` | `Shell.QuickAccess.New` | New label | `New_Click` |
| Open | QAT `Button` | `Shell.QuickAccess.Open` | Open label | `Open_Click` |
| Save | QAT `Button` | `Shell.QuickAccess.Save` | Save label | `Save_Click` |
| Undo | QAT `Button` | `Shell.QuickAccess.Undo` | Undo | `UndoCommand` |
| Redo | QAT `Button` | `Shell.QuickAccess.Redo` | Redo | `RedoCommand` |
| Revisions | QAT `Button` | `Shell.QuickAccess.Revisions` | Template revisions | `RevisionHistory_Click` |
| Printer chip | QAT `Border` | `Shell.QuickAccess.PrinterStatus` | (status) | `Template.PrinterProfile.PrinterName` |
| R2 Ribbon | `Border` | `Shell.Ribbon` | Ribbon | Chrome |
| Templates | Ribbon `Button` | `Shell.Ribbon.Templates` | Templates | `TemplateLibrary_Click` / `folder.png` |
| Import Excel | Ribbon `Button` | `Shell.Ribbon.ImportExcel` | Import Excel | `ImportExcel_Click` / `import_excel.png` |
| Update Excel | Ribbon `Button` | `Shell.Ribbon.UpdateExcel` | Update Excel | `RefreshExcelDataCommand` / `update_excel.png` |
| Printer Setup | Ribbon `Button` | `Shell.Ribbon.PrinterSetup` | Printer Setup | `PrinterSetup_Click` / `printer_setup.png` |
| Preview | Ribbon `Button` | `Shell.Ribbon.Preview` | Preview | `PrintPreview_Click` / `preview.png` |
| Print Current | Ribbon `Button` | `Shell.Ribbon.PrintCurrent` | Print Current | `PrintCurrentRowCommand` / `print_current.png` |
| Print all rows | Ribbon `Button` | `Shell.Ribbon.PrintAllRows` | Print all rows | `PrintAllRowsCommand` / `print_all_rows.png` |
| Print history | Ribbon `Button` | `Shell.Ribbon.PrintHistory` | Print history | `PrintHistory_Click` / `print_history.png` |
| Export Excel | Ribbon `Button` | `Shell.Ribbon.ExportExcel` | Export Excel | `ExportPrintHistory_Click` / `export_excel.png` |
| Test print | Ribbon `Button` | `Shell.Ribbon.TestPrint` | Test print | `PrintCalibrationCommand` / `test_print.png` |
| Panels restore | Ribbon `Button` | `Shell.Ribbon.Panels` | Panels restore | `ShowAllPanelsCommand` / `panels.png` |
| Snap to objects | Ribbon `ToggleButton` | `Shell.Ribbon.SnapObjects` | Snap to objects | canvas `IsSnapToObjectsEnabled` / `snap_objects.png` |
| Snap to grid | Ribbon `ToggleButton` | `Shell.Ribbon.SnapGrid` | Snap to grid | canvas `IsSnapToGridEnabled` / `snap_grid.png` |
| Delete Selection | Ribbon `Button` | `Shell.Ribbon.DeleteSelection` | Delete Selection | `DeleteSelectionCommand` / `delete_selection.png` |
| Help | Ribbon `Button` | `Shell.Ribbon.Help` | Help | `Help_Click` / `help.png` |
| Status zoom (not header) | Status `StackPanel` | `Shell.Status.Zoom` | Zoom | `Zoom` / `zoom_minus.png` + slider + `zoom_plus.png` |

- Use existing WPF theme resources before hardcoded colors.
- Use the repository icon system; do not use emoji or Unicode glyphs as icons.
- Keep one action owner; zoom is status-only.
- UI cannot infer success, data, queue state or completion without source evidence.

## 6. Runtime evidence

| Automated gate | Result/artifact |
| --- | --- |
| XAML compiles with zero warnings | Fast loop |
| Stable AutomationIds present | `designer header commands are unique` + `designer shell layout at target scales` |
| State transitions covered | Command CanExecute unchanged |
| 1024 x 600 layout contract covered | STA measure/arrange + optional offscreen raster |
| Keyboard/focus contract covered | Tab order left-to-right in shipped XAML |
| Long/empty/error data covered | Header independent of data |
| Relevant domain regressions pass | Protected Text/TextBox gates unchanged |
| Fast quality loop passes | `scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast` |

## 7. Completion rule

Exact Figma node `5:2`, WPF `MainWindow.xaml` header, AutomationId map, uniqueness tests and public version `0.229` agree. Text/TextBox ownership, wrap/clip and resize lifecycle were not modified.
