using AICONNECTOR;
using AICONNECTOR.Connectors;
using DBCONNECTOR.Connectors;
using DBCONNECTOR.Dtos.Mssql;
using Qdrant.Client.Grpc;

namespace AIFORBI.Services;

public class SettingsService
{
    public MssqlConnector mssqlConnector { get; set; }
    public OllamaConnector olcon { get; set; }
    public QdrantConnector qdcon { get; set; }
    public SettingsService()
    {
        mssqlConnector = new MssqlConnector(AppConfig.Configuration["ConnStrs:Mssql:ConnStr"], AppConfig.Configuration["ConnStrs:Mssql:DatabaseName"], AppConfig.Configuration["ConnStrs:Mssql:Schema"]);
        olcon = new OllamaConnector(AppConfig.Configuration["ConnStrs:Ollama:BaseUrl"], "nomic-embed-text", "qwen2.5-coder:7b");
        qdcon = new QdrantConnector(AppConfig.Configuration["ConnStrs:Qdrant:Host"], Convert.ToInt32(AppConfig.Configuration["ConnStrs:Qdrant:Grpc"]), "db_maps");

    }
    public MssqlDatabaseMap GetDbMapFast()
    {
        var dbMap = mssqlConnector.GetDbMap();
        for (int i = 0; i < dbMap.Tables.Count; i++)
        {
            var tableSum = mssqlConnector.GetTableSummary(dbMap.Tables[i].Table.Name);
            if (tableSum != null)
            {
                dbMap.Tables[i].RawJson = tableSum.JSONTXT;
                dbMap.Tables[i].AISummary = tableSum.AISUMTXT;
            }

        }
        var dbSum = mssqlConnector.GetTableSummary("_AIFORBIDBSUM_");
        if (dbSum != null)
        {
            dbMap.RawJson = dbSum.JSONTXT;
            dbMap.AISummary = dbSum.AISUMTXT;
        }
        return dbMap;
    }
    public MssqlDatabaseMap SummaryAndIndexDb(bool ForceToAISummary = false)
    {
        if (ForceToAISummary == true)
        {
            mssqlConnector.ResetAppTable();
        }
        mssqlConnector.CreateAppTables();
        var dbMap = mssqlConnector.GetDbMap();
        for (int i = 0; i < dbMap.Tables.Count; i++)
        {
            var tableSum = mssqlConnector.GetTableSummary(dbMap.Tables[i].Table.Name);
            if (tableSum == null || ForceToAISummary == true)
            {
                dbMap.Tables[i].RawJson = AiConnectorUtil.ToCompactJson(dbMap.Tables[i]);

                var system = """
                    Sen bir veri modeli analistisın. Aşağıdaki JSON, MSSQL'de bir veritabanındaki TEK bir tablonun
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
                    Veritabanı: {mssqlConnector._DatabaseName}
                    Hedef tablo: [{mssqlConnector._Schema}].[{dbMap.Tables[i].Table.Name}]

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
                mssqlConnector.AddSummaryToDb(new MssqlSummaryDto
                {
                    DATABASENAME = mssqlConnector._DatabaseName,
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
        var dbSum = mssqlConnector.GetTableSummary("_AIFORBIDBSUM_");
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
                Veritabanı ismi: {mssqlConnector._DatabaseName}

                Veritabanı tabloları, tabloların kolonları, ilişkileri ve tabloyu anlatan tüm kısa özet:
                ```özetler
                {allSummary}
                Yukarıdaki bilgilere dayanarak veritabanının genel ve detaylı anlatımını üret.
                """;



            var answer = olcon.Chat(system, user, new { num_ctx = 131072, num_predict = 1024 });
            if (!string.IsNullOrWhiteSpace(answer))
                answer = answer.Trim();

            dbMap.AISummary = answer;
            mssqlConnector.AddSummaryToDb(new MssqlSummaryDto
            {
                DATABASENAME = mssqlConnector._DatabaseName,
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
            var vecL = olcon.EmbedText(dbMap.Tables[i].AISummary);
            qdcon.CreateCollection(vectorSize: vecL.Length, distance: Distance.Cosine);
            var payloadL = new Dictionary<string, object?>
            {
                ["kind"] = "table_summary",
                ["db"] = dbMap.DatabaseName,
                ["schema"] = dbMap.Tables[i].Table.Schema,
                ["table"] = dbMap.Tables[i].Table.Name,
                ["text"] = dbMap.Tables[i].AISummary,
                ["json"] = dbMap.Tables[i].RawJson,              // null ise QdrantConnector zaten eklemiyor
                ["column_count"] = dbMap.Tables[i].TableColumns.Count,
                ["relation_count"] = dbMap.Tables[i].TableRelationships.Count,
                ["updated_at"] = DateTime.UtcNow.ToString("o")
            };

            var idL = AiConnectorUtil.CreateDeterministicGuid($"mssql:{dbMap.DatabaseName}.{dbMap.Tables[i].Table.Schema}.{dbMap.Tables[i].Table.Name}");
            qdcon.Upsert(vecL, payloadL, idL);
        }

        var vec = olcon.EmbedText(dbMap.AISummary);
        qdcon.CreateCollection(vectorSize: vec.Length, distance: Distance.Cosine);

        var payload = new Dictionary<string, object?>
        {
            ["kind"] = "db_overview",
            ["db"] = dbMap.DatabaseName,
            ["text"] = dbMap.AISummary,
            ["all_table_json"] = dbMap.RawJson,
            ["updated_at"] = DateTime.UtcNow.ToString("o")
        };

        var id = AiConnectorUtil.CreateDeterministicGuid($"mssql:{dbMap.DatabaseName}:overview");
        qdcon.Upsert(vec, payload, id);

        return dbMap;
    }

}
