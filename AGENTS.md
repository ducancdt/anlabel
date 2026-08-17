# Repository guardrails

## Protected Text / TextBox contract

The Text/TextBox behavior below is user-approved and must be treated as a protected product contract, not as refactoring latitude.

For any task that does not explicitly request a change to this contract:

- Do not alter Text/TextBox ownership, sizing, wrapping, clipping, padding defaults, resize lifecycle, selection-handle geometry, overflow behavior, designer/print parity, or Properties presets.
- Do not combine `Text` and `TextBox`, migrate one into the other, or share an auto-size path that blurs their distinct purposes.
- Preserve existing edits in the protected files. Avoid cleanup, renaming, formatting, abstraction, or incidental rewrites in these areas.
- If another change appears to require modifying a protected invariant, stop and explain the conflict instead of silently changing the behavior.

Authoritative invariants:

1. `Text` is content-owned and free-flowing. Content may determine its measured bounds; it is not constrained to a TextBox frame. NiceLabel Text cannot hand-edit W/H (size follows font; Font Scaling is a style factor). ANLAbel maps Text border-drag to: lock selection (`TextSizing=FixedFrame` so AutoFit will not re-expand) + compress glyphs into the frame via shared layout scale (horizontal and/or vertical; distortion allowed). Still not TextBox wrap/clip ownership; `ShouldConstrainToBox(Text)` stays false; no overflow Error solely because the selection is tight.
2. `TextBox` is user-frame-owned. Only an explicit user resize or Width/Height edit changes its frame. Content, binding values, and PreviewRow changes never mutate Width/Height.
3. TextBox wraps/reflows inside the authored width and is always clipped at the authored frame in designer, preview, and print.
4. A normal mouse-up commits resize through `Thumb.DragCompleted`; `LostMouseCapture` must not restore the start frame. Dragged width/height must remain after release.
5. New TextBox defaults are label-aware and compact: maximum `32 x 6 mm`, adaptive inset margin `4%` of the short label edge clamped to `0.5-2 mm`, placeholder `Text Box`, font `9 pt`, vertical Center, padding `0.2 mm`, no outline.
6. The default content rectangle must retain at least 90% of a `20 x 6 mm` frame. Padding presets remain `Tight 0`, `Compact 0.2`, and `Comfort 1`.
7. Selection handles retain a `10 DIP` hit target with a `5 DIP` visible marker so compact text remains visible while resize stays usable.
8. Existing authored padding and frame geometry are data. Do not migrate or overwrite them merely because new-object defaults differ.
9. `ShrinkFont` and `ScaleWidth` may change glyph layout only. They never mutate the TextBox frame.
10. Any explicitly requested contract change must update `PLAN.md`, `docs/NICELABEL_TEXTBOX_RESEARCH.md`, and regression coverage in the same change.

Protected implementation areas include:

- `src/ANLAbel.App/ViewModels/MainViewModel.cs` (`AddTextBox` and policy normalization)
- `src/ANLAbel.App/Controls/LabelDesignerCanvas.cs` (Text/TextBox layout and resize lifecycle)
- `src/ANLAbel.App/Controls/SelectionResizeAdorner.cs`
- `src/ANLAbel.App/MainWindow.xaml` and `.xaml.cs` (TextBox Properties controls)
- `src/ANLAbel.Printing/RenderPipeline/TextBoxOverflowDetector.cs`
- `src/ANLAbel.Printing/RenderPipeline/LabelVisualRenderer.cs`
- `src/ANLAbel.Printing/PrinterProfiles/PrintPreflightValidator.cs`

Required verification after an explicitly approved Text/TextBox change:

```powershell
dotnet build ANLAbel.slnx --no-restore
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj --no-build
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj --no-build
```

The custom regression suite must continue to pass these named gates:

- `Text stays free while TextBox stays bounded`
- `Text shrink-frame compresses glyphs`
- `text box does not resize object from text content`
- `text box reflows to fit frame when user resizes`
- `normal resize capture release does not cancel gesture`
- `new text box uses compact label-aware frame`
- `designer preview row keeps object geometry`

See `PLAN.md` under `Text / TextBox industrial contract` and `docs/NICELABEL_TEXTBOX_RESEARCH.md` for the decision record and research basis.
