# Quality gates

Use [`../AUTOMATED_QUALITY_LOOP.md`](../AUTOMATED_QUALITY_LOOP.md).

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Mutation
```

- Fast: zero-warning build, unit tests, application regressions, version
  projections, Text/TextBox named gates.
- Mutation: label-safety list, threshold 90. Do not lower it.
- Markdown links in active docs must resolve.
- No customer data in shipped templates.
