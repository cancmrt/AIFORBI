
namespace DBCONNECTOR.Dtos.Mssql;

public class MssqlRelationshipDto
{
    public string Direction { get; set; } = "";          // "Outgoing" | "Incoming"
    public string ForeignKey { get; set; } = "";

    public string ChildSchema { get; set; } = "";
    public string ChildTable { get; set; } = "";
    public string ChildColumns { get; set; } = "";       // "Col1, Col2" (gerekirse split edersin)

    public string ParentSchema { get; set; } = "";
    public string ParentTable { get; set; } = "";
    public string ParentColumns { get; set; } = "";

    public string OnDelete { get; set; } = "";           // NO_ACTION | CASCADE | SET_NULL | SET_DEFAULT
    public string OnUpdate { get; set; } = "";

    public bool IsDisabled { get; set; }
    public bool IsNotTrusted { get; set; }

    public bool ChildColumnsNullableAny { get; set; }    // FK kolonlarından en az biri nullable mı?
    public bool ChildColumnsHaveUniqueIndex { get; set; }// FK kolonu seti tam olarak unique index ile kaplı mı?
    public string? ChildCoveringIndexName { get; set; }  // FK kolonu setini birebir kaplayan bir index adı (varsa)
}