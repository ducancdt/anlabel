using System.IO;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;
using ANLAbel.Project.SaveLoad;
using Xunit;

namespace ANLAbel.UnitTests;

public sealed class ProjectFileServiceTests
{
    private readonly ProjectFileService _sut = new();

    [Fact]
    public async Task LoadAsync_EmptyObject_ThrowsInvalidDataException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            await File.WriteAllTextAsync(tempFile, "{}");
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _sut.LoadAsync(tempFile));
            Assert.Contains("schemaVersion is missing", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_SchemaVersionString_ThrowsInvalidDataException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            await File.WriteAllTextAsync(tempFile, "{\"schemaVersion\": \"2\"}");
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _sut.LoadAsync(tempFile));
            Assert.Contains("schemaVersion is invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_SchemaVersionDecimal_ThrowsInvalidDataException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            await File.WriteAllTextAsync(tempFile, "{\"schemaVersion\": 2.5}");
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _sut.LoadAsync(tempFile));
            Assert.Contains("schemaVersion is invalid", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_PartialLegacySignature_ThrowsInvalidDataException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            // Contains only part of the legacy signature (missing HeightMm, Dpi, Objects)
            var json = """
            {
                "Id": "test-id",
                "Name": "Partial Label",
                "WidthMm": 50.0
            }
            """;
            await File.WriteAllTextAsync(tempFile, json);
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _sut.LoadAsync(tempFile));
            Assert.Contains("schemaVersion is missing and payload is not a recognized legacy template", exception.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_FutureSchemaVersion_ThrowsInvalidDataExceptionWithUnsupportedSchemaInner()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            var futureJson = $$"""
            {
                "format": "{{ProjectFileService.FileFormat}}",
                "schemaVersion": {{ProjectFileService.CurrentSchemaVersion + 1}},
                "template": {}
            }
            """;
            await File.WriteAllTextAsync(tempFile, futureJson);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => _sut.LoadAsync(tempFile));
            Assert.IsType<UnsupportedProjectSchemaException>(exception.InnerException);
            Assert.Contains($"schema version {ProjectFileService.CurrentSchemaVersion + 1}", exception.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadWithRecoveryAsync_FutureSchemaVersion_ThrowsUnsupportedProjectSchemaExceptionDirectly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            var futureJson = $$"""
            {
                "format": "{{ProjectFileService.FileFormat}}",
                "schemaVersion": {{ProjectFileService.CurrentSchemaVersion + 1}},
                "template": {}
            }
            """;
            await File.WriteAllTextAsync(tempFile, futureJson);

            var exception = await Assert.ThrowsAsync<UnsupportedProjectSchemaException>(() => _sut.LoadWithRecoveryAsync(tempFile));
            Assert.Contains($"schema version {ProjectFileService.CurrentSchemaVersion + 1}", exception.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadAsync_ValidRawLegacyTemplate_LoadsAndPreservesValues()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            var rawLegacyJson = """
            {
                "Id": "legacy-id",
                "Name": "Legacy Label",
                "WidthMm": 55.5,
                "HeightMm": 22.25,
                "Dpi": 300,
                "Objects": []
            }
            """;
            await File.WriteAllTextAsync(tempFile, rawLegacyJson);

            var template = await _sut.LoadAsync(tempFile);

            Assert.NotNull(template);
            Assert.Equal("legacy-id", template.Id);
            Assert.Equal("Legacy Label", template.Name);
            Assert.Equal(55.5, template.WidthMm);
            Assert.Equal(22.25, template.HeightMm);
            Assert.Equal(300, template.Dpi);
            Assert.Empty(template.Objects);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_PreservesGeometryFontBindingAndBarcodeOptions()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            var template = new LabelTemplate
            {
                WidthMm = 76.25,
                HeightMm = 31.5
            };

            var obj = new LabelObject
            {
                Type = ObjectType.BarcodeCode128,
                XMm = 3.25,
                YMm = 4.5,
                WidthMm = 42,
                HeightMm = 12,
                Rotation = 90,
                BindingExpression = "part-number token",
                BarcodeSymbology = BarcodeSymbology.Code39,
                BarcodeApplicationProfile = BarcodeApplicationProfile.Gs1,
                BarcodeHriPlacement = BarcodeHriPlacement.Above,
                BarcodeTextFontSizePt = 8.5,
                BarcodeModuleWidthMm = 0.33,
                BarcodeWidthMode = BarcodeWidthMode.SizedFromX,
                BarcodeCheckDigitPolicy = BarcodeCheckDigitPolicy.Verify,
                BarcodeHriShowCheckDigit = false,
                Style = new ObjectStyle
                {
                    FontFamily = "Bahnschrift",
                    FontSizePt = 13
                }
            };
            template.Objects.Add(obj);

            await _sut.SaveAsync(template, tempFile);
            var loaded = await _sut.LoadAsync(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal(76.25, loaded.WidthMm);
            Assert.Equal(31.5, loaded.HeightMm);
            Assert.Single(loaded.Objects);

            var loadedObj = loaded.Objects[0];
            Assert.Equal(ObjectType.BarcodeCode128, loadedObj.Type);
            Assert.Equal(3.25, loadedObj.XMm);
            Assert.Equal(4.5, loadedObj.YMm);
            Assert.Equal(42, loadedObj.WidthMm);
            Assert.Equal(12, loadedObj.HeightMm);
            Assert.Equal(90, loadedObj.Rotation);
            Assert.Equal("part-number token", loadedObj.BindingExpression);
            Assert.Equal(BarcodeSymbology.Code39, loadedObj.BarcodeSymbology);
            Assert.Equal(BarcodeApplicationProfile.Gs1, loadedObj.BarcodeApplicationProfile);
            Assert.Equal(BarcodeHriPlacement.Above, loadedObj.BarcodeHriPlacement);
            Assert.Equal(8.5, loadedObj.BarcodeTextFontSizePt);
            Assert.Equal(0.33, loadedObj.BarcodeModuleWidthMm);
            Assert.Equal(BarcodeWidthMode.SizedFromX, loadedObj.BarcodeWidthMode);
            Assert.Equal(BarcodeCheckDigitPolicy.Verify, loadedObj.BarcodeCheckDigitPolicy);
            Assert.False(loadedObj.BarcodeHriShowCheckDigit);
            Assert.Equal("Bahnschrift", loadedObj.Style.FontFamily);
            Assert.Equal(13, loadedObj.Style.FontSizePt);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task RoundTrip_PrinterProfile_DriverSettingsSource_NormalizesToDriverAutomatic()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.anlabel");
        try
        {
            var template = new LabelTemplate
            {
                Name = "Driver Settings Test",
                WidthMm = 80,
                HeightMm = 40
            };
            template.PrinterProfile.PrinterName = "Zebra ZD420";
            template.PrinterProfile.SettingsSource = PrinterSettingsSource.Driver;
            // PaperSizeSource is normalized to DriverAutomatic by the contract
            Assert.Equal(PaperSizeSource.DriverAutomatic, template.PrinterProfile.PaperSizeSource);

            await _sut.SaveAsync(template, tempFile);
            var loaded = await _sut.LoadAsync(tempFile);

            Assert.NotNull(loaded);
            Assert.Equal(PrinterSettingsSource.Driver, loaded.PrinterProfile.SettingsSource);
            Assert.Equal(PaperSizeSource.DriverAutomatic, loaded.PrinterProfile.PaperSizeSource);
            Assert.Equal("Zebra ZD420", loaded.PrinterProfile.PrinterName);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
