# ANLAbel — current verification checkpoint (2026-08-13)

**Status:** evidence snapshot; not a release approval
**Documentation checkpoint:** `96f6ab5` (`docs: reconcile barcode execution status`)
**Scope:** record the current source/test/version evidence without staging the broad implementation wave already present in the worktree.

## Why this file exists

`MASTER_PLAN.md` and `PLAN.md` contain a large uncommitted implementation wave. Their historical entries and existing edits must remain intact, so this checkpoint records the current evidence in an additive file. After the implementation owner selects and commits the intended scope, copy the verified snapshot into the release-history sections of those two files; do not infer that the current dirty checkout is releasable.

## Current evidence

| Evidence | Result | Boundary |
| --- | --- | --- |
| Display/source version | `0.263` is canonical in `eng/Version.props` and projected to app metadata, shell title/build-channel text, Help and public Commercial/Trial installer metadata | The private License Master installer intentionally remains `1.0.0`; version parity is source/build evidence, not a signed release artifact while those source/installer files are dirty. |
| `dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false` | PASS · 0 warnings · 0 errors | Compile evidence for the current checkout. |
| `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet` | PASS · 356/356 | Unit/contract evidence; no physical-device claim. |
| `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build` | PASS · exit 0; 158 registered checks in the current runner | Application regression evidence, including the Operations Overview stale-refresh/scope gate. Named P1/P2 barcode gates and the protected Text/TextBox regression names are present in the runner; this is not a hardware smoke test. |
| Repository-local Markdown link audit | PASS · 57 Markdown files, 209 relative links checked, 0 broken paths | Relative paths/assets only; external URLs still require network/source review. |
| Worktree ownership | OPEN · broad dirty implementation wave and untracked research/code/assets remain | This checkpoint must not be used to stage or release those files. |

**Superseding docs-only Markdown audit (2026-08-13):** `64` Markdown files, `329` relative links checked, and `0` broken paths after the CC-P3 handoff. The earlier `57`/`209` row above remains the original checkpoint baseline; this rerun checks repository-local paths only, not external URLs.

**Superseding docs-only Markdown audit after CC-P4 (2026-08-13):** `65` Markdown files, `347` relative links checked, and `0` broken paths. The `64`/`329` figures above are the preceding CC-P3 audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after CC-P8 (2026-08-13):** `66` Markdown files, `365` relative links checked, and `0` broken paths. The `65`/`347` figures above are the preceding CC-P4 audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after CC-P6 (2026-08-13):** `67` Markdown files, `383` relative links checked, and `0` broken paths. The `66`/`365` figures above are the preceding CC-P8 audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after CC-P7 (2026-08-13):** `68` Markdown files, `406` relative links checked, and `0` broken paths. The `67`/`383` figures above are the preceding CC-P6 audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the CC UI/UX program index (2026-08-13):** `69` Markdown files, `446` relative links checked, and `0` broken paths. The `68`/`406` figures above are the preceding CC-P7 audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the P1/P2/P5 host decision packet (2026-08-13):** `70` Markdown files, `470` relative links checked, and `0` broken paths. The `69`/`446` figures above are the preceding CC UI/UX program audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the Figma escalation protocol (2026-08-13):** `70` Markdown files, `472` relative links checked, and `0` broken paths. The `70`/`470` figures above are the preceding host decision packet audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the P1/P2/P5 read-model contract (2026-08-13):** `71` Markdown files, `484` relative links checked, and `0` broken paths. The `70`/`472` figures above are the preceding Figma escalation protocol audit; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the P1 Operations Overview UI spec (2026-08-13):** `72` Markdown files, `494` relative links checked, and `0` broken paths. The `71`/`484` figures above are the preceding read-model contract audit; external URLs remain outside this local-path check.

The documentation-only [`CC_P2_PRINT_QUEUE_UI_SPEC.md`](../CC_P2_PRINT_QUEUE_UI_SPEC.md) maps read-only Printers metadata `2:37` to local queue evidence, queue/job state scope, responsive table/detail behavior and proposed AutomationIds. It does not authorize a queue console, queue mutation, host choice or Figma edit.

**Superseding docs-only Markdown audit after the P2 Print Queue UI spec (2026-08-13):** `73` Markdown files, `504` relative links checked, and `0` broken paths. The `72`/`494` figures above are the preceding P1 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P5_HISTORY_REPRINT_UI_SPEC.md`](../CC_P5_HISTORY_REPRINT_UI_SPEC.md) maps read-only History metadata `3:85` to a provenance-first activity/detail surface, separate CSV/job/state evidence and explicit controlled-reprint stages. It does not authorize a History host, queue/reprint mutation, runtime read-model code or a Figma edit.

**Superseding docs-only Markdown audit after the P5 History + reprint UI spec (2026-08-13):** `74` Markdown files, `514` relative links checked, and `0` broken paths. The `73`/`504` figures above are the preceding P2 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md) maps read-only Documents metadata `3:2` to local-root/built-in browse, validated revision evidence and guarded restore. It does not authorize a document browser, workflow/check-out/ACL mutation, runtime implementation or a Figma edit.

**Superseding docs-only Markdown audit after the P3 Document Library + Revision UI spec (2026-08-13):** `75` Markdown files, `524` relative links checked, and `0` broken paths. The `74`/`514` figures above are the preceding P5 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md`](../CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md) maps read-only Workflow metadata `7:2` to candidate document states, actor/audit boundaries and policy-on print blocking rules. It does not authorize a workflow store, permission change, Published gate, runtime implementation or Figma edit; P5 linked-reprint approval remains distinct.

