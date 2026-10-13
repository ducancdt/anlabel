# CC-P7 Local Maintenance handoff

**Status:** local host delivered; policy-dependent retention remains downstream (2026-08-13)
**Program:** [`CC_UI_UX_PROGRAM_INDEX.md`](CC_UI_UX_PROGRAM_INDEX.md)
**Specification:** [`CC_P7_ADMINISTRATION_UI_SPEC.md`](CC_P7_ADMINISTRATION_UI_SPEC.md)
**Decision record:** [`CC_P7_ADMINISTRATION_DECISION_PACKET.md`](CC_P7_ADMINISTRATION_DECISION_PACKET.md)

CC-P7 is a local maintenance hub, not an administration server. It may group links and source health for preferences, data-source registry/cleanup, evidence paths and a future retention preview/recovery workflow.

## Explicit exclusions

Software licensing, activation, entitlement, printer-seat accounting, authentication, users, roles, groups, SMTP, synchronization and server administration are outside the ANLAbel Control Center program. The NiceLabel guide and Figma Administration `5:41` retain those concepts only as competitor research.

## Existing owners

| Concern | Owner | P7 behavior |
| --- | --- | --- |
| Designer preferences | `DesignerPreferencesService` | Display/reset through existing owner; never mutate authored document geometry. |
| Printer preferences | `PrinterPreferencesService` and Printer Setup | Display local defaults; Printer Setup owns queue validation. |
| Data sources | `DataSourceRegistry`, Database Manager and Cleanup windows | Show source health and link to existing mutation owners. |
| Print evidence | P5 History, P6 Analytics and Print Center | Show path/availability and deep-link; no duplicate filters/actions. |
| Retention | No current owner | Preview-only until archive, backup, recovery and confirmation policies exist. |

## Research routing

Figma `5:41` supplies only category/sidebar/main-pane density. Replace the role table with source-backed local maintenance cards. Hide excluded server/licensing categories rather than showing disabled pseudo-features. No Figma write is needed until a concrete maintenance-state ambiguity appears.

## Delivered evidence

- `LocalMaintenanceWindow` is the single host and uses `CC.P7.Maintenance.*`
  accessibility IDs.
- It displays local preference, registry and history-evidence status without
  silently treating unavailable sources as zero.
- Printer Setup, Database Manager/Cleanup, History, Analytics and Print Center
  remain distinct action owners.
- The custom regression gate `local maintenance preserves owner boundaries`
  covers the local-source, unavailable-file and locked-retention semantics.

## Remaining acceptance

- one thin local maintenance host and one action owner per linked capability;
- preference fallback and registry future-schema/invalid states remain visible;
- retention is preview-first and cannot delete the only valid evidence;
- target-scale keyboard/focus/scroll evidence;
- full build/test evidence and unchanged protected Text/TextBox behavior.

Target-scale keyboard/focus/scroll inspection and any retention feature remain
open only when the required policy and evidence owners exist.
