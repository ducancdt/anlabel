# ANLAbel — continuation handoff (2026-08-13)

**Status:** active documentation handoff; documentation-only in this slice
**Scope:** reconcile the current Markdown roadmap and define the next evidence gate without touching another agent’s dirty code/UI files
**Protected contract:** the Text/TextBox rules in [`AGENTS.md`](../../AGENTS.md) remain unchanged

## Why this handoff exists

The repository currently contains a large, uncommitted implementation wave across the designer, print pipeline, barcode contracts, data connectors, tests, installers and UI assets. The existing roadmap files also contain new, partially reconciled status blocks. This note is intentionally additive: it records what must be reconciled after the implementation owner reaches a checkpoint; it does not rewrite historical entries or infer that uncommitted code is released.

The source-of-truth split is:

| Concern | Authoritative document | Rule for the next update |
| --- | --- | --- |
| Protected Text/TextBox behavior | [`AGENTS.md`](../../AGENTS.md), [`NICELABEL_TEXTBOX_RESEARCH.md`](../NICELABEL_TEXTBOX_RESEARCH.md) | Never relax or silently refactor the contract. |
| Product history and release narrative | [`MASTER_PLAN.md`](../../MASTER_PLAN.md), [`PLAN.md`](../../PLAN.md) | Append a verified checkpoint; preserve old history. |
| Whole-product execution | [`07-execution-plan.md`](07-execution-plan.md) | Close a slice only with named gates and explicit non-claims. |
| Industrial barcode sequence | [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) | Phase status must agree with the competitive matrix before claiming done. |
| Barcode competitive truth | [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md) | Keep “Have”, “Partial” and “Missing” evidence-based; never claim certification. |
| Designer shell / panels | [`NICELABEL_DESIGNER_SHELL_RESEARCH.md`](../NICELABEL_DESIGNER_SHELL_RESEARCH.md), [`industrial-panel-design.md`](../industrial-panel-design.md) | Use Figma as a visual reference and WPF runtime evidence as acceptance evidence. |
| Control Center comparison | [`NICELABEL_CONTROL_CENTER_USER_GUIDE.md`](../NICELABEL_CONTROL_CENTER_USER_GUIDE.md) | Research only; do not turn it into a web-LMS claim. |

## Reconciliation queue

These are documentation inconsistencies visible in the current worktree. They are deliberately recorded as **open** until the implementation owner supplies a clean checkpoint and fresh command output.

| Priority | Finding | Required resolution |
| --- | --- | --- |
| P0 | The `MASTER_PLAN.md` banner describes barcode P0/P1/P2 as shipped at product display `v0.202`, while the historical status heading still says `2026-08-10`. | Add one current-status block after the release gate with the actual display version, build result, application-test count, xUnit count and smoke evidence. Do not delete the historical entries. |
| P0 | [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) marks P1 and P2 complete, but its older research/checklist text and [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md) still describe P1/P2 as the next/open slice. | Reconcile the phase table, deferred list, “next coding slice” paragraph and matrix rows in one documentation change, using the same named regression gates. |
| P1 | [`industrial-panel-design.md`](../industrial-panel-design.md) is labeled “v0.201”, while the product banner points at `v0.202`. | Clarify that the Figma/design-system revision is the design baseline (if that is intended), or update it after a fresh screenshot review. Do not silently equate a design revision with a release version. |
| P1 | [`PLAN.md`](../../PLAN.md) contains later transform/data checkpoints than the current-status narrative in `MASTER_PLAN.md`. | Once the implementation wave is committed, append a single release snapshot to both files and link the detailed execution checkpoint; keep all earlier entries intact. |
| P2 | Several new Markdown files and UI assets are untracked in this worktree. | Include them in the owning implementation checkpoint only after their links, encoding and asset paths pass the repository audit. This handoff does not stage or commit them. |

## Ordered next work

### 1. Freeze an evidence snapshot

The implementation owner should first reach a clean or explicitly checkpointed worktree, then record:

```text
git status --short
git log -1 --oneline
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

The release snapshot must distinguish application regressions, xUnit tests, build warnings/errors and runtime smoke evidence. A green local test run does not close the hardware, driver, verifier or physical-label gates.

### 2. Reconcile the barcode documents

Use [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) as the ordered phase spine and [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md) as the gap matrix. The next documentation pass should, in one change:

1. make the P1/P2 status table, deferred/open list and “next coding slice” agree;
2. preserve the legacy-safe `FrameOwned` behavior and the explicit opt-in `SizedFromX` claim if those gates are green;
3. keep physical verifier, printer-native command, full GS1 registry and hardware certification as open/non-claims unless external evidence exists; and
4. keep `P1_LINEAR_GEOMETRY_NEXT_SLICE.md` as historical planning context or clearly mark it superseded—never leave two competing “next slice” documents without a pointer.

### 3. Use Figma only for a concrete UI/UX gate

No Figma connection is required for this documentation-only slice. Existing references are enough to plan the next review:

| UI surface | Existing reference | Review question |
| --- | --- | --- |
| Full designer shell | [NiceLabel shell file](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), full frame `2:2` | Do shell regions still map one-to-one to WPF `AutomationId`s without changing Text/TextBox behavior? |
| Frequency-first workspace/panels | [ANLAbel UI exploration](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), overview `8:2`, selected properties `13:2`, tabs `18:69` | Are Layers/Data and Label/Layout/Advanced real task switches, with no duplicate zoom or nested disclosure? |
| Excel link verification | Same Figma file, component `22:82` | Do Not linked / Checking / Verified / Stale / Failed states show evidence and a safe next action? |
| Control Center benchmark | [Control Center shells](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4) and local crops under `docs/assets/nicelabel-control-center/ui-screens/` | Which operations are evidence-backed local desktop features, and which remain research-only? |

Only invoke a Figma inspection/edit when a specific UI slice is selected and the current node does not answer the question. The acceptance artifact should be a screenshot or measured node review at the target window/display scales; a Figma frame alone is not runtime proof. Do not create a second design file for a surface already covered by the references above.

### 4. Close documentation links after the checkpoint

The owning change should run a repository-local link/path audit over Markdown links and referenced assets, then check that every newly named test or version appears in the file that owns that claim. Broken links, stale test counts and contradictory “next” labels remain open findings, not cosmetic cleanup.

## Definition of done for this handoff

- [ ] A clean implementation checkpoint exists; this note is not evidence that the current dirty worktree is releasable.
- [ ] `MASTER_PLAN.md`, `PLAN.md`, the reinvention execution plan and barcode documents agree on the current release snapshot.
- [ ] Historical entries remain intact and future/open hardware claims stay explicitly non-claims.
- [ ] Any UI change has a named Figma node (or an explicit reason to create one), a runtime screenshot/measurement gate and regression coverage.
- [ ] Text/TextBox protected behavior remains unchanged unless the user explicitly reopens that contract and updates its required docs/tests together.

## Handoff note

This file is a coordination aid, not a substitute for the owning implementation agent’s commit. It should be linked from the next verified checkpoint and then retained as the audit trail for the 2026-08-13 documentation reconciliation.
