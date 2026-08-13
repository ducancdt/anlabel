# ANLAbel — CC-P8 Applications / Automation UI/UX spec

**Status:** deferred, design-only local file-drop trigger spec; prerequisites, host and delivery semantics remain open (2026-08-13)
**Prerequisites:** P1/P2/P5 evidence paths and P4 document policy must remain stable
**Handoff:** [`CC_P8_AUTOMATION_UI_HANDOFF.md`](CC_P8_AUTOMATION_UI_HANDOFF.md)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, Applications `7:88`, History destination `3:101`

This spec maps the Figma Applications shell to the smallest local automation direction: one explicitly configured file-drop trigger that reuses the manual preflight → manifest → queue → History spine. It does not create a trigger runner, web form, TCP listener, cloud integration, background service or unattended-print capability.

## 1. Operator outcome

The first automation surface should let an operator:

1. inspect one local trigger's configuration, lifecycle and source-health state;
2. start/stop it explicitly and see whether it is stopped, starting, running, stopping or in error;
3. see detected, claimed, blocked, dispatched, observed and quarantined events with trigger/source/manifest provenance;
4. understand duplicate, locked, malformed, changed-after-claim and queue/policy failures without silent loss or retry storms;
5. open the existing P5 History / Print Center owners for accepted jobs and recovery;
6. recognize that Figma web apps, login, sharing, users/printers counts and remote integrations are deferred research vocabulary.

## 2. Figma node map (read-only)

Metadata for `7:88` was rechecked read-only on 2026-08-13. The frame is a web-application shell, not a local trigger console.

| Figma node | Metadata name / bounds | ANLAbel role | Boundary |
| --- | --- | --- | --- |
| `7:88` | `CC / Applications — Web Apps`, `1280 x 800` | Navigation/card density reference | No web application, login, sharing or cloud capability claim. |
| `7:109`–`7:123` | `AppSidebar`, `(0,92)`, `220 x 246`; Web Applications, Cloud Integrations, Automation, All Triggers, With errors, Started, Stopped | Candidate filter vocabulary | Only local trigger lifecycle/filter states may be reused after a durable contract exists. |
| `7:124` | `AppMain`, `(220,92)`, `1060 x 380` | Main list/status density reference | No trigger-detail, event, duplicate or quarantine layout is supplied. |
| `7:125`–`7:131` | Header `1020 x 100`; Web Applications, New web application, Share/configure | Research action language | No local sharing, remote-user login or web-app designer. |
| `7:132`–`7:156` | Three sample cards with user/printer counts and Open | Card density only | Sample counts/names are not local users, printers or license evidence. |
| `7:157`–`7:161` | Share settings: login, printer restrictions, History, file/database connections | Future product/security questions | Only History recording is compatible with the local direction; all other settings remain deferred. |
| `3:101` | History activity frame `1248 x 600` | Outcome/deep-link destination | Route automation provenance through P5; sample Automation rows are not product evidence. |

No dedicated trigger list/detail, duplicate-file, queue-blocked, retry or quarantine state exists in Figma. No new node is required until the owner selects a concrete first trigger task.

## 3. Local trigger contract

| Trigger field | Source/owner to approve | Display rule |
| --- | --- | --- |
| `TriggerId` / name / enabled | Future local trigger registry | Stable identity and configuration version; no inferred trigger rows from unrelated logs. |
| `WatchRoot` / pattern / recursive | Future file-drop configuration | Show exact local scope, permissions and pattern; validate before Start. |
| `ClaimProtocol` | Future move-to-processing, sidecar marker or hash ledger decision | Duplicate events collapse to one deterministic event ID/fingerprint; never consume twice. |
| `FileFingerprint` / detected time | Future stable source hash + local timestamp | Preserve source identity and lock/read status; never store raw payload in UI logs by default. |
| `SourceStatus` | Supported parser/data-binding owner | Detected, locked, malformed, unsupported, changed-after-claim and valid remain distinct. |
| `DocumentRevision` | P3 library/revision identity; P4 policy when enabled | Carry document/revision hash; changed document invalidates a prepared action. |
| `QueueIdentity` | Explicit printer/queue selection or operator-choice mode | Never use the Windows default silently; missing/canonical mismatch is blocked. |
| `Manifest` / preflight | Existing effective-plan, manifest and `PrintPreflightValidator` spine | Trigger uses the same manifest/output-contract checks as manual print. |
| `JobId` / outcome | Existing `PrintJobStateStore`, operation trace and P5 History | Attach trigger ID/config/source fingerprints; queue observation is not physical completion. |
| `ConfigurationFingerprint` | Future canonical trigger config hash | A config change invalidates any prepared plan created under the old config. |
| `Lifecycle` | Future trigger lifecycle store, separate from print-job state | Stopped/Starting/Running/Stopping/Error and per-file event states are explicit. |

The current linked-Excel `FileSystemWatcher` remains a stale-data notice. It is not a generic print trigger, claim ledger or automation audit.

## 4. Host-neutral wireframe

