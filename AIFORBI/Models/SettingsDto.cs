namespace AIFORBI.Models;

public class SettingsDto
{
    public ConnStrsDto ConnStrs { get; set; } = new();
    public LoggingDto Logging { get; set; } = new();
    public string AllowedHosts { get; set; } = "*";
}

public class ConnStrsDto
{
    public DbConnectorSettings DbConnector { get; set; } = new();
    public OllamaSettings Ollama { get; set; } = new();
    public QdrantSettings Qdrant { get; set; } = new();
    public AISettings AI { get; set; } = new();
    public GeminiSettings Gemini { get; set; } = new();
}

public class DbConnectorSettings
{
    public string Type { get; set; } = "Mssql";
    public MssqlSettings Mssql { get; set; } = new();
}

public class MssqlSettings
{
    public string ConnStr { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string Schema { get; set; } = "dbo";
}

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "";
    public string ChatModel { get; set; } = "";
    public string EmbedModel { get; set; } = "";
}

public class QdrantSettings
{
    public string Host { get; set; } = "";
    public string Grpc { get; set; } = "";
}

public class AISettings
{
    public string ChatProvider { get; set; } = "";
    public string EmbedProvider { get; set; } = "";
}

public class GeminiSettings
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public List<string> FallbackModels { get; set; } = new();
}

public class LoggingDto
{
    public LogLevelDto LogLevel { get; set; } = new();
}

public class LogLevelDto
{
    public string Default { get; set; } = "Information";
    public string MicrosoftAspNetCore { get; set; } = "Warning";
}