**Superseding docs-only Markdown audit after the P4 Approval Workflow UI spec (2026-08-13):** `76` Markdown files, `534` relative links checked, and `0` broken paths. The `75`/`524` figures above are the preceding P3 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P6_ANALYTICS_UI_SPEC.md`](../CC_P6_ANALYTICS_UI_SPEC.md) maps read-only Analytics metadata `5:2` to source-backed local metrics, explicit units, partial-source/unknown states and P5 History deep-links. It does not authorize telemetry, an Analytics window, physical-output claims, runtime implementation or Figma edit.

**Superseding docs-only Markdown audit after the P6 Local Analytics UI spec (2026-08-13):** `77` Markdown files, `544` relative links checked, and `0` broken paths. The `76`/`534` figures above are the preceding P4 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P7_ADMINISTRATION_UI_SPEC.md`](../CC_P7_ADMINISTRATION_UI_SPEC.md) maps read-only Administration metadata `5:41` to local activation/preferences/data-source/evidence links and retention-preview safety. It does not authorize roles/users/sync/server-license features, destructive retention, runtime implementation or Figma edit.

**Superseding docs-only Markdown audit after the P7 Administration UI spec (2026-08-13):** `78` Markdown files, `555` relative links checked, and `0` broken paths. The `77`/`544` figures above are the preceding P6 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md`](../CC_P8_APPLICATIONS_AUTOMATION_UI_SPEC.md) maps read-only Applications metadata `7:88` to a deferred local file-drop trigger contract, explicit claim/lifecycle/provenance states and P5 deep-links. It does not authorize a trigger runner, TCP/web/cloud scope, automatic retry, unattended dispatch, runtime implementation or Figma edit.

**Superseding docs-only Markdown audit after the P8 Applications/Automation UI spec (2026-08-13):** `79` Markdown files, `563` relative links checked, and `0` broken paths. The `78`/`555` figures above are the preceding P7 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`P3_BARCODE_CHECK_DIGIT_UI_SPEC.md`](../P3_BARCODE_CHECK_DIGIT_UI_SPEC.md) is the next open barcode UI/UX contract. It proposes a Code 39-first `None`/`Auto`/`Verify` policy, display-only HRI masking, fail-closed Verify states, responsive/accessibility gates and interim reuse of Figma `18:69`/`13:2`. It does not add a barcode model, change rendering, authorize a Figma edit or close P3.

**Superseding docs-only Markdown audit after the P3 barcode check-digit/HRI UI spec (2026-08-13):** `80` Markdown files, `575` relative links checked, and `0` broken paths. The `79`/`563` figures above are the preceding P8 UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`R4_DATA_WORKSPACE_UI_SPEC.md`](../R4_DATA_WORKSPACE_UI_SPEC.md) is the next R4 product-slice contract after v0.211. It maps the read-only Figma Data shell to one derived-field authoring task, sample/lineage diagnostics and atomic fail-closed Apply semantics; it does not add a transform editor, alter connectors or authorize a Figma edit.

**Superseding docs-only Markdown audit after the R4 Data Workspace UI spec (2026-08-13):** `81` Markdown files, `583` relative links checked, and `0` broken paths. The `80`/`575` figures above are the preceding P3 barcode UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`DATABASE_MANAGER_UI_SPEC.md`](../DATABASE_MANAGER_UI_SPEC.md) specifies the existing WPF Manager's source-list/detail states, async connection/preview evidence, usage-aware removal and guarded cleanup. It uses Figma `8:2`/`9:2` only as read-only shell reference and does not claim runtime click-through, a new Manager frame or a Figma edit.

**Superseding docs-only Markdown audit after the Database Manager UI spec (2026-08-13):** `82` Markdown files, `593` relative links checked, and `0` broken paths. The `81`/`583` figures above are the preceding R4 Data Workspace UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md`](../P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md) and [`P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md`](../P4_BARCODE_RATIO_QUIET_ZONE_UI_SPEC.md) define the next open barcode UI/UX contract: Code 39-first legal ratio, density as a derived readout, physical quiet-zone mm from shared X resolution and explicit non-claims. No barcode implementation or Figma edit is implied.

**Superseding docs-only Markdown audit after the P4 barcode ratio/quiet-zone UI docs (2026-08-13):** `84` Markdown files, `616` relative links checked, and `0` broken paths. The `82`/`593` figures above are the preceding Database Manager UI spec baseline; external URLs remain outside this local-path check.

