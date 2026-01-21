using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;

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
    private HttpClient _HttpClient { get; set; }

    public GeminiConnector(string apiKey, string model = "gemini-pro")
    {
        _ApiKey = apiKey;
        _Model = model;
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
        // Gemini API Endpoint
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_Model}:generateContent?key={_ApiKey}";

        // Construct Gemini Request Body
        // Gemini doesn't strictly distinguish "System" role in the same way as OpenAI/Ollama in the v1beta payload structure 
        // effectively, but we can prepend it to the user message or use the new 'systemInstruction' (available in some models)
        // For broad compatibility, we will merge system prompt or treat it as the first part of the conversation.
        
        // Simple structure: contents with parts.
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
             throw new Exception($"Gemini API Error ({p.StatusCode}): {raw}");
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

        return "";
    }
}
