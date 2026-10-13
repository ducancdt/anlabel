using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ANLAbel.App.ViewModels;

namespace ANLAbel.App;

public partial class NewTemplateWindow : Window
{
    public NewTemplateRequest? Request { get; private set; }

    public NewTemplateWindow()
    {
        InitializeComponent();
        var ui = UiStrings.Current;
        Title = ui.NewTemplateTitle;
        HeadingText.Text = ui.NewTemplateHeading;
        NameLabel.Text = ui.TemplateName;
        WidthLabel.Text = ui.WidthMm;
        HeightLabel.Text = ui.HeightMm;
        DpiLabel.Text = ui.ResolutionDpi;
        CancelButton.Content = ui.Cancel;
        CreateButton.Content = ui.Create;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadPositiveDouble(WidthBox.Text, out var width) ||
            !TryReadPositiveDouble(HeightBox.Text, out var height) ||
            DpiBox.SelectedItem is not ComboBoxItem dpiItem ||
            !int.TryParse(dpiItem.Content?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dpi))
        {
            MessageBox.Show(this, "Vui lòng nhập chiều rộng, chiều cao và DPI hợp lệ.", "Mẫu tem không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Request = new NewTemplateRequest(string.IsNullOrWhiteSpace(NameBox.Text) ? "Mẫu tem mới" : NameBox.Text.Trim(), width, height, dpi);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static bool TryReadPositiveDouble(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) && value > 0;
    }
}
