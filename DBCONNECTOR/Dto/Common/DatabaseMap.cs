namespace DBCONNECTOR.Dtos.Common;

public class DatabaseMap
{
    public string? DatabaseName { get; set; }
    public List<TableMap> Tables { get; set; } = new();
    public string? RawJson { get; set; }
    public string? AISummary { get; set; }
}