The documentation-only [`P5_2D_BARCODE_PARITY_UI_HANDOFF.md`](../P5_2D_BARCODE_PARITY_UI_HANDOFF.md) and [`P5_2D_BARCODE_PARITY_UI_SPEC.md`](../P5_2D_BARCODE_PARITY_UI_SPEC.md) define the next open 2D barcode UI/UX contract: QR naming/capacity preservation, standard-aware Data Matrix sizing/EC semantics, shared fixed-module DPI risk and explicit unsupported states. No barcode implementation, model field, Figma edit or P5 closure is implied.

The documentation-only [`P6_GS1_AI_UI_HANDOFF.md`](../P6_GS1_AI_UI_HANDOFF.md) and [`P6_GS1_AI_UI_SPEC.md`](../P6_GS1_AI_UI_SPEC.md) define the next open GS1 UI/UX contract: strict `(AI)value` notation, parsed AI/boundary/FNC1 diagnostics, registry provenance and separate geometry/preflight status. No parser/rendering implementation, full AI wizard, Figma edit or GS1 certification claim is implied.

**Superseding docs-only Markdown audit after the P6 GS1 AI UI docs (2026-08-13):** `88` Markdown files, `669` relative links/assets checked, and `0` broken paths. The `86`/`642` figures above are the preceding P5 baseline; external URLs remain outside this local-path audit.

The documentation-only [`P7_PRINT_METHOD_UI_HANDOFF.md`](../P7_PRINT_METHOD_UI_HANDOFF.md) and [`P7_PRINT_METHOD_UI_SPEC.md`](../P7_PRINT_METHOD_UI_SPEC.md) define the next open print-method contract: Graphic remains the default, Native is explicit and ADR/pilot-gated, requested/resolved paths and fallback reasons are visible, and no queue or method may change silently. No native emitter, driver adapter, Figma edit or printer-family pilot is claimed.

The P7 read-only Figma check used shell node `2:2` (`1440 x 900`), Print & Output `2:39`, Setup `2:41`, Preview `2:44`, Print `2:47`, printer status `2:19` and paper status `2:21`. The shell has no method/capability/fallback state, so it remains visual input only; P7 stays open until ADR, pilot, manifest evidence and target-scale runtime checks exist.

**Superseding docs-only Markdown audit after the P7 print-method UI docs (2026-08-13):** `90` Markdown files, `689` relative links/assets checked, and `0` broken paths. The `88`/`669` figures above are the preceding P6 baseline; external URLs remain outside this local-path audit.

The documentation-only [`P8_PHYSICAL_VERIFIER_UI_HANDOFF.md`](../P8_PHYSICAL_VERIFIER_UI_HANDOFF.md) and [`P8_PHYSICAL_VERIFIER_UI_SPEC.md`](../P8_PHYSICAL_VERIFIER_UI_SPEC.md) define the open hardware verifier surface: hash-only manifest-bound evidence, explicit ANSI/ISO grade policy, timeout/busy/cancel/rejection states, redacted support export and a strict distinction between queue/preflight/golden/visual audit and physical completion. No device SDK, signed-evidence schema, Figma edit or grade certification is claimed.

The P8 read-only Figma check used shell node `2:2` (`1440 x 900`) with Print & Output `2:39` and status bar `2:170`, plus Control Center History `3:85` (`1280 x 800`) with filters `3:99` and activity frame `3:101`. Neither contains verifier/device/grade states; P8 remains open until fixture, adapter, evidence and target-scale runtime gates exist.

**Superseding docs-only Markdown audit after the P8 physical-verifier UI docs (2026-08-13):** `92` Markdown files, `713` relative links/assets checked, and `0` broken paths. The `90`/`689` figures above are the preceding P7 baseline; external URLs remain outside this local-path audit.

The documentation-only [`BARCODE_UI_UX_PROGRAM_INDEX.md`](../BARCODE_UI_UX_PROGRAM_INDEX.md) now coordinates barcode P3-P8 state ownership, read-only Figma routing, shared runtime gates and explicit hardware/ADR non-claims. It does not replace the individual handoffs or authorize code/Figma edits.

The documentation-only [`P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md`](../P3_BARCODE_CHECK_DIGIT_DECISION_PACKET.md) is the next owner gate under that index. It records source evidence for the Code 39/ITF gap, recommends a Code 39-first `None`/`Auto`/`Verify` boundary, separates HRI masking from encoded modules, and requires D1-D5 sign-off before implementation. No barcode model, renderer, Figma or Text/TextBox change is implied.

The documentation-only [`P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md`](../P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md) is the following owner gate. It records the current logical-module/profile and X-quantization evidence, the missing ratio/density/physical-QZ contract, the side/total and legacy-X decisions, and the renderer/standards probe required before implementation. No barcode model, renderer, Figma or Text/TextBox change is implied.

