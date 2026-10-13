# CC-P7 Local Maintenance decision record

**Status:** product boundary decided; local host implemented (2026-08-13)
**Handoff:** [`CC_P7_ADMINISTRATION_UI_HANDOFF.md`](CC_P7_ADMINISTRATION_UI_HANDOFF.md)
**Specification:** [`CC_P7_ADMINISTRATION_UI_SPEC.md`](CC_P7_ADMINISTRATION_UI_SPEC.md)

## Decisions

| ID | Decision |
| --- | --- |
| D1 | Rename the product slice from light Administration to Local Maintenance. |
| D2 | Exclude software licensing, activation, entitlement and printer-seat accounting entirely. Existing release mechanics are untouched but receive no new UI or roadmap work. |
| D3 | Exclude authentication, roles, users/groups, SMTP, sync and server administration. |
| D4 | Reuse existing preference, Printer Setup, Database Manager/Cleanup, History, Analytics and Print Center owners. |
| D5 | Retention is preview/recovery-first; no archive/delete action until source-specific backup and recovery policy exists. |
| D6 | Figma Administration `5:41` is category/density reference only; excluded categories and sample roles never become controls or fixtures. |
| D7 | Runtime closure requires target-scale accessibility, failure-state fixtures and full regression evidence. |

## Delivered implementation

- A source-backed Local Maintenance host deep-links to the existing local
  owners and exposes source health without duplicating mutation actions.
- Retention is visibly unavailable rather than represented as a zero-result
  maintenance action.

## Open implementation work

- define retention sources, archive destination, recovery verification and confirmation;
- verify privacy-safe path display and target-scale layout.

These are downstream implementation tasks. Licensing is not an open decision and must not reappear in future P7 planning.
