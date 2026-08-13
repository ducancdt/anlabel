# ANLAbel — Figma → WPF UI/UX handoff template

Use this template for one concrete UI/UX slice. Copy it into the owning plan or issue, fill every required field, and link the resulting runtime evidence. Do not create a new Figma file until the existing references and nodes have been checked.

## 1. Slice identity

| Field | Value |
| --- | --- |
| Slice name | `<!-- concise user-facing name -->` |
| Owner / date | `<!-- person or agent / YYYY-MM-DD -->` |
| User problem | `<!-- what the operator cannot do or understand today -->` |
| In scope | `<!-- one vertical UI workflow -->` |
| Out of scope | `<!-- adjacent screens, backend work, or future phases -->` |
| Related plan | `<!-- Markdown path + section/issue link -->` |

## 2. Existing design reference

| Field | Value |
| --- | --- |
| Figma file URL | `<!-- existing file first; do not paste secrets -->` |
| File key | `<!-- if available -->` |
| Node/frame/component | `<!-- stable node ID and human name -->` |
| Existing asset/crop | `<!-- repository path, if any -->` |
| Why this reference is the right one | `<!-- task, state, and evidence -->` |
| New Figma file needed? | `Yes / No — reason:` |

Known ANLAbel references that should be reused when applicable:

