using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ANLAbel.App.TemplateLibrary;
using ANLAbel.Core.Models;

namespace ANLAbel.App;

public partial class TemplateLibraryWindow : Window
{
    public sealed class GalleryItem
    {
        public required LibraryTemplateItem Source { get; init; }
        public required ImageSource Thumbnail { get; init; }
        public string Name => Source.Name;
        public string SizeText => Source.SizeText;
        public string TypeText => Source.TypeText;
        public string Group => Source.Group;
    }

    private sealed record FilterDef(string Label, Func<GalleryItem, bool> Match);

    private readonly List<GalleryItem> _all = new();
    private readonly TemplateLibraryService _service;
    private Border? _selectedCard;

    /// <summary>The template the user chose (a fresh editable copy). Null if cancelled.</summary>
    public LabelTemplate? ChosenTemplate { get; private set; }

    public TemplateLibraryWindow(TemplateLibraryService service)
    {
        _service = service;
        InitializeComponent();

        foreach (var item in _service.Items)
        {
            _all.Add(new GalleryItem
            {
                Source = item,
                Thumbnail = LibraryThumbnailRenderer.Render(item.Template, 200, 140)
            });
        }

        BuildFilters();
        FilterList.SelectedIndex = 0;
    }

    private void BuildFilters()
    {
        AddFilterItem($"🗂  Tất cả  ({_all.Count})", _ => true);

        AddSectionHeader("⭐ Mẫu của bạn");
        AddSectionHeader("🏭 Tem tiêu chuẩn");
        const string stdGroup = "Tem công nghiệp";
        var stdCount = _all.Count(i => i.Group == stdGroup);
        if (stdCount > 0)
            AddFilterItem($"📂  Công nghiệp  ({stdCount})", i => i.Group == stdGroup);

        AddSectionHeader("Theo loại");
        foreach (var ty in new[] { "Chỉ chữ", "Mã vạch", "QR / 2D", "Mã vạch + QR" })
        {
            var count = _all.Count(i => i.TypeText == ty);
            if (count > 0)
                AddFilterItem($"🏷  {ty}  ({count})", i => i.TypeText == ty);
        }
    }

    private void AddSectionHeader(string label)
    {
        FilterList.Items.Add(new ListBoxItem
        {
            Content = label,
            IsEnabled = false,
            Padding = new Thickness(8, 8, 8, 2),
            Margin = new Thickness(0, 6, 0, 0),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x23, 0x7A)),
            Background = Brushes.Transparent
        });
    }

    private void AddFilterItem(string label, Func<GalleryItem, bool> match)
    {
        var def = new FilterDef(label, match);
        FilterList.Items.Add(new ListBoxItem { Content = label, Tag = def, Padding = new Thickness(10, 6, 8, 6) });
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterList.SelectedItem is not ListBoxItem { Tag: FilterDef def })
        {
            return;
        }

        var shown = _all.Where(def.Match).ToList();
        GalleryItems.ItemsSource = shown;
        CountText.Text = $"{shown.Count} mẫu";
        ClearSelection();
    }

    private void Card_Hover(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b != _selectedCard)
        {
            b.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#93C5FD")!;
        }
    }

    private void Card_Leave(object sender, MouseEventArgs e)
    {
        if (sender is Border b && b != _selectedCard)
        {
            b.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!;
        }
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border card || card.DataContext is not GalleryItem item)
        {
            return;
        }

        Select(card);

        if (e.ClickCount == 2)
        {
            Confirm(item);
        }
    }

    private void Select(Border card)
    {
        if (_selectedCard is not null)
        {
            _selectedCard.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E2E8F0")!;
            _selectedCard.BorderThickness = new Thickness(1);
        }
        _selectedCard = card;
        card.BorderBrush = (Brush)new BrushConverter().ConvertFromString("#1464D2")!;
        card.BorderThickness = new Thickness(2);
        UseButton.IsEnabled = true;
    }

    private void ClearSelection()
    {
        _selectedCard = null;
        UseButton.IsEnabled = false;
    }

    private void Use_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCard?.DataContext is GalleryItem item)
        {
            Confirm(item);
        }
    }

    private void Confirm(GalleryItem item)
    {
        ChosenTemplate = _service.Materialize(item.Source);
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
