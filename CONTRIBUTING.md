# Contributing to ANLAbel

Thank you for helping improve ANLAbel. Contributions that improve industrial label-printing reliability, printer compatibility, data handling, accessibility, documentation, and test coverage are welcome.

Participation in this project is governed by the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before you start

1. Search existing issues and pull requests to avoid duplicate work.
2. Open an issue before starting a large behavioral or architectural change.
3. Do not include customer names, part numbers, production data, activation keys, credentials, or proprietary label templates.
4. Keep sample templates generic and disconnected from private Excel files.

## Documentation and UI/UX contributions

- Start with the [roadmap](ROADMAP.md) and the [continuation handoff](docs/reinvention/10-continuation-handoff-2026-08-13.md) so a new note does not duplicate an active plan.
- Keep historical Markdown entries intact. Add a dated checkpoint, named command/test evidence, explicit open items, and links to the document that owns the decision.
- When a plan and implementation disagree, record the disagreement as open until a clean checkpoint supplies fresh evidence; do not silently turn an uncommitted change into a release claim.
- For a UI/UX proposal, identify the existing Figma file and node before creating anything new. Record the file URL, node/frame identifier, target window/display scale, mapped WPF surface or `AutomationId`, and the runtime screenshot or measurement that will validate it.
- Treat Figma as a design reference, not as runtime acceptance. A UI change still needs WPF behavior checks, regression coverage where applicable, and an explicit note when physical-printer evidence is outside the slice.
- Preserve the protected Text/TextBox behavior contract. A requested contract change must update its decision record and regression gates together; incidental layout cleanup is not a valid reason to reopen it.

## Development setup

ANLAbel targets .NET 8 and Windows WPF.

```powershell
dotnet restore ANLAbel.slnx
dotnet build ANLAbel.slnx -c Release --no-restore
```

Run both test suites before submitting a pull request:

```powershell
dotnet run --project src/ANLAbel.Tests/ANLAbel.Tests.csproj -c Release
dotnet test src/ANLAbel.UnitTests/ANLAbel.UnitTests.csproj -c Release
```

## Pull requests

- Keep each pull request focused on one coherent change.
- Explain the user impact, implementation approach, and validation performed.
- Add regression coverage for fixes and behavior changes where practical.
- Preserve compatibility with existing `.anlabel` templates unless a migration is documented.
- Do not perform slow Excel, network-share, or printer-driver I/O synchronously on the WPF UI thread.
- Do not mutate label geometry from rendering or UI refresh paths.

## Industrial printer reports

For printer-specific issues, include as much of the following as possible:

- Printer manufacturer and model.
- Windows driver and driver version.
- Printer DPI (for example 203, 300, or 600 DPI).
- Label width, height, gap/mark type, and orientation.
- Whether the problem appears in Designer, Print Preview, physical output, or all three.
- A minimal template and sample data with confidential information removed.

Physical-printer results are especially valuable because driver behavior differs between Zebra, TSC, Godex, SATO, Argox, Honeywell, Intermec, Citizen, Toshiba TEC, and Seagull/BarTender drivers.

## Questions and security

- Use [GitHub Discussions](https://github.com/ducancdt/anlabel/discussions) for setup questions and early ideas.
- Follow [SECURITY.md](SECURITY.md) when reporting a suspected vulnerability; do not disclose it in a public issue.

## Licensing contributions

By submitting a contribution, you agree that your contribution will be licensed under GPL-3.0-only, the same license as the project. Only submit work that you have the right to license.
