# CC-P1 Operations Overview owner decision packet

**Status:** documentation-only owner gate for a local read/route overview; no new host, aggregate service, Figma edit or Text/TextBox change is authorized (2026-08-13)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Upstream host/read-model gate:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Host decision packet:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Read-model contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**UI/UX handoff:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md)
**UI/UX specification:** [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md)
**P2 queue owner packet:** [`CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md`](CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md)
**P5 recovery owner packet:** [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md)
**Figma routing:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and boundary

The current desktop shell already exposes three separate evidence/action paths: named-printer verification, durable Print Center recovery and local activation/trial handling. It does not have one Operations Overview host, a shared refresh envelope, a recent-terminal-fault projection or a unified activation read model. The upstream P1/P2/P5 gate owns the host choice and cross-surface read model; this packet makes the concrete P1 card/read-route contract reviewable without implementing a host.

This packet covers:

- a local queue-health card derived from explicit saved-queue lookup;
- a recovery card derived from the durable state store and Print Center owner;
- a local activation/trial card only when a product build exposes that service;
- recent software-event diagnostics only after a defined source, time window and aggregation seam;
- explicit deep-links to Printer Setup, Print Center and History;
- refresh/error/timestamp semantics, UIA proposals, target-scale gates and source-backed fixtures;
- read-only Figma reuse of `2:2` without copying server, workstation, user or license-seat research values.