The documentation-only [`P5_2D_BARCODE_PARITY_DECISION_PACKET.md`](../P5_2D_BARCODE_PARITY_DECISION_PACKET.md) is the next 2D authoring owner gate. It preserves QR capacity/module behavior, records the current QR-shaped Data Matrix controls and renderer gap, requires an explicit automatic/unsupported boundary, and names the renderer/Figma/runtime evidence needed before implementation. No barcode model, renderer, Figma or Text/TextBox change is implied.

The documentation-only [`P6_GS1_AI_UI_DECISION_PACKET.md`](../P6_GS1_AI_UI_DECISION_PACKET.md) is the following GS1 diagnostics owner gate. It records the strict parser and curated/offline registry evidence, first AI-family choice, `[FNC1]` boundary copy, provenance/update policy, diagnostics-versus-geometry ownership and runtime/Figma evidence required before implementation. No parser, renderer, Figma or Text/TextBox change is implied.

The documentation-only [`P7_PRINT_METHOD_DECISION_PACKET.md`](../P7_PRINT_METHOD_DECISION_PACKET.md) is the following dispatch/output owner gate. It records the current WPF graphic path, missing native adapter/method fields, capability-record scope, explicit fallback, output-contract/parity boundary, manifest migration and real printer-family pilot evidence required before implementation. No print-method implementation, native command claim, Figma edit or Text/TextBox change is implied.

The documentation-only [`P8_PHYSICAL_VERIFIER_DECISION_PACKET.md`](../P8_PHYSICAL_VERIFIER_DECISION_PACKET.md) is the following hardware-gated evidence owner gate. It records the current hash-only manifest-bound verifier contract, adapter identity/correlation/timeout/busy rules, grade scales, completion requirement, redaction/signature policy, Figma boundary and lab fixture evidence required before implementation. No device SDK, certification claim, Figma edit or Text/TextBox change is implied.

