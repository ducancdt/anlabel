# ANLAbel — Figma → WPF UI/UX handoff template

Use this template for one concrete UI/UX slice. Copy it into the owning plan or issue, fill every required field, and link the resulting runtime evidence. Do not create a new Figma file until the existing references and nodes have been checked.

## 1. Slice identity

| Field | Value |
| --- | --- |
| Slice name | `<!-- concise user-facing name -->` |
| Owner / date | `<!-- person or agent / YYYY-MM-DD -->` |
| User problem | `<!-- what the operator cannot do or understand today -->` |
| In scope | `<!-- one vertical UI workflow -->` |
| Out of scope | `<!-- adjacent screens, backend work, or future phases -->` |
| Related plan | `<!-- Markdown path + section/issue link -->` |

## 2. Existing design reference

| Field | Value |
| --- | --- |
| Figma file URL | `<!-- existing file first; do not paste secrets -->` |
| File key | `<!-- if available -->` |
| Node/frame/component | `<!-- stable node ID and human name -->` |
| Existing asset/crop | `<!-- repository path, if any -->` |
| Why this reference is the right one | `<!-- task, state, and evidence -->` |
| New Figma file needed? | `Yes / No — reason:` |

Known ANLAbel references that should be reused when applicable:

- Full shell recreation: [Figma file](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), full frame `2:2`.
- Frequency-first panels: [Figma file](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), overview `8:2`, selected properties `13:2`, tabs `18:69`.
- Excel link verification component: same panels file, component `22:82`.
- Control Center research shells: [Figma file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4); local crops are under `docs/assets/nicelabel-control-center/ui-screens/`.

Coverage note (metadata checked 2026-08-13, panels file Page `0:1`): the page currently lists frames `1:2`, `4:2`, `8:2`, `13:2`, `18:69`, and `22:82`, with no dedicated Database Manager frame. Do not treat the Excel-link component as a complete Manager design; for a Manager slice, first record the exact workflow (unlink, test connection, preview, use, remove, or cleanup), then locate or create the smallest state-specific reference and map it to WPF controls.

Barcode UI coverage note: the same page has only the compact-ribbon text layer `1:8` (`Text TextBox Image Barcode`) for barcode authoring. That is a navigation hint, not a Properties/state design. P1/P2 barcode software slices are closed; P3 check-digit/HRI, P4 ratio/quiet-zone, P5 2D parity, P6 GS1 diagnostics, P7 print-method review and P8 physical-verifier evidence are routed through their review artifacts. Any P3+ UI slice must provide a state-specific node (or an explicit decision to reuse the existing WPF surface), measured controls, and runtime evidence. Do not infer check-digit policy, HRI display, X-dimension, 2D size/EC semantics, GS1 AI coverage, print-method resolution, verifier grade or preflight copy from that text layer alone.

Control Center coverage note: read-only metadata checked on 2026-08-13 for Control Center Page `0:1` exposes `CC / Overview` `2:2` (`1280 x 800`), `CC / Printers — Print Management` `2:37`, and `CC / History` `3:85`. The Overview frame is a research shell for the CC-P1 operations handoff, not a shipped web/LMS design. Route the local queue/recovery/activation slice through [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md); do not copy its LMS seat totals or server claims into WPF without local evidence.

Control Center program index note: route cross-surface order, action ownership, Figma node routing and evidence gates through [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md). The individual CC-P1 through CC-P8 handoffs remain authoritative for their state matrices; this index coordinates them and does not authorize code or Figma edits.

Shared host gate note: the index now records the existing WPF `Shell.*` automation regions, the `PrintCenterWindow` action owner and the open choice between a MainWindow hub, a dedicated local host or a staged P1 entry point. Figma Control Center frames remain visual input until that host, navigation vocabulary and target-scale runtime evidence are approved.

P1/P2/P5 host review note: use [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md) for the bounded host options and required owner record; the Figma Overview, Printers and History nodes remain read-only references.

P1/P2/P5 data note: use [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md) to keep state-store lineage, operation JSONL, per-label CSV and live queue lookup separate when mapping UI fields.

P1/P2/P5 implementation-gate note: use [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md) to record the single host, canonical read-model owner, queue identity, P1/P2/P5 boundaries, Print Center action owner, AutomationIds and target-scale evidence before adding navigation or WPF UI.

Designer shell/panel/Excel note: use [`DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md`](DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md) for the R1-R7 shell ownership map, the `268/280` versus `300/300` width reconciliation, the `Advanced` label decision and the five-state Excel verification evidence gate. It is documentation-only and does not authorize a shell change, Figma write or Text/TextBox change.

P1 UI spec note: [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md) is the host-neutral mapping from Figma Overview `2:2` to responsive WPF content and proposed AutomationIds; it remains open until a host and runtime evidence are selected.

P1 concrete-owner note: [`CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md`](CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md) records the local queue/recovery/activation card boundary, the missing queue timestamp and recent-terminal-fault aggregate, build-conditional activation behavior, explicit deep-links and D1-D10 fixtures. Reuse Figma `2:2` read-only; do not copy its server, workstation, user or license-seat values.

