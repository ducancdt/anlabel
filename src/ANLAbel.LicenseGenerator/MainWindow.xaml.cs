using System.Security.Cryptography;
using System.Windows;
using ANLAbel.Core.Licensing;

namespace ANLAbel.LicenseGenerator;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void ExpiryChanged(object sender, RoutedEventArgs e)
    {
        if (ExpiryPicker is not null) ExpiryPicker.IsEnabled = PermanentBox.IsChecked != true;
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var machine = ActivationLicense.NormalizeMachineId(MachineCodeBox.Text);
            if (machine.Length != 64) throw new InvalidOperationException("Mã máy không hợp lệ. Hãy sao chép nguyên mã từ ANLAbel Trial.");
            DateTimeOffset? expiry = null;
            if (PermanentBox.IsChecked != true)
            {
                if (ExpiryPicker.SelectedDate is not DateTime date) throw new InvalidOperationException("Hãy chọn ngày hết hạn.");
                expiry = new DateTimeOffset(date.Date.AddDays(1), TimeZoneInfo.Local.GetUtcOffset(date.Date.AddDays(1))).ToUniversalTime();
            }

            KeyBox.Text = MasterLicenseSigner.Create(machine, CustomerBox.Text, expiry);
            Clipboard.SetText(KeyBox.Text);
            StatusText.Text = "Đã tạo và sao chép key.";
        }
        catch (CryptographicException)
        {
            MessageBox.Show("Master key chỉ sử dụng được bằng tài khoản Windows đã tạo nó.", "Không mở được Master key", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Không thể tạo key", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(KeyBox.Text)) Clipboard.SetText(KeyBox.Text);
    }
}
