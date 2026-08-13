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

## 7. Handoff decision

Choose exactly one and explain it:

- [ ] **Ready for implementation** — reference, mapping, states, and acceptance evidence are complete.
- [ ] **Needs design review** — list the unresolved node/state/measurement question.
- [ ] **Blocked by external evidence** — identify the printer, driver, verifier, or workstation evidence required.
- [ ] **Deferred** — link the owning roadmap item and explain why this slice is not next.

**Decision note:** `<!-- one paragraph; no “done” claim without the evidence above -->`