```text
[Automation context: Local file-drop | configuration health | Stopped/Running | Refresh]

[Trigger filters: All | With errors | Started | Stopped]
[Trigger list/status]

[Selected trigger: root | pattern | document/revision | queue | configuration fingerprint]
[Recent events: detected | claimed | blocked | dispatched/observed | quarantined]

[Start | Stop | Configure | Open History | Open Print Center]
```

Start/Stop/Configure are explicit trigger-owner actions. History and Print Center are deep-links; Automation never creates a second reprint/dispatch path.

## 5. Lifecycle and failure matrix

| State | Visible evidence | Safe next action | Fail-closed rule |
| --- | --- | --- | --- |
| `Stopped` | Validated configuration and stopped status | Validate or Start explicitly | No file is consumed while stopped. |
| `Starting` | Root/pattern/source/queue checks and progress | Cancel or wait | Do not claim Running before every dependency is ready. |
| `Running` | Watch health, config fingerprint and recent events | Stop or inspect | Watcher errors move to Error/Stopped, not silent running. |
| `Stopping` | New claims blocked and in-flight work count | Wait; force-stop only by owner decision | No claimed file is abandoned without durable outcome. |
| `Error` | Configuration/watcher/source/preflight/queue/audit reason | Repair and restart explicitly | No retry storm or hidden dispatch. |
| `FileDetected` | Event ID, path/fingerprint, lock/debounce status | Claim after validation | Duplicate watcher events collapse to one identity. |
| `ClaimedPreparing` | Claim owner, source fingerprint, document/manifest progress | Cancel only through lifecycle owner | No second claim or concurrent dispatch for the fingerprint. |
| `Blocked` | Exact preflight, P4 policy, queue or output-contract reason | Repair source/config or open owner | No force-print or fallback queue. |
| `DispatchedObserved` | Job ID, trigger ID, manifest/config fingerprints, queue observation | Open History/Print Center | Never label physical output complete without verifier evidence. |
| `Quarantined` | Source path/fingerprint, reason and destination | Inspect/move/delete with confirmation | Never silently discard the source. |
| `ChangedAfterClaim` | Original/current fingerprint and claim timestamp | Quarantine or explicit new event | Do not dispatch stale bytes. |

## 6. Configuration, safety and responsive behavior

Minimum configuration requires owner decisions for trigger ID/name, root/pattern, recursive policy, claim/archive/error directories, file-lock timeout, parser/data binding, target document/revision, queue identity, debounce/deduplication, lifecycle store, redaction and retention. TCP/web scope requires a separate security/product decision covering authentication, replay, tenancy, data egress and hosting.

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May preserve the `220 DIP` sidebar and `1060 DIP` main density as a reference; recent events need an explicit scroll owner beyond Figma’s short main frame. | Trigger/status → config → events → Start/Stop → deep-links. |
| `1024 x 600` | Collapse filters into a drawer/narrow rail; stack selected configuration and recent events; keep blocked/quarantine reason visible without page-level horizontal scroll. | Keyboard order remains status → filters → trigger → config → event detail → actions. |
| `100%`, `125%`, `150%` | Reflow or clip only inside declared owners; never blindly scale sample cards. | Capture screenshot/UI Automation at every scale and record environment exceptions. |

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P8.Automation.Root` | `Local automation` |
| Trigger list | `CC.P8.Automation.TriggerList` | `Local triggers` |
| Status/filter | `CC.P8.Automation.StatusFilter` | `Trigger status filter` |
| Configuration | `CC.P8.Automation.Configuration` | `Selected trigger configuration` |
| Start/Stop | `CC.P8.Automation.Start` / `CC.P8.Automation.Stop` | `Start trigger` / `Stop trigger` |
| Event list | `CC.P8.Automation.EventList` | `Automation events` |
| Event detail | `CC.P8.Automation.EventDetail` | `Selected automation event` |
| Configure | `CC.P8.Automation.Configure` | `Configure trigger` |
| Open History | `CC.P8.Automation.OpenHistory` | `Open automation History` |
| Open Print Center | `CC.P8.Automation.OpenPrintCenter` | `Open Print Center` |
| Quarantine | `CC.P8.Automation.Quarantine` | `Quarantined source evidence` |

## 7. Acceptance gate

Before implementation review closes P8:

- P1/P2/P5 evidence paths and P4 document policy are stable, and one WPF host/lifecycle owner is named;
- fixtures cover duplicate watcher events, locked files, malformed/unsupported input, changed-after-claim, missing/canonical-mismatch queue, preflight/policy block, audit failure, app restart and stop-during-dispatch;
- trigger events carry stable trigger/source/config/document/manifest/job identities into P5 History;
- manual and trigger paths use the same effective plan, immutable manifest, preflight, queue and physical-verification boundaries;
- no automatic retry, fallback queue, `Print anyway`, raw payload/credential logging or silent source deletion exists;
- TCP/web/login/cloud and unattended-production claims remain visibly deferred;
- runtime screenshot/UI Automation covers `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus and scroll ownership;
- Figma sample apps, user/printer counts and Automation labels never become local runtime data without evidence;
- protected Text/TextBox behavior and print/recovery contracts remain untouched.

Until these prerequisites and gates close, this file is a UI/UX specification, not a shipped Automation host or trigger runner.