It does not choose among the upstream host options, add a second recovery/history/queue authority, dispatch/retry/approve/void/reprint from a card, create multi-user identity or entitlement semantics, claim physical output, edit Figma, or alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize, overflow or print parity.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Host and navigation boundary | Use the host selected by the upstream P1/P2/P5 gate. Keep the overview read/route-only and reuse existing Printer Setup, Print Center and History action owners. | Select one host (A/B/C), root/content/status owner and return/focus path before WPF navigation changes. |
| D2. Queue-health source | `MainViewModel.PrinterQueueStatus` plus `IPrinterQueueLookup` is the saved-queue authority; show requested/canonical identity, availability and diagnostic copy. | Approve a typed refresh envelope with timestamp/error (recommended) because current status has no observation time. |
| D3. Recovery-card source | `PrintJobRecoveryService.LoadAsync` over `PrintJobStateStore` is authoritative for non-terminal candidates and store diagnostics; Print Center remains the action owner. | Approve count/severity copy, refresh failure behavior and whether the card exposes candidate IDs or only a count/deep-link. |
| D4. Recent-terminal-fault scope | Do not invent the Figma “Recent Errors” table. Add a read-only aggregate only after a defined source (state store, operation JSONL or CSV), clock/timezone, redaction and terminal-state window are approved. | Choose source precedence, time window, timezone and empty/corrupt-tail behavior. |
| D5. Activation/entitlement source | Use `TrialLicenseService.Check()` only in builds that compile `TRIAL_BUILD`; show local trial/activated/expired/tampered/storage states and the existing Activation dialog route. | Approve cross-build behavior when activation is not compiled; never show server seats or Figma `Used: 0 Total: 100`. |
| D6. Refresh/request safety | One explicit refresh should capture queue/recovery/activation request identity, preserve last-known timestamp and reject late/partial results. Duplicate refresh needs a busy/cancel policy. | Choose all-card refresh versus independent refresh, cancellation and partial-failure copy. |
| D7. Card/deep-link semantics | Cards are read-only summaries. Open Printer Setup, Print Center or History explicitly; card click must not retry, dispatch, mutate templates or approve reprint. | Approve labels, keyboard order, disabled target behavior and focus restoration. |
| D8. Evidence/provenance copy | Every non-empty card shows source, observed-at basis and severity; unknown/stale/repair states remain visible. Queue/spool/recovery evidence never becomes physical completion. | Approve concise copy and redaction rules for job IDs, printer names, paths and activation customer/expiry. |
| D9. Figma/UIA route | Reuse Figma `2:2` card hierarchy read-only and use proposed `CC.P1.Overview.*` IDs. Request a smallest state-specific node only if a concrete missing state cannot be resolved by WPF reuse. | Name design/UIA owner and approve whether a P1 state reference is needed before implementation. |
| D10. Closure and regression | Close only with host/read-model sign-off, source fixtures, target-scale UIA/screenshots and build/unit/application evidence. This packet adds no implementation/test result. | Fill D1-D10 owners and attach the clean implementation checkpoint. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L560-L594) | The existing status bar shows the saved printer, an unavailable-queue warning and a pending-recovery action button. | It is not a unified overview, card read model or refresh timestamp surface. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L147-L154) | `ReviewPrintRecovery_Click` opens the existing `PrintCenterWindow` owner. | The entry point does not establish overview card state or target-scale behavior. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L724-L728) and [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L883-L899) | Printer Setup is the explicit queue-selection/repair flow and saved status refreshes after return. | It does not supply a fleet or operations overview. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L735-L738) | Print History is a separate explicit deep-link. | It does not provide a recent-events aggregate or merge authority. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L907-L982) | Named queue lookup is observable, asynchronous and late-result guarded by saved-name identity; missing queues fail closed. | `PrinterQueueLookupResult` has no observed-at timestamp, stale threshold or aggregate card projection. |
| [`PrinterQueueLookup.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrinterQueueLookup.cs#L11-L78) | Requested/canonical queue identity and no-default-fallback behavior are typed. | It resolves one saved queue, not a dashboard-wide health metric. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L988-L1007) | Recovery count/status text is derived from `PrintJobRecoveryReport`; repair diagnostics and pending candidates are visible. | There is no recent-terminal-fault card or unified refresh state. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1091-L1101) | Recovery refresh replays the durable state store and updates status for pending/repair states. | Current code does not define a dashboard timestamp, independent error envelope or recent-fault window. |
| [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs#L81-L115) | Non-terminal candidates and store diagnostics are explicit; automatic retry and physical verification remain false. | Terminal events are filtered from this report, so it cannot by itself populate “recent errors.” |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs#L205-L220) and [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs#L245-L274) | Replay returns latest events plus diagnostics and preserves a valid prefix when a tail is malformed. | It does not define a P1 time-window aggregate or UI copy. |
| [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs#L5-L78) and [`PrintOperationLogEntry.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogEntry.cs#L11-L64) | Operation JSONL is a best-effort append-only trace with local timestamp, outcome, queue/spool and support fingerprints. | The service has no read/aggregate API, and a write failure must not be presented as a complete recent-events source. |
| [`TrialLicenseService.cs`](../src/ANLAbel.App/Services/TrialLicenseService.cs#L10-L47) | Conditional trial builds can distinguish valid, expired, clock-tampered, storage-error and signed activation states, including expiry/customer payload. | It is internal/app-level and not a cross-build `MainViewModel` read model; customer/expiry copy needs privacy/owner policy. |
| [`App.xaml.cs`](../src/ANLAbel.App/App.xaml.cs#L16-L109) and [`ActivationWindow.xaml`](../src/ANLAbel.App/ActivationWindow.xaml#L1-L38) | `TRIAL_BUILD` startup/timer owns license display and opens the existing activation dialog. | A research license card is not evidence of a local activation card in every build. |
| [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md) | The cross-surface rule keeps state-store lineage, operation JSONL, CSV detail and live queue lookup separate. | It does not implement the P1 projection or choose a host. |
| [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md#L30-L37) | The upstream gate already requires one host, one projection owner and explicit P1 cards/deep-links. | It does not provide P1-specific source/time/privacy decisions. |

## Surface and action ownership

| Surface/action | Owner | Safe action | Boundary |
| --- | --- | --- | --- |
| Overview host/context | Upstream selected host | Show local scope, refresh state and last-known evidence | No new host until upstream choice is filled. |
| Queue health card | `MainViewModel` + `IPrinterQueueLookup` | Display saved queue identity, availability and repair route | No default fallback, queue mutation or physical claim. |
| Recovery card | `PrintJobRecoveryService`/`PrintJobStateStore` | Display pending/repair count and deep-link to Print Center | No reconcile, acknowledge, void or reprint from the card. |
| Recent software events | Approved future read-only aggregate | Show source/timestamp/severity and select/detail only | No invented 24-hour metric, source merge or success counter. |
| Activation card | `TrialLicenseService` when available | Display local trial/activation state and open Activation | No LMS seats, server identity or cross-build false “licensed” state. |
| Printer Setup | Existing `PrinterSetupWindow` | Repair/save the explicit queue and return | No second profile editor. |
| Print Center | Existing `PrintCenterWindow` | Reconcile/operator decision/guarded reprint through current owner | No row/card dispatch or automatic retry. |
| Print History | Existing history file/export owner | Open durable history/export | No queue observation flattened into terminal success. |
| Refresh | Approved P1 refresh seam | Refresh cards with request identity and timestamps | No stale/partial result shown as current. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Initial/loading | Busy state, scope and last successful refresh | Wait/cancel/refresh per approved policy | Do not show un-timestamped values as current. |
| No durable activity | Explicit empty recovery/events copy plus queue observation | Open Setup/Print Center or refresh | No activity does not prove physical idle/success. |
| Healthy named queue | Requested/canonical name, available state and observed-at | Open Setup/Print Center/History | Queue health is not physical verification. |
| Pending recovery | Count, latest candidate state/reason and source time | Open Print Center and choose an explicit action | No auto-retry, dispatch or approval from summary. |
| Store repair required | Diagnostics, valid-prefix/repair wording and warning severity | Open Print Center/support path | Do not present candidate count as safe to retry. |
| Terminal software fault | Source, terminal outcome, reason and event time | Inspect/detail/export redacted support evidence | Do not rewrite failed/voided history as success. |
| Recent-event source unavailable | Source-specific error and last successful time | Retry or use existing owner path | Do not show zero or empty as “no errors.” |
| Queue missing/mismatch | Named queue, canonical mismatch/error and repair link | Open Printer Setup | Never select Windows default or another queue. |
| Activation valid/expired/tampered/storage error | Local status, scope and repair copy | Open Activation or documented limited mode | No server/LMS/seat claim; fail closed for protected trial behavior. |
| Activation not compiled | “Activation status unavailable in this build” or approved omission | Continue through product build policy | Do not infer entitlement from Figma or title text. |
| Partial refresh failure | Per-card source/error with last-known timestamps | Retry the failed source | Do not combine new and stale data without labels. |
| Deep-link unavailable | Non-destructive message and return path | Close/use existing ribbon action | Do not create hidden duplicate authority. |

## Figma metadata boundary

Read-only metadata was rechecked on 2026-08-13 for the [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, node `2:2`:

| Node | Metadata | Safe reuse | Missing P1 proof |
| --- | --- | --- | --- |
| `2:2` | `CC / Overview`, `1280 x 800` | Overall card density and hierarchy | No local host, source aggregation or WPF scale proof |
| `2:3` | TopBar, `1280 x 48` | Optional context/header rhythm | Server identity/help behavior is not local evidence |
| `2:6` | Navigation, `1280 x 40` | Local module grouping only | No web/LMS navigation or route owner |
| `2:16` | Server Info, `1240 x 72` | Context/refresh banner pattern | Replace server name/time with local queue/workstation/source basis |
| `2:20` | Operational Workstations, `820 x 180` | Queue/recovery summary density | No local fleet/workstation identity model |
| `2:25` | License Status, `400 x 180` | Activation card hierarchy | No LMS Enterprise or `Used: 0 Total: 100` evidence |
| `2:30` | Recent Errors, `1240 x 200` | Event table density only | No current terminal-fault aggregate/time window |
| `2:35` | Research footer, `1240 x 44` | Omit or replace with local support/version copy | Figma QA/version text is not release evidence |

**Routing decision:** reuse `2:2` as read-only visual input. Do not call `get_design_context`, create a P1 frame or copy research server, workstation, user, license or error values. The upstream host packet must select the WPF owner; this packet supplies local card/source and fail-closed boundaries only.

## Accessibility and responsive gate

The proposed IDs in [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md) remain unapproved until the host is selected. The concrete vocabulary is:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root/status | `CC.P1.Overview.Root` / `CC.P1.Overview.Status` | Operations overview / Overview status |
| Refresh | `CC.P1.Overview.Refresh` | Refresh operations evidence |
| Context | `CC.P1.Overview.Context` | Local operations context |
| Queue card/link | `CC.P1.Overview.Queue` / `CC.P1.Overview.OpenPrinterSetup` | Queue health / Open Printer Setup |
| Recovery card/link | `CC.P1.Overview.Recovery` / `CC.P1.Overview.OpenPrintCenter` | Print recovery / Open Print Center |
| Activation card/link | `CC.P1.Overview.Activation` / `CC.P1.Overview.OpenActivation` | Local activation status / Open Activation |
| Recent events | `CC.P1.Overview.RecentEvents` | Recent software events |
| History link | `CC.P1.Overview.OpenHistory` | Open Print History |

Runtime evidence must cover `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception), keyboard order context → refresh → queue → recovery → activation → actions → recent events, focus restoration after each deep-link, one intentional vertical scroll owner and no clipping of error/repair/privacy copy. The target-scale pass must preserve the protected Text/TextBox, explicit queue and no-auto-retry contracts.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `CCP1_QueueCardUsesExplicitSavedIdentity` | Requested/canonical/available state is shown and missing queue never falls back to Windows default. | Lookup fixture and UIA card capture. |
| `CCP1_QueueRefreshTimestampAndLateResult` | Last-known/observed-at state is visible and an older queue result cannot overwrite a newer selection. | Async refresh fixture. |
| `CCP1_RecoveryCardPreservesDurableDiagnostics` | Pending candidates and corrupt-tail diagnostics map to warning states and deep-link Print Center. | State-store fixture and card screenshot. |
| `CCP1_RecentFaultWindowIsDefined` | Source, timezone, terminal-state filter and empty/corrupt behavior are explicit before the aggregate is enabled. | Aggregate contract fixture. |
| `CCP1_ActivationBuildBoundary` | Trial build shows local trial/activation states; non-trial build does not claim a missing activation service. | Conditional build/startup evidence. |
| `CCP1_PartialRefreshDoesNotMixEpochs` | One failed source retains its timestamp/error while other cards identify their newer epoch. | Multi-source refresh fixture. |
| `CCP1_DeepLinksPreserveActionOwners` | Setup, Print Center and History return focus without duplicate dispatch/recovery authority. | WPF/UIA click-through. |
| `CCP1_NoResearchValuesBecomeFixtures` | Figma server, seat, workstation and sample-error values never enter runtime data/tests. | Fixture/source review. |
| `CCP1_TargetScaleNoClipping` | Cards, diagnostics, privacy copy and actions remain usable at target sizes/scales. | Screenshots/UIA at all required scales. |
| `Protected_TextTextBox_contract_unchanged` | Overview work changes no Text/TextBox ownership, geometry, wrap/clip, padding, resize or print parity. | Protected regression suite after implementation changes. |

## No-go list

- Do not select a host, create a new window or add navigation before the upstream P1/P2/P5 host packet records one option and owner.
- Do not present Figma server, workstation, user, license-seat, time or recent-error samples as ANLAbel data.
- Do not invent a recent-errors count without source precedence, terminal-state filter, timezone, redaction and failure semantics.
- Do not mix queue lookup, state-store lineage, operation JSONL and CSV history into one success counter or timestamp.
- Do not let cards dispatch, retry, approve, void, reconcile or reprint; deep-links remain explicit.
- Do not hide missing queue, corrupt state, activation storage/tamper or partial-refresh errors behind zero/empty/healthy copy.
- Do not create a second Printer Setup, Print Center, History, activation or queue identity authority.
- Do not edit Figma or change Text/TextBox behavior for this documentation-only gate.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the packet open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Host/navigation/action boundary | `TBD` | `TBD` | `TBD` |  |
| D2. Queue card source/timestamp envelope | `TBD` | `TBD` | `TBD` |  |
| D3. Recovery card source/severity | `TBD` | `TBD` | `TBD` |  |
| D4. Recent-terminal-fault source/window | `TBD` | `TBD` | `TBD` |  |
| D5. Activation/build/privacy policy | `TBD` | `TBD` | `TBD` |  |
| D6. Refresh/cancellation/partial failure | `TBD` | `TBD` | `TBD` |  |
| D7. Card/deep-link/focus behavior | `TBD` | `TBD` | `TBD` |  |
| D8. Provenance/redaction/non-physical copy | `TBD` | `TBD` | `TBD` |  |
| D9. Figma route/AutomationIds | `TBD` | `TBD` | `TBD` |  |
| D10. Runtime/regression closure | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the P1 Operations Overview may move from documentation review to implementation evidence only after the upstream host/read-model decisions are filled, D1-D10 here are owned, queue/recovery/activation/recent-event fixtures pass, deep-links preserve existing action owners, target-scale UIA/screenshots are attached and the protected Text/TextBox/no-default-fallback/no-auto-retry/physical-output boundaries remain unchanged. Until then this is an open local operations read/route contract, not a shipped Control Center.
