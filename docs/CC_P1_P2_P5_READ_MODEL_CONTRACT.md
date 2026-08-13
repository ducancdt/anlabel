# ANLAbel — CC-P1/P2/P5 local evidence read-model contract

**Status:** design contract for review; documentation-only (2026-08-13)
**Host decision:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`

This document defines how the first Control Center surfaces may project existing local evidence. It does not create a read-model class, merge files at runtime, change print behavior, or authorize a UI. The source services remain authoritative until an implementation owner adopts this contract with tests.

## 1. Source authority map

ANLAbel currently has four different evidence shapes. “Unified” means a read-only projection with provenance, not one flattened success flag.

| Source | Current type/service | Authority | Timestamp basis | Failure rule |
| --- | --- | --- | --- | --- |
| Durable lifecycle | `PrintJobStateStore` → `PrintJobStateEvent` / `PrintJobRecoverySnapshot` | Authoritative for valid job lineage, latest lifecycle state, sequence/hash chain, operator action, related job and manifest fingerprint | `TimestampUtc` | Valid prefix may be shown; corrupt/incomplete tail is a visible diagnostic and blocks unsafe append/reprint action. |
| Job operation trace | `PrintOperationLogService` → `PrintOperationLogEntry` | Supplemental job-level outcome, spool polling, output-contract/design/resource fingerprints and redacted support fingerprint | `TimestampLocal`; `SpoolStatusObservedAtUtc` when present | Best-effort write failure or missing row must be shown as unavailable supplemental evidence, never as proof that no job exists. |
| Human label history | `PrintLogService` → `PrintLogEntry` in append-only `print-history.csv` | Per-label/detail and user export source; not lifecycle authority and not a one-to-one job ledger | `PrintedAt` is local `DateTime` without an offset | Missing/empty/parse-failed CSV is a source diagnostic; do not synthesize a job outcome from row count. |
| Current named queue | `IPrinterQueueLookup` → `PrinterQueueLookupResult`; `PrinterDiscoveryService` → `PrinterInfo` | Live saved-queue identity/availability and installed queue discovery for P1/P2 | Refresh completion time supplied by the future projection; source result has no timestamp today | Missing, renamed, inaccessible or canonical-mismatch queue remains unavailable; never substitute the Windows default. |

Activation/entitlement is a separate P1 local service (`TrialLicenseService`/`ActivationWindow`) and is not part of the P5 history join. It may be displayed as a separate card with its own source and refresh status.

## 2. Canonical projection fields

The future projection should expose these fields even when a value is unknown. Empty strings must not be silently converted to `Success`, `Completed`, `Online` or `Physical output verified`.

| Projection field | Source precedence | Display requirement |
| --- | --- | --- |
| `RecordId` / `JobId` | State event `JobId`, then operation entry `JobId`; CSV has no durable job key today | Use a stable job identity for job-level rows; label-only rows are explicitly `CsvLabelRecord` detail. |
| `Sequence` / lineage | State event `Sequence`, `PreviousHash`, `IntegrityHash` | Show sequence and integrity status in detail; never expose a display-only row index as lineage. |
| `LifecycleState` | Latest valid state event `To` | Preserve `Created`, `Preparing`, `PreflightPassed`, `Dispatching`, `SpoolAccepted`, `QueueObserved`, `Unknown`, `Failed`, `Cancelled`, `Completed`; do not invent “Printed” as a synonym. |
| `OperatorAction` | State event `OperatorAction` | Keep `Acknowledge`, `Void`, `ReprintRequested`, `ReprintApproved` separate from lifecycle. |
| `RelatedJobId` / `Actor` | State event, operation entry supplemental fields | Show parent/child linkage and actor/reason; blank actor is `Unknown`, not a system user. |
| `CanonicalTimestamp` | State `TimestampUtc`; otherwise operation spool observation UTC; otherwise operation local timestamp with an explicit local basis; CSV local time only | Show source-specific timestamp and basis. Do not compare unspecified local CSV time to UTC without a documented conversion. |
| `PrinterName` | State event, operation entry, then current saved queue request/canonical result | Preserve requested vs canonical names when they differ; mismatch is diagnostic, not a rename. |
| `SpoolJobId` / `QueueState` | State event and operation entry | Mark observations as job-scoped. Queue/spool acceptance is not physical output. |
| `ManifestFingerprint` | Valid state manifest/fingerprint; operation entry supplemental | A missing or invalid fingerprint makes controlled reprint ineligible. Never display a stale fingerprint as current. |
| `DocumentHash`, `SceneHash`, `OutputContractHash` | State event, then operation entry | Expose drift/missing values in detail; do not hide mismatch behind a green status. |
| `Outcome` / `OutcomeEvidence` | Operation entry supplemental, correlated by `JobId` | Show source and evidence text; do not let a best-effort outcome overwrite lifecycle state. |
| `PhysicalOutputVerified` | Accepted state verification evidence only | `false`/unknown for queue observations, CSV rows and ordinary operation outcomes. “Completed” requires the existing physical-verifier contract. |
| `SourceAvailability` / `Diagnostics` | Store diagnostics, queue lookup error, operation-log availability and CSV parse/read status | Keep per-source diagnostics; one failed source must not erase valid evidence from another. |

## 3. Join and precedence rules

1. **Load the durable state prefix first.** Replay `PrintJobStateStore`; retain the latest valid event per `JobId` and all store diagnostics.
2. **Correlate operation JSONL by `JobId`.** Merge supplemental fields only when the identity matches. If several entries exist, retain the event list/detail rather than silently selecting an arbitrary row.
3. **Link reprint lineage by `RelatedJobId`.** A request or approval is an operator event in the same state store; it is not a new dispatch unless a later explicit dispatch event exists.
4. **Keep CSV at label granularity.** Attach CSV rows only as detail/export evidence using available template/printer/time context. Do not fabricate a job key or count CSV rows as completed jobs.
5. **Resolve current queue separately.** P1/P2 may refresh the saved queue with `IPrinterQueueLookup`; this live result can update queue-health cards but must not rewrite historical job state.
6. **Preserve conflicts.** When sources disagree, show the values and source names, mark the projection `Conflict`/`Unknown`, and route the operator to detail or Print Center. Do not choose the most optimistic value.
7. **Fail closed on integrity diagnostics.** A corrupt state tail blocks append/reprint actions even when the valid prefix can populate a read-only table.
8. **Never infer physical output.** Queue status, spool completion, CSV presence, operation `Success` or Figma sample rows do not satisfy physical verification.

## 4. Surface-specific projection

| Surface | Allowed projection | Must not claim |
| --- | --- | --- |
| P1 Operations Overview | Counts of non-terminal recovery candidates, store diagnostics, current saved-queue availability/age, recent terminal software events and separate local activation status; each card includes source and refresh basis | 24-hour/server fleet totals, license seats from Figma, physical output, automatic retry or a hidden queue fallback |
| P2 Print Queue Console | Installed queue rows (`PrinterInfo`), canonical saved-queue lookup, queue/job observations with state taxonomy and timestamps; unknown/error rows remain visible | Printer-level health from one job observation, licensed-seat enforcement, printer-native completion or command capability without a new contract |
| P5 History | Job-level activity table/detail over state events plus supplemental operation entries; linked per-label CSV detail/export; explicit Request → Approve → Prepare → Dispatch deep-link | A flattened all-success counter, raw label payloads in the table, row-level implicit dispatch, or physical completion from software evidence |

## 5. Figma routing for this contract

Existing read-only nodes are sufficient for the information-architecture review:

| Surface | Figma nodes | Use | State still requiring runtime evidence |
| --- | --- | --- | --- |
| P1 | Overview `2:2`, cards `2:16`, `2:20`, `2:25`, `2:30` | Card grouping and density | Source/refresh/error copy and target-scale WPF behavior |
| P2 | Printers `2:37`, filter rail `2:51`, main pane `2:72` | Filter/table proportions | Queue-vs-job fields, unavailable queue, stale refresh and keyboard path |
| P5 | History `3:85`, filters `3:99`, table `3:101`, detail note `3:109` | Activity/filter/detail hierarchy | Provenance, corrupt tail, privacy, timezone and reprint eligibility |

No new Figma node is requested by this contract. If a later implementation needs a concrete missing state, follow the [Figma escalation protocol](figma-ui-handoff-template.md#figma-escalation-protocol): state the question, inspect metadata read-only, record the smallest node and then close with runtime evidence.

## 6. Implementation and verification gate

Before a read-model/UI implementation is called ready:

- pure tests cover source joins, precedence, timestamp basis, conflict/unknown behavior and corrupt-tail blocking;
- fixtures cover absent/empty/malformed CSV, missing operation JSONL, valid state prefix plus corrupt tail, duplicate `JobId`, missing manifest and queue canonical mismatch;
- P1/P2/P5 use the same `JobId`, queue identity and Print Center deep-link owner;
- table/card fields expose source, last refresh and diagnostic copy without raw label payload leakage;
- runtime screenshots/UI Automation cover `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus/scroll and disabled/unimplemented actions;
- relevant handoffs, [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md), continuation/checkpoint and any approved contract changes stay synchronized;
- protected Text/TextBox behavior remains untouched.

Until these gates close, this file is a review contract, not a claim that a P1/P2/P5 read model or UI exists.
