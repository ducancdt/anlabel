# CC-P2 Print Queue Console UI/UX handoff

**Status:** roadmap/pre-implementation; local printer discovery and single-queue evidence exist, but the fleet/queue console is not implemented or runtime-verified
**Parent plan:** [`MASTER_PLAN.md`](../MASTER_PLAN.md#control-center--lms-operations--large-improvement-plans-2026-08-12), section 4, CC-P2
**Related CC-P1 handoff:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md)
**Cross-surface handoff:** [`10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**Current verification boundary:** [`11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md)
**Figma reference:** [NiceLabel Control Center research shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, `CC / Printers — Print Management` node `2:37`

This is a documentation handoff, not an authorization to modify the dirty implementation wave. The Figma shell is a visual and information-architecture reference only. It does not prove that ANLAbel has a multi-workstation service, licensed-printer seat accounting, printer-native controls, or physical-label verification.

## 1. Operator task

An operator needs one local surface to answer which named queues are visible and what evidence is safe to act on:

1. discover the installed local/connection queues without silently selecting the Windows default;
2. filter the list by a defined status taxonomy and search by printer, port or workstation when that evidence exists;
3. inspect the selected queue/job observation and distinguish a live spooler observation from a durable print/recovery event;
4. use an explicit, capability-checked command only when the Windows spooler and the product contract support it.

The first vertical slice should be **read-only queue visibility**. Pause, resume, delete, reserve and unreserve are later command work, not implied by a Figma toolbar. No queue command may become an automatic retry, an implicit dispatch, or a physical-output claim.

## 2. Current implementation evidence

| Surface | Evidence in the current checkout | Acceptance boundary |
| --- | --- | --- |
| Installed-printer discovery | [`PrinterDiscoveryService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrinterDiscoveryService.cs#L11) reads Windows `Local` and `Connections` queues, exposes name/driver/default and uses the local stock-size catalog. | Discovery must remain explicit and deterministic. A failed enumeration is an empty/error state, not permission to use an unrelated queue. |
| Current setup dialog | [`PrinterSetupWindow.xaml`](../src/ANLAbel.App/PrinterSetupWindow.xaml#L1) is a fixed `760 x 480` dialog with stock size, DPI and printer selection; [`PrinterSetupWindow.xaml.cs`](../src/ANLAbel.App/PrinterSetupWindow.xaml.cs#L19) persists the selected printer/paper/DPI/orientation. | Keep setup as the label/profile editor. Do not turn it into a hidden fleet-management authority while introducing the queue console. |
| Saved-queue lookup | [`PrinterQueueLookup.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrinterQueueLookup.cs#L35) resolves the saved name and rejects a missing or canonical-name mismatch. | Missing, renamed or inaccessible queues stay warnings. The Windows default queue must never be substituted silently. |
| Main-shell warning | [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L907) exposes `PrinterQueueStatus`, and `#L944` refreshes the saved queue off the WPF dispatcher. The warning routes to Printer Setup through [`MainWindow.xaml.cs#L866`](../src/ANLAbel.App/MainWindow.xaml.cs#L866). | Preserve the existing repair path and stale-result guard when the console adds a broader list. |
| Spool observation | [`WindowsSpoolJobStatusReader.cs`](../src/ANLAbel.Printing/PrinterProfiles/WindowsSpoolJobStatusReader.cs#L1) reads one known spool identifier and maps error, paper-out, offline, intervention, blocked, paused, deleted, retained, printing, spooling and pending states. | This adapter is read-only and job-scoped. It cannot prove the printed mark, media result or physical completion; an unavailable spooler is terminal uncertainty for that observation. |
| Recovery surface | [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L1) already supports durable recovery/reconcile/acknowledge/void/linked-reprint actions and redacted support export. | Queue-console selection may deep-link to Print Center, but must not create a second recovery or reprint stack. |
| Print dispatch safety | [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs#L747) verifies the explicit queue and captures before/after spool identity; no fleet command path was found in the current WPF/source scan. | A future command strip needs a separate command contract, capability result, confirmation and durable outcome before any Pause/Resume/Delete control is enabled. |
| Existing test evidence | [`SpoolJobMonitoringTests.cs`](../src/ANLAbel.UnitTests/SpoolJobMonitoringTests.cs#L1) and [`PrintJobRecoveryServiceTests.cs`](../src/ANLAbel.UnitTests/PrintJobRecoveryServiceTests.cs#L1) cover modeled spool/recovery behavior; they do not constitute a multi-queue UI click-through. | Add console-specific fixtures and UI Automation only when the surface is implemented. Keep real-spooler/device evidence separate from software tests. |

## 3. Figma evidence and routing

Read-only Figma metadata was checked on 2026-08-13 for node `2:37`, `1280 x 800`. The structure is:

| Node | Name | Position/size | ANLAbel routing |
| --- | --- | --- | --- |
| `2:38` | TopBar | `(0, 0)`, `1280 x 48` | Reusable chrome reference only; no WPF size mandate. |
| `2:41` | Primary navigation | `(0, 48)`, `1280 x 40` | Keep only local modules that exist; do not imply web Control Center navigation. |
| `2:51` | Filter rail | `(16, 104)`, `220 x 680` | Maps to future status/group filters. The Figma labels `Licensed Printers`, `All Printers`, `Printers with Errors`, `Printing now`, `Ready to Print`, `Paused Printers`, `Facility A`, `Location 1` and `+ Add printer group...` are research vocabulary, not current product data. |
| `2:72` | Main print-management pane | `(252, 104)`, `1000 x 680` | Candidate table/command region for a future WPF sibling or Print Center extension. |
| `2:73` | Command strip sample | `(264, 116)` text row | Pause/Resume/Delete/Reserve/Unreserve/Settings must remain deferred until command semantics and Windows capability checks are approved. |
| `2:74` | Search sample | `(932, 116)` text row | Search by name/port/workstation only where those fields are actually observed. |
| `2:75` / `2:76` | Table header | `(264, 152)`, `976 x 28` | Useful column language: printer name, documents in queue, status, workstation, port. Define local null/unknown values before implementing. |
| `2:77`–`2:79` | Example rows | `ZEBRA GX430t`, `Paxar 676` samples | Research samples only; never copy printer names, IPs, queue counts or error text into product fixtures as live evidence. |
| `2:80` | Footer controls | `(272, 744)` text row | Printer count and a local filter can be useful; `Show unlicensed printers` and `View by workstation` require an explicit local data/entitlement decision. |

The research source describes a multi-workstation Control Center domain and licensed-printer operations in [`NICELABEL_CONTROL_CENTER_USER_GUIDE.md`](NICELABEL_CONTROL_CENTER_USER_GUIDE.md) and the extracted Print Management section in [`_raw_extract.md#L3315`](assets/nicelabel-control-center/_raw_extract.md#L3315). Those passages explain the benchmark, not ANLAbel product capabilities. No Figma edit or new file is needed for this handoff: `2:37` already answers the shell question. A Figma frame remains design input, not runtime acceptance.

## 4. Proposed implementation sequence

Keep CC-P2 vertical and local:

1. **M1 — Read-only multi-queue table:** discover `Local`/`Connections`, show canonical name, driver, default flag, current saved-template relationship and a timestamped lookup result. Keep unknown/error rows visible rather than dropping them.
2. **M1 — Status filters/search:** define a small local taxonomy (`Available`, `Printing`, `Paused`, `Offline`, `PaperOut`, `UserIntervention`, `Blocked`, `Retained`, `Error`, `Unknown`) and map each row to a safe next action. Do not claim a printer-level state when only a job-level observation exists.
3. **M2 — Selection and deep-links:** route selected recovery evidence to Print Center, profile repair to Printer Setup and durable history to Print History. Keep one owner for each action.
4. **M3 — Explicit command strip:** only after a command contract exists for Pause/Resume/Delete. Every command needs capability detection, confirmation where destructive, busy/timeout/error state, durable outcome and a no-physical-output disclaimer.
5. **Later — Local groups/seat display:** groups may be local preference filters. Licensed-printer or seat linkage stays deferred until a real entitlement source exists; it must not be inferred from Figma.

The first host decision is open: extend `PrintCenterWindow` with a read-only queue tab, add a sibling `PrintManagementWindow`, or expose a queue console from the CC-P1 overview. Do not create parallel authorities for queue identity or reprint lineage.

## 5. User-visible state matrix

| State | Visible evidence | Safe next action | Explicit non-claim |
| --- | --- | --- | --- |
| No queues discovered | Empty result with discovery timestamp and actionable diagnostic | Retry discovery or open Printer Setup | No queue found is not permission to use the Windows default. |
| No saved queue | Clear `Not selected` state and link to Printer Setup | Select and save a verified industrial queue | No print is dispatchable from this state. |
| One or more queues discovered | Rows with canonical name, driver/default marker and observation age | Search/filter/select a row | Discovery does not prove the queue is online or licensed. |
| Selected queue available | Verified lookup result, canonical name and last refresh | Inspect jobs or open setup/history | Queue availability is not physical label verification. |
| Queue missing/renamed/inaccessible | Error reason from lookup, last successful observation if any | Repair/reselect in Printer Setup | Never silently substitute another queue. |
| Job printing/spooling/pending | Job-scoped status, spool ID and observed-at timestamp | Refresh or open Print Center | A queue observation is not proof of printed content. |
| Job paused/blocked/offline/paper-out/intervention/error | Severity, exact mapped state and reason | Resolve the stated condition or review recovery | Do not auto-retry, auto-resume or mark success. |
| Job retained/deleted/not found | Terminal queue evidence and ambiguity wording | Inspect durable manifest/history or acknowledge/void in Print Center | Terminal queue state does not prove physical output. |
| Multiple selection with no approved command | Selection summary and disabled command strip | Review rows or clear selection | Selection alone must not dispatch or mutate a queue. |
| Command checking/busy | Target queue/job, capability result and disabled duplicate controls | Wait, cancel if contract allows, or show timeout | Never report success before the spooler response and durable outcome. |
| Command rejected/failed | Driver/spooler error, target identity and repair path | Retry only after an explicit new operator action and fresh evidence | A rejected command is not a print failure/success rewrite. |
| Search/filter no match | Query and `0 results` with clear/reset action | Clear filter or adjust the query | Do not imply that hidden/unlisted queues are absent. |

## 6. WPF mapping and acceptance contract

| Console region | Existing/proposed WPF owner | Stable mapping requirement |
| --- | --- | --- |
| Discovery/refresh | `PrinterDiscoveryService.GetInstalledPrinters()` plus a new explicit console refresh seam | Stable `PrintQueueConsole.Refresh` action; retain discovery timestamp and error. |
| Filter rail | New local view model over queue rows | Stable filter names; groups are preferences, not entitlement rules. |
| Queue table | New read-only `DataGrid` or a deliberate Print Center extension | Stable row identity from canonical queue name; never use display text as a spool/job ID. |
| Job evidence/detail | `WindowsSpoolJobStatusReader`, `PrintRecoveryReport`, `PrintCenterWindow` | Distinguish job-scoped spool observations from durable manifest/recovery evidence. |
| Setup deep-link | Existing `PrinterSetup_Click` / `PrinterSetupWindow` | Preserve the one explicit queue-selection path and current template update semantics. |
| History deep-link | Existing `PrintHistory_Click` and `OpenPrintHistoryFile()` | Do not create a second append-only history store. |
| Future command strip | New command service/contract, not current `PrinterDiscoveryService` | Require capability, confirmation, timeout, durable outcome and automation name before enabling. |

Protected behavior check:

- [ ] No Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or designer/print parity is changed.
- [ ] Queue-console filters and cards never mutate label geometry, data bindings, printer profile or print manifests.
- [ ] The existing explicit-queue/no-default-fallback and no-auto-retry contracts remain intact.
- [ ] Any future command contract is documented with regression coverage before a UI button is enabled.

## 7. Runtime and regression gates

Before calling CC-P2 implemented or verified, attach:

- runtime screenshots and/or UI Automation at `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception);
- discovery fixtures for no queues, one queue, multiple queues, default marker, duplicate/canonical mismatch and enumeration failure;
- queue/job fixtures for every mapped state, including unknown, not found and spooler restart/permission failure;
- keyboard/focus path for refresh, search, filter reset, row selection and deep-links;
- proof that a slow refresh cannot overwrite a newer queue selection and that stale timestamps are visible;
- if commands are implemented: capability/rejection/timeout/confirmation tests and durable evidence for every target queue/job; no auto-retry path;
- existing named spool/recovery tests plus build, unit-test and application-runner output copied into the owning clean checkpoint;
- explicit non-claims for Control Center multi-user service, licensed-seat enforcement, printer-native completion, physical verifier/grade and physical-label output.

Suggested commands:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 8. Owner decisions before UI implementation

1. Choose the host: extend `PrintCenterWindow`, add `PrintManagementWindow`, or route from the CC-P1 overview.
2. Approve the M1 read-only scope before designing Pause/Resume/Delete controls.
3. Define the authoritative queue-status source and which fields are queue-level versus job-level.
4. Decide whether local printer groups are needed in the first console and how they persist; keep them separate from licensing.
5. Assign stable AutomationIds, runtime screenshot/UI Automation ownership and the first clean implementation checkpoint.
6. Keep explicit queue identity, no-default-fallback, no-auto-retry, guarded reprint and protected Text/TextBox contracts unchanged.

Until these decisions and runtime evidence exist, this document is a handoff—not a claim that the Print Queue Console is shipped or design-verified.
