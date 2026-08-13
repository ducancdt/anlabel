# ANLAbel — current verification checkpoint (2026-08-13)

**Status:** evidence snapshot; not a release approval
**Documentation checkpoint:** `96f6ab5` (`docs: reconcile barcode execution status`)
**Scope:** record the current source/test/version evidence without staging the broad implementation wave already present in the worktree.

## Why this file exists

`MASTER_PLAN.md` and `PLAN.md` contain a large uncommitted implementation wave. Their historical entries and existing edits must remain intact, so this checkpoint records the current evidence in an additive file. After the implementation owner selects and commits the intended scope, copy the verified snapshot into the release-history sections of those two files; do not infer that the current dirty checkout is releasable.

## Current evidence

| Evidence | Result | Boundary |
| --- | --- | --- |
| Display/source version | `0.202` in the current app project, shell title/build-channel text, and public Commercial/Trial installer metadata | The private License Master installer intentionally remains `1.0.0`; version parity is not a signed release artifact while those source/installer files are dirty. |
| `dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false` | PASS · 0 warnings · 0 errors | Compile evidence for the current checkout. |
| `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet` | PASS · 356/356 | Unit/contract evidence; no physical-device claim. |
| `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build` | PASS · exit 0; 157 registered checks in the current runner | Application regression evidence. Named P1/P2 barcode gates and the protected Text/TextBox regression names are present in the runner; this is not a hardware smoke test. |
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

CC-P1 has the same documentation-only boundary: the current WPF `PrintCenterWindow` provides durable recovery actions, while queue status, activation and print-history entry points remain separate. Read-only Control Center metadata supplies Overview `2:2` plus future Printers `2:37` and History `3:85`, but no local Operations Overview exists yet. The proposed state matrix, local-evidence/non-claim rules and host-surface decision are recorded in [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](../CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md); no implementation or Figma edit is implied.

CC-P2 is also documentation-only: read-only metadata for Printers `2:37` supplies the filter/table/command shell, while the current WPF has no multi-queue console or Pause/Resume/Delete command service. The M1 read-only scope, state taxonomy and command deferral are recorded in [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](../CC_P2_PRINT_QUEUE_UI_HANDOFF.md); no implementation, queue mutation or Figma edit is implied.

CC-P5 is documentation-only as well: History `3:85` supplies filter/activity/detail affordances, while current WPF keeps CSV history, job JSONL and hash-chained state/reprint actions in separate owners. The provenance model, state matrix and exact-manifest reprint gates are recorded in [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](../CC_P5_HISTORY_REPRINT_UI_HANDOFF.md); no unified browser implementation or Figma edit is implied.

CC-P3 is documentation-only: Documents `3:2` supplies the browse/folder/card reference and Workflow `7:2` supplies deferred CC-P4 vocabulary, while current WPF has an embedded-template gallery and saved-file primary/backup/archive revision recovery but no configured local-root browser, check-out flag, or workflow enum. The root/host/preview/revision state matrix and owner decisions are recorded in [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md); no implementation or Figma edit is implied.

CC-P4 is documentation-only: Workflow `7:2` supplies candidate state/action/history vocabulary, while current source has no typed document workflow, local actor/role policy, transition audit store, or Published print gate. The state graph, legacy migration, audit boundary, and policy-on print matrix are recorded in [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](../CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md); P5 linked-reprint approval remains a separate job decision, and no implementation or Figma edit is implied.

CC-P8 is documentation-only and deferred: Applications `7:88` supplies a Web Applications shell and Automation sidebar, while current source has only the linked-Excel freshness watcher and manual manifest/preflight/queue path; no trigger host, file-claim protocol, TCP listener, web form or unattended-print service exists. The local file-drop proposal, lifecycle/deduplication/provenance matrix and prerequisite gates are recorded in [`CC_P8_AUTOMATION_UI_HANDOFF.md`](../CC_P8_AUTOMATION_UI_HANDOFF.md); History `3:101` is a research destination, not live automation evidence, and no implementation or Figma edit is implied.

CC-P6 is documentation-only: Analytics `5:2` supplies chart/filter reference regions, while current local evidence remains split across per-label CSV, best-effort operation JSONL and hash-chained job state; no cross-source aggregate or Analytics UI exists. The unit/provenance/timezone/redaction matrix and software-counter boundary are recorded in [`CC_P6_ANALYTICS_UI_HANDOFF.md`](../CC_P6_ANALYTICS_UI_HANDOFF.md); no cloud telemetry, physical-output claim, implementation or Figma edit is implied.

CC-P7 is documentation-only: Administration `5:41` supplies server-admin categories and sample role table, while current source supports local activation/DPAPI state, designer/printer preferences, versioned data-source registry/cleanup and local evidence files but no multi-user admin service. The thin-local-host, retention/recovery/privacy matrix is recorded in [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](../CC_P7_ADMINISTRATION_UI_HANDOFF.md); no roles, users, sync, SMTP, license-seat server, workflow admin, implementation or Figma edit is implied.

The cross-surface [`CC_UI_UX_PROGRAM_INDEX.md`](../CC_UI_UX_PROGRAM_INDEX.md) is also documentation-only. It links all CC-P1 through CC-P8 handoffs, preserves the dependency order, records one action owner per slice and maps the read-only Figma nodes listed above; it does not create a runtime Control Center, change WPF behavior or edit the Figma file.

Its shared host/navigation gate records the existing `Shell.*` WPF regions and `PrintCenterWindow` action owner, but leaves the host choice and proposed `CC.*` AutomationIds open until runtime evidence exists.

The P1/P2/P5 handoffs now link back to this program boundary and state their dependency ownership; this is documentation coordination only and does not close any runtime, UI Automation, print or physical-verifier gate.

The documentation-only [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](../CC_P1_P2_P5_HOST_DECISION_PACKET.md) consolidates the open host choice and Figma evidence boundary; no host, WPF navigation or Figma frame has been selected or edited.

The documentation-only [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](../CC_P1_P2_P5_READ_MODEL_CONTRACT.md) records current source authority and merge rules; it does not claim a runtime projection, UI, print result or physical verification.

The documentation-only [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](../CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md) records the Figma `2:2` mapping and responsive/UI Automation gates; no WPF host or Figma frame has been implemented or edited.

The handoff template's Figma escalation protocol is documentation-only: it requires a concrete missing state, read-only node metadata and runtime closure evidence before any future design connection is treated as actionable.

The downstream P3/P4/P6/P7/P8 handoffs now link to the same boundary and preserve one owner per operation; their design/review and deferred statuses remain open.

## Release gates still open

- clean implementation ownership and a fresh post-commit rerun of the commands above;
- physical verifier/grade evidence, printer-native command evidence, full GS1/catalog parity and physical-label evidence;
- any UI change requiring target-scale screenshot/measurement and the protected Text/TextBox contract gates.

Until those gates close, public wording remains **software regression evidence + graphic thermal path**, not verifier certification or a shipped multi-tenant Control Center/LMS.

## Handoff

This checkpoint is linked from [`10-continuation-handoff-2026-08-13.md`](10-continuation-handoff-2026-08-13.md). The historical release narratives in [`MASTER_PLAN.md`](../../MASTER_PLAN.md) and [`PLAN.md`](../../PLAN.md) remain unchanged by this file and should receive the next verified snapshot only from the owning clean implementation checkpoint.
