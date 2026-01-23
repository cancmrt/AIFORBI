using System;
using System.Diagnostics;
using AICONNECTOR;
using AICONNECTOR.Connectors;
using AIFORBI.Models;
using AIFORBI.Tools;
using DBCONNECTOR.Connectors;
using DBCONNECTOR.Dtos.Mssql;
using DBCONNECTOR.Interfaces;
using Qdrant.Client.Grpc;

namespace AIFORBI.Services;

public class AiResponse
{
    public string? SqlDistilOzet { get; set; }
    public string? GeneratedSql { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
}

public class ReportService : IReportService
{
    private readonly IChatRepository _chatRepository;
    private readonly IConnect _chatClient;
    private readonly IConnect _embedClient;
    private readonly QdrantConnector _qdcon;
    private readonly IDbConnector _dbConnector;
    private readonly SettingsService _settingsService;

    public ReportService(IChatRepository chatRepository, SettingsService settingsService, IDbConnector dbConnector, IConfiguration configuration)
    {
        _chatRepository = chatRepository;
        _settingsService = settingsService;
        _dbConnector = dbConnector;

        // Get configuration values with defaults
        var ollamaBaseUrl = configuration["ConnStrs:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var geminiApiKey = configuration["ConnStrs:Gemini:ApiKey"] ?? "";
        var geminiModel = configuration["ConnStrs:Gemini:Model"] ?? "gemini-pro";
        var qdrantHost = configuration["ConnStrs:Qdrant:Host"] ?? "localhost";
        var qdrantPort = configuration["ConnStrs:Qdrant:Grpc"] ?? "6334";


        // 1. Initialize Embed Provider (Defaulting to Ollama)
        _embedClient = new OllamaConnector(ollamaBaseUrl, "nomic-embed-text", "qwen2.5-coder:7b");

        // 2. Initialize Chat Provider
        var chatProvider = configuration["ConnStrs:AI:ChatProvider"] ?? "Ollama";
        if (chatProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
        {
            _chatClient = new GeminiConnector(geminiApiKey, geminiModel);
        }
        else
        {
            _chatClient = new OllamaConnector(ollamaBaseUrl, "nomic-embed-text", "qwen2.5-coder:7b");
        }

        _qdcon = new QdrantConnector(qdrantHost, Convert.ToInt32(qdrantPort), "db_maps");
    }
    private sealed record TableCtx(string Schema, string Table, string Summary, string Json);


    private static string BuildContextText(IEnumerable<TableCtx> ctxs)
    {
        return string.Join("\n\n",
            ctxs.Select(c => $"### [{c.Schema}].[{c.Table}]\n{c.Summary}"));
    }
    public string QdrantOzet(AskModel AskQ)
    {
        var qVec = _embedClient.EmbedText(AskQ.Question);

        // 2) Qdrant: db + table_summary filtre
        var filter = AiConnectorUtil.BuildEqualsFilter(
            ("db", _dbConnector.DatabaseName),
            ("kind", "table_summary")
        );

        int n = _settingsService.GetDbMapFast().Tables.Count;
        int topK = Math.Clamp((int)Math.Round(Math.Sqrt(n) + 4), 6, 20);
        var hits = _qdcon.Search(qVec, topK, filter: filter);
        var contexts = new List<TableCtx>();
        foreach (var h in hits)
        {
            var schema = h.Payload.TryGetValue("schema", out var sv) && sv.KindCase == Value.KindOneofCase.StringValue ? sv.StringValue : "?";
            var table = h.Payload.TryGetValue("table", out var tv2) && tv2.KindCase == Value.KindOneofCase.StringValue ? tv2.StringValue : "?";
            var text = h.Payload.TryGetValue("text", out var txt) && txt.KindCase == Value.KindOneofCase.StringValue ? txt.StringValue : "?";
            var json = h.Payload.TryGetValue("json", out var jsn) && jsn.KindCase == Value.KindOneofCase.StringValue ? jsn.StringValue : "?";
            contexts.Add(new TableCtx(schema, table, $"Summary: {text}", $"Json: {json}"));
        }

        // var filterDbSum = AiConnectorUtil.BuildEqualsFilter(
        //     ("db", "AdventureWorksDW2022"),
        //     ("kind", "db_overview")
        // );
        // var hitsDbSum = _qdcon.Search(qVec, 6, filter: filterDbSum);
        // foreach (var h in hitsDbSum)
        // {
        //     var text = h.Payload.TryGetValue("text", out var txt) && txt.KindCase == Value.KindOneofCase.StringValue ? txt.StringValue : "?";
        //     contexts.Add(new TableCtx("dbo", "general summary", $"Summary: {text}", $"Json: none"));
        // }

        //var tumOzet = AiConnectorUtil.ToCompactJson(setService.GetDbMapFast().Tables.Select(x => new { x.Table.Name, x.AISummary }).ToList());

        var tumOzet = BuildContextText(contexts);
        return tumOzet;
    }
    public AiResponse GenerateQuestionSql(AskModel AskQ, string tumOzet)
    {

        var systemDistil = $"""
            Sen profesyonel bir veritabanı uzmanısın.
            T-SQL üretebilmek için gereken özeti çıkaracaksın.
            Sana verilecek özette tablo adı, kolonları ve tablonun özetini anlatan bir json veya metin olacak
            Kullanıcının sorusu üzerine json veya metin verisinde alakalı tabloları bulup okuyacaksın ve sql sorgusu yazacaksın
            Sana verilen json'ın veya metinin dışına çıkmayacaksın, tablo alanları tablo isimlerine ve özetteki ilişkilerine dikkat et, json veya metin dışına çıkma jsonda veya metinde arama yap
            Uydurma yapma, istenen soruyu anla ve alakalı tabloları verilen jsonda veya metinde bulup ilişkilerini t-sql yazacak şekilde özetle 
            Özetin bir dil modelinin T-SQL yazmasını sağlayacak şekilde olacak.
            En az 10, en fazla 20 paragraf özet çıkart.
            T-SQL özetinin içinde bulunmayacak.
            Gereken tablolar ve bunların kolonları olacak.
            Bu tablolar ve kolonları uydurma, dikkatle kontrol et yanlış olmasın.
            Tablo adları kolon adları kesinlikle doğru olmalı iyice kontrol et.
            Tabloların adları : {_settingsService.GetDbMapFast().Tables.Select(x => x.Table.Name).Aggregate((a, b) => a + ", " + b)}
            """;

        var userDistil = $"""
            
            İstenen soruya uygun T-SQL üretilebilecek bir özet çıkart. Sana verilen özetin dışına çıkma

            Sorum (TR): {AskQ.Question}

            Databasedeki ilgili tablolar, tabloların kolonları ve anlatımını içeren json veya metin:
            {tumOzet}

            """;

        var sqlDistilOzet = _chatClient.Chat(systemDistil, userDistil);


        var system = $"""
            Sen bir T-SQL üretecisin. Sadece geçerli T-SQL SELECT döndür.
            Kurallar:
            - Yalnızca T-SQL sorgusu üret, açıklama/metin/markdown verme.
            - Tablo ve kolon adlarını bağlamda verildiği gibi kullan; uydurma yapma.
            - Tablo ve kolon adlarını kontrol et, yanlış olmasın. Mutlaka tabloda kolonların olduğuna emin ol
            - YALNIZCA SELECT SORGUSU KULLANCAKSIN
            - İstenen soru için birden fazla tabloları kullanman gerekiyorsa, ilişkilerine bakarak JOIN, LEFT JOIN, INNER JOIN, RIGHT JOIN gibi T-SQL özelliklerini kullanabilirsin.
            - Parametreleri içine göm variable olmayacak.
            - INSERT/UPDATE/DELETE/CREATE KESİNLİKLE YASAK
            - Uygunsa WITH CTE kullanabilirsin. DML/DDL (INSERT/UPDATE/DELETE/CREATE) YASAK.
            - Belirtilen özet kapsamında kullanıcının sorusunun sadece SELECT sqlini yazacaksın.
            - Tablo adları kolon adları kesinlikle doğru olmalı iyice kontrol et.
            - Tabloların adları : {_settingsService.GetDbMapFast().Tables.Select(x => x.Table.Name).Aggregate((a, b) => a + ", " + b)}
            """;



        var user = $"""
            Sorum (TR): {AskQ.Question}

            Sadece T-SQL döndür. Kod blokları veya açıklama ekleme.

            Tüm özet aşağıda(buraya bakıp sql sorugusunu yaz):
            {sqlDistilOzet}

            """;

        var sql = _chatClient.Chat(system, user);

        var pureSql = AiConnectorUtil.CleanRawSql(sql);

        var aiNewResponse = new AiResponse();
        aiNewResponse.SqlDistilOzet = sqlDistilOzet;
        aiNewResponse.GeneratedSql = pureSql;
        aiNewResponse.Error = null;

        return aiNewResponse;

    }

    public string GenerateTextExplanation(AskModel AskQ, string tumOzet)
    {

        var systemDistil = $"""
            Sen profesyonel bir veritabanı uzmanısın.
            Kullanıcının isteğine göre gereken özeti çıkaracaksın.
            Sana verilecek özette tablo adı, kolonları ve tablonun özetini anlatan bir json veya metin olacak
            Bu özetleri kullanarak kullanıcının sorusuna uygun açıklama metni yazacaksın
            Uydurma yapma, istenen soruyu anla ve kullanıcının isteyebileceği açıklama metnini yaz
            """;

        var userDistil = $"""
            
            Sana verilen sorunun dışına çıkma açıklama metni yaz. Sana verilen özetin dışına çıkma

            Sorum (TR): {AskQ.Question}

            Databasedeki ilgili tablolar, tabloların kolonları ve anlatımını içeren json veya metin:
            {tumOzet}

            """;

        var aciklamaOzet = _chatClient.Chat(systemDistil, userDistil);


        return aciklamaOzet;
    }

    public string SqlGiveErrorAskAIToCorrent(AskModel AskQ, AiResponse aiResponse)
    {
        var systemCorrector = $"""
            Sen profesyonel bir veritabanı uzmanısın ve T-SQL uzmanısın.
            Kullanıcı bir soru sordu, AI bir sql üretti ve hata aldı, bir uzman ve profesyonel olarak bunu çözeceksin
            Ancak üretilen bir T-SQL hata aldı sen bu hatayı düzeltmekle görevlisin.
            Sana kullanıcının sorduğu soruyu vereceğim
            Sana üretilen T-SQL'i de vereceğim
            Sana bu T-SQL'i çalıştırırken aldığı hatayı vereceğim.
            Sana bu hatalı T-SQL'i üretirken kullandığı özeti vereceğim.
            Sana verilecek özette tablo adı, kolonları ve tablonun özetini anlatan bir json veya metin olacak
            Sana verilen bu durumlarda arama yapacaksın ve hatayı düzelteceksin
            Sana verilen json'ın veya metinin dışına çıkmayacaksın, tablo alanları tablo isimlerine ve özetteki ilişkilerine dikkat et, json veya metin dışına çıkma
            Uydurma yapma, istenen soruyu anla, hatayı anla ve alakalı tabloları verilen jsonda veya metinde bulup doğru t-sql'i yaz
            Cevap olarak yalnızca T-SQL sorgusu üreteceksin.
            """;

        var userCorrector = $"""
            
            Kullancının sorduğu soru (TR): {AskQ.Question}
            Üretilen T-SQL: {aiResponse.GeneratedSql}
            Çalıştırılırken alınan hata: {aiResponse.Error}
            T-SQL üretirken kullandığı özet:
            {aiResponse.SqlDistilOzet}

            Yukardaki bilgilere göre T-SQL'i düzelt ve sadece T-SQL döndür. Kod blokları veya açıklama ekleme.

            """;

        var sql = _chatClient.Chat(systemCorrector, userCorrector);

        return AiConnectorUtil.CleanRawSql(sql);
    }
    public string DetectionOfUser(AskModel AskQ)
    {
        var systemCorrector = $"""
            Sen profesyonel anlamlandırma uzmanısın.
            Kullanıcının sorduğu sorudan yalnızca 3 bilgi döneceksin
            1- Eğer kullanıcı sadece açıklama istediyse "TEXT_ONLY" döneceksin
            2- Eğer kullanıcı sadece dataları görmek istediyse "TABLE_ONLY" döneceksin
            3- Eğer kullanıcı grafik istiyorsa "DRAW_GRAPHIC" döneceksin
            Cevap olarak yalnızca yukarıdaki 3 değerden birini döneceksin. Başka bir şey ekleme.
            """;


        var userCorrector = $"""
            
            Kullancının sorduğu soru (TR): {AskQ.Question}
            
            Yukardaki bilgilere göre bana kullanıcının isteğini anlamlandır ve sadece 3 değerden birini döndür: "TEXT_ONLY", "TABLE_ONLY", "DRAW_GRAPHIC"

            """;

        var sql = _chatClient.Chat(systemCorrector, userCorrector);

        return AiConnectorUtil.CleanRawSql(sql);
    }

    public AnswerModel AskQuestion(AskModel AskQ)
    {
        // SAVE USER QUESTION
        var sessionId = !string.IsNullOrEmpty(AskQ.SessionId) ? AskQ.SessionId : "default-session";

        try
        {
            _chatRepository.AddChatHistory(new ChatHistoryDto
            {
                SessionId = sessionId,
                Role = "user",
                Content = AskQ.Question,
                CreatedAt = DateTime.Now,
                IsHtml = false
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("History save error: " + ex.Message);
        }

        AnswerModel ans = new AnswerModel();
        Stopwatch sw = new Stopwatch();
        sw.Start();
        AiResponse? aResponse = null;
        string tumOzet = "";
        try
        {
            AskQ.UserDesireDetection = DetectionOfUser(AskQ);
            if (string.IsNullOrWhiteSpace(AskQ.UserDesireDetection))
                AskQ.UserDesireDetection = "TEXT_ONLY";
            ans.UserDesireDetection = AskQ.UserDesireDetection;
            tumOzet = QdrantOzet(AskQ);
        }
        catch (Exception ex)
        {
            aResponse = new AiResponse { Error = ex.Message };
        }

        if (AskQ.UserDesireDetection == "TEXT_ONLY")
        {
            var explanation = GenerateTextExplanation(AskQ, tumOzet);
            sw.Stop();
            ans.Answer = explanation;
            ans.ElapsedSeconds = ((int)sw.ElapsedMilliseconds) / 60;
            if (ans.ElapsedSeconds == 0) ans.ElapsedSeconds = 1;
        }
        else if (AskQ.UserDesireDetection == "TABLE_ONLY")
        {
            try
            {
                aResponse = GenerateQuestionSql(AskQ, tumOzet);
                if (aResponse.Error == null)
                {
                    var result = PollySyncHelpers.ExecuteWithFallbackAltRetry<string>(
                        primary: () =>
                        {
                            return _dbConnector.ExecuteRawSqlToJson(aResponse.GeneratedSql!);
                        },
                        alternative: (lastErr) =>
                        {
                            Console.WriteLine($"Alternative denemesi; önceki hata: {lastErr?.GetType().Name} - {lastErr?.Message}");
                            aResponse.Error = lastErr?.Message;
                            var result = SqlGiveErrorAskAIToCorrent(AskQ, aResponse);
                            aResponse.Error = null;
                            aResponse.GeneratedSql = result;
                            return _dbConnector.ExecuteRawSqlToJson(aResponse.GeneratedSql!);
                        },
                        alternativeMaxRetries: 3,
                        onFallback: ex => Console.WriteLine($"Fallback tetiklendi (primary hatası): {ex.Message}"),
                        onAlternativeRetry: (ex, wait, attempt) =>
                        Console.WriteLine($"Alternative retry #{attempt} | {wait.TotalMilliseconds} ms bekleme | Hata: {ex.Message}")
                    );
                    aResponse.Data = result;
                    ans.GeneratedGraphicHtmlCode = ApacheEchartsTableDrawer(AskQ, aResponse);
                    sw.Stop();
                    ans.Answer = aResponse.Data;
                    ans.ElapsedSeconds = ((int)sw.ElapsedMilliseconds) / 60;
                    if (ans.ElapsedSeconds == 0) ans.ElapsedSeconds = 1;
                }
            }
            catch (Exception ex)
            {
                aResponse = new AiResponse { Error = ex.Message };
            }

        }
        else if (AskQ.UserDesireDetection == "DRAW_GRAPHIC")
        {
            try
            {
                aResponse = GenerateQuestionSql(AskQ, tumOzet);
                if (aResponse.Error == null)
                {
                    var result = PollySyncHelpers.ExecuteWithFallbackAltRetry<string>(
                        primary: () =>
                        {
                            return _dbConnector.ExecuteRawSqlToJson(aResponse.GeneratedSql!);
                        },
                        alternative: (lastErr) =>
                        {
                            Console.WriteLine($"Alternative denemesi; önceki hata: {lastErr?.GetType().Name} - {lastErr?.Message}");
                            aResponse.Error = lastErr?.Message;
                            var result = SqlGiveErrorAskAIToCorrent(AskQ, aResponse);
                            aResponse.Error = null;
                            aResponse.GeneratedSql = result;
                            return _dbConnector.ExecuteRawSqlToJson(aResponse.GeneratedSql!);
                        },
                        alternativeMaxRetries: 3,
                        onFallback: ex => Console.WriteLine($"Fallback tetiklendi (primary hatası): {ex.Message}"),
                        onAlternativeRetry: (ex, wait, attempt) =>
                            Console.WriteLine($"Alternative retry #{attempt} | {wait.TotalMilliseconds} ms bekleme | Hata: {ex.Message}")
                    );
                    aResponse.Data = result;
                    ans.GeneratedGraphicHtmlCode = ApacheEchartsChartDrawer(AskQ, aResponse);
                    sw.Stop();
                    ans.Answer = aResponse.Data;
                    ans.ElapsedSeconds = ((int)sw.ElapsedMilliseconds) / 60;
                    if (ans.ElapsedSeconds == 0) ans.ElapsedSeconds = 1;

                }
            }
            catch (Exception ex)
            {
                aResponse = new AiResponse { Error = ex.Message };
            }
        }
        // SAVE ASSISTANT RESPONSE
        // Check if we have HTML, otherwise use Data or Error message
        string contentToSave = !string.IsNullOrEmpty(ans.GeneratedGraphicHtmlCode)
            ? ans.GeneratedGraphicHtmlCode
            : (!string.IsNullOrEmpty(ans.Answer) ? ans.Answer : (aResponse?.Error ?? "No response"));

        bool isHtml = !string.IsNullOrEmpty(ans.GeneratedGraphicHtmlCode);

        try
        {
            _chatRepository.AddChatHistory(new ChatHistoryDto
            {
                SessionId = sessionId,
                Role = "assistant",
                Content = contentToSave,
                CreatedAt = DateTime.Now,
                IsHtml = isHtml
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("History save error: " + ex.Message);
        }

        return ans;
    }

    public string ApacheEchartsChartDrawer(AskModel AskQ, AiResponse aiResponse)
    {
        var systemProgrammer = $"""
            Sen profesyonel bir yazılımcısın.
            Sana kullanıcının sorduğu soru verilecek,
            Bunun sonucunda AI tarafından üretilen özet verilecek
            Bunun sonucunda AI tarafından üretilen sql sorgusu verilecek
            Bunun sonucunda ortaya çıkan data json olarak verilecek
            Sen bu bilgileri kullanarak Apache ECharts ile html'de grafiğini oluşturacaksın.
            Bana direkt html çıktısı olarak vereceksin.
            Apache ECharts için CDN kullanacaksın.
            Başlıklar ve yazılar içeriğe uygun olsun.
            Sadece sana verilen datayı kullanacaksın uydurma yapma.
            Kullanıcının özel bir grafik isteği varsa onu yap.
            Kullanıcının özel grafik isteği yoksa kendin yorumla ve grafik mi, tablo mu oluşturulması daha doğru olur karar ver ve ona göre kodu üret.
            Hiçbir şekilde oluşturulan htmlin javascriptin içine açıklama satırı yorum vebenzeri şeyler ekleme.
            """;

        var userDrawer = $"""
            
            Kullanıcının Sorusu:{AskQ.Question}

            AI Database özeti: {aiResponse.SqlDistilOzet}

            AI Sql Sorgusu: {aiResponse.GeneratedSql}

            Bunun sonucunda çıkan data:
            {aiResponse.Data}

            Kodunu üret, sadece kod çıktısı olacak başka bir şey, yorum v.b. ekleme

            """;

        var resultOfHtml = _chatClient.Chat(systemProgrammer, userDrawer);


        return resultOfHtml;
    }
    public string ApacheEchartsTableDrawer(AskModel AskQ, AiResponse aiResponse)
    {
        var systemProgrammer = $"""
            Sen profesyonel bir yazılımcısın.
            Sana kullanıcının sorduğu soru verilecek,
            Bunun sonucunda AI tarafından üretilen özet verilecek
            Bunun sonucunda AI tarafından üretilen sql sorgusu verilecek
            Bunun sonucunda ortaya çıkan data json olarak verilecek
            Sen bu bilgileri kullanarak Apache ECharts ile html'de tablosunu oluşturacaksın.
            Bana direkt html çıktısı olarak vereceksin.
            Apache ECharts için CDN kullanacaksın.
            Başlıklar ve yazılar içeriğe uygun olsun.
            Sadece sana verilen datayı kullanacaksın uydurma yapma.
            Sadece tablo oluşturacaksın. Herhangi bir grafik oluşturmayacaksın.
            Hiçbir şekilde oluşturulan htmlin javascriptin içine açıklama satırı yorum vebenzeri şeyler ekleme.
            """;

        var userDrawer = $"""
            
            Kullanıcının Sorusu:{AskQ.Question}

            AI Database özeti: {aiResponse.SqlDistilOzet}

            AI Sql Sorgusu: {aiResponse.GeneratedSql}

            Bunun sonucunda çıkan data:
            {aiResponse.Data}

            Kodunu üret, sadece kod çıktısı olacak başka bir şey, yorum v.b. ekleme

            """;

        var resultOfHtml = _chatClient.Chat(systemProgrammer, userDrawer);


        return resultOfHtml;
    }

}
