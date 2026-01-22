namespace DBCONNECTOR.Interfaces;

/// <summary>
/// Interface for database initialization operations.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Creates application tables if they don't exist.
    /// </summary>
    void CreateAppTables();

    /// <summary>
    /// Resets/clears application data tables.
    /// </summary>
    void ResetAppTable();
}
