# CC-P6 local analytics owner decision packet

**Status:** M1 local Analytics host implemented; second log, cloud telemetry, physical-label counter, export authority and Text/TextBox change remain excluded (2026-08-13)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Handoff:** [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md)
**Specification:** [`CC_P6_ANALYTICS_UI_SPEC.md`](CC_P6_ANALYTICS_UI_SPEC.md)
**Related read model:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**Predecessor/action owner:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

CC-P6 is a read-only aggregation surface over three different local evidence granularities: per-label CSV rows, best-effort job operation JSONL and hash-chained print-job state. It must help an operator compare recorded software activity while preserving each source's identity, timestamp basis, completeness and non-claims.

```text
CSV label rows + operation trace + durable job state
        -> source health and explicit read model units
        -> filters with stated time/privacy rules
        -> charts/summary with provenance and disclaimer
        -> read-only deep-link to P5 History
```

The implemented M1 host follows this packet for source-separated counters, source health and the physical-output disclaimer. It does not add a second log, mutate jobs, retry/reprint, delete history, create cloud telemetry, infer users/materials/printer groups, or claim physical output. Text/TextBox ownership, sizing, wrapping, clipping, padding, overflow and print parity remain protected.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Metric units and source ownership | Keep `RecordedLabelRows`, `RecordedJobs`, `RecordedEvents`, `ErrorsOrUncertain`, `QueueEvidence`, `TemplateIdentity` and `PhysicalOutputVerified` as distinct projections. `PrintLogService` owns CSV row facts; `PrintJobStateStore` owns durable job lifecycle; `PrintOperationLogService` is supplemental best-effort trace. | Approve names, labels and which projection is the first chart/summary contract. |
| D2. Precedence and conflicts | Do not silently merge or choose a winner when CSV, operation JSONL and state events disagree. Deduplicate only by explicit stable identity (`JobId`, manifest fingerprint or source-row identity); preserve source/conflict diagnostics. | Approve conflict copy, duplicate policy and whether any state-store projection is canonical for job counts. |
| D3. Time and filter boundaries | Preserve source timestamps (`PrintLogEntry.PrintedAt` local; operation `TimestampLocal`; state event `TimestampUtc`). Use one declared viewer timezone and inclusive-start/exclusive-end date ranges; show source file timestamps and last refresh. | Choose viewer timezone, DST wording, date-boundary copy and allowed filter combinations. |
| D4. Dimension/identity policy | Expose queue/printer, template path/hash, print mode/outcome and lifecycle/error fields only when the source supplies them. Show `Unknown` or `Unavailable` for user, workstation/application, material and printer group; never infer identity from filenames, Windows defaults or Figma samples. | Approve raw-vs-redacted template identity, allowed actor fields and whether an explicit Unknown bucket is visible. |
| D5. Physical-output boundary | Ordinary CSV, JSONL, queue and state evidence means recorded software activity. Only accepted verifier evidence bound to the exact manifest/label may populate `PhysicalOutputVerified`; the disclaimer stays adjacent to charts/exports. | Approve verifier evidence language and whether any metric may display a separate verified subset. |
| D6. Host and P5 deep-link | Prefer one read-only Analytics view model hosted in the selected CC-P1/P5 host or a dedicated `AnalyticsWindow`; P5 remains the detail/provenance and reprint action owner. Analytics selection carries filter context but never adds reprint/queue actions. | Choose host/navigation route and stable AutomationIds for source health, filters, chart, History deep-link and export. |
| D7. Export, privacy and retention | M3 summary export is a user action over the filtered read model. Include source identities, units, timestamp basis, filter echo, generation time, diagnostics and disclaimer; apply approved path/user redaction and never export raw label payloads or credentials by default. | Decide whether export is required now, redaction policy, destination handling and local retention/cleanup ownership. |
| D8. Runtime/Figma/regression closure | Treat Figma Analytics `5:2` as density/vocabulary only. Require partial/corrupt/no-match/unknown states, one scroll owner, scale/accessibility evidence and source-unit/timezone/dedup/export fixtures before implementation. | Name product, host, read-model, privacy, UI Automation and QA owners; approve whether a state-specific Figma node is needed. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`PrintLogEntry.cs`](../src/ANLAbel.Data/PrintLogs/PrintLogEntry.cs) and [`PrintLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintLogService.cs) | One append-only CSV row is written per label-history record; fields include local `PrintedAt`, template/printer/mode, label counts and customer/source fields; on-demand Excel export is separate. | A CSV row is not automatically one job, successful physical output or complete coverage of every printer event. |
| [`PrintOperationLogEntry.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogEntry.cs) and [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs) | Job-level JSONL carries JobId, local timestamp, queue/spool, hashes, manifest, outcome, actor and support fingerprints; write failures are intentionally best-effort. | Missing operation lines are not zero activity and the trace is not a complete audit ledger. |
| [`PrintJobState.cs`](../src/ANLAbel.Core/Printing/PrintJobState.cs) | Lifecycle separates SpoolAccepted/QueueObserved/Unknown from Completed, Failed and physical-verification evidence; reprint actions are named job events. | Queue or Completed state does not prove physical media; job events are not per-label rows. |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs) | Per-job events are sequence/integrity chained; replay returns valid prefixes and diagnostics for malformed/corrupt tails. | A corrupt tail must not be hidden as zero or used to authorize retry; the store is not a general analytics database. |
| [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs) | Non-terminal/uncertain candidates and recovery diagnostics remain explicit; automatic retry is disabled. | Analytics cannot turn a recovery candidate into an action or successful count. |
| [`PrintJobOperatorActionService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobOperatorActionService.cs) | Reprint request/approval is a separate immutable-manifest action path. | Analytics must not expose or duplicate document/reprint commands. |
| [`PhysicalOutputVerification.cs`](../src/ANLAbel.Core/Printing/PhysicalOutputVerification.cs) and verifier contracts | Accepted physical evidence can bind to an exact manifest/label and carries a separate evidence contract. | Ordinary software counters cannot be relabeled as physically verified. |
| [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md) | P1/P2/P5 already define source separation, queue identity, corrupt-tail handling and History deep-link ownership. | P6 does not replace or flatten that read model; it extends it with explicit aggregate units. |
| Read-only Control Center Analytics [`asnGsLMxceJWb3HlfaE3q4`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `5:2` | Metadata gives `1280 x 800`, chart `5:16` (`820 x 520`), filter pane `5:31` (`400 x 520`), and topbar/nav `5:3`-`5:15`. | Sample bars, labels/printers/groups, user/application filters and Sign Out text do not prove local measurements or capabilities. |

## Proposed metric contract

Proposal only; D1-D5 must close before code or chart copy is authorized.

| Metric family | Unit/source | Display rule |
| --- | --- | --- |
| `RecordedLabelRows` | One parsed CSV row from `PrintLogService` | Say `recorded label rows`; preserve row identity, source path and local timestamp basis. |
| `RecordedJobs` | Stable `JobId` with latest valid lifecycle from `PrintJobStateStore` | Say `recorded jobs`; deduplicate only by JobId and show state-store diagnostics. |
| `RecordedEvents` | State transitions or operation entries | Label event unit and source; never present event count as label count. |
| `ErrorsOrUncertain` | Failed/Unknown/recovery candidates plus explicit operation errors | Keep failed, uncertain, queued, completed and verified states separate. |
| `QueueEvidence` | Queue/printer/spool observations and timestamps | Say `queue evidence observed locally`; never call it printer-ready or physical. |
| `TemplateIdentity` | Relative path/name plus document/manifest hash where present | Apply path/privacy policy; prefer stable hash/path over display name alone. |
| `User/Workstation/Application` | Explicit source fields only | Show value or `Unknown`; no inference from filename or OS default. |
| `Material/PrinterGroup` | No authoritative current source | Show `Unavailable`; disable filters until a named source exists. |
| `PhysicalOutputVerified` | Accepted verifier evidence bound to exact manifest/label | Ordinary rows/events are `Unknown/false`; keep software disclaimer. |
| `SourceHealth` | Exists/read/parse/replay status, last-read time and diagnostics | Unavailable/partial is visible and never rendered as zero. |

Aggregation rules:

1. A CSV row is not a job, and a job event is not a label. Every chart, legend and export names its unit and source.
2. Deduplicate only with an explicit key. If sources disagree, keep both provenance records and emit a conflict/partial diagnostic.
3. A date filter states timezone and inclusive/exclusive boundaries; source file timestamps and last refresh remain visible.
4. Malformed CSV, unreadable files, missing best-effort JSONL or a corrupt state tail reduce evidence quality; they do not produce a healthy zero.
5. Analytics is read-only: no append, repair, retry, reprint, queue mutation, template edit or log deletion.

## Policy and state matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Loading | Source list, filter echo, progress and previous refresh | Wait/cancel/refresh | Do not present stale totals as current without timestamp. |
| No sources | Missing path/status and `No local evidence` | Configure/repair/close | Empty is not zero physical output. |
| Healthy mixed sources | Per-source health, units and last refresh | Filter/select chart | Never flatten row/job/event units. |
| Partial source | Available totals plus missing/corrupt diagnostic | Repair/refresh or inspect History | Do not claim complete coverage. |
| Corrupt state tail | Last valid sequence and diagnostic | Open recovery/refresh | Exclude invalid tail visibly; no retry action. |
| CSV parse/permission failure | Path, error and last successful read | Repair/retry/export unavailable | Do not substitute an empty dataset. |
| Filter no match | Filter echo and zero recorded matches | Clear/adjust filters | Say `no matching recorded evidence`, not `no labels printed`. |
| Chart selection | Metric unit/source/filter context | Open filtered P5 History detail | No reprint, void or queue command from Analytics. |
| Unknown dimension | `Unknown`/`Unavailable` badge | Disable filter or keep explicit unknown bucket | Never infer identity/group/material. |
| Export success/failure | Destination, metric count, disclaimer or error | Open/retry | Export cannot alter source logs. |
| No physical verification | Persistent software-counter disclaimer | Inspect bound verifier evidence if available | Never label a chart physically printed/verified. |
| Figma research sample | Clearly marked design reference | None | Sample bars/users/groups/dates never become fixtures. |

## Host-neutral layout and ownership

```text
[Analytics context: Refresh | source health | last refresh | timezone basis]
[Recorded label rows / software errors chart]
[Legend: metric unit | source | filter echo | physical-output disclaimer]
[Filters: date | queue | template/hash | outcome/lifecycle | Apply | Clear]
[Selected context -> Open P5 History detail]
[Export filtered summary: sources | units | generated time | redaction/disclaimer]
```

Only one Analytics view model owns filters, refresh and export. P5 History remains the detail/provenance and reprint owner. At `1024 x 600`, stack chart and filters or use one deliberate filter drawer; keep unit, source, last-refresh and disclaimer reachable without page-level horizontal scroll.

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P6.Analytics.Root` | Local analytics |
| Source health | `CC.P6.Analytics.SourceHealth` | Analytics source health |
| Refresh | `CC.P6.Analytics.Refresh` | Refresh analytics |
| Date filters | `CC.P6.Analytics.DateFilters` | Analytics date range |
| Evidence filters | `CC.P6.Analytics.EvidenceFilters` | Evidence filters |
| Apply/Clear | `CC.P6.Analytics.ApplyFilters` / `CC.P6.Analytics.ClearFilters` | Apply filters / Clear filters |
| Chart/legend | `CC.P6.Analytics.Chart` / `CC.P6.Analytics.MetricLegend` | Recorded activity chart / Metric unit and source |
| Disclaimer | `CC.P6.Analytics.PhysicalDisclaimer` | Software-counter disclaimer |
| History deep-link | `CC.P6.Analytics.OpenHistory` | Open filtered History |
| Summary export | `CC.P6.Analytics.ExportSummary` | Export analytics summary |

## Fixture and regression packet

These are proposed fixtures and gates, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| Missing/empty CSV | Explicit source state and zero recorded rows | No physical-output conclusion; no silent fallback. |
| Valid CSV with multiple label rows | `RecordedLabelRows` count preserves row granularity | Source path, header/version and local time basis visible. |
| Missing/best-effort operation JSONL | Partial source diagnostic | Do not treat missing lines as zero jobs or errors. |
| Valid state prefix plus corrupt tail | Valid latest states plus corruption diagnostic | Invalid tail excluded; no retry/repair mutation from Analytics. |
| Duplicate JobId across sources | One explicit job projection plus provenance/conflict | No silent double count or silent winner. |
| CSV row and state event disagree | Both source facts retained | Chart/summary names unit and conflict status. |
| DST/date-boundary timestamps | Inclusive-start/exclusive-end result in chosen timezone | UTC/local basis and boundary wording are testable. |
| Unknown user/material/group | `Unknown`/`Unavailable` state | No filename or sample-data inference. |
| Filter no match | No-match copy and filter echo | Not phrased as no labels printed. |
| Redacted summary export | Sources, units, filter echo, generated time and disclaimer | No raw label payloads/credentials; source remains unchanged. |
| Accepted verifier evidence | Separate verified subset only when exact binding passes | Ordinary software counts keep disclaimer. |
| Figma sample bars/filters | Density/reference only | Sample values/users/groups never enter runtime fixtures. |

## No-go list

- Do not call a CSV row, completed job, spool observation or queue state physical output.
- Do not flatten CSV rows, operation entries and state events into one unqualified `printed labels` count.
- Do not treat missing, unreadable, best-effort or corrupt sources as a healthy zero.
- Do not infer users, workstations, applications, materials, printer groups or permissions from names, paths, Windows defaults or Figma samples.
- Do not add retry, reprint, void, queue mutation, log deletion or template-edit controls to Analytics.
- Do not create a second History detail, export authority, log store or state/recovery machine; deep-link to P5 owners.
- Do not export raw label payloads, credentials or unredacted local paths without an explicit privacy decision.
- Do not remove the software-counter disclaimer without exact manifest-bound verifier evidence.
- Do not change Text/TextBox ownership, sizing, wrapping, clipping, padding, overflow or print parity.

## Owner sign-off record

Record one owner, date and decision for every row. Blank rows keep CC-P6 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. Metric units/source ownership | `TBD` | `TBD` | `TBD` |
| D2. Precedence/conflict/deduplication | `TBD` | `TBD` | `TBD` |
| D3. Timezone/date boundaries | `TBD` | `TBD` | `TBD` |
| D4. Dimensions/identity/privacy | `TBD` | `TBD` | `TBD` |
| D5. Physical-output disclaimer/verifier subset | `TBD` | `TBD` | `TBD` |
| D6. Host/P5 deep-link ownership | `TBD` | `TBD` | `TBD` |
| D7. Export/redaction/retention | `TBD` | `TBD` | `TBD` |
| D8. Runtime/Figma/regression owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** CC-P6 may move from design review to implementation only after D1-D8 are filled, one read-model/filter/export owner is named, unit/time/privacy/conflict behavior is explicit, and source-health, corrupt-tail, no-match, deep-link and redacted-export fixtures are converted into runtime and regression gates. Until then, CC-P6 remains a local analytics plan and not a shipped dashboard or physical-output report.
