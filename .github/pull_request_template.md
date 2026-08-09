## Summary

Describe the change and the user or maintainer problem it solves.

## Validation

- [ ] `dotnet build ANLAbel.slnx -c Release --no-restore`
- [ ] Application regression tests
- [ ] xUnit tests
- [ ] Physical industrial-printer test, when printer behavior changes
- [ ] Documentation updated where needed

## Printer impact

List affected printer models, drivers, DPI, label sizes, orientation, and calibration behavior, or write `None`.

## Data safety

- [ ] Samples and screenshots contain no customer names, production data, credentials, private Excel paths, or proprietary templates.
- [ ] New library templates use generic placeholders and do not auto-link an Excel file.

## Compatibility

Describe any effect on existing `.anlabel` templates or write `None`.
