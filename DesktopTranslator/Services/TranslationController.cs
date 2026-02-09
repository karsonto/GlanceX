using System.Drawing;
using DesktopTranslator.Models;

namespace DesktopTranslator.Services;

public class TranslationController : IDisposable
{
    private readonly ScreenCaptureService _captureService;
    private readonly OcrService _ocrService;
    private TranslationService? _translationService;
    private readonly SettingsService _settingsService;

    private CancellationTokenSource? _cts;
    private string _lastOcrText = "";
    private bool _isRunning;
    private bool _disposed;

    public event Action? TranslationStarted;
    public event Action<string>? TranslationCompleted;
    public event Action<string>? TranslationChunkReceived;
    public event Action? TranslationStreamFinished;
    public event Action<string>? TranslationError;
    public event Action<string>? OcrTextRecognized;
    public event Action<int>? TranslationCountChanged;

    public bool IsRunning => _isRunning;
    public int TranslationCount { get; private set; }

    public TranslationController(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _captureService = new ScreenCaptureService();
        _ocrService = new OcrService();
    }

    /// <summary>
    /// Ensure the translation service is initialized / updated with current settings.
    /// </summary>
    private void EnsureTranslationService()
    {
        var s = _settingsService.Settings;
        if (_translationService == null)
        {
            _translationService = new TranslationService(s.ApiKey, s.BaseUrl, s.Model, s.SystemPrompt, s.Temperature);
        }
        else
        {
            _translationService.UpdateConfig(s.ApiKey, s.BaseUrl, s.Model, s.SystemPrompt, s.Temperature);
        }
    }

    /// <summary>
    /// Start the real-time translation loop.
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning) return;

        var settings = _settingsService.Settings;
        if (!settings.HasRegion)
        {
            TranslationError?.Invoke("请先选择翻译区域。");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            TranslationError?.Invoke("请先在设置中配置 API Key。");
            return;
        }

        // Initialize OCR engine
        var ocrLang = AppSettings.OcrLanguageMap.GetValueOrDefault(settings.SourceLanguage, "en-US");
        if (!_ocrService.SetLanguage(ocrLang))
        {
            TranslationError?.Invoke($"无法初始化 OCR 引擎，语言 {ocrLang} 可能未安装。");
            return;
        }

        EnsureTranslationService();
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _lastOcrText = "";

        try
        {
            await TranslationLoopAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            TranslationError?.Invoke($"翻译循环异常：{ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Stop the translation loop.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _isRunning = false;
    }

    private async Task TranslationLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var settings = _settingsService.Settings;

            try
            {
                // 1. Capture the selected region
                using var bitmap = _captureService.CaptureRegion(
                    settings.RegionX, settings.RegionY,
                    settings.RegionWidth, settings.RegionHeight);

                // 2. OCR
                var ocrText = await _ocrService.RecognizeAsync(bitmap);
                ocrText = ocrText.Trim();
                OcrTextRecognized?.Invoke(ocrText);

                // 3. Skip if text hasn't changed (de-duplicate)
                if (!string.IsNullOrWhiteSpace(ocrText) && ocrText != _lastOcrText)
                {
                    _lastOcrText = ocrText;

                    // 4. Debounce: wait a short moment to see if text stabilizes
                    await Task.Delay(500, ct);

                    // Re-capture to confirm text is stable
                    using var bitmap2 = _captureService.CaptureRegion(
                        settings.RegionX, settings.RegionY,
                        settings.RegionWidth, settings.RegionHeight);
                    var confirmText = (await _ocrService.RecognizeAsync(bitmap2)).Trim();

                    if (confirmText != ocrText)
                    {
                        // Text is still changing, skip this round
                        _lastOcrText = confirmText;
                        await Task.Delay(settings.CaptureIntervalSeconds * 1000, ct);
                        continue;
                    }

                    // 5. Translate
                    TranslationStarted?.Invoke();

                    try
                    {
                        // Use streaming translation
                        await foreach (var chunk in _translationService!.TranslateStreamAsync(
                            ocrText, settings.TargetLanguage, ct))
                        {
                            TranslationChunkReceived?.Invoke(chunk);
                        }
                        TranslationStreamFinished?.Invoke();

                        TranslationCount++;
                        TranslationCountChanged?.Invoke(TranslationCount);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        TranslationError?.Invoke($"翻译失败：{ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Translation loop error: {ex.Message}");
            }

            // Wait for the next capture interval
            await Task.Delay(settings.CaptureIntervalSeconds * 1000, ct);
        }
    }

    /// <summary>
    /// Perform a single one-shot translation of the current region.
    /// </summary>
    public async Task TranslateOnceAsync()
    {
        var settings = _settingsService.Settings;
        if (!settings.HasRegion)
        {
            TranslationError?.Invoke("请先选择翻译区域。");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            TranslationError?.Invoke("请先在设置中配置 API Key。");
            return;
        }

        var ocrLang = AppSettings.OcrLanguageMap.GetValueOrDefault(settings.SourceLanguage, "en-US");
        if (!_ocrService.SetLanguage(ocrLang))
        {
            TranslationError?.Invoke($"无法初始化 OCR 引擎。");
            return;
        }

        EnsureTranslationService();
        TranslationStarted?.Invoke();

        try
        {
            using var bitmap = _captureService.CaptureRegion(
                settings.RegionX, settings.RegionY,
                settings.RegionWidth, settings.RegionHeight);

            var ocrText = (await _ocrService.RecognizeAsync(bitmap)).Trim();
            OcrTextRecognized?.Invoke(ocrText);

            if (string.IsNullOrWhiteSpace(ocrText))
            {
                TranslationError?.Invoke("未识别到文字内容。");
                return;
            }

            var result = await _translationService!.TranslateAsync(ocrText, settings.TargetLanguage);
            TranslationCompleted?.Invoke(result);

            TranslationCount++;
            TranslationCountChanged?.Invoke(TranslationCount);
        }
        catch (Exception ex)
        {
            TranslationError?.Invoke($"翻译失败：{ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        _translationService?.Dispose();
    }
}
