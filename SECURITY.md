# Security policy

## Supported versions

Security fixes are applied to the latest source on the default branch and, when practical, to the newest published release.

## Reporting a vulnerability

Please do not open a public issue for a suspected vulnerability.

Email `ducancdt@gmail.com` with:

- a concise description of the issue and its impact;
- affected version or commit;
- reproducible steps or a minimal proof of concept;
- suggested mitigations, if known;
- whether any details have already been disclosed elsewhere.

Do not include production data, credentials, private Excel workbooks, customer templates, or personal information. Use synthetic data whenever possible.

You should receive an acknowledgement within seven days. The maintainer will validate the report, coordinate a fix and release where appropriate, and agree on disclosure timing with the reporter.

## Scope

Useful reports include unsafe file handling, path traversal, insecure deserialization, formula or spreadsheet-related injection, dependency vulnerabilities with a demonstrated impact, and issues that could cause unintended printing or disclosure of label data.
