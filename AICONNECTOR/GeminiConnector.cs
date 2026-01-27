using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AICONNECTOR.Connectors;

public class GeminiConnector : IConnect
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private string _ApiKey { get; set; }
    private string _Model { get; set; }
    private IEnumerable<string> _FallbackModels { get; set; }
    private HttpClient _HttpClient { get; set; }

    public GeminiConnector(string apiKey, string model = "gemini-pro", IEnumerable<string>? fallbackModels = null)
    {
        _ApiKey = apiKey;
        _Model = model;
        _FallbackModels = fallbackModels ?? new List<string>();
        _HttpClient = new HttpClient();
    }

    public float[] EmbedText(string text)
    {
        // Not implemented for this iteration as we are using hybrid approach
        // If needed, can use: models/embedding-001
        throw new NotImplementedException("Gemini EmbedText is not implemented yet in this hybrid setup.");
    }

    public string Chat(string systemPrompt, string userPrompt, object? extraParams = null)
    {
        // Define model hierarchy: Primary (configured) -> Fallbacks
        var models = new List<string> { _Model };
        
        foreach (var fb in _FallbackModels)
        {
            if (!models.Contains(fb, StringComparer.OrdinalIgnoreCase))
            {
                models.Add(fb);
            }
        }

        Exception? lastException = null;

        foreach (var model in models)
        {
            try
            {
                // Gemini API Endpoint
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_ApiKey}";

                // Construct Gemini Request Body
                var payload = new
                {
                    contents = new[]
                    {
                        new {
                            role = "user",
                            parts = new[] {
                                new { text = systemPrompt + "\n\n" + userPrompt }
                            }
                        }
                    }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

                var p = _HttpClient.PostAsync(url, jsonContent).ConfigureAwait(false).GetAwaiter().GetResult();

                var raw = p.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                if (!p.IsSuccessStatusCode)
                {
                    throw new Exception($"Gemini API Error ({p.StatusCode}) with model {model}: {raw}");
                }

                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                // Response format: candidates[0].content.parts[0].text
                if (root.TryGetProperty("candidates", out var candidates)
                    && candidates.ValueKind == JsonValueKind.Array
                    && candidates.GetArrayLength() > 0)
                {
                    var firstCand = candidates[0];
                    if (firstCand.TryGetProperty("content", out var content)
                        && content.TryGetProperty("parts", out var parts)
                        && parts.ValueKind == JsonValueKind.Array
                        && parts.GetArrayLength() > 0)
                    {
                        return parts[0].GetProperty("text").GetString() ?? "";
                    }
                }
                
                // If success but no text, technically a success from API but empty response. 
                // We return empty string or could try next model. usage implies we accept it.
                return "";
            }
            catch (Exception ex)
            {
                lastException = ex;
                // Try next model
                continue;
            }
        }

        throw lastException ?? new Exception("All Gemini models failed.");
    }
}
