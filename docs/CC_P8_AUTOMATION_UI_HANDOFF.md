# ANLAbel — CC-P8 Applications / Automation UI handoff

**Status:** deferred roadmap slice; design/architecture review only (2026-08-13)
**Owning roadmap:** [`MASTER_PLAN.md`](../MASTER_PLAN.md), section `3. Applications / Automation`
**Prerequisites:** CC-P1 Operations Overview, CC-P2 Print Queue, CC-P5 History, and CC-P4 document policy evidence must remain stable before an automation host is implemented.
**Related handoffs:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md), [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md), [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md), [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md) remains authoritative for Text/TextBox behavior.
**Program index / host gate:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md), sections 2 and 5
**UI/UX content spec:** [`CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md`](CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md)

CC-P8 remains deferred until P1/P2/P5 evidence paths and P4 document policy are stable. Any future host must call the existing preflight -> manifest -> queue spine and write History; it cannot bypass the shared navigation/owner gate or introduce a second dispatch path.

This handoff records the smallest safe direction for future local automation. It does not add a trigger runner, web form, cloud integration, background service, or Figma edit. Automation must call the same validated preflight, immutable manifest, explicit queue, and History path as manual Quick Print; it must never become a silent second dispatch stack.

## 1. Product boundary

The roadmap describes NiceLabel Web Applications and Automation Manager: shared web forms, login/printer restrictions, trigger list, start/stop, configuration and filtered automation logs. ANLAbel is currently a desktop-only WPF application:

- The source has a `FileSystemWatcher` for linked Excel freshness notices. It marks data stale and waits for an explicit user update; it is not a generic print trigger host.
- Manual printing already resolves an effective printer plan, runs preflight, creates a manifest and records durable job transitions before dispatch. This is the only acceptable print spine for a future trigger.
- Local print/data operation logs exist, but the print operation trace is explicitly best-effort. The hash-chained `PrintJobStateStore` is stronger job evidence, yet neither store is a trigger configuration or exactly-once delivery ledger.
- No trigger registry, folder claim/quarantine protocol, TCP listener, background lifecycle service, web application designer, login provider, cloud integration or automation-specific UI exists.

CC-P8 therefore starts as **design-only trigger contract → one local file-drop trigger**. Multi-user web applications, remote login, cloud sync, arbitrary TCP listeners, and unattended production printing remain separate product/security decisions.

## 2. Existing source evidence

| Surface/evidence | Current behavior | CC-P8 implication |
| --- | --- | --- |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs) `StartWatchingExcelFile` / `OnLinkedExcelFileChanged` | Watches one linked Excel file, debounces events, and raises a stale-data notice without silently reloading. Failures to watch are swallowed as a convenience-path failure. | Do not reuse this watcher as a print trigger. A trigger needs ownership, debounce/idempotency, file-lock handling, lifecycle state, cancellation and a durable outcome. |
| [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs) `CreateEffectivePlanAsync`, `ValidateRowsAsync`, `PrintRowsWithResultAsync` | Manual/preview paths use effective printer plans, software preflight, immutable snapshots and explicit queue dispatch. | Automation must invoke these same boundaries and revalidate immediately before dispatch; no trigger may call a low-level renderer or printer API directly. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs) print batch path | Records `Created → Preparing → PreflightPassed → Dispatching` job transitions, carries manifest/output hashes, and resolves spool identity separately. | Trigger identity, configuration fingerprint and source-file fingerprint must be attached to the same job manifest/history projection. Physical completion remains unverified unless the existing verifier evidence says otherwise. |
| [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs) | Writes local JSONL per-print entries; I/O failures are swallowed so logging never blocks the print job. | It can be a convenience trace, not the sole automation audit. A trigger failure/success state needs a durable store or explicit non-claim if logging fails. |
| [`DataOperationLogService.cs`](../src/ANLAbel.Data/DataLogs/DataOperationLogService.cs) | Writes best-effort JSONL data-source operation entries; it is not a trigger log. | Keep data freshness events separate from trigger lifecycle and print outcomes. |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs) | Append-only per-job transitions are sequenced and chained to previous/integrity hashes; replay stops at invalid events. | Reuse the integrity discipline for trigger outcomes only after deciding trigger IDs, delivery semantics, retention and recovery. Do not silently mix trigger configuration events into job state. |
| [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs) | Reprint request/approval is explicit, exact-manifest guarded and never auto-dispatches. | Automation must not use Request/Approve reprint as a substitute for trigger authorization; a trigger-generated job still needs a distinct source/trigger identity. |
| Current WPF UI | `MainWindow`/`PrintPreviewWindow`/`PrintCenterWindow` cover authoring, preview and recovery; no Automation window or trigger controls are shipped. | Pick one future host and stable automation names before XAML work. Do not add disabled-looking research buttons to current surfaces. |

## 3. Figma reference and routing

