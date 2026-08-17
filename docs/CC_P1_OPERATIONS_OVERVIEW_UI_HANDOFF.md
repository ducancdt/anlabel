# CC-P1 Operations Overview implementation handoff

**Status:** implemented and runtime-verified (2026-08-13)
**Execution gate:** [`CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md`](CC_P1_P2_P5_IMPLEMENTATION_GATE_PACKET.md)
**Host decision:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**UI contract:** [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md)
**Source contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Research:** [`NICELABEL_CONTROL_CENTER_USER_GUIDE.md`](NICELABEL_CONTROL_CENTER_USER_GUIDE.md)

## Operator task

From the desktop shell, answer two questions quickly:

1. Is the explicitly configured industrial queue available, and when was it checked?
2. Are there durable recovery jobs or state-store diagnostics that require review?

Then route explicitly to Print Center, Printer Setup or Print History. The overview never dispatches, retries, reprints, mutates a queue or claims physical output.

Software licensing, activation, entitlement and printer-seat accounting are excluded from this product slice and from the wider Control Center implementation program.

## Existing owners to reuse

| Evidence/action | Current owner | P1 rule |
| --- | --- | --- |
| Saved queue | `MainViewModel.PrinterQueueStatus` / `RefreshPrinterQueueStatusAsync` | Preserve requested/canonical identity and errors; no default fallback. |
| Recovery | `PrintJobRecoveryService`, `PrintJobStateStore`, `MainViewModel.PrintRecoveryReport` | Show candidates/diagnostics read-only; actions stay in Print Center. |
| Print Center | `PrintCenterWindow` | Deep-link only. |
| Printer selection/repair | `PrinterSetupWindow` | Deep-link and refresh after return. |
| History/export | Existing MainWindow handlers and `PrintLogService` | Deep-link only; no P1 history browser. |

## Research-to-product mapping

Figma Control Center Overview `2:2` (`1280 x 800`) is sufficient for the first layout decision:

| Research region | ANLAbel mapping |
| --- | --- |
| Context/header `2:16` | Local queue identity, source health and refresh timestamp |
| Operational summary `2:20` | Queue Evidence and Recovery Evidence cards |
| License card `2:25` | Omitted; licensing is out of scope |
| Recent errors `2:30` | Bounded recovery candidates/store diagnostics; no invented 24-hour metric |
| Server/nav/footer | Omitted from the staged P1 window |

No Figma write or `get_design_context` call is needed. Runtime WPF evidence, not a new design frame, closes this slice.

## States

| State | Visible evidence | Safe action |
| --- | --- | --- |
| Loading | Busy state and previous successful refresh time | Wait; duplicate refresh disabled |
| Queue available | Requested/canonical name, availability, observed-at | Open Printer Setup or Print Center |
| Queue missing/mismatch/error | Requested name, diagnostic and observed-at | Open Printer Setup; never substitute default |
| No recovery | Explicit zero candidates plus source status | Continue or refresh |
| Pending recovery | Count, JobId/state/reason/timestamp | Open Print Center |
| Corrupt state tail | Valid-prefix notice and store diagnostics | Open Print Center/support path; no unsafe action |
| Partial refresh failure | Per-source error and last-known timestamp | Retry failed source; do not relabel stale values current |

## Acceptance

- One P1-only `OperationsOverviewWindow` and one shell entry point.
- `CC.P1.Overview.*` AutomationIds; existing `Shell.*` IDs unchanged.
- One outer vertical scroll owner; keyboard path Refresh → actions → evidence list → Close.
- Layout usable at `1024 x 600`, `100%`, `125%`, `150%` or a documented exception.
- Build, unit tests and application regression runner pass.
- Protected Text/TextBox contract remains untouched.

P2 Queue and P5 History remain follow-up slices. Do not expand P1 to implement their tables or commands.

## Verification result

- Runtime/UIA smoke opened the overview from `CC.P1.OpenOverview`, observed all P1 cards/actions and opened the existing Print Center owner.
- Queue unavailable stayed explicit and did not fall back to a Windows default.
- Build completed with 0 warnings and 0 errors; unit tests passed 356/356; the application runner exited 0.
- The runtime smoke used the 1040 x 700 default window on the current desktop. WPF retains a 900 x 560 minimum plus bounded scrolling; this is not a multi-monitor/display-scale certification.
