using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace ANLAbel.Data.Excel;

/// <summary>
/// Finds a formula cell that has no calculated value in an .xlsx/.xlsm sheet
/// (CORE DB04). Excel stores the last calculated result of every formula; a
/// workbook written by another program may store only the formula. The reader
/// returns such a cell as empty, which would print a blank field as if it were
/// valid data, so import must stop and say which cell to recalculate.
/// </summary>
internal static class XlsxFormulaValueInspector
{
    private const string OfficeDocumentRelationship = "/officeDocument";

    public readonly record struct FormulaCell(string Reference, int RowNumber, int ColumnNumber);

    /// <summary>
    /// Returns the first formula cell without a cached value at or below
    /// <paramref name="firstRow"/>, or null when there is none or the workbook
    /// layout is not a standard OpenXML package (the main reader decides those).
    /// </summary>
    public static FormulaCell? FindFormulaWithoutValue(string filePath, string sheetName, int firstRow, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var package = new ZipArchive(stream, ZipArchiveMode.Read);
            var sheetPath = FindSheetPart(package, sheetName);
            var entry = sheetPath is null ? null : package.GetEntry(sheetPath);
            if (entry is null)
            {
                return null;
            }

            using var sheet = entry.Open();
            return Scan(sheet, firstRow, cancellationToken);
        }
        catch (Exception exception) when (exception is InvalidDataException or XmlException or IOException)
        {
            return null;
        }
    }

    private static string? FindSheetPart(ZipArchive package, string sheetName)
    {
        var workbookPath = ResolveTarget(string.Empty, ReadRelationships(package, "_rels/.rels")
            .FirstOrDefault(relationship => relationship.Type.EndsWith(OfficeDocumentRelationship, StringComparison.Ordinal)).Target);
        if (string.IsNullOrEmpty(workbookPath) || package.GetEntry(workbookPath) is not { } workbookEntry)
        {
            return null;
        }

        string? relationshipId;
        using (var workbookStream = workbookEntry.Open())
        {
            var workbook = XDocument.Load(workbookStream);
            relationshipId = workbook.Descendants()
                .Where(element => element.Name.LocalName == "sheet")
                .FirstOrDefault(element => string.Equals((string?)element.Attribute("name"), sheetName, StringComparison.OrdinalIgnoreCase))
                ?.Attributes()
                .FirstOrDefault(attribute => attribute.Name.LocalName == "id")
                ?.Value;
        }

        if (relationshipId is null)
        {
            return null;
        }

        var directory = workbookPath.Contains('/') ? workbookPath[..(workbookPath.LastIndexOf('/') + 1)] : string.Empty;
        var relationshipsPath = $"{directory}_rels/{workbookPath[directory.Length..]}.rels";
        var target = ReadRelationships(package, relationshipsPath)
            .FirstOrDefault(relationship => relationship.Id == relationshipId).Target;
        return ResolveTarget(directory, target);
    }

    private static IEnumerable<(string Id, string Type, string Target)> ReadRelationships(ZipArchive package, string path)
    {
        if (package.GetEntry(path) is not { } entry)
        {
            return Array.Empty<(string, string, string)>();
        }

        using var stream = entry.Open();
        return XDocument.Load(stream).Descendants()
            .Where(element => element.Name.LocalName == "Relationship")
            .Select(element => ((string?)element.Attribute("Id") ?? string.Empty,
                                (string?)element.Attribute("Type") ?? string.Empty,
                                (string?)element.Attribute("Target") ?? string.Empty))
            .ToArray();
    }

    private static string ResolveTarget(string directory, string? target)
    {
        if (string.IsNullOrEmpty(target))
        {
            return string.Empty;
        }

        return target.StartsWith('/') ? target[1..] : directory + target;
    }

    private static FormulaCell? Scan(Stream sheet, int firstRow, CancellationToken cancellationToken)
    {
        using var reader = XmlReader.Create(sheet, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        var rowNumber = 0;
        var columnNumber = 0;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            if (reader.LocalName == "row")
            {
                cancellationToken.ThrowIfCancellationRequested();
                rowNumber = int.TryParse(reader.GetAttribute("r"), out var explicitRow) ? explicitRow : rowNumber + 1;
                columnNumber = 0;
                continue;
            }

            if (reader.LocalName != "c")
            {
                continue;
            }

            var reference = reader.GetAttribute("r");
            columnNumber = TryParseColumn(reference, out var explicitColumn) ? explicitColumn : columnNumber + 1;
            if (reader.IsEmptyElement)
            {
                continue;
            }

            var hasFormula = false;
            var hasValue = false;
            var depth = reader.Depth;
            while (reader.Read() && reader.Depth > depth)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    hasFormula |= reader.LocalName == "f";
                    hasValue |= reader.LocalName is "v" or "is";
                }
            }

            if (hasFormula && !hasValue && rowNumber >= firstRow)
            {
                return new FormulaCell(reference ?? $"{DataColumnOrigin.ToColumnLetter(columnNumber)}{rowNumber}", rowNumber, columnNumber);
            }
        }

        return null;
    }

    private static bool TryParseColumn(string? reference, out int column)
    {
        column = 0;
        if (string.IsNullOrEmpty(reference))
        {
            return false;
        }

        foreach (var character in reference)
        {
            if (character is >= 'A' and <= 'Z')
            {
                column = column * 26 + (character - 'A' + 1);
            }
            else
            {
                break;
            }
        }

        return column > 0;
    }
}
