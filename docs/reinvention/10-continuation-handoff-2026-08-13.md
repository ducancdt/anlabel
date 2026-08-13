# ANLAbel — continuation handoff (2026-08-13)

**Status:** active documentation handoff; documentation-only in this slice
**Scope:** reconcile the current Markdown roadmap and define the next evidence gate without touching another agent’s dirty code/UI files
**Protected contract:** the Text/TextBox rules in [`AGENTS.md`](../../AGENTS.md) remain unchanged

**Current evidence pointer:** [`11-verification-checkpoint-2026-08-13.md`](11-verification-checkpoint-2026-08-13.md) records the latest docs-only checkpoint; it is not a release approval.

## Why this handoff exists

The repository currently contains a large, uncommitted implementation wave across the designer, print pipeline, barcode contracts, data connectors, tests, installers and UI assets. The existing roadmap files also contain new, partially reconciled status blocks. This note is intentionally additive: it records what must be reconciled after the implementation owner reaches a checkpoint; it does not rewrite historical entries or infer that uncommitted code is released.

The source-of-truth split is:

| Concern | Authoritative document | Rule for the next update |
| --- | --- | --- |
| Protected Text/TextBox behavior | [`AGENTS.md`](../../AGENTS.md), [`NICELABEL_TEXTBOX_RESEARCH.md`](../NICELABEL_TEXTBOX_RESEARCH.md) | Never relax or silently refactor the contract. |
| Product history and release narrative | [`MASTER_PLAN.md`](../../MASTER_PLAN.md), [`PLAN.md`](../../PLAN.md) | Append a verified checkpoint; preserve old history. |
| Whole-product execution | [`07-execution-plan.md`](07-execution-plan.md) | Close a slice only with named gates and explicit non-claims. |
| Industrial barcode sequence | [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) | Phase status must agree with the competitive matrix before claiming done. |
| Barcode competitive truth | [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md) | Keep “Have”, “Partial” and “Missing” evidence-based; never claim certification. |
| Designer shell / panels | [`NICELABEL_DESIGNER_SHELL_RESEARCH.md`](../NICELABEL_DESIGNER_SHELL_RESEARCH.md), [`industrial-panel-design.md`](../industrial-panel-design.md), [`figma-ui-handoff-template.md`](../figma-ui-handoff-template.md) | Use Figma as a visual reference and WPF runtime evidence as acceptance evidence. |
| Control Center comparison | [`NICELABEL_CONTROL_CENTER_USER_GUIDE.md`](../NICELABEL_CONTROL_CENTER_USER_GUIDE.md) | Research only; do not turn it into a web-LMS claim. |

## Reconciliation queue

These are documentation inconsistencies visible in the current worktree. They are deliberately recorded as **open** until the implementation owner supplies a clean checkpoint and fresh command output.