P2 UI spec note: [`CC_P2_PRINT_QUEUE_UI_SPEC.md`](CC_P2_PRINT_QUEUE_UI_SPEC.md) is the host-neutral, read-only mapping from Figma Printers `2:37` to queue source fields, status scope, responsive table/detail content and proposed AutomationIds; it remains open until a host and runtime evidence are selected.

P2 concrete-owner note: [`CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md`](CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md) narrows M1 to source-backed read-only queue visibility, canonical identity, refresh/error semantics, queue/job scope and explicit deep-links. Reuse Figma `2:37` read-only; defer its command-strip, licensing, workstation and sample-row vocabulary until local contracts exist.

P5 UI spec note: [`CC_P5_HISTORY_REPRINT_UI_SPEC.md`](CC_P5_HISTORY_REPRINT_UI_SPEC.md) is the provenance-first mapping from Figma History `3:85` to three-source activity/detail content, explicit reprint eligibility and proposed AutomationIds; it remains open until a host and runtime evidence are selected.

P5 Print Center recovery note: use [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md) for the existing WPF recovery/reprint action owner, corrupt-tail and exact-manifest gates, approved-preview callback and redacted support export. Figma History `3:85`/`3:109` supplies hierarchy only; it does not authorize a second dispatch path or a new recovery frame.

P3 UI spec note: [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md) is the local-root/built-in browse and revision mapping from Figma Documents `3:2`; it keeps validated load, diff, audit and guarded restore explicit while workflow/check-out/ACL states remain deferred.

P4 UI spec note: [`CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md`](CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md) maps Figma Workflow `7:2` to candidate document-state, actor/audit and policy-on print content; it keeps document approval separate from P5 linked-reprint approval and remains review-only until a store/host/policy decision exists.

P6 UI spec note: [`CC_P6_ANALYTICS_UI_SPEC.md`](CC_P6_ANALYTICS_UI_SPEC.md) maps Figma Analytics `5:2` to source-backed local aggregates and explicit label/job/event units, partial-source states and P5 deep-links; it remains read-only and does not treat sample bars or dimensions as product data.

P6 owner-decision note: [`CC_P6_ANALYTICS_DECISION_PACKET.md`](CC_P6_ANALYTICS_DECISION_PACKET.md) records metric units, CSV/operation/state precedence and conflicts, timezone boundaries, dimension/privacy rules, the physical-output disclaimer, P5 deep-link/export ownership and runtime/Figma fixtures. Complete its D1-D8 sign-off before implementation; the packet does not authorize an Analytics window, telemetry or a Figma write.

Barcode P7 UI spec note: [`P7_PRINT_METHOD_UI_SPEC.md`](P7_PRINT_METHOD_UI_SPEC.md) maps the existing shell Print & Output group `2:39` (Setup `2:41`, Preview `2:44`, Print `2:47`) to an ADR-first graphic/native method surface. Shell node `2:2` has no method, capability or fallback state; keep it read-only until a printer-family pilot and runtime evidence exist.

Barcode P8 UI spec note: [`P8_PHYSICAL_VERIFIER_UI_SPEC.md`](P8_PHYSICAL_VERIFIER_UI_SPEC.md) maps job-level verifier/grade evidence to a future Print Center or History detail surface. Shell node `2:2` and Control Center History `3:85` have no verifier state; keep both read-only until a real fixture/adapter supplies a named state and runtime evidence.

Print Preview/Calibration note: use [`PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md`](PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md) for the existing WPF Preview/Printer Setup ownership, effective-plan/preflight/stale-data states and physical calibration boundary. The shell Print & Output nodes `2:39`/`2:41`/`2:44`/`2:47` are read-only grouping references and do not authorize a new calibration frame.

Barcode UI/UX program note: use [`BARCODE_UI_UX_PROGRAM_INDEX.md`](BARCODE_UI_UX_PROGRAM_INDEX.md) for P3-P8 sequencing, one action owner, shared acceptance gates and the read-only Figma node map. Individual handoffs/specs remain authoritative; this index does not authorize implementation or Figma writes.

P3 owner-decision note: [`P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md`](P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md) records the source-backed Code 39/ITF gap, recommended Code 39-first boundary, `None`/`Auto`/`Verify` and HRI display decisions, legacy migration guardrails, and the explicit reuse-vs-state-specific Figma choice. Complete its D1-D5 sign-off before implementation; the packet does not authorize a Figma write.

P4 owner-decision note: [`P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md`](P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md) records the source-backed ratio/density/physical-QZ gap, the existing logical-module/profile policies, the per-side versus total convention, legacy X=0 boundary and explicit Figma reuse/state-node choice. Complete its D1-D6 sign-off before implementation; the packet does not authorize a Figma write.

P5 owner-decision note: [`P5_2D_BARCODE_PARITY_DECISION_PACKET.md`](P5_2D_BARCODE_PARITY_DECISION_PACKET.md) preserves the shipped QR contract, records the current QR-shaped Data Matrix gap, requires a renderer capability probe, and defines explicit unsupported-control and Figma reuse/state-node choices. Complete its D1-D7 sign-off before implementation; the packet does not authorize a Figma write.

