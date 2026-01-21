namespace AICONNECTOR.Connectors;

public interface IConnect
{
    float[] EmbedText(string text);
    string Chat(string systemPrompt, string userPrompt, object? extraParams = null);
}
