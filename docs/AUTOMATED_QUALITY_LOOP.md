# Quality loop

Prove the shipped local label app still works. That is the only quality claim.

A **90% mutation score** on the configured label-safety files is the blocking
proxy. It is not a physical print certificate.

Current public version `0.259`. Thresholds stay `high/low/break = 90`. New
files join the mutate list only after the combined score stays at least 90.

```text
change → build → unit → application regressions
      → mutation of label-safety contracts (when needed)
      → score ≥ 90 ? keep : strengthen tests and rerun
```

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Fast
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelQualityLoop.ps1 -Mode Mutation
```

Reports stay local and gitignored. Text/TextBox named gates must keep passing.
