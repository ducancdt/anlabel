using System.Text.Json;
using System.Text.Json.Serialization;
using ANLAbel.Core.Enums;
using ANLAbel.Core.Models;

namespace ANLAbel.Core.Printing;

/// <summary>
/// Owns the boundary between an open authored label and mutable settings used
/// by one print-preview session. A preview must never edit the authored model
/// through a shared reference. Persistence remains an explicit editor action.
/// </summary>
public static class PrintSessionTemplateContract
{
    private static readonly JsonSerializerOptions SnapshotOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static LabelTemplate Capture(LabelTemplate source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var json = JsonSerializer.Serialize(source, SnapshotOptions);
        return JsonSerializer.Deserialize<LabelTemplate>(json, SnapshotOptions)
            ?? throw new InvalidOperationException("Could not create an isolated print-session snapshot.");
    }

    /// <summary>
    /// Commits print-owned configuration only. Objects, bindings, guides,
    /// transforms, identity and database configuration remain authored data
    /// and are deliberately excluded from this operation.
    /// </summary>
    public static void CommitPrintConfiguration(LabelTemplate session, LabelTemplate authored)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(authored);

        authored.WidthMm = session.WidthMm;
        authored.HeightMm = session.HeightMm;
        authored.Orientation = session.Orientation;
        authored.Dpi = session.Dpi;
        authored.GapMm = session.GapMm;
        authored.MarginMm = session.MarginMm;

        var source = session.PrinterProfile;
        var target = authored.PrinterProfile;
        target.PrinterName = source.PrinterName;
        target.PaperName = source.PaperName;
        target.LabelStockName = source.LabelStockName;
        target.SettingsSource = source.SettingsSource;
        target.PaperSizeSource = source.SettingsSource == PrinterSettingsSource.Driver
            ? PaperSizeSource.DriverAutomatic
            : source.PaperSizeSource;
        target.MediaType = source.MediaType;
        target.FeedDirection = source.FeedDirection;
        target.Rotated180 = source.Rotated180;
        target.Dpi = source.Dpi;
        target.LabelWidthMm = source.LabelWidthMm;
        target.LabelHeightMm = source.LabelHeightMm;
        target.GapMm = source.GapMm;
        target.OffsetXMm = source.OffsetXMm;
        target.OffsetYMm = source.OffsetYMm;
        target.ScaleX = source.ScaleX;
        target.ScaleY = source.ScaleY;
        target.PhysicalWidthMm = source.PhysicalWidthMm;
        target.PhysicalHeightMm = source.PhysicalHeightMm;
        target.MediaProvenance = source.MediaProvenance;
        target.DriverMediaIdentity = source.DriverMediaIdentity;
        target.DriverIdentity = source.DriverIdentity;
        target.PrintMethod = source.PrintMethod;
    }
}
