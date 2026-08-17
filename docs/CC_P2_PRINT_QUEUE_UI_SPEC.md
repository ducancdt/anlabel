# ANLAbel — CC-P2 Print Queue Console UI/UX spec

**Status:** staged read-only M1 implemented; runtime UI smoke remains pending (2026-08-13)
**Host decision:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Evidence contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Handoff:** [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md)
**Concrete owner decision packet:** [`CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md`](CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, Printers `2:37`

This spec defines the first **read-only** queue console. The staged `PrintQueueConsoleWindow` maps the Figma Print Management shell to local Windows discovery and explicit saved-queue lookup. It does not add queue commands, printer groups, license seats or a second recovery/dispatch owner.

The concrete source/action boundary, current empty-list error gap, owner sign-off rows and M1 fixtures are recorded in [`CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md`](CC_P2_PRINT_QUEUE_UI_DECISION_PACKET.md); this file remains the host-neutral UI behavior contract.

## 1. Operator outcome

The console should let an operator:

1. discover installed `Local` and `Connections` queues without silently selecting the default;
2. distinguish discovered queue identity from a saved queue lookup and from a job-scoped spool observation;
3. filter/search only fields that local evidence actually supplies;
4. select a queue/job and deep-link to Printer Setup, Print Center or History;
5. see unknown, inaccessible and stale evidence instead of optimistic “ready” copy.

Pause, Resume, Delete, Reserve and Unreserve remain deferred command concepts. A Figma command strip is not a capability contract.

## 2. Figma node map (read-only)

Metadata for `2:37` was rechecked read-only on 2026-08-13. The node names below are actual metadata names; “role” is the ANLAbel mapping.

| Figma node | Metadata name / bounds | ANLAbel role | Boundary |
| --- | --- | --- | --- |
| `2:37` | `CC / Printers — Print Management`, `1280 x 800` | Visual density/reference frame | Not a WPF size mandate or capability claim. |
| `2:38` | `TopBar`, `1280 x 48` | Optional host chrome reference | No server/sign-out behavior without local owner. |
| `2:41` | `Frame`, `1280 x 40` | Primary navigation area | Show only local modules; no web/LMS navigation claim. |
| `2:51` | `Frame`, `(16,104)`, `220 x 680` | Filter rail | Research labels such as Licensed Printers, Facility A and paused are not current data. |
| `2:72` | `Frame`, `(252,104)`, `1000 x 680` | Main queue table/detail region | Read-only table first; command strip stays disabled/deferred. |
| `2:73` | Command text sample | Future command affordance | Requires capability, confirmation, timeout and durable outcome contract. |
| `2:74` | Search text sample | Search affordance | Search only local fields; current `PrinterInfo` has no port/workstation field. |
| `2:75`/`2:76` | Table header sample | Column-language reference | Do not invent queue counts/status fields absent from source. |
| `2:77`–`2:79` | Example rows | Empty/example density only | Printer names, IPs, queue counts and error text are research samples. |
| `2:80` | Footer control sample | Local count/filter summary reference | “Unlicensed” is omitted by scope; workstation grouping requires separate local evidence. |

No new Figma node is required for this read-only spec. Missing state questions follow the [Figma escalation protocol](figma-ui-handoff-template.md#figma-escalation-protocol).

## 3. Source-to-row contract

| Row field | Current source | Display rule |
| --- | --- | --- |
| `QueueName` | `PrinterInfo.Name`, `PrinterQueueLookupResult.RequestedName`/`CanonicalName` | Show requested and canonical values when both exist; mismatch is an error state. |
| `DriverName` | `PrinterInfo.DriverName` | Empty/unknown is explicit; do not infer a driver from queue name. |
| `IsDefault` | `PrinterInfo.IsDefault` | A default marker is informational; it never becomes an implicit selected queue. |
| `SavedRelation` | Current template `PrinterProfile.PrinterName` plus lookup result | Show `Not selected`, `Saved and available`, `Saved but unavailable` or `Different discovered queue`. |
| `QueueAvailability` | `PrinterQueueLookupResult.IsAvailable` and `ErrorMessage` | Queue-level availability only; preserve diagnostic text and refresh timestamp. |
| `DiscoveryStatus` | `PrinterDiscoveryService.GetInstalledPrinters()` | Current catch returns an empty list; a future console seam must distinguish `Empty` from `EnumerationFailed`. |
| `SpoolJobId`/`JobStatus` | `WindowsSpoolJobStatusReader` and `SpoolJobMonitor` | Job-scoped observation; show `Unknown`/timeout explicitly and never promote it to queue health. |
| `DocumentsInQueue` | Not supplied by current `PrinterInfo`/lookup contract | Hide or show `Unknown` until a named read-only source exists; never copy Figma sample counts. |
| `Port`/`Workstation` | Not supplied by current `PrinterInfo` contract | Do not show as populated columns; search/filter remains unavailable or explicitly unsupported. |
| `ObservedAt` | Future projection around discovery/lookup/spool refresh | Include timestamp basis and age; current lookup result itself has no timestamp. |
| `RecoveryLink` | `PrintRecoveryReport` / `PrintCenterWindow` | Deep-link only; no row-level retry/reprint shortcut. |

## 4. Host-neutral wireframe

The eventual host may be the P1 overview, a `PrintManagementWindow` sibling or an extension of Print Center. Keep this content order:

```text
[Queue console context: Refresh | saved queue | last discovery/lookup]

[Filters / search rail]  [Read-only queue table]
[All | Available | ...]  [Name | Driver | Default | Saved relation | Status | Age]

[Selected queue/job detail + explicit links]
[Printer Setup | Print Center | History]
```

The detail region is optional in M1 but must not become a second recovery ledger. Selection alone never mutates a profile, queue or print manifest.

## 5. Status and filter vocabulary

### Queue-level states

| State | Source/evidence | Safe action | Non-claim |
| --- | --- | --- | --- |
| `NotSelected` | Empty saved printer name | Open Printer Setup | No print is dispatchable from this state. |
| `Discovered` | `PrinterInfo` row | Select/inspect | Discovery does not prove online or licensed. |
| `SavedAvailable` | Explicit lookup returns canonical match | Inspect/setup/history | Availability is not physical output. |
| `SavedUnavailable` | Missing/canonical mismatch/lookup error | Repair in Printer Setup | Never use another/default queue silently. |
| `EnumerationFailed` | Future explicit discovery diagnostic | Retry or support path | Empty list is not proof that no queues exist. |
| `Unknown` | Incomplete/ambiguous source | Inspect diagnostics | Unknown is not Ready. |

### Job-scoped states

Use the existing mapped taxonomy: `Printing`, `Spooling`, `Pending`, `Paused`, `Offline`, `PaperOut`, `UserIntervention`, `Blocked`, `Retained`, `Deleted`, `NotFound`, `Error`, `Unknown`. Every row displays scope (`Job observation`) and observed-at basis. None of these states proves a physical mark was printed.

Filters in M1 should be limited to `All`, queue name, driver, saved relation, job/status when a job observation exists, and `Unknown/Error`. Figma groups, licensing filters, facility/location and workstation filters remain deferred until local contracts exist.

## 6. Responsive behavior

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May preserve the visual 220-DIP rail + 1000-DIP main proportion as a reference. | Rail and table have intentional owners; selected detail stays reachable. |
| `1024 x 600` | Collapse the rail into a filter drawer/stack or narrow filter region; table receives the main width; no page-level horizontal scroll. | Keyboard order: refresh → filter/search → table → detail → deep-links; selection survives refresh when identity still exists. |
| `100%`, `125%`, `150%` | Reflow or clip only inside declared owners; do not blindly scale the Figma frame. | Capture screenshot/UI Automation for every scale and record environment exceptions. |

## 7. Proposed automation vocabulary

Proposals only; final IDs require the host decision and UI Automation evidence.

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P2.QueueConsole.Root` | `Print queue console` |
| Refresh | `CC.P2.QueueConsole.Refresh` | `Refresh printer queues` |
| Search | `CC.P2.QueueConsole.Search` | `Search printer queues` |
| Filter rail/drawer | `CC.P2.QueueConsole.Filters` | `Queue filters` |
| Queue table | `CC.P2.QueueConsole.QueueTable` | `Discovered printer queues` |
| Selected detail | `CC.P2.QueueConsole.Detail` | `Selected queue evidence` |
| Printer Setup link | `CC.P2.QueueConsole.OpenPrinterSetup` | `Open Printer Setup` |
| Print Center link | `CC.P2.QueueConsole.OpenPrintCenter` | `Open Print Center` |
| History link | `CC.P2.QueueConsole.OpenHistory` | `Open Print History` |
| Deferred command strip | `CC.P2.QueueConsole.Commands` | `Queue commands unavailable` |

The deferred command strip should be absent or clearly disabled until a separate command contract exists; it must not look actionable merely because the Figma shell contains text labels.

## 8. Acceptance gate

Before implementation review closes P2:

- the host packet names the host and reuses P1/P5 owners;
- discovery distinguishes empty from enumeration failure and preserves `Local`/`Connections` scope;
- queue-level and job-level fields are visually distinct;
- saved queue canonical mismatch, missing queue, stale refresh, no queues, one queue and multiple queues have fixtures;
- search/filter cannot imply unavailable fields such as port/workstation; licensing/seat filters do not exist in this product scope;
- Print Center, Printer Setup and History remain explicit deep-links;
- any future command has capability/rejection/timeout/confirmation/durable-outcome tests before its button is enabled;
- runtime screenshot/UI Automation covers `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus and scroll ownership;
- Figma sample rows/copy never become runtime fixtures without local evidence;
- protected Text/TextBox behavior and print contracts remain untouched.

Until these gates close, this file is a UI/UX specification, not a shipped Print Queue Console.
