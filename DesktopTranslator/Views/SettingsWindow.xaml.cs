using System.Windows;
using System.Windows.Media;
using DesktopTranslator.Services;
using DesktopTranslator.ViewModels;

namespace DesktopTranslator.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private bool _apiKeyVisible;

    public SettingsWindow(SettingsService settingsService)
    {
        InitializeComponent();
        _viewModel = new SettingsViewModel(settingsService);
        DataContext = _viewModel;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set the PasswordBox value (cannot bind PasswordBox directly)
        PwdApiKey.Password = _viewModel.ApiKey;
    }

    private void PwdApiKey_PasswordChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.ApiKey = PwdApiKey.Password;
    }

    private void ToggleApiKeyVisibility_Click(object sender, RoutedEventArgs e)
    {
        _apiKeyVisible = !_apiKeyVisible;

        if (_apiKeyVisible)
        {
            PwdApiKey.Visibility = Visibility.Collapsed;
            TxtApiKeyVisible.Visibility = Visibility.Visible;
            TxtApiKeyVisible.Text = _viewModel.ApiKey;
            EyeIcon.Text = "\uED1A"; // Eye off icon
        }
        else
        {
            TxtApiKeyVisible.Visibility = Visibility.Collapsed;
            PwdApiKey.Visibility = Visibility.Visible;
            PwdApiKey.Password = _viewModel.ApiKey;
            EyeIcon.Text = "\uE7B3"; // Eye icon
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        BtnTest.Content = "测试中...";
        BtnTest.IsEnabled = false;
        TxtTestResult.Foreground = (Brush)FindResource("TextSecondaryBrush");

        await _viewModel.TestConnectionCommand.ExecuteAsync(null);

        BtnTest.Content = "测试连接";
        BtnTest.IsEnabled = true;

        TxtTestResult.Foreground = _viewModel.TestSuccess
            ? (Brush)FindResource("SuccessBrush")
            : (Brush)FindResource("ErrorBrush");
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Ensure API key from passwordbox is synced
        if (PwdApiKey.Visibility == Visibility.Visible)
        {
            _viewModel.ApiKey = PwdApiKey.Password;
        }

        _viewModel.SaveCommand.Execute(null);
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
