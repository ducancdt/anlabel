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

## 6. Handoff decision

Choose exactly one and explain it:

- [ ] **Ready for implementation** — reference, mapping, states, and acceptance evidence are complete.
- [ ] **Needs design review** — list the unresolved node/state/measurement question.
- [ ] **Blocked by external evidence** — identify the printer, driver, verifier, or workstation evidence required.
- [ ] **Deferred** — link the owning roadmap item and explain why this slice is not next.

**Decision note:** `<!-- one paragraph; no “done” claim without the evidence above -->`
