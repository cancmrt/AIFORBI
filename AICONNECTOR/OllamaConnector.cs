using System.Text;
using System.Text.Json;
using Ollama;

namespace AICONNECTOR.Connectors;

public class OllamaConnector : IConnect
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
    private string _BaseUrl { get; set; }
    private string _EmbedModel { get; set; }
    private string _ChatModel { get; set; }

    private HttpClient _HttpClient { get; set; }

    public OllamaConnector(string BaseUrl, string EmbedModel, string ChatModel)
    {
        _BaseUrl = BaseUrl;
        _EmbedModel = EmbedModel;
        _ChatModel = ChatModel;
        _HttpClient = new HttpClient
        {
            BaseAddress = new Uri(_BaseUrl),
            Timeout = TimeSpan.FromMinutes(60)
        };
    }

    public float[] EmbedText(string text)
    {
        var payload = new
        {
            model = _EmbedModel,
            input = text,
            prompt = text
        };
        var resp = _HttpClient.PostAsync(
               "/api/embeddings",
               new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json")
           ).ConfigureAwait(false).GetAwaiter().GetResult();

        var raw = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        // Şekil A: { "embedding": [ ... ] }
        if (root.TryGetProperty("embedding", out var embA) && embA.ValueKind == JsonValueKind.Array)
            return embA.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();

        // Şekil B: { "data": [ { "embedding": [ ... ] } ] }
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            var first = data.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("embedding", out var embB))
                return embB.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
        }

        throw new InvalidOperationException("Ollama embeddings yanıtı beklenen formatta değil: " + raw);
    }
    public string Chat(
            string systemPrompt,
            string userPrompt,
            object? extraParams = null)
    {
        var messages = new object[]
        {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt }
        };

        var dict = new Dictionary<string, object?>
        {
            ["model"] = _ChatModel,
            ["stream"] = false,
            ["messages"] = messages
        };

        // temperature, top_p, num_ctx vb. ek parametreleri geçir
        if (extraParams is not null)
        {
            foreach (var p in extraParams.GetType().GetProperties())
                dict[p.Name] = p.GetValue(extraParams);
        }

        var resp = _HttpClient.PostAsync(
            "/api/chat",
            new StringContent(JsonSerializer.Serialize(dict, _json), Encoding.UTF8, "application/json")
        ).ConfigureAwait(false).GetAwaiter().GetResult();

        var raw = resp.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        // Tipik: { "message": { "role":"assistant","content":"..." }, ... }
        if (root.TryGetProperty("message", out var msg)
            && msg.ValueKind == JsonValueKind.Object
            && msg.TryGetProperty("content", out var content))
            return content.GetString() ?? string.Empty;

        // Alternatif: { "response": "..." }
        if (root.TryGetProperty("response", out var response))
            return response.GetString() ?? string.Empty;

        // Fallback: ham gövde
        return raw;
    }
    public void UpdateLLModel(string name)
    {
        _ChatModel = name;
    }
    public void UpdateEmbedModel(string name)
    {
        _EmbedModel = name;
    }
}
    
