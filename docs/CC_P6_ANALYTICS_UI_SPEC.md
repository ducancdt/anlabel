# ANLAbel — CC-P6 Local Analytics UI/UX spec

**Status:** design-only read-only analytics spec; source precedence, host and metric policy remain open (2026-08-13)
**Predecessors:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md), [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Handoff:** [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, Analytics `5:2`

This spec maps the Figma Analytics shell to local read-only aggregates over CSV label rows, job-level operation traces and hash-chained job state. It does not create a second log, reprint/queue authority, cloud telemetry stream or physical-label counter.

## 1. Operator outcome

The first Analytics surface should let an operator:

1. see recorded software activity with its unit, source, timestamp basis and last-refresh status;
2. compare label-row activity with job/error/uncertainty evidence without flattening them into one count;
3. filter by fields that local sources actually provide and see unsupported dimensions as unavailable;
4. inspect partial, corrupt, stale, empty and no-match states without converting them to zero success;
5. deep-link a selected period/template/queue to P5 History and export a redacted summary without mutating any source.

“Printed labels” in this surface means recorded software activity unless an accepted physical-verifier result is attached to the exact manifest/label. The disclaimer stays adjacent to every chart/export.

## 2. Figma node map (read-only)

Metadata for `5:2` was rechecked read-only on 2026-08-13. The chart bars and filter labels are visual language only; they are not product measurements or capabilities.

| Figma node | Metadata name / bounds | ANLAbel role | Boundary |
| --- | --- | --- | --- |
| `5:2` | `CC / Analytics`, `1280 x 800` | Analytics density reference | Not a web/server telemetry or physical-output contract. |
| `5:3`–`5:15` | Topbar/nav, `1280 x 88`; Analytics selected | Optional host chrome | Show only local modules; no sign-out or server identity. |
| `5:16` | Chart region `(16,120)`, `820 x 520` | Chart/metric owner | Sample bars and “Labels / Printers / Groups” note are not ANLAbel values. |
| `5:17` | `Number of printed labels / errors` heading | Candidate metric language | Must be qualified with unit, source and physical-verification disclaimer. |
| `5:18`–`5:29` | Sample bars | Density/reference only | Never copy heights or values into fixtures or release evidence. |
| `5:31` | Filter pane `(860,120)`, `400 x 520` | Filter owner | Expose only date/queue/template fields with local source and privacy rules. |
| `5:33`–`5:38` | Print dates, Computer/Application, Printer group, User, Label name, Label dimensions | Candidate filters | User/workstation/group/material filters remain disabled/unavailable without named sources. |
| `5:39` | Apply filters control | Filter action reference | Filtering is read-only and must expose the filter echo/time basis. |

The frame has no loading, empty, partial-source, corrupt-tail, no-match, tooltip, detail, export or physical-verification states. No new Figma node is required; request a state-specific reference only when a concrete WPF question cannot be answered from source contracts.

## 3. Metric and source contract

| Metric family | Unit / current source | Display rule |
| --- | --- | --- |
| `RecordedLabelRows` | One `PrintLogEntry` per CSV label row from `PrintLogService` | Say “recorded label rows”; preserve CSV path, parse status, local `PrintedAt` basis and row-level granularity. |
| `RecordedJobs` | Stable `JobId`/latest lifecycle from `PrintJobStateStore`; operation JSONL is supplemental | Say “recorded jobs”; deduplicate only by explicit `JobId`, never count CSV rows as jobs. |
| `RecordedEvents` | State-store transitions or operation entries | Label the event unit and source; do not present event count as label count. |
| `ErrorsOrUncertain` | State `Failed`/`Unknown`, recovery candidates, operation `Success=false`/error evidence | Keep failed, uncertain, queued and terminal verified states separate; expose classification source. |
| `QueueEvidence` | Printer/queue names, spool observations, timestamps from state/operation/queue lookup | Say “queue evidence observed locally”; never call it printer-ready or physically printed. |
| `TemplateIdentity` | Operation/template path/name, manifest/document hash, P3 revision identity | Prefer hash/relative identity; apply path/privacy policy. |
| `User/Workstation/Application` | Only explicit source fields such as an approved actor field | Show value or `Unknown`; never infer from filename, Windows default or Figma sample copy. |
| `Material/PrinterGroup` | No current authoritative local source | Show `Unavailable` and disable filters until a named source exists. |
| `PhysicalOutputVerified` | Accepted verifier evidence bound to the exact manifest/label | `Unknown/false` for ordinary CSV, JSONL and queue data; never remove the disclaimer without verifier evidence. |
| `SourceHealth` | File existence/read/parse status, last-read time, state diagnostics | Show per-source availability and diagnostics; unavailable is not zero. |

Aggregation rules:

1. A CSV row is not automatically a unique job; a job event is not automatically one label.
2. Deduplicate only by explicit stable identity (`JobId`, manifest fingerprint or source-row identity). Preserve conflicts and source names.
3. Date filters state UTC/local basis and boundary inclusivity; show source file timestamps and last refresh.
4. Corrupt JSONL tail, unreadable CSV, permission failure and missing best-effort trace reduce evidence quality; never render them as zero.
5. Analytics never mutates logs, creates retries, dispatches printers or changes templates.

## 4. Host-neutral wireframe

```text
[Analytics context: Refresh | source health | last refresh | timezone basis]

[Recorded label rows / software errors chart]
[Metric legend: unit | source | filter echo | physical-output disclaimer]

[Filters: date range | queue | template/hash | print mode/outcome when available | Apply | Clear]

[Selected period/template/queue -> Open P5 History detail]
[Export filtered summary: sources | units | generated time | redaction/disclaimer]
```

Chart selection is read-only. The detail link carries filter context to P5 History but does not create a second activity/detail or reprint stack.

## 5. State and failure matrix

| State | Visible evidence | Safe next action | Fail-closed rule |
| --- | --- | --- | --- |
| `Loading` | Source list, filter echo and progress | Wait/cancel/refresh | Do not show stale totals as current without timestamp. |
| `NoSources` | Missing paths/status and `No local evidence` | Configure/repair/close | Empty is not zero physical output. |
| `HealthyMixedSources` | Per-source health, units and last refresh | Filter/select chart | Never flatten row/job/event units. |
| `PartialSource` | Available totals plus missing/corrupt diagnostic | Repair/refresh or inspect History | Do not claim complete coverage. |
| `CorruptStateTail` | Last valid sequence and diagnostic | Open recovery/refresh | Exclude invalid tail visibly; no retry action. |
| `CsvReadFailure` | Path, parse/permission error and last successful read | Repair/retry/export unavailable | Do not substitute an empty dataset. |
| `FilterNoMatch` | Filter echo and zero recorded matches | Clear/adjust filters | Say no matching recorded evidence, not no labels printed. |
| `ChartSelection` | Metric unit, source and filter context | Open P5 History detail | No reprint/void/queue command from Analytics. |
| `UnknownDimension` | `Unknown`/`Unavailable` badge | Disable filter or keep explicit unknown bucket | Never infer identity/group/material. |
| `ExportSuccessOrFailure` | Destination, metric count, disclaimer or error | Open file/retry | Export cannot alter source logs. |
| `NoPhysicalVerification` | Persistent software-counter disclaimer | Inspect verifier-linked detail if present | Never label a chart physically printed/verified. |

## 6. Filters, responsive behavior and automation

M1 filters are date range, queue name, template path/hash, print mode/outcome when present, lifecycle/error classification and source availability. `User`, `Workstation`, `Computer/Application`, `Printer group`, `Material` and unqualified dimensions are unavailable until a local source/privacy decision exists.

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May preserve the `820 x 520` chart and `400 x 520` filter proportions as visual reference. | Chart and filter have declared owners; disclaimer stays visible. |
| `1024 x 600` | Stack chart and filter or collapse filters into a drawer; preserve metric/unit/disclaimer and avoid page-level horizontal scroll. | Keyboard order: source status → filters → Apply/Clear → chart → detail/export. |
| `100%`, `125%`, `150%` | Reflow or clip only inside declared owners; do not blindly scale Figma bars. | Capture screenshot/UI Automation at every scale and record environment exceptions. |

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P6.Analytics.Root` | `Local analytics` |
| Source health | `CC.P6.Analytics.SourceHealth` | `Analytics source health` |
| Refresh | `CC.P6.Analytics.Refresh` | `Refresh analytics` |
| Date filters | `CC.P6.Analytics.DateFilters` | `Analytics date range` |
| Queue/template filters | `CC.P6.Analytics.EvidenceFilters` | `Evidence filters` |
| Apply/Clear | `CC.P6.Analytics.ApplyFilters` / `CC.P6.Analytics.ClearFilters` | `Apply filters` / `Clear filters` |
| Chart | `CC.P6.Analytics.Chart` | `Recorded activity chart` |
| Metric legend | `CC.P6.Analytics.MetricLegend` | `Metric unit and source` |
| Physical disclaimer | `CC.P6.Analytics.PhysicalDisclaimer` | `Software-counter disclaimer` |
| History deep-link | `CC.P6.Analytics.OpenHistory` | `Open filtered History` |
| Summary export | `CC.P6.Analytics.ExportSummary` | `Export analytics summary` |

## 7. Acceptance gate

Before implementation review closes P6:

- owner approves source precedence, label/job/event units, date/timezone boundaries, privacy/redaction and the P5 deep-link host;
- fixtures cover absent/empty/partial/malformed CSV, missing operation JSONL, valid state prefix plus corrupt tail, duplicate JobId, stale timestamps, unknown dimensions and no-match filters;
- every aggregate exposes source, unit, filter echo, last refresh and physical-output disclaimer;
- charts never copy Figma bar values or sample labels/groups/users into runtime fixtures;
- export is read-only, redacted and includes source identities, units, generated time, date basis and disclaimer;
- Analytics exposes no reprint, queue mutation, log deletion or template edit path;
- runtime screenshot/UI Automation covers `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus and scroll ownership;
- protected Text/TextBox behavior and print/recovery contracts remain untouched.

Until these gates close, this file is a UI/UX specification, not a shipped Analytics window or physical-output report.