| Priority | Finding | Required resolution |
| --- | --- | --- |
| P0 · evidence captured, owner commit still open | The `MASTER_PLAN.md` banner describes barcode P0/P1/P2 as shipped at product display `v0.202`, while the historical status heading still says `2026-08-10`. | Use the [current verification checkpoint](11-verification-checkpoint-2026-08-13.md) as the source for display version, build result, 157 registered application checks, 356 xUnit checks and Markdown link audit. After a clean implementation owner commit, append the snapshot to `MASTER_PLAN.md` and `PLAN.md` without deleting history. |
| P0 · closed 2026-08-13 (docs-only) | [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md), [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md), and [`P1_LINEAR_GEOMETRY_NEXT_SLICE.md`](../P1_LINEAR_GEOMETRY_NEXT_SLICE.md) had conflicting P1/P2 next/open wording. | Reconciled in one documentation checkpoint; named build, xUnit, and application gates are recorded in the execution spine and closure record. |
| P1 | [`industrial-panel-design.md`](../industrial-panel-design.md) is labeled “v0.201”, while the product banner points at `v0.202`. | Clarify that the Figma/design-system revision is the design baseline (if that is intended), or update it after a fresh screenshot review. Do not silently equate a design revision with a release version. |
| P1 | [`PLAN.md`](../../PLAN.md) contains later transform/data checkpoints than the current-status narrative in `MASTER_PLAN.md`. | Once the implementation wave is committed, append a single release snapshot to both files and link the detailed execution checkpoint; keep all earlier entries intact. |
| P2 | Several new Markdown files and UI assets are untracked in this worktree. | Include them in the owning implementation checkpoint only after their links, encoding and asset paths pass the repository audit. This handoff does not stage or commit them. |
| P1 | Figma metadata for panels node `8:2` reports two `300 DIP` panels, while `industrial-panel-design.md` documents Workspace `268 DIP` and Properties `280 DIP`. | Treat the values as competing design/reference evidence. Resolve with a named decision and runtime screenshots at target scales before changing WPF widths. |
| P1 | Figma node `18:69` names the third Properties tab `More`, while the product notes call it `Advanced`. | Choose one operator-facing label, update the design/reference note and UI acceptance IDs together, and preserve the three-task tab contract. |
| P1 | The panels Figma Page `0:1` inventory contains shell/panel/Properties/Excel-link frames but no dedicated Database Manager frame, while [`database-manager-module-plan.md`](../database-manager-module-plan.md) specifies a separate manager workflow. | Before a Manager UI slice, record the exact workflow states and either locate/create a dedicated reference or explicitly reuse the shell/panel language; then map stable WPF controls and runtime evidence. |
| P2 | Figma shell node `2:2` carries QA text `GPL-3.0 · v0.201`, while the product banner points at `v0.202`. | Keep the design label as historical reference until a release owner explicitly reconciles it; do not infer release metadata from a Figma text layer. |

### Current implementation baseline (read-only evidence)

The CC-P1 through CC-P8 handoffs are now joined by [`CC_UI_UX_PROGRAM_INDEX.md`](../CC_UI_UX_PROGRAM_INDEX.md), a documentation-only cross-surface map. It preserves the roadmap execution order `CC-P1 -> CC-P2 -> CC-P5 -> CC-P3 -> CC-P4 -> CC-P6 -> CC-P7 -> CC-P8`, assigns one action/data owner per surface, and routes each read-only Figma node to its individual handoff. The index does not add implementation or Figma-edit authority; the individual handoffs remain the state and acceptance owners.

The same index now carries the shared host/navigation gate: current `Shell.*` regions and `PrintCenterWindow` actions are evidence owners, while MainWindow hub vs dedicated local host vs staged P1 entry point remains an owner decision. Proposed `CC.*` AutomationIds are not runtime IDs until a host is selected and target-scale UI Automation evidence is captured.

The P1/P2/P5 handoffs now point directly to that index and declare their upstream ownership: P1 host/readiness, P2 canonical queue/status evidence, and P5 three-source history plus exact-manifest reprint. No new implementation or Figma edit is implied.

The new [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](../CC_P1_P2_P5_HOST_DECISION_PACKET.md) consolidates the three host choices, current WPF/Figma evidence, selection criteria and required owner record. It recommends staged P1 evidence as the review starting point, but does not choose or authorize implementation for the product owner.

The new [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](../CC_P1_P2_P5_READ_MODEL_CONTRACT.md) defines source authority, joins, timestamp basis, conflict handling and surface-specific projections for P1/P2/P5. It is a review contract only; no read-model class or UI is implemented.

The new [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](../CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md) maps the read-model fields to Figma Overview `2:2`, a responsive host-neutral wireframe, proposed `CC.P1.*` AutomationIds and state gates. It does not select a host or authorize code/Figma edits.

The handoff template now carries a Figma escalation protocol: identify a missing state first, inspect metadata read-only, map it to a WPF owner, and close with runtime evidence. No new Figma file or frame is required by the current P1/P2/P5 packet.

The downstream P3/P4/P6/P7/P8 handoffs now point to the same program index and name their bounded owners: revision access, document policy, read-only aggregation, local administration links and deferred automation. These remain documentation-only dependencies; no downstream UI or Figma edit is authorized.

