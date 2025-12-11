
namespace DBCONNECTOR.Dtos.Mssql;

public class MssqlDatabaseMap
{
    public string DatabaseName { get; set; }
    public List<MssqlTableMap> Tables { get; set; }
    public string RawJson { get; set; }
    public string AISummary { get; set; }
}