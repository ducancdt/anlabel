# ANLAbel — current verification checkpoint (2026-08-13)

**Status:** evidence snapshot; not a release approval
**Documentation checkpoint:** `96f6ab5` (`docs: reconcile barcode execution status`)
**Scope:** record the current source/test/version evidence without staging the broad implementation wave already present in the worktree.

## Why this file exists

`MASTER_PLAN.md` and `PLAN.md` contain a large uncommitted implementation wave. Their historical entries and existing edits must remain intact, so this checkpoint records the current evidence in an additive file. After the implementation owner selects and commits the intended scope, copy the verified snapshot into the release-history sections of those two files; do not infer that the current dirty checkout is releasable.

## Current evidence

| Evidence | Result | Boundary |
| --- | --- | --- |
| Display/source version | `0.202` in the current app project, shell title/build-channel text, and public Commercial/Trial installer metadata | The private License Master installer intentionally remains `1.0.0`; version parity is not a signed release artifact while those source/installer files are dirty. |
| `dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false` | PASS · 0 warnings · 0 errors | Compile evidence for the current checkout. |
| `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet` | PASS · 356/356 | Unit/contract evidence; no physical-device claim. |
| `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build` | PASS · exit 0; 157 registered checks in the current runner | Application regression evidence. Named P1/P2 barcode gates and the protected Text/TextBox regression names are present in the runner; this is not a hardware smoke test. |
| Repository-local Markdown link audit | PASS · 57 Markdown files, 209 relative links checked, 0 broken paths | Relative paths/assets only; external URLs still require network/source review. |
| Worktree ownership | OPEN · broad dirty implementation wave and untracked research/code/assets remain | This checkpoint must not be used to stage or release those files. |

### Named barcode gates rechecked

The application runner passed the gates that close the current barcode software slices:

- `linear barcode width follows quantized X-dim when SizedFromX`
- `compiled scene print uses SizedFromX production width`
- `legacy frame-owned width not auto-sized when X is zero`
- `barcode HRI above reserves top strip`
- `barcode HRI placement survives clone and save`

The corresponding status and non-claims are recorded in [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](../INDUSTRIAL_BARCODE_EXECUTION_PLAN.md), [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](../BARCODE_NICELABEL_BARTENDER_RESEARCH.md), and [`P1_LINEAR_GEOMETRY_NEXT_SLICE.md`](../P1_LINEAR_GEOMETRY_NEXT_SLICE.md).

## Figma boundary

No new Figma inspection or edit was needed for this checkpoint: it changes documentation and evidence ownership, not UI behavior. Existing read-only references remain authoritative for visual intent:

- shell `2:2`;
- panels `8:2`, selected Properties `13:2`, tabs `18:69`;
- Excel verification component `22:82`.

There is still no dedicated barcode-Properties frame. If P3 check-digit/HRI policy becomes a UI slice, select the first operator task, name a Figma node (or explicitly reuse a current reference), then close it with a runtime screenshot/measurement and regression coverage. A Figma frame alone is not runtime proof.

## Release gates still open

- clean implementation ownership and a fresh post-commit rerun of the commands above;
- physical verifier/grade evidence, printer-native command evidence, full GS1/catalog parity and physical-label evidence;
- any UI change requiring target-scale screenshot/measurement and the protected Text/TextBox contract gates.

Until those gates close, public wording remains **software regression evidence + graphic thermal path**, not verifier certification or a shipped multi-tenant Control Center/LMS.

## Handoff

This checkpoint is linked from [`10-continuation-handoff-2026-08-13.md`](10-continuation-handoff-2026-08-13.md). The historical release narratives in [`MASTER_PLAN.md`](../../MASTER_PLAN.md) and [`PLAN.md`](../../PLAN.md) remain unchanged by this file and should receive the next verified snapshot only from the owning clean implementation checkpoint.