CC-P1 also remains an open UI/UX finding: the roadmap names an Operations Overview, but the current WPF only has a recovery dialog, queue warning and separate setup/history/activation entry points. Follow [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](../CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md) to choose one host surface, define local evidence/time-window semantics and attach target-scale runtime evidence before claiming the overview exists.

CC-P2 is the next queue-management finding: the roadmap names a multi-queue Print Management surface, while the current WPF has printer discovery, saved-queue lookup and one-job spool observation but no fleet table or queue-command service. Follow [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](../CC_P2_PRINT_QUEUE_UI_HANDOFF.md) to approve the M1 read-only host and status taxonomy before designing Pause/Resume/Delete controls.

CC-P5 is the next history/reprint finding: the roadmap names a unified History browser, while current WPF exposes an external CSV shortcut, separate job JSONL/state stores and guarded Print Center actions. Follow [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](../CC_P5_HISTORY_REPRINT_UI_HANDOFF.md) to define source provenance and keep Request → Approve → Prepare → Dispatch explicit before changing UI.

CC-P3 is the next document-library/revision finding: the roadmap names local storage, folders, preview, revision access and later check-out/workflow, while current WPF has only the embedded-template gallery plus saved-file revision recovery. Read-only Control Center metadata routes the browse shell through Documents `3:2` and workflow vocabulary through `7:2`; follow [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md) to choose the local root, host, preview and revision entry points before adding workflow/check-out/ACL behavior.

CC-P4 is the next approval-workflow finding: the roadmap names persisted Draft/InReview/Approved/Published/Rejected states and a policy-on Published print gate, while current source has only a versioned template envelope, normal preflight, and separate hash-chained print-job/reprint approval events. Follow [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](../CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md) to approve migration, actor/audit, transition and policy semantics before adding document actions; do not reuse P5 linked-reprint approval as template approval.

CC-P8 is deliberately deferred automation work: the roadmap names trigger list/start-stop/configuration and filtered automation logs, while current source has only an Excel freshness watcher and the manual manifest/preflight/queue path. Figma Applications `7:88` provides a Web Applications shell and Automation sidebar but no trigger-detail state; follow [`CC_P8_AUTOMATION_UI_HANDOFF.md`](../CC_P8_AUTOMATION_UI_HANDOFF.md) to define one local file-drop contract and provenance/lifecycle gates before any trigger host or Figma edit.

CC-P6 is the next analytics finding: the roadmap names read-only charts and filters over local logs, while current source has per-label CSV, best-effort operation JSONL and hash-chained job state but no Analytics module or authoritative cross-source aggregate. Figma Analytics `5:2` supplies chart/filter density only; follow [`CC_P6_ANALYTICS_UI_HANDOFF.md`](../CC_P6_ANALYTICS_UI_HANDOFF.md) to approve source units, precedence, timezone, redaction and software-counter wording before adding UI or export.

CC-P7 is the next administration finding: the roadmap names a light local admin shell plus retention/alerts options, while current source has local activation, designer/printer preferences, versioned data-source registry/cleanup and local logs but no roles/users/server/sync service. Figma Administration `5:41` supplies broad categories and a sample role table only; follow [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](../CC_P7_ADMINISTRATION_UI_HANDOFF.md) to choose a thin local host, ownership links, retention/recovery and privacy rules before adding UI or mutation.

The current WPF file confirms that the `268/280` and `Advanced` values are not only prose in the untracked panel note; they are already the implementation baseline in the dirty worktree:

