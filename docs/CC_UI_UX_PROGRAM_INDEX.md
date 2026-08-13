# ANLAbel — Control Center UI/UX program index

**Status:** documentation coordination index; no UI implementation or Figma edit is authorized (2026-08-13)
**Roadmap source:** [`MASTER_PLAN.md`](../MASTER_PLAN.md), Control Center / LMS / Operations large improvement plans
**Execution checkpoint:** [`reinvention/10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md)
**Verification boundary:** [`reinvention/11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md)
**Figma routing template:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)

This index is the cross-surface map for the CC-P1…P8 Markdown handoffs. It prevents a research frame from being mistaken for a shipped WPF feature, keeps one action owner per operation, and records the dependency order before any UI or Figma work begins. The individual handoff remains authoritative for its state matrix and acceptance details.

## 1. Program status at a glance

| Slice | Product surface | Current evidence | Figma route | Status / next gate |
| --- | --- | --- | --- | --- |
| CC-P1 | Operations Overview | `PrintCenterWindow`, queue status, activation and history entry points exist separately; no unified overview. | [Overview `2:2`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) (`1280 × 800`) | [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md) · choose host, local cards/time window, runtime evidence. |
| CC-P2 | Print Queue Console | Queue discovery, named-queue lookup and one-job spool observation; no fleet table or command service. | Printers `2:37` (`1280 × 800`) | [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md) · approve read-only host/status taxonomy; defer mutations. |
| CC-P5 | History + controlled reprint | CSV per-label history, best-effort operation JSONL, hash-chained job state, Print Center/reprint guard. | History `3:85` (`1280 × 800`) | [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md) · define read model/provenance/time/privacy; one reprint owner. |
| CC-P3 | Document Library + Revision | Embedded gallery plus primary/`.bak`/`.revisions` inspection, semantic diff and validated restore; no local-root browser. | Documents `3:2` (`1280 × 800`) | [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md) · choose root/host/preview/revision entry points. |
| CC-P4 | Approval Workflow | Versioned envelope and print preflight exist; no document state enum, transition store, actor policy or Published gate. | Workflow `7:2` (`1280 × 800`) | [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md) · approve migration/audit/policy contract; separate from P5 reprint approval. |
| CC-P6 | Local Analytics | CSV/JSONL/state evidence exists; no cross-source aggregate or Analytics window. | Analytics `5:2` (`1280 × 800`) | [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md) · approve units/source precedence/timezone/redaction; read-only only. |
| CC-P7 | Administration (light) | Local activation, designer/printer preferences, data-source registry/cleanup and local logs; no multi-user admin service. | Administration `5:41` (`1280 × 800`) | [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md) · choose thin local host and retention/recovery/privacy rules. |
| CC-P8 | Applications / Automation | Excel freshness watcher and manual manifest/preflight/queue path; no trigger host. | Applications `7:88` (`1280 × 800`); History destination `3:101` | [`CC_P8_AUTOMATION_UI_HANDOFF.md`](CC_P8_AUTOMATION_UI_HANDOFF.md) · deferred; approve one local file-drop contract before implementation. |

All rows are roadmap/design evidence, not release approval. “Current evidence” means source/test artifacts observed in the checkout; it does not imply runtime click-through, physical printer completion, cloud parity or multi-user identity.

## 2. Dependency and ownership order

The roadmap’s suggested build order is preserved:

```text
P1 Operations home
  └─ P2 Queue read model ──┐
                           ├─ P5 History/read model + controlled reprint
                           │    ├─ P3 Document library + revision
                           │    │    └─ P4 Document approval policy/gate
                           │    ├─ P6 Local analytics (read-only deep-links)
                           │    └─ P8 Automation (later; same print spine)
                           └─ P7 Local administration (links/retention; roles later)
```

This is a sequencing constraint, not a claim that earlier slices are shipped. Before changing a downstream surface:

1. the upstream evidence path must have an owner and a named runtime/automation gate;
2. the downstream slice must reuse the upstream source/action owner rather than copy its data or dispatch path;
3. unresolved source, identity, privacy, or physical-verifier gaps remain visible in the UI and handoff.

The upstream handoffs now carry this routing note directly: [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md) owns the host/readiness gate, [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md) owns canonical queue/status evidence, and [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md) owns the three-source read model and exact-manifest reprint gate.

