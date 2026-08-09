# Contributing to ANLAbel

Thank you for helping improve ANLAbel. Contributions that improve industrial label-printing reliability, printer compatibility, data handling, accessibility, documentation, and test coverage are welcome.

Participation in this project is governed by the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before you start

1. Search existing issues and pull requests to avoid duplicate work.
2. Open an issue before starting a large behavioral or architectural change.
3. Do not include customer names, part numbers, production data, activation keys, credentials, or proprietary label templates.
4. Keep sample templates generic and disconnected from private Excel files.

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
