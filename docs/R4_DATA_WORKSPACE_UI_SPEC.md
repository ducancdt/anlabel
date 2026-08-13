# ANLAbel — R4 Data Workspace authoring and diagnostics UI/UX specification

**Status:** documentation-only, pre-implementation vertical-slice contract (2026-08-13)
**Parent plan:** [`reinvention/07-execution-plan.md`](reinvention/07-execution-plan.md) §R4.4
**Next-slice evidence:** [`reinvention/07-execution-plan.md`](reinvention/07-execution-plan.md) v0.211
**Handoff:** [`R4_DATA_WORKSPACE_UI_HANDOFF.md`](R4_DATA_WORKSPACE_UI_HANDOFF.md)
**Owner decision packet:** [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md)
**Figma reference:** panels file `kqyNBI0DgRHnPzJTDBIui5`, Data shell `9:2`

This specification turns the next R4 product slice into a reviewable operator contract: author one derived field, inspect its sample value and lineage, and repair diagnostics before preview or print. It does not implement a transform editor, add a connector, change the legacy binding language, or change any Text/TextBox behavior.

## 1. Operator outcome

For an imported Excel or CSV source, an operator should be able to:

1. choose one or more source fields;
2. name a derived output such as `PrintName`;
3. author a bounded formula such as `CONCAT(FIELD("PartNo"), "-", FIELD("Lot"))`;
4. see the selected sample row, resolved value and input-field lineage;
5. understand and repair a parse error, missing field, duplicate output or dependency cycle;
6. know that Apply is atomic and that a failed draft cannot replace the last valid committed definition.

The first slice is a compact Data-tab authoring and diagnostics surface. It is not a general-purpose expression IDE, a connector-management wizard or a graph canvas.

## 2. Current evidence and boundary

| Area | Current source evidence | UI implication |
| --- | --- | --- |
| Connector snapshot | `DataConnectorContracts` and `DataTableDataConnector` expose immutable schema/records with paging and cancellation; `MainViewModel.DataConnector` publishes the snapshot beside legacy `DataView`. | Show source identity/schema/row context without creating a second data store. |
| Transform definitions | `LabelTemplate.DataTransforms` persists typed definitions; `MainViewModel.DataTransforms` exposes the committed list. | Draft edits stay local until Apply; save/load/clone must preserve definitions and fingerprint. |
| Evaluation | `DataTransformPipeline` evaluates Formula AST in dependency order and returns sample values plus `DataTransformLineage`. | Display field-level status and lineage from the same evaluator used by preview/print. |
| Dispatch guard | `MainViewModel.DataTransformError` blocks Current Row and All Rows dispatch when a transform fails. | Diagnostics must identify the failing transform and never offer raw-value fallback. |
| Document identity | `DocumentSnapshot.DataTransformFingerprint` includes transform changes in document identity. | A successful Apply is a document change; a draft that is not applied must not alter the saved identity. |

The first vertical slice may reuse the existing Excel/CSV import and binding paths. ODBC/SQL/HTTP credentials, prompt variables, full filter/sort, paging controls and a multi-node graph remain separate product decisions.

## 3. Read-only Figma evidence

Metadata for the panels file was checked read-only on 2026-08-13. The existing nodes provide shell/card language, not transform-specific states:

| Node | Measured metadata | Proposed use | Missing state |
| --- | --- | --- | --- |
| `8:2` | `ANLAbel - Frequency-first Panels v0.198`, `664 × 788` | Workspace/Properties shell and existing panel language | No transform behavior |
| `8:15` | Workspace tabs, `300 × 42` | Preserve real `Layers` / `Data` task switch | No transform tab content |
| `9:2` | Data tab content, `300 × 610`, positioned at `y=90` | Data surface container | No transform editor or sample table |
| `9:3` | Data source / Empty, `276 × 142` | No-source summary and Import action | No linked-source variants |
| `9:16` | Current data context, `276 × 102` | Workbook/preview-row context | No schema/row detail |
| `9:27` | Data settings / Collapsed, `276 × 62` | Secondary disclosure; its hint mentions Transforms | No transform controls |
| `9:35` | Binding checks / Clear, `276 × 42` | Existing diagnostics anchor | No transform error/lineage variants |

**Routing decision:** reuse `9:2`/`9:3`/`9:16`/`9:27`/`9:35` as the visual shell and state anchors. Until an owner approves a state-specific reference, new transform controls are an implementation-owned extension of the WPF Data tab. Do not widen the WPF `268/280` columns from the Figma `300/300` reference, and do not edit Figma merely to fill missing states.