The downstream handoffs carry the same boundary: [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md) owns local revision access, [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md) owns document policy/audit, [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md) owns read-only aggregation, [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md) owns local settings/retention links, and [`CC_P8_AUTOMATION_UI_HANDOFF.md`](CC_P8_AUTOMATION_UI_HANDOFF.md) owns the deferred trigger contract. None may create a second action or dispatch authority.

| Action/data owner | Reused by | Never duplicate |
| --- | --- | --- |
| Queue discovery, effective ticket and spool observation | P1, P2, P5, P8 | Queue mutation or a second queue-success definition. |
| `PrintJobStateStore` / manifest lineage / Print Center | P1, P5, P6, P8 | Reprint, void, retry or lifecycle state in Analytics/Automation. |
| `TemplateLibraryService` / `ProjectRevisionService` | P3, P4, P7 links | A second revision/archive or silent materialization path. |
| Document workflow store (future) | P3/P4, print policy | Print-job approval store or best-effort operation JSONL. |
| Local CSV/JSONL/state read model (future) | P5, P6, P1 | A flattened “all successful” counter. |
| Activation/preferences/registry services | P1/P7 | Figma server seats, roles, users or sync semantics. |
| Manual preflight → manifest → queue dispatch | P8 | Trigger-specific low-level printer calls or automatic retry. |

## 3. Figma node map and evidence boundary

All metadata below was checked read-only in Control Center file key `asnGsLMxceJWb3HlfaE3q4` on 2026-08-13. The file has one page `0:1` (`NiceLabel Control Center`). No frame was edited, duplicated or treated as runtime proof.

| Node | Name / role | Key measured regions | Missing states that require WPF evidence |
| --- | --- | --- | --- |
| `2:2` | CC / Overview | Overview shell `1280 × 800`; workstation/license/error card hierarchy | Local evidence health, empty/error, queue/activation failure and deep-link behavior. |
| `2:37` | CC / Printers — Print Management | Filter rail `2:51` `220 × 680`; main pane `2:72` `1000 × 680` | Capability/command outcomes, unavailable queue, stale/ambiguous spool state. |
| `3:2` | CC / Documents — Storage | Toolbar `3:16`; folder rail `3:19` `240 × 620`; file pane `3:29` `980 × 620` | Selected detail, invalid file, diff, restore, dirty edit and check-out state. |
| `7:2` | CC / Documents — Workflow | Sidebar `7:23` `220 × 229`; workflow main `7:37`; action row `7:59`; history `7:69` | Unknown/migrated state, permission/audit failure, stale revision and policy-blocked print. |
| `3:85` | CC / History | Filter bar `3:99` `1248 × 56`; activity frame `3:101` `1248 × 600` | Three-source merge/provenance, corrupt tail, local time, privacy and controlled reprint detail. |
| `5:2` | CC / Analytics | Chart `5:16` `820 × 520`; filters `5:31` `400 × 520` | Source health, partial data, no-match, units/tooltips, export and physical-verifier disclaimer. |
| `5:41` | CC / Administration | Sidebar `5:55` `240 × 680`; role table `5:69` `980 × 680` | Local activation/preferences/registry/retention and unsupported server-category states. |
| `7:88` | CC / Applications — Web Apps | Automation sidebar `7:109`–`7:123`; web-app main `7:124` `1060 × 380` | Trigger configuration/detail, claim/deduplication, stop/restart, quarantine and security states. |
| `3:101` | History destination for automation | Submitted/type/module/workstation/user/status rows `3:102`–`3:109` | Trigger identity/privacy/retention and explicit job linkage. |

Rule: if the existing node does not answer a concrete state question, first record an owner-approved WPF reuse decision or request a smallest state-specific Figma reference. Do not create a new design file just to fill missing runtime states.

## 4. Shared acceptance gates

These gates apply to every CC slice and must be attached to the owning handoff before it can be called implemented:

| Gate | Required evidence | Non-claim |
| --- | --- | --- |
| Source truth | Named service/file, field provenance, stale/partial/corrupt behavior and timestamp basis | UI chrome or sample Figma rows are not live data. |
| Action ownership | One command owner for queue, history/reprint, revision, workflow, data source, activation and export | No second dispatch/reprint/archive/retention stack. |
| Runtime | Screenshot or UI Automation at `1024 × 600`, `100%`, `125%`, `150%` (or explicit environment exception) | Figma dimensions alone do not prove WPF reachability, clipping or keyboard behavior. |
| Accessibility | Stable AutomationIds/names, keyboard path, focus order, disabled/error copy and intentional scroll owner | A visual match at one scale is insufficient. |
| Data safety | Dirty/invalid/future-schema/permission/audit failure and cancellation paths are explicit and non-destructive | “Exists”, “queued”, “completed” or “logged” is not automatically “safe” or “physical.” |
| Print parity | Preview, preflight, manifest and dispatch use the same effective plan/output contract | No Text/TextBox geometry or ownership changes. |
| Regression | Named application regression plus unit/contract tests for pure policy/read-model rules | Green tests do not prove physical verifier or driver certification. |
| External evidence | Physical verifier, driver, printer, network, identity or security evidence explicitly marked open when unavailable | No Control Center/LMS, multi-tenant, cloud or physical-output claim without evidence. |

