# CC-P1 Operations Overview UI/UX handoff

**Status:** roadmap/pre-implementation; the recovery surface exists, but the unified overview is not implemented or runtime-verified
**Parent plan:** [`MASTER_PLAN.md`](../MASTER_PLAN.md#control-center--lms-operations--large-improvement-plans-2026-08-12), section 1, CC-P1
**Cross-surface handoff:** [`10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**Current verification boundary:** [`11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md)
**Program index / host gate:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md), sections 2 and 5
**Host decision packet:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Figma reference:** [NiceLabel Control Center research shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`

This is a documentation handoff, not an authorization to modify the dirty implementation wave. It does not claim a web Control Center, multi-tenant LMS, physical print completion, or license-seat parity.

CC-P1 is the upstream host and local-evidence gate for the suggested `CC-P1 -> CC-P2 -> CC-P5` sequence. Close the host choice, refresh/read-model ownership and explicit deep-link contract here before downstream queue or History surfaces add navigation. The shared index coordinates this sequence; this handoff remains authoritative for P1 states and acceptance details.

## 1. Operator task

From the ANLAbel desktop shell, an operator should be able to answer three questions without opening several unrelated dialogs:

1. Is the configured industrial queue currently verified and healthy?
2. Are there durable print/recovery events that require an explicit decision?
3. What local activation or entitlement context applies, and where can the operator repair it?

The overview then provides explicit links to **Print Center**, **Printer Setup** and **Print History**. It is an operations home for local evidence, not a dispatch surface: it must never infer physical output, silently fall back to the Windows default queue, or retry a job automatically.

## 2. Current implementation evidence

| Surface | Evidence in the current checkout | Acceptance boundary |
| --- | --- | --- |
| Existing recovery dialog | [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L1) is `1180 x 720` with a `900 x 520` minimum. It exposes refresh, scan/search, a durable-job grid, queue reconciliation, acknowledge/void, linked reprint approval, guarded preview and redacted support evidence. | Preserve these explicit actions when the overview links into the dialog; do not turn a card click into an implicit print or retry. |
| Recovery entry point | The status-bar warning and `ReviewPrintRecovery_Click` are in [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L585) and [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L147). | The overview must be reachable from the main shell and must refresh the same report before presenting a count or warning. |
| Queue evidence | `MainViewModel.PrinterQueueStatus` and `HasPrinterQueueWarning` are backed by the named-queue lookup in [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L907). | Missing/unverified queues remain warnings. The overview must not replace a missing named queue with the Windows default. |
| Durable recovery evidence | `PrintRecoveryReport` is refreshed by the main view model and rendered by `PrintCenterWindow`; job lineage and operation evidence are persisted through [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs#L1). | Counts and recent states must be derived from the durable report/log contract, with a visible stale/diagnostic state when refresh fails. |
| Existing deep links | `PrinterSetup_Click` and `PrintHistory_Click` are wired in [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L724). | The overview can route to these existing actions; it must not invent a second printer or history authority. |
| Local activation | The current activation dialog is [`ActivationWindow.xaml`](../src/ANLAbel.App/ActivationWindow.xaml#L1), backed by [`TrialLicenseService.cs`](../src/ANLAbel.App/Services/TrialLicenseService.cs#L1). | A first slice may show local activation/entitlement status and link to activation. It must not display the Figma research value `Used: 0 Total: 100` as if it were ANLAbel data. |
| Existing support gate | The application runner includes `print center exports redacted support evidence from durable jobs` in [`Program.cs`](../src/ANLAbel.Tests/Program.cs#L188). | Keep the evidence export redacted and non-physical; add overview-specific regression only when the UI is implemented. |

## 3. Figma evidence and routing

Read-only Figma metadata was checked on 2026-08-13 for Control Center Page `0:1`. The top-level `CC / Overview` frame is `2:2`, `1280 x 800`. Its useful structural nodes are:

| Node | Name | Size | How it informs ANLAbel |
| --- | --- | --- | --- |
| `2:3` | TopBar | `1280 x 48` | A visual reference for a compact operations header; not a WPF size mandate. |
| `2:6` | Primary navigation | `1280 x 40` | Provides the Overview/Documents/Applications/Printers/History/Analytics/Administration task grouping. ANLAbel should expose only local surfaces that actually exist. |
| `2:16` | Server Info | `1240 x 72` | Reuse the idea of a context/status banner; use local workstation, queue and refresh evidence instead of a server claim. |
| `2:20` | Operational Workstations | `820 x 180` | Maps to a future local queue/workstation summary only if the underlying evidence exists. Empty state must be honest. |
| `2:25` | License Status | `400 x 180` | Reuse the card hierarchy, but source content from local activation/entitlement APIs; do not copy LMS seat totals. |
| `2:30` | Recent Errors | `1240 x 200` | A possible layout for durable recovery/error summaries; current ANLAbel has no shipped 24-hour dashboard aggregation yet. |
| `2:35` | Research footer | `1240 x 44` | Treat Figma QA/research text as design metadata, never as release or license copy. |

The same page has `CC / Printers — Print Management` (`2:37`, `1280 x 800`) and `CC / History` (`3:85`, `1280 x 800`). They are future visual references for deep-link targets, not part of the first CC-P1 implementation. No Figma edit or new file is needed for this slice: `2:2` already supplies the structural reference. A Figma frame alone is not runtime proof.

## 4. Proposed first slice

Keep CC-P1 vertical and evidence-backed:

1. **M1 — Operations cards:** queue verification/health, pending recovery count, recent terminal faults and local activation/entitlement summary from existing services and durable logs.
2. **M1 — Explicit deep links:** open the existing Print Center, Printer Setup and Print History actions; preserve owner/refresh behavior.
3. **M2 — Queue identity:** show the configured printer and last observation timestamp, with a repair path for missing, stale or unavailable queues.
4. **M3 — Deferred filters:** workstation/user or multi-user filters only after a durable local identity model and the admin phase exist.

The first implementation decision is still open: extend `PrintCenterWindow` with a summary header, add a separate `OperationsOverviewWindow`, or make a ribbon/status-bar hub that opens both. Do not implement all three surfaces at once.

## 5. User-visible state matrix

| State | Visible evidence | Safe next action | Explicit non-claim |
| --- | --- | --- | --- |
| No durable activity | `No recent recovery activity` plus last queue observation or `Not verified` | Open Printer Setup or Print Center; refresh | No jobs does not prove a physical printer is idle or successful. |
| Verified/healthy named queue | Canonical queue name, availability and observation time | Open Print Center, Printer Setup or History | Queue observation is not physical label verification. |
| Pending recovery | Count, latest job IDs/states and reason severity | Open Print Center and choose Reconcile/Acknowledge/Void/Reprint explicitly | Never auto-retry or dispatch from a summary card. |
| Terminal fault | Durable reason, queue/spool/manifest identity where available | Inspect details or export redacted support evidence | A failed/voided event remains history; do not rewrite it as success. |
| Queue missing, stale or inaccessible | Warning with named queue and lookup error | Open Printer Setup; repair/reselect the explicit queue | Do not silently use the Windows default queue. |
| Activation/entitlement unavailable | Local status and a link to Activation | Open Activation or continue in the documented trial/limited mode | Do not claim LMS server status or seat totals. |
| Refresh/loading | Busy indicator, last-known timestamp and disabled duplicate refresh | Wait for completion; show the error if it fails | Stale values must not be presented as current. |
| Deep-link target unavailable | Non-destructive message and return path to the main shell | Close the message or use the existing ribbon action | Do not create a second hidden authority for the target surface. |

## 6. WPF mapping and acceptance contract

| Overview region | Existing/proposed WPF owner | Stable mapping requirement |
| --- | --- | --- |
| Header/context | New overview host (window, ribbon hub or Print Center extension — owner decision) | Stable `OperationsOverview` region and a named refresh command. |
| Queue card | `MainViewModel.PrinterQueueStatus`, `RefreshPrinterQueueStatusAsync` | Show canonical name, availability, message and timestamp; preserve fail-closed queue selection. |
| Recovery card | `MainViewModel.PrintRecoveryReport`, `PrintCenterWindow` | Link to `ReviewPrintRecovery_Click`; refresh before count; expose no implicit action. |
| License card | `TrialLicenseService`, `ActivationWindow` | Use local activation vocabulary and a stable `Open Activation` action. |
| Recent errors | `PrintOperationLogService` plus a new read-only aggregate seam if approved | Record the time window, source and empty/error state; do not invent a 24-hour metric without a defined clock/source. |
| Deep links | Existing `PrinterSetup_Click`, `PrintHistory_Click`, recovery handler | Reuse existing commands and ownership; UI Automation names must remain stable. |

Protected behavior check:

- [ ] No Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or designer/print parity is changed.
- [ ] No overview card mutates label geometry, data bindings or print manifests.
- [ ] Any future contract change updates `PLAN.md`, the TextBox research record and named regression gates together.

## 7. Runtime and regression gates

Before calling CC-P1 implemented or verified, attach:

- runtime screenshots and/or UI Automation at `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception);
- keyboard/focus path for refresh, Print Center, Printer Setup, Print History and Escape/close;
- empty, healthy, pending-recovery, terminal-fault, missing-queue, activation-unavailable and refresh-failure fixtures;
- proof that the cards use durable local evidence and preserve timestamps/diagnostics after a failed refresh;
- existing Print Center recovery and redacted-support-evidence checks, plus a named overview regression when code exists;
- build, unit-test and application-runner output copied into the owning clean checkpoint;
- explicit non-claims for physical verifier/grade, printer-native completion, multi-tenant LMS and copied Figma license totals.

Suggested commands:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 8. Owner decisions before UI implementation

1. Choose the host: extend `PrintCenterWindow`, add `OperationsOverviewWindow`, or use a ribbon/status-bar hub.
2. Approve local copy for queue health, recent faults, activation/entitlement and empty states; explicitly reject LMS/server wording unless a separate product decision exists.
3. Define the source and time window for “recent errors” and the behavior when logs or queue lookup are unavailable.
4. Assign stable AutomationIds, runtime screenshot/UI Automation ownership and the first implementation checkpoint.
5. Keep the existing no-auto-retry, explicit queue identity, guarded reprint and protected Text/TextBox contracts unchanged.

Until these decisions and runtime evidence exist, this document is a handoff—not a claim that Operations Overview is shipped or design-verified.
