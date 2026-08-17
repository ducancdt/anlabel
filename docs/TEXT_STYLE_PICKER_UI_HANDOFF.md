# ANLAbel Excel-like font/size picker — Figma to WPF handoff

Filled instance of [`figma-ui-handoff-template.md`](figma-ui-handoff-template.md).

## 1. Slice identity

| Field | Required value |
| --- | --- |
| Local label outcome | Properties typography: pick font and point size in one compact Excel-like row |
| In scope | Text/TextBox Properties `Text Style` font family, size, Bold/Italic/Underline |
| Out of scope | Text/TextBox frame contract, padding presets, ShrinkFont/ScaleWidth, barcode HRI font, header chrome |
| Related active plan | `docs/reinvention/07-execution-plan.md` §4 L1.4 UI rule |
| Implementation date | `2026-08-15` |

## 2. Exact Figma authority

| Field | Required value |
| --- | --- |
| Figma file URL | https://www.figma.com/design/kqyNBI0DgRHnPzJTDBIui5 |
| Node ID | `26:5` |
| Node name | `ANLAbel — Excel alignment icons v0.251` |
| Created or updated with `@figma` | `Yes` |
| Existing component/tokens reused | Properties file `kqyNBI0DgRHnPzJTDBIui5`; alignment Ready `26:6` (`HorizontalAlignGroup` `26:26`, `VerticalAlignGroup` `26:42`). Font strip `25:5` remains. |

## 3. Required states

| State | Figma node/variant | Runtime source | Primary action |
| --- | --- | --- | --- |
| Empty | N/A — card only shows when Text/TextBox is selected | `SelectedObject.Type` DataTrigger | None |
| Loading/busy | N/A — installed font list is local | `FontFamilies` | None |
| Ready/success | `25:6` State=Ready | Two-way `Style.FontFamily` / `Style.FontSizePt` / Bold/Italic/Underline | Type or pick |
| Stale | N/A | N/A | N/A |
| Blocked/error | Typed size outside 4–200 pt reverts | `TextStylePickerCatalog.TryParseSizePt` | Keep last valid size |
| Disabled/read-only | `24:52` State=Disabled | Whole card collapsed when no text object | Select Text/TextBox |
| Large/long data | `25:25` LicensedFonts | `TextStylePickerCatalog.FilterInstalled` whitelist only | Scroll combo |

## 4. Layout contract

| Requirement | Decision/evidence |
| --- | --- |
| 1024 x 600 effective fit | One 26 px row: font fill + 56 px size + 26 px toggles, inside 300 px Properties |
| 1280 x 720 and 1920 x 1080 behavior | Same row; font combo grows |
| 100/125/150/200% scale intent | 26 px hit targets; no extra caption row |
| One explicit scroll owner | Combo dropdowns only |
| Long text (+40%) wrapping/truncation | Font name truncates in combo; list items preview the family |
| Empty and maximum-data density | Disabled when no text object; catalog is 10 preferred families |
| Keyboard focus order | Font family → size → B → I → U → Align |
| Visible focus and high contrast | Toggle checked uses `#EAF3FF` / `#1464D2` |

## 5. Figma to WPF mapping

| Figma node/component | WPF control/resource | AutomationId | Accessible name | Data/state source |
| --- | --- | --- | --- | --- |
| FontFamilyCombo `25:10` | `ComboBox` `FontToolbarCombo` | `Properties.TextStyle.FontFamily` | Font family | `FilterInstalled` / `Style.FontFamily` |
| FontSizeCombo `25:13` | Editable `ComboBox` + catalog | `Properties.TextStyle.FontSize` | Font size | `FontSizes` / `Style.FontSizePt` |
| StyleIconGroup `25:16` | 24 px segmented B\|I\|U | `Properties.TextStyle.IconGroup` | Style group | Bold/Italic/Underline |
| Icon/Bold `25:17` | `ToggleButton` `FontStyleIcon` | `Properties.TextStyle.Bold` | Bold | `Style.Bold` |
| Icon/Italic `25:19` | `ToggleButton` | `Properties.TextStyle.Italic` | Italic | `Style.Italic` |
| Icon/Underline `25:21` | `ToggleButton` | `Properties.TextStyle.Underline` | Underline | `Style.Underline` |
| HorizontalAlignGroup `26:26` | Segmented radios via `TextStyleAlignmentContract` | `Properties.TextStyle.AlignHorizontal` | Horizontal align | `Style.Alignment` |
| VerticalAlignGroup `26:42` | Segmented radios | `Properties.TextStyle.AlignVertical` | Vertical align | `Style.VerticalAlignment` |

## 6. Runtime evidence

| Automated gate | Result/artifact |
| --- | --- |
| `TextStylePickerCatalogTests` | Typed 9.5 accepted; 3.9 / 201 / non-numeric fail closed |
| `TextStylePickerChromeContractTests` | XAML AutomationIds, editable size, font preview template |
| Fast quality loop | After version bump |