The documentation-only [`CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md) is the next downstream owner gate. It records configured-root cardinality, Built-in/local source precedence, host/action ownership, validated file/thumbnail states, primary/backup/archive lineage, compare/restore/dirty-edit policy and the CC-P4 workflow boundary required before implementation. No document browser, workflow/check-out/ACL control, Figma edit or Text/TextBox change is implied.

The documentation-only [`CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md`](../CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md) is the next downstream owner gate. It records the candidate state graph, envelope/migration and exact-hash coverage, local actor/audit boundary, policy-on print composition, host/action ownership and deferred scheduling/roles required before implementation. No workflow enum/store, Published print gate, permissions change, Figma edit or Text/TextBox change is implied.

The documentation-only [`CC_P6_ANALYTICS_DECISION_PACKET.md`](../CC_P6_ANALYTICS_DECISION_PACKET.md) is the following downstream owner gate. It records label-row/job/event units, CSV/operation/state precedence and conflict handling, timezone and dimension/privacy rules, the physical-output disclaimer, P5 deep-link/export ownership and source-health/runtime fixtures required before implementation. No Analytics window, telemetry, physical-label claim, Figma edit or Text/TextBox change is implied.

The documentation-only [`CC_P7_ADMINISTRATION_DECISION_PACKET.md`](../CC_P7_ADMINISTRATION_DECISION_PACKET.md) is the following downstream owner gate. It records thin-host/action ownership, local activation/preferences/registry/cleanup boundaries, evidence and retention-preview safety, privacy/security rules and unsupported server-category treatment required before implementation. No Admin window, role/user/sync/license-seat feature, destructive retention, Figma edit or Text/TextBox change is implied.

The documentation-only [`CC_P8_AUTOMATION_DECISION_PACKET.md`](../CC_P8_AUTOMATION_DECISION_PACKET.md) is the following downstream owner gate. It records the first local file-drop prerequisite, one trigger/lifecycle owner, claim/deduplication and restart semantics, configuration/provenance fields, shared preflight/manifest/queue/History spine, privacy and deferred TCP/web/security boundaries required before implementation. No trigger runner, unattended print, web app, Figma edit or Text/TextBox change is implied.

**Superseding docs-only Markdown audit after the barcode UI/UX program index (2026-08-13):** `93` Markdown files, `729` relative links/assets checked, and `0` broken paths. The `92`/`713` figures above are the preceding P8 baseline; external URLs remain outside this local-path audit.

**Superseding docs-only Markdown audit after the P3 barcode owner decision packet (2026-08-13):** `94` Markdown files, `749` relative links/assets checked, and `0` broken paths. The `93`/`729` figures above are the preceding barcode UI/UX program-index baseline; external URLs remain outside this local-path audit.

**Superseding docs-only Markdown audit after the P4 barcode ratio/QZ owner decision packet (2026-08-13):** `95` Markdown files, `771` relative links/assets checked, and `0` broken paths. The `94`/`749` figures above are the preceding P3 barcode owner-packet baseline; external URLs remain outside this local-path audit.

**Superseding docs-only Markdown audit after the P5 2D barcode parity owner decision packet (2026-08-13):** `96` Markdown files, `793` relative links/assets checked, and `0` broken paths. The `95`/`771` figures above are the preceding P4 barcode owner-packet baseline; external URLs remain outside this local-path audit.

**Superseding docs-only Markdown audit after the P6 GS1 diagnostics owner decision packet (2026-08-13):** `97` Markdown files, `816` relative links/assets checked, and `0` broken paths. The `96`/`793` figures above are the preceding P5 barcode owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the P7 print-method owner decision packet (2026-08-13):** `98` Markdown files, `845` relative links/assets checked, and `0` broken paths. The `97`/`816` figures above are the preceding P6 GS1 owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the P8 physical-verifier owner decision packet (2026-08-13):** `99` Markdown files, `875` relative links/assets checked, and `0` broken paths. The `98`/`845` figures above are the preceding P7 print-method owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the CC-P3 document-library/revision owner decision packet (2026-08-13):** `100` Markdown files, `899` relative links/assets checked, and `0` broken paths. The `99`/`875` figures above are the preceding P8 physical-verifier owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the CC-P4 approval-workflow owner decision packet (2026-08-13):** `101` Markdown files, `923` relative links/assets checked, and `0` broken paths. The `100`/`899` figures above are the preceding CC-P3 document-library/revision owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the CC-P6 analytics owner decision packet (2026-08-13):** `102` Markdown files, `947` relative links/assets checked, and `0` broken paths. The `101`/`923` figures above are the preceding CC-P4 approval-workflow owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the CC-P7 administration owner decision packet (2026-08-13):** `103` Markdown files, `973` relative links/assets checked, and `0` broken paths. The `102`/`947` figures above are the preceding CC-P6 analytics owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the CC-P8 automation owner decision packet (2026-08-13):** `104` Markdown files, `997` relative links/assets checked, and `0` broken paths. The `103`/`973` figures above are the preceding CC-P7 administration owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the CC-P1/P2/P5 upstream implementation-gate packet (2026-08-13):** `105` Markdown files, `1014` relative links/assets checked, and `0` broken paths. The `104`/`997` figures above are the preceding CC-P8 automation owner-packet baseline; external URLs remain outside this local-path check.

**Superseding docs-only Markdown audit after the P5 2D barcode parity UI docs (2026-08-13):** `86` Markdown files, `642` relative links/assets checked, and `0` broken paths. The `84`/`616` figures above are the preceding P4 baseline; external URLs remain outside this local-path audit.

### Named barcode gates rechecked

The application runner passed the gates that close the current barcode software slices:

- `linear barcode width follows quantized X-dim when SizedFromX`
- `compiled scene print uses SizedFromX production width`
- `legacy frame-owned width not auto-sized when X is zero`
- `barcode HRI above reserves top strip`
- `barcode HRI placement survives clone and save`

The corresponding status and non-claims are recorded in [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md), [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md), and [`P1_LINEAR_GEOMETRY_NEXT_SLICE.md`](../P1_LINEAR_GEOMETRY_NEXT_SLICE.md).

## Figma boundary

No new Figma inspection or edit was needed for this checkpoint: it changes documentation and evidence ownership, not UI behavior. Existing read-only references remain authoritative for visual intent:

- shell `2:2`;
- panels `8:2`, Data tab `9:2`, selected Properties `13:2`, tabs `18:69`;
- Excel verification component `22:82`.

There is still no dedicated barcode-Properties frame. If P3 check-digit/HRI policy becomes a UI slice, select the first operator task, name a Figma node (or explicitly reuse a current reference), then close it with a runtime screenshot/measurement and regression coverage. A Figma frame alone is not runtime proof.

The next R4 Data Workspace UI slice has the same boundary: Figma `9:2` supplies the Data-tab shell and empty/current/settings/binding-check cards, but no transform editor, sample table, lineage or invalid-state variants. The detailed scope and ready gate are recorded in [`R4_DATA_WORKSPACE_UI_HANDOFF.md`](../R4_DATA_WORKSPACE_UI_HANDOFF.md); no implementation or release claim is implied by the reference.

Database Manager has a separate evidence boundary: the current WPF `DatabaseManagerWindow`/`DataSourceCleanupWindow` code exists, but the recorded plan says the click-through has not been manually verified and Figma Page `0:1` has no Manager frame. The state matrix and owner gate are recorded in [`DATABASE_MANAGER_UI_HANDOFF.md`](../DATABASE_MANAGER_UI_HANDOFF.md).

CC-P1 is now implemented as a staged local `OperationsOverviewWindow`. It projects explicit saved-queue evidence plus durable recovery candidates/diagnostics, rejects stale refresh epochs and deep-links to the existing Printer Setup, Print Center and Print History owners. Licensing is excluded. Runtime/UIA smoke at the 1040 x 700 default window and post-change build/unit/application suites passed; Figma Overview `2:2` remained a read-only hierarchy reference.

CC-P2 is also documentation-only: read-only metadata for Printers `2:37` supplies the filter/table/command shell, while the current WPF has no multi-queue console or Pause/Resume/Delete command service. The M1 read-only scope, state taxonomy and command deferral are recorded in [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](../CC_P2_PRINT_QUEUE_UI_HANDOFF.md); no implementation, queue mutation or Figma edit is implied.

CC-P5 is documentation-only as well: History `3:85` supplies filter/activity/detail affordances, while current WPF keeps CSV history, job JSONL and hash-chained state/reprint actions in separate owners. The provenance model, state matrix and exact-manifest reprint gates are recorded in [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](../CC_P5_HISTORY_REPRINT_UI_HANDOFF.md); no unified browser implementation or Figma edit is implied.

CC-P3 is documentation-only: Documents `3:2` supplies the browse/folder/card reference and Workflow `7:2` supplies deferred CC-P4 vocabulary, while current WPF has an embedded-template gallery and saved-file primary/backup/archive revision recovery but no configured local-root browser, check-out flag, or workflow enum. The root/host/preview/revision state matrix and owner decisions are recorded in [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md); no implementation or Figma edit is implied.

CC-P4 is documentation-only: Workflow `7:2` supplies candidate state/action/history vocabulary, while current source has no typed document workflow, local actor/role policy, transition audit store, or Published print gate. The state graph, legacy migration, audit boundary, and policy-on print matrix are recorded in [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](../CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md); P5 linked-reprint approval remains a separate job decision, and no implementation or Figma edit is implied.

CC-P8 is documentation-only and deferred: Applications `7:88` supplies a Web Applications shell and Automation sidebar, while current source has only the linked-Excel freshness watcher and manual manifest/preflight/queue path; no trigger host, file-claim protocol, TCP listener, web form or unattended-print service exists. The local file-drop proposal, lifecycle/deduplication/provenance matrix and prerequisite gates are recorded in [`CC_P8_AUTOMATION_UI_HANDOFF.md`](../CC_P8_AUTOMATION_UI_HANDOFF.md); History `3:101` is a research destination, not live automation evidence, and no implementation or Figma edit is implied.

CC-P6 is documentation-only: Analytics `5:2` supplies chart/filter reference regions, while current local evidence remains split across per-label CSV, best-effort operation JSONL and hash-chained job state; no cross-source aggregate or Analytics UI exists. The unit/provenance/timezone/redaction matrix and software-counter boundary are recorded in [`CC_P6_ANALYTICS_UI_HANDOFF.md`](../CC_P6_ANALYTICS_UI_HANDOFF.md); no cloud telemetry, physical-output claim, implementation or Figma edit is implied.

CC-P7 is documentation-only and renamed Local Maintenance: Administration `5:41` supplies hierarchy references only, while the product scope is limited to local preferences, versioned data-source registry/cleanup, evidence links and retention preview/recovery. Licensing, activation, roles/users, sync, SMTP, workflow administration and server categories are excluded. See [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](../CC_P7_ADMINISTRATION_UI_HANDOFF.md).

The cross-surface [`CC_UI_UX_PROGRAM_INDEX.md`](../CC_UI_UX_PROGRAM_INDEX.md) coordinates CC-P1 through CC-P8, preserves the dependency order, records one action owner per slice and maps the read-only Figma nodes listed above. CC-P1 is the implemented staged window; P2-P8 remain separate follow-up scopes.

Its shared host/navigation gate now records the staged P1 window, existing `Shell.*` regions, `PrintCenterWindow` action owner and verified `CC.P1.Overview.*` AutomationIds.

The P1/P2/P5 handoffs now link back to this program boundary and state their dependency ownership; this is documentation coordination only and does not close any runtime, UI Automation, print or physical-verifier gate.

[`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](../CC_P1_P2_P5_HOST_DECISION_PACKET.md) records the selected staged P1 host and the read-only Figma evidence boundary. No permanent multi-module Control Center shell or Figma edit was created.

