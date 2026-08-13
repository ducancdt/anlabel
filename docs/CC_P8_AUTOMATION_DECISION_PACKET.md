# CC-P8 local automation owner decision packet

**Status:** documentation-only owner gate; no trigger runner, background service, web application, TCP listener, unattended-print capability, new Figma node or Text/TextBox change is authorized by this packet (2026-08-13)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Handoff:** [`CC_P8_AUTOMATION_UI_HANDOFF.md`](CC_P8_AUTOMATION_UI_HANDOFF.md)
**Specification:** [`CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md`](CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md)
**Prerequisites:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md), [`CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md`](CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

CC-P8 is the deferred local automation slice. The smallest safe candidate is one explicitly configured local file-drop trigger that reuses the manual preflight -> immutable manifest -> explicit queue -> History spine. It must not turn the existing linked-Excel freshness watcher into a print trigger, create a second dispatch stack or imply NiceLabel web applications, login, cloud, TCP or unattended production printing.

```text
configured local root/pattern + explicit claim protocol
        -> source validation and trigger lifecycle
        -> existing document/policy/preflight/manifest/output checks
        -> explicit queue dispatch and P5 History provenance
        -> durable accepted/blocked/quarantined outcome
```

This packet is an owner-review gate. It does not add a trigger registry, watcher, lifecycle service, manifest fields, queue call, automatic retry or Figma edit. Existing Text/TextBox ownership, geometry, overflow and print parity remain protected.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. First trigger and prerequisites | Start with one local file-drop trigger only after P1/P2/P5 evidence owners and P4 document-print policy are stable. Keep Excel freshness as a stale-data notice, not a trigger. | Confirm file-drop as M1, supported file/schema and prerequisite closure criteria. |
| D2. Host/lifecycle owner | Choose one WPF host (`AutomationWindow`, Print Center-adjacent panel or approved CC shell) and one lifecycle/configuration service. Start/Stop/Configure and event detail have one action owner. | Name host, navigation route, lifecycle store and stable AutomationIds. |
| D3. Claim/deduplication protocol | Prefer an explicit move-to-processing/quarantine protocol or a durable fingerprint ledger with deterministic event ID, lock timeout and crash recovery. Repeated watcher events collapse to one claim; no at-least-once ambiguity is hidden. | Choose claim mechanism, source/archive/error directories, file-lock timeout, hash algorithm and duplicate policy. |
| D4. Configuration/provenance | Persist trigger ID/name, enabled state, root/pattern/recursive policy, parser/data binding, target document/revision, queue identity, configuration fingerprint, debounce, redaction and retention. Attach trigger/source/config fingerprints to the job manifest/history projection. | Approve fields, schema/version/migration, queue selection versus operator choice and privacy rules. |
| D5. Print spine and policy gate | Use existing effective-plan creation, P4 workflow policy, software preflight, `PrintJobManifest` and `DispatchRevalidationContract` immediately before dispatch. No low-level renderer/printer call, fallback queue or `Print anyway` path. | Confirm Published policy behavior, allowed unattended document/data sources and exact block copy. |
| D6. Lifecycle/recovery semantics | Use explicit `Stopped`, `Starting`, `Running`, `Stopping`, `Error` plus per-file Detected/Claimed/Blocked/Dispatched/Quarantined/ChangedAfterClaim. Persist enough outcome to recover after restart; no automatic retry in M1. | Approve stop-during-dispatch behavior, retry as a new event, quarantine/delete confirmation and audit failure handling. |
| D7. History/log/privacy ownership | P5 History remains the detail/reprint owner. `PrintJobStateStore` is durable job lineage; operation/data logs are supplemental best-effort traces. Record trigger/source/config identity without raw payloads/credentials; preserve partial/failed evidence. | Choose trigger-event store/key, retention, path redaction, History fields and export/support-bundle treatment. |
| D8. Security/deferred protocols and closure | Defer TCP, web forms, login, cloud, sync, remote users and unattended production until separate authentication/replay/tenancy/data-egress/hosting review. Figma `7:88`/`3:101` are shell/destination references only; require state-specific runtime evidence. | Name product, security, host, dispatch, History, UI Automation and QA owners; approve whether a trigger-detail Figma node is needed. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs) `StartWatchingExcelFile` / `OnLinkedExcelFileChanged` | Existing `FileSystemWatcher` watches one linked Excel file, debounces and raises stale-data notice; it does not silently reload. | It is not a generic trigger, claim ledger, file-lock protocol, lifecycle service or print automation authority. |
| [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs) | Manual paths create effective plans, validate rows, snapshot data and dispatch explicitly through WPF print/queue boundaries. | A future trigger cannot call a low-level renderer/printer API or bypass P4 policy, preflight or queue identity. |
| [`PrintJobManifest.cs`](../src/ANLAbel.Core/Printing/PrintJobManifest.cs) | Manifest v2 stores metadata/hashes only, including template/data/scene/output contract fingerprints; raw row payloads are not durable manifest data. | Trigger identity/config/source fields are not currently part of the manifest; owner must approve additive schema/migration before implementation. |
| [`DispatchRevalidationContract.cs`](../src/ANLAbel.Core/Printing/DispatchRevalidationContract.cs) | Document/output-contract/ticket drift immediately before dispatch fails closed and prevents submission. | It does not provide trigger claim/deduplication or trigger lifecycle persistence. |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs) | Job events are sequence/integrity chained and replay stops at invalid tails. | It is not a trigger registry or exactly-once delivery ledger; mixing trigger config events into job state needs a separate decision. |
| [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs) and [`DataOperationLogService.cs`](../src/ANLAbel.Data/DataLogs/DataOperationLogService.cs) | Print/data operation traces are local JSONL and best-effort. | Missing lines are not zero/success and cannot be the only trigger audit when claim/recovery semantics matter. |
| [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs) | Reprint request/approval is an explicit immutable-manifest job action and does not auto-dispatch. | A trigger is not reprint approval; do not route trigger authorization through P5 operator actions. |
| [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md) and [`CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md`](CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md) | Queue identity, History provenance and policy-on document checks are upstream boundaries. | P8 cannot close unresolved queue/document policy semantics by hiding them behind automation. |
| Read-only Applications [`asnGsLMxceJWb3HlfaE3q4`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `7:88` | Metadata gives `1280 x 800`, AppSidebar `7:109`-`7:123` (`220 x 246`), AppMain `7:124` (`1060 x 380`), sample cards `7:132`-`7:156` and share settings `7:157`-`7:161`. | Web apps, login, users/printers counts, cloud integrations, sharing and sample Automation rows are not local capabilities. |
| Read-only History destination node `3:101` | Activity frame is `1248 x 600` with submitted/type/module/workstation/user/status vocabulary and detail note `3:109`. | Sample Automation row is not evidence of a trigger module; P5 owns the actual History read model. |

## Proposed trigger and provenance contract

Proposal only; D1-D7 must close before code or a Start button is authorized.

| Field/state | Source/owner to approve | Display rule |
| --- | --- | --- |
| `TriggerId`/name/enabled | Future local trigger registry | Stable ID, config version and explicit enabled state; no inferred rows from unrelated logs. |
| `WatchRoot`/pattern/recursive | Future file-drop configuration | Show validated local scope/permissions; no consumption while stopped. |
| `ClaimProtocol` | Future move/marker/fingerprint ledger | Claim once, record event ID/fingerprint and recover after crash; duplicate events remain visible as duplicates, not new jobs. |
| `FileFingerprint`/detected time | Future canonical source hash and local timestamp | Preserve source identity/lock status; raw payload stays out of UI/audit by default. |
| `SourceStatus` | Supported parser/data binding owner | Detected, locked, malformed, unsupported, valid and changed-after-claim remain separate. |
| `DocumentRevision` | P3 revision identity plus P4 policy | Carry document/revision hash; changed document invalidates prepared work. |
| `QueueIdentity` | Explicit queue selection or approved operator-choice mode | Never use Windows default silently; missing/canonical mismatch blocks. |
| `Manifest`/preflight | Existing effective plan, manifest and `PrintPreflightValidator` | Trigger and manual paths produce the same checks; manifest fingerprint remains authoritative. |
| `JobId`/outcome | Existing job state plus P5 History projection | Add trigger/source/config provenance only after schema decision; queue observation is not physical completion. |
| `ConfigurationFingerprint` | Future canonical trigger config hash | Configuration drift invalidates prepared plans and is shown as a named block. |
| `Lifecycle` | Future trigger lifecycle store, separate from job state | Stopped/Starting/Running/Stopping/Error and per-file outcomes are explicit. |

## Lifecycle and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Stopped | Validated configuration and stopped status | Validate or Start explicitly | No file is consumed while stopped. |
| Starting | Root/pattern/source/queue checks and progress | Cancel or wait | Do not claim Running before every dependency is ready. |
| Running | Watch health, config fingerprint and recent events | Stop or inspect | Watcher errors move to Error/Stopped, not silent running. |
| Stopping | New claims blocked and in-flight count | Wait; force-stop only by approved policy | No claimed file is abandoned without durable outcome. |
| Error | Configuration, watcher, source, preflight, queue or audit reason | Repair and restart explicitly | No retry storm or hidden dispatch. |
| File detected | Event ID, path-safe fingerprint, lock/debounce status | Claim after validation | Duplicate watcher events collapse to one identity. |
| Claimed/preparing | Claim owner, source fingerprint, document/manifest progress | Cancel only through lifecycle owner | No second claim or concurrent dispatch for same fingerprint. |
| Blocked | Exact P4 policy, preflight, queue or output-contract reason | Repair source/config or open owner | No force-print or fallback queue. |
| Dispatched/observed | Job ID, trigger/config/source/manifest fingerprints and queue evidence | Open History/Print Center | Never label physical output complete without verifier evidence. |
| Quarantined | Source fingerprint, reason and destination | Inspect/move/delete with confirmation | Never silently discard the source. |
| Changed after claim | Original/current fingerprint and claim time | Quarantine or explicit new event | Do not dispatch stale bytes. |
| Figma research sample | Clearly marked design reference | None | Sample users/printers/counts/status never become runtime data. |

## Host-neutral layout and automation

```text
[Automation: local file-drop | config health | Stopped/Running | Refresh]
[Trigger filters: All | With errors | Started | Stopped]
[Trigger list/status]
[Selected trigger: root | pattern | document/revision | queue | config fingerprint]
[Recent events: detected | claimed | blocked | dispatched/observed | quarantined]
[Start | Stop | Configure | Open History | Open Print Center]
```

Start/Stop/Configure are trigger-owner actions. History and Print Center are deep-links; Automation never creates a second reprint/dispatch path. At `1024 x 600`, use one intentional scroll owner and keep blocked/quarantine reasons visible.

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P8.Automation.Root` | Local automation |
| Trigger list/status | `CC.P8.Automation.TriggerList` / `CC.P8.Automation.StatusFilter` | Local triggers / Trigger status filter |
| Configuration | `CC.P8.Automation.Configuration` | Selected trigger configuration |
| Start/Stop | `CC.P8.Automation.Start` / `CC.P8.Automation.Stop` | Start trigger / Stop trigger |
| Event list/detail | `CC.P8.Automation.EventList` / `CC.P8.Automation.EventDetail` | Automation events / Selected automation event |
| Configure | `CC.P8.Automation.Configure` | Configure trigger |
| History/Print Center | `CC.P8.Automation.OpenHistory` / `CC.P8.Automation.OpenPrintCenter` | Open automation History / Open Print Center |
| Quarantine | `CC.P8.Automation.Quarantine` | Quarantined source evidence |

## Fixture and regression packet

These are proposed fixtures and gates, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| Duplicate watcher events for one file | One claim/event identity and no duplicate job | Claim ledger/protocol is durable and visible. |
| Locked file then readable | Locked diagnostic, bounded wait and explicit next state | No partial read or silent drop. |
| Malformed/unsupported input | Quarantine with reason and source fingerprint | Source is not silently deleted or dispatched. |
| Changed after claim | Block/quarantine with original/current fingerprints | No stale-byte dispatch. |
| Missing/canonical-mismatch queue | Named queue diagnostic | No Windows-default or fallback queue. |
| P4 policy/preflight/output-contract block | Blocked event and safe owner link | No `Print anyway`; Text/TextBox and preflight contracts unchanged. |
| Accepted manual/trigger job | Same effective plan/manifest/preflight/queue path | Trigger/source/config provenance reaches P5 History. |
| Audit-store failure | Explicit Error/Blocked outcome | No optimistic success; best-effort trace absence is visible. |
| App restart with claimed/in-flight file | Recoverable durable outcome or quarantine decision | No duplicate dispatch and no silent loss. |
| Stop during dispatch | New claims blocked and in-flight result explicit | Driver/queue result remains truthful. |
| Retry request | New explicit event after review | No automatic retry or reprint-approval shortcut. |
| Figma sample Automation row | Density/reference only | Sample module/user/status never becomes fixture data. |

## No-go list

- Do not reuse the linked-Excel freshness watcher as a print trigger or silently reload data after a file event.
- Do not create a second preflight/manifest/queue/History/dispatch path or call a low-level printer API from a trigger.
- Do not consume files while stopped, claim them twice, dispatch changed/stale bytes or silently discard malformed/duplicate sources.
- Do not add automatic retry, fallback queue, `Print anyway`, or route trigger authorization through P5 reprint approval.
- Do not treat queue observation, job completion or sample Automation rows as physical output.
- Do not store raw source payloads, credentials or unrestricted paths in UI/audit/export by default.
- Do not infer login, roles, cloud, TCP, sharing, printer restrictions, user counts or unattended production from Figma `7:88`.
- Do not change Text/TextBox ownership, sizing, wrapping, clipping, padding, overflow or print parity.

## Owner sign-off record

Record one owner, date and decision for every row. Blank rows keep CC-P8 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. First trigger/prerequisites | `TBD` | `TBD` | `TBD` |
| D2. Host/lifecycle owner | `TBD` | `TBD` | `TBD` |
| D3. Claim/deduplication protocol | `TBD` | `TBD` | `TBD` |
| D4. Configuration/provenance schema | `TBD` | `TBD` | `TBD` |
| D5. Print spine/policy gate | `TBD` | `TBD` | `TBD` |
| D6. Lifecycle/recovery/retry | `TBD` | `TBD` | `TBD` |
| D7. History/log/privacy ownership | `TBD` | `TBD` | `TBD` |
| D8. Security/deferred protocols/closure | `TBD` | `TBD` | `TBD` |

**Closure rule:** CC-P8 may move from deferred design review to implementation only after D1-D8 are filled, P1/P2/P5/P4 prerequisites are closed, one trigger/lifecycle owner and one dispatch spine are named, and claim/deduplication/restart/stop/policy/audit fixtures are converted into runtime and regression gates. Until then, CC-P8 remains a local trigger plan and not a shipped Automation host, web application or unattended printer.
