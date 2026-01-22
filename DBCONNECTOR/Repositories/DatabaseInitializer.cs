using Dapper;
using Microsoft.Data.SqlClient;
using DBCONNECTOR.Interfaces;

namespace DBCONNECTOR.Repositories;

/// <summary>
/// SQL Server implementation of IDatabaseInitializer.
/// </summary>
public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public void CreateAppTables()
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIFORBI_DB_SUMMARIES' AND xtype='U')
        BEGIN
            CREATE TABLE AIFORBI_DB_SUMMARIES
            (
                ID INT IDENTITY(1,1) PRIMARY KEY,
                TABLENAME NVARCHAR(200) NOT NULL,
                JSONTXT NVARCHAR(MAX) NULL,
                AISUMTXT NVARCHAR(MAX) NULL,
                DATABASENAME NVARCHAR(200) NULL
            );
        END

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIFORBI_CHAT_HISTORY' AND xtype='U')
        BEGIN
            CREATE TABLE AIFORBI_CHAT_HISTORY
            (
                ID INT IDENTITY(1,1) PRIMARY KEY,
                SESSIONID NVARCHAR(100) NULL,
                ROLE NVARCHAR(50) NULL,
                CONTENT NVARCHAR(MAX) NULL,
                CREATEDAT DATETIME DEFAULT GETDATE(),
                ISHTML BIT DEFAULT 0
            );
        END

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIFORBI_USERS' AND xtype='U')
        BEGIN
            CREATE TABLE AIFORBI_USERS
            (
                ID INT IDENTITY(1,1) PRIMARY KEY,
                EMAIL NVARCHAR(200) NOT NULL UNIQUE,
                PASSWORD_HASH NVARCHAR(200) NOT NULL,
                DISPLAY_NAME NVARCHAR(100) NULL,
                CREATED_AT DATETIME DEFAULT GETDATE()
            );
            INSERT INTO AIFORBI_USERS (EMAIL, PASSWORD_HASH, DISPLAY_NAME)
            VALUES ('admin@admin.com', '123456', 'Admin');
        END

        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIFORBI_CHAT_SESSIONS' AND xtype='U')
        BEGIN
            CREATE TABLE AIFORBI_CHAT_SESSIONS
            (
                ID INT IDENTITY(1,1) PRIMARY KEY,
                SESSION_ID NVARCHAR(100) NOT NULL UNIQUE,
                USER_ID INT NOT NULL,
                TITLE NVARCHAR(200) NULL,
                CREATED_AT DATETIME DEFAULT GETDATE(),
                FOREIGN KEY (USER_ID) REFERENCES AIFORBI_USERS(ID)
            );
        END
        ";

        conn.Execute(sql);
    }

    public void ResetAppTable()
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"DELETE FROM AIFORBI_DB_SUMMARIES";

        conn.Execute(sql);
    }
}