P6 owner-decision note: [`P6_GS1_AI_UI_DECISION_PACKET.md`](P6_GS1_AI_UI_DECISION_PACKET.md) records the strict parser/registry evidence, first AI-family choice, `[FNC1]` boundary copy, curated-versus-official provenance, diagnostics/geometry split and Figma shell reuse/state-node decision. Complete its D1-D7 sign-off before implementation; the packet does not authorize a Figma write or a GS1 certification claim.

P7 owner-decision note: [`P7_PRINT_METHOD_DECISION_PACKET.md`](P7_PRINT_METHOD_DECISION_PACKET.md) records the Graphic/Native model, queue/driver capability scope, explicit fallback, output-contract/parity boundary, manifest migration and pilot/runtime gates. Complete its D1-D8 sign-off before implementation; the packet does not authorize native output or a Figma write.

P8 owner-decision note: [`P8_PHYSICAL_VERIFIER_DECISION_PACKET.md`](P8_PHYSICAL_VERIFIER_DECISION_PACKET.md) records the job-level host, hash-only verifier/grade boundary, manifest/content binding, adapter correlation/timeout/busy states, completion rule, redaction/signature policy and lab/runtime gates. Complete its D1-D8 sign-off before implementation; the packet does not authorize a device SDK, certification claim or Figma write.

