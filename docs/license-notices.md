# License notices

**ANLAbel - Label Designer**
Created by **Duc An**
Email: ducancdt@gmail.com

ANLAbel is distributed under GPL-3.0. This file records the direct package dependencies visible in the repository at the current source checkpoint; the project files remain the version source of truth.

## Direct runtime dependencies

| Package | Version | Used by | License | Project / license source |
| --- | ---: | --- | --- | --- |
| ClosedXML | 0.105.0 | ANLAbel.Data Excel workbook operations | MIT | [project](https://github.com/ClosedXML/ClosedXML) · [NuGet license](https://licenses.nuget.org/MIT) |
| ExcelDataReader | 3.7.0 | ANLAbel.Data XLS/XLSX reading path | MIT | [project](https://github.com/ExcelDataReader/ExcelDataReader) · [NuGet license](https://licenses.nuget.org/MIT) |
| System.Text.Encoding.CodePages | 8.0.0 | ANLAbel.Data legacy code-page decoding | MIT | [dotnet/runtime](https://github.com/dotnet/runtime) · [NuGet license](https://licenses.nuget.org/MIT) |
| ZXing.Net | 0.16.11 | ANLAbel.Barcode barcode rendering/validation | Apache-2.0 | [project](https://github.com/micjahn/ZXing.Net) · [NuGet license](https://licenses.nuget.org/Apache-2.0) |

The package versions above were checked against the current csproj files and local NuGet nuspec metadata on 2026-08-13. Update this table in the same change as any PackageReference version change.

## Notices that require packaging review

- ClosedXML brings transitive packages including ClosedXML.Parser, DocumentFormat.OpenXml, ExcelNumberFormat, RBush.Signed, SixLabors.Fonts, and other framework packages. Generate the final transitive inventory from the restored assets/lock output for each release; do not assume the direct-package table is exhaustive.
- System.Text.Encoding.CodePages includes a THIRD-PARTY-NOTICES file covering additional .NET/runtime and data-license notices. Preserve the package notice when redistributing the dependency.
- ZXing.Net is Apache-2.0; retain the Apache notice and any required attribution when distributing the barcode assembly.
- ClosedXML and ExcelDataReader are MIT; retain their copyright/license text in the release notice bundle.
- The .NET/WPF platform libraries and Windows printer APIs are platform/runtime components, not vendored source dependencies in this repository. Record the target runtime prerequisites separately from NuGet notices.

## Development-only packages

The xUnit, Microsoft.NET.Test.Sdk, coverlet collector, and xunit.runner.visualstudio references in ANLAbel.UnitTests support verification and are not product runtime features. Keep their licenses in the development/compliance inventory if test binaries or SDK caches are redistributed; they do not belong in the end-user feature list.

## Before publishing a release

1. Compare this file with every PackageReference in the solution.
2. Restore/package from a clean checkout and enumerate direct plus transitive assets.
3. Preserve each package's license, copyright, NOTICE, and THIRD-PARTY-NOTICES files as required by that license.
4. Confirm installer payloads do not accidentally include development-only test assemblies or raw package caches.
5. Attach the final dependency inventory to the owning release checkpoint; a passing build alone is not a license audit.
