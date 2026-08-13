# ANLAbel — CC-P7 Administration UI handoff

**Status:** roadmap/design review; local-admin scope only (2026-08-13)
**Owning roadmap:** [`MASTER_PLAN.md`](../MASTER_PLAN.md), section `7. Administration`
**Related handoffs:** [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md), [`CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md`](CC_P3_DOCUMENT_LIBRARY_REVISION_UI_HANDOFF.md), [`CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md`](CC_P4_APPROVAL_WORKFLOW_UI_HANDOFF.md), [`CC_P6_ANALYTICS_UI_HANDOFF.md`](CC_P6_ANALYTICS_UI_HANDOFF.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md) remains authoritative for Text/TextBox behavior.
**Program index / host gate:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md), sections 2 and 5

CC-P7 is a cross-cutting local-settings/retention slice, not a replacement for the P1-P6 action owners. Reuse the shared host only for local activation, preferences, data-source and evidence-retention links; roles, users, sync and server-license semantics remain outside this roadmap boundary.

This handoff translates the CC-P7 “light admin” roadmap into an evidence-backed local desktop boundary. It does not add roles, users, a server license seat table, SMTP alerts, synchronization, workflow administration, or Figma edits. The Figma Administration frame is a competitive research shell; current ANLAbel evidence supports only local activation, preferences, data-source registry/cleanup and local log/recovery ownership.

## 1. Product boundary

NiceLabel’s research administration surface includes authentication, roles, users/groups, workflow/versioning, database replacements, variables, alerts, application server, synchronization, history cleanup and licenses. ANLAbel today is a single-machine WPF application with:

- machine-bound trial/activation validation stored through protected local files and the current-user registry;
- designer and printer preferences stored locally and independently from label documents;
- a versioned local data-source registry with atomic save, legacy migration and future-schema rejection;
- an explicit orphaned-data-source cleanup dialog;
- local CSV/JSONL/hash-chained print evidence, but no retention scheduler or centralized admin service.

CC-P7 is therefore **local configuration and maintenance with explicit source ownership**. It is not a Control Center server, multi-user identity/role system, license-seat manager, SMTP appliance, sync service or approval-policy authority.

## 2. Existing source evidence