## 5. Shared host and navigation gate

The CC slices need one host decision before they add navigation or a second command surface. The current WPF shell is real evidence, but it is the Label Designer shell, not a shipped Control Center host:

| Current source region | Evidence | Boundary |
| --- | --- | --- |
| Quick access / ribbon / status | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L375) exposes `Shell.QuickAccess`; [`#L434`](../src/ANLAbel.App/MainWindow.xaml#L434) exposes `Shell.Ribbon`; [`#L561`](../src/ANLAbel.App/MainWindow.xaml#L561) exposes `Shell.Status`. | Reuse is possible only through an owner-approved host decision; these regions do not prove a CC navigation shell. |
| Workspace / canvas / properties | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L635) exposes `Shell.Toolbox`; [`#L695`](../src/ANLAbel.App/MainWindow.xaml#L695) exposes `Shell.Workspace`; [`#L1012`](../src/ANLAbel.App/MainWindow.xaml#L1012) exposes `Shell.Canvas`; [`#L1082`](../src/ANLAbel.App/MainWindow.xaml#L1082) exposes `Shell.Properties`. | Preserve the existing designer shell and the protected Text/TextBox contract; do not reinterpret these IDs as CC module IDs. |
| Recovery / controlled actions | [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L96) owns reconcile, acknowledge, void, linked reprint, approved preview and support-evidence actions. | P1/P5 must deep-link to this owner rather than create a second dispatch/reprint stack. |

### Host choice remains open

The owner must choose one of these bounded options before a CC navigation implementation:

1. a `MainWindow` hub that deep-links to existing windows and keeps the designer shell as the primary host;
2. a dedicated local `ControlCenterWindow` that reuses the same services and stable action owners; or
3. a staged P1-only entry point that proves the read model before adding a persistent navigation shell.

The Figma Overview/Printers/History/Documents/Workflow/Analytics/Administration/Applications frames are visual references for those options. They do not authorize a browser, multi-tenant identity, server license seats or new WPF windows by themselves.

### Proposed future navigation vocabulary

If a host is approved, reserve a stable vocabulary such as `CC.Root`, `CC.Nav.Overview`, `CC.Nav.Printers`, `CC.Nav.History`, `CC.Nav.Documents`, `CC.Nav.Workflow`, `CC.Nav.Analytics`, `CC.Nav.Administration`, `CC.Nav.Automation`, `CC.Content` and `CC.Status`. These are proposals, not current AutomationIds; the owner must reconcile them with existing `Shell.*` IDs and attach UI Automation evidence at `1024 x 600`, `100%`, `125%` and `150%`.

Before any slice closes, the host gate must name the navigation owner, disabled/not-implemented behavior, keyboard/focus path, scroll owner and deep-link target. Missing Figma states should be recorded as WPF evidence gaps or an explicit reuse decision; do not create a new Figma file just to make the shell appear complete.

## 6. Cross-surface owner decisions still open

1. Host choice for P1/P2/P3/P5/P6/P7 and stable AutomationId vocabulary.
2. Local queue/status/time/privacy semantics and P5 three-source precedence.
3. P3 root/preview/revision entry points and P4 workflow migration/actor/audit/print policy.
4. P6 source units/timezone/redaction and P7 retention/recovery scope.
5. P8 local file-drop trigger claim/deduplication semantics and prerequisite policy gate.
6. Whether any future UI needs a new state-specific Figma node; if so, identify the smallest state and keep the existing file.

## 7. Current decision

**Program is mapped; all slices remain open until their individual gates close.** The Markdown handoffs and Figma metadata now cover the roadmap’s CC-P1…P8 surfaces without authorizing code or design edits. The next implementation decision should select one upstream slice (P1/P2/P5) and attach runtime evidence before moving to downstream P3/P4/P6/P7/P8 work.