| Surface | Current WPF evidence | Implication for the open Figma findings |
| --- | --- | --- |
| Main shell columns | [`MainWindow.xaml`](../../src/ANLAbel.App/MainWindow.xaml#L617) binds the Toolbox column to `268` DIP and [`#L621`](../../src/ANLAbel.App/MainWindow.xaml#L621) binds the Properties column to `280` DIP. | Keep the Figma `8:2` `300/300` panels as a competing reference revision; do not widen the runtime columns from metadata alone. |
| Workspace regions | `Shell.Toolbox`, `Shell.Workspace`, `Shell.Canvas` and `Shell.Properties` are explicit automation regions in the same shell. | A future screenshot/measurement review can compare regions one-to-one without inventing a second shell map. |
| Properties task tabs | [`MainWindow.xaml`](../../src/ANLAbel.App/MainWindow.xaml#L1206) exposes `Label` at [`#L1214`](../../src/ANLAbel.App/MainWindow.xaml#L1214), `Layout` at [`#L1222`](../../src/ANLAbel.App/MainWindow.xaml#L1222) and `Advanced` at [`#L1230`](../../src/ANLAbel.App/MainWindow.xaml#L1230). | Keep `Advanced` as the current operator-facing label; treat Figma node `18:69`'s `More` name as an unresolved design-language variant, not an automatic rename. |
| Panel design note | [`industrial-panel-design.md#L55`](../industrial-panel-design.md#L55) and [`#L82`](../industrial-panel-design.md#L82) independently record `268/280`; [`#L72`](../industrial-panel-design.md#L72) records `Advanced`. | The design note and WPF currently agree. The P1 queue stays open until target-scale runtime screenshots and an explicit design decision reconcile the Figma variants. |

**Interim decision:** for this continuation, the WPF `268/280` widths and `Advanced` label remain the working product baseline. Figma nodes `8:2` and `18:69` remain read-only visual evidence; no code, UI, or Figma edit is authorized by this note. A later UI slice may change either value only with a named decision, target-scale screenshot/measurement, updated automation names, and regression coverage.

## Ordered next work

### 1. Freeze an evidence snapshot

The implementation owner should first reach a clean or explicitly checkpointed worktree, then record:

```text
git status --short
git log -1 --oneline
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

The release snapshot must distinguish application regressions, xUnit tests, build warnings/errors and runtime smoke evidence. A green local test run does not close the hardware, driver, verifier or physical-label gates.

#### Dirty-worktree verification observed 2026-08-13

The commands above were run against the current broad, uncommitted implementation wave. They provide useful engineering evidence, but **do not close the P0 release checkpoint** because `git status` is not clean and the changed files have not been reconciled into an owning commit:

| Check | Result | Scope note |
| --- | --- | --- |
| `dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false` | PASS · 0 warnings · 0 errors · 34.91s | Compile evidence only. |
| `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet` | PASS · 356/356 | xUnit/contract evidence for the current binaries. |
| `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build` | PASS · 157/157, 0 failures | Application regression harness; not a physical-printer smoke test. |

The next owner must rerun the same gates after selecting/staging the intended implementation scope, then attach the clean commit, display version, and any manual UI/hardware evidence before changing the release claim.

### 2. Reconcile the barcode documents

Use [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) as the ordered phase spine and [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md) as the gap matrix. The following documentation checkpoint completed the reconciliation in one change:

1. made the P1/P2 status table, deferred/open list and “next coding slice” agree;
2. preserved the legacy-safe `FrameOwned` behavior and the explicit opt-in `SizedFromX` claim because those gates are green;
3. kept physical verifier, printer-native command, full GS1 registry and hardware certification as open/non-claims because no external evidence exists; and
4. marked `P1_LINEAR_GEOMETRY_NEXT_SLICE.md` as historical closure context so two competing “next slice” documents are not left without a pointer.

#### Barcode documentation reconciliation (2026-08-13)

- P1 is now marked closed for the software geometry slice: logical module count, effective-module readout, opt-in `SizedFromX`, and legacy `FrameOwned` behavior.
- P2 is now marked closed for the software HRI triad: `None`, `Below`, and `Above`, with shared designer/preview/print geometry and clone/save coverage.
- The P1 note is explicitly a closure record; the next open barcode phase is P3.
- Physical verifier/grade, printer-native commands, full GS1/catalog parity, and the dirty-worktree release checkpoint remain open non-claims.

#### Barcode evidence crosswalk (read-only, 2026-08-13)

The current source tree and the green application regression run provide a useful crosswalk. The three barcode documents above now carry the corresponding status wording; the implementation wave remains dirty and is not release-approved:

| Slice | Current source/test evidence | Historical discrepancy | Documentation result |
| --- | --- | --- | --- |
| P0 · X-dimension and print-DPI quantization | `LinearBarcodeModuleContract`, shared print preflight, and the existing P0 regression gates are present; `print preflight blocks undersized linear X-dim at print dpi` and related tests pass. | Research marks P0 done and the execution spine marks P0 done. | Keep one canonical “done” row and carry the same gate names into the matrix; no new claim is needed. |
| P1 · logical modules / opt-in `SizedFromX` · closed 2026-08-13 | [`LinearBarcodeProductionWidth.cs`](../../src/ANLAbel.Printing/RenderPipeline/LinearBarcodeProductionWidth.cs), [`LinearBarcodeModuleContract.cs`](../../src/ANLAbel.Core/Barcode/LinearBarcodeModuleContract.cs), and tests `linear barcode width follows quantized X-dim when SizedFromX`, `compiled scene print uses SizedFromX production width`, and `legacy frame-owned width not auto-sized when X is zero` are present and passed. | The older research wording called P1+ open and the closure note still read as pre-ship. | Resolved across the execution spine, research matrix, deferred list, and P1 closure record. `FrameOwned` remains the legacy default; `SizedFromX` is explicit. |
| P2 · HRI placement · closed 2026-08-13 | [`BarcodeHriLayout.cs`](../../src/ANLAbel.Core/Barcode/BarcodeHriLayout.cs), [`BarcodeHriLayoutTests.cs`](../../src/ANLAbel.UnitTests/BarcodeHriLayoutTests.cs), and application gates cover `None`, `Below`, `Above`, clone/save and shared print geometry. | The older bottom roadmap still listed P2 as next. | Resolved in the phase table, bottom roadmap, research M9 row, and handoff; optional UPC/offset polish remains open. |
| External industrial proof | No physical verifier, printer-native command path, full GS1 certification, or hardware campaign was run in this environment. | Software gates can be over-read as industrial certification. | Kept as explicit non-claims/open gates even when P0–P2 software tests are green. |

### 3. Use Figma only for a concrete UI/UX gate

Read-only Figma metadata has now been checked for shell `2:2`, panels `8:2`, Properties tabs `18:69`, and Excel verification `22:82`. The detailed dimensions and state nodes are recorded in [`figma-ui-handoff-template.md`](../figma-ui-handoff-template.md). Existing references remain the design source; no Figma file was edited or duplicated:

| UI surface | Existing reference | Review question |
| --- | --- | --- |
| Full designer shell | [NiceLabel shell file](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), full frame `2:2` | Do shell regions still map one-to-one to WPF `AutomationId`s without changing Text/TextBox behavior? |
| Frequency-first workspace/panels | [ANLAbel UI exploration](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), overview `8:2`, selected properties `13:2`, tabs `18:69` | Are Layers/Data and Label/Layout/Advanced real task switches, with no duplicate zoom or nested disclosure? |
| Excel link verification | Same Figma file, component `22:82` | Do Not linked / Checking / Verified / Stale / Failed states show evidence and a safe next action? |
| Data Workspace authoring/diagnostics | Same Figma file, Data shell `8:2`/`9:2`, empty/current/settings/checks `9:3`, `9:16`, `9:27`, `9:35`; no transform editor states | Define the first transform-authoring task, then approve shell reuse or name a state-specific reference before adding controls. See [`R4_DATA_WORKSPACE_UI_HANDOFF.md`](../R4_DATA_WORKSPACE_UI_HANDOFF.md). |
| Database Manager | No dedicated frame in panels Page `0:1`; current WPF `DatabaseManagerWindow` has the planned list/detail, Test/Preview/Use/Remove/Cleanup states | Follow [`DATABASE_MANAGER_UI_HANDOFF.md`](../DATABASE_MANAGER_UI_HANDOFF.md): approve current WPF information architecture or name a state-specific reference, then attach runtime click-through evidence. |
| Barcode authoring Properties | Read-only Page `0:1` scan on 2026-08-13 finds only ribbon text layer `1:8`; no barcode-properties/check-digit/HRI state-specific frame. Current WPF card is [`MainWindow.xaml#L1878`](../../src/ANLAbel.App/MainWindow.xaml#L1878) through the barcode validation/readout controls | P1/P2 software slices are closed. For P3, use `18:69`/`13:2` as interim selected-Properties language only; the owner must approve that reuse or name a state-specific node before changing the panel. See [`P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md`](../P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md). |
| Control Center benchmark | [Control Center shells](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) and local crops under `docs/assets/nicelabel-control-center/ui-screens/` | Which operations are evidence-backed local desktop features, and which remain research-only? |

| Control Center benchmark / CC-P1 Operations Overview | [Control Center shells](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Overview `2:2`, future deep-link references `2:37` and `3:85`; local crops under `docs/assets/nicelabel-control-center/ui-screens/` | Which operations are evidence-backed local desktop features, and which remain research-only? Follow [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](../CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md) for the state matrix and owner decision. |

| Control Center benchmark / CC-P2 Print Queue Console | [Control Center Printers shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `2:37`; future local state-specific reference remains open | Which queue fields are real local evidence, which filters are safe, and which command semantics need a separate contract? Follow [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](../CC_P2_PRINT_QUEUE_UI_HANDOFF.md) before changing WPF or Figma. |

| Control Center benchmark / CC-P5 History + controlled reprint | [Control Center History shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `3:85`; detail/reprint/error child states remain open | How should CSV per-label rows, job JSONL and hash-chained state events be projected without losing provenance, and where does the exact-manifest action owner live? Follow [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](../CC_P5_HISTORY_REPRINT_UI_HANDOFF.md) before changing WPF or Figma. |

| Control Center benchmark / CC-P3 Document Library + Revision | [Control Center Documents and Workflow shells](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Documents `3:2`, Workflow `7:2`; selected-file, invalid-file, diff and restore states remain open | Which local-root/folder model and host should own browse and revision access, and which CC-P4 workflow policy is approved? Follow [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md) before changing WPF or Figma. |

| Control Center benchmark / CC-P4 Approval Workflow | [Control Center Workflow shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `7:2`; invalid/permission/audit-failure/print-blocked states remain open | Which state graph, legacy-file migration, local actor model and policy-on print gate are approved, and which host owns transitions? Follow [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](../CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md) before changing WPF or Figma. |

| Control Center benchmark / CC-P8 Applications + Automation | [Control Center Applications shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `7:88`; Automation sidebar `7:109`–`7:123`, History destination `3:101`; trigger configuration/detail states remain open | Is the first task a local file-drop trigger, what claim/deduplication and lifecycle semantics are required, and which WPF host owns it? Follow [`CC_P8_AUTOMATION_UI_HANDOFF.md`](../CC_P8_AUTOMATION_UI_HANDOFF.md) before changing WPF, dispatch code or Figma. |

| Control Center benchmark / CC-P6 Local Analytics | [Control Center Analytics shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `5:2`; chart `5:16`, filters `5:31`; source-health/partial/no-match/detail states remain open | Which local sources and units are authoritative, what timezone/privacy rules apply, and where does History deep-link? Follow [`CC_P6_ANALYTICS_UI_HANDOFF.md`](../CC_P6_ANALYTICS_UI_HANDOFF.md) before changing WPF, logs or Figma. |

| Control Center benchmark / CC-P7 Administration | [Control Center Administration shell](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `5:41`; categories `5:55`, role table `5:69`; local activation/preferences/registry/retention states remain open | Should the product use a thin local Admin hub, and what retention/recovery/privacy policy is approved? Follow [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](../CC_P7_ADMINISTRATION_UI_HANDOFF.md) before changing WPF, settings/logs or Figma. |

Only invoke a Figma inspection/edit when a specific UI slice is selected and the current node does not answer the question. The acceptance artifact should be a screenshot or measured node review at the target window/display scales; a Figma frame alone is not runtime proof. Do not create a second design file for a surface already covered by the references above.

### 4. Close documentation links after the checkpoint

**Superseding audit after the CC-P1 handoff (2026-08-13):** `61` Markdown files, `269` relative links checked, and `0` broken paths. The `60`/`252` figures in the paragraph below are the preceding post-Database-Manager baseline; use this rerun for the current documentation checkpoint.

**Superseding audit after the CC-P2 handoff (2026-08-13):** `62` Markdown files, `291` relative links checked, and `0` broken paths. The `61`/`269` figures above are the preceding CC-P1 baseline.

**Superseding audit after the CC-P5 handoff (2026-08-13):** `63` Markdown files, `311` relative links checked, and `0` broken paths. The `62`/`291` figures above are the preceding CC-P2 baseline.

**Superseding audit after the CC-P3 handoff (2026-08-13):** `64` Markdown files, `329` relative links checked, and `0` broken paths. The `63`/`311` figures above are the preceding CC-P5 baseline; external URLs remain outside this local-path audit.

**Superseding audit after the CC-P4 handoff (2026-08-13):** `65` Markdown files, `347` relative links checked, and `0` broken paths. The `64`/`329` figures above are the preceding CC-P3 baseline; external URLs remain outside this local-path audit.

**Superseding audit after the CC-P8 handoff (2026-08-13):** `66` Markdown files, `365` relative links checked, and `0` broken paths. The `65`/`347` figures above are the preceding CC-P4 baseline; external URLs remain outside this local-path audit.

**Superseding audit after the CC-P6 handoff (2026-08-13):** `67` Markdown files, `383` relative links checked, and `0` broken paths. The `66`/`365` figures above are the preceding CC-P8 baseline; external URLs remain outside this local-path audit.

**Superseding audit after the CC-P7 handoff (2026-08-13):** `68` Markdown files, `406` relative links checked, and `0` broken paths. The `67`/`383` figures above are the preceding CC-P6 baseline; external URLs remain outside this local-path audit.

**Superseding audit after the CC UI/UX program index (2026-08-13):** `69` Markdown files, `446` relative links checked, and `0` broken paths. The `68`/`406` figures above are the preceding CC-P7 baseline; external URLs remain outside this local-path audit.

**Superseding audit after the P1/P2/P5 host decision packet (2026-08-13):** `70` Markdown files, `470` relative links checked, and `0` broken paths. The `69`/`446` figures above are the preceding CC UI/UX program baseline; external URLs remain outside this local-path audit.

**Superseding audit after the Figma escalation protocol (2026-08-13):** `70` Markdown files, `472` relative links checked, and `0` broken paths. The `70`/`470` figures above are the preceding host decision packet baseline; external URLs remain outside this local-path audit.

**Superseding audit after the P1/P2/P5 read-model contract (2026-08-13):** `71` Markdown files, `484` relative links checked, and `0` broken paths. The `70`/`472` figures above are the preceding Figma escalation protocol baseline; external URLs remain outside this local-path audit.

**Superseding audit after the P1 Operations Overview UI spec (2026-08-13):** `72` Markdown files, `494` relative links checked, and `0` broken paths. The `71`/`484` figures above are the preceding read-model contract baseline; external URLs remain outside this local-path audit.

The new [`CC_P2_PRINT_QUEUE_UI_SPEC.md`](../CC_P2_PRINT_QUEUE_UI_SPEC.md) maps the read-only Figma Printers node `2:37` to local queue discovery, saved-queue lookup, job-scoped spool evidence, responsive table/detail behavior and proposed AutomationIds. It does not authorize a queue console, command service, host choice, or Figma edit.

**Superseding audit after the P2 Print Queue UI spec (2026-08-13):** `73` Markdown files, `504` relative links checked, and `0` broken paths. The `72`/`494` figures above are the preceding P1 UI spec baseline; external URLs remain outside this local-path audit.

The new [`CC_P5_HISTORY_REPRINT_UI_SPEC.md`](../CC_P5_HISTORY_REPRINT_UI_SPEC.md) maps read-only Figma History `3:85` to a provenance-first activity/detail surface over state-store, operation JSONL and CSV detail, with explicit Request → Approve → Prepare → Dispatch boundaries. It does not authorize a History host, a new reprint owner, runtime merge code or a Figma edit.

**Superseding audit after the P5 History + reprint UI spec (2026-08-13):** `74` Markdown files, `514` relative links checked, and `0` broken paths. The `73`/`504` figures above are the preceding P2 UI spec baseline; external URLs remain outside this local-path audit.

The new [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md`](../CC_P3_DOCUMENT_LIBRARY_REVISION_UI_SPEC.md) maps read-only Figma Documents `3:2` to configured-root/built-in browse, validated file metadata, primary/backup/archive revision evidence and guarded restore. It does not authorize a document browser, workflow/check-out/ACL controls, or a Figma edit.

**Superseding audit after the P3 Document Library + Revision UI spec (2026-08-13):** `75` Markdown files, `524` relative links checked, and `0` broken paths. The `74`/`514` figures above are the preceding P5 UI spec baseline; external URLs remain outside this local-path audit.

The new [`CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md`](../CC_P4_APPROVAL_WORKFLOW_UI_SPEC.md) maps read-only Figma Workflow `7:2` to candidate document states, explicit actor/audit evidence and a fail-closed policy-on print boundary. It does not authorize a workflow enum/store, permissions model, Published gate, runtime UI or a Figma edit; P5 linked-reprint approval remains separate.

**Superseding audit after the P4 Approval Workflow UI spec (2026-08-13):** `76` Markdown files, `534` relative links checked, and `0` broken paths. The `75`/`524` figures above are the preceding P3 UI spec baseline; external URLs remain outside this local-path audit.

The new [`CC_P6_ANALYTICS_UI_SPEC.md`](../CC_P6_ANALYTICS_UI_SPEC.md) maps read-only Figma Analytics `5:2` to source-backed local metrics with separate label/job/event units, source-health/partial-data states, safe filters and P5 History deep-links. It does not authorize an Analytics window, telemetry, physical-output count or Figma edit.

**Superseding audit after the P6 Local Analytics UI spec (2026-08-13):** `77` Markdown files, `544` relative links checked, and `0` broken paths. The `76`/`534` figures above are the preceding P4 UI spec baseline; external URLs remain outside this local-path audit.

The [current verification checkpoint](11-verification-checkpoint-2026-08-13.md) records the earlier audit at 57 Markdown files and 209 relative links. The latest docs-only audit after the P3/R4 handoffs and Database Manager UI handoff is 60 Markdown files, 252 relative links, and 0 broken paths. After the implementation owner reaches a clean checkpoint, rerun it and check that every newly named test or version appears in the file that owns that claim. Broken links, stale test counts and contradictory “next” labels remain open findings, not cosmetic cleanup.

## Definition of done for this handoff

- [ ] A clean implementation checkpoint exists; this note is not evidence that the current dirty worktree is releasable.
- [ ] `MASTER_PLAN.md`, `PLAN.md`, the reinvention execution plan and barcode documents agree on the current release snapshot.
- [ ] Historical entries remain intact and future/open hardware claims stay explicitly non-claims.
- [ ] Any UI change has a named Figma node (or an explicit reason to create one), a runtime screenshot/measurement gate and regression coverage.
- [ ] Text/TextBox protected behavior remains unchanged unless the user explicitly reopens that contract and updates its required docs/tests together.

## Handoff note

This file is a coordination aid, not a substitute for the owning implementation agent’s commit. It should be linked from the next verified checkpoint and then retained as the audit trail for the 2026-08-13 documentation reconciliation.
