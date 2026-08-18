# ANLAbel designer chrome icons — Figma to WPF handoff

Filled instance of [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md).

## 1. Slice identity

| Field | Required value |
| --- | --- |
| Local label outcome | Designer chrome icons share one ink color and stay readable at 16 DIP |
| In scope | `src/ANLAbel.App/Icons/*.png` already referenced by the designer shell |
| Out of scope | Control Center, new commands, Text/TextBox contract, Properties declutter |
| Related active plan | `docs/reinvention/07-execution-plan.md` Boundary (Figma before UI) |
| Implementation date | `2026-08-16` |

## 2. Exact Figma authority

| Field | Required value |
| --- | --- |
| Figma file URL | https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-Shell?node-id=10-2 |
| Node ID | `10:2` |
| Node name | `ANLAbel — Designer icons v0.267` |
| Created or updated with `@figma` | `Yes` (sheet in existing shell file). MCP export later required local rasterization to the same contract. |
| Existing component/tokens reused | Shell file `zdN71qfzrYV6pPt1b2FRRc`; ink `#1B4F8A` |

## 3. Required states

| State | Figma node/variant | Runtime source | Primary action |
| --- | --- | --- | --- |
| Empty | N/A — chrome always present | `MainWindow` Image sources | None |
| Ready/success | `10:2` grid tiles | `Icons/{command}.png` | File / data / print / toolbox |
| Disabled/read-only | Same glyph, WPF command `CanExecute` | Existing commands | None |
| Other | N/A | N/A | N/A |

## 4. Layout contract

| Requirement | Decision/evidence |
| --- | --- |
| Smallest shipped raster | `SmallIconImage` 16×16; tabs 14×14; ribbon 20×20 |
| Stroke | 2px on 24px artboard (~5px on 64px export) so 16 DIP stays a closed mark |
| Color | One ink `#1B4F8A` |
| Mapping | Existing filenames only; no new AutomationIds |

## 5. Evidence

`designer header commands are unique` plus `designer icon sources exist on disk`. Fast after version bump.
