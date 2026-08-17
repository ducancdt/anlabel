# CC-P1/P2/P5 execution gate

**Status:** CC-P1 implementation verified; P2/P5 remain follow-up slices (2026-08-13)
**First slice:** CC-P1 local Operations Overview
**Host decision:** [`CC_P1_P2_P5_HOST_DECISION_PACKET.md`](CC_P1_P2_P5_HOST_DECISION_PACKET.md)
**Read-model contract:** [`CC_P1_P2_P5_READ_MODEL_CONTRACT.md`](CC_P1_P2_P5_READ_MODEL_CONTRACT.md)
**P1 UI contract:** [`CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md`](CC_P1_OPERATIONS_OVERVIEW_UI_SPEC.md)
**Protected contract:** [`AGENTS.md`](../AGENTS.md)

## Why this is executable now

Research and source inspection already answer the decisions needed for the first slice:

- NiceLabel Control Center is a server/web product; ANLAbel adopts only its information hierarchy.
- Existing Figma Overview `2:2` supplies context/card/diagnostic density but no runtime truth.
- `MainViewModel` already owns explicit saved-queue lookup.
- `PrintJobStateStore` and `PrintJobRecoveryService` already own durable recovery evidence.
- `PrintCenterWindow`, `PrinterSetupWindow` and Print History already own their actions.
- Software licensing/activation/seat management is explicitly excluded.

Therefore implementation no longer waits for a blank D1-D8 owner table. Unknown runtime behavior is closed by fixtures and UI evidence, not another decision packet.

## Ordered implementation

### Slice 1 — immutable P1 snapshot

Create one read-only snapshot with:

- request epoch and refreshed-at timestamp;
- requested/canonical queue identity, availability and diagnostic;
- pending recovery count and repair-required/store diagnostics;
- a bounded list of recovery candidates or recent durable events with source timestamps;
- independent partial-failure state per source.

Do not add CSV/operation/state joins beyond the fields needed by P1. Full three-source History remains P5.

### Slice 2 — staged WPF host

Create `OperationsOverviewWindow` with:

- context + Refresh;
- Queue Evidence card;
- Recovery Evidence card;
- explicit Open Printer Setup, Open Print Center and Open Print History buttons;
- diagnostics/recovery list with one intentional scroll owner;
- stable `CC.P1.Overview.*` AutomationIds and accessible names.

No license/activation card, server identity, workstation count, user count or printer-seat count is allowed.

### Slice 3 — shell route

Add one explicit entry point to the existing Print/operations area. The route owns window lifetime and focus restoration but delegates all operational actions to existing handlers/windows.

### Slice 4 — verification

Required commands:

```powershell
dotnet build ANLAbel.slnx --no-restore
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

Required named coverage:

- explicit saved queue never falls back to Windows default;
- recovery snapshot preserves corrupt-tail diagnostics;
- overview refresh rejects stale request epochs;
- overview routes preserve existing action owners;
- target-scale layout/focus/AutomationIds are reachable;
- protected Text/TextBox named gates remain green.

## Stop rules

Stop and record a concrete blocker only when:

- source APIs cannot provide the required field without changing an upstream contract;
- implementing the host would require touching protected Text/TextBox behavior;
- a runtime state requires a design decision that source, research and existing Figma metadata cannot answer;
- build/test failures prove the dirty implementation wave is not a safe baseline.

Do not stop merely because P2/P5/P3…P8 decisions remain open. Do not create another handoff/spec/decision packet for P1.

## Evidence log

| Gate | Current result | Evidence |
| --- | --- | --- |
| Host choice | DECIDED | Staged P1 `OperationsOverviewWindow` |
| License scope | EXCLUDED | No licensing/activation/seat UI or read model |
| Figma route | SUFFICIENT | Read-only Overview `2:2`; no write required |
| Baseline build/test | PASS | Build 0 warnings/errors; unit 356/356; application runner exit 0 before P1 edits |
| P1 read model | PASS | `OperationsOverviewViewModel`; regression rejects a stale epoch and preserves a partial-source result |
| P1 WPF/runtime | PASS | Runtime smoke at 1040 x 700 on the current desktop; UIA found every `CC.P1.Overview.*` route and Print Center opened through the existing owner |
| Target-size boundary | PASS (software) | WPF uses DIP layout with a 900 x 560 minimum and one bounded outer scroll owner; no physical display-scale certification is claimed |
| Post-change suite | PASS | Build 0 warnings/errors; unit 356/356; application runner exit 0 including the new P1 gate and protected Text/TextBox gates |

P2 Queue and P5 History may now start as separate read-only slices. They must not expand P1 or introduce licensing scope.
