# CC-P5 History + controlled reprint UI owner decision packet

**Status:** staged read-only History host/read-model implemented; no second reprint/dispatch stack, Figma write or Text/TextBox change is authorized (2026-08-13)
**P5 handoff:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md)
**P5 content spec:** [`CC_P5_HISTORY_REPRINT_UI_SPEC.md`](CC_P5_HISTORY_REPRINT_UI_SPEC.md)
**Existing recovery/action owner:** [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md)
**Host/read-model gate:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Read-model contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Figma routing:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and boundary

CC-P5 already has a provenance-first handoff/spec and a concrete WPF `PrintCenterWindow`
recovery owner. This packet closes the remaining owner gap for a future History browser/read
model: what is projected, which source wins, how unknown or corrupt evidence is shown, and how a
selected row reaches controlled reprint without becoming a second dispatch surface.

This packet remains the decision record. The current CSV shortcut, best-effort operation trace and
hash-chained state store are now projected read-only by `PrintHistoryViewModel`; Print Center remains
the sole action owner. The implementation adds no queue command, reprint button or Figma node.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. History host/detail owner | Choose one host through the upstream P1/P2/P5 packet. The History region is read-only and owns projection, filtering and detail; the existing Print Center remains the sole recovery/reprint mutation owner. | Name the host, root/content/detail owner and return path before XAML/navigation work. |
| D2. Source precedence | Use the read-model contract: valid state-store lineage first, operation JSONL supplemental, per-label CSV detail/export, live queue lookup separate. Preserve source badges, conflicts and diagnostics. | Approve the canonical projection schema and one service/view-model owner. |
| D3. Identity and granularity | Use `JobId` for job rows and `RelatedJobId` for reprint lineage. A CSV row without a durable job key remains a `CsvLabelRecord`; never fabricate a job by timestamp, template or printer. | Approve join/display keys and the selected-detail relationship rule. |
| D4. Time, filters and privacy | Prefer `TimestampUtc`; expose `TimestampLocal` only with its basis. Keep Figma `User`/`Workstation` unavailable until a named local source and privacy rule exist. | Approve timezone, retention, redaction and filter semantics. |
| D5. Unknown, stale and corrupt evidence | Keep source-specific unavailable/error states visible. A valid state prefix may be read for support, but a corrupt tail blocks append and controlled reprint. | Approve repair/archive copy, diagnostics owner and stale thresholds. |
| D6. Detail and export | Detail may show lineage, hashes, manifest validity, queue observations, actor/reason and related jobs. CSV/Excel and redacted support evidence remain explicit links/exports; raw label payloads stay out of the activity table and support bundle. | Name detail/export owners and approve redaction/retention. |
| D7. Controlled reprint | History shows eligibility and deep-links only. Request -> Approve -> Prepare -> Dispatch stays explicit in `PrintCenterWindow`/`MainViewModel`; no row-level dispatch shortcut or automatic retry. | Approve the return path and one owner for each stage. |
| D8. Manifest and physical claims | Approval requires exact immutable-manifest equality. Dispatch rebuilds and revalidates current inputs. Queue/spool observations, support export and ordinary operation outcomes do not prove physical output. | Approve mismatch copy and the separate physical-verifier boundary. |
| D9. Figma route | Reuse read-only metadata from History `3:85` for hierarchy/density only. Request a smallest state-specific reference only when a concrete local state is missing. | Name the missing state and runtime evidence before any Figma escalation or write. |
| D10. Closure | Close the owner gate only after source fixtures, privacy/diagnostic copy, target-scale UIA/screenshots and existing state/recovery/operator-action/manifest tests are attached. | Name product, read-model, UIA, QA and checkpoint owners. |

## Current source evidence

