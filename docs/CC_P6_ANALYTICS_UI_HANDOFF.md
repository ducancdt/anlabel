# ANLAbel — CC-P6 local Analytics UI handoff

**Status:** roadmap/pre-implementation; read-only local analytics design review (2026-08-13)
**Owning roadmap:** [`MASTER_PLAN.md`](../MASTER_PLAN.md), section `6. Analytics`
**Related handoffs:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md), [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md), [`CC_P2_PRINT_QUEUE_UI_HANDOFF.md`](CC_P2_PRINT_QUEUE_UI_HANDOFF.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md) remains authoritative for Text/TextBox behavior.
**Program index / host gate:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md), sections 2 and 5

CC-P6 consumes the P5 read model and P1/P2 queue identity as read-only evidence. It may deep-link to History but must not create a second log, reprint, queue-success or physical-output authority; host and navigation remain the shared program decision.

This handoff defines a local, read-only analytics slice over existing evidence. It does not add cloud telemetry, cost optimization claims, a physical-label counter, a second log store, or a Figma edit. Analytics must preserve each source’s granularity and provenance: a per-label CSV row, a best-effort job trace and a hash-chained job-state event are not interchangeable facts.

## 1. Product boundary

The roadmap calls for dimensions such as Labels, Printer Groups, Users, Computers/Applications and Materials; charts for printed labels versus errors; date/printer/label filters; and a disclaimer separating software counters from verified physical labels. ANLAbel today has useful local evidence but no Analytics window:

- human-facing append-only CSV rows for label-print history;
- best-effort machine-readable print-operation JSONL;
- durable hash-chained job-state events, recovery diagnostics and manifest/output hashes;
- local queue/printer names and timestamps when those sources provide them;
- no centralized telemetry, user directory, printer-group registry, material catalog, or physical-verifier result for every label.

CC-P6 is therefore **read-only local aggregation with explicit “unknown/unavailable” values**. It must not infer user, material, application, printer group or physical completion from missing fields. It must not mutate jobs, reprint, delete logs, or alter label geometry.

## 2. Existing source evidence

| Source/evidence | Current behavior | Analytics implication |
| --- | --- | --- |
| [`PrintLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintLogService.cs) | Appends one human-facing row per printed label to local `print-history.csv`; parses rows and exports an on-demand Excel report. | Label counts are row-level source facts. Preserve the CSV path, header/version, parse warnings and local time basis; do not call a CSV row “physically verified.” |
| [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs) / [`PrintOperationLogEntry.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogEntry.cs) | Appends job-level JSONL with queue/spool, hashes, manifest and support fields; write failures are intentionally swallowed so print is not blocked. | Use as an optional job trace with availability status. Missing lines are an evidence gap, not zero activity; show source health and do not silently fill counts. |
| [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs) | Durable per-job events carry sequence/previous/integrity hashes and recover the latest valid state; corrupt tails produce diagnostics. | Use as the lifecycle/recovery authority for job counts and error states. Surface corrupt-tail diagnostics and keep invalid events out of aggregate totals unless the view labels the exclusion. |
| [`PrintJobRecoveryService.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobRecoveryService.cs) | Classifies non-terminal/uncertain jobs and keeps automatic retry disabled. | “Error”/“uncertain” counts must remain distinct from completed, queued and physically verified; Analytics never creates retry actions. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs) / [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs) | Current UI opens the CSV or exports it; preview writes history and job evidence through separate owners. | A future Analytics host must link back to P5 History for details, not duplicate export, reprint or recovery behavior. |
| [`PrintOperationLogEntry.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogEntry.cs) and manifest fields | Job entries can carry document/scene/output fingerprints, queue identity and operator action, but fields may be empty or redacted. | Dimension availability is per source and per row. Do not use empty actor/material/workstation fields as a shared “unknown user” identity without documenting the projection. |

## 3. Figma reference and routing

Use the existing [ANLAbel Control Center Figma file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) as research input. Read-only metadata for frame `5:2` was checked on 2026-08-13; no Figma node was edited or duplicated.

