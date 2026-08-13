# CC-P1/P2/P5 upstream implementation-gate decision packet

**Status:** documentation-only owner gate; no host selection, WPF navigation change, read-model implementation, new Figma node or Text/TextBox change is authorized by this packet (2026-08-13)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Host options:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Read-model contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**P1 handoff:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md)
**P2 handoff:** [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md)
**P5 handoff:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md)
**P2 M1 owner packet:** [`CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md`](CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

The upstream sequence `CC-P1 -> CC-P2 -> CC-P5` is the implementation prerequisite for the downstream packets already mapped in the program. This packet turns the remaining cross-surface choices into one bounded readiness record: choose one host, adopt one local evidence projection, keep queue identity explicit, keep History/reprint actions in Print Center, and attach target-scale runtime evidence before navigation grows.

```text
one approved host/entry point
        -> one source-backed P1/P2/P5 read model
        -> Overview cards + queue table + History detail/deep-links
        -> existing Print Center action owner
        -> runtime/UI Automation evidence at target scales
```

This is not release approval. It does not create a Control Center, merge logs at runtime, add queue commands, authorize reprint dispatch or edit Figma. Existing Text/TextBox ownership, sizing, wrapping, clipping, padding, overflow and print parity remain protected.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. First host | Use existing host packet Option C: stage a P1-only entry point that proves cards, refresh and deep-links before persistent P2/P5 navigation. Option A (`PrintCenterWindow` extension) remains viable; Option B (new full shell) waits for read-model/runtime evidence. | Select A/B/C, name the owning window/class and define root/content/status/return ownership. |
| D2. Canonical read model | Adopt [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md): state store is lifecycle authority, operation JSONL supplemental, CSV per-label detail/export, live queue lookup separate. Preserve conflicts/unknowns and per-source diagnostics. | Approve projection schema, conflict copy, refresh snapshot boundary and one view-model/service owner. |
| D3. Queue identity/status | Use the explicitly configured/canonical named queue and `IPrinterQueueLookup`; show requested versus canonical names, availability, queue/job scope and refresh time. Missing/renamed/inaccessible/canonical-mismatch queues remain unavailable; no Windows-default fallback. | Approve queue fields/status taxonomy, stale threshold, refresh cancellation and target queue owner. |
| D4. P1 Overview | First cards: queue verification/health, pending recovery/non-terminal count, recent terminal software faults with a defined local time window, and separate local activation status. Every card displays source, last refresh and empty/stale/error state. | Define recent-fault window/timezone, count semantics, refresh command and activation copy. |
| D5. P2 Queue Console | M1 is read-only discovery/status: installed queues, saved canonical queue, queue/job observations, search/filter and unknown/error rows. Figma Pause/Resume/Delete/Reserve labels are deferred command concepts until capability/confirmation/timeout/durable outcome contracts exist. | Choose host/deep-link, queue/job fields, group/filter scope and whether printer groups are deferred. |
| D6. P5 History/reprint | History uses state/operation provenance with linked CSV detail/export. Print Center owns reconcile, acknowledge, void, request/approve reprint, guarded preview, dispatch and support export. No row/card dispatch shortcut or second action stack. | Approve History host/detail owner, reprint eligibility copy, local timezone/privacy and corrupt-tail handling. |
| D7. Navigation/accessibility | One root navigation/return path with stable `CC.*` IDs distinct from existing `Shell.*`; keyboard/focus restoration, one scroll owner and disabled/unimplemented states are explicit. Do not rename protected shell regions for Figma parity. | Finalize IDs/names, focus order, close/return behavior, target scales and screen-reader copy. |
| D8. Verification/closure | Close only with source fixtures, P1/P2/P5 cross-links, runtime screenshots/UI Automation at `1024 x 600`, `100%`, `125%`, `150%`, and build/unit/application evidence in the owning checkpoint. Figma nodes are visual references, not runtime proof. | Name product, host, read-model, UI Automation, QA and checkpoint owners; approve Figma reuse or smallest state-specific escalation. |

## Host decision matrix

| Option | First proof | Main risk | Readiness condition |
| --- | --- | --- | --- |
| A. Extend `PrintCenterWindow` | Recovery, queue and History share one local action owner | Recovery dialog becomes a broad operations shell before its information architecture is measured | Add a read-only summary without changing recovery action semantics; prove focus/scroll/close behavior. |
| B. New `OperationsOverviewWindow`/`ControlCenterWindow` | Stable local operations home and future module navigation | New window/navigation/AutomationId surface before read model is proven | Only after P1 source/read-model fixtures and target-scale evidence exist. |
| C. Staged P1 entry point (recommended) | Cards, refresh and explicit deep-links prove local evidence first | Temporary navigation may need a later migration | Implement only P1 read-only evidence; defer persistent P2/P5 shell decisions. |

The owner must record one option, host, root/content/status regions, return path and action owners in the host packet before any XAML navigation change.

## Evidence and ownership contract

| Surface | Source owner | Projection boundary |
| --- | --- | --- |
| P1 queue card | `MainViewModel.PrinterQueueStatus`, named queue refresh and `IPrinterQueueLookup` | Queue availability/name/message/timestamp; no physical or printer-native completion claim. |
| P1 recovery card | `PrintRecoveryReport`, `PrintJobRecoveryService`, `PrintJobStateStore` | Non-terminal/recovery diagnostics and recent terminal software events; no automatic retry or hidden action. |
| P1 activation card | `TrialLicenseService`/`ActivationWindow` | Local activation/trial/entitlement status only; no server seats or Figma license totals. |
| P2 queue table | `PrinterDiscoveryService`, named queue lookup and spool observations | Installed queue rows and queue/job state; unknown/stale/permission errors remain visible. |
| P5 History table/detail | `PrintJobStateStore`, `PrintOperationLogService`, `PrintLogService` | State lineage first, operation supplemental, CSV per-label detail/export; no flattened success count. |
| P5 action strip | Existing `PrintCenterWindow` and `MainViewModel` operator/dispatch services | Explicit Request -> Approve -> Prepare -> Dispatch; no card/list implicit dispatch. |
| P1/P2/P5 Figma references | Read-only nodes `2:2`, `2:37`, `3:85` | Density and information architecture only; sample server, seat, printer, user and activity values are not fixtures. |

## Read-only Figma evidence rechecked

| Node | Measured metadata | Safe use | Missing proof |
| --- | --- | --- | --- |
| `2:2` `CC / Overview` | `1280 x 800`; context `2:16` `1240 x 72`; workstation `2:20` `820 x 180`; license `2:25` `400 x 180`; errors `2:30` `1240 x 200` | Card hierarchy and compact operations grouping | Local source, timestamps, queue failure, activation-unavailable and WPF scale behavior. |
| `2:37` `CC / Printers — Print Management` | `1280 x 800`; filter rail `2:51` `220 x 680`; main pane `2:72` `1000 x 680`; sample rows `2:77`-`2:79` | Read-only table/filter proportions and deep-link language | Capability, queue/job distinction, stale/error, permission and command contracts. |
| `3:85` `CC / History` | `1280 x 800`; filters `3:99` `1248 x 56`; activity `3:101` `1248 x 600`; detail note `3:109` | Activity/filter/detail hierarchy and P5 destination | Three-source provenance, local time/privacy, corrupt-tail and exact-manifest gating. |

No Figma node was edited or duplicated. A new state-specific node is justified only after the owner selects a concrete missing WPF state and records the question, source owner and runtime gate.

## Cross-surface state and failure matrix

| State | P1/P2/P5 visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Initial/loading | Source list, busy state, previous refresh timestamp | Wait/cancel/refresh | Never show stale values as current without timestamp. |
| Healthy named queue | Canonical queue name, availability and observation time | Open setup/queue/Print Center | Queue observation is not physical verification. |
| Queue missing/stale/mismatch | Named requested queue, lookup error and timestamp | Open Printer Setup/refresh | Do not substitute Windows default or rename history. |
| No recovery/history sources | Explicit empty/source status | Open setup/continue or refresh | Empty files do not prove no physical output. |
| Pending recovery | Count, JobIds, state/reason severity and refresh | Open Print Center; choose action explicitly | No auto-retry/dispatch from card or row. |
| Mixed history sources | Per-row source, unit, timestamp basis and diagnostics | Filter/select detail | Do not flatten CSV rows, operation events and state jobs. |
| Corrupt state tail | Valid prefix plus corruption diagnostic | Repair/archive through explicit support path | Block append/reprint; do not show green success. |
| Reprint mismatch | Exact changed fields: count, queue, DPI, design/data/output hashes | Refresh/review/cancel | No force/ignore mismatch bypass. |
| Activation unavailable | Local status and repair link | Open Activation | No server/LMS entitlement claim. |
| Target deep-link unavailable | Non-destructive error and return path | Close/use existing owner action | Do not create hidden duplicate authority. |
| Figma sample data | Clearly marked research reference | None | Never copy sample values into runtime/fixtures. |

## Runtime and automation contract

The selected host must record these proposed IDs only after owner approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root/status | `CC.P1P2P5.Root` / `CC.P1P2P5.Status` | Local operations / Operations status |
| Refresh | `CC.P1P2P5.Refresh` | Refresh local operations evidence |
| P1 queue/recovery/activation cards | `CC.P1.Overview.Queue` / `CC.P1.Overview.Recovery` / `CC.P1.Overview.Activation` | Queue evidence / Recovery evidence / Activation status |
| P2 queue list/filters | `CC.P2.Queue.List` / `CC.P2.Queue.Filters` | Printer queue evidence / Queue filters |
| P5 History list/detail | `CC.P5.History.List` / `CC.P5.History.Detail` | Print history / Selected print evidence |
| Deep-links | `CC.P1.OpenPrintCenter`, `CC.P1.OpenPrinterSetup`, `CC.P1.OpenHistory` | Open Print Center / Open Printer Setup / Open Print History |
| Reprint action owner | Existing Print Center IDs; no new row-level dispatch ID | Existing explicit recovery/reprint names |

At minimum, evidence must cover keyboard order, focus restoration after deep-link/close, disabled/unimplemented actions, one intentional scroll owner and no clipping at `1024 x 600`, `100%`, `125%`, `150%`.

## Fixture and regression packet

These are proposed fixtures and gates, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| No queue / renamed queue / permission failure | Warning with requested/canonical identity and repair route | No default fallback; refresh timestamp visible. |
| One/multiple installed queues | Deterministic discovery and explicit selected queue | Queue rows remain distinct from job rows. |
| Healthy queue + no recovery | Honest cards with source/refresh and empty copy | No physical-success inference. |
| Pending recovery + terminal fault | Count/reason and deep-link to Print Center | No implicit retry/dispatch. |
| Mixed state/operation/CSV sources | Provenance-preserving rows and linked CSV detail | No CSV row-to-job fabrication. |
| Malformed/corrupt state tail | Valid prefix visible; action path blocked/diagnosed | No append/reprint while integrity is invalid. |
| Duplicate JobId/conflicting source fields | Conflict/unknown state with source values retained | No optimistic winner. |
| Reprint manifest mismatch | Named changed fields and blocked action | Existing exact-manifest guard remains owner. |
| Activation unavailable/storage error | Local status and Activation link | No LMS/seat claim. |
| Slow/failed refresh | Busy/stale/error state cannot overwrite newer evidence | Last refresh and diagnostics remain visible. |
| Deep-link close/focus | Return to source host with focus restored | UI Automation/screenshot at target scales. |
| Figma sample rows/cards | Reference only | No sample users, seats, printer addresses or dates become fixtures. |

## No-go list

- Do not implement A/B/C navigation before the owner records one selected host and action owner.
- Do not add a second queue, History, recovery, reprint, export or dispatch authority.
- Do not substitute a Windows-default queue, flatten source evidence or convert missing diagnostics to zero/success.
- Do not present Figma server name, LMS seats, printer counts, user names, workstation rows or sample activity as local data.
- Do not expose Pause/Resume/Delete/Reserve/Unreserve commands without capability, confirmation, timeout and durable-outcome contracts.
- Do not let Overview/Queue/History cards or rows dispatch, retry, approve, cancel or mutate templates implicitly.
- Do not change Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or designer/preview/print parity.

## Owner sign-off record

Record one owner, date and decision for every row. Blank rows keep the upstream implementation gate open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. First host (A/B/C) | `TBD` | `TBD` | `TBD` |
| D2. Canonical read-model owner | `TBD` | `TBD` | `TBD` |
| D3. Queue identity/status semantics | `TBD` | `TBD` | `TBD` |
| D4. P1 card/time-window/refresh policy | `TBD` | `TBD` | `TBD` |
| D5. P2 read-only console scope | `TBD` | `TBD` | `TBD` |
| D6. P5 History/reprint owner | `TBD` | `TBD` | `TBD` |
| D7. Navigation/AutomationIds/accessibility | `TBD` | `TBD` | `TBD` |
| D8. Runtime/Figma/verification owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** The upstream P1/P2/P5 sequence may move from review to implementation only after D1-D8 are filled, one host/action owner is named, source/queue/time/privacy semantics are accepted, fixtures cover empty/stale/conflict/corrupt/mismatch states and target-scale runtime evidence is attached. Until then, this remains a readiness decision record and not a shipped Control Center.
