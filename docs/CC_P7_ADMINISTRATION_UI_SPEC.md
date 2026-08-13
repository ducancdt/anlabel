# ANLAbel — CC-P7 Administration (light) UI/UX spec

**Status:** design-only local settings/maintenance spec; host, retention and security policy remain open (2026-08-13)
**Predecessors:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md), [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md), [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md)
**Handoff:** [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md)
**Program route:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Figma reference:** [NiceLabel Control Center research file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4), Page `0:1`, Administration `5:41`

This spec maps the Figma Administration shell to a thin local desktop surface for activation status, designer/printer preferences, data-source registry/cleanup and evidence links. It does not add roles, users, sync, server licenses, SMTP, workflow administration or a second action owner.

## 1. Operator outcome

The first local-admin surface should let an operator:

1. see machine-bound activation status and open the existing activation owner;
2. inspect/reset local designer and printer preferences without changing authored label data;
3. inspect data-source registry path/schema/health and open the existing manager/cleanup owner;
4. reach P5 History, P6 Analytics and Print Center evidence owners without duplicating their actions;
5. preview any future retention/archive operation with source, age, size, backup and recovery evidence before confirmation;
6. recognize unsupported server-admin categories as unavailable rather than pending local capability.

## 2. Figma node map (read-only)

Metadata for `5:41` was rechecked read-only on 2026-08-13. The role table and server categories are research vocabulary, not local product data.

| Figma node | Metadata name / bounds | ANLAbel role | Boundary |
| --- | --- | --- | --- |
| `5:41` | `CC / Administration`, `1280 x 800` | Administration density/reference frame | Not a WPF server-admin, identity or license-seat contract. |
| `5:42`–`5:54` | Topbar/nav, `1280 x 90` | Optional host chrome | No sign-out, help or server identity without a local owner. |
| `5:55` | Sidebar, `(16,104)`, `240 x 680` | Category/navigation reference | Only local activation, preferences, data sources and evidence links have current source owners. |
| `5:56`–`5:68` | Authentication, roles, users, workflows, replacements, variables, alerts, server, sync, cleanup, licenses | Research category list | Unsupported categories must be hidden, disabled with reason or marked server-only; never implied as shipped. |
| `5:69` | Main pane, `(276,104)`, `980 x 680` | Local status/detail owner | Replace sample role table with source-backed local cards/status rows. |
| `5:70`–`5:78` | Access Roles table and sample members/permissions | Density/reference only | `Administrator`, `Approver`, `Designer`, `Operator`, `Viewer` are not ANLAbel identities. |

The frame has no activation-error, preference-corrupt, registry-future-schema, cleanup-confirmation, retention-preview or recovery state. No new Figma node is required for this documentation spec.

## 3. Local source-to-card contract

| Admin region | Current owner/source | Display rule |
| --- | --- | --- |
| `ActivationStatus` | `TrialLicenseService.Check()` / `ActivationLicense` / existing `ActivationWindow` | Show valid, trial, missing, expired, wrong-machine, tampered or storage-error status with machine scope. Never show server seats or “Used/Total” claims. |
| `DesignerPreferences` | `DesignerPreferencesService` local file | Show snap/grid values, path/scope and load diagnostic; reset/save only through the preference owner. Do not move authored geometry or Text/TextBox policy into preferences. |
| `PrinterPreferences` | `PrinterPreferencesService` local file | Show last-used printer/paper/DPI/orientation and fallback diagnostic. Printer Setup remains the owner of queue validation and physical readiness. |
| `DataSourceRegistry` | Versioned `DataSourceRegistry` under AppData | Show path, schema/version, entry count, last load/save and migration/future-schema status. Save failure is visible; unknown entries are never silently overwritten. |
| `DataSourceCleanup` | Existing `DatabaseManagerWindow` / `DataSourceCleanupWindow` | Link to the existing selected-source confirmation flow; cleanup is registry maintenance, not log deletion or physical data erasure. |
| `PrintEvidenceLinks` | P5 History, P6 Analytics, Print Center and CSV/JSONL/state owners | Show per-source path/availability/last-read diagnostics and deep-link; do not duplicate filters, recovery, reprint or export authority. |
| `RetentionPreview` | No current scheduler/service; future explicit policy | Preview CSV/operation/state/revision/workflow sources separately with age, size, destination, backup and recovery test before any archive/delete action. |
| `Roles/Users/Sync/Server/Seats` | No local authoritative source | `Unavailable in local desktop` or research-only; no sample members, permissions or entitlements. |

## 4. Host-neutral wireframe

```text
[Local Administration: machine scope | Refresh | local source status]

[Activation status] [Designer preferences] [Printer preferences]

[Data sources: registry path/schema/count | Manage | Cleanup]
[Evidence: History | Analytics | Print Center | source health]

[Maintenance: retention preview / archive (deferred until policy and recovery proof)]
[Unsupported server categories: unavailable in local desktop]
```

