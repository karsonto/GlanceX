using System.Windows;
using DesktopTranslator.Services;

namespace DesktopTranslator;

public partial class App : Application
{
    private HotkeyService? _hotkeyService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handling
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show(
                $"发生未处理的错误：\n\n{args.Exception.Message}",
                "桌面翻译器 - 错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"发生严重错误：\n\n{ex.Message}",
                    "桌面翻译器 - 严重错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        base.OnExit(e);
    }

    public void SetHotkeyService(HotkeyService service)
    {
        _hotkeyService = service;
    }
}
