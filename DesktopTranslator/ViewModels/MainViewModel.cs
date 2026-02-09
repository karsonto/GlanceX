using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopTranslator.Models;
using DesktopTranslator.Services;
using DesktopTranslator.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace DesktopTranslator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly TranslationController _controller;
    private TranslationOverlay? _overlay;

    [ObservableProperty] private bool _isTranslating;
    [ObservableProperty] private bool _hasRegion;
    [ObservableProperty] private string _regionInfo = "未选择";
    [ObservableProperty] private string _statusText = "就绪";
    [ObservableProperty] private string _connectionStatus = "未连接";
    [ObservableProperty] private int _translationCount;
    [ObservableProperty] private string _lastTranslationTime = "--:--:--";
    [ObservableProperty] private int _captureInterval = 3;
    [ObservableProperty] private string _selectedSourceLanguage = "English";
    [ObservableProperty] private string _selectedTargetLanguage = "中文";

    public ObservableCollection<string> SourceLanguages { get; } = new(AppSettings.SupportedSourceLanguages);
    public ObservableCollection<string> TargetLanguages { get; } = new(AppSettings.SupportedTargetLanguages);

    public TranslationController Controller => _controller;

    public MainViewModel(SettingsService settingsService, TranslationController controller)
    {
        _settingsService = settingsService;
        _controller = controller;

        // Load saved settings
        var s = _settingsService.Settings;
        CaptureInterval = s.CaptureIntervalSeconds;
        SelectedSourceLanguage = s.SourceLanguage;
        SelectedTargetLanguage = s.TargetLanguage;
        HasRegion = s.HasRegion;
        if (s.HasRegion)
        {
            RegionInfo = $"{s.RegionWidth} × {s.RegionHeight} px";
        }

        // Wire up controller events
        _controller.TranslationStarted += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusText = "翻译中...";
                _overlay?.ShowLoading();
            });
        };

        _controller.TranslationCompleted += text =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.ShowResult(text);
                StatusText = "翻译完成";
                LastTranslationTime = DateTime.Now.ToString("HH:mm:ss");
            });
        };

        _controller.TranslationChunkReceived += chunk =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.ResetErrorState();
                _overlay?.AppendStreamText(chunk);
            });
        };

        _controller.TranslationStreamFinished += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.FinishStream();
                StatusText = "翻译完成";
                LastTranslationTime = DateTime.Now.ToString("HH:mm:ss");
            });
        };

        _controller.TranslationError += error =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.ShowError(error);
                StatusText = $"错误: {error}";
            });
        };

        _controller.TranslationCountChanged += count =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                TranslationCount = count;
            });
        };

        // Check API connection status
        CheckConnectionStatus();
    }

    private async void CheckConnectionStatus()
    {
        var s = _settingsService.Settings;
        if (string.IsNullOrWhiteSpace(s.ApiKey))
        {
            ConnectionStatus = "未配置 API";
            return;
        }

        ConnectionStatus = "检测中...";
        try
        {
            using var service = new TranslationService(s.ApiKey, s.BaseUrl, s.Model, s.SystemPrompt, s.Temperature);
            var error = await service.TestConnectionAsync();
            ConnectionStatus = error == null ? "已连接" : "连接失败";
        }
        catch
        {
            ConnectionStatus = "连接失败";
        }
    }

    public void SetOverlay(TranslationOverlay overlay)
    {
        _overlay = overlay;

        // Restore overlay position
        var s = _settingsService.Settings;
        if (s.OverlayWidth > 0 && s.OverlayHeight > 0)
        {
            _overlay.RestoreWindowState(s.OverlayLeft, s.OverlayTop, s.OverlayWidth, s.OverlayHeight);
        }
    }

    [RelayCommand]
    private void SelectRegion()
    {
        var selector = new RegionSelector();
        var result = selector.ShowDialog();

        if (result == true && selector.SelectedRegion.HasValue)
        {
            var region = selector.SelectedRegion.Value;
            _settingsService.UpdateSettings(s =>
            {
                s.RegionX = region.X;
                s.RegionY = region.Y;
                s.RegionWidth = region.Width;
                s.RegionHeight = region.Height;
            });

            HasRegion = true;
            RegionInfo = $"{region.Width} × {region.Height} px";
            StatusText = "区域已选择，可以开始翻译";
        }
    }

    [RelayCommand]
    private async Task ToggleTranslation()
    {
        if (IsTranslating)
        {
            _controller.Stop();
            IsTranslating = false;
            StatusText = "已停止";
        }
        else
        {
            // Save current language settings
            _settingsService.UpdateSettings(s =>
            {
                s.SourceLanguage = SelectedSourceLanguage;
                s.TargetLanguage = SelectedTargetLanguage;
                s.CaptureIntervalSeconds = CaptureInterval;
            });

            // Show overlay
            if (_overlay != null)
            {
                _overlay.Show();
            }

            IsTranslating = true;
            StatusText = "正在启动翻译...";

            await _controller.StartAsync();

            // When StartAsync returns, translation has stopped
            IsTranslating = false;
        }
    }

    [RelayCommand]
    private async Task TranslateOnce()
    {
        _settingsService.UpdateSettings(s =>
        {
            s.SourceLanguage = SelectedSourceLanguage;
            s.TargetLanguage = SelectedTargetLanguage;
        });

        if (_overlay != null)
        {
            _overlay.Show();
        }

        await _controller.TranslateOnceAsync();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_settingsService)
        {
            Owner = Application.Current.MainWindow
        };

        if (settingsWindow.ShowDialog() == true)
        {
            StatusText = "设置已保存";
            CheckConnectionStatus();
        }
    }

    [RelayCommand]
    private void ShowOverlay()
    {
        _overlay?.Show();
    }

    public void SaveOverlayState()
    {
        if (_overlay != null)
        {
            var state = _overlay.GetWindowState();
            _settingsService.UpdateSettings(s =>
            {
                s.OverlayLeft = state.Left;
                s.OverlayTop = state.Top;
                s.OverlayWidth = state.Width;
                s.OverlayHeight = state.Height;
            });
        }
    }

    partial void OnCaptureIntervalChanged(int value)
    {
        _settingsService.UpdateSettings(s => s.CaptureIntervalSeconds = value);
    }

    partial void OnSelectedSourceLanguageChanged(string value)
    {
        _settingsService.UpdateSettings(s =>
        {
            s.SourceLanguage = value;
            s.OcrLanguage = AppSettings.OcrLanguageMap.GetValueOrDefault(value, "en-US");
        });
    }

    partial void OnSelectedTargetLanguageChanged(string value)
    {
        _settingsService.UpdateSettings(s => s.TargetLanguage = value);
    }
}