| Surface/evidence | Current behavior | CC-P7 implication |
| --- | --- | --- |
| [`TrialLicenseService.cs`](../src/ANLAbel.App/Services/TrialLicenseService.cs) / [`ActivationWindow.xaml`](../src/ANLAbel.App/ActivationWindow.xaml) | Validates machine-bound activation payloads; protects local license/trial state with DPAPI; mirrors state to LocalAppData/AppData and current-user registry where possible; reports invalid, wrong-machine, expired, tampered or storage-error states. | Admin may show local activation status and open the existing activation flow. Never display Figma `Used: 0 Total: 100`, server seats, or multi-user entitlement. A storage error must remain visible and must not silently become “licensed.” |
| [`ActivationLicense.cs`](../src/ANLAbel.Core/Licensing/ActivationLicense.cs) | Defines signed activation payload/validation status and machine binding. | Keep key validation in the licensing owner; admin UI is a read/repair surface, not a key-generation or signature-bypass path. |
| [`DesignerPreferences.cs`](../src/ANLAbel.Data/Preferences/DesignerPreferences.cs) / [`DesignerPreferencesService.cs`](../src/ANLAbel.Data/Preferences/DesignerPreferencesService.cs) | Stores Snap-to-objects, Snap-to-grid and grid step in LocalAppData; malformed/missing/unreadable settings fall back to defaults. | Admin may expose workspace defaults as local preferences. Preserve the invariant that opening a template does not rewrite user workspace behavior. Do not move authored label geometry or Text/TextBox policy into admin preferences. |
| [`PrinterPreferencesService.cs`](../src/ANLAbel.App/Services/PrinterPreferencesService.cs) | Stores last-used printer, paper/category, DPI and orientation; failures are non-critical and fall back to defaults. | A local preferences panel may show/reset these values, but it must not claim queue health, license entitlement or physical printer readiness. Existing Printer Setup remains the owner of printer validation. |
| [`DataSourceRegistry.cs`](../src/ANLAbel.Data/DataSourceRegistry.cs) | Machine-wide versioned registry under AppData; supports legacy bare-array migration, future-schema rejection, atomic temp-file replacement, upsert and remove. | Data Sources admin can expose registry health, path, schema and repair/relink entry points. Registry save failure must be explicit; do not silently discard entries or rewrite template bindings. |
| [`DataSourceCleanupWindow.xaml`](../src/ANLAbel.App/DataSourceCleanupWindow.xaml) / `.xaml.cs` | `560 × 440` dialog lists missing/unused sources, supports multi-select and confirmation before irreversible registry removal. | Reuse the existing cleanup owner and confirmation semantics. This is maintenance, not log cleanup, file deletion or physical data erasure. |
| [`PrintLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintLogService.cs), [`PrintOperationLogService.cs`](../src/ANLAbel.Data/PrintLogs/PrintOperationLogService.cs), [`PrintJobStateStore.cs`](../src/ANLAbel.Data/PrintLogs/PrintJobStateStore.cs) | CSV is append-only and exportable; operation JSONL is best-effort; job state is hash-chained and has recovery diagnostics. No retention scheduler exists. | A future “Log retention” admin slice must define source-by-source archive/backup/recovery first. It must not delete logs simply because a UI button exists or treat best-effort JSONL as complete audit. P5 History remains the read/action owner. |
| [`MainWindow.xaml.cs`](../src/ANLAbel.App/MainWindow.xaml.cs) | Existing activation, data-source manager/cleanup, printer setup, history and preferences entry points are separate. | Choose whether CC-P7 is a thin Admin window linking to existing owners or a grouped settings shell. Do not duplicate activation, cleanup, history or printer mutation services. |

## 3. Figma reference and routing

Use the existing [ANLAbel Control Center Figma file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) as research input. Read-only metadata for frame `5:41` was checked on 2026-08-13; no Figma node was edited or duplicated.

Frame `5:41` (`CC / Administration`, `1280 × 800`) has a broad server-admin vocabulary:

| Node | Measured reference | WPF/design boundary |
| --- | --- | --- |
| `5:55` | Sidebar `(16,104)`, `240 × 680`; Authentication, Access Roles, Role Permissions, Application Users/Groups, Versioning and Workflows, Database Replacements, Global Variables, Alerts, Application Server, Synchronization, History Log Cleanup, Licenses. | Treat these as research categories. Only local activation, data-source registry/cleanup, designer/printer preferences and a carefully scoped local log-retention proposal have source evidence today. |
| `5:69` | Main pane `(276,104)`, `980 × 680`; role table and sample rows Administrator, Approver, Designer, Operator, Viewer. | Sample roles/members/permissions are not ANLAbel identities. Do not implement role controls or show sample members without an approved local identity/permission contract. |
| `5:56`–`5:68` | Category copy includes server/sync/licenses. | No local server or sync feature follows from the frame. Keep unsupported categories disabled, deferred or explicitly marked research-only. |
| `5:42`–`5:54` | Shared topbar/nav `1280 × 90`. | Shell density reference only; no web Control Center claim or requirement to recreate it in WPF. |

The frame has no local activation-status, preferences-corrupt, registry-future-schema, cleanup-confirmation, retention-preview, permission-denied or audit-recovery state. Those states need WPF acceptance design if a local admin slice is approved. A new Figma frame is unnecessary for this documentation checkpoint.

## 4. Proposed local-admin slices

### M1 — Local status and links

1. Show local activation/entitlement status, machine-code/help path and a link to the existing Activation window.
2. Show preference locations and current values for designer/printer defaults with reset/reload semantics.
3. Show data-source registry path/schema/entry count and links to Manage/Cleanup existing owners.
4. Show local evidence paths/last-read diagnostics as links to P5 History, without duplicating history actions.

### M2 — Maintenance with explicit confirmation

1. Data-source cleanup remains the existing selected-source confirmation flow.
2. A future log-retention preview must enumerate CSV/operation/state files, size, age, backup destination and recovery path before any archive/delete action.
3. Retention must never delete the only valid job-state evidence or silently trim revision/workflow audit; retention policy owners are separate from Analytics/History read models.

### M3 — Optional local alerts/settings

Only after source/ownership decisions: add local queue-fault notification preferences or SMTP as a separate security/product decision. Do not imply server alerts, roles or user groups from the Figma categories.

Roles/users/groups, workflow administration, application server, synchronization, centralized licenses and remote authentication remain deferred.

## 5. State and failure matrix

| State | Visible evidence | Safe action | Fail-closed rule |
| --- | --- | --- | --- |
| Activation valid | Local status, customer/expiry if available, machine scope | Open activation/details | Never show server seat totals. |
| Activation missing/expired/wrong machine | Exact local status and repair path | Activate or continue in documented limited/trial mode | Do not silently treat invalid as valid. |
| Activation storage error/tamper | Error source and repair guidance | Retry/repair/support | No entitlement claim. |
| Preferences loaded | File scope, values and last-read status | Edit/reset/save | Preferences cannot mutate template-authored geometry or protected Text/TextBox behavior. |
| Preferences corrupt/unreadable | Fallback defaults plus diagnostic | Repair/reset | Do not hide that defaults are active. |
| Registry healthy | Schema, path, source count and last load/save | Manage/relink/cleanup | Save failures remain visible; no silent data loss. |
| Registry future schema/invalid | Version/error and safe upgrade path | Upgrade/backup/close | Never downgrade or overwrite unknown entries. |
| Cleanup candidates | Missing path, last-use evidence and selection count | Confirm removal or cancel | No removal without explicit confirmation; current-template source remains protected by existing owner rules. |
| Log retention preview | Source, age/size, archive destination, recovery test/status | Archive/backup/cancel | No delete without a verified recovery plan and explicit confirmation. |
| Unsupported server role/license/sync category | “Not available in local desktop” / research-only label | Return to supported local surface | Do not expose disabled controls as if they were pending implementation. |

## 6. WPF mapping and acceptance gates

| Gate | Evidence before implementation closure |
| --- | --- |
| Host decision | Owner chooses a thin local `AdminWindow` or grouped Settings surface; existing Activation, Printer Setup, Data Source Manager/Cleanup and History remain action owners. |
| Scale/layout | Runtime screenshots/UI Automation at `1024 × 600`, `100%`, `125%`, `150%`; long category lists use one intentional scroll owner and do not hide primary actions. |
| Accessibility | Stable names/AutomationIds for activation status/open, preference sections, registry status/manage/cleanup, retention preview/archive/cancel and unsupported-category copy. |
| Data safety | Atomic registry/preferences behavior, explicit cleanup confirmation, backup/recovery preview and no silent license/log downgrade. |
| Contract safety | No Text/TextBox ownership, sizing, wrapping, clipping, padding, resize, overflow or designer/preview/print parity changes. |
| Separation of ownership | Admin does not duplicate print recovery/reprint, History filters, printer validation, workflow transitions or data-source mutation. |
| Privacy/security | No raw activation key display/storage in logs; machine/license/customer data follows local privacy rules; no role identity claim without a real provider. |
| Regression | Unit/contract coverage for activation status mapping, preferences fallback, registry migration/future-schema failure, cleanup confirmation and retention dry-run/recovery before enabling mutation. |
| Figma | Record exact state-specific node and measured dimensions if a UI slice is approved. `5:41` alone is not runtime proof. |

## 7. Owner decisions needed

1. Should CC-P7 be a thin local Admin hub or a Settings surface embedded in MainWindow?
2. Which local status cards are useful: activation, data sources, preferences, log evidence, or printer setup links?
3. What log-retention policy is required, and what backup/recovery verification must precede deletion?
4. Which paths and customer/license fields may appear in local UI/export/support bundles?
5. Are local queue-fault alerts in scope, and if SMTP is considered, who owns credentials, TLS and failure handling?
6. Is there a real local identity/role requirement for CC-P4 approval, or should all copy remain “local operator”?
7. Which unsupported categories should remain hidden versus visibly marked “server feature not available”?

## 8. Decision

**Needs product/design review.** Figma `5:41` supplies a broad Administration information architecture, but current evidence supports only local activation, preferences, data-source registry/cleanup and a future retention design. The next safe step is to choose a thin local host and retention/security policy; no roles, users, sync, SMTP, license-seat server, workflow admin, or Figma edit is authorized by this handoff.
