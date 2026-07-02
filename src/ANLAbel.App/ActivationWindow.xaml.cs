using System.Windows;
using ANLAbel.App.Services;
using ANLAbel.Core.Licensing;

namespace ANLAbel.App;

public partial class ActivationWindow : Window
{
    private readonly TrialLicenseService _licenseService;
    internal ActivationWindow(TrialLicenseService licenseService)
    {
        InitializeComponent();
        _licenseService = licenseService;
        MachineCodeBox.Text = licenseService.MachineCode;
    }

    private void CopyMachine_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(MachineCodeBox.Text);

    private void Activate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = _licenseService.Activate(ActivationKeyBox.Text);
            if (!result.IsValid)
            {
                var message = result.Status switch
                {
                    ActivationValidationStatus.WrongMachine => "Key này được cấp cho máy khác.",
                    ActivationValidationStatus.Expired => "Key kích hoạt đã hết hạn.",
                    _ => "Key không hợp lệ hoặc đã bị thay đổi.",
                };
                MessageBox.Show(message, "Kích hoạt thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("ANLAbel đã được kích hoạt thành công.", "Kích hoạt hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể lưu kích hoạt", MessageBoxButton.OK, MessageBoxImage.Stop); }
    }
}
