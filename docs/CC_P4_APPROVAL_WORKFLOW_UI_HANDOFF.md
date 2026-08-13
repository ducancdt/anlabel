# ANLAbel — CC-P4 Approval Workflow UI handoff

**Status:** roadmap/design review; documentation-only checkpoint (2026-08-13)
**Owning roadmap:** [`MASTER_PLAN.md`](../MASTER_PLAN.md), section `2. Documents — storage, versioning, workflow`
**Predecessor:** [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md)
**Continuation:** [`reinvention/10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md) remains authoritative for Text/TextBox behavior.

This handoff defines the product and UI boundary for a future local approval workflow. It does not add a workflow enum, print gate, permissions model, or Figma edit. The existing “Approve reprint” action is a print-job lineage decision and must not be presented as approval of a label document.

## 1. Product boundary

The roadmap calls for a persisted document workflow with `Draft`, `InReview`, `Approved`, `Published`, and `Rejected`, explicit operator actions, and durable transition audit. NiceLabel’s research frame also shows scheduled publishing, user identity, comments, and a folder/status sidebar. ANLAbel is currently a single-machine, filesystem-first desktop application:

- A saved `.anlabel` file has a versioned JSON envelope and a `LabelTemplate` payload, but no typed document workflow state.
- Print preview/print already has software preflight, queue/output-contract checks, and immutable job/reprint evidence, but no document publication policy.
- The local print event store is hash-chained and actor-aware, but its lifecycle is for print jobs. Reusing it for document approval without a key/retention/actor decision would conflate two different audit domains.
- There is no role/identity provider, shared approval inbox, check-out lock, folder ACL, or multi-user synchronization service.

CC-P4 is therefore a **local document-state and policy decision**. It is not a web/LMS workflow, a claim of authenticated multi-user approval, or a replacement for P5’s exact-manifest reprint approval.

## 2. Existing source evidence

| Surface/evidence | Current behavior | CC-P4 implication |
| --- | --- | --- |
| [`LabelTemplate.cs`](../src/ANLAbel.Core/Models/LabelTemplate.cs) | The model has identity, geometry, printer/data configuration, objects/guides/transforms, and `JsonExtensionData`; no workflow state, actor, comment, or transition collection is typed. | Choose whether workflow metadata belongs in the saved envelope or template payload. A future typed contract must define missing/unknown state behavior and preserve existing authored data. `ExtensionData` is a forward-compatibility mechanism, not a policy gate by itself. |
| [`ProjectFileService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectFileService.cs) | `CurrentSchemaVersion` is `2`; save writes `format`, `schemaVersion`, and `template`; future schema versions fail closed; legacy raw payloads remain loadable. | A workflow rollout needs an explicit migration/backward-compatibility rule. Do not silently treat a missing field as `Published`; when policy is on, missing/unknown metadata must be an explicit block or an owner-approved legacy exception. |
| [`PrintPreflightValidator.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs) | Preflight validates geometry, bindings, fonts, text, image and barcode risks, including protected TextBox behavior; it has no document workflow input. | The publication gate must be a separate policy check composed with preflight. It must not change Text/TextBox layout or turn a workflow warning into an overflow/geometry rule. |
| [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs) | `ValidateRows` and `ValidateRowsAsync` run preflight; preview/dispatch use resolved render/output plans and explicit queues; no Published-state check exists. | The same document revision/state must be checked at preview/prepare and immediately before dispatch. If state or document hash changes, invalidate the prepared plan and fail closed. |
| [`PrintJobState.cs`](../src/ANLAbel.Core/Printing/PrintJobState.cs) | Job lifecycle (`Created`, `Preparing`, queue/terminal states) and operator actions include `ReprintRequested`/`ReprintApproved`; transitions are not document approval states. | Keep job approval and document approval in separate names, IDs, and acceptance tests. A document being Published never proves physical output; a reprint being approved never publishes a document. |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs) | Append-only per-job events carry sequence, previous hash, integrity hash, document/scene/output hashes and actor; replay stops at invalid integrity. | Reuse this hash-chain discipline as a design constraint, but choose a document-workflow store/key before implementation. A job log is not automatically a document approval history. |
| [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs) | Explicit reprint request/approval is durable and exact-manifest guarded; approval does not dispatch. | Keep the P5 `Request → Approve → Prepare → Dispatch` chain unchanged. Do not route template Request approval/Approve buttons to these job methods. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs) / [`PrintPreviewWindow.xaml`](../src/ANLAbel.App/PrintPreviewWindow.xaml) | Current UI exposes print preview, queue/recovery and linked-reprint review, but no document workflow status/actions. | Decide whether status/actions belong in the CC-P3 document library, the existing designer shell, or a separate workflow window. Do not add buttons merely because the Figma labels exist. |

## 3. Figma reference and routing

Use the existing [ANLAbel Control Center Figma file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) as a research reference. Read-only metadata was checked on 2026-08-13; no Figma node was edited or duplicated.

Frame `7:2` (`CC / Documents — Workflow`, `1280 × 800`) gives state vocabulary and density, not implementation proof:

| Node | Measured reference | WPF/design question |
| --- | --- | --- |
| `7:23` | Sidebar `(0,92)`, `220 × 229`; Root, Approved, Drafts, In Review, Published, Rejected. | Are these real local folders, virtual status filters, or both? A document cannot be placed into “Published” by moving a file alone. |
| `7:37` | Workflow main `(220,92)`, `1060 × 812`; the content extends below the `800`-high frame. | Define one intentional scroll owner and the minimum window/scale behavior before implementing a WPF view. |
| `7:38`–`7:41` | Header `1020 × 100` with `Workflow status — label_shipping_v3.nlbl` and `REQUEST APPROVAL`. | The primary action must be state-aware and disabled/hidden when the current revision is invalid, dirty, or already in review. |
| `7:42`–`7:53` | Main path: Draft → Request approval → Approved → Published. | Treat this as a candidate transition graph; owner must approve who may perform each edge and what revision hash is covered. |
| `7:54`–`7:58` | Rejected (return to Draft) and Scheduled for publishing alternates. | Rejection reason is required for a useful local audit; scheduling stays deferred until a durable clock/store policy exists. |
| `7:59`–`7:67` | Approve, Reject, Request changes, Publish now action row. | Research labels only. No current command service, role policy, or transition store authorizes these actions. |
| `7:68`–`7:87` | Step history `1020 × 300`: When, User, From, To, Comment; sample dates/users are illustrative. | Current local logs have timestamps/reasons and a normalized actor for print jobs, not authenticated workflow users. Do not present sample identities as product evidence. |

The frame has no invalid/unsupported file, unsaved-edit, stale-review, permission-denied, audit-write-failure, policy-off, or print-blocked state. Those states require a state-specific WPF acceptance design. A new Figma frame is not required for this documentation checkpoint; create or reuse one only after the owner chooses the first operator task.

## 4. Proposed state contract for review

This is a proposal for owner review, not a code authorization.

| State | Meaning | Allowed next action | Required evidence |
| --- | --- | --- | --- |
| `Draft` | Editable revision that is not under review. | Request approval; edit/save creates or retains Draft according to revision rule. | File path, document hash, dirty state, actor and comment when requested. |
| `InReview` | A specific immutable document revision is awaiting a decision. | Approve, Reject/Request changes, or Cancel review if owner allows. | Review revision hash, requested-by actor/time, comment, and no silent replacement by a newer edit. |
| `Approved` | Review decision passed for the covered revision. | Publish; optionally return to Draft only through an explicit change/reopen action. | Approver actor/time/comment and exact revision hash. |
| `Published` | Revision is allowed by the selected production-print policy. | Preview/print; editing must create a new reviewable revision or reopen explicitly. | Published actor/time, source approval event, document hash and policy version. |
| `Rejected` | Review failed and the covered revision cannot be published. | Return to Draft / Request changes with a reason. | Rejector actor/time/reason and covered revision hash. |
| Missing/unknown | Existing or future file has no recognized workflow state. | Inspect, migrate, or choose a policy exception. | Diagnostic; never infer Published. |

Candidate transition graph:

```text
Draft --Request approval--> InReview --Approve--> Approved --Publish--> Published
                              |                    |
                              +--Reject/changes--> Rejected --Return to Draft--+
```

The owner must decide whether `Rejected` is a durable state or an event that immediately returns to `Draft`, whether an approved revision can be edited in place, and whether publishing is a state transition or a separate immutable release pointer.

## 5. Print-policy boundary

The roadmap permits a policy switch: block print of non-`Published` documents when policy is on. The UI and service must make the choice explicit:

| Policy mode | Missing/unknown state | `Draft`/`InReview`/`Approved`/`Rejected` | `Published` | User-facing behavior |
| --- | --- | --- | --- | --- |
| Policy off (compatibility) | Allow existing preflight rules to decide | Allow existing preflight rules to decide | Allow | Show status as informational; do not claim approval. |
| Policy on / fail closed | Block before preview/prepare/dispatch | Block with state, revision hash, and safe next action | Continue to normal preflight/queue/output checks | No bypass/“print anyway” button unless separately approved as a policy exception. |
| Policy configuration invalid | Block or refuse to enable production print | Block | Block until policy is repaired | Explain configuration source/version; do not silently fall back to off. |

The gate must be evaluated against the same immutable document hash used by preview/manifest/dispatch. A transition after preview invalidates the prepared action; a document edit after approval must not inherit the old Published decision without the owner-approved revision rule. This gate is independent of Text/TextBox overflow, sizing, wrapping, clipping, padding, and print geometry.

## 6. Durable audit and actor boundary

Before implementation, choose a document-workflow event shape with at least:

- event sequence and previous/integrity hash (or an explicitly justified equivalent);
- document ID, path/relative library identity, revision/document hash and schema/policy version;
- `From`, `To`, action, UTC timestamp, actor identifier, comment/reason;
- result/error and whether the event was accepted, rejected, or only requested;
- a clear distinction between a state transition and a file save/revision archive event.

The current normalized print actor (often the local `operator`) is not authenticated identity. Until a role/identity source exists, UI copy must say “local operator” or equivalent and must not imply separation of duties. If the product owner requires requester/approver separation, define the local identity mechanism before exposing Approve.

Audit failure is a hard boundary: if a transition cannot be durably written and verified, the UI must not show the new state or allow a policy-on print based on it. A torn final line may be recoverable only under the chosen store’s explicit replay rules; do not copy the print-log recovery behavior without documenting the same guarantees.

## 7. Proposed vertical slices

### M1 — Contract and read-only status

1. Approve the state enum, missing-state policy, revision/hash coverage, actor wording, and policy mode.
2. Show status and the latest valid transition in the CC-P3 library/revision surface without enabling mutations.
3. Add diagnostic states for unknown schema, invalid file, stale review and audit-store failure.

### M2 — Local transition actions

1. Add Request approval, Approve, Reject/Request changes and Publish only after M1 persistence and role decisions.
2. Require comments for rejection/request changes; require explicit confirmation for Publish and any reopen of a Published revision.
3. Keep transition buttons state-aware, keyboard reachable, and disabled while the selected revision is dirty or invalid.

### M3 — Production-print gate

1. Compose workflow policy with existing preview/preflight/queue/output-contract checks.
2. Re-evaluate state and document hash immediately before prepare/dispatch.
3. Add a named block reason and safe route back to the library/review surface; never silently downgrade to warning.

Scheduled publishing, multi-user locks, folder ACLs, remote sync, authenticated identity, and browser/LMS surfaces remain later decisions.

## 8. UI state and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| No workflow metadata | “Workflow status unavailable” plus file/schema/hash | Inspect or migrate | Never display Published. |
| Draft clean | Status, revision hash, last transition, Request approval | Request approval or edit | Do not submit a dirty/unvalidated revision. |
| Draft dirty | Dirty marker and current file path | Save, discard with confirmation, or cancel | No transition may cover unsaved bytes. |
| InReview | Requested-by/time, covered hash, comment | Approve, Reject, Request changes, Cancel if allowed | Newer file hash invalidates the review. |
| Approved | Approver/time/comment, covered hash | Publish or reopen according to policy | Do not print if the current hash differs. |
| Published | Publisher/time, policy/version, hash | Preview/print if policy allows | A later edit cannot inherit Published silently. |
| Rejected | Reason, actor/time, covered hash | Return to Draft / request changes | Reject is not a print cancellation or file deletion. |
| Permission/role denied | Required role and current local identity wording | Sign in/choose allowed operator, or cancel | No disabled-control bypass. |
| Audit write/verify failure | Store path and actionable error | Retry/repair/cancel | Do not show transition or enable print. |
| Policy-on print blocked | State, hash, policy source/version, next action | Open review/library or cancel | No force-print path in this slice. |
| Figma-only research sample | Clearly marked design reference | None | Do not copy sample user/date/status as live data. |

## 9. WPF and acceptance gates

| Gate | Evidence before implementation closure |
| --- | --- |
| Host decision | Owner chooses CC-P3 library/revision extension, designer-shell panel, or a separate workflow window; exactly one transition service/store owns mutations. |
| Scale/layout | Runtime screenshots or UI Automation at `1024 × 600`, `100%`, `125%`, and `150%`; WorkflowMain’s long content has one intentional scroll owner. |
| Keyboard/accessibility | Stable names/AutomationIds for status, revision hash, Request approval, Approve, Reject/Request changes, Publish, comment, policy-block reason, and confirmation/cancel. |
| Data safety | Dirty edits, stale hashes, unknown schema, invalid files, role denial, and audit failures are visible and non-destructive. |
| Print parity | Preview/prepare/dispatch all consult the same workflow policy and document hash; no Text/TextBox contract or geometry change. |
| Separation from P5 | Document approval tests and reprint-approval tests use different commands, stores/keys, and user copy. |
| Regression | Add transition-matrix, serialization/migration, audit-integrity, stale-hash, policy-on/off, and preview/dispatch revalidation coverage before enabling actions. |
| Figma | Record the selected state-specific node and measured dimensions; metadata frame `7:2` alone is not runtime proof. No new file/edit is required for this handoff. |

## 10. Owner decisions needed

1. Should workflow metadata live inside `LabelTemplate`, beside it in the versioned envelope, or in a sidecar keyed by document hash?
2. What is the behavior for legacy files with no workflow state when policy is on: block, explicit one-time migration to Draft, or owner-approved legacy exception?
3. Is “Approved” printable, or only “Published”? Is Publish a mutable state or an immutable release pointer?
4. Must requester and approver be different local identities, and how is identity established without a server?
5. Is `Rejected` durable, and what exact action returns it to Draft? Are comments mandatory for both rejection and request changes?
6. Which local store owns workflow events, retention, hash verification, backup/archive, and recovery after a torn write?
7. Which host owns the controls, and what automation IDs/labels are approved for Vietnamese/English operator copy?
8. Is scheduling explicitly deferred, or is there a durable local clock/time-zone requirement to define now?

## 11. Decision

**Needs product/design review.** Figma `7:2` is sufficient to route the candidate state vocabulary and action/history density, but current ANLAbel source has no typed document workflow, actor/role policy, transition store, or Published print gate. The next safe step is to approve the state/migration/audit/policy contract and a state-specific host; no workflow controls, print blocking, or Figma edit are authorized by this documentation checkpoint.
