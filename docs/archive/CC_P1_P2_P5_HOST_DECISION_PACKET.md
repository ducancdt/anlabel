# ANLAbel — CC-P1/P2/P5 host decision

**Status:** selected host implemented and verified (2026-08-13)
**Selected option:** staged P1-only local entry point (former Option C)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Execution gate:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Read model:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Research reference:** [Control Center Overview `2:2`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4)

## Decision

Implement a small local `OperationsOverviewWindow` reached from the existing desktop shell. The first slice contains only source-backed queue status, durable recovery/diagnostics and explicit links to the existing Print Center, Printer Setup and Print History owners.

This decision replaces the earlier A/B/C review loop. It does not select a permanent multi-module Control Center shell. P2 and P5 remain separate follow-up views until the P1 read model and runtime behavior are proven.

## Product boundary

- Local WPF desktop only; no browser/server Control Center.
- No software licensing, activation, entitlement or printer-seat feature.
- No fleet/workstation/user identity inferred from NiceLabel samples.
- No retry, reprint, queue mutation or dispatch from an overview card.
- No physical-output claim from queue, CSV, JSONL or lifecycle evidence.
- No Text/TextBox ownership, geometry, layout, resize, overflow or print-path change.

Existing Trial/Commercial release mechanics are not removed by this decision; they are simply outside this product program and are not exposed in the overview.

## Reused owners

| Concern | Existing owner | P1 behavior |
| --- | --- | --- |
| Saved queue | `MainViewModel.RefreshPrinterQueueStatusAsync` / `IPrinterQueueLookup` | Display requested/canonical identity, availability, error and refresh timestamp. Never use the Windows default as a fallback. |
| Recovery | `PrintJobRecoveryService` / `PrintJobStateStore` | Display candidate count and store diagnostics; open `PrintCenterWindow` for actions. |
| Printer repair | `PrinterSetupWindow` | Deep-link only; refresh queue evidence after return. |
| History | `PrintLogService` and existing shell History/Export handlers | Deep-link only; no second history or export implementation in P1. |
| Visual hierarchy | Figma `2:2`, read-only | Reuse context/card/recent-diagnostics density; omit server, license, user and workstation semantics. |

## Host contract

| Field | Selected value |
| --- | --- |
| Host | `OperationsOverviewWindow` (P1 content only) |
| Entry point | Explicit button from the existing `MainWindow` Print/operations area |
| Return path | Owned window close/Escape returns focus to the invoking shell button |
| Refresh | One request epoch; queue and recovery retain independent source/error/timestamp state |
| Scroll | One outer vertical scroll owner; diagnostics/table owns any row scrolling |
| Automation prefix | `CC.P1.Overview.*`; never rename existing `Shell.*` IDs |
| Figma | Reuse `2:2` metadata; no Figma write is required for the first slice |

## Runtime close gate

The selected host is verified only when:

1. healthy, unavailable and canonical-mismatch queue fixtures remain explicit;
2. empty recovery, pending recovery and corrupt-tail diagnostics are distinguishable;
3. late refresh results cannot overwrite a newer refresh;
4. Print Center, Printer Setup and History routes reuse current owners and restore focus;
5. the window is usable at `1024 x 600`, `100%`, `125%` and `150%` or records a concrete environment exception;
6. build, unit tests and the application regression runner pass;
7. protected Text/TextBox regressions remain green.

Runtime results are recorded in the execution gate and verification checkpoint; this file remains the host decision record.
