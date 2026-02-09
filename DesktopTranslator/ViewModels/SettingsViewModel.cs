using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopTranslator.Services;

namespace DesktopTranslator.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _baseUrl = "https://api.openai.com/v1";
    [ObservableProperty] private string _model = "gpt-4o-mini";
    [ObservableProperty] private double _temperature = 0.3;
    [ObservableProperty] private string _systemPrompt = "";
    [ObservableProperty] private string _testResult = "";
    [ObservableProperty] private bool _isTesting;
    [ObservableProperty] private bool _testSuccess;
    [ObservableProperty] private bool _showApiKey;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _settingsService.Settings;
        ApiKey = s.ApiKey;
        BaseUrl = s.BaseUrl;
        Model = s.Model;
        Temperature = s.Temperature;
        SystemPrompt = s.SystemPrompt;
    }

    [RelayCommand]
    private void Save()
    {
        _settingsService.UpdateSettings(s =>
        {
            s.ApiKey = ApiKey;
            s.BaseUrl = BaseUrl;
            s.Model = Model;
            s.Temperature = Temperature;
            s.SystemPrompt = SystemPrompt;
        });
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        IsTesting = true;
        TestResult = "";
        TestSuccess = false;

        try
        {
            using var service = new TranslationService(ApiKey, BaseUrl, Model, SystemPrompt, Temperature);
            var error = await service.TestConnectionAsync();

            if (error == null)
            {
                TestResult = "连接成功！API 可正常使用。";
                TestSuccess = true;
            }
            else
            {
                TestResult = $"连接失败：{error}";
                TestSuccess = false;
            }
        }
        catch (Exception ex)
        {
            TestResult = $"测试出错：{ex.Message}";
            TestSuccess = false;
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void ToggleApiKeyVisibility()
    {
        ShowApiKey = !ShowApiKey;
    }
}
