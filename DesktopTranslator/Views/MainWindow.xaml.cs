using System.Windows;
using System.Windows.Media;
using DesktopTranslator.Services;
using DesktopTranslator.ViewModels;

namespace DesktopTranslator.Views;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly TranslationController _controller;
    private readonly HotkeyService _hotkeyService;
    private readonly MainViewModel _viewModel;
    private readonly TranslationOverlay _overlay;
    private readonly MiniWidget _miniWidget;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize services
        _settingsService = new SettingsService();
        _controller = new TranslationController(_settingsService);
        _hotkeyService = new HotkeyService();

        // Create overlay
        _overlay = new TranslationOverlay();

        // Create mini widget
        _miniWidget = new MiniWidget();
        _miniWidget.RestoreRequested += () => Dispatcher.Invoke(ShowAndActivate);

        // Create and set ViewModel
        _viewModel = new MainViewModel(_settingsService, _controller);
        _viewModel.SetOverlay(_overlay);
        DataContext = _viewModel;

        // Register with App for cleanup
        if (Application.Current is App app)
        {
            app.SetHotkeyService(_hotkeyService);
        }

        // Subscribe to IsTranslating changes for UI updates
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsTranslating))
            {
                UpdateToggleButton(_viewModel.IsTranslating);
                UpdateTrayMenu(_viewModel.IsTranslating);
                UpdateWidgetBreathing(_viewModel.IsTranslating);
            }
            else if (e.PropertyName == nameof(MainViewModel.ConnectionStatus))
            {
                UpdateConnectionDot(_viewModel.ConnectionStatus);
            }
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize hotkeys after window handle is available
        _hotkeyService.Initialize(this);
        _hotkeyService.SelectRegionPressed += () =>
        {
            Dispatcher.Invoke(() => _viewModel.SelectRegionCommand.Execute(null));
        };
        _hotkeyService.ToggleTranslationPressed += () =>
        {
            Dispatcher.Invoke(() => _viewModel.ToggleTranslationCommand.Execute(null));
        };

        // Set initial states
        UpdateConnectionDot(_viewModel.ConnectionStatus);

        // Generate tray icon programmatically
        CreateTrayIcon();
    }

    private void CreateTrayIcon()
    {
        // Create a simple icon using DrawingImage
        var drawingGroup = new DrawingGroup();
        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            null,
            new EllipseGeometry(new Point(8, 8), 8, 8)));
        drawingGroup.Children.Add(new GeometryDrawing(
            Brushes.White,
            null,
            Geometry.Parse("M 4,5 L 4,11 L 6,11 L 6,9 L 8,11 L 10.5,11 L 7.5,8 L 10,5 L 7.5,5 L 6,7 L 6,5 Z")));

        var drawingImage = new DrawingImage(drawingGroup);
        drawingImage.Freeze();

        // Convert to a System.Drawing.Icon for the tray
        // Use a simple approach - render to bitmap
        try
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawImage(drawingImage, new Rect(0, 0, 16, 16));
            }

            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            // Use WPF's GeneratedIcon approach - just set tooltip
            // The TaskbarIcon control will use the application icon
        }
        catch
        {
            // Tray icon generation failed, not critical
        }
    }

    private void UpdateToggleButton(bool isTranslating)
    {
        Dispatcher.Invoke(() =>
        {
            ToggleIcon.Text = isTranslating ? "\uE71A" : "\uE768"; // Stop / Play
            ToggleText.Text = isTranslating ? "停止翻译" : "开始翻译";
        });
    }

    private void UpdateTrayMenu(bool isTranslating)
    {
        Dispatcher.Invoke(() =>
        {
            TrayToggleItem.Header = isTranslating ? "停止翻译" : "开始翻译";
        });
    }

    private void UpdateWidgetBreathing(bool isTranslating)
    {
        Dispatcher.Invoke(() =>
        {
            if (isTranslating)
                _miniWidget.StartBreathing();
            else
                _miniWidget.StopBreathing();
        });
    }

    private void UpdateConnectionDot(string status)
    {
        Dispatcher.Invoke(() =>
        {
            StatusDot.Fill = status switch
            {
                "已连接" => (Brush)FindResource("SuccessBrush"),
                "连接失败" or "未配置 API" => (Brush)FindResource("ErrorBrush"),
                "检测中..." => (Brush)FindResource("WarningBrush"),
                _ => (Brush)FindResource("TextTertiaryBrush")
            };
        });
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.OpenSettingsCommand.Execute(null);
    }

    // ==================== Window Events ====================

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
            _miniWidget.Show();
            return;
        }

        // Actually closing
        _viewModel.SaveOverlayState();
        _controller.Stop();
        _controller.Dispose();
        _hotkeyService.Dispose();
        _overlay.Close();
        _miniWidget.Close();
        TrayIcon.Dispose();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _miniWidget.Show();
        }
    }

    // ==================== Tray Icon Events ====================

    private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void ShowMainWindow_Click(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void TraySelectRegion_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectRegionCommand.Execute(null);
    }

    private void TrayToggleTranslation_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleTranslationCommand.Execute(null);
    }

    private void TraySettings_Click(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
        _viewModel.OpenSettingsCommand.Execute(null);
    }

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        _isExiting = true;
        Application.Current.Shutdown();
    }

    private void ShowAndActivate()
    {
        _miniWidget.Hide();
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}
