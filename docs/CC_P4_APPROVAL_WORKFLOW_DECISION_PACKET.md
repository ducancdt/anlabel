# CC-P4 approval workflow owner decision packet

**Status:** Core transition graph, local sidecar audit and publication-policy evaluator implemented; permissions model, policy configuration/print-path integration, new Figma node and Text/TextBox change remain open (2026-08-14)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Handoff:** [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md)
**Specification:** [`CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md`](CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md)
**Predecessor:** [`CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md)
**P5 separation contract:** [`CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md`](CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

CC-P4 is the document-state and publication-policy slice that follows local document/revision access. It must make a review decision traceable to one validated revision without confusing document approval with P5 linked-reprint approval, normal print preflight, queue acceptance or physical-output verification.

```text
validated local revision + exact document hash
        -> workflow state/transition decision
        -> durable document-workflow audit event
        -> policy evaluation at preview/prepare/dispatch
        -> revalidate state + hash before output
```

The packet remains a review gate for policy integration. The pure
`DocumentWorkflowContract` provides the state vocabulary and fail-closed graph;
`DocumentWorkflowStore` records a hash-chained local sidecar audit keyed to the
saved document path/hash; and `DocumentWorkflowPrintPolicy` evaluates Off versus
RequirePublished fail-closed. No policy configuration is wired into preview,
prepare or dispatch yet, and no identity/roles, local browser or Figma edit is
implied. Existing Text/TextBox ownership, geometry, overflow and
designer/preview/print parity remain protected.

**Current dependency note:** P3 owns validated local document identity and one revision/restore path;
P4 may compose a future document policy only after that identity is stable. P5 linked-reprint
approval remains a separate print-job decision and never becomes a document transition. This packet
keeps both downstream action domains documentation-only.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. State vocabulary and transition machine | Use explicit `Draft`, `InReview`, `Approved`, `Published`, `Rejected` and `Unknown`/missing diagnostics. Keep `Scheduled` out of M1 until a durable local clock and recovery policy exist. Every mutation is a named transition, never a free-form status edit. | Approve enum names, whether `Rejected` is durable, reopen/cancel edges, and whether `Published` is mutable state or an immutable release pointer. |
| D2. Metadata placement and legacy migration | Prefer workflow metadata beside `template` in the versioned project envelope, with an explicit schema bump and a policy version. Keep `LabelTemplate.ExtensionData`/`TemplateExtensionContract` for authored forward-compatible content, not an implicit approval gate. Missing/unknown workflow metadata is `Unknown`, never `Published`. | Choose envelope, template or sidecar ownership; define schema migration, one-time Draft conversion versus block, and preservation of unknown fields. |
| D3. Revision/hash coverage | Bind every request, decision and publish event to one validated revision/document hash plus path/document identity. Dirty edits or a changed hash invalidate review and prepared print actions; no newer file silently inherits approval. | Approve canonical hash input, path/identity fields, whether a release pointer is retained, and the stale-review UX. |
| D4. Actor, comment and audit owner | Create one future document-workflow event store distinct from `PrintJobStateStore` and operation JSONL. Reuse hash-chain/integrity discipline, but label the actor `local operator` until an authenticated identity source exists. Require a reason/comment for rejection/request changes and record audit-write failure as a hard block. | Choose local identity/separation-of-duties rules, event key/retention/backup/recovery, mandatory comments and audit repair behavior. |
| D5. Print-policy composition | Policy `Off` is informational. Policy `On` fails closed for missing/unknown, Draft, InReview, Approved and Rejected unless the owner explicitly permits an exception; `Published` still runs normal preflight, output-contract and queue checks. Re-evaluate state and hash at preview/prepare/dispatch. | Decide whether `Approved` is printable, the exact policy-on exception path, configuration ownership/versioning and user-facing block copy. |
| D6. Host and action ownership | Prefer extending the CC-P3 library/revision detail path with one workflow detail surface, or choose one dedicated local workflow host through the shared host packet. One transition service/store owns Request/Approve/Reject/Request changes/Publish; never route document actions to P5 reprint methods. | Choose host, deep-link/navigation route, command owner and stable AutomationIds for the first operator task. |
| D7. Scheduling, roles and destructive edges | Keep scheduled publishing, server roles, check-out, ACL, remote sync and multi-user inbox deferred. Require explicit confirmation for Publish/reopen and preserve the current file/revision on rejected or failed transitions. | Approve whether requester/approver separation is required locally, whether Reject returns to Draft immediately, and the allowed reopen/cancel actions. |
| D8. Runtime/Figma/regression closure | Treat Figma Workflow `7:2` as vocabulary/density only. Require state-specific WPF evidence, one scroll owner, keyboard/accessibility IDs, redacted path/hash display and transition/policy fixtures before implementation. | Name product, WPF host, workflow-store, print-policy, UI Automation and QA owners; approve whether a state-specific Figma node is needed. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`LabelTemplate.cs`](../src/ANLAbel.Core/Models/LabelTemplate.cs#L95-L101) `ExtensionData` and [`TemplateExtensionContract.cs`](../src/ANLAbel.Core/Models/TemplateExtensionContract.cs#L12-L30) | Unknown template members survive round trips and can contribute a deterministic extension fingerprint. | Extension data is not a workflow state, actor record, role check or durable transition history. |
| [`ProjectFileService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectFileService.cs#L11-L61) and [`ProjectFileService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectFileService.cs#L282-L318) | Current envelope writes `format`, `schemaVersion` (`2`) and `template`; future schema fails closed while legacy raw payloads remain loadable. | A missing workflow field cannot safely mean Published; migration and policy exceptions are still product decisions. |
| [`ProjectRevisionService.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionService.cs#L89-L218) and [`ProjectRevisionArchive.cs`](../src/ANLAbel.Project/SaveLoad/ProjectRevisionArchive.cs#L37-L193) | Primary/backup/archive bytes can be validated, compared, restored and retained with bounded local audit evidence. | A valid revision archive is not an approval event or an authenticated multi-user audit store. |
| [`DocumentSnapshot.cs`](../src/ANLAbel.Core/Scene/DocumentSnapshot.cs#L18-L78) and [`DocumentSnapshot.cs`](../src/ANLAbel.Core/Scene/DocumentSnapshot.cs#L435-L480) | The current document hash covers the snapshot, authored extension fingerprint and persisted design/data identity. | A hash proves byte/content identity for the chosen snapshot; it is not an actor decision or Published policy. |
| [`PrintPreflightValidator.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs#L32-L88) | Existing preflight owns geometry, binding, font, image, barcode and TextBox checks. | It has no document workflow input; publication policy must compose outside it and must not alter Text/TextBox rules. |
| [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs#L292-L348) and [`PrintService.cs`](../src/ANLAbel.Printing/PrinterProfiles/PrintService.cs#L350-L390) | Preview/prepare and dispatch use explicit plans, rows and queue/output contracts; no Published-state check exists. | A future policy must compose with these owners and re-check state/hash without changing geometry/preflight rules. |
| [`DispatchRevalidationContract.cs`](../src/ANLAbel.Core/Printing/DispatchRevalidationContract.cs#L1-L90) | Prepared versus final document/output identity can be compared and changed fields reported before submission. | Dispatch revalidation is not document workflow storage or actor approval. |
| [`PrintJobState.cs`](../src/ANLAbel.Core/Printing/PrintJobState.cs#L1-L90) and [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs#L63-L187) | Reprint request/approval is an immutable-manifest job action and does not dispatch by itself. | `ReprintApproved` is not document approval; reusing its command/store would conflate two audit domains. |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs#L1-L90) | Print events carry sequence, previous/integrity hashes, manifest/scene/output hashes and normalized actor data. | A print actor such as `operator` is not authenticated workflow identity, and the job store is not automatically the workflow owner. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L701-L716) and [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L309-L401) | Current WPF reaches library, revision, preview, queue and recovery surfaces; preview performs effective-plan and preflight work before dispatch. | No current transition service, role policy, workflow history or Published print gate exists. |
| Read-only Control Center Workflow [`asnGsLMxceJWb3HlfaE3q4`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `7:2` | Metadata gives `1280 x 800`; sidebar `7:23` (`220 x 229`); `WorkflowMain` `7:37` (`1060 x 812`); state path `7:42`; actions `7:59`; history `7:69` (`1020 x 300`). | Sample users/dates and labels do not prove local states, identity, permissions, scheduling, failure handling or print eligibility. |

## Proposed state and transition contract

Proposal only; implementation requires D1-D7 approval and a versioned event schema.

| State | Meaning | Candidate next action | Required evidence |
| --- | --- | --- | --- |
| `Draft` | Editable validated revision not under review. | Request approval; save/edit follows the chosen revision rule. | Document identity, exact hash, dirty state, local-operator actor and optional comment. |
| `InReview` | One immutable revision awaits a decision. | Approve, Reject/Request changes, or owner-approved cancel. | Requested-by actor/time, covered hash and comment. |
| `Approved` | Review passed for the covered revision. | Publish or explicit reopen. | Approver actor/time/comment and exact revision hash. |
| `Published` | Revision is eligible under the selected print policy. | Preview/print if policy allows; edit creates a new reviewable revision. | Publisher actor/time, policy version, source approval and hash. |
| `Rejected` | Covered revision failed review. | Return to Draft / Request changes with a reason. | Rejector actor/time/reason and covered hash. |
| `Unknown` | Missing or unrecognized workflow metadata. | Inspect, migrate or choose an owner-approved compatibility path. | Schema/policy diagnostic; never infer Published. |

Candidate graph:

```text
Draft --Request approval--> InReview --Approve--> Approved --Publish--> Published
                              |                    |
                              +--Reject/changes--> Rejected --Return to Draft--+
```

The graph is not a command authorization. A transition must validate the current state, current validated revision/hash, actor policy, comment requirements, event-store write and policy version before reporting the new state.

## Policy and ownership boundaries

| Concern | Single future/current authority | Boundary |
| --- | --- | --- |
| File/revision identity | P3 validated load plus `ProjectRevisionService` | Workflow covers the exact validated revision; thumbnails, names and sample cards cannot establish identity. |
| Workflow state/event | One future document-workflow store/transition service | Keep event keys and retention separate from print-job state and best-effort operation JSONL. |
| Actor/comment | Future local actor policy | Say `local operator` until identity is authenticated; do not imply independent approval from a free-text name. |
| Print eligibility | Future workflow policy composed with existing preflight/output/queue contracts | Same state/hash is checked at preview/prepare/dispatch; policy does not change geometry or Text/TextBox behavior. |
| Reprint approval | Existing `PrintJobOperatorActionService` and immutable print manifest | P5 `Request -> Approve -> Prepare -> Dispatch` remains separate from document transitions. |
| Figma | Read-only metadata | Reuse `7:2` for information architecture only; state-specific runtime evidence is required for each enabled action. |

## Policy matrix for owner approval

| Policy mode | Missing/unknown | Draft/InReview/Approved/Rejected | Published | User-facing rule |
| --- | --- | --- | --- | --- |
| `Off` | Existing preflight decides | Existing preflight decides | Existing preflight decides | Show workflow as informational; never claim approval. |
| `On` / fail closed | Block before preview/prepare/dispatch | Block with state, hash and safe next action unless an explicitly approved exception exists | Continue normal preflight, output-contract and queue checks | No implicit `Print anyway` path. |
| Invalid configuration | Block or refuse production print | Block | Block until repaired | Show source/version; never silently fall back to `Off`. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| No workflow metadata | Status unavailable, file/schema/hash and migration route | Inspect or migrate | Never display Published. |
| Draft clean | State, hash, last transition | Request approval or edit | Do not submit dirty/unvalidated bytes. |
| Draft dirty | Dirty marker and current file identity | Save, discard with confirmation or cancel | No transition covers unsaved bytes. |
| InReview | Requested-by/time, covered hash and comment | Approve, reject/request changes, or cancel if allowed | Newer hash invalidates review. |
| Approved | Approver/time/comment and covered hash | Publish or reopen per policy | Do not print if current hash differs. |
| Published | Publisher/time, policy/version and hash | Preview/print if policy allows | Later edits cannot inherit Published silently. |
| Rejected | Reason, actor/time and covered hash | Return to Draft / request changes | Reject is not print cancellation or file deletion. |
| Role/identity denied | Required role and local-identity wording | Choose allowed operator or cancel | No disabled-control bypass. |
| Audit write/verify failure | Store path and actionable error | Retry/repair/cancel | Do not show transition or enable policy-on print. |
| Policy-on print blocked | State/hash/policy source/version and next action | Open library/review or cancel | No force-print path in this slice. |
| Figma research sample | Clearly marked design reference | None | Sample user/date/status never becomes runtime data. |

## Host-neutral layout and ownership

Keep this order whether the owner extends P3 or selects a dedicated local host:

```text
[Document identity | path/source | state | revision hash | dirty/valid]
[State path: Draft -> InReview -> Approved -> Published]
[Rejected / Request changes branch and reason]
[State-aware actions: Request approval | Approve | Reject | Request changes | Publish]
[Policy card: mode/version | print eligibility | exact block reason]
[History: UTC/local time | local operator | From | To | hash | comment | result]
```

Only one transition service/store may own the mutations. Controls stay hidden or disabled while the selected revision is dirty/invalid, the actor policy is unresolved or the audit write path is unavailable. At `1024 x 600`, stack the state path, actions, policy and history under one intentional scroll owner; never solve overflow by changing protected label geometry.

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P4.Workflow.Root` | Document approval workflow |
| Status | `CC.P4.Workflow.Status` | Document workflow status |
| Revision/hash | `CC.P4.Workflow.Revision` | Reviewed document revision |
| State path | `CC.P4.Workflow.StatePath` | Workflow state path |
| Request approval | `CC.P4.Workflow.RequestApproval` | Request document approval |
| Approve | `CC.P4.Workflow.Approve` | Approve document revision |
| Reject/request changes | `CC.P4.Workflow.Reject` | Reject or request changes |
| Publish | `CC.P4.Workflow.Publish` | Publish approved revision |
| Policy/block reason | `CC.P4.Workflow.Policy` / `CC.P4.Workflow.BlockReason` | Print policy status / Why print is blocked |
| History | `CC.P4.Workflow.StepHistory` | Document workflow history |

## Fixture and regression packet

These are proposed fixtures and gates, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| Legacy raw payload with no workflow metadata | Loads as `Unknown`/compatibility diagnostic | Never silently Published; migration decision is visible. |
| Future/unknown workflow schema | Fail-closed diagnostic | Existing project-load unsupported-schema behavior remains intact. |
| Valid Draft -> InReview -> Approved -> Published | Each accepted transition records exact hash and audit event | Invalid edge, duplicate event and stale hash are rejected. |
| Reject/request changes | Reason/comment required according to policy | Durable `Rejected` versus immediate Draft behavior is explicit. |
| Dirty edit or changed file during review | Transition/print preparation is blocked | New revision must be saved and reviewed separately. |
| Audit append or integrity verification failure | Old state remains visible; action fails with repair path | No optimistic new state or policy-on print. |
| Policy Off/On/Invalid | Informational, fail-closed, and configuration-error states match matrix | Preview/prepare/dispatch all revalidate the same state/hash. |
| Approved versus Published print attempt | Owner-approved print rule is enforced without changing preflight | Text/TextBox and output-contract regressions remain unchanged. |
| P5 reprint approval | Existing immutable-manifest path remains independent | Different command, store/key, AutomationId and copy. |
| Figma sample history | Metadata informs density only | Sample identities/dates/comments never become fixtures. |

## No-go list

- Do not default missing workflow metadata to `Published` or treat a file save as approval.
- Do not put a second workflow state machine in `LabelTemplate`, `MainWindow`, Print Center or the P5 reprint store.
- Do not route document `Approve` to `ReprintApproved`, and do not claim that document approval proves queue acceptance or physical output.
- Do not enable transitions for dirty/invalid/stale revisions, failed audit writes or unresolved actor policy.
- Do not silently inherit Published after editing, restoring or replacing the covered revision.
- Do not infer roles, users, ACLs, check-out, scheduling, sync or server governance from Figma labels or sample history.
- Do not add a `Print anyway` bypass in the policy-on slice without an explicit policy/actor decision.
- Do not change Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or print parity.

## Owner sign-off record

Record one owner, date and decision for every row. Blank rows keep CC-P4 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. State vocabulary/transition graph | `TBD` | `TBD` | `TBD` |
| D2. Metadata placement/migration | `TBD` | `TBD` | `TBD` |
| D3. Revision/hash coverage | `TBD` | `TBD` | `TBD` |
| D4. Actor/comment/audit owner | `TBD` | `TBD` | `TBD` |
| D5. Print-policy composition | `TBD` | `TBD` | `TBD` |
| D6. Host/command ownership | `TBD` | `TBD` | `TBD` |
| D7. Scheduling/roles/destructive edges | `TBD` | `TBD` | `TBD` |
| D8. Runtime/Figma/regression owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** The local transition host/store and pure policy evaluator are
implemented. Print-path enforcement remains open until the policy mode,
configuration owner and explicit preview/prepare/dispatch integration are
chosen and covered by fixtures. Until then, P4 is not a Published print gate.