Frame `5:2` (`CC / Analytics`, `1280 × 800`) has two useful regions:

| Node | Measured reference | WPF/design boundary |
| --- | --- | --- |
| `5:16` | Chart region `(16,120)`, `820 × 520`; heading “Number of printed labels / errors”; sample bars and “Labels / Printers / Groups” note. | Treat chart density and comparison language as visual guidance. The bars are not ANLAbel measurements and must not be copied as values. |
| `5:31` | Filter pane `(860,120)`, `400 × 520`; Print dates, Computer/Application, Printer group, User, Label name, Label dimensions, Apply filters. | Only expose filters backed by local fields. Missing dimensions remain unavailable; label dimensions should be derived from validated template metadata, not arbitrary display text. |
| `5:3`–`5:15` | Shared `1280 × 88` topbar/nav with Analytics selected. | Reuse shell vocabulary only if a WPF host is approved; no web Control Center or server claim follows from this frame. |

The frame has no loading, empty, corrupt-source, filter-no-match, source-health, tooltip, table/detail, export, or physical-verification state. Those states need a state-specific WPF acceptance design. P5 History `3:101` remains the detail/provenance destination; Analytics should deep-link rather than grow a second event-detail stack.

## 4. Measurement and aggregation contract

Before implementation, approve a read model that keeps these units separate:

| Metric family | Unit | Candidate source | Safe wording |
| --- | --- | --- | --- |
| Label activity | CSV label rows, with explicit row/status semantics | `print-history.csv` | “Recorded label rows” or “software print-history rows.” |
| Job activity | Job IDs and lifecycle transitions | hash-chained state store; operation JSONL as trace | “Recorded jobs/events”; identify duplicate or missing source evidence. |
| Errors/uncertainty | Job/source events classified by reason/state | state store, recovery diagnostics, operation trace | “Recorded software errors/uncertain jobs.” |
| Queue/printer | Canonical queue name and queue observations | manifest/job entries/queue services | “Queue evidence observed locally”; no physical-ready claim. |
| Template | Relative path/ID and document hash | manifest/revision/library sources | Prefer hash/path identity; redact raw customer names where policy requires. |
| User/workstation/application | Only when the source explicitly supplies it | event/operator fields | Show value or `Unknown`; never infer identity from filename or Windows default. |
| Material/printer group | Not currently authoritative | future local preferences only | `Unavailable` until a named local source exists. |

Aggregation rules for review:

1. A CSV row is not automatically a unique job; a job event is not automatically one label. Charts must label their unit and source.
2. Deduplicate only with an explicit stable key (job ID, manifest fingerprint, or source row identity). If sources disagree, retain both provenance records and show a diagnostic instead of choosing a silent winner.
3. Date filters must state UTC/local basis and inclusive/exclusive boundaries. The UI should display the last refresh time and source file timestamps.
4. A corrupt JSONL tail, unreadable CSV, permission failure or missing best-effort trace reduces evidence quality; it must not be represented as zero.
5. “Printed labels” means recorded software activity unless a physical-verifier result is attached to that exact manifest/label. The dashboard must keep the non-claim visible near the chart/export.

## 5. Proposed read-only vertical slices

### M1 — Aggregate service and source health

1. Define source adapters for CSV, operation JSONL and state store without rewriting any source.
2. Return per-source availability, path, last-read time, parse warning/corrupt-tail diagnostic and row/event counts.
3. Produce an immutable read model with metric unit, source, timestamp basis, filter echo and non-claim text.

### M2 — Two charts and safe filters

1. Start with recorded label rows over time and software errors/uncertain jobs over time.
2. Add only filters with evidence-backed fields: date range, queue name, template path/hash; expose user/workstation/application only when present.
3. Show empty/no-match, stale source and partial-source states; do not render zeros for unavailable stores.
4. Deep-link a selected period/template/queue into the P5 History read model once that host exists.

### M3 — Read-only CSV summary export

