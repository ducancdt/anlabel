# CC-P5 History + controlled reprint UI/UX handoff

**Status:** roadmap/pre-implementation; append-only logs and guarded reprint services exist, but a unified History browser is not implemented or runtime-verified
**Parent plan:** [`MASTER_PLAN.md`](../MASTER_PLAN.md#control-center--lms-operations--large-improvement-plans-2026-08-12), section 5, CC-P5
**Related CC-P1 handoff:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md)
**Related CC-P2 handoff:** [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md)
**Cross-surface handoff:** [`10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**Current verification boundary:** [`11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md)
**Program index / host gate:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md), sections 2 and 5
**Host decision packet:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Read-model contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Figma reference:** [NiceLabel Control Center research shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, `CC / History` node `3:85`

This is a documentation handoff, not an authorization to modify the dirty implementation wave. It does not claim a shipped Control Center, physical-output verification, or permission to create a second reprint/dispatch stack.

CC-P5 is the first downstream read-model/reprint gate after P1 and P2. It must reuse the selected host, queue identity and Print Center action owner; the program index coordinates that order, while this handoff remains authoritative for source precedence, provenance, detail and exact-manifest acceptance.

## 1. Operator task

An operator needs one local, read-only activity surface to answer:

1. what was submitted, for which template/queue, and what durable evidence exists;
2. whether the outcome is a software/spool observation, a terminal failure, an operator decision, or accepted physical-verifier evidence;
3. which job lineage, manifest fingerprint and queue observations support the record;
4. whether a controlled reprint may be requested, approved, prepared and dispatched as separate explicit steps.

The first browser must preserve source provenance. ANLAbel currently has a human-facing per-label CSV, a best-effort job-level JSONL trace and a hash-chained job-state JSONL. “Unified” means a read model over those sources; it must not rewrite or silently flatten them into one success flag.

## 2. Current implementation evidence

| Surface | Evidence in the current checkout | Acceptance boundary |
| --- | --- | --- |
| Human-facing print history | [`PrintLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintLogService.cs#L1) appends one row per label to `print-history.csv` and exports an on-demand `.xlsx` report at `#L65`. | Preserve per-label fields and CSV provenance. Export is a user action, not the History browser's storage authority. |
| Job-level operation trace | [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs#L10) appends machine-readable JSONL under local app data; write failures are intentionally best-effort and never block printing. [`PrintOperationLogEntry.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogEntry.cs#L11) carries queue/spool, output hashes, manifest, operator action and redacted support fingerprint fields. | The browser must show missing/unavailable trace evidence honestly; a best-effort log cannot be presented as a complete audit ledger. |
| Durable job lineage | [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs#L1) sequences and hash-chains lifecycle events, replays the latest valid event per job at `#L205`, and exposes corrupt-tail diagnostics. | The state store remains the source of truth for lifecycle/reprint lineage. A corrupt tail blocks unsafe action and must be visible in the UI. |
| Recovery classification | [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs#L39) returns only non-terminal candidates, preserves store diagnostics and sets `AutomaticRetryAllowed` to false. | History may link to Print Center for reconciliation, but must never turn a row into an implicit retry. |
| Operator decisions | [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs#L1) records Acknowledge/Void/RequestReprint/ApproveReprint in the same event store. Request creates a linked `Created` child without dispatch at `#L71`; approval checks the exact immutable manifest at `#L116`. | Keep actions audit/lineage-first. Void does not send a printer cancellation; request/approval do not dispatch. |
| Current recovery UI | [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L1) is a `1180 x 720` recovery dialog with job search, durable evidence grid, explicit actions and redacted support export. | Reuse this action owner or deep-link to it. Do not build a second reprint button path in History. |
| Current History entry | [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L735) opens the CSV through `OpenPrintHistoryFile`; `#L740` exports it to Excel. [`MainViewModel.cs#L4803`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L4803) reports when the CSV does not yet exist. | The first History browser must replace or sit beside the external-file shortcut deliberately; do not hide the existing path or claim that it is already a browser. |
| Guarded dispatch | [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1247) rebuilds the current manifest and blocks reprint dispatch on row/label count, queue, DPI, design, output-contract or data fingerprint mismatch. | A History row can link to prepare/dispatch only after explicit approval and a fresh exact-manifest comparison. |
| Existing regression evidence | The application runner names `print log CSV append is fast and escapes fields correctly`, `print operation log records job-level trace`, `quick print log carries bounded queue observation` and `print center exports redacted support evidence from durable jobs`. | These are software contracts, not proof of a History UI click-through or physical printer completion. |

## 3. Figma evidence and routing

Read-only Figma metadata was checked on 2026-08-13 for node `3:85`, `1280 x 800`. The structure is:

| Node | Name | Position/size | ANLAbel routing |
| --- | --- | --- | --- |
| `3:86` | TopBar | `(0, 0)`, `1280 x 48` | Reusable research chrome only; no WPF size mandate. |
| `3:89` | Primary navigation | `(0, 48)`, `1280 x 40` | Expose only local modules that exist; do not imply a web Control Center. |
| `3:99` | Filters | `(16, 104)`, `1248 x 56` | Candidate date/module/workstation/user/status filters. Each filter needs a defined local source and timezone/unknown rule. |
| `3:101` | Activity table | `(16, 176)`, `1248 x 600` | Candidate read-only table and detail owner; do not treat sample rows as live evidence. |
| `3:102` / `3:103` | Table header | `1248 x 32`; Submitted, Type, Module, Workstation, User, Status, Details | Use as visual language after field provenance and privacy rules are approved. |
| `3:104`–`3:108` | Sample activity rows | y `224`–`352` | Research samples only; example users/workstations/statuses must not become fixtures or claims without source data. |
| `3:109` | Activity details / Reprint / Errors tabs note | `(12, 240)` inside table frame | Affordance only: metadata exposes no concrete detail/reprint/error child states. A state-specific local reference or explicit WPF reuse decision remains open. |

The frame is a useful information-architecture reference, but it does not define ANLAbel's three-source merge semantics, manifest privacy, local time zone, corrupt-tail behavior or reprint gates. No Figma edit or new file is needed for this handoff: `3:85` answers the shell question, while runtime screenshots/UI Automation and the source contract close the slice.

## 4. Proposed implementation sequence

Keep CC-P5 read-only first and lineage-safe:

1. **M1 — Canonical activity read model:** normalize job-level state events and operation-log entries into a view model with `source`, `JobId`, timestamp, lifecycle/action, queue/spool identity, manifest fingerprint, outcome evidence and diagnostics. Keep the per-label CSV as a linked detail/export source rather than pretending it is the same event granularity.
2. **M1 — Browser/filter shell:** show empty/loading/stale/corrupt-tail/error states and filters for time range, module/print mode, printer/queue, lifecycle/operator action and outcome. Preserve local timestamps and UTC evidence where both exist.
3. **M2 — Detail drawer:** expose the selected job's complete lineage, manifest/output/design hashes, queue observations, related reprint child, actor/reason and support-evidence fingerprint. Never display raw label row payloads as a convenience detail field.
4. **M2 — Controlled reprint deep-link:** link Request, Approve, guarded Preview and Dispatch to the existing `PrintCenterWindow`/`MainViewModel` services. The browser may show eligibility, but cannot dispatch from a list row without the explicit sequence.
5. **M3 — Support export:** reuse the redacted support-evidence contract for a selected durable job and display the resulting fingerprint; export failure must not alter lineage.

The host decision is open: add a read-only `HistoryWindow`, add a History tab/sibling to `PrintCenterWindow`, or make History a view inside the CC-P1 operations surface. Choose one owner for filters, detail and reprint actions before implementation.

## 5. User-visible state matrix

| State | Visible evidence | Safe next action | Explicit non-claim |
| --- | --- | --- | --- |
| No history sources | Source paths/status and `No activity recorded` | Refresh or continue printing; open setup/help if needed | Empty files do not prove that no physical label was printed elsewhere. |
| Loading/refreshing | Source-by-source busy state and last successful refresh | Wait or cancel if supported | Do not replace stale rows with an empty success state. |
| Mixed sources available | Row provenance (`state store`, `operation JSONL`, `print CSV`) and timestamp basis | Filter or select a row | Different sources are not automatically one-to-one records. |
| Filter no match | Query/filter summary and clear action | Clear or adjust filters | No match is not evidence that the job never existed. |
| Selected accepted/spool row | Job ID, queue/spool observation, outcome evidence and manifest fingerprint | Open detail or Print Center | Spool acceptance/queue completion is not physical-output verification. |
| Selected failed/unknown row | Durable reason, source diagnostics and next safe action | Inspect, reconcile or acknowledge/void | Do not rewrite an uncertain/fault event as success. |
| Corrupt/incomplete event tail | Warning that valid prefix is available but append/action is blocked | Repair/archive the event log through an explicit support path | Never enable reprint while the state store is damaged. |
| Reprint not requested | Eligibility explanation and Request action if non-terminal | Request linked reprint explicitly | Request creates lineage only; no preparation or dispatch. |
| Reprint requested | Parent/child IDs, actor, reason and immutable manifest fingerprint | Review child and approve or stop | Approval is not dispatch or physical completion. |
| Reprint approval blocked | Exact mismatch fields (counts, queue, DPI, design/data/output hashes) | Refresh current template/data or cancel the plan | Never offer a force/ignore-mismatch bypass. |
| Reprint approved | Approved child, manifest validity and guarded Preview/Dispatch actions | Preview, then explicit dispatch through existing service | Dispatch is allowed only after a fresh exact-manifest comparison. |
| Support export running/failed | Redacted bundle path/fingerprint or actionable failure | Retry export explicitly; preserve lineage | Export contains no raw label payload and does not prove physical output. |

## 6. WPF mapping and acceptance contract

| History region | Existing/proposed WPF owner | Stable mapping requirement |
| --- | --- | --- |
| Source refresh/status | New History view model over `PrintJobStateStore`, `PrintOperationLogService` and `PrintLogService` | Stable `History.Refresh` action; preserve per-source diagnostics and last-refresh timestamps. |
| Filter bar | New pure filter policy/read model | Stable names for date/module/queue/action/status; filtering must not mutate logs or state. |
| Activity table | New read-only `DataGrid` or deliberate extension of Print Center | Stable `JobId`/lineage identity; no raw row payloads or ambiguous display-only keys. |
| Detail drawer | `PrintJobStateStore.ReadEventsAsync`, operation entry projection and CSV detail lookup | Show provenance, hashes, queue evidence, actor/reason and related jobs; preserve unknown values. |
| Recovery/reprint actions | Existing `PrintCenterWindow` and `MainViewModel` operator-action/dispatch services | One action owner; no duplicate dispatch stack; keep explicit Request → Approve → Prepare → Dispatch order. |
| Support export | Existing `PrintCenterWindow.BuildSupportEvidence` / `PrintSupportEvidenceContract` | Reuse redaction and fingerprint contract; export is read-only with respect to lineage. |

Protected behavior check:

- [ ] No Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or designer/print parity is changed.
- [ ] History filtering/detail/reprint actions never mutate label geometry, data bindings or authored object data.
- [ ] The existing no-auto-retry, explicit queue identity, exact-manifest and physical-verification contracts remain intact.
- [ ] Any future History/reprint contract change is documented with matching regression coverage before a UI button changes behavior.

## 7. Runtime and regression gates

Before calling CC-P5 implemented or verified, attach:

- runtime screenshots and/or UI Automation at `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception);
- source fixtures for no files, empty files, mixed CSV/operation/state records, stale timestamps, malformed JSON tail and permission/read failures;
- filter fixtures for date/timezone, module/print mode, queue, lifecycle/action, status and no-match behavior;
- detail fixtures proving lineage ordering, hash/manifest validity, related reprint IDs, actor/reason and redacted fields;
- reprint fixtures covering request, exact-manifest approval, mismatch blocking, guarded preview and explicit dispatch; no force path and no automatic retry;
- support-export success/failure and redaction/fingerprint checks;
- existing CSV/JSONL/state-store/recovery/operator-action/Print Center tests plus build, unit-test and application-runner output copied into the owning clean checkpoint;
- explicit non-claims for Control Center server history, multi-user identity, cloud retention, physical verifier/grade and physical-label output.

Suggested commands:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 8. Owner decisions before UI implementation

1. Choose the host: a new `HistoryWindow`, a History tab/sibling of Print Center, or a CC-P1 view.
2. Define the canonical read-model schema and source precedence when CSV, operation JSONL and state-store events disagree.
3. Approve local time-zone, retention, privacy/redaction and corrupt-tail copy.
4. Assign stable AutomationIds and runtime screenshot/UI Automation ownership for filter, table, detail, reprint and export states.
5. Keep existing Print Center/reprint services as the only action owner; do not add a row-level dispatch shortcut.
6. Preserve the protected Text/TextBox contract and all current exact-manifest/no-auto-retry gates.

Until these decisions and runtime evidence exist, this document is a handoff—not a claim that History or controlled reprint UX is shipped or design-verified.
