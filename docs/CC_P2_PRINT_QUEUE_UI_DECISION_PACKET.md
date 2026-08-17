# CC-P2 Print Queue Console owner decision packet

**Status:** M1 read-only queue-visibility slice implemented; no queue command, Figma edit or Text/TextBox change is authorized (2026-08-13)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Upstream host/read-model gate:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Host decision packet:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Read-model contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**UI/UX handoff:** [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md)
**UI/UX specification:** [`CC_P2_PRINT_QUEUE_UI_SPEC.md`](CC_P2_PRINT_QUEUE_UI_SPEC.md)
**P5 recovery/action owner:** [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md)
**Figma routing:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and boundary

The current checkout has source-backed local printer discovery, explicit saved-queue lookup and one-job spool observation. M1 now adds a local read-only table/filter model in `PrintQueueConsoleWindow`; it still has no queue-command service. The upstream P1/P2/P5 gate owns the cross-surface read model and this packet records the concrete P2 boundary without creating a second queue/recovery authority.

This packet covers:

- deterministic discovery of Windows `Local` and `Connections` queues;
- explicit saved-queue identity and canonical-name mismatch handling;
- a read-only queue table/detail projection with source scope, age and unknown/error states;
- filters/search limited to fields the local source actually supplies;
- job-scoped spool observations and explicit deep-links to Printer Setup, Print Center and History;
- refresh/request identity, empty-versus-enumeration-failure semantics, UIA proposals and target-scale fixtures;
- read-only Figma reuse of `2:37` without copying research samples into product data.