- Full shell recreation: [Figma file](https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation), full frame `2:2`.
- Frequency-first panels: [Figma file](https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5), overview `8:2`, selected properties `13:2`, tabs `18:69`.
- Excel link verification component: same panels file, component `22:82`.
- Control Center research shells: [Figma file](https://www.figma.com/design/asnGsLMxceJWb3HlfaE3q4); local crops are under `docs/assets/nicelabel-control-center/ui-screens/`.

Coverage note (metadata checked 2026-08-13, panels file Page `0:1`): the page currently lists frames `1:2`, `4:2`, `8:2`, `13:2`, `18:69`, and `22:82`, with no dedicated Database Manager frame. Do not treat the Excel-link component as a complete Manager design; for a Manager slice, first record the exact workflow (unlink, test connection, preview, use, remove, or cleanup), then locate or create the smallest state-specific reference and map it to WPF controls.

Barcode UI coverage note: the same page has only the compact-ribbon text layer `1:8` (`Text TextBox Image Barcode`) for barcode authoring. That is a navigation hint, not a Properties/state design. P1/P2 barcode software slices are closed; the proposed P3 check-digit/HRI slice is routed through [`P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md`](P3_BARCODE_CHECK_DIGIT_UI_HANDOFF.md). Any P3+ UI slice must provide a state-specific node (or an explicit decision to reuse the existing WPF surface), measured controls, and runtime evidence. Do not infer check-digit policy, HRI display, X-dimension, or preflight copy from that text layer alone.

Control Center coverage note: read-only metadata checked on 2026-08-13 for Control Center Page `0:1` exposes `CC / Overview` `2:2` (`1280 x 800`), `CC / Printers — Print Management` `2:37`, and `CC / History` `3:85`. The Overview frame is a research shell for the CC-P1 operations handoff, not a shipped web/LMS design. Route the local queue/recovery/activation slice through [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md); do not copy its LMS seat totals or server claims into WPF without local evidence.

## 3. Contract and behavior

### User-visible states

| State | Trigger | Visible evidence | Safe next action | Failure/empty behavior |
| --- | --- | --- | --- | --- |
| Default | `<!-- initial condition -->` | `<!-- labels, values, status -->` | `<!-- primary action -->` | `<!-- no-data rule -->` |
| Loading | `<!-- action in progress -->` | `<!-- progress/disabled state -->` | `<!-- cancel/retry rule -->` | `<!-- timeout rule -->` |
| Success | `<!-- verified condition -->` | `<!-- durable evidence -->` | `<!-- next action -->` | `<!-- stale rule -->` |
| Error | `<!-- failure condition -->` | `<!-- actionable error -->` | `<!-- repair/relink action -->` | `<!-- fail-closed behavior -->` |

### WPF mapping

| Design region | WPF surface/control | AutomationId or stable name | Data/command owner | Notes |
| --- | --- | --- | --- | --- |
| `<!-- node name -->` | `<!-- XAML/window/control -->` | `<!-- AutomationId -->` | `<!-- ViewModel/service -->` | `<!-- parity constraint -->` |

### Protected behavior check

- [ ] The change does not alter Text/TextBox ownership, sizing, wrapping, clipping, padding, resize lifecycle, overflow, or designer/preview/print parity.
- [ ] Existing authored label geometry and data are preserved; no silent migration is introduced.
- [ ] Any explicitly requested contract change has its decision record and regression gates updated together.

## 4. Measurement and accessibility target

Record the target before implementation; do not accept a visual match that only works at one scale.

| Target | Required value/evidence |
| --- | --- |
| Minimum window | `1024 × 600` (or explain why the slice differs) |
| Display scale | `100%`, `125%`, `150%` (plus observed OS DPI if relevant) |
| Text/icon clipping | `none`; record the screenshot or UI Automation result |
| Keyboard path | `<!-- focus order, shortcut, Escape/Enter behavior -->` |
| Screen-reader/name | `<!-- AutomationId/Name/HelpText expectations -->` |
| Contrast/disabled state | `<!-- state-specific check -->` |
| Scroll behavior | `<!-- one intentional scroll owner; no accidental nesting -->` |

## 5. Evidence package

Figma is a design input. Runtime evidence closes the slice.

- [ ] Figma node review recorded (URL, node ID, state, and measured dimensions).
- [ ] WPF runtime screenshot captured at the target window size and display scales.
- [ ] UI Automation or equivalent stable-control check recorded where applicable.
- [ ] Named application regression added or updated for the user-visible behavior.
- [ ] Unit/contract test added when the change introduces a pure policy or geometry rule.
- [ ] Build and both test commands pass, with counts copied into the owning Markdown checkpoint.
- [ ] Print/preview parity checked when the slice affects label output; otherwise the exclusion is documented.
- [ ] Physical-printer or verifier evidence is explicitly marked open when not available.

Recommended command evidence:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 6. Read-only Figma metadata snapshot

The current Excel-link UI reference was checked through Figma metadata on 2026-08-13. This confirms the design structure and node names only; it does not prove WPF runtime behavior, typography, accessibility, or print parity.

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `22:82` | `Excel Link Verification` | `(4924, 0)` | `620 × 455` |
| `22:5` | `State=Not linked` | `(0, 0)` | `284 × 125` |
| `22:22` | `State=Checking` | `(312, 0)` | `284 × 125` |
| `22:37` | `State=Verified` | `(0, 153)` | `284 × 125` |
| `22:52` | `State=Stale` | `(312, 153)` | `284 × 125` |
| `22:67` | `State=Failed` | `(0, 306)` | `284 × 125` |

When implementing this slice, preserve the five-state model and then attach independent WPF/runtime evidence to each state.

The full shell reference was also checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `2:2` | `ANLAbel — Full Shell v1` | `(80, 80)` | `1440 × 900` |
| `2:3` | `R1 Quick Access` | `(0, 0)` | `1440 × 52` |
| `2:23` | `R2 Ribbon` | `(0, 52)` | `1440 × 90` |
| `2:80` | `R3 Toolbox` | `(12, 12)` | `250 × 271` |
| `2:109` | `R4 Workspace` | `(12, 291)` | `250 × 421` |
| `2:123` | `R5 Design Surface` | `(268, 0)` | `880 × 724` |
| `2:132` | `R6 Object Properties` | `(1148, 0)` | `292 × 724` |
| `2:170` | `R7 Status Bar` | `(0, 866)` | `1440 × 34` |

The shell metadata exposes the intended WPF mapping: `Shell.QuickAccess`, `Shell.Ribbon`, `Shell.Toolbox`, `Shell.Workspace`, `Shell.Canvas`, `Shell.Properties`, and `Shell.Status`. The Figma QA label currently reads `GPL-3.0 · v0.201`; treat that as a design-reference version and reconcile it with the product release snapshot before changing either artifact.

The frequency-first panel reference was checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `8:2` | `ANLAbel — Frequency-first Panels v0.198` | `(3260, 0)` | `664 × 788` |
| `8:4` | `Panel pair` | `(24, 64)` | `616 × 700` |
| `8:5` | `Workspace panel` | `(0, 0)` | `300 × 700` |
| `8:6` | `Properties panel` | `(316, 0)` | `300 × 700` |
| `8:15` | `Workspace tabs` | `(0, 48)` | `300 × 42` |
| `9:2` | `Data tab content` | `(0, 90)` | `300 × 610` |
| `9:45` | `Properties empty state content` | `(0, 48)` | `300 × 652` |

The reference makes `Layers` and `Data` real tabs, with `Data` active, a compact `No data linked` action, collapsed data settings, and binding checks. It also exposes a documentation discrepancy: the Figma reference uses `300 DIP` panels, while `docs/industrial-panel-design.md` currently documents `268 DIP` Workspace and `280 DIP` Properties. Keep this as an open reconciliation item; do not change WPF dimensions from metadata alone without a runtime screenshot and an owning design decision.

The selected-object Properties reference was checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `18:69` | `ANLAbel — Properties tabs v0.200` | `(4504, 0)` | `300 × 700` |
| `18:70` | `Properties header` | `(0, 0)` | `300 × 48` |
| `18:71` | `Selected object summary` | `(0, 48)` | `300 × 68` |
| `18:72` | `Properties tabs` | `(0, 116)` | `300 × 38` |
| `18:73` | `Label tab content` | `(0, 154)` | `300 × 546` |
| `18:94` | `Text Box behavior card` | `(8, 182)` | `284 × 126` |
| `18:95` | `Typography quick card` | `(8, 315)` | `284 × 174` |

The Label tab prioritizes Content, `Wrap in fixed frame`, `Block print and warn`, and typography. The node name for the third tab is `Properties tab / More`, while the current product notes call that tab `Advanced`; keep the naming discrepancy open until the owner chooses the final label. The Text Box behavior controls are contract-sensitive and must not be changed merely to match a visual reference.

The compact selected-object reference was checked through Figma metadata on 2026-08-13:

| Node | Name | Position in parent | Size |
| --- | --- | --- | --- |
| `13:2` | `ANLAbel — Properties selected v0.199` | `(4044, 0)` | `300 × 700` |
| `13:7` | `Selected object content` | `(0, 48)` | `300 × 652` |
| `13:8` | `Selected object summary` | `(12, 12)` | `276 × 62` |
| `15:2` | `Content card` | `(12, 82)` | `276 × 150` |
| `15:16` | `Text Box behavior` | `(12, 240)` | `276 × 137` |
| `15:32` | `Utility · Position and size · collapsed` | `(12, 385)` | `276 × 48` |
| `15:41` | `Utility · Advanced · collapsed` | `(12, 441)` | `276 × 48` |

The selected summary explicitly says `Fixed frame · wraps and clips at bounds`; the behavior card exposes `Auto wrap`, `Print boundary: Clip`, and a fit status. This is direct design evidence for the protected TextBox contract. It also explains the naming evolution: v0.199 uses a collapsed `Advanced` utility section, while v0.200 uses a third tab named `More`. Choose the intended revision before implementation instead of treating the labels as interchangeable.

## 7. Current continuation decisions (2026-08-13)

This table routes the open UI/UX findings already backed by the read-only metadata above. It is not a release claim and does not authorize code or Figma edits by itself.

| Surface | Current status | Evidence | Decision / next owner action |
| --- | --- | --- | --- |
| Excel link verification | Existing reference and implementation evidence | Component `22:82`; five-state contract is recorded in the panel plan and current verification checkpoint | Reuse the existing reference. Any further UI change still needs runtime screenshot/automation evidence for each state. |
| Data Workspace authoring/diagnostics | Needs design review | Figma `8:2`/`9:2` provides the Data-tab shell, empty source `9:3`, current context `9:16`, collapsed settings `9:27` and binding checks `9:35`; no transform editor, sample table, lineage or invalid-state variant is present | Follow [`R4_DATA_WORKSPACE_UI_HANDOFF.md`](R4_DATA_WORKSPACE_UI_HANDOFF.md): reuse the shell only, then approve WPF reuse or name a state-specific node before adding controls. |
| Shell and frequency-first panels | Needs design review | Figma `8:2` reports `300/300 DIP`; current WPF/design note records Workspace `268` and Properties `280` | Keep WPF `268/280` as the working baseline. Do not change widths until an owner decision and target-scale runtime measurement resolve the competing values. |
| Properties third task label | Needs design review | Figma node `18:69` uses `More`; current WPF exposes `Advanced`; the compact reference also uses `Advanced` | Keep `Advanced` as the operator-facing label. Rename only after the owner chooses one label and updates automation/acceptance names together. |
| Database Manager | Needs runtime/design review | Panels Page `0:1` has no Manager frame; current WPF `DatabaseManagerWindow` already has list/detail, Test Connection, Preview, Use, Remove and Cleanup states | Follow [`DATABASE_MANAGER_UI_HANDOFF.md`](DATABASE_MANAGER_UI_HANDOFF.md): approve current WPF information architecture or name a state-specific reference, then close the runtime state matrix. |
| Barcode P3 authoring | Deferred pending a state reference | Page `0:1` metadata scan (2026-08-13) finds only ribbon text layer `1:8`; no barcode Properties/check-digit/HRI state exists. P1/P2 software evidence is closed and P3 is check-digit/HRI display policy. | Use `18:69`/`13:2` as interim shell language only; owner must explicitly approve reuse or provide a state-specific node, then add runtime evidence and regression coverage. |
| CC-P1 Operations Overview | Roadmap; needs product/design review | Control Center `2:2` gives the Overview shell, license/workstation/error card hierarchy and nav; current WPF has `PrintCenterWindow` recovery plus queue/activation/deep-link primitives, but no unified overview | Follow [`CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md`](CC_P1_OPERATIONS_OVERVIEW_UI_HANDOFF.md): choose the host surface and local evidence contract before implementing cards or changing Figma. |
| Text/TextBox behavior | Protected contract | [`AGENTS.md`](../AGENTS.md) and [`NICELABEL_TEXTBOX_RESEARCH.md`](NICELABEL_TEXTBOX_RESEARCH.md) | Do not use a visual reference to alter ownership, sizing, wrapping, clipping, padding, resize lifecycle or print parity without an explicit contract change. |

**Decision rule:** a Figma frame is design input only. The owning slice remains open until the target window/display-scale screenshot or UI Automation measurement, named regression, and relevant build/test evidence are attached. Use [`10-continuation-handoff-2026-08-13.md`](reinvention/10-continuation-handoff-2026-08-13.md) for cross-surface ownership and [`11-verification-checkpoint-2026-08-13.md`](reinvention/11-verification-checkpoint-2026-08-13.md) for the current dirty-worktree boundary.

## 8. Handoff decision

Choose exactly one and explain it:

- [ ] **Ready for implementation** — reference, mapping, states, and acceptance evidence are complete.
- [ ] **Needs design review** — list the unresolved node/state/measurement question.
- [ ] **Blocked by external evidence** — identify the printer, driver, verifier, or workstation evidence required.
- [ ] **Deferred** — link the owning roadmap item and explain why this slice is not next.

**Decision note:** `<!-- one paragraph; no “done” claim without the evidence above -->`