| Source/owner | What exists now | History implication |
| --- | --- | --- |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs#L199-L220) | Replays the latest valid event per job and returns store diagnostics. | Lifecycle/lineage authority; snapshot is not a retry signal. |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs#L245-L281) | Keeps the valid prefix when a malformed/invalid tail is encountered and records a diagnostic. | Show the prefix plus warning; do not flatten the read into clean empty history. |
| [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs#L81-L115) | Reports pending candidates/diagnostics and hard-codes `AutomaticRetryAllowed` to false. | Recovery status can be linked, never silently converted to reprint permission. |
| [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs#L140-L190) | Filters terminal events and classifies damaged logs as `RepairEventLog`. | Preserve non-terminal, terminal and repair states separately. |
| [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs#L5-L77) | Appends job-level JSONL under local app data; write failures are best-effort and swallowed. | Missing operation evidence is an honest partial-source state, not proof of no print. |
| [`PrintOperationLogEntry.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogEntry.cs#L11-L64) | Carries job, template/queue, spool, outcome, hashes, manifest, actor/action and support fingerprint fields. | Project machine evidence with field-level provenance; do not treat it as lifecycle authority. |
| [`PrintLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintLogService.cs#L1-L65) | Appends one row per label to `%AppData%/ANLAbel/print-history.csv` and exports a user-requested `.xlsx`. | Keep CSV at label granularity and expose it as detail/export, not a job ledger. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs#L735-L757) | Current History button opens the CSV and a separate command exports Excel. | A future browser must deliberately replace or sit beside this shortcut; do not claim it is already a browser. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L342) | Exposes the human-facing history path. | Show the path/source status only when the selected host has a privacy-approved copy policy. |
| [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs#L63-L119) | Request creates a linked `Created` child and does not dispatch. | Keep parent/child lineage and actor/reason visible. |
| [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs#L122-L187) | Approval requires an exact valid immutable manifest and still does not dispatch. | History may show eligibility/deep-link only; no bypass or force action. |
| [`MainViewModel.cs`](../src/ANLAbel.App/ViewModels/MainViewModel.cs#L1220-L1313) | Approved dispatch rebuilds current rows/template/queue/DPI/output data and blocks drift. | A reprint link must return to this fresh exact-manifest guard. |
| [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L20-L105) | Existing `1180 x 720` dialog owns refresh, read-only grid, details and explicit action buttons. | Reuse/deep-link this action owner; do not duplicate its controls in History. |
| [`PrintCenterWindow.xaml.cs`](../src/ANLAbel.App/PrintCenterWindow.xaml.cs#L48-L97) | Refresh reapplies a filter and retains selected `JobId` when still visible. | History must define the same identity-retention rule for its own snapshot. |
| [`PrintCenterWindow.xaml.cs`](../src/ANLAbel.App/PrintCenterWindow.xaml.cs#L433-L497) | Details show durable evidence; busy state disables grid/actions; export remains available for selected jobs. | Preserve fail-closed action enablement and redacted export semantics. |

## Projection and action ownership

| Surface | Owner | Contract |
| --- | --- | --- |
| History refresh/read model | Future P5 History service/view model, after D1/D2 | Read valid source snapshots, retain per-source status/diagnostics and publish immutable rows. No log mutation. |
| Lifecycle/lineage | `PrintJobStateStore` | Latest valid event, event history, `RelatedJobId`, actor/action and manifest evidence. Corrupt-tail diagnostic blocks mutations. |
| Supplemental job outcome | `PrintOperationLogService` / `PrintOperationLogEntry` | Job-level outcome, spool observations and hashes; unavailable or write-loss stays visible. |
| Per-label detail/export | `PrintLogService` | CSV/Excel remains user-directed detail/export. Never infer a job identity from a label row. |
| Live queue context | Existing queue lookup/reconciliation owners | Separate observation with printer/spool identity and timestamp; never rewrite historical state. |
| Recovery actions | `PrintCenterWindow` + `MainViewModel` | Reconcile, acknowledge, void, request, approve, guarded preview, prepare/dispatch and support export. One owner only. |
| Host/navigation | Upstream `CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md` | Select one root/return path and stable `CC.*` IDs; no navigation implementation is implied here. |
| Visual reference | Figma `3:85` metadata | Hierarchy/density only. Sample users, dates, workstations and statuses are not local fixtures. |

## State and failure matrix

| State | Visible evidence | Safe next action | Fail-closed rule |
| --- | --- | --- | --- |
| Opening/loading | Busy state, source list and last successful refresh | Wait, cancel if supported or refresh | Do not replace stale rows with an empty success state. |
| No source file | Per-source path/status and `No activity recorded` | Refresh or open setup/help | Empty files do not prove that no label was printed elsewhere. |
| Mixed sources | Source badge, timestamp basis and field-level provenance | Filter or select | CSV, operation JSONL and state events are not automatically one record. |
| Operation trace unavailable | State/CSV rows remain with an explicit missing-trace warning | Inspect available detail or retry a read | Best-effort log loss is not a clean success/no-history result. |
| CSV unavailable/partial | Job activity remains; label-detail link says unavailable/partial | Open source status or export when available | Do not fabricate label counts or payloads. |
| Filter no match | Query summary and Clear action | Clear or adjust filters | No match is not evidence that a job never existed. |
| Selected active/unknown | Job ID, lifecycle, queue/spool observation, reason and source diagnostics | Inspect or open Print Center | Queue/spool acceptance is not physical output. |
| Selected terminal | Durable terminal event and any verifier evidence separately | Inspect/export only | Do not mutate a terminal job or equate `Completed` with generic queue completion. |
| Corrupt/incomplete tail | Valid prefix plus repair warning and diagnostic | Repair/archive through support ownership | Disable append, reprint request and approval until the log is safe. |
| Reprint not requested | Eligibility explanation and link to Print Center | Request one linked child explicitly | Request does not prepare or dispatch. |
| Reprint requested | Parent/child IDs, actor/reason and captured manifest fingerprint | Review in Print Center and approve or stop | Approval is not dispatch or physical completion. |
| Approval blocked | Exact missing/mismatch fields (counts, queue, DPI, design/data/output hashes) | Refresh current inputs or cancel | Never offer force/ignore-mismatch. |
| Reprint approved | Approved child, valid immutable manifest and guarded preview link | Preview, then continue via existing owner | History never dispatches from a list row. |
| Support export running/failed | Busy state or redacted file/fingerprint/error | Retry explicitly | Export failure does not alter lineage or prove output. |
| Source conflict | Both values, source labels and timestamp basis | Inspect detail and escalate policy decision | Do not silently choose a greener value. |

## Figma metadata boundary and routing

Read-only metadata was rechecked on 2026-08-13 for the existing [NiceLabel Control Center research
file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, node `3:85` (`CC / History`,
`1280 x 800`):

| Node | Metadata | Safe reuse | Missing local proof |
| --- | --- | --- | --- |
| `3:85` | History frame, `1280 x 800` | Activity/filter/detail hierarchy | No host, source merge or WPF scale mandate. |
| `3:86` / `3:89` | Top bar `1280 x 48`; navigation `1280 x 40` | Optional chrome/navigation density | No local sign-out/help or module identity owner. |
| `3:99` | Filters `(16,104)`, `1248 x 56` | Filter-bar density | No local date/timezone, module, workstation or user source. |
| `3:101` | Activity frame `(16,176)`, `1248 x 600` | Read-only table/detail hierarchy | No source badges, corrupt-tail state or recovery action semantics. |
| `3:102` / `3:103` | Header `1248 x 32`; Submitted/Type/Module/Workstation/User/Status/Details | Column-language reference | Sample columns do not define local fields or privacy approval. |
| `3:104`–`3:108` | Example activity rows | Density/empty-state reference only | Example users, dates, workstations and statuses are not fixtures. |
| `3:109` | Activity details / Reprint / Errors note | Deep-link/detail affordance | No concrete local reprint child, approval mismatch or error state. |

**Routing decision:** use this metadata read-only; do not call `get_design_context`, create/edit a
Figma node or copy sample values for this documentation-only gate. If implementation later needs a
missing state, record the state question, source owner and smallest requested node in the
[Figma escalation protocol](figma-ui-handoff-template.md#figma-escalation-protocol), then close it
with runtime evidence.

## Interaction, accessibility and responsive gate

The activity table is read-only. Selection, filtering, CSV detail and support export do not mutate
the event store. History links to Print Center; only the existing action owner may run
`Request -> Approve -> Prepare -> Dispatch`.

| Region/control | Proposed AutomationId | Accessible name / requirement |
| --- | --- | --- |
| Root | `CC.P5.History.Root` | Print history; host chosen upstream. |
| Refresh/source status | `CC.P5.History.Refresh` / `CC.P5.History.SourceStatus` | Refresh print history; source availability and last refresh are announced. |
| Filters/search | `CC.P5.History.Filters` / `CC.P5.History.Search` | Filter/search print history; keyboard Clear remains reachable. |
| Activity table | `CC.P5.History.ActivityTable` | Print activity; single selection and stable `JobId` identity. |
| Selected detail/lineage | `CC.P5.History.Detail` / `CC.P5.History.Lineage` | Selected job evidence / job lineage; unknown values stay explicit. |
| Reprint eligibility/links | `CC.P5.History.ReprintEligibility` / `CC.P5.History.OpenPrintCenter` | Eligibility copy and Open Print Center; no row dispatch. |
| Export/detail links | `CC.P5.History.OpenCsvDetail` / `CC.P5.History.ExportSupportEvidence` | Open label history detail / Export redacted support evidence. |

Runtime review must cover `1024 x 600`, `100%`, `125%` and `150%` (or an environment exception),
keyboard order from refresh/source status through filters/table/detail/links, focus restoration
after refresh, one table scroll owner, and no page-level horizontal clipping. These are proposals;
they are not stable implementation IDs until the host owner signs off.

## Fixture and regression packet

These names describe the required implementation evidence; this documentation-only change adds no
tests and does not claim a click-through.

| Fixture/regression | Required assertion |
| --- | --- |
| `History_NoSourcesPreservesSourceStatus` | Absent/empty state, operation and CSV sources remain explicit; no physical-no-output claim. |
| `History_MergesWithFieldProvenance` | State lineage wins lifecycle; operation JSONL supplements; CSV remains label detail; conflicts stay visible. |
| `History_CorruptTailKeepsValidPrefix` | Valid prefix is readable, diagnostic is visible and append/reprint approval is blocked. |
| `History_TimestampBasisIsExplicit` | UTC/local/unknown timestamps are not silently compared or converted. |
| `History_DoesNotFabricateCsvJobIdentity` | A CSV-only record is label detail, not a synthetic job row. |
| `History_FilterRetainsStableSelection` | Refresh/filter retains a selected identity only when it remains in the snapshot. |
| `History_DeepLinksToPrintCenterOwner` | History has no duplicate mutation path and preserves Request -> Approve -> Prepare -> Dispatch. |
| `History_ExactManifestMismatchBlocks` | Missing/invalid/count/queue/DPI/design/data/output drift blocks approval or dispatch with named fields. |
| `History_SupportExportRemainsRedacted` | Export carries a fingerprint and no raw label payload or physical-output claim. |
| `History_TargetScaleAndUIA` | Screenshots/UIA prove focus, selection, scroll and accessible names at target scales. |
| Existing state/recovery/action/manifest suites | Keep [`PrintJobStateStoreTests.cs`](../src/ANLAbel.UnitTests/PrintJobStateStoreTests.cs), [`PrintJobRecoveryServiceTests.cs`](../src/ANLAbel.UnitTests/PrintJobRecoveryServiceTests.cs), [`PrintJobOperatorActionServiceTests.cs`](../src/ANLAbel.UnitTests/PrintJobOperatorActionServiceTests.cs), [`PrintJobManifestTests.cs`](../src/ANLAbel.UnitTests/PrintJobManifestTests.cs) and [`PrintSupportEvidenceContractTests.cs`](../src/ANLAbel.UnitTests/PrintSupportEvidenceContractTests.cs) green. |

## No-go list

- Do not turn `PrintCenterWindow` into the History browser without the upstream host/read-model decision.
- Do not add a second CSV/JSONL/state merge store, queue-success definition, manifest builder or dispatch stack.
- Do not infer joins from timestamps, template names, printer names or Figma sample rows.
- Do not hide missing/best-effort/corrupt source evidence behind `0 jobs`, green status or a physical-output claim.
- Do not expose raw `LabelContent`/`RowData` in the activity table or support bundle.
- Do not let History mutate logs, send printer commands, authorize automatic retry or bypass exact-manifest validation.
- Do not edit Figma or alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or print parity.

## Owner sign-off record

Blank rows keep this packet open. Record one owner, date, option and evidence link per decision.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. History host/detail owner | `TBD` | `TBD` | `TBD` |  |
| D2. Source precedence/read-model owner | `TBD` | `TBD` | `TBD` |  |
| D3. Identity/granularity/join policy | `TBD` | `TBD` | `TBD` |  |
| D4. Time/filter/privacy policy | `TBD` | `TBD` | `TBD` |  |
| D5. Unknown/stale/corrupt-tail handling | `TBD` | `TBD` | `TBD` |  |
| D6. Detail/export/redaction owner | `TBD` | `TBD` | `TBD` |  |
| D7. Request/approve/prepare/dispatch return path | `TBD` | `TBD` | `TBD` |  |
| D8. Exact-manifest/physical-claim boundary | `TBD` | `TBD` | `TBD` |  |
| D9. Figma escalation/routing | `TBD` | `TBD` | `TBD` |  |
| D10. UIA/runtime/QA closure | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** move CC-P5 from documentation review to implementation/release evidence only
after D1–D10 are filled, the History host and one action owner are named, source/provenance/privacy
fixtures pass, corrupt-tail and exact-manifest gates remain fail-closed, target-scale UIA/screenshots
are attached and physical claims remain backed by a separate verifier/calibration record. Until then
this is an open local owner contract, not a shipped History browser, reprint certification or
physical-output claim.
