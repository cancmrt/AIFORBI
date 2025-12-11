using System;
using System.Diagnostics;
using AICONNECTOR;
using AICONNECTOR.Connectors;
using AIFORBI.Models;
using AIFORBI.Tools;
using DBCONNECTOR.Connectors;
using Qdrant.Client.Grpc;

namespace AIFORBI.Services;

public class AiResponse {
    public string SqlDistilOzet { get; set; }
    public string GeneratedSql { get; set; }
    public string Data { get; set; }
    public string Error { get; set; }
}
public class ReportService
{
    public OllamaConnector olcon { get; set; }
    public QdrantConnector qdcon { get; set; }
    public MssqlConnector mscon { get; set; }

    public ReportService()
    {
        olcon = new OllamaConnector(AppConfig.Configuration["ConnStrs:Ollama:BaseUrl"], "nomic-embed-text", "qwen2.5-coder:7b");
        qdcon = new QdrantConnector(AppConfig.Configuration["ConnStrs:Qdrant:Host"], Convert.ToInt32(AppConfig.Configuration["ConnStrs:Qdrant:Grpc"]), "db_maps");
        mscon = new MssqlConnector(AppConfig.Configuration["ConnStrs:Mssql:ConnStr"], AppConfig.Configuration["ConnStrs:Mssql:DatabaseName"], AppConfig.Configuration["ConnStrs:Mssql:Schema"]);
    }
    private sealed record TableCtx(string Schema, string Table, string Summary, string Json);


    private static string BuildContextText(IEnumerable<TableCtx> ctxs)
    {
        return string.Join("\n\n",
            ctxs.Select(c => $"### [{c.Schema}].[{c.Table}]\n{c.Summary}"));
    }

    public AiResponse GenerateQuestionSql(AskModel AskQ)
    {
        var qVec = olcon.EmbedText(AskQ.Question);

        // 2) Qdrant: db + table_summary filtre
        var filter = AiConnectorUtil.BuildEqualsFilter(
            ("db", "AdventureWorksDW2022"),
            ("kind", "table_summary")
        );

        SettingsService setService = new SettingsService();
        int n = setService.GetDbMapFast().Tables.Count;
        int topK = Math.Clamp((int)Math.Round(Math.Sqrt(n) + 4), 6, 20);
        var hits = qdcon.Search(qVec, topK, filter: filter);
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
        // var hitsDbSum = qdcon.Search(qVec, 6, filter: filterDbSum);
        // foreach (var h in hitsDbSum)
        // {
        //     var text = h.Payload.TryGetValue("text", out var txt) && txt.KindCase == Value.KindOneofCase.StringValue ? txt.StringValue : "?";
        //     contexts.Add(new TableCtx("dbo", "general summary", $"Summary: {text}", $"Json: none"));
        // }

        //var tumOzet = AiConnectorUtil.ToCompactJson(setService.GetDbMapFast().Tables.Select(x => new { x.Table.Name, x.AISummary }).ToList());

        var tumOzet = BuildContextText(contexts);
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
            """;

        var userDistil = $"""
            
            İstenen soruya uygun T-SQL üretilebilecek bir özet çıkart. Sana verilen özetin dışına çıkma

            Sorum (TR): {AskQ.Question}

            Databasedeki ilgili tablolar, tabloların kolonları ve anlatımını içeren json veya metin:
            {tumOzet}

            """;

        var sqlDistilOzet = olcon.Chat(systemDistil, userDistil);


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
            """;



        var user = $"""
            Sorum (TR): {AskQ.Question}

            Sadece T-SQL döndür. Kod blokları veya açıklama ekleme.

            Tüm özet aşağıda(buraya bakıp sql sorugusunu yaz):
            {sqlDistilOzet}

            """;

        var sql = olcon.Chat(system, user);

        var pureSql = AiConnectorUtil.CleanRawSql(sql);

        var aiNewResponse = new AiResponse();
        aiNewResponse.SqlDistilOzet = sqlDistilOzet;
        aiNewResponse.GeneratedSql = pureSql;
        aiNewResponse.Error = null;

        return aiNewResponse;

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

        var sql = olcon.Chat(systemCorrector, userCorrector);

        return AiConnectorUtil.CleanRawSql(sql);
    }

    public AnswerModel AskQuestion(AskModel AskQ)
    {
        AnswerModel ans = new AnswerModel();
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var aResponse = GenerateQuestionSql(AskQ);

        var result = PollySyncHelpers.ExecuteWithFallbackAltRetry(
            primary: () =>
            {
                return mscon.ExecuteRawSqlToJson(aResponse.GeneratedSql);
            },
            alternative: (lastErr) =>
            {
                Console.WriteLine($"Alternative denemesi; önceki hata: {lastErr?.GetType().Name} - {lastErr?.Message}");
                aResponse.Error = lastErr?.Message;
                var result = SqlGiveErrorAskAIToCorrent(AskQ, aResponse);
                aResponse.Error = null;
                aResponse.GeneratedSql = result;
                return mscon.ExecuteRawSqlToJson(aResponse.GeneratedSql);
            },
            alternativeMaxRetries: 3,
            onFallback: ex => Console.WriteLine($"Fallback tetiklendi (primary hatası): {ex.Message}"),
            onAlternativeRetry: (ex, wait, attempt) =>
                Console.WriteLine($"Alternative retry #{attempt} | {wait.TotalMilliseconds} ms bekleme | Hata: {ex.Message}")
        );

        aResponse.Data = result;

        if(AskQ.DrawGraphic == true)
        {
            ans.GeneratedGraphicHtmlCode = D3jsHtmlDrawer(AskQ, aResponse);
        }

        sw.Stop();
        ans.Answer = result;
        ans.ElapsedSeconds = ((int)sw.ElapsedMilliseconds) / 60;
        return ans;
    }

    public string D3jsHtmlDrawer(AskModel AskQ, AiResponse aiResponse)
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
            
            """;

        var userDrawer = $"""
            
            Kullanıcının Sorusu:{AskQ.Question}

            AI Database özeti: {aiResponse.SqlDistilOzet}

            AI Sql Sorgusu: {aiResponse.GeneratedSql}

            Bunun sonucunda çıkan data:
            {aiResponse.Data}

            Kodunu üret, sadece kod çıktısı olacak başka bir şey, yorum v.b. ekleme

            """;

        var resultOfHtml = olcon.Chat(systemProgrammer, userDrawer);


        return resultOfHtml;
    }

}
