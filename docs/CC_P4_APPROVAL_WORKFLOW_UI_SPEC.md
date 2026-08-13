# ANLAbel — CC-P4 Approval Workflow UI/UX spec

**Status:** design-only document-state/policy spec; workflow store, actor policy and host remain open (2026-08-13)
**Predecessor:** [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md)
**Handoff:** [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, Workflow `7:2`

This spec maps the Figma Workflow research frame to a future local document-state surface. It does not add a workflow enum, transition store, permission model or Published print gate. Document approval remains distinct from P5 linked-reprint approval and from physical-output verification.

## 1. Operator outcome

The first workflow surface should let an operator:

1. see the selected document identity, revision/hash, dirty state and current workflow state;
2. understand which transition is available and which evidence/role/policy condition blocks it;
3. inspect a durable step history with timestamp, local-operator wording, from/to state, comment and covered revision hash;
4. distinguish document approval from print-job reprint approval and from normal print preflight;
5. see unknown schema, stale revision, audit failure and policy-blocked print as explicit states rather than optimistic Published copy.

## 2. Figma node map (read-only)

Metadata for `7:2` was rechecked read-only on 2026-08-13. The frame supplies candidate vocabulary and density only; sample users, dates and actions are not ANLAbel evidence.

| Figma node | Metadata name / bounds | ANLAbel role | Boundary |
| --- | --- | --- | --- |
| `7:2` | `CC / Documents — Workflow`, `1280 x 800` | Workflow information-architecture reference | Not a WPF size, identity, approval or scheduling contract. |
| `7:23` | Sidebar, `(0,92)`, `220 x 229` | State/filter navigation reference | Root, Approved, Drafts, In Review, Published and Rejected are not current folders or enum values. |
| `7:37` | `WorkflowMain`, `(220,92)`, `1060 x 812` | Main status/detail owner | Content extends below the research frame; WPF must choose one scroll owner. |
| `7:38`–`7:41` | Status header `1020 x 100`; `REQUEST APPROVAL` | Selected document/status header | Action must be state-, revision- and policy-aware; no control is enabled by Figma text alone. |
| `7:42`–`7:53` | Draft → Request approval → Approved → Published | Candidate transition graph | Owner must approve state semantics, covered hash and actor policy. |
| `7:54`–`7:58` | Rejected and Scheduled for publishing alternates | Deferred state vocabulary | Rejection reason may be local; scheduling requires a durable clock/store decision. |
| `7:59`–`7:67` | Approve, Reject, Request changes, Publish now | Candidate action row | No current command service or role policy authorizes these actions. |
| `7:68`–`7:87` | Step history `1020 x 300`: When, User, From, To, Comment | Audit/history density reference | Local print actors are not authenticated workflow users; sample identities are not fixtures. |

The frame has no invalid-file, dirty-edit, stale-review, permission-denied, audit-write-failure, policy-off or policy-blocked-print states. No new Figma node is required for this documentation spec; if a concrete state needs design evidence, follow the [Figma escalation protocol](figma-ui-handoff-template.md#figma-escalation-protocol).

## 3. Proposed document-state contract

Proposal only; implementation requires an owner decision and a versioned migration/audit contract.

| State | Meaning | Candidate next action | Required evidence |
| --- | --- | --- | --- |
| `Draft` | Editable revision not under review | Request approval; edit/save follows the chosen revision rule | Document identity/hash, dirty state, local operator and comment when requested |
| `InReview` | Immutable revision awaiting decision | Approve, Reject/Request changes, or owner-approved cancel | Requested-by actor/time, covered revision hash and comment |
| `Approved` | Decision passed for the covered revision | Publish or explicitly reopen | Approver actor/time/comment and exact revision hash |
| `Published` | Revision allowed by the selected production-print policy | Preview/print if policy allows; edit creates a new reviewable revision | Publisher actor/time, policy version, source approval and hash |
| `Rejected` | Covered revision failed review | Return to Draft / Request changes with reason | Rejector actor/time/reason and covered revision hash |
| `Unknown` | Missing or unrecognized workflow metadata | Inspect, migrate or choose an owner-approved exception | Schema/diagnostic; never infer Published |

Candidate transition graph:

```text
Draft --Request approval--> InReview --Approve--> Approved --Publish--> Published
                              |                    |
                              +--Reject/changes--> Rejected --Return to Draft--+
```

The owner must decide whether Rejected is durable or immediately returns to Draft, whether Approved can be edited in place, and whether Published is mutable state or an immutable release pointer.

## 4. Source and policy boundaries

| UI evidence | Current/future owner | Display rule |
| --- | --- | --- |
| Document identity/schema | `ProjectFileService` saved envelope (`format`, `schemaVersion`, `template`) | Missing workflow metadata is explicit `Unknown`; never default legacy files to Published. Future schema remains fail-closed. |
| Revision/hash/dirty state | P3 `ProjectRevisionService`, validated load and `DocumentSnapshot` | Transition covers one exact validated revision; dirty or changed hash invalidates an in-flight review. |
| Workflow state/transition | Future document-workflow store or sidecar chosen by owner | Keep state events separate from print-job `PrintJobStateStore`; show source, policy version and diagnostics. |
| Actor/comment | Future local identity/actor policy | Until authenticated roles exist, copy says `local operator`; do not imply separation of duties from a free-text actor. |
| Print preflight | Existing `PrintPreflightValidator` and output/queue contracts | Workflow policy composes with preflight; it does not change geometry, Text/TextBox layout or overflow rules. |
| P5 reprint approval | Existing `PrintJobOperatorActionService` and immutable print manifest | Never route document Approve to `ReprintApproved`; different command, ID, store/key and acceptance tests. |
| Policy mode | Future explicit configuration/version | Policy off is informational; policy on may block non-Published only after the owner approves the migration and exception path. Invalid policy configuration never silently becomes off. |

## 5. Host-neutral wireframe

Keep this order whether the host extends P3 library/revision, adds a workflow panel or uses a dedicated local window:

```text
[Document identity | source/path | workflow state | revision hash | dirty/valid]

[State path: Draft -> InReview -> Approved -> Published]
[Rejected / Request changes branch and reason]

[State-aware actions: Request approval | Approve | Reject | Request changes | Publish]
[Policy card: mode/version | print eligibility | exact block reason]

[Step history: UTC/local time | local operator | From | To | revision hash | comment | result]
```

Actions remain hidden or disabled until the state, revision validity, actor policy and audit-write path are ready. The workflow surface never dispatches a printer and never claims physical output.

## 6. Print-policy matrix

| Policy mode | Missing/unknown state | `Draft`/`InReview`/`Approved`/`Rejected` | `Published` | UI behavior |
| --- | --- | --- | --- | --- |
| `Off` | Existing preflight decides | Existing preflight decides | Existing preflight decides | Show workflow as informational; do not claim approval. |
| `On` / fail closed | Block before preview/prepare/dispatch | Block with state, hash and safe next action | Continue normal preflight/queue/output checks | No `Print anyway` bypass unless separately approved. |
| Invalid configuration | Block or refuse to enable production print | Block | Block until repaired | Show configuration source/version; never fall back silently. |

The policy must be evaluated against the same document hash used by preview/manifest/dispatch. A transition or edit after preview invalidates the prepared action. This gate is independent of Text/TextBox sizing, wrapping, clipping, padding and print geometry.

## 7. State and failure matrix

| State | Visible evidence | Safe next action | Fail-closed rule |
| --- | --- | --- | --- |
| `NoWorkflowMetadata` | Status unavailable, file/schema/hash and migration link | Inspect or migrate | Never display Published. |
| `DraftClean` | State, revision hash, last transition | Request approval or edit | Do not submit dirty/unvalidated bytes. |
| `DraftDirty` | Dirty marker and current file identity | Save, discard with confirmation or cancel | No transition may cover unsaved bytes. |
| `InReview` | Requested-by/time, covered hash and comment | Approve, reject/request changes or cancel if allowed | Newer hash invalidates review. |
| `Approved` | Approver/time/comment and covered hash | Publish or reopen per policy | Do not print if current hash differs. |
| `Published` | Publisher/time, policy/version and hash | Preview/print if policy allows | Later edits cannot inherit Published silently. |
| `Rejected` | Reason, actor/time and covered hash | Return to Draft / request changes | Reject is not print cancellation or file deletion. |
| `RoleDenied` | Required role and local-identity wording | Choose allowed operator or cancel | No disabled-control bypass. |
| `AuditWriteFailed` | Store path and actionable error | Retry/repair/cancel | Do not show transition or enable policy-on print. |
| `PolicyBlocked` | State/hash/policy source/version and next action | Open library/review or cancel | No force-print path in this slice. |
| `ResearchSample` | Clearly marked design reference | None | Do not copy sample user/date/status as live data. |

## 8. Responsive behavior and automation vocabulary

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May preserve the `220 DIP` sidebar and `1020 DIP` workflow main proportions; content must fit a deliberate scroll owner because Figma `7:37` extends to `812` high. | Status → state path → action/policy → history focus order. |
| `1024 x 600` | Collapse sidebar to a state filter/drawer; stack status, actions, policy and history; keep block reason visible without page-level horizontal scroll. | Keyboard order remains status → state path → actions → policy → history → confirmation. |
| `100%`, `125%`, `150%` | Reflow or clip only inside declared owners; never blindly scale the Figma frame. | Capture screenshot/UI Automation at each scale and record environment exceptions. |

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P4.Workflow.Root` | `Document approval workflow` |
| Document status | `CC.P4.Workflow.Status` | `Document workflow status` |
| Revision/hash | `CC.P4.Workflow.Revision` | `Reviewed document revision` |
| State path | `CC.P4.Workflow.StatePath` | `Workflow state path` |
| Request approval | `CC.P4.Workflow.RequestApproval` | `Request document approval` |
| Approve | `CC.P4.Workflow.Approve` | `Approve document revision` |
| Reject/request changes | `CC.P4.Workflow.Reject` | `Reject or request changes` |
| Publish | `CC.P4.Workflow.Publish` | `Publish approved revision` |
| Policy card | `CC.P4.Workflow.Policy` | `Print policy status` |
| Block reason | `CC.P4.Workflow.BlockReason` | `Why print is blocked` |
| Step history | `CC.P4.Workflow.StepHistory` | `Document workflow history` |
| Confirmation | `CC.P4.Workflow.Confirm` | `Confirm workflow action` |

## 9. Acceptance gate

Before implementation review closes P4:

- owner approves the state enum, missing-state migration, revision/hash coverage, actor wording, policy mode and durable workflow store;
- fixtures cover legacy/missing metadata, unknown schema, dirty/changed hash, every transition, invalid transition, duplicate/replay event, audit write/verify failure and role denial;
- document approval and P5 reprint approval use different commands, IDs, stores/keys and user-facing copy;
- policy-on/off/invalid configuration and preview/prepare/dispatch revalidation are tested without changing existing preflight or Text/TextBox behavior;
- state, revision hash, actor/comment, policy version and block reason remain visible and source-backed;
- runtime screenshot/UI Automation covers `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus and scroll ownership;
- Figma sample users, dates, folders and statuses never become runtime data without local evidence;
- no workflow action is enabled merely because the research frame contains a matching label.

Until these decisions and gates close, this file is a UI/UX specification, not a shipped approval workflow or Published print gate.
