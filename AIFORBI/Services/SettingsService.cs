using DBCONNECTOR.Dtos.Common;
using DBCONNECTOR.Interfaces;
using AICONNECTOR;
using AICONNECTOR.Connectors;
using Qdrant.Client.Grpc;
using Microsoft.Extensions.Configuration;

namespace AIFORBI.Services;

public class SettingsService
{
    private readonly IDbConnector _dbConnector;
    public OllamaConnector olcon { get; set; }
    public QdrantConnector qdcon { get; set; }

    public SettingsService(IDbConnector dbConnector, IConfiguration configuration)
    {
        _dbConnector = dbConnector;
        
        var ollamaBaseUrl = configuration["ConnStrs:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var qdrantHost = configuration["ConnStrs:Qdrant:Host"] ?? "localhost";
        var qdrantPort = configuration["ConnStrs:Qdrant:Grpc"] ?? "6334";

        olcon = new OllamaConnector(ollamaBaseUrl, "nomic-embed-text", "qwen2.5-coder:7b");
        qdcon = new QdrantConnector(qdrantHost, Convert.ToInt32(qdrantPort), "db_maps");
    }

    public DatabaseMap GetDbMapFast()
    {
        var dbMap = _dbConnector.GetDbMap();
        for (int i = 0; i < dbMap.Tables.Count; i++)
        {
            var tableSum = _dbConnector.GetTableSummary(dbMap.Tables[i].Table.Name!);
            if (tableSum != null)
            {
                dbMap.Tables[i].RawJson = tableSum.JSONTXT;
                dbMap.Tables[i].AISummary = tableSum.AISUMTXT;
            }

        }
        var dbSum = _dbConnector.GetTableSummary("_AIFORBIDBSUM_");
        if (dbSum != null)
        {
            dbMap.RawJson = dbSum.JSONTXT;
            dbMap.AISummary = dbSum.AISUMTXT;
        }
        return dbMap;
    }
    public DatabaseMap SummaryAndIndexDb(bool ForceToAISummary = false)
    {
        if (ForceToAISummary == true)
        {
            _dbConnector.ResetAppTable();
        }
        _dbConnector.CreateAppTables();
        var dbMap = _dbConnector.GetDbMap();
        for (int i = 0; i < dbMap.Tables.Count; i++)
        {
            var tableSum = _dbConnector.GetTableSummary(dbMap.Tables[i].Table.Name!);
            if (tableSum == null || ForceToAISummary == true)
            {
                dbMap.Tables[i].RawJson = AiConnectorUtil.ToCompactJson(dbMap.Tables[i]);

                var system = """
                    Sen bir veri modeli analistisın. Aşağıdaki JSON, bir veritabanındaki TEK bir tablonun
                    kolon ve ilişki bilgilerini içerir (table, tableColumns, tableRelationships).

                    Kurallar:
                    - JSON dışına çıkma; uydurma yapma.
                    - 'Giden' (bu tablo -> parent) ve 'Gelen' (child -> bu tablo) ilişkileri ayrı başlıklarla madde madde yaz.
                    - Her maddede: FK_Adı | [schema].[child] (child kolonları) -> [schema].[parent] (parent kolonları)
                    | ON DELETE/UPDATE: X/Y | Kardinalite ipucu (FK seti unique ise 1-1, değilse 1-n).
                    - Tablo/kolon adlarını [schema].[table] biçiminde kullan.
                    - Başta 1-2 cümle kısa tanım, sonda 10 cümle özet ver. 
                    - Özet harici mutlaka tablonun kolonlarını yaz, ilişkili olduğu tabloları yaz ve nasıl ilişkili olduğunu belirt.
                    """;


                var user = $"""
                    Veritabanı: {_dbConnector.DatabaseName}
                    Hedef tablo: [{_dbConnector.Schema}].[{dbMap.Tables[i].Table.Name}]

                    JSON:
                    ```json
                    {dbMap.Tables[i].RawJson}
                    İstenen çıktı:

                    Kısa tanım (1-2 cümle)

                    Giden ilişkiler (madde madde)

                    Gelen ilişkiler (madde madde)

                    Özet (10 cümle)

                    Tablonun kolonları

                    Tablonun ilişkileri
                    """;
                var answer = olcon.Chat(system, user, new { num_ctx = 131072, num_predict = 1024 });
                if (!string.IsNullOrWhiteSpace(answer))
                    answer = answer.Trim();

                dbMap.Tables[i].AISummary = answer;
                _dbConnector.AddSummaryToDb(new SummaryDto
                {
                    DATABASENAME = _dbConnector.DatabaseName,
                    TABLENAME = dbMap.Tables[i].Table.Name,
                    JSONTXT = dbMap.Tables[i].RawJson,
                    AISUMTXT = dbMap.Tables[i].AISummary
                });
            }
            else
            {
                dbMap.Tables[i].RawJson = tableSum.JSONTXT;
                dbMap.Tables[i].AISummary = tableSum.AISUMTXT;
            }

        }
        var dbSum = _dbConnector.GetTableSummary("_AIFORBIDBSUM_");
        if (dbSum == null || ForceToAISummary == true)
        {
            dbMap.RawJson = AiConnectorUtil.ToCompactJson(dbMap.Tables);
            var allSummary = string.Join("\n\n---\n\n", dbMap.Tables.Select(x => x.AISummary));

            var system = """
                Sen bir veri modeli analistisın. Sana bir veritabanına ait tablolar ilişkiler ve tablo-özetleri yazı içinde verilecek.
                Görev: Sağlanan bilgilere sadık kalarak veritabanının üst düzey dokümantasyonunu yaz. Uzun ve kapsamlı bir döküman olacak.
                Kurallar:
                - Uydurma yapma; yalnızca verilen bilgiyi kullan.
                - Bölümler halinde yaz (başlıklarla):
                1) Mimari genel görünüm (OLTP/OLAP ipuçları, ilişki yoğunluğu)
                2) Konu alanları / domain’ler (tablo grupları)
                3) Merkez tablolar ve ilişki kalıpları (1-n, 1-1 ipuçları)
                4) Veri akışı / yaşam döngüsü (kısa)
                5) Yaygın analitik sorular & rapor metrikleri (kısa maddeler)
                6) Uyarılar / veri kalitesi riskleri (varsa)
                7) Yönetici özeti (1 paragraf)
                - Tablo adlarını [schema].[table] biçiminde kullan.
                """;

            var user = $"""
                Veritabanı ismi: {_dbConnector.DatabaseName}

                Veritabanı tabloları, tabloların kolonları, ilişkileri ve tabloyu anlatan tüm kısa özet:
                ```özetler
                {allSummary}
                Yukarıdaki bilgilere dayanarak veritabanının genel ve detaylı anlatımını üret.
                """;



            var answer = olcon.Chat(system, user, new { num_ctx = 131072, num_predict = 1024 });
            if (!string.IsNullOrWhiteSpace(answer))
                answer = answer.Trim();

            dbMap.AISummary = answer;
            _dbConnector.AddSummaryToDb(new SummaryDto
            {
                DATABASENAME = _dbConnector.DatabaseName,
                TABLENAME = "_AIFORBIDBSUM_",
                JSONTXT = dbMap.RawJson,
                AISUMTXT = dbMap.AISummary
            });
        }
        else
        {
            dbMap.RawJson = dbSum.JSONTXT;
            dbMap.AISummary = dbSum.AISUMTXT;
        }


        for (var i = 0; i < dbMap.Tables.Count; i++)
        {
            var summary = dbMap.Tables[i].AISummary ?? "";
            var vecL = olcon.EmbedText(summary);
            qdcon.CreateCollection(vectorSize: vecL.Length, distance: Distance.Cosine);
            var payloadL = new Dictionary<string, object?>
            {
                ["kind"] = "table_summary",
                ["db"] = dbMap.DatabaseName,
                ["schema"] = dbMap.Tables[i].Table.Schema,
                ["table"] = dbMap.Tables[i].Table.Name,
                ["text"] = summary,
                ["json"] = dbMap.Tables[i].RawJson,              // null ise QdrantConnector zaten eklemiyor
                ["column_count"] = dbMap.Tables[i].TableColumns.Count,
                ["relation_count"] = dbMap.Tables[i].TableRelationships.Count,
                ["updated_at"] = DateTime.UtcNow.ToString("o")
            };

            var idL = AiConnectorUtil.CreateDeterministicGuid($"mssql:{dbMap.DatabaseName}.{dbMap.Tables[i].Table.Schema}.{dbMap.Tables[i].Table.Name}");
            qdcon.Upsert(vecL, payloadL, idL);
        }

        var dbSummary = dbMap.AISummary ?? "";
        var vec = olcon.EmbedText(dbSummary);
        qdcon.CreateCollection(vectorSize: vec.Length, distance: Distance.Cosine);

        var payload = new Dictionary<string, object?>
        {
            ["kind"] = "db_overview",
            ["db"] = dbMap.DatabaseName,
            ["text"] = dbSummary,
            ["all_table_json"] = dbMap.RawJson,
            ["updated_at"] = DateTime.UtcNow.ToString("o")
        };

        var id = AiConnectorUtil.CreateDeterministicGuid($"mssql:{dbMap.DatabaseName}:overview");
        qdcon.Upsert(vec, payload, id);

        return dbMap;
    }

}
