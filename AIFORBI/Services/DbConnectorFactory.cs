using DBCONNECTOR.Connectors;
using DBCONNECTOR.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AIFORBI.Services;

public static class DbConnectorFactory
{
    public static IDbConnector Create(IConfiguration configuration)
    {
        var connectorType = configuration["ConnStrs:DbConnector:Type"] ?? "Mssql";
        
        switch (connectorType.ToLower())
        {
            case "mssql":
                var mssqlConnStr = configuration["ConnStrs:DbConnector:Mssql:ConnStr"] 
                    ?? throw new InvalidOperationException("Mssql connection string is missing.");
                var mssqlDbName = configuration["ConnStrs:DbConnector:Mssql:DatabaseName"] ?? "";
                var mssqlSchema = configuration["ConnStrs:DbConnector:Mssql:Schema"] ?? "dbo";
                return new MssqlConnector(mssqlConnStr, mssqlDbName, mssqlSchema);
            
            // Future connectors can be added here
            // case "postgresql":
            //     return new PostgresqlConnector(...);
                
            default:
                throw new NotSupportedException($"Database connector type '{connectorType}' is not supported.");
        }
    }
}
