using System.Windows.Media;

namespace ANLAbel.App.ViewModels;

public sealed class PrintPreviewPageViewModel
{
    public int PageNumber { get; init; }
    public ImageSource PreviewImage { get; init; } = null!;
    public double Width { get; init; }
    public double Height { get; init; }
}
