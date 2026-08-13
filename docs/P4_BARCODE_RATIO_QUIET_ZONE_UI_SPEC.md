# ANLAbel — P4 barcode ratio, density and physical quiet-zone UI/UX specification

**Status:** documentation-only, pre-implementation UI/UX contract (2026-08-13)
**Execution spine:** [`INDUSTRIAL_BARCODE_EXECUTION_PLAN.md`](INDUSTRIAL_BARCODE_EXECUTION_PLAN.md) §P4
**Handoff:** [`P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md`](P4_BARCODE_RATIO_QUIET_ZONE_UI_HANDOFF.md)
**Research gap:** [`BARCODE_NICELABEL_BARTENDER_RESEARCH.md`](BARCODE_NICELABEL_BARTENDER_RESEARCH.md) M3/M4/M14
**Owner decision packet:** [`P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md`](P4_BARCODE_RATIO_QUIET_ZONE_DECISION_PACKET.md)
**Figma boundary:** selected-Properties language from `18:69` / `13:2`; no P4-specific frame is present

This document maps P4 to a safe operator surface: ratio is an authored per-symbology policy, density is a derived readout, and quiet-zone width is reported in physical millimetres from the effective print-DPI X-dimension. It does not add model fields, edit Figma, change barcode rendering or claim P4 complete.

## 1. Operator outcome

The operator should be able to distinguish three different things in the Barcode Properties card:

1. the legal ratio policy for the selected linear standard;
2. the effective physical X-dimension and its derived density;
3. the observed/required quiet-zone width in millimetres.

The surface must make clear that only an explicit `SizedFromX` policy owns production width. Ratio and density must not create a second automatic frame-sizing path.

## 2. Existing UI and Figma evidence

