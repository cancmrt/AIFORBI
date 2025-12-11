namespace DBCONNECTOR.Dtos.Mssql;

public class MssqlTableMap
{
    public MssqlTableDto Table { get; set; }
    public List<MssqlColumnDto> TableColumns { get; set; }
    public List<MssqlRelationshipDto> TableRelationships { get; set; }

    public string RawJson { get; set; }
    public string AISummary { get; set; }
}