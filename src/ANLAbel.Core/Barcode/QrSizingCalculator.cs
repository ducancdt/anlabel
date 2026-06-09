namespace ANLAbel.Core.Barcode;

public sealed class QrSizingCalculator
{
    private readonly IQrCapacityProvider _capacityProvider;

    public QrSizingCalculator()
        : this(new QrCapacityTable())
    {
    }

    public QrSizingCalculator(IQrCapacityProvider capacityProvider)
    {
        _capacityProvider = capacityProvider ?? throw new ArgumentNullException(nameof(capacityProvider));
    }

    public QrSizingResult Calculate(string data, BarcodeObjectBounds currentBounds, QrBarcodeOptions options)
    {
        var validationError = ValidateOptions(options);
        if (validationError is not null)
        {
            return QrSizingResult.Invalid(validationError);
        }

        return options.SizingMode switch
        {
            QrSizingMode.AutoSizeByData => CalculateAutoSize(data, options),
            QrSizingMode.FixedVersionAndModuleSize => CalculateFixedVersion(data, options),
            _ => QrSizingResult.Invalid("Unsupported QR sizing mode.")
        };
    }

    private QrSizingResult CalculateAutoSize(string data, QrBarcodeOptions options)
    {
        for (var version = QrVersionHelper.MinVersion; version <= QrVersionHelper.MaxVersion; version++)
        {
            if (_capacityProvider.CanEncodeByteMode(data, version, options.ErrorCorrection))
            {
                return CreateValidResult(version, options);
            }
        }

        return QrSizingResult.Invalid("Data is too long for QR version 40.");
    }

    private QrSizingResult CalculateFixedVersion(string data, QrBarcodeOptions options)
    {
        if (options.FixedVersion is null || !QrVersionHelper.IsValidVersion(options.FixedVersion.Value))
        {
            return QrSizingResult.Invalid("FixedVersion must be from 1 to 40.");
        }

        var version = options.FixedVersion.Value;
        if (!_capacityProvider.CanEncodeByteMode(data, version, options.ErrorCorrection))
        {
            return QrSizingResult.Invalid("Data is too long for selected QR version.");
        }

        return CreateValidResult(version, options);
    }

    private static QrSizingResult CreateValidResult(int version, QrBarcodeOptions options)
    {
        var moduleCount = QrVersionHelper.GetModuleCount(version);
        var totalModules = moduleCount + options.QuietZoneModules * 2;
        var finalSizePx = totalModules * options.ModuleSizePx;
        var finalSizeMm = finalSizePx / (double)options.Dpi * 25.4;

        return new QrSizingResult
        {
            IsValid = true,
            Version = version,
            ModuleCount = moduleCount,
            TotalModules = totalModules,
            ModuleSizePx = options.ModuleSizePx,
            FinalSizePx = finalSizePx,
            FinalSizeMm = finalSizeMm,
            WidthMm = finalSizeMm,
            HeightMm = finalSizeMm
        };
    }

    private static string? ValidateOptions(QrBarcodeOptions options)
    {
        if (options.ModuleSizePx <= 0)
        {
            return "ModuleSizePx must be greater than 0.";
        }

        if (options.Dpi <= 0)
        {
            return "Dpi must be greater than 0.";
        }

        if (options.QuietZoneModules < 0)
        {
            return "QuietZoneModules must be greater than or equal to 0.";
        }

        return null;
    }
}