The documentation-only [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](../CC_P1_P2_P5_READ_MODEL_CONTRACT.md) records current source authority and merge rules; it does not claim a runtime projection, UI, print result or physical verification.

[`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](../CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md) is the executed P1 runbook and evidence log. P2 Queue and P5 History remain follow-up slices under the same action/source ownership boundaries.

The documentation-only [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](../R4_DATA_SURFACES_OWNER_DECISION_PACKET.md) is the next non-CC data-surface owner gate. It consolidates one shared source/connector identity, separate Data Workspace and Database Manager hosts, transform draft/commit semantics, Manager async/mutation safety, read-only Figma reuse and target-scale runtime fixtures; it does not authorize a transform editor, registry rewrite, new Manager frame, Figma write or Text/TextBox change.

**Superseding docs-only Markdown audit after the R4 data-surface owner decision packet (2026-08-13):** `106` Markdown files, `1046` relative links/assets checked, and `0` broken paths. The `105`/`1014` figures above are the preceding CC upstream baseline; external URLs remain outside this local-path check.

The documentation-only [`DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md`](../DESIGNER_SHELL_PANEL_EXCEL_VERIFICATION_DECISION_PACKET.md) is the next UI/UX owner gate. It consolidates the R1-R7 shell/action map, the `268/280` versus `300/300` panel-width reconciliation, the `Advanced` tab-label baseline, the five-state Excel verification contract, read-only Figma routing and target-scale/UIA fixtures. It does not authorize a shell change, Figma write, Database Manager redesign or Text/TextBox change.

**Superseding audit after the designer shell/panel/Excel verification owner packet (2026-08-13):** `107` Markdown files, `1072` relative links/assets checked, and `0` broken paths. The `106`/`1046` figures above are the preceding R4 data-surface baseline; external URLs remain outside this local-path check.

The documentation-only [`PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md`](../PRINT_PREVIEW_CALIBRATION_UI_DECISION_PACKET.md) is the next UI/UX owner gate. It keeps the existing WPF Preview/Printer Setup/Calibration workflow authoritative, routes Figma metadata read-only and separates software queue acceptance from physical calibration evidence.

**Superseding docs-only Markdown audit after the Print Preview/Calibration UI owner packet (2026-08-13):** `108` Markdown files, `1101` relative links/assets checked, and `0` broken paths. The `107`/`1072` figures above are the preceding designer shell/panel/Excel baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md`](../CC_P5_PRINT_CENTER_RECOVERY_UI_DECISION_PACKET.md) is the next local action-owner gate. It keeps the existing WPF Print Center recovery/reprint/support-export owner explicit, routes Figma History metadata read-only and preserves the P1/P2/P5 exact-manifest/no-auto-retry boundary.