The current barcode card in [`MainWindow.xaml`](../src/ANLAbel.App/MainWindow.xaml#L1878) already groups Standard, Application profile, Quiet zone (modules), HRI placement, HRI size, X-dimension, `SizedFromX`, effective-module readout and validation. P4 proposes a small linear-only extension to that card.

The read-only Figma panels file `kqyNBI0DgRHnPzJTDBIui5` has no barcode-specific Properties frame. Existing references are:

| Node | Measured evidence | P4 use | Boundary |
| --- | --- | --- | --- |
| `18:69` | Selected Properties tabs, `300 × 700`; content cards are `284 DIP` | Grouping/card spacing and compact two-column control language | No ratio or quiet-zone state |
| `13:2` | Selected Properties shell, `300 × 700`; content card is `276 DIP` | Dense readout/status treatment | No barcode semantics |
| `1:8` | Ribbon text `Text TextBox Image Barcode` | Navigation vocabulary only | Not a Properties design |

**Routing:** reuse the shell only after owner approval; keep the current WPF barcode card as the working baseline. A Figma frame is not a runtime or physical measurement artifact.

## 3. Proposed physical/data contract

| Field/readout | Meaning | Source of truth | Mutation |
| --- | --- | --- | --- |
| Ratio policy | Legal per-symbology wide/narrow ratio, initially proposed for Code 39 | Future typed Core/renderer contract | Explicit user choice; legacy-safe default |
| Effective X | Whole-dot quantized module width at the print plan DPI | `LinearBarcodeModuleContract.Resolve` | Authored X or legacy frame estimate according to existing rules |
| Derived density | Human-readable presentation of effective X/ratio/symbol structure | Same resolution + renderer metadata | Read-only; never an independent size input |
| Quiet-zone modules | Authored logical margin | Existing barcode application/render options | Explicit user choice, no automatic migration |
| Physical quiet zone | `quietZoneModules × effectiveModuleWidthMm` (with the contract’s side/total convention) | One shared Core/Printing contract | Read-only; warning/repair only |
| Required minimum | Industrial/profile-specific threshold | Existing application-profile policy or future typed policy | Must name profile/source; no invented certification |
| Status | Supported, legacy/unresolved, valid, invalid, below-minimum, not-applicable | Shared validator result | Must preserve exact reason and severity |

The owner must decide whether the displayed quiet-zone value is per side or total. The implementation cannot use both conventions in different surfaces.

## 4. Host-neutral wireframe

```text
[Barcode]
[Standard] [Application profile]
[Quiet zone (modules)] [HRI placement]

-- Linear sizing (only when applicable) --
[Ratio: 2.0:1 ▼] [Density: derived / read-only]
[X-dim (mm): 0.33] [Size width from X × modules: □]
[Effective: 0.33 mm · 13.0 mil · 4 dots @ 300 DPI]
[Quiet zone: 10 modules = 3.30 mm per side]

[Validation / warning]
```

For QR/Data Matrix/Aztec/PDF417, the ratio/density group is hidden or disabled with an accessible explanation. The existing square-module controls remain the owner of 2D geometry.

## 5. UI state matrix

| State | Required controls/readout | Safe action | No-claim rule |
| --- | --- | --- | --- |
| Linear standard without ratio support | Explain `Ratio not applicable`; show current X/QZ/effective readout | Edit supported fields or choose another standard | Do not show a no-op ratio selector |
| Supported ratio, valid X | Legal ratio selector; density derived; effective X/dots/DPI and QZ mm | Change ratio or authored X explicitly | No automatic frame mutation unless `SizedFromX` is checked |
| Supported ratio, legacy X = 0 | Legacy/frame-owned badge; density/QZ physical value marked unresolved or estimated per approved contract | Set X or keep legacy | Do not call estimate a verified physical measurement |
| Illegal/unsupported ratio | Error/status with legal values and selected symbology | Choose a legal ratio | Block preview/print; no silent clamp |
| QZ above minimum | Observed value, required value/profile and neutral status | Continue normal preflight | Software readout is not verifier grade |
| QZ below minimum | Observed mm/modules, required threshold, severity and repair hint | Increase QZ/X or deliberately change profile | No silent shrink, fallback or “Print anyway” |
| GS1 profile | Profile name, QZ basis and application validation state | Repair geometry/data | Never claim full GS1 certification |
| Ratio changed with `FrameOwned` | Derived symbol geometry changes; authored frame remains unchanged | Inspect preview and accept explicit geometry | Do not auto-resize authored object |
| Ratio changed with `SizedFromX` | Production width resolves from effective X × logical modules under existing policy | Inspect width/readout and preview | Keep legacy/default behavior isolated |

## 6. Interaction and persistence rules

1. Changing Standard refreshes ratio applicability before the user can apply an invalid value.
2. Changing Ratio invalidates the current derived readout and recomputes it from the same payload, X resolution, DPI and renderer metadata used by preflight.
3. Changing X or printer profile refreshes density and physical QZ together; stale values are not displayed as current.
4. Changing quiet-zone modules changes the physical-QZ readout and validation only; it does not mutate object width or HRI layout.
5. Legacy files preserve authored width, modules, HRI placement and all existing object geometry. New P4 fields use explicit safe defaults.
6. Preview, print and designer diagnostics consume the same resolved ratio/X/QZ result. A UI-only approximation is not acceptable.

## 7. Proposed AutomationIds and accessibility

These IDs are proposals until the owner approves the host and runtime implementation:

| Region/control | Proposed `AutomationId` | Accessible name |
| --- | --- | --- |
| Linear sizing group | `Barcode.Properties.LinearSizing` | `Linear barcode sizing` |
| Ratio selector | `Barcode.Properties.Ratio` | `Wide narrow ratio` |
| Density readout | `Barcode.Properties.Density` | `Derived barcode density` |
| X-dimension | `Barcode.Properties.XDimension` | `X dimension in millimetres` |
| Effective readout | `Barcode.Properties.EffectiveModule` | `Effective module at print DPI` |
| Quiet-zone modules | `Barcode.Properties.QuietZoneModules` | `Quiet zone modules` |
| Physical QZ readout | `Barcode.Properties.QuietZonePhysical` | `Physical quiet zone` |
| QZ status | `Barcode.Properties.QuietZoneStatus` | `Quiet zone validation` |
| Repair guidance | `Barcode.Properties.QuietZoneRepair` | `Quiet zone repair guidance` |

Keyboard order should remain Standard → Application profile → Quiet zone/HRI → Ratio → X/effective readout → physical-QZ status → validation. Disabled controls must explain why they are not applicable.

## 8. Responsive/runtime evidence

| Target | Layout behavior | Required evidence |
| --- | --- | --- |
| `1280 × 800` | Two-column ratio/density and X/QZ readouts may remain compact; long warning wraps within the card | Supported ratio + QZ-valid screenshot/UI Automation |
| `1024 × 600` | Stack ratio/density and physical-QZ lines; keep below-minimum reason and repair action visible | Unsupported, invalid-ratio and low-QZ states |
| `100%`, `125%`, `150%` | Reflow within the existing Properties column; no blind `300 DIP` scaling or clipping | Record scale, focus, wrap and measured card bounds |

Runtime evidence must include a controlled payload before/after ratio change and a preflight message whose physical-QZ value matches the readout. Figma metadata alone cannot prove this.

## 9. Acceptance gates for implementation

P4 may be marked implemented only when:

1. at least one optional-ratio symbology has legal/illegal policy fixtures;
2. ratio changes controlled vector/module or production-width evidence;
3. density is derived and has no independent mutation path;
4. physical QZ is computed from the same effective X resolution as preflight at print DPI;
5. GS1/application-profile low-QZ behavior is explicit and fail-closed according to approved severity;
6. save/load/clone/document snapshot preserve new P4 fields and legacy files remain unchanged;
7. P0/P1/P2 barcode gates, QR/Data Matrix gates and protected Text/TextBox gates remain green;
8. designer/preview/print parity and target-scale runtime evidence are captured;
9. no physical verifier, native-printer or certification claim is added without external evidence.

Suggested verification remains:

```powershell
dotnet build ANLAbel.slnx --no-restore --nologo -v quiet -p:UseSharedCompilation=false -nodeReuse:false
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build --nologo -v quiet
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

## 10. Explicit non-goals

- ratio controls for every barcode type;
- density as a third size driver;
- automatic migration or resizing of legacy frames;
- native printer quiet-zone commands, bearer bars, UPC split digits or full catalog parity;
- physical verifier grade or GS1 certification;
- a new Figma frame solely to satisfy this document;
- any Text/TextBox ownership, sizing, wrapping, clipping, padding, resize or print-contract change.

Until the owner confirms the first symbology, ratio/value convention, QZ side/total convention, warning threshold and runtime evidence owner, P4 remains a UI/UX specification.