CC-P3 owner-decision note: [`CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md) records local-root cardinality, Built-in/local source precedence, host/action ownership, validated file/thumbnail states, revision retention, compare/restore/dirty-edit policy and the CC-P4 workflow boundary. Complete its D1-D8 sign-off before implementation; the packet does not authorize a browser, workflow controls or a Figma write.

CC-P4 owner-decision note: [`CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md`](CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md) records the candidate state graph, envelope/migration and exact-hash coverage, local actor/audit boundary, policy-on print composition, host/action ownership and deferred scheduling/roles. Complete its D1-D8 sign-off before implementation; the packet does not authorize workflow controls, a Published gate or a Figma write.

P7 UI spec note: [`CC_P7_ADMINISTRATION_UI_SPEC.md`](CC_P7_ADMINISTRATION_UI_SPEC.md) maps Figma Administration `5:41` to local activation/preferences/data-source/evidence links and a future retention preview; it keeps roles, users, sync, server licenses and unsupported categories out of the local desktop contract.

P7 owner-decision note: [`CC_P7_ADMINISTRATION_DECISION_PACKET.md`](CC_P7_ADMINISTRATION_DECISION_PACKET.md) records thin-host/action ownership, local activation/preferences/registry boundaries, evidence and retention-preview safety, privacy/security rules and unsupported server-category treatment. Complete its D1-D8 sign-off before implementation; the packet does not authorize roles, sync, server licenses, destructive retention or a Figma write.

P8 UI spec note: [`CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md`](CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md) maps Figma Applications `7:88` to a deferred local file-drop trigger contract, explicit lifecycle/claim/provenance states and P5 deep-links; web apps, TCP, cloud, login and unattended printing remain out of scope.

P8 owner-decision note: [`CC_P8_AUTOMATION_DECISION_PACKET.md`](CC_P8_AUTOMATION_DECISION_PACKET.md) records the first-trigger prerequisite, one host/lifecycle owner, claim/deduplication and restart semantics, configuration/provenance schema, shared preflight/manifest/queue spine, History/privacy and deferred TCP/web/security scope. Complete its D1-D8 sign-off before implementation; the packet does not authorize a trigger runner, unattended printing or a Figma write.

R4 data-surface owner-decision note: [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md) records one shared source/connector identity, separate Data Workspace and Database Manager action owners, transform draft/commit and fail-closed rules, Manager async/mutation safety, proposed AutomationIds, read-only Figma reuse and target-scale runtime fixtures. Complete D1-D10 before implementation/runtime closure; the packet does not authorize a transform editor, registry rewrite, new Manager frame or Figma write.

Database Manager concrete-owner note: [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](DATABASE_MANAGER_UI_DECISION_PACKET.md) records the existing WPF Manager/Cleanup source states, focus-loss persistence, async wait-cursor/late-result gap, relink/use/remove/fallback rules and proposed UIA fixtures. Reuse Figma `8:2`/`9:2` read-only; do not create a Manager frame merely to fill the metadata gap.

Data Workspace concrete-owner note: [`R4_DATA_WORKSPACE_UI_DECISION_PACKET.md`](R4_DATA_WORKSPACE_UI_DECISION_PACKET.md) records the current read-only transform list, bounded formula/lineage owner, atomic draft/Apply boundary, invalid/stale fail-closed rule, proposed UIA fixtures and the explicit raw-fallback gap in `CreatePreviewRow`. Reuse Figma `8:2`/`9:2` read-only; do not infer a transform editor, sample table or lineage frame from the shell metadata.

## 3. Contract and behavior

CC-P2 routing note: Control Center Printers metadata `2:37` is a research shell with a `220 DIP` filter rail and `1000 DIP` main pane. Route the local read-only queue slice through [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md); its Pause/Resume/Delete/Reserve/Unreserve labels are deferred command concepts, not current ANLAbel capabilities.

CC-P5 routing note: Control Center History metadata `3:85` supplies a `1248 x 56` filter bar and `1248 x 600` activity frame, with a note for details/reprint/errors but no concrete child states. Route the local three-source read model and exact-manifest reprint gate through [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md); do not treat the Figma sample rows as live history or physical-output evidence.

CC-P3 routing note: Control Center Documents metadata `3:2` supplies the browse/storage shell (toolbar `3:16`, folder rail `3:19`, files pane `3:29`, sample cards `3:31`–`3:84`). Workflow metadata `7:2` is a separate CC-P4 research state (`7:23`, `7:38`–`7:87`), not an ANLAbel implementation. Route local root/folder/search/preview/revision questions through [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md); do not infer workflow, check-out, ACL, or multi-user behavior from the research shell.

CC-P4 routing note: Control Center Workflow metadata `7:2` supplies candidate Draft → Request approval → Approved → Published vocabulary, Rejected/Scheduled alternates, actions and step-history density. Route document-state, actor/audit, migration and policy-on print-gate questions through [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md); keep template approval separate from P5 linked-reprint approval and do not treat Figma sample users or dates as live evidence.

CC-P8 routing note: Control Center Applications metadata `7:88` is a Web Applications shell with an Automation sidebar (`7:109`–`7:123`) but no trigger-detail/configuration state; History activity frame `3:101` is the provenance destination. Route local file-drop trigger, lifecycle, deduplication, manifest and History questions through [`CC_P8_AUTOMATION_UI_HANDOFF.md`](CC_P8_AUTOMATION_UI_HANDOFF.md); do not infer a trigger runner, login policy, cloud integration, TCP listener or unattended-print capability from the web-app research shell.

CC-P6 routing note: Control Center Analytics metadata `5:2` supplies a chart region `5:16` and filter pane `5:31`, but no source-health, partial-data, empty, detail or physical-verification states. Route local CSV/JSONL/hash-chain aggregation, unit/provenance, timezone, redaction and software-counter disclaimer questions through [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md); do not copy Figma sample bars or call local counters physical output.

CC-P7 routing note: Control Center Administration metadata `5:41` supplies a broad server-admin sidebar `5:55` and role table `5:69`, but current ANLAbel evidence is limited to local activation, preferences, data-source registry/cleanup and local logs. Route local-admin host, retention, privacy and unsupported-category questions through [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md); do not infer roles, users, sync, SMTP, workflow administration or license-seat server behavior from the research shell.

### User-visible states

| State | Trigger | Visible evidence | Safe next action | Failure/empty behavior |
| --- | --- | --- | --- | --- |
| Default | `<!-- initial condition -->` | `<!-- labels, values, status -->` | `<!-- primary action -->` | `<!-- no-data rule -->` |
| Loading | `<!-- action in progress -->` | `<!-- progress/disabled state -->` | `<!-- cancel/retry rule -->` | `<!-- timeout rule -->` |
| Success | `<!-- verified condition -->` | `<!-- durable evidence -->` | `<!-- next action -->` | `<!-- stale rule -->` |
| Error | `<!-- failure condition -->` | `<!-- actionable error -->` | `<!-- repair/relink action -->` | `<!-- fail-closed behavior -->` |

### WPF mapping

| Design region | WPF surface/control | AutomationId or stable name | Data/command owner | Notes |
| --- | --- | --- | --- | --- |
| `<!-- node name -->` | `<!-- XAML/window/control -->` | `<!-- AutomationId -->` | `<!-- ViewModel/service -->` | `<!-- parity constraint -->` |

### Protected behavior check

- [ ] The change does not alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow, or designer/preview/print parity.
- [ ] Existing authored label geometry and data are preserved; no silent migration is introduced.
- [ ] Any explicitly requested contract change has its decision record and regression gates updated together.

## 4. Measurement and accessibility target

Record the target before implementation; do not accept a visual match that only works at one scale.

| Target | Required value/evidence |
| --- | --- |
| Minimum window | `1024 × 600` (or explain why the slice differs) |
| Display scale | `100%`, `125%`, `150%` (plus observed OS DPI if relevant) |
| Text/icon clipping | `none`; record the screenshot or UI Automation result |
| Keyboard path | `<!-- focus order, shortcut, Escape/Enter behavior -->` |
| Screen-reader/name | `<!-- AutomationId/Name/HelpText expectations -->` |
| Contrast/disabled state | `<!-- state-specific check -->` |
| Scroll behavior | `<!-- one intentional scroll owner; no accidental nesting -->` |

## 5. Evidence package

Figma is a design input. Runtime evidence closes the slice.

- [ ] Figma node review recorded (URL, node ID, state, and measured dimensions).
- [ ] WPF runtime screenshot captured at the target window size and display scales.
- [ ] UI Automation or equivalent stable-control check recorded where applicable.
- [ ] Named application regression added or updated for the user-visible behavior.
- [ ] Unit/contract test added when the change introduces a pure policy or geometry rule.
- [ ] Build and both test commands pass, with counts copied into the owning Markdown checkpoint.
- [ ] Print/preview parity checked when the slice affects label output; otherwise the exclusion is documented.
- [ ] Physical-printer or verifier evidence is explicitly marked open when not available.

Recommended command evidence:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 6. Read-only Figma metadata snapshot

The current Excel-link UI reference was checked through Figma metadata on 2026-08-13. This confirms the design structure and node names only; it does not prove WPF runtime behavior, typography, accessibility, or print parity.

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `22:82` | `Excel Link Verification` | `(4924, 0)` | `620 × 455` |
| `22:5` | `State=Not linked` | `(0, 0)` | `284 × 125` |
| `22:22` | `State=Checking` | `(312, 0)` | `284 × 125` |
| `22:37` | `State=Verified` | `(0, 153)` | `284 × 125` |
| `22:52` | `State=Stale` | `(312, 153)` | `284 × 125` |
| `22:67` | `State=Failed` | `(0, 306)` | `284 × 125` |

When implementing this slice, preserve the five-state model and then attach independent WPF/runtime evidence to each state.

The full shell reference was also checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `2:2` | `ANLAbel — Full Shell v1` | `(80, 80)` | `1440 × 900` |
| `2:3` | `R1 Quick Access` | `(0, 0)` | `1440 × 52` |
| `2:23` | `R2 Ribbon` | `(0, 52)` | `1440 × 90` |
| `2:80` | `R3 Toolbox` | `(12, 12)` | `250 × 271` |
| `2:109` | `R4 Workspace` | `(12, 291)` | `250 × 421` |
| `2:123` | `R5 Design Surface` | `(268, 0)` | `880 × 724` |
| `2:132` | `R6 Object Properties` | `(1148, 0)` | `292 × 724` |
| `2:170` | `R7 Status Bar` | `(0, 866)` | `1440 × 34` |

The shell metadata exposes the intended WPF mapping: `Shell.QuickAccess`, `Shell.Ribbon`, `Shell.Toolbox`, `Shell.Workspace`, `Shell.Canvas`, `Shell.Properties`, and `Shell.Status`. The Figma QA label currently reads `GPL-3.0 · v0.201`; treat that as a design-reference version and reconcile it with the product release snapshot before changing either artifact.

The frequency-first panel reference was checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `8:2` | `ANLAbel — Frequency-first Panels v0.198` | `(3260, 0)` | `664 × 788` |
| `8:4` | `Panel pair` | `(24, 64)` | `616 × 700` |
| `8:5` | `Workspace panel` | `(0, 0)` | `300 × 700` |
| `8:6` | `Properties panel` | `(316, 0)` | `300 × 700` |
| `8:15` | `Workspace tabs` | `(0, 48)` | `300 × 42` |
| `9:2` | `Data tab content` | `(0, 90)` | `300 × 610` |
| `9:45` | `Properties empty state content` | `(0, 48)` | `300 × 652` |

The reference makes `Layers` and `Data` real tabs, with `Data` active, a compact `No data linked` action, collapsed data settings, and binding checks. It also exposes a documentation discrepancy: the Figma reference uses `300 DIP` panels, while `docs/industrial-panel-design.md` currently documents `268 DIP` Workspace and `280 DIP` Properties. Keep this as an open reconciliation item; do not change WPF dimensions from metadata alone without a runtime screenshot and an owning design decision.

The selected-object Properties reference was checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `18:69` | `ANLAbel — Properties tabs v0.200` | `(4504, 0)` | `300 × 700` |
| `18:70` | `Properties header` | `(0, 0)` | `300 × 48` |
| `18:71` | `Selected object summary` | `(0, 48)` | `300 × 68` |
| `18:72` | `Properties tabs` | `(0, 116)` | `300 × 38` |
| `18:73` | `Label tab content` | `(0, 154)` | `300 × 546` |
| `18:94` | `Text Box behavior card` | `(8, 182)` | `284 × 126` |
| `18:95` | `Typography quick card` | `(8, 315)` | `284 × 174` |

The Label tab prioritizes Content, `Wrap in fixed frame`, `Block print and warn`, and typography. The node name for the third tab is `Properties tab / More`, while the current product notes call that tab `Advanced`; keep the naming discrepancy open until the owner chooses the final label. The Text Box behavior controls are contract-sensitive and must not be changed merely to match a visual reference.

The compact selected-object reference was checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `13:2` | `ANLAbel — Properties selected v0.199` | `(4044, 0)` | `300 × 700` |
| `13:7` | `Selected object content` | `(0, 48)` | `300 × 652` |
| `13:8` | `Selected object summary` | `(12, 12)` | `276 × 62` |
| `15:2` | `Content card` | `(12, 82)` | `276 × 150` |
| `15:16` | `Text Box behavior` | `(12, 240)` | `276 × 137` |
| `15:32` | `Utility · Position and size · collapsed` | `(12, 385)` | `276 × 48` |
| `15:41` | `Utility · Advanced · collapsed` | `(12, 441)` | `276 × 48` |

The selected summary explicitly says `Fixed frame · wraps and clips at bounds`; the behavior card exposes `Auto wrap`, `Print boundary: Clip`, and a fit status. This is direct design evidence for the protected TextBox contract. It also explains the naming evolution: v0.199 uses a collapsed `Advanced` utility section, while v0.200 uses a third tab named `More`. Choose the intended revision before implementation instead of treating the labels as interchangeable.

## 7. Current continuation decisions (2026-08-13)

This table routes the open UI/UX findings already backed by the read-only metadata above. It is not a release claim and does not authorize code or Figma edits by itself.

| Surface | Current status | Evidence | Decision / next owner action |
| --- | --- | --- | --- |
| Excel link verification | Owner packet plus existing implementation evidence | Component `22:82`; five-state contract is recorded in [`DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md`](DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md) and current verification checkpoint | Reuse the existing reference and MainWindow state owner. Any further UI change still needs runtime screenshot/automation evidence for each state. |
| Data Workspace authoring/diagnostics | Owner packet open; needs design review | Figma `8:2`/`9:2` provides the Data-tab shell, empty source `9:3`, current context `9:16`, collapsed settings `9:27` and binding checks `9:35`; no transform editor, sample table, lineage or invalid-state variant is present. Review [`R4_DATA_WORKSPACE_UI_DECISION_PACKET.md`](R4_DATA_WORKSPACE_UI_DECISION_PACKET.md) alongside [`R4_DATA_WORKSPACE_UI_SPEC.md`](R4_DATA_WORKSPACE_UI_SPEC.md). | Follow [`R4_DATA_WORKSPACE_UI_HANDOFF.md`](R4_DATA_WORKSPACE_UI_HANDOFF.md): reuse the shell only, then approve WPF reuse or name a state-specific node before adding controls. |
| Shell and frequency-first panels | Owner packet open; baseline recommendation recorded | Figma `8:2` reports `300/300 DIP`; current WPF/design note records Workspace `268` and Properties `280`; see [`DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md`](DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md) | Keep WPF `268/280` as the working baseline. Do not change widths until the packet's owner decision and target-scale runtime measurement resolve the competing values. |
| Properties third task label | Owner packet open; baseline recommendation recorded | Figma node `18:69` uses `More`; current WPF exposes `Advanced`; the compact reference also uses `Advanced`; see [`DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md`](DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md) | Keep `Advanced` as the operator-facing label. Rename only after the owner chooses one label and updates automation/acceptance names together. |
| Database Manager | Existing WPF workflow; owner/runtime gate open | Panels Page `0:1` has no Manager frame; current WPF `DatabaseManagerWindow`/`DataSourceCleanupWindow` owns list/detail, Test Connection, Preview, Use, Remove and guarded Cleanup states. Review [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](DATABASE_MANAGER_UI_DECISION_PACKET.md) with [`DATABASE_MANAGER_UI_SPEC.md`](DATABASE_MANAGER_UI_SPEC.md). | Reuse current WPF information architecture and Figma `8:2`/`9:2` shell language; close with async snapshot/registry/cleanup fixtures and target-scale UIA evidence before any redesign. |
| CC-P2 Print Queue Console | M1 owner packet open; host remains upstream | Figma `2:37` supplies filter rail `2:51`, main pane `2:72`, command/search/table/footer samples; source supplies local/connection discovery, saved queue lookup and one-job spool observation. Review [`CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md`](CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md) with [`CC_P2_PRINT_QUEUE_UI_SPEC.md`](CC_P2_PRINT_QUEUE_UI_SPEC.md). | Keep M1 read-only and host-neutral until [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md) selects the host; do not enable Figma command labels or copy research sample values. |
| Barcode P3 authoring | Deferred pending a state reference | Page `0:1` metadata scan (2026-08-13) finds only ribbon text layer `1:8`; no barcode Properties/check-digit/HRI state exists. P1/P2 software evidence is closed and P3 is check-digit/HRI display policy. The reviewable UI contract is [`P3_BARCODE_CHECK_DIGIT_UI_SPEC.md`](P3_BARCODE_CHECK_DIGIT_UI_SPEC.md). | Use `18:69`/`13:2` as interim shell language only; owner must explicitly approve reuse or provide a state-specific node, then add runtime evidence and regression coverage. |
| Barcode P4 ratio / density / physical quiet zone | Deferred pending contract and state review | Existing Properties card exposes X-dimension and logical quiet-zone modules, but no ratio, derived density or physical-mm quiet-zone state. P4 review artifacts are [`P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md) and [`P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md). | Reuse `18:69`/`13:2` only for grouping/status language; approve the first symbology, QZ side/total convention and runtime evidence before changing the panel. |
| Barcode P5 2D parity | Deferred pending renderer/contract review | Current WPF exposes QR-named sizing/EC controls across QR and Data Matrix; source has QR capacity/ECC and shared matrix module warnings, but no first-class DM size/EC fields. Figma `18:69`/`13:2` has no 2D barcode state. Review [`P5_2D_BARCODE_PARITY_UI_HANDOFF.md`](P5_2D_BARCODE_PARITY_UI_HANDOFF.md) and [`P5_2D_BARCODE_PARITY_UI_SPEC.md`](P5_2D_BARCODE_PARITY_UI_SPEC.md). | Reuse the selected-Properties shell only for grouping and status language. Confirm supported DM semantics, unavailable-control treatment, QR copy and runtime evidence before changing the panel. |
| Barcode P6 GS1 AI diagnostics | Deferred pending subset/contract review | Current WPF exposes `Application profile` and one combined validation string. Core has strict `(AI)value` parsing, FNC1 normalization, versioned curated/official registry data and fail-closed preflight; no AI rows, boundary/FNC1 view or registry provenance card exists. Figma `13:2`/`18:69` has no GS1 state. Review [`P6_GS1_AI_UI_HANDOFF.md`](P6_GS1_AI_UI_HANDOFF.md) and [`P6_GS1_AI_UI_SPEC.md`](P6_GS1_AI_UI_SPEC.md). | Reuse the selected-Properties shell only for grouping/status density. Confirm demanded AI classes, `[FNC1]` copy, registry update policy and runtime evidence before adding a diagnostics surface; do not claim a full wizard or certification. |
| Barcode P7 print method | Deferred pending ADR/capability/pilot review | Current `PrintService` dispatches WPF `PrintDocument` through the app-rendered graphic path; `PrintRenderPlan`/`EffectiveOutputContract` bind queue/ticket/DPI/media/imageable-area evidence, while `PrintJobManifest.PrintMode` is only a workflow label and no native adapter/method state exists. Figma shell `2:2`/`2:39` has no method/capability/fallback state. Review [`P7_PRINT_METHOD_UI_HANDOFF.md`](P7_PRINT_METHOD_UI_HANDOFF.md), [`P7_PRINT_METHOD_UI_SPEC.md`](P7_PRINT_METHOD_UI_SPEC.md) and [`P7_PRINT_METHOD_DECISION_PACKET.md`](P7_PRINT_METHOD_DECISION_PACKET.md). | Keep Graphic as the evidenced baseline; approve method vocabulary, explicit fallback, durable fields, host and printer-family pilot before exposing Native. |
| Barcode P8 physical verifier | Deferred pending hardware/adapter/lab review | Core already separates queue/preflight from physical evidence: `PhysicalOutputVerificationEvidence` is hash-only and manifest-bound; the adapter enforces identity/correlation/timeout/busy; `PrintJobStateMachine` requires accepted scanner/verifier evidence for `Completed`. Print Center has no verifier controls, and Figma History `3:85` has no verifier state. Review [`P8_PHYSICAL_VERIFIER_UI_HANDOFF.md`](P8_PHYSICAL_VERIFIER_UI_HANDOFF.md), [`P8_PHYSICAL_VERIFIER_UI_SPEC.md`](P8_PHYSICAL_VERIFIER_UI_SPEC.md) and [`P8_PHYSICAL_VERIFIER_DECISION_PACKET.md`](P8_PHYSICAL_VERIFIER_DECISION_PACKET.md). | Keep queue/spool/preflight/golden/visual evidence explicitly unverified; approve one lab fixture, adapter owner, grade policy, host and redaction/signature rule before displaying any physical claim. |
| CC-P3 Document Library + Revision | Deferred pending root/host/revision policy review | Current WPF has an embedded Template Library plus validated primary/`.bak`/`.revisions` inspection, semantic diff and guarded restore; no configured local-root browser or workflow/check-out/ACL state exists. Figma Documents `3:2` supplies browse/folder/card density only; selected-file, invalid, diff, restore and dirty-edit states are missing. Review [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md), [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md) and [`CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md). | Keep Built-in/local identity, validated load, one revision owner and explicit restore confirmation; defer workflow/check-out/ACL and do not infer them from Figma. |
| CC UI/UX program map | Coordination source; not release evidence | [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md) connects CC-P1 through CC-P8, the dependency order, action owners and read-only Figma routes. | Use the program index for cross-surface sequencing; close each individual handoff only with its own runtime/evidence gate. |
| CC-P1 Operations Overview | Owner packet open; host remains upstream | Control Center `2:2` gives the Overview shell, license/workstation/error card hierarchy and nav; current WPF has `PrintCenterWindow` recovery plus queue/activation/deep-link primitives, but no unified overview, queue timestamp envelope or recent-terminal-fault aggregate. Review [`CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md`](CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md) with [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md). | Follow [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md): keep the overview read/route-only, choose the upstream host and approve local source/time/privacy contracts before adding cards or changing Figma. |
| CC-P2 Print Queue Console | Roadmap; M1 read-only slice needs product/design review | Control Center `2:37` gives the Printers shell, filter rail, table and command vocabulary; current WPF has discovery, saved-queue lookup and one-job spool observation, but no fleet table or command service | Follow [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md): approve the read-only host, status taxonomy and local queue evidence before enabling any command strip. |
| CC-P5 History + controlled reprint | Roadmap; needs read-model/design review | Control Center `3:85` gives filters/activity/detail affordances; current WPF has CSV history, job JSONL, hash-chained state/recovery and guarded reprint services, but no unified browser | Follow [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md): define provenance/retention/time semantics and keep Request → Approve → Prepare → Dispatch explicit. |
| CC-P5 Print Center recovery owner | Existing WPF action surface; owner/runtime gate open | `PrintCenterWindow` owns recovery refresh, queue reconciliation, acknowledge/void, linked reprint request/approval, approved-preview callback and redacted support export; it has no stable AutomationIds or target-scale click-through evidence | Follow [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md): reuse the WPF action owner, close with corrupt-tail/exact-manifest fixtures and target-scale UIA evidence, and keep Figma History read-only. |
| CC-P3 Document Library + Revision | Roadmap; needs product/design review | Control Center Documents `3:2` gives browse/folder/card density and Workflow `7:2` gives deferred state vocabulary; current WPF has embedded Template Library plus local primary/`.bak`/`.revisions` inspection, semantic diff and validated restore, but no local-root browser or workflow enum | Follow [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md): choose root/host/preview/revision entry points, keep invalid revisions fail-closed, and defer CC-P4 workflow/check-out/ACL decisions. |
| CC-P4 Approval Workflow | Roadmap; needs product/design review | Workflow `7:2` gives candidate states/actions/history; current source has versioned template envelope, preflight and hash-chained print-job events but no typed document workflow, actor/role policy or Published print gate | Follow [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md): approve state/migration/audit/policy contract first; keep document approval distinct from linked-reprint approval and defer scheduling/multi-user identity. |
| CC-P8 Applications / Automation | Deferred; design-only | Applications `7:88` gives Web Applications cards/share language plus Automation filters `7:109`–`7:123`; History `3:101` gives activity density; current source has only an Excel freshness watcher and manual manifest/preflight/queue path, not a trigger host | Follow [`CC_P8_AUTOMATION_UI_HANDOFF.md`](CC_P8_AUTOMATION_UI_HANDOFF.md): approve one local file-drop contract, lifecycle/provenance and WPF host after CC-P1/P2/P5/P4 prerequisites; defer TCP/web/login/cloud scope. |
| CC-P6 Local Analytics | Roadmap; needs product/design review | Analytics `5:2` gives chart/filter density; current source has per-label CSV, best-effort operation JSONL and hash-chained job state, but no Analytics UI or authoritative cross-source aggregate | Follow [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md): approve units/source precedence/timezone/redaction/host first; keep read-only and label software counters rather than physical verification. |
| CC-P7 Administration | Roadmap; needs product/design review | Administration `5:41` gives server-admin categories and role table; current source has local activation/DPAPI state, designer/printer preferences, versioned data-source registry/cleanup and local logs, but no multi-user admin service | Follow [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md): choose thin local host, ownership links, retention/recovery and privacy rules; defer roles/users/sync/SMTP/license-seat server claims. |
| Print Preview / Calibration | Existing WPF workflow; UI/UX owner gate open | `PrintPreviewWindow` owns preview/rows/preflight/print and `PrinterSetupWindow` owns queue/media/DPI/orientation; shell `2:39` only has Setup/Preview/Print grouping and no calibration/plan state. Review [`PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md`](PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md). | Reuse the current WPF surfaces; close only with target-scale/UIA evidence, effective-plan/preflight/stale-data fixtures and a separate physical calibration record for any hardware claim. |
| Text/TextBox behavior | Protected contract | [`AGENTS.md`](../AGENTS.md) and [`NICELABEL_TEXTBOX_RESEARCH.md`](NICELABEL_TEXTBOX_RESEARCH.md) | Do not use a visual reference to alter ownership, sizing, wrapping, clipping, padding, resize lifecycle or print parity without an explicit contract change. |