**Superseding docs-only Markdown audit after the Print Center recovery owner packet (2026-08-13):** `109` Markdown files, `1133` relative links/assets checked, and `0` broken paths. The `108`/`1101` figures above are the preceding Print Preview/Calibration baseline; external URLs remain outside this local-path check.

The documentation-only [`DATABASE_MANAGER_UI_DECISION_PACKET.md`](../DATABASE_MANAGER_UI_DECISION_PACKET.md) is the next non-CC data-surface owner gate. It keeps the existing WPF Database Manager/Cleanup action owners explicit, routes panels Figma metadata read-only and preserves the R4 source identity, fallback and protected Text/TextBox boundaries.

**Superseding docs-only Markdown audit after the Database Manager/Cleanup owner packet (2026-08-13):** `110` Markdown files, `1169` relative links/assets checked, and `0` broken paths. The `109`/`1133` figures above are the preceding Print Center recovery baseline; external URLs remain outside this local-path check.

The documentation-only [`R4_DATA_WORKSPACE_UI_DECISION_PACKET.md`](../R4_DATA_WORKSPACE_UI_DECISION_PACKET.md) is the next R4 owner gate. It makes the Data-tab transform authoring/diagnostics boundary concrete, preserves the Manager/Workspace split, records the invalid raw-fallback risk and read-only Figma `8:2`/`9:2` route, and does not authorize implementation, a transform editor or a Figma write.

**Superseding docs-only Markdown audit after the R4 Data Workspace authoring/diagnostics owner packet (2026-08-13):** `111` Markdown files, `1205` relative links/assets checked, and `0` broken paths. The `110`/`1169` figures above are the preceding Database Manager/Cleanup owner-packet baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md`](../CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md) is the next CC owner gate. It makes the M1 queue-visibility boundary concrete, preserves the upstream host and Print Center action owners, records the read-only Figma `2:37` route and keeps command/physical-output claims deferred. It does not authorize implementation, a new queue host or a Figma write.

**Superseding docs-only Markdown audit after the CC-P2 Print Queue M1 owner packet (2026-08-13):** `112` Markdown files, `1243` relative links/assets checked, and `0` broken paths. The `111`/`1205` figures above are the preceding R4 Data Workspace owner-packet baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md`](../CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md) is the next CC owner gate. It makes the local Operations Overview card/deep-link boundary concrete, records the current queue timestamp, recent-terminal-fault and activation build gaps, preserves the upstream host and existing Print Center/Printer Setup/History owners, and keeps Figma `2:2` read-only. It does not authorize implementation, a new host or a Figma write.

**Superseding docs-only Markdown audit after the CC-P1 Operations Overview owner packet (2026-08-13):** `113` Markdown files, `1285` relative links/assets checked, and `0` broken paths. The `112`/`1243` figures above are the preceding CC-P2 Print Queue owner-packet baseline; external URLs remain outside this local-path check.

The documentation-only [`CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md`](../CC_P5_HISTORY_REPRINT_UI_DECISION_PACKET.md) is the next CC owner gate. It makes History/read-model provenance, identity/granularity, time/privacy, corrupt-tail diagnostics and the read-only return to the existing Print Center action owner concrete; Figma History `3:85` remains read-only and no runtime merge or second dispatch path is authorized.