## 4. Data and draft contract

The field names below describe UI concepts; the implementation owner must bind them to the existing Core types rather than duplicating evaluation logic in XAML.

| Concept | Required display | Mutation rule |
| --- | --- | --- |
| Source context | Connector/source identity, workbook or CSV, sheet/pseudo-sheet, schema fingerprint, row count/freshness and selected row | Refresh/relink uses the existing source owner; a stale or failed source is never greened by cached values. |
| Source field | Name, type/shape when known and sample value | Selecting a field inserts a safe `FIELD("Name")` token; it does not rewrite existing bindings. |
| Transform draft | Output name, formula, selected inputs, draft status and sample result | Draft edits are local; Apply validates the whole definition before replacing the committed value. |
| Committed transform | Stable output name, normalized formula, definition order and fingerprint | Save/load/clone preserve the definition and document identity. |
| Lineage | `Output ← input fields`, plus nested derived-field dependencies | Derived from `DataTransformLineage`; no guessed lineage from string search. |
| Diagnostic | Code/severity, transform/output, formula location if available, source fields and repair text | Error state blocks preview/print until the committed definition is valid. |
| Preview row | Selected source row plus derived values | Preview and dispatch resolve through the same transform pipeline. |

### 4.1 Formula authoring boundary

The first UI should expose only the supported Formula AST vocabulary already owned by Core, such as `FIELD`, `CONCAT` and the existing bounded functions. Field insertion and fixed-text insertion are safer than free-form code. Unsupported functions, arbitrary code, credentials, network calls and hidden variables must produce an explicit unsupported/parse diagnostic rather than a best-effort result.

## 5. Host-neutral wireframe

```text
[Data context: source | sheet | schema | freshness | selected row]

[Source fields]                         [Sample row / resolved values]
[search or compact field list]          [row selector + refresh context]

[Transforms]
[PrintName  ✓ valid   PartNo, Lot]      [Add transform]
[selected transform editor]
  Output name: [PrintName]
  Formula:     [CONCAT(FIELD("PartNo"), "-", FIELD("Lot"))]
  Inputs:      [PartNo] [Lot]
  Result:      [ABC-042]
  Lineage:     [PrintName ← PartNo, Lot]
  [Validate] [Apply] [Cancel] [Remove]

[Diagnostics / binding checks]
[no issues | parse/missing/duplicate/cycle/stale details + repair action]
```

At narrow sizes, the field list, sample context and selected editor stack vertically. The diagnostic reason remains visible; it must not be hidden behind a tooltip or page-level horizontal scroll.

## 6. State matrix and safe actions

| State | Required visible evidence | Safe action | Preview/print rule |
| --- | --- | --- | --- |
| No source linked | `No data linked`, source explanation and Import Excel/CSV | Import or keep the label data-free | No transform preview; no false row |
| Source linked, no transforms | Source identity, schema/row context, selected sample row and Add transform | Add a draft or continue binding source fields | Existing source bindings remain available |
| Draft editing | Output name, formula, selected inputs and draft marker | Validate, Apply, Cancel or Remove draft | Draft cannot affect preview/dispatch until Apply |
| Draft valid | Normalized formula, sample result, input fields and neutral/valid status | Apply atomically | Preview uses committed transform only after Apply |
| Formula parse/evaluation error | Exact transform/output, message/location and repair hint | Edit draft or Cancel to last committed definition | Block while the committed/effective transform is invalid |
| Missing source field | Missing field name and source schema context | Select a current field or repair formula/source | No raw fallback; block affected preview/print |
| Duplicate output | Both definitions/output names and conflict explanation | Rename or remove one definition | Block until output identity is unique |
| Dependency cycle | Cycle participants in deterministic order | Break the cycle or remove a definition | Block preview/print for the affected evaluation |
| Source stale/failed | Existing stale/failed evidence, timestamp and Update/Relink action | Refresh or relink first | Do not mark transform green from cached data |
| Binding issue | Affected label object/binding and link to object diagnostic | Repair mapping/output or choose a valid field | Block raw fallback dispatch |
| All-row partial failure | First failing row, source identity and transform diagnostic; later valid rows do not clear it | Repair data/transform, then revalidate all rows | Entire batch remains blocked |
| Saved/reloaded transform | Definition, fingerprint, lineage and result match before save | Edit explicitly to create a new draft | Document identity changes only after committed edit |