Use the existing [ANLAbel Control Center Figma file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) as research input. Read-only metadata was checked on 2026-08-13; no Figma node was edited or duplicated.

### Applications shell (`7:88`)

Frame `7:88` (`CC / Applications — Web Apps`, `1280 × 800`) is a web-application research shell, not an automation console:

| Node | Measured reference | WPF/design boundary |
| --- | --- | --- |
| `7:109`–`7:123` | `AppSidebar` at `(0,92)`, `220 × 246`; Web Applications, Cloud Integrations, Automation, All Triggers, With errors, Started, Stopped. | Navigation vocabulary only. It does not prove that a local service, trigger registry, or status taxonomy exists. |
| `7:124` | `AppMain` at `(220,92)`, `1060 × 380`. | Main frame is shorter than the workflow frame and is not a trigger-detail layout. Define an intentional scroll owner for any future list/log view. |
| `7:125`–`7:131` | Header `1020 × 100`; “Web Applications”, “+ New web application”, “Share / configure”. | Login policy, sharing and remote users are out of scope for the first local trigger slice. Do not map these labels to local WPF capabilities. |
| `7:132`–`7:156` | Three sample web-app cards with user/printer counts and Open actions. | Sample counts are research data, not local license/printer evidence. They must not be copied into Operations Overview or automation status. |
| `7:157`–`7:161` | Share settings: enforce login, restrict printers, record printing to History, file/database connections. | These are future product/security decisions; only the History-recording invariant is compatible with the local trigger direction. |

There is no dedicated trigger list, trigger configuration, run detail, duplicate-file, queue-blocked, retry, or log-filter state in `7:88`. A new state-specific Figma reference is only justified after the owner selects the first trigger task.

### History routing (`3:101`)

The Control Center History shell has activity frame `3:101` at `(16,176)`, `1248 × 600`, with row fields for submitted time, type, module, workstation, user and status (`3:102`–`3:109`). Route automation outcomes through the P5 History read model; use `trigger identity` as an explicit provenance field only after its privacy/retention semantics are approved. The sample “Automation” row is not evidence that ANLAbel has an Automation module.

## 4. Proposed trigger contract for review

This is a design proposal, not implementation authorization.

### First vertical slice: one local file-drop trigger

1. A configured watch root and file pattern are explicit, displayed and validated before start.
2. A detected file is assigned a deterministic trigger event ID/fingerprint and moved or marked through an explicit claim protocol; repeated watcher events must not create duplicate jobs.
3. The trigger loads a supported input, resolves the selected document/data source, creates the same immutable manifest as manual print, runs effective-plan preflight and dispatches only through the existing queue service.
4. Every accepted, blocked, failed, canceled or quarantined event records trigger identity, source fingerprint, document/manifest hashes, queue identity and reason in the chosen History/job projections.
5. No automatic retry or “print anyway” bypass exists in M1. Recovery is visible and operator-directed.

### Later trigger types

| Type | Status | Required separate decision |
| --- | --- | --- |
| File/folder drop | Candidate M1 | Claim/quarantine, duplicate policy, file-lock timeout, archive/error paths, and source schema. |
| TCP trigger | Deferred | Listener binding, authentication, framing, replay protection, rate limits, shutdown and threat model. |
| Cloud/web form | Deferred / separate product | Login, printer restrictions, tenancy, data egress, server hosting, audit identity and privacy. |
| Excel freshness watcher | Existing data feature | Remains a stale-data notice; never silently becomes print automation. |

## 5. Lifecycle and safety contract

| State | Meaning | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| `Stopped` | Trigger is configured but not processing. | Validate configuration; Start explicitly. | No file is consumed while stopped. |
| `Starting` | Root/pattern/source/queue checks are running. | Cancel or wait. | Do not claim Started until every required dependency is ready. |
| `Running` | Watcher/service is active and reporting health. | Stop; inspect recent events. | Watcher errors move to Error/Stopped, not silent running. |
| `Stopping` | New claims are blocked while in-flight work drains/cancels. | Wait or force-stop only after owner decision. | Do not abandon a claimed file without a durable outcome. |
| `Error` | Configuration, watcher, source, preflight, queue or audit failure. | Inspect diagnostic; repair; restart explicitly. | No retry storm or hidden dispatch. |
| `With errors` | Filtered view of event/job failures. | Open detail/quarantine/recovery path. | Preserve source and manifest evidence. |
| `File detected` | Event observed but not yet claimed. | Claim after debounce and validation. | Duplicate events collapse to one event ID/fingerprint. |
| `Claimed / preparing` | Source ownership established; manifest/preflight running. | Cancel only through explicit lifecycle. | No second claim or concurrent dispatch for same fingerprint. |
| `Blocked` | Preflight, document workflow policy, queue or output contract rejected the job. | Show exact reason and safe repair. | No force-print or automatic fallback queue. |
| `Dispatched / observed` | Existing print job path accepted or observed a queue identity. | Open History/Print Center. | Do not label physical output complete without verifier evidence. |
| `Quarantined` | Source is malformed, unsupported, duplicate or unsafe. | Inspect/move/delete only with confirmation. | Never silently discard the source. |

