using DBCONNECTOR.Dtos.Common;

namespace DBCONNECTOR.Interfaces;

/// <summary>
/// Interface for database connectors. All database implementations must implement this interface.
/// </summary>
public interface IDbConnector
{
    /// <summary>
    /// Database name
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Schema name (if applicable)
    /// </summary>
    string? Schema { get; }

    /// <summary>
    /// Get all tables in the database
    /// </summary>
    List<TableDto> GetDbTables(bool includeViews);

    /// <summary>
    /// Get all columns for a specific table
    /// </summary>
    List<ColumnDto> GetDbTableColumns(TableDto table);

    /// <summary>
    /// Get all relationships for a specific table
    /// </summary>
    List<RelationshipDto> GetDbTableRelations(TableDto table);

    /// <summary>
    /// Get the complete database map (tables, columns, relationships)
    /// </summary>
    DatabaseMap GetDbMap();

    /// <summary>
    /// Create application-specific tables (summaries, chat history, users, sessions)
    /// </summary>
    void CreateAppTables();

    /// <summary>
    /// Reset/clear the summary table
    /// </summary>
    void ResetAppTable();

    /// <summary>
    /// Execute raw SQL and return results as JSON
    /// </summary>
    string ExecuteRawSqlToJson(string rawSql);

    /// <summary>
    /// Add a summary record to the database
    /// </summary>
    SummaryDto AddSummaryToDb(SummaryDto summary);

    /// <summary>
    /// Get summary for a specific table
    /// </summary>
    SummaryDto? GetTableSummary(string tableName);
}
