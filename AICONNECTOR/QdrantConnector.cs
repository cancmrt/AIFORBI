
using Qdrant.Client;
using Qdrant.Client.Grpc;


using QValue     = Qdrant.Client.Grpc.Value;
using QStruct    = Qdrant.Client.Grpc.Struct;
using QListValue = Qdrant.Client.Grpc.ListValue;
using QNull      = Qdrant.Client.Grpc.NullValue;

namespace AICONNECTOR.Connectors;
public class QdrantConnector
{

    private string _Host { get; set; }
    private string _Collection { get; set; }
    private int _GrpcPort { get; set; }

    private QdrantClient _client;

    public QdrantConnector(string Host, int GrpcPort, string Collection)
    {
        _Host = Host;
        _GrpcPort = GrpcPort;
        _Collection = Collection;
        _client = new QdrantClient(_Host, _GrpcPort);

    }
    public void CreateCollection(int vectorSize, Distance distance = Distance.Cosine)
    {
        var exists = _client.CollectionExistsAsync(_Collection).ConfigureAwait(false).GetAwaiter().GetResult();
        if (!exists)
        {
            _client.CreateCollectionAsync(_Collection, new VectorParams
            {
                Size = (uint)vectorSize,
                Distance = distance
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
    public void RecreateCollection(int vectorSize, Distance distance = Distance.Cosine)
    {
        var exists =  _client.CollectionExistsAsync(_Collection).ConfigureAwait(false).GetAwaiter().GetResult();
        if (exists)  _client.DeleteCollectionAsync(_Collection).ConfigureAwait(false).GetAwaiter().GetResult();
        _client.CreateCollectionAsync(_Collection, new VectorParams
        {
            Size = (uint)vectorSize,
            Distance = distance
        }).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    public void Upsert(float[] vector, IDictionary<string, object?> payload, Guid id)
    {
        var point = new PointStruct
        {
            Id = id,
            Vectors = vector,
            Payload = { ToPayload(payload) }
        };
        _client.UpsertAsync(_Collection, new[] { point }).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    public void UpsertBatch(List<PointStruct> items)
    {

        if (items.Count > 0)
            _client.UpsertAsync(_Collection, items).ConfigureAwait(false).GetAwaiter().GetResult();
    }
    public IReadOnlyList<ScoredPoint> Search(float[] vector, int topK, Filter? filter = null)
    {
        var res = _client.SearchAsync(
            _Collection,
            vector,
            limit: (uint)topK,
            filter: filter
        ).ConfigureAwait(false).GetAwaiter().GetResult();

        return res;
    }
    private static IDictionary<string, QValue> ToPayload(IDictionary<string, object?> src)
    {
        var dst = new Dictionary<string, QValue>(StringComparer.Ordinal);
        foreach (var (k, v) in src)
        {
            if (v is null) continue;              // null ise payload’a EKLEME
            dst[k] = ToValue(v);
        }
        return dst;
    }

    private static QValue ToValue(object v)
    {
        switch (v)
        {
            case string s:   return new QValue { StringValue  = s };
            case bool b:     return new QValue { BoolValue    = b };

            // tamsayıları IntegerValue'ya koy
            case byte i8:    return new QValue { IntegerValue = i8 };
            case sbyte si8:  return new QValue { IntegerValue = si8 };
            case short i16:  return new QValue { IntegerValue = i16 };
            case ushort ui16:return new QValue { IntegerValue = ui16 };
            case int i32:    return new QValue { IntegerValue = i32 };
            case uint ui32:  return new QValue { IntegerValue = ui32 };
            case long i64:   return new QValue { IntegerValue = i64 };
            case ulong ui64: return new QValue { IntegerValue = unchecked((long)ui64) }; // dikkat: sınır aşımı olabilir

            // kayan nokta/döviz tiplerini DoubleValue'ya koy
            case float f:    return new QValue { DoubleValue  = f };
            case double d:   return new QValue { DoubleValue  = d };
            case decimal m:  return new QValue { DoubleValue  = (double)m };

            // bazı yaygın özel tipleri string'e çevir
            case DateTime dt: return new QValue { StringValue = dt.ToString("o") }; // ISO-8601
            case Guid g:      return new QValue { StringValue = g.ToString() };

            // Liste -> QListValue; iç öğeleri yine ToValue ile QValue'ya çevir
            case IEnumerable<object?> objList:
            {
                var list = new QListValue();
                foreach (var item in objList)
                {
                    if (item is null) continue;
                    list.Values.Add(ToValue(item));
                }
                return new QValue { ListValue = list };
            }
            // Sözlük -> QStruct
            case IDictionary<string, object?> dict:
            {
                var st = new QStruct();
                foreach (var kv in dict)
                {
                    if (kv.Value is null) continue;
                    st.Fields[kv.Key] = ToValue(kv.Value);
                }
                return new QValue { StructValue = st };
            }

            default:
                return new QValue { StringValue = v.ToString() ?? string.Empty };
        }
    }

}