It does not choose the upstream host, add Pause/Resume/Delete/Reserve/Unreserve, infer licensing or workstations, add a printer-group entitlement model, replace `PrinterSetupWindow`, duplicate Print Center recovery/reprint, claim physical output, edit Figma, or alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize, overflow or print parity.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Host and action boundary | Use the host selected by the upstream P1/P2/P5 gate; keep this slice as a read-only projection and keep Printer Setup, Print Center and History as explicit existing action owners. | Select the host/deep-link owner in the upstream packet before any WPF navigation change. |
| D2. Discovery authority | `PrinterDiscoveryService` is the M1 discovery authority and enumerates only Windows `Local` and `Connections` queues. It returns `PrinterInfo` rows with name, driver, default marker and catalog paper sizes. | Approve whether discovery failures become a typed error envelope (recommended) instead of the current empty-list fallback. |
| D3. Queue identity and fallback | `PrinterQueueLookupResult` is the saved-queue evidence boundary; requested and canonical names must remain visible, and a missing/mismatched queue never falls back to the Windows default. | Approve display copy, stale threshold and the repair route to Printer Setup. |
| D4. Refresh/request safety | Capture discovery/selection identity, timestamp and cancellation token per refresh. A slow result must not overwrite a newer selection; duplicate refreshes need an explicit busy policy. | Choose cancellation, duplicate-click and refresh-failure policy before implementation. |
| D5. Row projection and scope | M1 rows show only locally observed fields: queue name, driver, default marker, saved relation, lookup status, source scope and observed-at time. Job state appears only when a known spool ID is available and is labeled job-scoped. | Approve exact columns, unknown/null copy and whether the selected detail is inline or a deliberate host extension. |
| D6. Search and filters | Search queue name and driver; filter `All`, saved relation, available/unavailable and observed job state only when evidence exists. Figma licensing, facility, location, workstation, port and queue-count samples remain deferred. | Approve the first filter set and whether local preference groups are deferred entirely. |
| D7. Deep-links and selection | Selection can open Printer Setup for profile repair, Print Center for durable recovery evidence or History for append-only records. Each action keeps its current owner and restores focus to the queue surface on return. | Approve link labels, keyboard order and selected-row retention after return/refresh. |
| D8. Spool evidence boundary | `SpoolJobObservation`/`SpoolJobMonitor` expose queue-reported state, age, job identity and timeout/unknown semantics. Even `Completed` has `PhysicalOutputVerified = false`. | Approve state severity/copy and the detail disclosure for queue-level versus job-level evidence. |
| D9. Commands and Figma/UIA route | The Figma command strip is research vocabulary only; command controls are absent or clearly disabled until a separate capability/confirmation/timeout/durable-outcome contract exists. Reuse node `2:37` read-only and use proposed `CC.P2.QueueConsole.*` IDs. | Name the command-contract owner and decide whether a future missing state needs the smallest state-specific Figma reference. |
| D10. Closure and regression | Close only with source fixtures, stale/duplicate refresh evidence, target-scale UIA/screenshots and upstream host/read-model sign-off. This packet adds no implementation/test result. | Fill D1-D10 owners and attach the clean implementation checkpoint when the slice is built. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`PrinterDiscoveryService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrinterDiscoveryService.cs#L9-L32) | Discovery requests Windows `Local` and `Connections` queues, marks the default and sorts deterministically. | A caught exception currently becomes an empty list, so UI cannot distinguish “none installed” from enumeration failure without a result-envelope decision. |
| [`PrinterInfo.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrinterInfo.cs#L3-L19) | The current row model contains name, driver, default marker, display name and catalog paper sizes. | It has no port, workstation, queue-count, license or live online field. |
| [`PrinterQueueLookup.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrinterQueueLookup.cs#L11-L78) | Saved queue lookup preserves requested/canonical identity and fails closed on empty, missing, inaccessible or canonical-mismatch queues. | It does not enumerate a fleet or observe every queue's jobs. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L907-L982) | The current template queue warning is observable, refreshes off the dispatcher and rejects a late result when the saved name changed. | It verifies one saved queue only; it is not a multi-row console view model. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1417-L1420) | The WPF shell exposes discovery through a read-only `GetInstalledPrinters()` seam. | The seam does not carry an enumeration error or refresh timestamp. |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L560-L584) | The existing status bar shows the saved queue and an actionable unavailable-queue warning. | It is not a queue table, filter rail or selected-detail surface. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L724-L728) and [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L883-L899) | Printer Setup remains the explicit queue-selection/repair owner and refreshes the saved lookup after return. | It does not provide a fleet-management action owner. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L735-L738), [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L147-L155) | History and Print Center are separate explicit entry points. | A future queue row must not duplicate their recovery/reprint authority. |
| [`SpoolJobMonitoring.cs`](../src/ANLAbel.Core/Printing/SpoolJobMonitoring.cs#L1-L101) | Queue/job states, observed-at data, timeout-to-unknown and the physical-output disclaimer are typed contracts. | A queue observation cannot verify media, sensor or the printed mark. |
| [`WindowsSpoolJobStatusReader.cs`](../src/ANLAbel.Printing/PrinterProfiles/WindowsSpoolJobStatusReader.cs#L11-L82) | One known printer/job ID can be read asynchronously; spooler errors and missing jobs become explicit terminal/unknown evidence. | It cannot discover jobs without an identity or prove a printer-level state. |
| [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md#L30-L37) | The upstream gate already defines P2 M1 as read-only discovery/status and defers command labels. | It does not provide P2-specific row copy, refresh failure policy or a dedicated owner sign-off. |
| [`SpoolJobMonitoringTests.cs`](../src/ANLAbel.UnitTests/SpoolJobMonitoringTests.cs#L1-L20), [`Program.cs`](../src/ANLAbel.Tests/Program.cs#L6022-L6115) | Existing tests cover modeled spool behavior, missing named queues and ViewModel warnings. | No test proves a queue-console click-through, multi-row refresh or target-scale UIA. |

## Surface and action ownership

| Surface/action | Owner | Safe action | Boundary |
| --- | --- | --- | --- |
| Queue discovery | `PrinterDiscoveryService` or an approved typed wrapper | Refresh `Local`/`Connections` rows with timestamp/error | No implicit default selection; no licensing claim. |
| Saved queue evidence | `IPrinterQueueLookup` + `MainViewModel` | Resolve the explicitly saved name and show requested/canonical status | No silent substitution or template mutation from a row click. |
| Queue table/detail | Approved P2 host view model | Present read-only rows and selected evidence | Do not invent fields absent from `PrinterInfo`/spool observation. |
| Search/filter | P2 projection only | Filter observed local fields and preserve unknown/error rows | No Figma facility, workstation, port or seat semantics. |
| Job observation | `SpoolJobMonitor`/`WindowsSpoolJobStatusReader` | Read one known spool ID; show state and observed-at basis | No auto-retry, queue mutation or physical-output claim. |
| Printer Setup deep-link | Existing `PrinterSetupWindow` | Repair/save explicit queue and return to refresh | Do not create a second profile editor. |
| Print Center deep-link | Existing `PrintCenterWindow` | Review/reconcile/acknowledge/void/guarded reprint through current owner | No row-level dispatch or recovery duplicate. |
| History deep-link | Existing append-only history owner | Open durable records/export through current owner | No queue observation flattened into history success. |
| Future command strip | Separate future command contract | Absent/disabled until capability, confirmation, timeout and durable result exist | Figma labels alone never enable mutation. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Discovering | Busy state, request scope and last successful timestamp | Wait/cancel/refresh according to approved policy | Do not replace current rows with an unscoped late result. |
| No queues | Empty result with discovery timestamp | Retry or open Printer Setup | Never select the Windows default silently. |
| Enumeration failed | Explicit error separate from zero rows | Retry/support path | Do not turn permission/spooler failure into “no printers”. |
| One/multiple queues | Rows with name, driver, default marker and source scope | Search/filter/select | Discovery does not prove availability, license or physical output. |
| Saved queue available | Requested/canonical match and observed-at | Inspect or open setup/history | Keep saved identity explicit. |
| Saved queue missing/mismatch | Error reason, requested name and last observation | Repair in Printer Setup | Never substitute another/default queue. |
| Selected queue with no job | Queue-level detail plus “no job observation” | Open setup or clear selection | Do not imply queue status from a missing job. |
| Job pending/printing/spooling | Job ID, printer, state, observed-at and scope label | Refresh or open Print Center | No success or physical-output claim. |
| Job paused/offline/paper-out/intervention/error | Severity, mapped state and reason | Resolve condition or review recovery | No auto-resume, retry or dispatch. |
| Job completed/retained/deleted/not found | Terminal state and physical-output disclaimer | Review durable evidence or acknowledge/void in owner | Queue completion is never physical verification. |
| Refresh canceled/timed out | Cancellation/timeout copy and prior timestamp | Retry explicitly | Do not report success from a partial/late read. |
| Search/filter no match | Query/filter and `0 results` with clear action | Clear or adjust | Do not imply unlisted queues are absent. |
| Deep-link return | Queue selection, refreshed status and focus target | Continue or repair | Do not lose identity or create duplicate action stack. |
| Future command unavailable | Absent/disabled command region with explanation | None until separate contract | Research labels cannot be actionable controls. |

## Figma metadata boundary

Read-only metadata was rechecked on 2026-08-13 for the [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, node `2:37`:

| Node | Metadata | Safe reuse | Missing P2 proof |
| --- | --- | --- | --- |
| `2:37` | `CC / Printers — Print Management`, `1280 x 800` | Density and information-architecture reference only | No local host, source error or runtime scale proof |
| `2:51` | Filter rail, `220 x 680` | Candidate filter grouping | Licensed/seat labels are omitted by scope; facility/location/paused labels require a local source contract |
| `2:72` | Main pane, `1000 x 680` | Candidate table/detail region | No queue/job scope, unknown/stale/permission state or WPF owner |
| `2:73` | Pause/Resume/Delete/Reserve/Unreserve/Settings text | Record as deferred research vocabulary | No capability, confirmation, timeout or durable outcome contract |
| `2:74` | Search by name, port and workstation text | Search-language reference | Current `PrinterInfo` does not supply port/workstation |
| `2:75`/`2:76` | Table header sample | Column-density reference | Queue counts/status values are not local evidence |
| `2:77`–`2:79` | Example printer rows | Empty/example density only | Names, IPs, counts and errors are research samples, not fixtures |
| `2:80` | Footer count/group toggles | Candidate local count/filter summary | Licensing and workstation toggles remain deferred |

**Routing decision:** use the Figma shell only as read-only visual input. Do not call `get_design_context`, create a P2 frame, copy sample rows or widen a WPF host from `1280 x 800`. The upstream host packet must choose the WPF owner; this packet supplies the source-backed state and action boundaries only.

## Accessibility and responsive gate

The proposed IDs in [`CC_P2_PRINT_QUEUE_UI_SPEC.md`](CC_P2_PRINT_QUEUE_UI_SPEC.md) remain unapproved until the host is selected. The concrete M1 vocabulary is:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root/status | `CC.P2.QueueConsole.Root` / `CC.P2.QueueConsole.Status` | Print queue console / Queue evidence status |
| Refresh | `CC.P2.QueueConsole.Refresh` | Refresh printer queues |
| Search | `CC.P2.QueueConsole.Search` | Search printer queues |
| Filters | `CC.P2.QueueConsole.Filters` | Queue filters |
| Queue table | `CC.P2.QueueConsole.QueueTable` | Discovered printer queues |
| Selected detail | `CC.P2.QueueConsole.Detail` | Selected queue evidence |
| Setup/Center/History links | `CC.P2.QueueConsole.OpenPrinterSetup`, `CC.P2.QueueConsole.OpenPrintCenter`, `CC.P2.QueueConsole.OpenHistory` | Open Printer Setup / Open Print Center / Open Print History |
| Deferred commands | `CC.P2.QueueConsole.Commands` | Queue commands unavailable |

Runtime evidence must cover `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception), keyboard order refresh → search/filter → table → detail → deep-link, focus restoration after return, one intentional scroll owner and no horizontal clipping of error/unknown copy. The target-scale pass must also preserve the protected Text/TextBox and explicit-queue/no-default-fallback contracts.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `CCP2_DiscoveryEmptyIsNotEnumerationFailure` | Zero queues and failed enumeration have distinct state/copy/timestamp. | Typed discovery result fixture and UIA capture. |
| `CCP2_LocalAndConnectionQueuesAreDeterministic` | `Local`/`Connections` scope, default marker and stable sort are preserved. | Discovery fixture with one/multiple queues. |
| `CCP2_SavedQueueCanonicalMismatchFailsClosed` | Requested/canonical names and repair action remain visible; no default fallback. | Lookup fixture and status capture. |
| `CCP2_SlowRefreshCannotOverwriteNewSelection` | A late result for an older request is ignored. | Cancellation/request-identity fixture. |
| `CCP2_SearchUsesObservedFieldsOnly` | Search/filter never exposes unsupported port/workstation/license fields as facts. | View-model projection fixture. |
| `CCP2_QueueAndJobScopeRemainDistinct` | Queue rows and known job observations show separate scopes and timestamps. | Read-model projection fixture. |
| `CCP2_SpoolStatesRemainNonPhysical` | Completed/retained/deleted/not-found states retain the physical-output disclaimer. | Spool monitor fixture. |
| `CCP2_DeepLinksPreserveActionOwners` | Setup, Print Center and History links return focus without duplicating actions. | WPF/UIA click-through evidence. |
| `CCP2_DeferredCommandsStayUnavailable` | Figma command labels do not enable queue mutation before a separate contract. | Disabled/absent-state screenshot and automation check. |
| `CCP2_TargetScaleNoHorizontalClipping` | M1 table/detail/error copy remains usable at target sizes/scales. | Screenshots/UIA at all required scales. |
| `Protected_TextTextBox_contract_unchanged` | Queue UI work changes no Text/TextBox ownership, geometry, wrap/clip, padding, resize or print parity. | Protected regression suite after implementation changes. |

