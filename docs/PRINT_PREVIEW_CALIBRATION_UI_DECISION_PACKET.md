# Print Preview and Calibration UI owner decision packet

**Status:** documentation-only owner gate; no preview redesign, calibration automation, Figma write, or Text/TextBox change is authorized by this packet (2026-08-13)
**Reliability plan:** [`print-preview-reliability-plan.md`](print-preview-reliability-plan.md)
**Physical calibration guide:** [`print-calibration.md`](print-calibration.md)
**Figma handoff template:** [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md)
**Barcode/output boundaries:** [`P7_PRINT_METHOD_DECISION_PACKET.md`](P7_PRINT_METHOD_DECISION_PACKET.md), [`P8_PHYSICAL_VERIFIER_DECISION_PACKET.md`](P8_PHYSICAL_VERIFIER_DECISION_PACKET.md)
**Protected contract:** [`../AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

The repository has a working WPF `PrintPreviewWindow` and a separate `PrinterSetupWindow`, but no dedicated UI/UX owner record for their state transitions, evidence copy or calibration boundary. This packet makes that existing workflow reviewable without inventing a calibration screen from a Figma shell.

The packet covers:

- preview-window host and action ownership;
- design-only versus effective printer-plan trust;
- asynchronous refresh, cancellation and stale-result behavior;
- Excel-row selection/filter/copies and preflight issue navigation;
- explicit print, spool and physical-output non-claims;
- label setup, calibration, reset and hardware-evidence boundaries;
- target-scale, keyboard/focus, scroll and regression closure.

It does not add a new Print Center/Preview host, change the renderer or print pipeline, infer a native printer method, certify a physical label, or alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or designer/preview/print parity. Blank owner rows keep the slice open.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Preview host and action ownership | Keep `PrintPreviewWindow` as the first preview/selection/calibration host and `PrinterSetupWindow` as the explicit queue/media/DPI/orientation editor. Keep `PrintService` as the effective-plan, preflight and dispatch owner; keep Print Center/reprint actions separate. | Confirm the host pair, close/return path and one owner for each action before any UI restructuring. |
| D2. Plan trust vocabulary | Show the current `PrintRenderPlan` summary and distinguish design-only, effective-ticket-unverified and imageable-area-verified states. A Figma preview or a green visual card is never physical-output proof. | Approve exact status labels, severity and the minimum evidence required before the Print action is enabled or prompts. |
| D3. Preview refresh/cancellation | Keep refresh asynchronous, cancelable and operation-identified. A canceled or stale operation must not replace the current rows, plan or bitmap; driver resolution remains off the dispatcher. | Name the owner for busy/cancel copy, cancellation timing and any target-scale wait threshold. |
| D4. Data freshness and print choice | Capture the Excel write-time at preview open. If it changes, show the existing explicit choice to cancel and update or print the rows currently shown; never reload silently. Quick Print remains governed by its separate stale-data block. | Approve wording, whether the explicit “print shown data” choice remains allowed, and the evidence required for that decision. |
| D5. Row selection/filter/copies | Keep row selection, column filter, copies and session duplicate warning in the preview owner. Filtering changes selection only; it never edits Excel or dispatches. Copies expand from the selected source row and preserve source-row identity. | Confirm selection persistence across refresh/page changes and the accessible names for row-level actions. |
| D6. Preflight and issue navigation | Show source-backed issue count/detail, keep template-level or all-row failures fail-closed, and allow partial printing only after an explicit skip confirmation. Clicking an issue navigates to its label without mutating the template. | Approve the skip/cancel copy, maximum visible issue count, row-number basis and support-export link. |
| D7. Printer setup/calibration | Keep setup as an explicit profile editor. Calibration prints one calibration label through the selected effective queue; Reset restores offsets/scales to `0/0/1/1`; physical measurement and scanner evidence remain outside the app. | Name the hardware/pilot owner, calibration evidence record and whether any measured correction may be persisted. |
| D8. Print outcome/non-claims | Persist job/operation evidence and show `Canceled`, `Failed`, `SpoolAccepted` and any device-confirmed outcome distinctly. `SpoolAccepted` never means the label was physically correct or barcode-verified. | Approve outcome copy, retry/duplicate-warning ownership and the boundary to P8 physical verification. |
| D9. Figma and accessibility route | Reuse shell Print & Output `2:39` plus Setup `2:41`, Preview `2:44` and Print `2:47` only for grouping/icon language. Use existing WPF controls and proposed IDs below; no new Figma frame is needed for the current state questions. | Name the design/UIA owner if a missing state later requires a smallest state-specific reference. |
| D10. Closure and regression | Close only with source fixtures, runtime screenshots/UIA at supported scales, hardware evidence where claimed, and a clean implementation checkpoint. This packet itself adds no code/test result. | Fill sign-off rows and link the closing commit, screenshots, UIA measurements and physical-calibration record. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`PrintPreviewWindow.xaml`](../src/ANLAbel.App/PrintPreviewWindow.xaml#L1-L18) | The current preview host starts at `1200 x 760`, caps to the work area and maximizes on smaller work areas; the left preview and right `300 DIP` settings columns are existing layout. | The default size is not a target-scale acceptance measurement. |
| [`PrintPreviewWindow.xaml`](../src/ANLAbel.App/PrintPreviewWindow.xaml#L20-L183) | Preview page navigation, zoom surface, virtualized Excel row list, filter and copies controls already have separate interaction areas and intentional scroll behavior. | It does not prove keyboard traversal, focus restoration or visual fit at `1024 x 600`. |
| [`PrintPreviewWindow.xaml`](../src/ANLAbel.App/PrintPreviewWindow.xaml#L186-L281) | Printer summary, Print Plan summary, preflight status/progress/cancel and issue navigation are rendered in the settings column. | A visible summary does not prove that the selected queue/ticket is effective or current. |
| [`PrintPreviewWindow.xaml`](../src/ANLAbel.App/PrintPreviewWindow.xaml#L283-L363) | Label Setup and Calibration are explicit collapsed sections; controls expose dimensions, DPI, scale, offsets, Print calibration and Reset. | The calibration controls do not replace physical measurement or verifier evidence. |
| [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L65-L127) | The constructor captures template/data/queue context, restores preferences, subscribes close cancellation and starts a refresh. | Startup success is not a runtime click-through or driver proof. |
| [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L129-L217) | Preview title, label/DPI summary, plan summary, row counts, preflight text, busy/progress and print eligibility are centralized on the window owner. | These properties do not establish a product-approved vocabulary or accessible IDs. |
| [`PrintPreviewWindow.Async.cs`](../src/ANLAbel.App/PrintPreviewWindow.Async.cs#L60-L174) | Refresh builds rows, resolves the effective plan, validates rows, rejects stale operations and renders the current page asynchronously; cancel/failure keeps explicit status. | It does not prove every driver or scale behaves the same on hardware. |
| [`PrintPreviewWindow.Async.cs`](../src/ANLAbel.App/PrintPreviewWindow.Async.cs#L178-L238) | Missing queue or failed driver validation becomes a design-only/plan issue instead of silently treating the design plan as production-ready. | A design-only preview is not a print approval. |
| [`PrintPreviewWindow.Async.cs`](../src/ANLAbel.App/PrintPreviewWindow.Async.cs#L264-L310) | A new preview operation cancels the prior operation and exposes busy/progress/cancel state. | It does not authorize duplicate background operations or a second renderer. |
| [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L261-L642) | Print checks stale Excel data, selected rows, effective queue plan, duplicate session rows, preflight results, state transitions, operation logs and user-facing outcomes. | `SpoolAccepted` is not physical completion; code alone does not prove a user understood the prompt. |
| [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L750-L764) | Excel write-time changes are detected relative to the snapshot captured at preview open. | The check is best-effort and does not silently refresh rows. |
| [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L809-L840) | Apply setup refreshes preview, Print calibration uses the selected print service, and Reset restores offset/scale defaults. | It does not record physical measurement or scanner results. |
| [`PrintPreviewWindow.xaml.cs`](../src/ANLAbel.App/PrintPreviewWindow.xaml.cs#L881-L1048) | Row checks, copies, select-all and filters update selection and refresh pages without editing source data. | It does not prove UIA names or focus order for virtualized rows. |
| [`PrinterSetupWindow.xaml`](../src/ANLAbel.App/PrinterSetupWindow.xaml#L1-L94) and [`PrinterSetupWindow.xaml.cs`](../src/ANLAbel.App/PrinterSetupWindow.xaml.cs#L15-L171) | Setup owns standard/custom media, width/height, orientation, DPI and explicit printer selection before Apply. | It does not expose driver-native capability claims or physical verification. |
| [`print-calibration.md`](print-calibration.md#L1-L86) | Calibration is a physical measurement workflow with explicit queue/media/DPI/offset/scale and synthetic-data guidance. | It is not evidence that a printer, scanner or verifier has been run. |
| [`AGENTS.md`](../AGENTS.md) | Text/TextBox geometry and behavior are protected. | It does not choose preview copy or calibration ownership. |

## Surface and action ownership

| Surface/action | Current owner | Boundary |
| --- | --- | --- |
| Preview page/image/zoom | `PrintPreviewWindow` + `PreviewRasterizer` | Read-only render of the current rows and effective/design plan; must not write object geometry. |
| Excel row/filter/copies | `PrintPreviewWindow` + `TrackingRowViewModel` | Selection and copies are preview-session state; source workbook remains unchanged. |
| Effective queue/ticket/DPI/media plan | `PrintService` / `PrintRenderPlan` | One plan is reused for validation, preview metadata and dispatch; no UI-side plan math. |
| Preflight | `PrintService` / `PrintPreflightValidator` | Template/data/output issues remain explicit; no raw fallback after a blocking issue. |
| Printer/media setup | `PrinterSetupWindow` / `PrinterPreferencesService` | Explicit queue/profile editor; no hidden default-queue substitution. |
| Calibration dispatch | `PrintService.PrintCalibrationWithResultAsync` | One calibration label through the selected queue; physical measurement is external evidence. |
| Job lifecycle/operation trace | `PrintJobStateStore`, `PrintOperationLogService`, `PrintLogService` | Durable evidence and human-facing history remain distinct; no second dispatch path. |
| Reconcile/reprint/support actions | `PrintCenterWindow` and existing approved action owner | Preview may deep-link or return; it must not duplicate Print Center mutations. |
| Physical verification | P8 verifier/coordinator boundary | Queue acceptance, golden/visual checks and calibration do not become physical completion. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Opening / preparing | Busy indicator, progress text and disabled duplicate actions | Wait or Cancel | A prior operation cannot overwrite a newer operation. |
| Design-only plan | `PrintPlanSummaryText` says design-only or ticket is unverified; queue/setup route is visible | Open Printer Setup, select a queue and refresh | Do not call the preview production-ready or dispatch from an unvalidated plan. |
| Effective plan ready | Queue name, effective DPI X/Y, offset/rotation/margin and imageable-area evidence | Review preview and preflight | A plan is a contract snapshot, not physical success. |
| Preflight passed | `Preflight passed` plus selected-row count and current plan | Print or change selection/setup | Revalidate the effective plan and selected rows before dispatch. |
| Preflight issues | Count/detail, first issue list and `Go to this label` navigation | Repair, deselect bad rows after explicit prompt or cancel | Template-level/all-row failures block; no silent raw fallback. |
| No rows selected | `0` selected and explanatory Print message | Select rows or clear filter | No empty job or implicit “print all” action. |
| Excel changed since preview | Warning prompt identifies the snapshot risk | Cancel/update first or explicitly print the rows shown | Never reload silently; the operator choice must be recorded in the UI path. |
| Row already printed this session | Source row numbers and duplicate-label warning | Cancel or explicitly continue | No automatic retry or hidden duplicate dispatch. |
| Refresh canceled / failed | `Preview update canceled` or failure detail; current valid view remains identifiable | Retry, repair queue/data or close | A canceled/stale result cannot replace current rows/bitmap/plan. |
| Print preparation blocked | Selected queue contract error and warning dialog | Repair Printer Setup or close | No durable preparation event or spool submission from a design-only plan. |
| Print canceled | Explicit canceled status and lifecycle transition when a job exists | Review/close | Cancellation is not failure or completion. |
| Spool accepted | Queue/job evidence, operation log and support fingerprint when available | Inspect Print Center/History; physical check remains external | Never label as physically printed, scanned or certified. |
| Print failed | Error detail, lifecycle failure and operation trace | Repair/retry only through explicit owner action | No green history or automatic retry. |
| Calibration not completed | Warning/error status from the print service | Repair queue/media/driver or stop | No measured correction is inferred from a failed attempt. |
| Calibration accepted | Queue accepted the one calibration job and guide is available | Measure physically and record evidence | Acceptance is not label alignment, barcode scan or certification. |

## Calibration boundary

Calibration is deliberately split into two contracts:

```text
WPF controls: DPI / offsets / scale / Print calibration / Reset
        -> PrintService + selected queue
        -> one calibration label accepted by the spooler
        -> external measurement, scan and evidence record
```

The UI may persist the authored `PrinterProfile` corrections through the existing setup path, but it must not infer a correction from Figma dimensions, a spool acceptance, a single software raster or a barcode icon. The physical record must identify printer/driver/media, effective DPI X/Y, measured dimensions/offset/scale, scanner/verifier result and any queue-correlation limitation. Keep synthetic data in screenshots and support artifacts.

## Read-only Figma metadata boundary

Metadata was rechecked read-only on 2026-08-13 in the existing shell recreation file [`zdN71qfzrYV6pPt1b2FRRc`](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation). It is design input only:

| Node | Metadata | Safe reuse | Missing state |
| --- | --- | --- | --- |
| `2:39` | `Group/Print & Output`, `147 x 58`, children Setup/Preview/Print | Ribbon grouping and action vocabulary | No preview window, plan trust, preflight, calibration or spool state |
| `2:41` | Setup child, `45 x 43`; icon + `Setup` label | Deep-link/group language for Printer Setup | No media/DPI/driver state |
| `2:44` | Preview child, `55 x 43`; icon + `Preview` label | Entry-point language only | No preview canvas/page/filter state |
| `2:47` | Print child, `39 x 43`; icon + `Print` label | Explicit print action placement | No preflight/confirmation/outcome state |
| `2:19` | Printer status frame, `123 x 21`, `Printer not selected` | Compact missing-printer vocabulary | No WPF queue/ticket evidence |
| `2:21` | Paper status frame, `125 x 21`, `Paper: 100 x 50 mm` | Compact media summary language | No calibration or physical measurement |

**Routing decision:** reuse the existing WPF Preview/Calibration surfaces and the shell's action grouping. Do not call `get_design_context`, create a new Figma frame or copy Figma server/sample values for this documentation-only slice. If a future owner requests a visual redesign, first name the missing state and the smallest state-specific reference, then close it with WPF runtime evidence.

## Accessibility and responsive gate

Current XAML has no stable `AutomationProperties.AutomationId` on the preview controls, so the following are proposals only:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Window/root | `PrintPreview.Root` | Print Preview |
| Preview canvas/page | `PrintPreview.Canvas` / `PrintPreview.Page` | Label preview / Current label |
| Page navigation | `PrintPreview.PreviousPage`, `PrintPreview.PageInput`, `PrintPreview.NextPage` | Previous label / Label number / Next label |
| Excel rows | `PrintPreview.TrackingList`, `PrintPreview.RowFilter`, `PrintPreview.RowFilterColumn`, `PrintPreview.SelectAll` | Excel rows / Find rows / Filter column / Select visible rows |
| Preflight | `PrintPreview.PreflightStatus`, `PrintPreview.PreflightIssues`, `PrintPreview.PreflightCancel` | Preflight status / Preflight issues / Cancel preview update |
| Plan/setup | `PrintPreview.PrintPlan`, `PrintPreview.LabelSetup`, `PrintPreview.Calibration`, `PrintPreview.OpenPrinterSetup` | Effective print plan / Label setup / Calibration / Label size and printer setup |
| Primary actions | `PrintPreview.Close`, `PrintPreview.Print`, `PrintPreview.PrintCalibration`, `PrintPreview.ResetCalibration` | Close / Print selected labels / Print calibration / Reset offset and scale |
| Printer Setup | `PrinterSetup.Root`, `PrinterSetup.PaperList`, `PrinterSetup.Dpi`, `PrinterSetup.Printer`, `PrinterSetup.Apply` | Printer setup / Standard label sizes / DPI / Printer / Apply |

Runtime evidence must cover `1024 x 600`, `100%`, `125%` and `150%` (or a recorded environment exception), keyboard order from plan/setup through rows/preflight to Print/Close, visible focus after issue navigation and dialog return, one intentional scroll owner per region, and no horizontal clipping of required action copy. The preview canvas may scroll/zoom; it must not create a second hidden data-scroll owner.

## Fixture and regression packet

These are proposed fixture names and assertions, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| `Preview_NoPrinter_isDesignOnly` | Missing/invalid queue yields design-only plan issue and no production-ready claim. | View-model/source assertion and runtime screenshot. |
| `Preview_EffectivePlan_matchesDispatch` | Preview metadata, preflight and dispatch share the same effective queue/ticket/DPI/media/output contract. | Deterministic plan hash and regression output. |
| `Preview_Refresh_cancel_keepsCurrentSnapshot` | Canceling or superseding an operation cannot overwrite current rows, plan or bitmap. | Async operation fixture and UIA status capture. |
| `Preview_PreflightIssue_goToLabel` | Clicking an issue selects the originating label without mutating template geometry/content. | Row-number mapping and focus/screenshot evidence. |
| `Preview_Filter_selection_and_copies` | Filter selects matches, clearing restores default selection, and copies preserve source-row identity. | View-model fixture plus virtualized-list click-through. |
| `Preview_StaleExcel_requiresExplicitChoice` | File change prompts cancel/update versus print-shown-data; no silent reload. | Controlled write-time fixture and message/action evidence. |
| `Preview_AlreadyPrinted_warnsWithinSession` | Reprinting a previously accepted source row requires explicit confirmation. | Session fixture and duplicate-warning screenshot. |
| `Preview_PrintOutcome_keepsSpoolSeparateFromPhysical` | Canceled/failed/spool-accepted/device-confirmed outcomes remain distinct in status and logs. | State-store/operation-log assertion and UIA capture. |
| `Calibration_requiresExplicitQueue` | Calibration cannot silently choose the Windows default queue. | Queue-missing fixture and setup repair path. |
| `Calibration_Reset_restoresProfile` | Reset writes `OffsetX/Y=0` and `ScaleX/Y=1`, then refreshes preview. | Profile before/after assertion and screenshot. |
| `Preview_1024x600_scale_focus_scroll` | Required actions, focus, page navigation, row filter and settings remain reachable without unintended horizontal clipping. | Runtime screenshots/UIA at target scales. |
| `Protected_TextTextBox_contract_unchanged` | Preview/calibration work does not change Text/TextBox ownership, frame geometry, wrap/clip, padding, resize or print parity. | Required protected regression suite after any code change. |

## No-go list

- Do not create a new Figma preview/calibration screen merely because the shell contains Setup/Preview/Print icons.
- Do not treat `SpoolAccepted`, a green software preflight, a calibration raster or a Figma sample as physical-label or barcode-verifier evidence.
- Do not silently reload Excel data after preview opens, silently choose a default queue, or let a stale async result replace a current preview.
- Do not let row filters, issue buttons, cards or preview controls dispatch a second print/reprint path; Print Center and PrintService remain the action owners.
- Do not make calibration alter label objects, Text/TextBox frame geometry or authored content; corrections belong to the printer profile and require physical measurement.
- Do not add Native/ZPL/EPL/TSPL controls from this packet; P7 ADR/capability/pilot gates remain authoritative.
- Do not mark the packet closed without target-scale runtime evidence and, for physical claims, a real printer/scanner evidence record.

## Owner sign-off record

Record one owner, date and approved option for every row. Blank rows keep the packet open.

| Decision | Owner | Date | Approved option / notes | Evidence link |
| --- | --- | --- | --- | --- |
| D1. Preview/setup host and action owners | `TBD` | `TBD` | `TBD` |  |
| D2. Plan trust vocabulary and Print enable/prompt policy | `TBD` | `TBD` | `TBD` |  |
| D3. Refresh/cancel/stale-result behavior | `TBD` | `TBD` | `TBD` |  |
| D4. Excel freshness choice | `TBD` | `TBD` | `TBD` |  |
| D5. Row/filter/copies/duplicate semantics | `TBD` | `TBD` | `TBD` |  |
| D6. Preflight/partial-print wording | `TBD` | `TBD` | `TBD` |  |
| D7. Calibration/hardware evidence boundary | `TBD` | `TBD` | `TBD` |  |
| D8. Outcome/non-claim vocabulary | `TBD` | `TBD` | `TBD` |  |
| D9. Figma route and proposed AutomationIds | `TBD` | `TBD` | `TBD` |  |
| D10. Runtime/QA/closure owner | `TBD` | `TBD` | `TBD` |  |

**Closure rule:** the Print Preview/Calibration slice may move from documentation review to implementation or release evidence only after D1-D10 are filled, one WPF action owner exists per mutation, the effective-plan/preflight/stale-data fixtures pass, target-scale UIA/screenshots are attached, and any physical claim is backed by the calibration/scanner record. Until then this is an open UI/UX contract and makes no release, printer-certification or physical-output claim.
