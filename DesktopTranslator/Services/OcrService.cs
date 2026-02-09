using System.Drawing;
using Windows.Globalization;
using Windows.Media.Ocr;
using DesktopTranslator.Helpers;

namespace DesktopTranslator.Services;

public class OcrService
{
    private OcrEngine? _ocrEngine;
    private string _currentLanguage = "";

    /// <summary>
    /// Initialize or reinitialize the OCR engine with the specified language.
    /// </summary>
    public bool SetLanguage(string bcp47Language)
    {
        if (_currentLanguage == bcp47Language && _ocrEngine != null)
            return true;

        try
        {
            var language = new Language(bcp47Language);
            if (OcrEngine.IsLanguageSupported(language))
            {
                _ocrEngine = OcrEngine.TryCreateFromLanguage(language);
                _currentLanguage = bcp47Language;
                return _ocrEngine != null;
            }

            // Fall back to first available language
            var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
            if (availableLanguages.Count > 0)
            {
                _ocrEngine = OcrEngine.TryCreateFromLanguage(availableLanguages[0]);
                _currentLanguage = availableLanguages[0].LanguageTag;
                return _ocrEngine != null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OCR init error: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Recognize text from a System.Drawing.Bitmap.
    /// </summary>
    public async Task<string> RecognizeAsync(Bitmap bitmap)
    {
        if (_ocrEngine == null)
            throw new InvalidOperationException("OCR engine is not initialized. Call SetLanguage first.");

        using var softwareBitmap = await BitmapHelper.ConvertToSoftwareBitmapAsync(bitmap);
        var result = await _ocrEngine.RecognizeAsync(softwareBitmap);

        return result.Text ?? string.Empty;
    }

    /// <summary>
    /// Get all available OCR languages installed on this system.
    /// </summary>
    public static IReadOnlyList<Language> GetAvailableLanguages()
    {
        return OcrEngine.AvailableRecognizerLanguages;
    }
}
