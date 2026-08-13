# CC-P7 local administration owner decision packet

**Status:** documentation-only owner gate; no Admin window, role/user system, server license table, sync/SMTP service, destructive retention action, new Figma node or Text/TextBox change is authorized by this packet (2026-08-13)
**Program index:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Handoff:** [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md)
**Specification:** [`CC_P7_ADMINISTRATION_UI_SPEC.md`](CC_P7_ADMINISTRATION_UI_SPEC.md)
**Related owners:** [`CC_P5_HISTORY_REPRINT_UI_HANDOFF.md`](CC_P5_HISTORY_REPRINT_UI_HANDOFF.md), [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md)

## Purpose and decision boundary

CC-P7 is a thin local desktop administration surface. It should make machine-bound activation, workspace/printer preferences, data-source registry health and evidence-owner links understandable without pretending that ANLAbel has server authentication, roles, groups, workflow administration, synchronization, SMTP, license-seat management or a retention scheduler.

```text
local activation + preferences + data-source registry + evidence paths
        -> source-backed status cards and owner links
        -> explicit confirmation for existing cleanup owners
        -> retention preview only after backup/recovery policy closes
```

The packet is a design-review gate. Existing Activation, Printer Setup, Database Manager/Cleanup, P5 History and P6 Analytics owners remain authoritative for their mutations and read models. Text/TextBox ownership, sizing, wrapping, clipping, padding, overflow and print parity remain protected.

## Decision summary

| Decision | Evidence-backed recommendation | Owner choice required |
| --- | --- | --- |
| D1. Host and action ownership | Prefer a thin local `AdminWindow`/settings hub that links to existing Activation, Printer Setup, Database Manager/Cleanup, P5 History and P6 Analytics owners. Do not duplicate their services or commands. | Choose host/navigation route, whether unsupported categories are hidden or visibly marked, and stable AutomationIds. |
| D2. Activation scope/status | Display only local validation states from `TrialLicenseService`/`ActivationLicense`: valid, trial, missing, expired, wrong-machine, tampered/clock error and storage error. Show machine scope/help/expiry as permitted; never show server seats or Figma `Used/Total`. | Approve customer/expiry/machine-code display and the repair/support route; define copy for limited/trial behavior. |
| D3. Preferences boundary | Show/reset designer snap/grid and printer last-used values through their existing services. Preferences are workspace defaults, not authored template geometry or Text/TextBox policy; Printer Setup remains queue/readiness owner. | Choose which values are visible, reset semantics, path/privacy copy and whether reload is explicit. |
| D4. Data-source registry/cleanup | Show registry path/schema/count/load-save diagnostics. Route Manage/Cleanup to `DatabaseManagerWindow`/`DataSourceCleanupWindow`; preserve atomic save, legacy migration and future-schema rejection. | Approve relink/cleanup entry points, current-template protection copy and whether registry repair is read-only in M1. |
| D5. Evidence links and retention | Link to P5 History/P6 Analytics/Print Center and show source health only; do not duplicate filters, reprint, recovery or export. A future retention flow must preview CSV/operation/state/revision/workflow sources separately with age/size, destination, backup and recovery test before any archive/delete. | Decide whether retention preview is in M1/M2, source retention owners, backup location, recovery verification and irreversible-action policy. |
| D6. Privacy and security | Redact activation keys, raw label payloads, credentials and sensitive absolute paths by default. Local UI may show a relative/path-safe identity plus source location only under an approved privacy rule; no identity/role claim from a free-text actor. | Approve local path/customer/license visibility, support-bundle fields, credential handling and machine-code masking. |
| D7. Unsupported server categories | Keep authentication, roles, users/groups, workflow admin, variables, alerts, application server, sync and license seats explicitly unavailable/research-only. Do not copy Figma sample roles/members/permissions into local state. | Choose hidden versus disabled-with-reason presentation and the exact `Not available in local desktop` wording. |
| D8. Runtime/Figma/regression closure | Treat Figma Administration `5:41` as category/density reference only. Require activation-error, preferences-fallback, registry-future-schema, cleanup-confirmation, evidence-partial and retention-preview states at target scales before implementation. | Name product, host, activation/preferences/registry/retention/privacy, UI Automation and QA owners; approve whether a state-specific Figma node is needed. |

## Source evidence and implications

