# ANLAbel Architecture

ANLAbel Phase 1 is a WPF `.NET 8` desktop app using MVVM-style separation.

## Modules
- `ANLAbel.App`: WPF shell, designer canvas control, dialogs and ViewModels.
- `ANLAbel.Core`: template models, object models, enums, mm conversion and binding expression helpers.
- `ANLAbel.Project`: `.anlabel` JSON save/load service.
- `ANLAbel.Data`: reserved for Excel/CSV import in Phase 2.
- `ANLAbel.Barcode`: reserved barcode renderer abstraction for Phase 3.
- `ANLAbel.Printing`: reserved print render plan/pipeline for Phase 4.

## Unit policy
All persisted geometry is stored in millimeters:
- `LabelTemplate.WidthMm`, `HeightMm`, `GapMm`, `MarginMm`.
- `LabelObject.XMm`, `YMm`, `WidthMm`, `HeightMm`.

Screen preview converts mm to WPF device-independent pixels using 96 DPI and current zoom. Printing must not reuse screen preview; Phase 4 will render from the mm model to printer dots using the real printer DPI.

## Phase 1 designer
`LabelDesignerCanvas` is a WPF control responsible for pointer interaction only:
- click selects an object;
- drag moves an object and updates mm coordinates;
- resize grip updates mm size;
- object rendering is rebuilt from model state.

Business data remains in `LabelTemplate` and `LabelObject`.
