# ANLAbel — CC-P5 History + controlled reprint UI/UX spec

**Status:** staged read-only History host implemented; runtime scale smoke remains pending (2026-08-13)
**Host decision:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Evidence contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Handoff:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md)
**Existing recovery action owner packet:** [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md)
**Concrete History owner packet:** [`CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md`](CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, History `3:85`

This spec maps the Figma History shell to a local, read-only projection over durable job state, supplemental operation JSONL and per-label CSV detail. The staged `PrintHistoryWindow` implements that projection without exposing raw label payloads or adding a second reprint/dispatch owner.

The existing WPF `PrintCenterWindow` remains the recovery/reprint action owner; its source-backed state and action gates are recorded in [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md). The future History surface must deep-link to that owner rather than copy its buttons or dispatch path.

The remaining History/read-model expansion, privacy and runtime-scale closure decisions are recorded in [`CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md`](CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md). The implemented M1 host remains limited to the stated local, provenance-preserving projection.

## 1. Operator outcome

The first History surface should let an operator:

1. answer what was submitted, for which template/queue, and what durable evidence exists;
2. distinguish lifecycle state, spool observation, supplemental operation outcome, per-label detail and accepted physical-verifier evidence;
3. inspect job lineage, source provenance, timestamps, diagnostics and immutable manifest fingerprints;
4. understand whether a linked reprint is eligible, blocked or already requested;
5. reach the existing Print Center / preview / support-export owners through explicit links, never through a row-level dispatch shortcut.

"Unified" means a read-only projection with provenance. It is not a flattened success flag and it must not hide an unavailable source behind an empty table.

## 2. Figma node map (read-only)

Metadata for `3:85` was rechecked read-only on 2026-08-13. The node names and bounds below are the visual reference; the ANLAbel role is deliberately narrower.

| Figma node | Metadata name / bounds | ANLAbel role | Boundary |
| --- | --- | --- | --- |
| `3:85` | `CC / History`, `1280 x 800` | Activity/filter/detail density reference | Not a WPF size mandate, server-history claim or retention policy. |
| `3:86` | `Frame`, `(0,0)`, `1280 x 48` | Optional host chrome | No sign-out, help or web identity without a local owner. |
| `3:89` | `Frame`, `(0,48)`, `1280 x 40` | Primary navigation reference | Show only local modules that exist. |
| `3:99` | `Frame`, `(16,104)`, `1248 x 56` | Filter bar | Every filter needs a local source, timezone basis and unknown rule. |
| `3:101` | `Frame`, `(16,176)`, `1248 x 600` | Activity table/detail owner | Read-only activity first; selection must not mutate logs. |
| `3:102`/`3:103` | Header `1248 x 32`; Submitted, Type, Module, Workstation, User, Status, Details | Column-language reference | Do not populate Workstation/User from sample copy or infer a module identity. |
| `3:104`–`3:108` | Example activity rows | Density/empty-state reference only | Example users, workstations, dates and statuses are not runtime fixtures. |
| `3:109` | Activity details / Reprint / Errors tabs note | Deep-link/detail affordance | Metadata contains no concrete local child states; the implemented host keeps Print Center as the action owner. |

No new Figma node is required for this spec. If a missing state needs design evidence, follow the [Figma escalation protocol](figma-ui-handoff-template.md#figma-escalation-protocol) and request the smallest state-specific reference.

## 3. Source-to-record contract

| Projection field | Current source and precedence | Display rule |
| --- | --- | --- |
| `JobId` / `RecordId` | Valid `PrintJobStateEvent.JobId`, then `PrintOperationLogEntry.JobId`; `PrintLogEntry` has no durable job key | Use a stable job identity for job rows. Label-only records are explicitly `CsvLabelRecord`, never fabricated jobs. |
| `LifecycleState` | Latest valid state-store event `To` | Preserve `Created`, `Preparing`, `PreflightPassed`, `Dispatching`, `SpoolAccepted`, `QueueObserved`, `Unknown`, `Failed`, `Cancelled`, `Completed`; `Completed` requires accepted physical-verifier evidence. |
| `SubmittedAt` / `ObservedAt` | State `TimestampUtc`; then operation `SpoolStatusObservedAtUtc`; then operation `TimestampLocal`; CSV `PrintedAt` is local with no offset | Show timestamp and basis (`UTC`, explicit local, or unknown). Do not compare CSV local time to UTC without a documented conversion. |
| `Template` / `PrinterName` | Operation entry; state event; current queue lookup only as separate live context | Preserve requested/canonical queue values and source. Current lookup must not rewrite historical state. |
| `PrintMode` / `Outcome` | Operation JSONL; CSV is per-label supplemental detail | Show `OutcomeEvidence` and source. `Success` never overwrites lifecycle or physical-verification state. |
| `OperatorAction` / `RelatedJobId` / `Actor` | State event | Keep Acknowledge, Void, ReprintRequested and ReprintApproved separate from lifecycle; blank actor is `Unknown`. |
| `SpoolJobId` / `QueueState` | State event and operation entry | Mark as job-scoped spool observation. `Completed`, `NotFound` or timeout never proves physical output. |
| `ManifestFingerprint` | Valid state manifest/fingerprint; operation entry supplemental | Show validity and mismatch diagnostics. Missing/invalid fingerprint blocks controlled reprint. |
| `DocumentHash` / `SceneHash` / `OutputContractHash` | State event, then operation entry | Detail-only evidence for drift and exact-manifest review; never hide a mismatch behind green status. |
| `LabelDetail` | `PrintLogService` → `PrintLogEntry` in append-only CSV | Link to per-label detail/export with redaction/privacy rules; do not put raw `LabelContent` or `RowData` in the activity table. |
| `SourceAvailability` / `Diagnostics` | State replay diagnostics, operation-log read status, CSV parse/read status | Keep source badges and last-refresh/error text. One failed source must not erase valid evidence from another. |
| `PhysicalOutputVerified` | Accepted `PhysicalOutputVerificationEvidence` bound to the manifest | `false`/`Unknown` for queue observations, CSV rows and ordinary operation outcomes. |

## 4. Host-neutral wireframe

Keep this content order whether the host is a new History window, a Print Center sibling/tab or a P1 view:

```text
[History context: Refresh | source status | last refresh | timezone basis]

[Date range | module/print mode | queue | lifecycle/status | operator action | Search | Clear]

[Activity table: Time | Job/Record | Type | Template/Queue | Lifecycle | Action | Evidence | Source]

[Selected detail]
[Lineage timeline | source cards: state store | operation JSONL | CSV detail]
[Manifest/hashes | queue/spool evidence | diagnostics | related job]

[Reprint eligibility: Request -> Approve -> Prepare -> Dispatch]
[Open Print Center | Open approved preview | Export support evidence | Open CSV detail]
```

The activity table is read-only. Selection, filtering, CSV detail and support export do not mutate the event store. `Request`, `Approve`, `Prepare` and `Dispatch` are explicit stages; only the existing Print Center/MainViewModel services may own the action path.

## 5. State and provenance matrix

| State | Visible evidence | Safe next action | Explicit non-claim |
| --- | --- | --- | --- |
| `NoSources` | Per-source paths/status and `No activity recorded` | Refresh or open setup/help | Empty files do not prove that no label was printed elsewhere. |
| `Loading` | Busy state plus last successful refresh | Wait/cancel if supported | Do not replace stale rows with an empty success state. |
| `MixedSources` | Source badges, row provenance and timestamp basis | Filter or select a row | CSV, operation JSONL and state events are not automatically one-to-one. |
| `FilterNoMatch` | Filter summary and Clear action | Clear or adjust filters | No match is not evidence that a job never existed. |
| `SelectedActive` | Job ID, lifecycle, queue/spool observation and source diagnostics | Inspect detail or reconcile in Print Center | Queue/spool acceptance is not physical output. |
| `SelectedFailedOrUnknown` | Durable reason, conflict/unknown copy and next safe action | Inspect, reconcile, acknowledge or void | Never rewrite an uncertain/fault event as success. |
| `SelectedCompleted` | Accepted verifier evidence, manifest binding and source | Inspect/export only | `Completed` is not a generic synonym for queue completion. |
| `CorruptTail` | Valid prefix plus visible repair warning | Repair/archive through explicit support path | Never enable append, reprint or approval while the state store is damaged. |
| `ReprintNotRequested` | Eligibility explanation and Request link when allowed | Request a linked child explicitly | Request does not prepare or dispatch. |
| `ReprintRequested` | Parent/child IDs, actor, reason and captured manifest fingerprint | Review child and approve or stop | Approval is not dispatch or physical completion. |
| `ApprovalBlocked` | Exact mismatch/missing-manifest fields | Refresh current inputs or cancel | No force/ignore-mismatch bypass. |
| `ReprintApproved` | Approved child, immutable-manifest validity and guarded Preview/Dispatch links | Preview, then explicit dispatch through existing owner | History never dispatches from a list row. |
| `SupportExportFailed` | Redacted export error and retry action | Retry export explicitly | Export failure does not alter lineage or prove output. |

## 6. Filters and responsive behavior

M1 filters may include exact `JobId`, template, printer/queue, spool ID, manifest fingerprint, date/time with an explicit timezone basis, print mode/module when supplied by the operation trace, lifecycle state, operator action, outcome and source availability. Figma `User` and `Workstation` filters remain unavailable unless a named local source and privacy policy are approved; `OperatorActor` is not automatically a workstation or multi-user identity.

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May preserve the `1248 x 56` filter bar and `1248 x 600` table proportions as a visual reference. | Table owns row scrolling; detail remains reachable without hiding source diagnostics. |
| `1024 x 600` | Wrap filters into two rows or a filter drawer; table columns prioritize time, JobId, lifecycle, queue and evidence; detail stacks below or opens as a bounded pane. | Keyboard order: refresh/source status → filters → table → detail → explicit links; selection survives refresh when identity remains. |
| `100%`, `125%`, `150%` | Reflow or clip only inside declared owners; no page-level horizontal scroll and no blind Figma scaling. | Capture screenshot/UI Automation at every scale; record environment exceptions. |

## 7. Proposed automation vocabulary

Proposals only; final IDs require the host decision and runtime UI Automation evidence.

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P5.History.Root` | `Print history` |
| Refresh | `CC.P5.History.Refresh` | `Refresh print history` |
| Source status | `CC.P5.History.SourceStatus` | `History source status` |
| Filter bar | `CC.P5.History.Filters` | `History filters` |
| Search | `CC.P5.History.Search` | `Search print history` |
| Activity table | `CC.P5.History.ActivityTable` | `Print activity` |
| Selected detail | `CC.P5.History.Detail` | `Selected job evidence` |
| Lineage | `CC.P5.History.Lineage` | `Job lineage` |
| Reprint eligibility | `CC.P5.History.ReprintEligibility` | `Reprint eligibility` |
| Request | `CC.P5.History.RequestReprint` | `Request linked reprint` |
| Approve | `CC.P5.History.ApproveReprint` | `Approve linked reprint` |
| Print Center link | `CC.P5.History.OpenPrintCenter` | `Open Print Center` |
| Approved preview link | `CC.P5.History.OpenApprovedPreview` | `Open approved preview` |
| Support export | `CC.P5.History.ExportSupportEvidence` | `Export redacted support evidence` |
| CSV detail link | `CC.P5.History.OpenCsvDetail` | `Open label history detail` |

## 8. Acceptance gate

Before implementation review closes P5:

- the host packet chooses one host and reuses P1/P2 owners plus the existing Print Center action owner;
- the read model loads the valid state prefix first, correlates operation JSONL by `JobId`, keeps CSV at label granularity and preserves conflicts/diagnostics;
- fixtures cover no sources, empty/malformed CSV, missing operation JSONL, mixed sources, duplicate/unknown IDs, stale timestamps, valid prefix plus corrupt tail, missing/invalid manifest and queue identity mismatch;
- the table/detail visibly separate lifecycle, spool observation, operation outcome, CSV detail and accepted physical verification;
- raw label payloads remain out of the activity table and support export follows the existing redaction/fingerprint contract;
- reprint fixtures cover Request, linked child, exact-manifest approval, mismatch blocking, guarded preview and explicit dispatch, with no force path or automatic retry;
- runtime screenshot/UI Automation covers `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus and scroll ownership;
- Figma sample users, workstations, dates and statuses never become runtime fixtures without local evidence;
- protected Text/TextBox behavior and print/manifest contracts remain untouched.

Until these gates close, this file is a UI/UX specification, not a shipped History browser or controlled-reprint implementation.
