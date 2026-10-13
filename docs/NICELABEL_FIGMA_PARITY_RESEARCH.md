# NiceLabel 2019 & Figma Shell Parity Research

> Reference status (2026-09-15): competitor research, not a full implementation
> requirement or parity backlog. Apply [reference policy](REFERENCE_POLICY.md)
> and [core standard](CORE_SOFTWARE_STANDARD.md) to select ANLAbel core behavior.

## 1. Overview & Research Basis

This document records the visual and structural alignment between:
1. **NiceLabel 2019 PowerForms Reference Catalog** (`images/catalog.json` and 294 figures extracted from `ug-NiceLabel_2019_PowerForms_User_Guide-en.pdf`).
2. **Figma Recreation File** (`zdN71qfzrYV6pPt1b2FRRc` "ANLAbel — NiceLabel Shell Recreation") queried via Figma MCP.
3. **ANLAbel Desktop WPF Client** (`src/ANLAbel.App/`).

---

## 2. Shell Structure & Token Alignment

### R1 Quick Access Header
- **NiceLabel Reference:** `fig_p026_01_workspace_overview.png`, `fig_p052_01_home_tab.png`.
- **Figma Model:** Node `12:3` (`R1 Quick Access`), `layout_903e90c0`, fill `#063B66`, border `#173B72`, height `52px`.
- **ANLAbel Implementation:** `src/ANLAbel.App/MainWindow.xaml`
  - Header background: `#063B66`
  - Border brush: `#173B72`
  - NiceLabel Ribbon Tab Strip recreation: `File Home Data View Solution Help Design Text` in `#0C4A7E` container with `#3B7CCB` stroke and `#FFFFFF` text.
  - Document status badge: Printer name and paper dimensions pills.

### R3 Object Toolbox
- **NiceLabel Reference:** `fig_p026_01_workspace_overview.png` shows object tools:
  - `Text` (Standard content-owned text)
  - `Text Box` (Bounded text frame)
  - `Bar Code`
  - `Picture`
  - `Rectangle`, `Line`, `Ellipse`
- **Figma Model:** Node `12:77` (`R3 Toolbox`) with 8 tool items.
- **ANLAbel Implementation:**
  - Standard text button updated from `Static Text` to `Text` with tooltip `Text (single line, content-owned)`.
  - Full adherence to `AGENTS.md` Text / TextBox industrial contract.

### Print Preview Window
- **NiceLabel Reference:** `fig_p206_01_print_pane_default_printing_form.png` (Default printing form backstage view with live preview, quantity inputs, and printer preflight status).
- **Figma Model:** Node `17:26` (`ANLAbel — Print Preview Basic Reliability v1`)
  - Top header: `#1464D2`, title "Print Preview — Shipping Label", subtitle "Excel rows · barcode preflight · one label per feed", Trust badge "Ready — verified" (`#CFF7D3` background, `#02542D` text).
  - Preview canvas area: `#EEF2F6` neutral background.
  - Label preview card: White background, `#CBD5E1` border, drop shadow `0px 4px 14px rgba(15, 23, 42, 0.18)` (`BlurRadius=14, ShadowDepth=4, Opacity=0.18, Color=#0F172A`).
  - Right settings pane: Header `#1464D2`, printer contract card, preflight badge.
- **ANLAbel Implementation:** `src/ANLAbel.App/PrintPreviewWindow.xaml`
  - Top header styled to `#1464D2` with title, subtitle, and trust badge pill.
  - `PreviewScrollViewer` background set to `#EEF2F6`.
  - Preview card border styled with `DropShadowEffect` and `#CBD5E1` border.
  - Settings panel header aligned to `#1464D2`.

---

## 3. Invariants & Contract Verification

1. **Text / TextBox Invariants:**
   - Text remains content-owned and free-flowing.
   - TextBox remains user-frame-owned, wrapped and clipped to authored bounds.
   - All named gates in `src/ANLAbel.Tests/` pass cleanly without regression.

2. **Test Gates Verified:**
   - `dotnet build ANLAbel.slnx`: 0 Warnings, 0 Errors.
   - `dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj`: 774 passed, 0 failed.
   - `dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj`: 100% passed (exit code 0).
