using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DesktopTranslator.Services;

public class TranslationService : IDisposable
{
    private HttpClient _httpClient;
    private string _baseUrl;
    private string _model;
    private string _systemPrompt;
    private double _temperature;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TranslationService(string apiKey, string baseUrl, string model, string systemPrompt, double temperature)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _systemPrompt = systemPrompt;
        _temperature = temperature;

        _httpClient = CreateHttpClient(apiKey);
    }

    private static HttpClient CreateHttpClient(string apiKey)
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        return client;
    }

    public void UpdateConfig(string apiKey, string baseUrl, string model, string systemPrompt, double temperature)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _systemPrompt = systemPrompt;
        _temperature = temperature;

        _httpClient.Dispose();
        _httpClient = CreateHttpClient(apiKey);
    }

    /// <summary>
    /// Translate text using non-streaming mode with retry logic.
    /// </summary>
    public async Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var resolvedPrompt = _systemPrompt.Replace("{target_language}", targetLanguage);
        var maxRetries = 2;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var requestBody = new
                {
                    model = _model,
                    messages = new object[]
                    {
                        new { role = "system", content = resolvedPrompt },
                        new { role = "user", content = text }
                    },
                    temperature = _temperature,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{_baseUrl}/chat/completions", content, cancellationToken);

                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

                using var doc = JsonDocument.Parse(responseJson);
                var result = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return result?.Trim() ?? string.Empty;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception) when (attempt < maxRetries)
            {
                await Task.Delay(1000 * (attempt + 1), cancellationToken);
            }
        }

        throw new InvalidOperationException("Translation failed after all retry attempts.");
    }

    /// <summary>
    /// Translate text using streaming mode (SSE).
    /// Yields partial translation chunks as they arrive.
    /// </summary>
    public async IAsyncEnumerable<string> TranslateStreamAsync(
        string text,
        string targetLanguage,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var resolvedPrompt = _systemPrompt.Replace("{target_language}", targetLanguage);

        var requestBody = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = resolvedPrompt },
                new { role = "user", content = text }
            },
            temperature = _temperature,
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]")
                break;

            string? chunk = null;
            try
            {
                using var chunkDoc = JsonDocument.Parse(data);
                var delta = chunkDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (delta.TryGetProperty("content", out var contentProp))
                {
                    chunk = contentProp.GetString();
                }
            }
            catch (JsonException)
            {
                // Skip malformed chunks
                continue;
            }

            if (!string.IsNullOrEmpty(chunk))
                yield return chunk;
        }
    }

    /// <summary>
    /// Test the API connection by sending a simple request.
    /// Returns null on success, or an error message on failure.
    /// </summary>
    public async Task<string?> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                messages = new object[]
                {
                    new { role = "user", content = "Hi" }
                },
                max_tokens = 5
            };

            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/chat/completions", content, cancellationToken);

            if (response.IsSuccessStatusCode)
                return null;

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return $"HTTP {(int)response.StatusCode}: {errorBody}";
        }
        catch (TaskCanceledException)
        {
            return "请求超时，请检查网络连接和 API 地址。";
        }
        catch (HttpRequestException ex)
        {
            return $"网络错误：{ex.Message}";
        }
        catch (Exception ex)
        {
            return $"未知错误：{ex.Message}";
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
