namespace DBCONNECTOR.Dtos.Mssql;
public class MssqlSummaryDto
{
    public int ID { get; set; }
    public string TABLENAME { get; set; }
    public string JSONTXT { get; set; }
    public string AISUMTXT { get; set; }
    public string DATABASENAME { get; set; }
}