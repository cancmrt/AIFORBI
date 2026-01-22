namespace DBCONNECTOR.Dtos.Common;

public class TableMap
{
    public TableDto Table { get; set; } = null!;
    public List<ColumnDto> TableColumns { get; set; } = new();
    public List<RelationshipDto> TableRelationships { get; set; } = new();
    public string? RawJson { get; set; }
    public string? AISummary { get; set; }
}
