using System.IO;

namespace ANLAbel.App.ViewModels;

/// <summary>
/// The template file was changed by another ANLAbel window or PC after this
/// session opened or saved it. The save did not write anything.
/// </summary>
public sealed class DocumentChangedOnDiskException : IOException
{
    public DocumentChangedOnDiskException(string filePath)
        : base($"The template file was saved elsewhere after it was opened here: {filePath}")
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}
