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

            var usageText = result.Payload?.ExpiresUtc is { } expiresUtc
                ? $"Thời hạn sử dụng: còn {Math.Max(0, (int)Math.Ceiling((expiresUtc - DateTimeOffset.UtcNow).TotalDays))} ngày (hết hạn {expiresUtc.ToLocalTime():dd/MM/yyyy})."
                : "Thời hạn sử dụng: vĩnh viễn (không giới hạn thời gian).";
            MessageBox.Show($"ANLAbel đã được kích hoạt thành công.\n\n{usageText}", "Kích hoạt hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể lưu kích hoạt", MessageBoxButton.OK, MessageBoxImage.Stop); }
    }
}