## 6. Configuration and provenance fields

Before implementation, approve the minimum persisted configuration:

- stable trigger ID/name and enabled/disabled state;
- local root/pattern, recursive policy, claim/archive/error directories and file-lock timeout;
- source parser/data binding and target document path/ID/revision hash;
- printer queue identity or an explicit “operator chooses queue” mode (no implicit Windows default);
- debounce/deduplication policy and event fingerprint algorithm;
- lifecycle/start/stop timestamps, local operator identity wording, configuration version/hash;
- retention and redaction rules for source paths, payloads, usernames and History exports.

The job manifest should carry trigger ID and source fingerprint alongside the existing document/data/scene/output-contract hashes. A trigger configuration change must invalidate any prepared plan created under the old configuration. Do not store raw credentials or source payloads in a UI log by default.

## 7. Proposed UI slices

### M0 — Read-only trigger model and health

1. Define trigger identity, status taxonomy, configuration validity, event/job linkage and error severity.
2. Project existing manual job/data logs into a read-only “automation evidence” view only if provenance is explicit; do not manufacture trigger rows from unrelated logs.
3. Select host: a sibling `AutomationWindow`, a Print Center-adjacent panel, or a future Control Center shell. Keep one lifecycle owner.

### M1 — File-drop trigger

1. Configure/validate one local root and pattern; show permissions, queue and document readiness.
2. Start/Stop with explicit confirmation and visible lifecycle; show recent event list, quarantine and blocked reasons.
3. Route accepted source through the manual manifest/preflight/dispatch path and P5 History; no second print code path.

### M2 — Operational controls and filters

1. Add With errors, Started and Stopped filters only after status transitions are durable.
2. Provide retry only as a new explicit event after the source/manifest mismatch and reason are reviewed.
3. Add export/redaction through the existing History/support-bundle policy.

### M3 — TCP/web decision

Do not implement until a security/product decision covers authentication, replay, tenancy, printer policy, data egress, hosting and incident recovery. The Figma web-app shell alone is not authorization.

## 8. WPF and acceptance gates

| Gate | Evidence before implementation closure |
| --- | --- |
| Prerequisite gate | CC-P1/P2/P5 evidence paths and the CC-P4 document-print policy are stable; no automation work starts on unresolved dispatch semantics. |
| Host decision | One WPF host and one lifecycle/configuration owner are named; no parallel watcher/dispatch stack. |
| Scale/accessibility | Runtime screenshots or UI Automation at `1024 × 600`, `100%`, `125%`, `150%`; stable names for trigger list, status, Start, Stop, Configure, event detail, quarantine and retry/cancel. |
| Data safety | Duplicate, locked, malformed, changed-after-claim and audit-failure fixtures prove no silent loss or duplicate dispatch. |
| Print parity | Trigger and manual print produce the same effective plan/manifest/preflight/output-contract checks; trigger identity survives into History. |
| Recovery | Queue unavailable, driver refusal, source change, app restart and stop-during-dispatch leave an explicit durable outcome. |
| Security | File path traversal, untrusted input, credential exposure, listener binding and privilege boundaries are reviewed before TCP/web scope. |
| Regression | Unit/contract coverage for deduplication, claim lifecycle, configuration validation, trigger-to-manifest provenance, policy/preflight blocking and restart recovery; application UI regression for Start/Stop/error paths. |
| Figma | Record a state-specific node and measured dimensions after the first task is chosen. `7:88` and `3:101` are references, not runtime proof. |

## 9. Owner decisions needed

1. Is the first trigger explicitly a local file-drop watcher, or is CC-P8 design-only until a product owner names another task?
2. Which WPF host owns configuration/lifecycle and what is the stable AutomationId vocabulary?
3. What claim/deduplication semantics are required: move-to-processing, sidecar marker, hash ledger, or another protocol?
4. Which document/data binding and printer queue are allowed for an unattended trigger, and how is CC-P4 Published policy enforced?
5. What does “retry” mean, and who authorizes it after a blocked/mismatched manifest?
6. Which logs are durable source-of-truth versus best-effort traces, and how are source paths/payloads redacted in History/support bundles?
7. When, if ever, may a TCP listener or web form be reconsidered, and what authentication/tenancy evidence is required?

## 10. Decision

**Deferred; design-only.** Figma `7:88` provides a web-app/navigation shell and `3:101` provides a History density reference, but no trigger-detail state exists and the current source has no automation host. The next safe action is to approve a local file-drop contract, lifecycle/provenance semantics and one WPF host after CC-P1/P2/P5/P4 gates are stable. No trigger runner, web app, TCP listener, automatic retry, or Figma edit is authorized by this handoff.