1. Export the filtered aggregate with source paths/identities, units, date basis, generated time and disclaimer.
2. Apply the existing redaction/support-bundle policy; never export raw label payloads or credentials by default.
3. Keep export a user action; it must not mutate logs or create a new authority.

Cloud telemetry, cost optimization, scheduled reports, user directory joins and physical-label certification remain out of scope.

## 6. UI state and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Loading | Sources, filter echo and progress | Wait/cancel/refresh | Do not display stale totals as current without timestamp. |
| No sources | Missing paths/status and “No local evidence” | Configure/repair/close | Empty is not zero physical output. |
| Healthy mixed sources | Per-source health, units and last refresh | Filter/select chart | Never flatten row/job/event units. |
| Partial source | Available totals plus missing/corrupt source diagnostic | Repair/refresh; inspect History | Do not claim complete coverage. |
| Corrupt JSONL tail | Last valid sequence and diagnostic | Open recovery/refresh | Exclude invalid tail and show exclusion; no retry action. |
| CSV parse/permission failure | Path, error and last successful read | Repair/retry/export unavailable | Do not substitute an empty dataset. |
| Date/filter no match | Filter echo and zero recorded rows | Clear/adjust filters | Say “no matching recorded evidence,” not “no labels printed.” |
| Chart selection | Metric unit, source and filter context | Open P5 History detail | No reprint/void command from Analytics. |
| Unknown user/material/group | `Unknown`/`Unavailable` badge | Keep filter disabled or explicit unknown bucket | Never infer identity or group membership. |
| Export success/failure | Destination, row/metric count, disclaimer or error | Open file/retry | Export cannot alter source logs. |
| Physical verification absent | Persistent software-counter disclaimer | Inspect verifier-linked evidence if available | Never label a chart “physically printed/verified.” |

## 7. WPF mapping and acceptance gates

| Gate | Evidence before implementation closure |
| --- | --- |
| Host decision | Owner chooses a read-only `AnalyticsWindow`, a CC-P1 view, or a sibling of P5 History; one view model owns filters/refresh/export. |
| Scale/layout | Runtime screenshot/UI Automation at `1024 × 600`, `100%`, `125%`, `150%`; chart/filter panes have one intentional scroll owner and no clipped disclaimer. |
| Accessibility | Stable names/AutomationIds for source health, date range, queue/template filters, Apply/Clear, chart metric/unit, last refresh and Export summary. |
| Provenance | Every aggregate exposes source, unit, timestamp basis, filter echo and partial/corrupt diagnostics. |
| Data safety | Analytics is read-only; no reprint, queue mutation, log deletion or template edit path is reachable from the view. |
| Privacy/redaction | Raw paths, user names and source payloads follow the approved local/export redaction policy. |
| Regression | Unit/contract coverage for unit separation, deduplication, timezone boundaries, corrupt-tail handling, partial sources, unknown dimensions and export disclaimer; UI regression for loading/empty/error/filter states. |
| Figma | Record selected state-specific node and measured dimensions. `5:2` is visual input only; it does not prove chart correctness or runtime accessibility. |

## 8. Owner decisions needed

1. Which sources are authoritative for label rows, jobs and errors when CSV/operation/state stores disagree?
2. What local timezone and date-boundary wording should filters use?
3. Should template identity display raw relative path, document hash, name, or a redacted combination?
4. Which user/workstation/application fields are allowed in local UI and exported summaries?
5. Is an `Unknown` bucket visible, or should unsupported dimensions be disabled until a source exists?
6. Which host owns detail/deep-link behavior with P5 History, and what stable AutomationIds are approved?
7. Is CSV summary export required for M3, and what redaction/retention rules apply?
8. What exact physical-verifier evidence, if any, may remove the software-counter disclaimer for a selected metric?

## 9. Decision

**Needs product/design review.** Figma `5:2` supplies chart/filter density, while current local evidence can support a read-only aggregate only if CSV rows, job events and best-effort traces remain distinguishable. The next safe step is to approve source precedence, units, timezone, privacy and host decisions; no analytics window, cloud telemetry, physical-output claim or Figma edit is authorized by this handoff.
