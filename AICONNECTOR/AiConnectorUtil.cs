using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Qdrant.Client.Grpc;
using Match = Qdrant.Client.Grpc.Match;

namespace AICONNECTOR;

public static class AiConnectorUtil
{
    public static string ToCompactJson(object o)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
        return JsonSerializer.Serialize(o, opts);
    }
    public static Guid CreateDeterministicGuid(string name)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(name));
        Span<byte> g = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(g);
        g[6] = (byte)((g[6] & 0x0F) | (5 << 4)); // version 5
        g[8] = (byte)((g[8] & 0x3F) | 0x80);     // variant
        return new Guid(g);
    }
    public static Filter BuildEqualsFilter(params (string key, string value)[] pairs)
    {
        var f = new Filter();
        foreach (var (k, v) in pairs)
        {
            f.Must.Add(new Condition
            {
                Field = new FieldCondition
                {
                    Key = k,
                    Match = new Match { Keyword = v }
                }
            });
        }
        return f;
    }
    public static string CleanRawSql(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string sql = input;

        // 1. Kod bloğu işaretlerini kaldır (```sql, ```, ```SQL, vb.)
        sql = Regex.Replace(sql, @"```[\s\S]*?sql", "", RegexOptions.IgnoreCase);
        sql = sql.Replace("```", "");

        // 2. “SQL:” veya “Query:” gibi önekleri kaldır
        sql = Regex.Replace(sql, @"(?i)\b(sql|query)\s*:\s*", "", RegexOptions.IgnoreCase);

        // 3. Tek satırlık açıklamaları kaldır (-- ile başlayanlar)
        sql = Regex.Replace(sql, @"--.*", "", RegexOptions.Multiline);

        // 4. Çok satırlı açıklamaları kaldır (/* ... */)
        sql = Regex.Replace(sql, @"/\*[\s\S]*?\*/", "", RegexOptions.Multiline);

        // 5. Çift tırnak, tek tırnak içinde code block veya açıklama varsa temizle
        sql = sql.Replace("\"", "").Replace("'", "");

        // 6. Satır başı, sekme, fazla boşlukları normalize et
        sql = Regex.Replace(sql, @"\s+", " ");

        // 7. Baş ve sondaki boşlukları kırp
        sql = sql.Trim();

        return sql;
    }
}