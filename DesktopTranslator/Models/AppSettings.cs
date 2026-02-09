namespace DesktopTranslator.Models;

public class AppSettings
{
    // API Configuration
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.3;
    public string SystemPrompt { get; set; } = "You are a professional translator. Translate the following text to {target_language}. Only output the translation, nothing else. Preserve the original formatting.";

    // Translation Settings
    public string SourceLanguage { get; set; } = "English";
    public string TargetLanguage { get; set; } = "中文";
    public string OcrLanguage { get; set; } = "en-US";
    public int CaptureIntervalSeconds { get; set; } = 3;

    // Hotkeys
    public string HotkeySelectRegion { get; set; } = "Ctrl+Alt+S";
    public string HotkeyToggleTranslation { get; set; } = "Ctrl+Alt+T";

    // Window State
    public double OverlayLeft { get; set; } = 100;
    public double OverlayTop { get; set; } = 100;
    public double OverlayWidth { get; set; } = 400;
    public double OverlayHeight { get; set; } = 300;
    public bool OverlayPinned { get; set; } = false;

    // Selected Region (physical pixels)
    public int RegionX { get; set; }
    public int RegionY { get; set; }
    public int RegionWidth { get; set; }
    public int RegionHeight { get; set; }
    public bool HasRegion => RegionWidth > 0 && RegionHeight > 0;

    public static readonly string[] SupportedSourceLanguages =
    [
        "English", "中文", "日本語", "한국어", "Français", "Deutsch", "Español", "Русский", "Português", "العربية"
    ];

    public static readonly string[] SupportedTargetLanguages =
    [
        "中文", "English", "日本語", "한국어", "Français", "Deutsch", "Español", "Русский", "Português", "العربية"
    ];

    public static readonly Dictionary<string, string> OcrLanguageMap = new()
    {
        ["English"] = "en-US",
        ["中文"] = "zh-CN",
        ["日本語"] = "ja",
        ["한국어"] = "ko",
        ["Français"] = "fr-FR",
        ["Deutsch"] = "de-DE",
        ["Español"] = "es-ES",
        ["Русский"] = "ru-RU",
        ["Português"] = "pt-BR",
        ["العربية"] = "ar-SA"
    };
}
