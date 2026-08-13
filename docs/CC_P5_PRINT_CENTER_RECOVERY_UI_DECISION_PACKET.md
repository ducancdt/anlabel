# Print Center and recovery UI owner decision packet

**Status:** documentation-only owner gate; the existing WPF recovery surface remains the only action owner and no new History host, reprint path, Figma edit or Text/TextBox change is authorized (2026-08-13)
**P5 handoff:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md)
**P5 content spec:** [`CC_P5_HISTORY_REPRINT_UI_SPEC.md`](CC_P5_HISTORY_REPRINT_UI_SPEC.md)
**P1/P2/P5 host/read-model gate:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Figma routing:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Print Preview boundary:** [`PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md`](PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and boundary

The checkout contains a concrete WPF `PrintCenterWindow` for recovery, operator decisions, linked reprint approval, guarded preview and redacted support export. CC-P5 still describes a future provenance-first History browser. This packet makes the existing action owner reviewable without promoting the recovery dialog into a History browser or inventing a second dispatch stack.

This packet covers:

- the current Print Center host, entry/focus path and one-owner action boundary;
- recovery snapshot, filtering, selection retention and event-log diagnostics;
- queue reconciliation, acknowledge, void, linked reprint request and approval semantics;
- the approved-preview callback and fresh manifest/dispatch boundary;
- redacted support-evidence export and physical-output non-claims;
- responsive, keyboard/focus, AutomationId and regression closure gates.

It does not add a History read model, merge CSV/JSONL/state sources, add queue commands, add automatic retry, send a printer cancellation for `Void`, infer physical completion, edit Figma, or alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or print parity. Blank owner rows keep this slice open.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Host and action ownership | Keep `PrintCenterWindow` as the sole local owner for recovery and operator actions. A future History surface may deep-link here; it must not duplicate buttons or dispatch. | Confirm the host/return path and one owner for every mutation before navigation work. |
| D2. Recovery snapshot and filtering | `RefreshPrintRecoveryAsync` loads the valid state-store projection; `PrintRecoveryCandidateFilter` filters a snapshot and preserves a selected `JobId` when it remains visible. | Approve refresh cadence, stale-snapshot copy and selection behavior when a candidate disappears. |
| D3. Diagnostics and corrupt tail | Show `StoreDiagnostics` and keep the valid prefix useful for support, but classify every candidate as `RepairEventLog` when the append log is damaged. | Approve repair/archive wording and the support owner; no UI action may bypass the repair boundary. |
| D4. Queue reconciliation | `Reconcile` is an explicit queue observation requiring a safe printer and spool identity. It records queue evidence only and never authorizes retry or physical completion. | Confirm timeout copy, queue-state vocabulary and the owner for a follow-up operator decision. |
| D5. Acknowledge and void | `Acknowledge` records review; `Void` records a terminal lineage decision without sending a printer cancellation. Both remain explicit, auditable actions. | Approve actor/reason capture and the confirmation copy for `Void`. |
| D6. Request reprint | `Request reprint` creates a linked `Created` child and preserves parent/child lineage; it does not prepare or dispatch. | Confirm whether a reason is required and where the child/parent relationship is shown. |
| D7. Approve reprint | Approval requires the child to be awaiting approval and the exact immutable manifest fingerprint to match. Approval is not dispatch or physical output. | Approve mismatch wording, reviewer identity and whether a detail view is required before approval. |
| D8. Preview and dispatch | `Open approved preview` is a host callback only. The existing `MainViewModel` dispatch path rebuilds current rows/template/queue/DPI/output data and blocks any exact-manifest mismatch. | Confirm the return path and the owner that starts preparation/dispatch after preview. |
| D9. Support evidence | Export the redacted durable-job bundle for any selected candidate, preserve a fingerprint and state explicitly that physical completion is not claimed. | Approve export location/privacy wording and support-retention handling. |
| D10. Accessibility and closure | Keep current WPF controls as the working baseline, add stable AutomationIds only through an approved implementation slice, and close with target-scale/UIA plus source fixtures. | Name UIA/QA owners and attach runtime evidence, tests and a clean checkpoint. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L1-L12) | The current dialog is `1180 x 720`, has a `900 x 520` minimum, centers on its owner and receives a window-level keyboard hook. | These dimensions are not a target-scale acceptance measurement and do not imply a Control Center shell. |
| [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L20-L50) | Refresh, job search and result-count copy are separate controls; the search box receives focus on load. | Search text is not a canonical History filter contract or a source merge. |
| [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L53-L76) | The grid is read-only, single-select and exposes job/state/action/printer/spool/queue/manifest/reason columns. | A grid row does not expose the complete three-source P5 History detail model. |
| [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L78-L105) | Details and explicit Reconcile/Acknowledge/Void/Request/Approve/Preview/Support actions share one action region. | Button presence does not prove safe enablement, keyboard order or runtime click-through. |
| [`PrintCenterWindow.xaml.cs`](../src/ANLAbel.App/PrintCenterWindow.xaml.cs#L36-L97) | Load/refresh reads a recovery report, reapplies the filter and tries to retain the selected job identity. | There is no cancel button or source-by-source History refresh contract yet. |
| [`PrintCenterWindow.xaml.cs`](../src/ANLAbel.App/PrintCenterWindow.xaml.cs#L99-L159) | F5 refresh, Escape filter/grid navigation, Enter job lookup and Ctrl+Enter approved-preview behavior are deliberately distinct. | Keyboard behavior still needs target-scale/UIA evidence. |
| [`PrintCenterWindow.xaml.cs`](../src/ANLAbel.App/PrintCenterWindow.xaml.cs#L161-L332) | Reconcile, acknowledge, void, request and approve call `MainViewModel` owners and refresh after each result; preview is guarded by approved action and manifest. | Code paths do not prove user comprehension or the future History host decision. |
| [`PrintCenterWindow.xaml.cs`](../src/ANLAbel.App/PrintCenterWindow.xaml.cs#L334-L402) | Support export is file-backed, asynchronous, redacted and fingerprinted through `PrintSupportEvidenceContract`. | An export fingerprint is not a physical-label or barcode-verifier result. |
| [`PrintCenterWindow.xaml.cs`](../src/ANLAbel.App/PrintCenterWindow.xaml.cs#L433-L502) | Details expose durable state/queue/manifest copy; `SetBusy` disables all mutations during work; action enablement depends on lifecycle/action/manifest. | Current `AutomationProperties.Name` values are not stable `AutomationId` evidence. |
| [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs#L19-L107) | Recovery candidates are non-terminal, diagnostics are preserved and `AutomaticRetryAllowed` is always false. | A recovery candidate is not a permission to reprint. |
| [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs#L172-L325) | Corrupt tails force `RepairEventLog`; queue identity and terminal queue states produce explicit operator decisions. | Queue observation cannot prove physical output. |
| [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs#L63-L187) | Request creates a linked child without dispatch; approval checks the exact immutable manifest and still does not dispatch. | Approval is not preparation, spool acceptance or physical completion. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1091-L1240) | Refresh, reconcile, acknowledge, void, request and approve all refresh the report and publish operator-facing status. | The view model does not choose the future P5 History host. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1242-L1324) | Approved dispatch rebuilds the current manifest and blocks count, queue, DPI, design, output-contract or data-fingerprint drift. | A UI link must not bypass this exact-manifest guard. |
| [`Program.cs`](../src/ANLAbel.Tests/Program.cs#L8159-L8212) and recovery unit tests | Support evidence redaction and recovery/operator-action contracts have named software coverage. | No test here proves a WPF click-through, target-scale layout or physical printer result. |

## Surface and action ownership

| Surface/action | Current owner | Safe action | Boundary |
| --- | --- | --- | --- |
| Recovery refresh/report | `MainViewModel.RefreshPrintRecoveryAsync` + `PrintJobRecoveryService` | Load a new valid snapshot and preserve diagnostics | Never turn an empty/failed read into a clean no-history claim. |
| Search/filter | `PrintCenterWindow` + `PrintRecoveryCandidateFilter` | Filter in-memory candidates and retain identity | No log mutation, source merge or dispatch. |
| Queue reconciliation | `MainViewModel.ReconcilePrintJobAsync` + `PrintJobRecoveryService` | Append an observation for a safe queue/spool identity | No automatic retry or physical-output claim. |
| Acknowledge | `PrintJobOperatorActionService.AcknowledgeAsync` | Append an auditable operator decision | Does not mark output complete. |
| Void | `PrintJobOperatorActionService.VoidAsync` | Append a terminal lineage decision | Sends no printer cancellation command. |
| Linked reprint request | `PrintJobOperatorActionService.RequestReprintAsync` | Create a `Created` child linked to the parent | Does not prepare or dispatch. |
| Linked reprint approval | `PrintJobOperatorActionService.ApproveReprintAsync` | Record approval after exact-manifest match | Does not dispatch or certify output. |
| Approved preview | `PrintCenterWindow` callback to existing preview host | Open only a valid approved child | Missing callback is a visible blocked state, not a fallback dispatch. |
| Approved dispatch | Existing `MainViewModel.DispatchApprovedPrintJobReprintAsync` | Rebuild and compare the current exact manifest, then use normal print ownership | History/Print Center must not create a second dispatch stack. |
| Support export | `PrintCenterWindow.BuildSupportEvidence` + `PrintSupportEvidenceContract` | Write redacted JSON and show fingerprint | No raw label payload and no physical-verifier claim. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Opening/loading | Busy state; refresh/grid/actions disabled | Wait for the report | No mutation may run against a partially refreshed snapshot. |
| No candidates | `0 jobs` and the report summary | Refresh or continue printing | Empty recovery is not proof that no label exists elsewhere. |
| Filtered candidates | `n/total match`, selected identity when available | Inspect or clear the filter | Filtering never edits durable sources. |
| Selected queue-reconcile candidate | Printer/spool identity, queue state and Reconcile enabled | Query the queue, then decide | Observation is not retry or physical completion. |
| Missing queue identity | Reason explains that an operator must decide | Acknowledge, void or inspect | Reconcile stays disabled; no default queue substitution. |
| Operator-decision candidate | Durable reason, state and action controls | Acknowledge, void or request a linked reprint | No automatic retry path. |
| Corrupt/incomplete event tail | Diagnostics in details and `RepairEventLog` reason | Repair/archive through support ownership | Reconcile, reprint request and approval must remain blocked until the log is safe. |
| Terminal candidate | Read-only evidence and support export | Inspect/export only | Do not mutate a terminal job. |
| Reprint not requested | Request enabled only for an actionable non-approved candidate | Create one linked child | No row-level preparation or dispatch. |
| Reprint requested | Related child and manifest validity visible | Approve or stop | Approval requires the exact immutable manifest. |
| Approval mismatch/invalid manifest | Actionable mismatch or invalid-manifest message | Refresh current inputs or cancel | No force/ignore-mismatch bypass. |
| Reprint approved | Approved child and valid manifest | Open guarded preview; continue through existing owner | Preview callback is not dispatch. |
| Preview callback missing | Explicit `approved-preview action` unavailable message | Close or return to host | Never substitute a print action. |
| Support export running/failed | Busy state or actionable file error | Retry export explicitly | Export failure does not alter lineage. |
| Action failure | Warning with service summary; report can refresh again | Repair/inspect and retry only explicitly | No green status or hidden retry. |

## Figma metadata boundary

Read-only metadata was rechecked on 2026-08-13 for the existing [Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `3:85` (`CC / History`, `1280 x 800`). The metadata contains:

| Node | Metadata | Safe reuse | Missing state |
| --- | --- | --- | --- |
| `3:85` | History frame, `1280 x 800` | Information-architecture and active-navigation reference | No local recovery dialog, queue action state or exact-manifest gate |
| `3:99` | Filter frame, `(16,104)`, `1248 x 56` | Filter-density reference | No local source, timezone or unknown-state semantics |
| `3:101` | Activity frame, `(16,176)`, `1248 x 600` | Read-only activity/detail hierarchy | No WPF recovery action region or corrupt-tail state |
| `3:102`/`3:103` | Table header with Submitted/Type/Module/Workstation/User/Status/Details | Column-language reference only | Sample fields are not local data and do not define the PrintCenter grid |
| `3:104`-`3:108` | Sample activity rows | Empty/density reference only | User, workstation, dates and statuses must not become fixtures without local evidence |
| `3:109` | Activity details / Reprint / Errors note | Affordance for a future History detail/deep-link | No concrete reprint child, approval mismatch or recovery state |

**Routing decision:** keep the existing WPF `PrintCenterWindow` as the recovery/reprint action owner and keep the Figma History shell read-only. Do not call `get_design_context`, create a new frame or copy Figma sample values for this documentation-only slice. If the future History implementation needs a state-specific reference, first name the missing state and the smallest node, then close it with runtime evidence.

## Accessibility and responsive gate

The current XAML supplies `AutomationProperties.Name` for the main controls but no stable `AutomationProperties.AutomationId`. The following names are proposals only:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Window/root | `CC.P5.PrintCenter.Root` | Print Center / Recovery |
| Refresh | `CC.P5.PrintCenter.Refresh` | Refresh print recovery |
| Search | `CC.P5.PrintCenter.Search` | Scan or search print job |
| Filter summary | `CC.P5.PrintCenter.FilterSummary` | Matching recovery jobs |
| Jobs grid | `CC.P5.PrintCenter.JobsGrid` | Print recovery jobs |
| Details | `CC.P5.PrintCenter.Details` | Selected durable job evidence |
| Reconcile | `CC.P5.PrintCenter.ReconcileQueue` | Reconcile selected queue |
| Acknowledge | `CC.P5.PrintCenter.Acknowledge` | Acknowledge selected job |
| Void | `CC.P5.PrintCenter.Void` | Void selected job |
| Request | `CC.P5.PrintCenter.RequestReprint` | Request linked reprint |
| Approve | `CC.P5.PrintCenter.ApproveReprint` | Approve linked reprint |
| Preview | `CC.P5.PrintCenter.OpenApprovedPreview` | Open approved preview |
| Support export | `CC.P5.PrintCenter.ExportSupportEvidence` | Export redacted support evidence |

Runtime evidence must cover `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception), keyboard order from Refresh/Search through grid/details to actions, visible focus after refresh/action dialogs, selection retention after refresh/filter, a single table scroll owner and no unintended horizontal clipping. The future History surface may use a different responsive host, but it must deep-link to these owners rather than copy their actions.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `PrintCenter_NoSources_isNotPhysicalNoOutput` | Empty/absent state-store data remains an explicit empty recovery report. | View-model fixture and runtime copy. |
| `PrintCenter_FilterRetainsSelectedJobId` | Refresh/filter retains the selected identity when the candidate remains visible and clears it safely when it disappears. | Filter fixture plus UIA capture. |
| `PrintCenter_CorruptTail_disablesRecoveryActions` | Valid prefix is visible for support, but candidates are classified `RepairEventLog` and mutations stay blocked. | State-store fixture and details screenshot. |
| `PrintCenter_ReconcileRequiresSafeQueueIdentity` | Missing printer/spool identity cannot call the queue observer; safe identity appends observation only. | Recovery-service tests and action-state capture. |
| `PrintCenter_VoidDoesNotCancelPrinter` | Void appends a terminal lineage event and sends no printer command. | Operator-action fixture and operation log assertion. |
| `PrintCenter_RequestCreatesLinkedChild` | Request creates one linked `Created` child without preparation or dispatch. | Parent/child event fixture. |
| `PrintCenter_ApproveRequiresExactManifest` | Mismatch/invalid manifest blocks approval; exact immutable manifest records approval only. | Operator-action tests and warning copy. |
| `PrintCenter_ApprovedPreviewRequiresHostCallback` | Approved valid child opens only through the host callback; absent callback is a visible warning. | Window click-through/UIA evidence. |
| `PrintCenter_SupportExportRedactsPayload` | Any selected candidate exports a fingerprinted bundle without raw label payload or physical-output claim. | Existing redaction test and saved JSON inspection. |
| `PrintCenter_BusyDisablesMutations` | Refresh/action work disables grid and all mutation/export buttons until completion. | Runtime screenshot and focus evidence. |
| `Protected_TextTextBox_contract_unchanged` | Recovery/reprint UI work changes no Text/TextBox ownership, frame geometry, wrap/clip, padding, resize or print parity. | Protected regression suite after any implementation change. |

## No-go list

- Do not turn `PrintCenterWindow` into the P5 History browser without the P1/P2/P5 host/read-model decision.
- Do not add a History-row dispatch/reprint shortcut, a second manifest builder or a second queue-success definition.
- Do not interpret queue observation, spool acceptance, `Completed`, support-export success or Figma sample rows as physical output or verifier evidence.
- Do not let `Void` send a printer cancellation, let `Acknowledge` mark output complete or let recovery classification authorize automatic retry.
- Do not approve a linked reprint without exact immutable-manifest equality or add a force/ignore-mismatch path.
- Do not place raw `LabelContent`/`RowData` in the grid or support bundle.
- Do not add native print-method controls or physical-verifier controls from this packet; P7/P8 gates remain authoritative.
- Do not edit Figma or alter the protected Text/TextBox contract for this documentation-only owner gate.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the packet open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Host and action ownership | `TBD` | `TBD` | `TBD` |  |
| D2. Snapshot/filter/selection policy | `TBD` | `TBD` | `TBD` |  |
| D3. Diagnostics/corrupt-tail repair boundary | `TBD` | `TBD` | `TBD` |  |
| D4. Queue reconciliation semantics | `TBD` | `TBD` | `TBD` |  |
| D5. Acknowledge/void actor and copy | `TBD` | `TBD` | `TBD` |  |
| D6. Linked reprint request | `TBD` | `TBD` | `TBD` |  |
| D7. Exact-manifest approval | `TBD` | `TBD` | `TBD` |  |
| D8. Approved preview/dispatch return path | `TBD` | `TBD` | `TBD` |  |
| D9. Support export/privacy | `TBD` | `TBD` | `TBD` |  |
| D10. UIA/runtime/QA closure | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the Print Center recovery owner slice may move from documentation review to implementation/release evidence only after D1-D10 are filled, one action owner exists per mutation, corrupt-tail and exact-manifest fixtures pass, target-scale UIA/screenshots are attached and physical claims remain backed by a separate verifier/calibration record. Until then this is an open local action contract and makes no History, printer-certification or physical-output claim.