**Decision rule:** a Figma frame is design input only. The owning slice remains open until the target window/display-scale screenshot or UI Automation measurement, named regression, and relevant build/test evidence are attached. Use [`10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md) for cross-surface ownership and [`11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md) for the current dirty-worktree boundary.

### Figma escalation protocol

Connect to Figma only when a concrete UI/UX question cannot be answered by the current WPF source, this template, or an existing read-only node:

1. State the operator task and missing state/measurement first; do not browse for visual inspiration alone.
2. Inspect metadata read-only and record the file URL, page, node ID, node name, measured bounds and state represented.
3. Map the node to an owning WPF surface, stable AutomationId proposal and local source/action owner. Treat sample data, server copy and license totals as research content unless local evidence proves them.
4. If no node answers the state question, record an explicit WPF reuse decision or request the smallest state-specific Figma reference. Do not create a new file or duplicate a shell merely to fill an evidence gap.
5. Close the UI slice with target-scale runtime screenshot/UI Automation, keyboard/focus/scroll evidence, named regression and relevant build/test output. A Figma node alone never closes acceptance.

For the current Control Center sequence, route host questions through [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md), then use [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md) to preserve one node/state owner per downstream slice.

## 8. Handoff decision

Choose exactly one and explain it:

- [ ] **Ready for implementation** — reference, mapping, states, and acceptance evidence are complete.
- [ ] **Needs design review** — list the unresolved node/state/measurement question.
- [ ] **Blocked by external evidence** — identify the printer, driver, verifier, or workstation evidence required.
- [ ] **Deferred** — link the owning roadmap item and explain why this slice is not next.

**Decision note:** `<!-- one paragraph; no “done” claim without the evidence above -->`