**Superseding docs-only Markdown audit after the CC-P5 History/reprint owner packet (2026-08-13):** `114` Markdown files, `1328` relative links/assets checked, and `0` broken paths. The `113`/`1285` figures above are the preceding CC-P1 Operations Overview owner-packet baseline; external URLs remain outside this local-path check.

The next downstream CC gate after P5 is the existing documentation-only [`CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_DECISION_PACKET.md). It owns configured-root/Built-in browse, validated file identity, one revision/restore path and the P3-to-P4 workflow boundary; Figma Documents `3:2` remains read-only and no document browser or workflow implementation is implied.

**Superseding docs-only Markdown audit after the P5-to-P3 downstream routing update (2026-08-13):** `114` Markdown files, `1340` relative links/assets checked, and `0` broken paths. The `114`/`1328` figures above are the preceding P5 History/reprint owner-packet baseline; external URLs remain outside this local-path check.

The next policy gate after P3 is the existing documentation-only [`CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md`](../CC_P4_APPROVAL_WORKFLOW_DECISION_PACKET.md). It composes candidate document state/audit and policy-on print behavior over the validated P3 revision/hash, keeps P5 reprint approval separate, and routes Figma Workflow `7:2` read-only; no workflow store, Published gate or Figma edit is implied.

**Superseding docs-only Markdown audit after the P3-to-P4 workflow routing update (2026-08-13):** `114` Markdown files, `1352` relative links/assets checked, and `0` broken paths. The `114`/`1340` figures above are the preceding P5-to-P3 routing baseline; external URLs remain outside this local-path check.

**Superseding Markdown audit after the CC-P1 implementation (2026-08-13):** `114` Markdown files, `1252` repository-relative Markdown destinations checked by the current normalized-path audit, and `0` broken paths. External URLs and heading-anchor validity remain outside this local-path check.

[`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](../CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md) is now implemented by the staged P1 WPF host. Runtime/UIA evidence closed the first layout and deep-link gate; Figma `2:2` remained read-only and was not edited.

The handoff template's Figma escalation protocol is documentation-only: it requires a concrete missing state, read-only node metadata and runtime closure evidence before any future design connection is treated as actionable.

The downstream P3/P4/P6/P7/P8 handoffs now link to the same boundary and preserve one owner per operation; their design/review and deferred statuses remain open.

## Release gates still open

### Software-only continuation (v0.212)

Public version metadata is now canonicalized in `eng/Version.props`; the
application regression checks source projections and the compiled assembly
metadata. R4 Data Workspace M1 adds atomic draft transforms, Core-owned
sample/lineage validation, no raw preview fallback on invalid transforms, undo
coverage and a `1024 × 600` WPF Automation tree gate. Physical/device,
display-scale visual and operator-usability checks remain deferred external
evidence as defined in [`../VERSIONING.md`](../VERSIONING.md).

### Local Workflow host (v0.213)

P4 now has a local, modeless Workflow host for a saved template. Its Draft,
In Review, Approved, Published and Rejected transitions are validated by the
Core contract and recorded in a separate, hash-chained sidecar audit. A changed
document hash starts a new Draft revision; an invalid audit tail blocks further
changes. The host has a `1024 x 600` WPF Automation tree regression gate and
does not add a print, queue or unattended-dispatch action. Physical output and
operator review remain deferred external evidence under
[`../VERSIONING.md`](../VERSIONING.md).

### Local Automation evidence console (v0.214)

P8 now has a modeless local evidence console for the durable fingerprint claim
ledger. It surfaces an explicit Stopped/no-runner status, configuration boundary
and redacted event summary, with deep links to the existing History and Print
Center owners. It has no watcher, source consumption, queue call or automatic
print path. A `1024 x 600` WPF Automation-tree regression gates the host;
hardware, operator and unattended-dispatch evidence remains deferred.

### Superseding execution boundary (2026-08-13)

CC-P1 is implemented as a staged local `OperationsOverviewWindow` with queue/recovery evidence and deep-links to existing owners. Licensing/activation/entitlement/seat features are excluded from the Control Center product scope. The authoritative implementation evidence is [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](../CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md); older activation-card and blank-owner rows in this historical checkpoint are superseded.

- clean implementation ownership and a fresh post-commit rerun of the commands above;
- physical verifier/grade evidence, printer-native command evidence, full GS1/catalog parity and physical-label evidence;
- any UI change requiring target-scale screenshot/measurement and the protected Text/TextBox contract gates.

Until those gates close, public wording remains **software regression evidence + graphic thermal path**, not verifier certification or a shipped multi-tenant Control Center/LMS.

## Handoff

This checkpoint is linked from [`10-continuation-handoff-2026-08-13.md`](10-continuation-handoff-2026-08-13.md). The historical release narratives in [`MASTER_PLAN.md`](../../MASTER_PLAN.md) and [`PLAN.md`](../../PLAN.md) remain unchanged by this file and should receive the next verified snapshot only from the owning clean implementation checkpoint.