## No-go list

- Do not select or imply a host until the upstream P1/P2/P5 host packet records one option and action owner.
- Do not convert `PrinterDiscoveryService`'s current empty-list exception fallback into a product success state; preserve the error-envelope gap for implementation.
- Do not silently use the Windows default, another queue, a Figma sample printer or a guessed port/workstation/license field.
- Do not flatten queue-level and job-level evidence or mark queue/spool completion as physical output.
- Do not add Pause/Resume/Delete/Reserve/Unreserve, auto-retry, dispatch, recovery or reprint actions to the M1 table.
- Do not create a second Printer Setup, Print Center, History, queue identity or reprint authority.
- Do not edit Figma or infer a missing state from `2:37` metadata alone.
- Do not change Text/TextBox behavior or label geometry to fit a future queue console.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the packet open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Host/deep-link/action boundary | `TBD` | `TBD` | `TBD` |  |
| D2. Discovery authority/error envelope | `TBD` | `TBD` | `TBD` |  |
| D3. Queue identity/fallback policy | `TBD` | `TBD` | `TBD` |  |
| D4. Refresh/cancellation/request identity | `TBD` | `TBD` | `TBD` |  |
| D5. Row fields and queue/job scope | `TBD` | `TBD` | `TBD` |  |
| D6. Search/filter vocabulary | `TBD` | `TBD` | `TBD` |  |
| D7. Deep-link selection/focus behavior | `TBD` | `TBD` | `TBD` |  |
| D8. Spool/physical-evidence copy | `TBD` | `TBD` | `TBD` |  |
| D9. Commands/Figma/UIA route | `TBD` | `TBD` | `TBD` |  |
| D10. Runtime/regression closure | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the P2 M1 queue-visibility slice may move from documentation review to implementation evidence only after the upstream host decision is filled, D1-D10 here are owned, discovery/error/identity/refresh fixtures pass, command controls remain deferred, target-scale UIA/screenshots are attached and the protected Text/TextBox/no-default-fallback/physical-output boundaries remain unchanged. Until then this is an open queue-observability UI contract, not a shipped Control Center.