## 7. Action and accessibility contract

These IDs are proposals until the WPF owner approves the host and runtime evidence:

| Region/control | Proposed `AutomationId` | Accessible name |
| --- | --- | --- |
| Data root | `DataWorkspace.Root` | `Data workspace` |
| Source summary | `DataWorkspace.SourceSummary` | `Data source context` |
| Source field list | `DataWorkspace.SourceFieldList` | `Source fields` |
| Sample row/context | `DataWorkspace.SampleContext` | `Selected sample row` |
| Transform list | `DataWorkspace.TransformList` | `Committed transforms` |
| Add transform | `DataWorkspace.AddTransform` | `Add transform` |
| Output name | `DataWorkspace.TransformOutputName` | `Derived output name` |
| Formula editor | `DataWorkspace.TransformFormula` | `Transform formula` |
| Input/lineage | `DataWorkspace.TransformLineage` | `Transform inputs and lineage` |
| Result preview | `DataWorkspace.TransformResult` | `Sample transform result` |
| Validate | `DataWorkspace.ValidateTransform` | `Validate transform` |
| Apply | `DataWorkspace.ApplyTransform` | `Apply transform` |
| Cancel draft | `DataWorkspace.CancelTransform` | `Cancel transform draft` |
| Diagnostics | `DataWorkspace.Diagnostics` | `Data diagnostics` |
| Binding checks | `DataWorkspace.BindingChecks` | `Binding checks` |

Keyboard order should be source/context → field list → sample row → transform list → editor → Validate/Apply/Cancel → diagnostics/binding checks. Error announcements must include the output name and a repair-oriented message.

## 8. Responsive and visual acceptance

| Target | Required behavior | Evidence |
| --- | --- | --- |
| `1280 × 800` | Keep source/context and transform editor readable within the existing Data tab; a compact field list may scroll independently | Screenshot/UI Automation for no-source, valid transform and lineage |
| `1024 × 600` | Stack source fields, sample context and editor; keep invalid formula/cycle reason and repair action visible | Screenshot/UI Automation for invalid, cycle and stale/failed states |
| `100%`, `125%`, `150%` | Reflow inside the existing panel; no blind scaling of the `300 DIP` Figma reference and no page-level horizontal scroll | Record window size, display scale, focus order and clipping result |

Runtime evidence is the acceptance artifact. A Figma frame alone does not prove formula evaluation, source freshness, persistence or dispatch blocking.

## 9. Implementation readiness and regression gates

Before the R4 Data Workspace slice is called implemented:

1. Add/edit/cancel/remove and save/load/clone round trips preserve `DataTransformDefinition` and fingerprint.
2. A valid sample displays the transformed value and deterministic input-field lineage.
3. Parse error, missing field, duplicate output and dependency cycle each have actionable diagnostics and fail-closed preview/print behavior.
4. Current Row, Preview and All Rows use the same transformed values; no raw fallback is dispatched after an error.
5. Source stale/failed state remains owned by the existing import/relink/refresh path.
6. Existing Excel/CSV, binding, print preflight, barcode and protected Text/TextBox gates remain green.
7. Runtime screenshot/UI Automation covers the state matrix at `1024 × 600`, `100%`, `125%` and `150%` (or a documented environment exception).
8. The implementation owner records the chosen formula copy, sample-row default, stable IDs and clean commit before marking the handoff ready.

Suggested verification commands:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 10. Explicit non-goals

- ODBC/SQL/HTTP connectors, credentials or a new connection wizard;
- full filter/sort builder, paging controls or a graph-canvas editor;
- prompt/database/date/counter variables beyond the existing supported evaluator;
- silent migration of legacy `{Field}`/Formula meaning;
- raw-value fallback after transform failure;
- any Text/TextBox ownership, sizing, wrapping, clipping, padding, resize or print-contract change;
- a claim of typed connector parity, physical print verification or Figma implementation.

Until the owner approves the first formula vocabulary, draft/commit UX, Figma reuse and runtime evidence owner, this file remains a UI/UX specification and R4 Data Workspace remains open.

The shared source/read-model and Database Manager boundary is governed by [`R4_DATA_SURFACES_OWNER_DECISION_PACKET.md`](R4_DATA_SURFACES_OWNER_DECISION_PACKET.md); do not infer registry ownership, Manager behavior or a new Figma state from this transform specification alone.