Every mutation is owned by the existing service/window. Admin selection and status refresh are read-only; cleanup, preference save, activation and future retention operations require the owner’s confirmation semantics.

## 5. State and failure matrix

| State | Visible evidence | Safe next action | Fail-closed rule |
| --- | --- | --- | --- |
| `ActivationValid` | Local status, customer/expiry if available and machine scope | Open details/activation | Never show a server seat total. |
| `ActivationMissingOrExpired` | Exact local status and repair path | Activate or continue in documented trial/limited mode | Do not silently treat invalid as valid. |
| `ActivationStorageOrTamperError` | Error source and guidance | Retry/repair/support | No entitlement claim. |
| `PreferencesLoaded` | File scope, values and last-read basis | Edit/reset/save | Preferences cannot mutate template-authored geometry or protected Text/TextBox behavior. |
| `PreferencesFallback` | Defaults plus malformed/unreadable diagnostic | Repair/reset | Do not hide that defaults are active. |
| `RegistryHealthy` | Schema/path/count and last load/save | Manage/relink/cleanup | Save failures remain visible; no silent data loss. |
| `RegistryFutureOrInvalid` | Version/error and safe upgrade path | Upgrade/backup/close | Never downgrade or overwrite unknown entries. |
| `CleanupCandidates` | Missing/unused path, selection count and current-template warning | Confirm removal or cancel | No removal without explicit confirmation. |
| `RetentionPreview` | Source, age/size, archive destination, backup and recovery test | Archive/backup/cancel | No delete without verified recovery plan and confirmation. |
| `EvidencePartialOrUnavailable` | Per-source status, path and last successful read | Open owner/repair/refresh | Unavailable is not zero and never erases valid evidence. |
| `UnsupportedServerCategory` | `Not available in local desktop` / research-only label | Return to supported local surface | Do not expose disabled controls as pending implementation. |

## 6. Responsive behavior and automation

| Target | Layout behavior | Scroll/focus rule |
| --- | --- | --- |
| `1280 x 800` | May preserve the `240 DIP` category rail and `980 DIP` main proportions as visual reference; local cards replace role rows. | One category/status scroll owner; primary local links remain visible. |
| `1024 x 600` | Collapse categories into a drawer/narrow scope; stack activation/preferences/registry/evidence cards; keep failure copy and supported links visible. | Keyboard order: source status → category → selected card → owner link → confirmation. |
| `100%`, `125%`, `150%` | Reflow or clip only inside declared owners; no page-level horizontal scroll and no blind Figma scaling. | Capture screenshot/UI Automation at every scale and record environment exceptions. |

Proposed IDs require host approval:

| Region/control | Proposed AutomationId | Accessible name |
| --- | --- | --- |
| Root | `CC.P7.Admin.Root` | `Local administration` |
| Refresh/source status | `CC.P7.Admin.SourceStatus` | `Local administration source status` |
| Activation card/open | `CC.P7.Admin.Activation` / `CC.P7.Admin.OpenActivation` | `Activation status` / `Open activation` |
| Designer preferences | `CC.P7.Admin.DesignerPreferences` | `Designer preferences` |
| Printer preferences | `CC.P7.Admin.PrinterPreferences` | `Printer preferences` |
| Data-source registry | `CC.P7.Admin.DataSources` | `Data-source registry status` |
| Manage/Cleanup | `CC.P7.Admin.ManageDataSources` / `CC.P7.Admin.CleanupDataSources` | `Manage data sources` / `Clean up data sources` |
| Evidence links | `CC.P7.Admin.EvidenceLinks` | `Print evidence links` |
| Retention preview | `CC.P7.Admin.RetentionPreview` | `Preview evidence retention` |
| Unsupported category | `CC.P7.Admin.UnsupportedCategory` | `Unavailable in local desktop` |

## 7. Acceptance gate

Before implementation review closes P7:

- owner chooses a thin local Admin host and keeps Activation, Printer Setup, Database Manager/Cleanup, P5 History and P6 Analytics as action owners;
- fixtures cover activation valid/missing/expired/wrong-machine/tampered/storage-error, preference fallback, registry migration/future-schema/error, cleanup confirmation and retention dry-run/recovery;
- local status cards expose source path/scope, last-read basis and diagnostics without server/user/role/seat claims;
- any destructive cleanup/archive/delete operation names its source, backup and recovery path and requires explicit confirmation;
- unsupported Figma categories never become hidden pseudo-features or sample identities;
- runtime screenshot/UI Automation covers `1024 x 600`, `100%`, `125%`, `150%`, keyboard/focus and scroll ownership;
- no Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow or designer/preview/print parity changes are introduced.

Until these gates close, this file is a UI/UX specification, not a shipped Administration surface or server-admin feature.
