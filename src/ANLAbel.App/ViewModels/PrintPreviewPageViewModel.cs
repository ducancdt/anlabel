using System.Windows.Media;
using ANLAbel.Core.Printing;

namespace ANLAbel.App.ViewModels;

public sealed class PrintPreviewPageViewModel
{
    public static List<PrintPreviewPageViewModel> CreateMetadata(int pageCount, double width, double height)
    {
        if (pageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        }

        var pages = new List<PrintPreviewPageViewModel>(pageCount);
        for (var index = 0; index < pageCount; index++)
        {
            pages.Add(new PrintPreviewPageViewModel
            {
                PageNumber = index + 1,
                Width = width,
                Height = height
            });
        }

        return pages;
    }

    public int PageNumber { get; init; }
    public ImageSource? PreviewImage { get; set; }
    /// <summary>
    /// Exact preview device-frame identity. This is evidence for the preview
    /// bitmap only; it never substitutes for driver or physical-output proof.
    /// </summary>
    public RasterGoldenIdentity? PreviewRasterIdentity { get; set; }
    public double Width { get; init; }
    public double Height { get; init; }
}
