# CC-P1 Operations Overview decision record

**Status:** decisions implemented and verified (2026-08-13)
**Execution runbook:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Handoff:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md)
**Specification:** [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md)

## Accepted decisions

| ID | Decision |
| --- | --- |
| D1 | Use a staged P1-only `OperationsOverviewWindow`; do not create a permanent multi-module Control Center shell yet. |
| D2 | Queue evidence comes from explicit saved-queue lookup. Requested/canonical identity, error and observed-at remain visible; no Windows-default fallback. |
| D3 | Recovery evidence comes from `PrintJobRecoveryService`/`PrintJobStateStore`; corrupt-tail diagnostics remain visible and Print Center owns all actions. |
| D4 | Do not invent a “last 24 hours” error metric in P1. Show bounded recovery candidates/store diagnostics; add broader terminal-event aggregation only through P5/P6 read models. |
| D5 | Software licensing, activation, entitlement and printer-seat accounting are excluded. No card, route, filter, data field or milestone is created for them. |
| D6 | Refresh uses a request epoch and per-source state so late/partial results cannot overwrite newer evidence or masquerade as a complete snapshot. |
| D7 | Cards are read-only. Buttons route to existing Printer Setup, Print Center and Print History owners. |
| D8 | Use Figma Overview `2:2` only for hierarchy/density; omit server, navigation, users, workstations, license and sample values. No Figma write is required. |
| D9 | Use `CC.P1.Overview.*` AutomationIds and keep existing `Shell.*` identifiers unchanged. |
| D10 | Closure requires fixtures, target-scale runtime/UIA evidence and the full build/unit/application regression suite. |

## Non-negotiable boundaries

- no auto retry, implicit dispatch, queue mutation or reprint from P1;
- no physical-output inference;
- no second source/action owner;
- no raw payload/credential exposure;
- no protected Text/TextBox change;
- no product claim copied from competitor research.

## Closure evidence

| Evidence | Status |
| --- | --- |
| Baseline build/test on current dirty wave | PASS before source edits |
| Read-only snapshot and refresh epoch tests | PASS; stale epoch rejected and partial-source state retained |
| WPF host and explicit deep-link | PASS; Quick Access route and existing Print Center owner verified through UI Automation |
| Target-size/runtime evidence | PASS at 1040 x 700 current desktop; 900 x 560 WPF minimum/scroll boundary retained; no multi-monitor certification claimed |
| Post-change build/unit/application suite | PASS; build 0/0, unit 356/356, application runner exit 0 |

Figma remained read-only because runtime hierarchy and action ownership were already unambiguous.
