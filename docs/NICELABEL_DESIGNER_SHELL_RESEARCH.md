# NiceLabel Designer main-shell research → ANLAbel Figma recreation

**Research date:** 2026-08-11  
**Scope:** Full **Desktop Designer main window shell** (workspace chrome). Not every NiceLabel modal, PowerForms, or Dynamic Data Manager surface.  
**Account used for Figma:** `andd@deltec.com.vn` (plan `An Duong Duc's team`)  
**Path used:** Official Loftware Help + Figma MCP write into a dedicated design file.

## Official NiceLabel / Loftware sources

| Topic | URL |
| --- | --- |
| Workspace Overview (section) | https://help.nicelabel.com/hc/en-001/sections/360005979818-Workspace-Overview |
| Tabs and Ribbons | https://help.nicelabel.com/hc/en-001/articles/4402145576209-Tabs-and-Ribbons |
| Design Surface | https://help.nicelabel.com/hc/en-001/articles/4403726067857-Design-Surface |
| Printer and Status Bar | https://help.nicelabel.com/hc/en-001/articles/4402145575697-Printer-and-Status-Bar |
| Document Properties | https://help.nicelabel.com/hc/en-001/articles/4402145576721-Document-Properties-and-Management-Dialogs |
| Layers Panel | https://help.nicelabel.com/hc/en-001/articles/4402152638993-Layers-Panel |
| Working with Objects | https://help.nicelabel.com/hc/en-001/articles/4402145579537-Working-with-Objects |
| Label Objects (Text / Text Box) | https://help.nicelabel.com/hc/en-001/articles/4402152643729-Label-Objects |

Also: `docs/industrial-panel-design.md`, `docs/NICELABEL_TEXTBOX_RESEARCH.md`.

## NiceLabel main-shell regions (verified)

1. **Tabs + Ribbon (top)** — Tabs group interrelated commands; ribbon is a band of labeled groups (standard Windows UI).
2. **Design surface (center)** — Create/position objects; rulers, grid, snap, zoom; double-click surface → Label Properties; F4 pins Object Properties to the right.
3. **Objects / explorer (left)** — Insert object types; Layers panel (Pro+) for stacking.
4. **Object Properties editor (right)** — Context controls for selection; dialog or pinned dock.
5. **Printer + Status bar (bottom)** — Printer selection, status, zoom; surface size tracks printer driver.
6. **Document management (dialogs)** — Label/form properties, data managers — **deferred** from main-shell frame.

## Region map: NiceLabel → ANLAbel → Figma → code

| # | NiceLabel | ANLAbel | Figma frame name | Code surface |
| --- | --- | --- | --- | --- |
| R1 | Title / quick access | Header + QA buttons | `R1 Quick Access` | `MainWindow.xaml` top DockPanel |
| R2 | Tabs + Ribbon | Compact ribbon groups | `R2 Ribbon` | Ribbon Border under header |
| R3 | Objects panel | Insert objects toolbox | `R3 Toolbox` | Left column top |
| R4 | Layers / explorer | Workspace Layers + Data | `R4 Workspace` | Left TabControl |
| R5 | Design surface | Canvas + rulers | `R5 Design Surface` | `LabelDesignerCanvas` host |
| R6 | Object Properties (F4) | Object Properties panel | `R6 Object Properties` | Right Properties column |
| R7 | Printer + Status bar | Status + printer + zoom | `R7 Status Bar` | Bottom status Border |
| ALL | Full window | Full shell | `ANLAbel — Full Shell v1` | Entire `MainWindow` DockPanel/Grid |

## Gaps vs full NiceLabel product (explicit non-goals)

- Dynamic Data Manager complete UI  
- PowerForms / form designer  
- Multi-layer Layers model (Pro) beyond object list  
- Floating properties dialog animation  
- Full contextual ribbon tabs (Home/Data/View/Design as separate tab strip)  
- Landing page, Automation, branding clone  

## Figma delivery

- File and node IDs are recorded at the bottom of this note after creation.  
- Screenshots exported under implementer scratch as `figma-shell-*.png`.  
- Product font for ANLAbel shell: **Segoe UI** (WPF); Figma uses **Inter** where Segoe is unavailable (same as prior industrial panels).

## Protected contracts

Shell redesign must not alter Text/TextBox industrial layout contracts in `AGENTS.md` (content ownership, wrap/clip, resize lifecycle).

## Figma file (this delivery)

- **URL:** https://www.figma.com/design/zdN71qfzrYV6pPt1b2FRRc/ANLAbel-%E2%80%94-NiceLabel-Shell-Recreation  
- **fileKey:** `zdN71qfzrYV6pPt1b2FRRc`  
- **Full shell node:** `2:2` (`ANLAbel — Full Shell v1`)  
- **Region nodes:** R1 `2:3`, R2 `2:23`, R3 `2:80`, R4 `2:109`, R5 `2:123`, R6 `2:132`, R7 `2:170`  
- **Legend:** `2:176`  
- **Screenshots:** `docs/assets/nicelabel-shell/figma-shell-*.png`  
- **Prior exploration file (panels):** https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5  

## Code delivery

- `MainWindow.xaml` shell AutomationIds: `Shell.QuickAccess`, `Shell.Ribbon`, `Shell.Toolbox`, `Shell.Workspace`, `Shell.Canvas`, `Shell.Properties`, `Shell.Status`  
- Properties header: **Object Properties** (NiceLabel F4 pin editor naming)  
- Status bar: printer label + status + zoom (NiceLabel Printer and Status Bar roles)  
- Regression: `main shell regions match NiceLabel map AutomationIds`  
