# ANLAbel — CC-P1/P2/P5 host and navigation decision packet

**Status:** owner review required; documentation-only (2026-08-13)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Roadmap source:** [`MASTER_PLAN.md`](../MASTER_PLAN.md#control-center--lms-operations--large-improvement-plans-2026-08-12)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`

This packet turns the repeated P1/P2/P5 host question into one reviewable decision. It does not select a host on behalf of the product owner, add a WPF window, change navigation, or authorize a Figma edit. The individual handoffs remain authoritative for state matrices and acceptance details:

- [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md) — host/readiness and local operations evidence;
- [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md) — canonical queue/status and read-only spool evidence;
- [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md) — three-source history, provenance and exact-manifest reprint.

## 1. Decision to make

Choose exactly one first host for the upstream sequence `CC-P1 -> CC-P2 -> CC-P5`:

| Option | Host shape | What it proves first | Main risk | Figma route |
| --- | --- | --- | --- | --- |
| A | Extend `PrintCenterWindow` with a summary header and read-only tabs | Recovery, queue and history can share one local action owner immediately | Recovery dialog becomes a broad operations shell before its information architecture is measured | Overview `2:2` for summary; Printers `2:37` and History `3:85` only as deep-link references |
| B | Add a dedicated local `OperationsOverviewWindow` / `ControlCenterWindow` | A stable local operations home can host cards, navigation and future read-only modules | Adds a new window, focus/close path and AutomationId surface before the read model is proven | Overview `2:2`, primary navigation `2:6`, cards `2:16`, `2:20`, `2:25`, `2:30` |
| C | Stage a P1-only entry point that proves the read model, then add persistent navigation | Local queue/recovery/activation evidence and deep links can be verified without committing to a multi-module shell | Temporary navigation may need one follow-up migration when P2/P5 are ready | Overview `2:2` first; defer Printers `2:37` and History `3:85` integration |

### Recommendation for review

Use **Option C as the next implementation gate** unless the product owner explicitly prefers an existing-window extension. It limits the first change to evidence refresh, honest cards and explicit deep links; it does not require a second dispatch stack or a permanent navigation shell before P1 data semantics are known. This is a recommendation for review, not an implementation decision.

Option A is reasonable if the owner wants to keep recovery as the single visible operations surface. Option B should wait until the P1 read model, host dimensions, focus behavior and stable `CC.*` AutomationIds have runtime evidence.

## 2. Evidence already available

### WPF action and shell owners

| Evidence | Current owner | Constraint for the decision |
| --- | --- | --- |
| Designer shell regions | [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L375) exposes `Shell.QuickAccess`, [`#L434`](../src/ANLAbel.App/MainWindow.xaml#L434) exposes `Shell.Ribbon`, [`#L695`](../src/ANLAbel.App/MainWindow.xaml#L695) exposes `Shell.Workspace`, [`#L1012`](../src/ANLAbel.App/MainWindow.xaml#L1012) exposes `Shell.Canvas`, [`#L1082`](../src/ANLAbel.App/MainWindow.xaml#L1082) exposes `Shell.Properties`, and [`#L561`](../src/ANLAbel.App/MainWindow.xaml#L561) exposes `Shell.Status`. | These are existing designer AutomationIds, not Control Center module IDs. Preserve the protected Text/TextBox contract and do not rename or repurpose them merely to match Figma. |
| Recovery actions | [`PrintCenterWindow.xaml`](../src/ANLAbel.App/PrintCenterWindow.xaml#L96) owns reconcile, acknowledge, void, linked reprint, approved preview and support-evidence export. | P1/P5 must link to this owner; no card or table row may silently dispatch, retry or create a second reprint path. |
| Queue observation | `MainViewModel.PrinterQueueStatus` and named-queue refresh are recorded in [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md) and [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md). | Missing/unverified named queues remain warnings; no Windows-default fallback. |
| History sources | CSV per-label history, best-effort operation JSONL and hash-chained job-state evidence are recorded in [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md). | A unified view is a read model, not a rewritten audit ledger. |

### Figma structural references

| Slice | Existing nodes | Use in host review | Missing runtime proof |
| --- | --- | --- | --- |
| P1 | Overview `2:2`; top bar `2:3`; primary nav `2:6`; context `2:16`; workstation `2:20`; license `2:25`; errors `2:30` | Card hierarchy, information grouping and visual density only | Local timestamps, queue failure, empty/recovery states, keyboard path and WPF scale behavior |
| P2 | Printers `2:37`; filter rail `2:51`; main pane `2:72` | Read-only filter/table proportions and deep-link destination | Queue capability, stale/ambiguous spool state, enumeration failure and command deferral |
| P5 | History `3:85`; filters `3:99`; activity table `3:101`; detail note `3:109` | Activity/filter/detail information architecture | Three-source provenance, corrupt tail, local time/privacy and exact-manifest action gating |

The metadata was inspected read-only. It does not authorize a browser, server identity, license-seat totals, multi-user navigation or a new Figma frame. If a concrete state is absent, record an explicit WPF reuse decision or request the smallest state-specific reference after the host is chosen.

## 3. Selection criteria

The owner decision is acceptable only if the selected option satisfies every criterion:

1. **Single action owner:** queue observation, recovery, reprint, history export and setup retain their existing services and deep-link targets.
2. **Local evidence truth:** every card/filter/table field names its source, timestamp basis and stale/empty/error behavior.
3. **No implicit dispatch:** P1/P2/P5 read surfaces never auto-retry, cancel, print, approve or substitute a queue.
4. **Navigation contract:** the host names its root, content region, status region, close/return path, disabled/unimplemented behavior and intentional scroll owner.
5. **Accessibility contract:** stable AutomationIds/names, keyboard traversal and focus restoration are recorded before the slice is called verified.
6. **Scale evidence:** runtime screenshot or UI Automation evidence exists at `1024 x 600`, `100%`, `125%` and `150%` (or a documented environment exception).
7. **Protected behavior:** no Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow, designer/print parity or Properties preset changes.
8. **Figma boundary:** Figma dimensions and sample rows are design input only; missing states remain explicit open evidence gaps.

## 4. Required decision record

Before implementation, append one short owner decision to this packet with:

| Field | Required value |
| --- | --- |
| Selected option | `A`, `B` or `C` |
| Host owner | Existing window, new local window or staged entry point; name the owning file/class when known |
| P1 refresh owner | Command/service, source paths and last-refresh timestamp behavior |
| P2 queue owner | Canonical queue identity, queue-vs-job fields and stale/error behavior |
| P5 history owner | Source precedence, local timezone, privacy/redaction and corrupt-tail behavior |
| Deep links | Print Center, Printer Setup, History/export and return path |
| Automation vocabulary | Final IDs/names; distinguish proposed `CC.*` IDs from existing `Shell.*` IDs |
| Runtime evidence owner | Screenshot/UI Automation owner and target scale matrix |
| Figma decision | Reuse listed nodes or request a smallest state-specific reference; never infer runtime proof |
| Non-claims | Physical verifier, printer-native completion, multi-tenant/server identity and license-seat parity remain open unless separately evidenced |

## 5. Close gate for P1 → P2 → P5

The sequence can move from host review to implementation only when the owner record above exists and:

- P1 has a refresh/read-model fixture for healthy, empty, stale, missing-queue, pending-recovery, terminal-fault and activation-unavailable states;
- P2 has read-only discovery/status fixtures and preserves canonical queue identity without default fallback;
- P5 has a source/provenance fixture for CSV, operation JSONL, state-store events, malformed tail and exact-manifest mismatch;
- all three surfaces share one deep-link and action owner;
- the selected host has the target-scale keyboard/focus/scroll evidence;
- the relevant handoff and [`11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md) are updated without changing protected Text/TextBox behavior.

Until then, this packet remains an open decision record. It is not release approval, UI implementation evidence or a Figma design request.
