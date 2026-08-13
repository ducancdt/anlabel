# ANLAbel — CC-P1 Operations Overview UI/UX spec

**Status:** design-only content spec; host not selected (2026-08-13)
**Host decision:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Evidence contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Handoff:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md)
**Concrete owner decision packet:** [`CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md`](CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, Overview `2:2`

This spec defines the P1 content contract independently of the eventual WPF host. It is intended to fit Option C (staged P1 entry point) from the host packet, but it does not select that option or authorize a new window. It does not copy server, license-seat or workstation claims from the research shell and does not change the protected Text/TextBox contract.

The concrete source/action boundary, queue timestamp and recent-terminal-fault gaps, activation build boundary and D1-D10 owner rows are recorded in [`CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md`](CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md); this file remains the host-neutral content contract.

## 1. Operator outcome

From one local operations surface, the operator should be able to answer:

1. Is the explicitly saved industrial queue available and when was it last checked?
2. Are there durable print/recovery events requiring an explicit decision?
3. What local activation/entitlement state applies?
4. Which existing owner opens Printer Setup, Print Center or History without an implicit print/retry action?

The overview is a read/route surface. It is not a dispatcher, retry engine, history ledger or physical-output verifier.

## 2. Figma node map (read-only)

Metadata for `2:2` was rechecked read-only on 2026-08-13. The node names below are the actual metadata names; “role” is the ANLAbel mapping, not a Figma product claim.

| Figma node | Metadata name / bounds | ANLAbel role | Copy/data boundary |
| --- | --- | --- | --- |
| `2:2` | `CC / Overview`, `1280 x 800` | Content reference and visual density baseline | Not a WPF size mandate. |
| `2:3` | `TopBar`, `1280 x 48` | Optional host context/header reference | Do not copy server identity or Help behavior without local owner. |
| `2:6` | `Frame`, `1280 x 40` | Primary-navigation area reference | Show only local modules that exist; no web/LMS navigation claim. |
| `2:16` | `Frame`, `1240 x 72` | Local context/refresh banner | Replace “server name/time” with saved queue, local workstation and refresh basis. |
| `2:20` | `Frame`, `820 x 180` | Queue/recovery summary region | Empty state is honest; no fleet/workstation count without local evidence. |
| `2:25` | `Frame`, `400 x 180` | Activation/entitlement card | Never copy `LMS Enterprise` or `Used: 0 Total: 100`. |
| `2:30` | `Frame`, `1240 x 200` | Recent software fault/recovery region | Use local diagnostics and source timestamps; sample users/statuses are not fixtures. |
| `2:35` | `Frame`, `1240 x 44` | Research footer only | Omit or replace with local support/version copy; do not copy research/version text. |

No additional Figma node is required for the content spec. A future missing state follows the [Figma escalation protocol](figma-ui-handoff-template.md#figma-escalation-protocol), not an automatic new frame.

## 3. Content wireframe contract

The outer host may be `PrintCenterWindow`, a dedicated local window or a staged entry point. The content order below remains stable across those choices:

```text
[Operations context + Refresh + last successful refresh]

[Queue health]       [Recovery / durable faults]
[Activation status]  [Explicit actions: Print Center | Printer Setup | History]

[Recent software events / recovery diagnostics — intentional table scroll owner]
```

### Region contract

| Region | Required content | Source/owner | Safe action |
| --- | --- | --- | --- |
| Context | Local queue name (requested/canonical when available), last refresh, source status and diagnostics badge | `PrinterQueueLookupResult` + read-model refresh | Refresh; open Printer Setup when unavailable |
| Queue health | `Not selected`, `Available`, `Unavailable`, canonical mismatch, lookup error and observation age | `MainViewModel.PrinterQueueStatus`; P1/P2 read-model contract | Printer Setup; never default fallback |
| Recovery | Pending candidate count, repair-required flag, latest reason/severity and last event time | `PrintJobRecoveryReport` / `PrintJobStateStore` | Open Print Center; reconcile/acknowledge/void explicitly |
| Activation | Local activation/entitlement status and expiry/limited-mode copy only when supplied by the local service | `TrialLicenseService` / `ActivationWindow` | Open Activation; no server-seat wording |
| Explicit actions | `Open Print Center`, `Open Printer Setup`, `Open History`/export, `Refresh` | Existing `MainWindow` handlers and `PrintCenterWindow` owner | Deep-link only; no card click dispatch |
| Recent events | Read-only rows with source, JobId when available, lifecycle/action, queue/spool observation, timestamp basis and diagnostic state | P1/P2/P5 read-model contract | Select/detail or open Print Center; no inline reprint |

## 4. Responsive behavior

Figma `1280 x 800` is a visual reference; the WPF content must be designed for the evidence gate, not scaled blindly.

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May use the research proportion: wide queue/recovery region beside narrower activation card, with recent events below. | Host owns outer navigation; overview owns one vertical content scroll owner. |
| `1024 x 600` | Reflow summary cards to two columns or a single column; keep context and explicit actions reachable without horizontal page scroll. Recent events use the intentional table scroll owner. | Keyboard order is context → queue → recovery → activation → actions → recent events; focus survives refresh. |
| `100%`, `125%`, `150%` display scale | Preserve text legibility, card minimums, disabled/error copy and visible action names; do not depend on Figma DIP dimensions. | Capture screenshot/UI Automation at each scale; record any environment exception. |

The overview must not introduce a second horizontal scroll owner merely to preserve the 1280-wide reference. If a table needs horizontal scrolling for diagnostic fields, the table itself owns it and the focus path remains documented.

## 5. Proposed automation vocabulary

These are proposals until the host decision is recorded. They must not replace existing `Shell.*` IDs in the designer window.

| Region/control | Proposed AutomationId | Proposed accessible name |
| --- | --- | --- |
| Root | `CC.P1.Overview.Root` | `Operations overview` |
| Refresh | `CC.P1.Overview.Refresh` | `Refresh operations evidence` |
| Context | `CC.P1.Overview.Context` | `Operations context` |
| Queue card | `CC.P1.Overview.QueueCard` | `Saved printer queue health` |
| Recovery card | `CC.P1.Overview.RecoveryCard` | `Print recovery status` |
| Activation card | `CC.P1.Overview.ActivationCard` | `Local activation status` |
| Print Center link | `CC.P1.Overview.OpenPrintCenter` | `Open Print Center` |
| Printer Setup link | `CC.P1.Overview.OpenPrinterSetup` | `Open Printer Setup` |
| History link | `CC.P1.Overview.OpenHistory` | `Open Print History` |
| Recent events table | `CC.P1.Overview.RecentEvents` | `Recent print events` |
| Diagnostics | `CC.P1.Overview.Diagnostics` | `Evidence diagnostics` |

Final IDs require owner approval and UI Automation evidence. They are not a request to edit `MainWindow.xaml` now.

## 6. User-visible state matrix

| State | Required copy/evidence | Safe action | Explicit non-claim |
| --- | --- | --- | --- |
| Loading | Busy state, disabled duplicate refresh, last successful refresh if any | Wait/cancel if supported | Current values are not implied. |
| Healthy queue / no recovery | Canonical queue, observation age, `No print jobs need reconciliation` | Open setup/center/history | Queue health is not physical output. |
| Queue unavailable | Requested name, lookup error/canonical mismatch and last known timestamp | Open Printer Setup | No default queue substitution. |
| Pending recovery | Candidate count, latest JobIds/reasons and repair flag | Open Print Center | No automatic retry or dispatch. |
| Terminal software fault | Source, lifecycle/action, reason and timestamp basis | Inspect/detail/support evidence | Failure is not rewritten as success. |
| Store diagnostic | Valid-prefix notice and repair-required copy | Open Print Center/support path | No reprint while the state log is damaged. |
| Activation unavailable | Local status, limited-mode/repair copy and Activation link | Open Activation | No LMS server or seat-total claim. |
| No recent activity | Source availability and `No recent recovery activity` | Refresh or continue safely | Empty local evidence does not prove no physical print elsewhere. |
| Deep-link unavailable | Non-destructive error and return path | Close/return; use existing shell action | No hidden second authority. |

## 7. Acceptance gate

The P1 content spec is ready for implementation review only when:

- the host packet records Option A/B/C and the navigation owner;
- the read-model contract supplies source, timestamp and diagnostic fields for every card/table value;
- fixtures cover loading, healthy, unavailable, pending, terminal, diagnostic, activation and empty states;
- explicit links reuse existing Print Center, Printer Setup and History owners;
- keyboard/focus/scroll behavior is measured at `1024 x 600`, `100%`, `125%`, `150%`;
- proposed `CC.P1.*` IDs are reconciled with the final host and tested through UI Automation;
- Figma remains design input and no research server/license/sample copy reaches runtime without local evidence;
- protected Text/TextBox behavior remains unchanged and the relevant docs/checkpoint are updated together.

Until those gates close, this is a UI/UX specification, not a shipped Operations Overview.
