# ANLAbel — CC-P7 Local Maintenance UI/UX contract

**Status:** local-maintenance host implemented; retention remains policy-locked (2026-08-13)
**Handoff:** [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md)
**Decision record:** [`CC_P7_ADMINISTRATION_DECISION_PACKET.md`](CC_P7_ADMINISTRATION_DECISION_PACKET.md)
**Figma reference:** [Administration `5:41`](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4)

## Operator outcome

The future surface may:

1. inspect/reset designer and printer preferences through their owners;
2. inspect data-source registry health and open Manager/Cleanup;
3. reach History, Analytics and Print Center evidence owners;
4. preview retention/archive impact and recovery evidence before any mutation.

It does not implement licensing/activation, identity, roles, users, server sync or alerts.

## Delivered slice

`LocalMaintenanceWindow` provides the thin local host with `CC.P7.Maintenance.*`
automation IDs. It reads local preference/registry/history status and routes
Printer Setup, Database Manager (including its existing Cleanup owner), History,
Analytics and Print Center to their existing owners. The host neither writes
preferences nor removes/archives any evidence.

## Host-neutral layout

```text
[Local Maintenance | Refresh source status]
[Preferences] [Printer defaults]
[Data sources: path-safe identity | schema | count | Manage | Cleanup]
[Evidence: History | Analytics | Print Center | source health]
[Retention preview/recovery — unavailable until policy exists]
```

## States

| State | Required behavior |
| --- | --- |
| Preferences healthy/fallback | Show file scope and diagnostic; defaults are never silently labeled saved values. |
| Registry healthy/future/invalid | Preserve schema/path/count or fail-closed diagnostic; never overwrite unknown data. |
| Cleanup candidates | Existing selection and confirmation owner; safe cancel and current-template protection. |
| Evidence partial/unavailable | Per-source status; unavailable is not zero. |
| Retention preview | Source, age/size, destination, backup and recovery test; mutation remains locked without policy. |

The delivered host explicitly reports retention/recovery as unavailable because
the source, archive, backup and recovery policy has not been approved. It must
not be interpreted as an empty retention queue.

## Figma boundary

Use sidebar `5:55` and main pane `5:69` for density only. Authentication, role, user, workflow-admin, server, sync, license and sample-member nodes are omitted, not deferred controls. No new Figma node is required for this contract.

## Accessibility and safety

- `CC.P7.Maintenance.*` AutomationId prefix;
- one intentional scroll owner at `1024 x 600`, `100%`, `125%`, `150%`;
- stable keyboard routes to Manager, Cleanup, History, Analytics and Print Center;
- no destructive retention without verified backup/recovery and confirmation;
- no Text/TextBox or authored geometry mutation.
