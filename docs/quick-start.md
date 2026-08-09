# ANLAbel quick start

This guide covers the shortest safe path from installation to a physical industrial-label print.

## 1. Install

Download the latest Windows x64 installer from [GitHub Releases](https://github.com/ducancdt/anlabel/releases/latest). The installer is currently unsigned, so Windows SmartScreen may ask you to confirm that you trust the download.

## 2. Prepare the printer

Install the Windows driver supplied by your printer manufacturer or integrator. In the driver preferences, confirm:

- physical label width and height;
- portrait or landscape orientation;
- gap, black-mark, or continuous-media mode;
- printer resolution, normally 203, 300, or 600 DPI;
- calibration and sensor settings on the printer itself.

ANLAbel is designed for industrial label printers such as Zebra, TSC, Godex, SATO, Argox, Honeywell, Intermec, Citizen, and Toshiba TEC devices.

## 3. Create a label

Start with a blank label or a generic built-in template. Set the physical label dimensions, then add text, images, shapes, and barcode objects from the tool panel.

Keep important content inside the printable area. Printer drivers may reserve small non-printable margins.

## 4. Connect Excel data

Open the Data Source Manager, choose an Excel workbook and sheet, preview the header row, and bind label objects to the required columns. Templates do not ship with a fixed Excel path; each user links their own data source.

Avoid moving or renaming the workbook after linking it. If the source changes, relink it before printing.

## 5. Preview and print

Select the target printer and confirm its DPI. Open Print Preview and resolve preflight warnings before sending a batch.

For the first physical test:

1. Print one label only.
2. Measure the output with a ruler or caliper.
3. Confirm barcode readability with the intended scanner.
4. Adjust calibration or print offset only after verifying the driver paper size and orientation.

## Getting help

Use the [bug-report form](https://github.com/ducancdt/anlabel/issues/new?template=bug_report.yml) for reproducible defects. Include the printer model, driver version, DPI, media size, orientation, and whether the issue appears in Designer, Print Preview, physical output, or all three.

Do not upload production data, customer names, credentials, or proprietary label templates.
