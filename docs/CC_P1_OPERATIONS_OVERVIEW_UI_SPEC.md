# ANLAbel — CC-P1 Operations Overview UI/UX contract

**Status:** implemented and verified for the staged P1 host (2026-08-13)
**Host:** `OperationsOverviewWindow`
**Execution:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Decision record:** [`CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md`](CC_P1_OPERATIONS_OVERVIEW_UI_DECISION_PACKET.md)
**Figma reference:** [Control Center Overview `2:2`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4)

## Content contract

```text
[Local operations | Refresh | source status | refreshed-at]

[Queue Evidence]                 [Recovery Evidence]
[requested/canonical/status]     [candidate count/repair required]
[Open Printer Setup]             [Open Print Center]

[Durable recovery candidates / store diagnostics]

[Open Print History]                                      [Close]
```

There is no license/activation/entitlement/seat region. The Figma license card `2:25` is deliberately omitted.

## Fields

| Region | Required fields | Source |
| --- | --- | --- |
| Context | refresh state, refreshed-at, source warnings | P1 snapshot |
| Queue | requested name, canonical name, availability, diagnostic, observed-at | `PrinterQueueLookupResult` plus refresh envelope |
| Recovery | candidate count, repair-required, source diagnostic | `PrintJobRecoveryReport` |
| Candidate rows | JobId, latest state, reason, last-event UTC, queue name when present | recovery candidates/latest valid state |
| Actions | Refresh, Open Printer Setup, Open Print Center, Open Print History, Close | existing owners/delegates |

Do not expose raw label content, `RowData`, credentials or unredacted customer paths.

## State behavior

| State | UI behavior |
| --- | --- |
| Initial/loading | Disable duplicate Refresh; keep previous snapshot labeled with its timestamp if present. |
| Success | Show one refresh timestamp and per-source observed/error state. |
| Queue unavailable | Warning styling, requested identity and repair action; no default fallback. |
| Empty recovery | Explicit `No print jobs need reconciliation`; never translate to physical success. |
| Pending recovery | Show bounded rows and Open Print Center; no inline job action. |
| Store repair required | Show diagnostic warning even if a valid prefix is available. |
| Partial failure | Preserve successful source data and label failed source separately; never present mixed epochs as one healthy snapshot. |
| Deep-link failure | Non-destructive message; retain overview and focus. |

## Layout and accessibility

| Target | Requirement |
| --- | --- |
| `1280 x 800` | Two evidence cards above a bounded diagnostics list. |
| `1024 x 600` | Cards may stack; no page-level horizontal scroll; actions remain reachable. |
| `100%`, `125%`, `150%` | No clipped card titles, diagnostics or buttons; one intentional outer vertical scroll owner. |
| Keyboard | Refresh → Printer Setup → Print Center → History → evidence list → Close. Escape closes only when no child dialog is active. |

Automation vocabulary:

| Control | AutomationId |
| --- | --- |
| Root/status | `CC.P1.Overview.Root` / `CC.P1.Overview.Status` |
| Refresh | `CC.P1.Overview.Refresh` |
| Queue card | `CC.P1.Overview.Queue` |
| Recovery card | `CC.P1.Overview.Recovery` |
| Diagnostics | `CC.P1.Overview.Diagnostics` |
| Printer Setup | `CC.P1.Overview.OpenPrinterSetup` |
| Print Center | `CC.P1.Overview.OpenPrintCenter` |
| History | `CC.P1.Overview.OpenHistory` |
| Close | `CC.P1.Overview.Close` |

## Acceptance gate

- Source contract and refresh epochs have deterministic tests.
- Explicit routes reuse current owners and restore focus.
- Queue unavailable/mismatch, empty/pending/corrupt recovery and partial refresh are covered.
- Target-scale screenshot/UI Automation evidence is attached.
- Full build, unit and application regression commands pass.
- Protected Text/TextBox contract remains unchanged.

Figma `2:2` already answers the hierarchy question. Use Figma again only if implementation exposes a concrete state/layout ambiguity that this contract cannot answer.
