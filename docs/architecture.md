# ANLAbel architecture

**Status:** current source-tree map (2026-08-13) · not a release claim

The old Phase 1–4 labels are historical planning language. The current repository already contains the data, barcode, scene/render, print-preflight, revision, and industrial-output boundaries described below. Release status still belongs to [`MASTER_PLAN.md`](../MASTER_PLAN.md), [`PLAN.md`](../PLAN.md), and the [continuation handoff](reinvention/10-continuation-handoff-2026-08-13.md).

## Runtime modules

| Module | Responsibility | Boundary to preserve |
| --- | --- | --- |
| ANLAbel.App | WPF shell, designer canvas, Properties panel, data/database windows, preview, print center, revisions, and operator commands. | UI owns interaction and dialogs; policy and persistence stay in Core/Data/Project services. |
| ANLAbel.Core | Label/template models, enums, binding expressions, physical geometry, text layout contracts, barcode contracts, scene snapshots, and output identities. | Persist geometry in millimetres; keep pure contracts deterministic and UI-independent. |
| ANLAbel.Data | Excel/CSV connectors, shared data-source registry, freshness/watchers, data-operation logs, and print-operation/job evidence. | Workbook I/O is async/cancellable where applicable; log failures must not corrupt the primary operation. |
| ANLAbel.Project | .anlabel JSON envelope, schema migration, atomic save/load, backups, revision archive, and recovery results. | Preserve backward compatibility; future schema versions fail explicitly rather than being silently downgraded. |
| ANLAbel.Barcode | Renderer abstraction and ZXing-backed raster/vector barcode generation, validation, and logical-module seams. | Keep the engine behind interfaces; print geometry uses the effective print-plan DPI. |
| ANLAbel.Printing | Scene compilation, shared text/barcode/image layout, printer profiles, preflight, preview pages, queue dispatch, and support evidence. | Preview and print consume the same immutable scene/layout identity; physical-printer claims remain evidence-gated. |
| ANLAbel.UnitTests | xUnit tests for pure contracts, persistence, geometry, data, barcode, print, and queue policies. | A green unit suite is necessary but does not prove WPF click-through or physical output. |
| ANLAbel.Tests | Application regression harness for end-to-end model/render/preflight workflows and named product gates. | Keep named regression output readable and do not turn a dirty-worktree run into a release claim. |

## Main data and rendering flow

~~~mermaid
flowchart LR
    User["Operator"] --> App["ANLAbel.App\nWPF shell and commands"]
    App --> Core["ANLAbel.Core\nmm model, policies, snapshots"]
    App --> Data["ANLAbel.Data\nExcel/CSV, registry, logs"]
    App --> Project["ANLAbel.Project\nJSON, revisions, recovery"]
    Core --> Scene["Scene compiler\nimmutable output identity"]
    Scene --> Printing["ANLAbel.Printing\npreview, preflight, queue"]
    Scene --> Barcode["ANLAbel.Barcode\nrenderer and module seams"]
    Printing --> Hardware["Windows printer queue\nphysical evidence required"]
~~~

The important direction is one-way: UI actions mutate the model through commands/services; rendering consumes a snapshot and does not write layout back into the authored document unless an explicit user edit (for example, a resize gesture or opt-in barcode sizing policy) owns that change.

## Geometry and output invariants

- Persisted label and object geometry is in millimetres (WidthMm, HeightMm, XMm, YMm). WPF uses DIP for the screen; printer output uses dots at the effective print-plan DPI.
- Preview and print share scene/layout contracts and LabelVisualRenderer/preflight policies. A preview image is not reused as the print source.
- Linear barcode FrameOwned remains the legacy-safe default. SizedFromX is explicit opt-in and derives production width from quantized X-dimension × pure logical module count.
- HRI placement is a shared None / Below / Above policy across designer, preview, print, and preflight; the legacy visibility flag is only a compatibility mapping.
- Text and TextBox remain distinct. Text is content-owned/free-flowing; TextBox is user-frame-owned, wraps and clips inside its authored frame, and must not resize from content or binding values. The authoritative protected contract is [`AGENTS.md`](../AGENTS.md) and [`NICELABEL_TEXTBOX_RESEARCH.md`](NICELABEL_TEXTBOX_RESEARCH.md).

## Persistence and data boundaries

ProjectFileService owns the .anlabel envelope and revision safety. DatabaseConfig stores template-local binding context, while DataSourceRegistry stores reusable shared-source metadata. Import, refresh, relink, unlink, row tracking, and copies-per-record should remain separate operations so a template can return to standalone mode without losing authored binding expressions.

Bindings resolve values for the selected row; they do not own object geometry. A missing field is a visible data/preflight problem, not permission to rewrite a TextBox frame or silently change authored layout.

## UI/UX evidence boundary

Figma is a visual reference and state-design input, not runtime acceptance. The reusable [Figma → WPF handoff template](figma-ui-handoff-template.md) records node IDs, state matrices, WPF mappings, target scales, and evidence gates. Current read-only metadata covers the shell, frequency-first panels, Properties variants, and Excel-link states; it does not yet provide dedicated Database Manager or barcode-Properties frames. Those gaps are tracked in the [continuation handoff](reinvention/10-continuation-handoff-2026-08-13.md), so no new Figma file or runtime rename should be inferred from a partial frame.

## Verification boundary

The recommended local gates are:

~~~powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
~~~

The latest documented run is explicitly a dirty-worktree checkpoint: build passed with zero warnings/errors, xUnit passed 356/356, and the application harness passed 157/157. It is useful engineering evidence, but it does not close release, UI click-through, printer-driver, barcode-verifier, or physical-label gates.