| Evidence | What it proves | What it does not prove |
| --- | --- | --- |
| [`TrialLicenseService.cs`](../src/ANLAbel.App/Services/TrialLicenseService.cs) and [`ActivationWindow.xaml`](../src/ANLAbel.App/ActivationWindow.xaml) | Local trial state uses protected files/current-user registry where possible; activation keys are machine-bound and status includes valid, expired, wrong-machine, tampered/clock and storage-error paths. | It is not a server identity, seat counter, role provider or key-generation surface. |
| [`ActivationLicense.cs`](../src/ANLAbel.Core/Licensing/ActivationLicense.cs) | Signed activation payload carries product, machine, customer, expiry and license identity and validates machine/expiry. | Admin must not expose raw keys or bypass signature/machine validation. |
| [`DesignerPreferences.cs`](../src/ANLAbel.Data/Preferences/DesignerPreferences.cs) and [`DesignerPreferencesService.cs`](../src/ANLAbel.Data/Preferences/DesignerPreferencesService.cs) | Snap/grid preferences are local and malformed/missing settings fall back to defaults. | Preferences do not own label geometry, authored data or protected Text/TextBox behavior. |
| [`PrinterPreferencesService.cs`](../src/ANLAbel.App/Services/PrinterPreferencesService.cs) | Last-used printer/paper/DPI/orientation are stored locally with non-critical fallback. | Preferences do not prove queue health, printer readiness, native capability or physical output. |
| [`DataSourceRegistry.cs`](../src/ANLAbel.Data/DataSourceRegistry.cs) | Machine-wide versioned registry (`schemaVersion=1`) supports legacy array migration, atomic replacement, upsert/remove and future-schema rejection. | Registry status does not authorize template-binding rewrites or imply server database replacement. |
| [`DataSourceCleanupWindow.xaml`](../src/ANLAbel.App/DataSourceCleanupWindow.xaml) and code-behind | Existing `560 x 440` dialog lists missing/unused sources and requires selected removal action. | Cleanup is not print-log deletion, file erasure, retention or physical data destruction. |
| [`PrintLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintLogService.cs), [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs) and [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs) | Local CSV, best-effort operation JSONL and hash-chained job evidence have different durability/diagnostic rules. | There is no retention scheduler; Admin must not delete or flatten sources by exposing a button. P5/P6 remain owners. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs) | Existing activation, data-source, history, print and preference entry points are separate. | Reachable commands do not prove a unified Admin host or authorize duplicate command paths. |
| Read-only Control Center Administration [`asnGsLMxceJWb3HlfaE3q4`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), node `5:41` | Metadata gives `1280 x 800`, sidebar `5:55` (`240 x 680`), main pane `5:69` (`980 x 680`), topbar/nav `5:42`-`5:54` and role sample rows `5:70`-`5:78`. | Authentication, roles, users, sync, server, alerts and license-seat labels/sample members are not local product evidence. |

## Proposed local-admin contract

Proposal only; implementation requires D1-D7 approval.

| Region | Current owner/source | Display rule |
| --- | --- | --- |
| `ActivationStatus` | `TrialLicenseService.Check()` / `ActivationLicense` / `ActivationWindow` | Show local status, scope and actionable diagnostic; never show raw key or server seat total. |
| `DesignerPreferences` | `DesignerPreferencesService` | Show snap/grid values and fallback/read status; reset/save through existing owner only. |
| `PrinterPreferences` | `PrinterPreferencesService` | Show last-used values and fallback diagnostic; Printer Setup owns validation. |
| `DataSourceRegistry` | `DataSourceRegistry` | Show path-safe location, schema/count and load/save status; future schema is fail-closed. |
| `DataSourceCleanup` | Existing Database Manager/Cleanup windows | Link to selected-source confirmation; no silent removal or template rewrite. |
| `PrintEvidenceLinks` | P5 History, P6 Analytics, Print Center | Show path/availability/last-read diagnostics and deep-links; no duplicate actions. |
| `RetentionPreview` | No current scheduler/service | Preview source-by-source age/size/backup/recovery before any future archive/delete; no mutation in M1. |
| `Roles/Users/Sync/Server/Seats` | No local authoritative source | `Unavailable in local desktop` or research-only; no sample data. |

## State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Activation valid | Local status, expiry/customer if approved and machine scope | Open details/activation | Never show server seat totals. |
| Activation missing/expired/wrong machine | Exact local status and repair path | Activate or continue in documented trial/limited mode | Do not silently treat invalid as valid. |
| Activation storage/tamper error | Error source and repair guidance | Retry/repair/support | No entitlement claim. |
| Preferences loaded | File scope, values and last-read basis | Edit/reset/save | Preferences cannot mutate template-authored geometry or Text/TextBox policy. |
| Preferences fallback | Defaults plus malformed/unreadable diagnostic | Repair/reset | Do not hide that defaults are active. |
| Registry healthy | Schema/path/count and last load/save | Manage/relink/cleanup | Save failure remains visible; no silent data loss. |
| Registry future/invalid | Version/error and safe upgrade path | Upgrade/backup/close | Never downgrade or overwrite unknown entries. |
| Cleanup candidates | Missing/unused path, selection count and current-template warning | Confirm removal or cancel | No removal without explicit confirmation. |
| Evidence partial/unavailable | Per-source status, path-safe identity and last successful read | Open owner/repair/refresh | Unavailable is not zero and does not erase valid evidence. |
| Retention preview | Source, age/size, destination, backup and recovery test | Archive/backup/cancel after policy closes | No delete without verified recovery plan and confirmation. |
| Unsupported server category | `Not available in local desktop` / research-only label | Return to supported local surface | Do not expose disabled controls as pending implementation. |
| Figma research sample | Clearly marked design reference | None | Sample roles/members/permissions/seat values never become fixtures. |

## Host-neutral layout and automation

```text
[Local Administration: machine scope | Refresh | source status]
[Activation status] [Designer preferences] [Printer preferences]
[Data sources: schema/path-safe count | Manage | Cleanup]
[Evidence: History | Analytics | Print Center | source health]
[Maintenance: retention preview (deferred until policy/recovery proof)]
[Unsupported server categories: unavailable in local desktop]
```

Only existing owners may mutate activation, preferences or data sources. At `1024 x 600`, collapse categories into one scope/drawer and stack cards under one intentional scroll owner; keep failure copy and supported links visible without page-level horizontal scroll.

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root/source status | `CC.P7.Admin.Root` / `CC.P7.Admin.SourceStatus` | Local administration / Local administration source status |
| Activation | `CC.P7.Admin.Activation` / `CC.P7.Admin.OpenActivation` | Activation status / Open activation |
| Preferences | `CC.P7.Admin.DesignerPreferences` / `CC.P7.Admin.PrinterPreferences` | Designer preferences / Printer preferences |
| Data sources | `CC.P7.Admin.DataSources` | Data-source registry status |
| Manage/Cleanup | `CC.P7.Admin.ManageDataSources` / `CC.P7.Admin.CleanupDataSources` | Manage data sources / Clean up data sources |
| Evidence links | `CC.P7.Admin.EvidenceLinks` | Print evidence links |
| Retention | `CC.P7.Admin.RetentionPreview` | Preview evidence retention |
| Unsupported category | `CC.P7.Admin.UnsupportedCategory` | Unavailable in local desktop |

## Fixture and regression packet

These are proposed fixtures and gates, not tests added by this documentation-only change.

| Fixture | Expected result | Required evidence |
| --- | --- | --- |
| Activation valid/missing/expired/wrong-machine | Correct local status and repair path | No seat/role/server claim; raw key remains hidden. |
| Activation tampered/storage error | Explicit error and no entitlement | Existing licensing owner remains source of truth. |
| Preferences missing/malformed | Defaults plus diagnostic | Reset does not mutate template geometry or Text/TextBox behavior. |
| Registry legacy array | Explicit migration to schema 1 | Existing entries preserved and save remains atomic. |
| Registry future schema/invalid JSON | Fail-closed diagnostic | Unknown entries not overwritten or downgraded. |
| Cleanup selected/cancelled/current-template source | Confirmation and safe cancel/protection | No silent registry removal or template-binding rewrite. |
| Evidence source missing/partial/corrupt | Link/status diagnostic | P5/P6 owners remain authoritative; unavailable is not zero. |
| Retention dry run | Per-source age/size/destination/backup/recovery preview | No delete; only approved recovery plan may unlock later action. |
| Retention recovery failure | Action blocked with repair path | Never delete the only valid job/revision/workflow evidence. |
| Unsupported Figma role/license/sync sample | Unavailable/research-only state | Sample members, permissions and seats never become runtime data. |

## No-go list

- Do not copy Figma authentication, role, user/group, workflow, server, sync, alert or license-seat categories into local product claims.
- Do not expose raw activation keys, credentials, customer payloads or unrestricted absolute paths in UI/log/export by default.
- Do not make Admin a second owner for activation, Printer Setup, data-source cleanup, History, Analytics, recovery, reprint or workflow transitions.
- Do not delete CSV/operation/state/revision/workflow evidence from a generic retention button; preview sources separately and require verified backup/recovery policy first.
- Do not treat a preferences fallback, registry failure or activation storage error as healthy/valid without an explicit diagnostic.
- Do not rewrite template bindings, authored geometry or Text/TextBox policy from local settings/admin status.
- Do not claim server authentication, roles, sync, SMTP, license seats or physical printer readiness without a named local source and owner.
- Do not change Text/TextBox ownership, sizing, wrapping, clipping, padding, overflow or print parity.

## Owner sign-off record

Record one owner, date and decision for every row. Blank rows keep CC-P7 open.

| Decision | Owner | Date | Approved option / notes |
| --- | --- | --- | --- |
| D1. Host/action ownership | `TBD` | `TBD` | `TBD` |
| D2. Activation status/scope | `TBD` | `TBD` | `TBD` |
| D3. Preferences boundary | `TBD` | `TBD` | `TBD` |
| D4. Registry/cleanup ownership | `TBD` | `TBD` | `TBD` |
| D5. Evidence/retention policy | `TBD` | `TBD` | `TBD` |
| D6. Privacy/security display | `TBD` | `TBD` | `TBD` |
| D7. Unsupported-category treatment | `TBD` | `TBD` | `TBD` |
| D8. Runtime/Figma/regression owners | `TBD` | `TBD` | `TBD` |

**Closure rule:** CC-P7 may move from design review to implementation only after D1-D8 are filled, existing mutation/read owners are named, privacy and retention/recovery behavior are explicit, and activation/preferences/registry/cleanup/partial-evidence fixtures are converted into runtime and regression gates. Until then, CC-P7 remains a local administration plan and not a server-admin surface or destructive retention tool.
