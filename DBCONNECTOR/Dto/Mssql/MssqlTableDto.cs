
namespace DBCONNECTOR.Dtos.Mssql;

public class MssqlTableDto
{
    public string Catalog { get; set; } = "";
    public string Schema { get; set; } = "";
    public string Name { get; set; } = "";
    public string TableType { get; set; } = ""; // BASE TABLE / VIEW
}