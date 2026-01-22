
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DBCONNECTOR.Dtos.Common;
using DBCONNECTOR.Dtos.Mssql;
using DBCONNECTOR.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace DBCONNECTOR.Connectors;

public class MssqlConnector : IDbConnector
{
    private string _DbConnectionString { get; set; }
    public string _DatabaseName { get; set; }
    public string? _Schema { get; set; }

    // IDbConnector interface properties
    public string DatabaseName => _DatabaseName;
    public string? Schema => _Schema;

    public MssqlConnector(string DbConnectionString, string DatabaseName, string Schema)
    {
        _DbConnectionString = DbConnectionString;
        _DatabaseName = DatabaseName;
        _Schema = Schema;
    }

    public List<TableDto> GetDbTables(bool includeViews)
    {

        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = $@"
        SELECT
        t.TABLE_CATALOG  AS [Catalog],
        t.TABLE_SCHEMA   AS [Schema],
        t.TABLE_NAME     AS [Name],
        t.TABLE_TYPE     AS [TableType]
        FROM INFORMATION_SCHEMA.TABLES t
        WHERE 1=1 AND t.TABLE_NAME != 'AIFORBI_DB_SUMMARIES'
        {(includeViews ? "" : "AND t.TABLE_TYPE = 'BASE TABLE'")}
        {(string.IsNullOrWhiteSpace(_Schema) ? "" : "AND t.TABLE_SCHEMA = @schema")}
        ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME;";

        var list = conn.Query<TableDto>(
            new CommandDefinition(sql, new { schema = _Schema })).ToList();

        return list;
    }
    public List<ColumnDto> GetDbTableColumns(TableDto table)
    {

        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
                    SELECT
                    Catalog         = DB_NAME(),
                    ObjectType      = CASE o.[type] WHEN 'U' THEN 'TABLE' WHEN 'V' THEN 'VIEW' ELSE o.[type] END,
                    [Schema]        = s.name,
                    [Table]         = o.name,
                    [Column]        = c.name,
                    OrdinalPosition = c.column_id,

                    DataType        = ty.name,
                    FullDataType    =
                        CASE
                        WHEN ty.name IN ('nvarchar','nchar')
                            THEN ty.name + '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length/2 AS varchar(10)) END + ')'
                        WHEN ty.name IN ('varchar','char','varbinary','binary')
                            THEN ty.name + '(' + CASE WHEN c.max_length = -1 THEN 'max' ELSE CAST(c.max_length AS varchar(10)) END + ')'
                        WHEN ty.name IN ('decimal','numeric')
                            THEN ty.name + '(' + CAST(c.[precision] AS varchar(10)) + ',' + CAST(c.scale AS varchar(10)) + ')'
                        WHEN ty.name IN ('datetime2','time','datetimeoffset')
                            THEN ty.name + '(' + CAST(c.scale AS varchar(10)) + ')'
                        ELSE ty.name
                        END,

                    MaxLength       = c.max_length,
                    [Precision]     = c.[precision],
                    [Scale]         = c.scale,

                    IsNullable      = CONVERT(bit, c.is_nullable),

                    -- VIEW'lerde default/identity olmaz → zaten NULL/0 döner
                    DefaultDefinition = dc.definition,

                    IsIdentity      = CONVERT(bit, c.is_identity),
                    IdentitySeed      = (SELECT TOP 1 CONVERT(varchar(100), ic.seed_value)
                                        FROM sys.identity_columns ic
                                        WHERE ic.object_id = c.object_id AND ic.column_id = c.column_id),
                    IdentityIncrement = (SELECT TOP 1 CONVERT(varchar(100), ic.increment_value)
                                        FROM sys.identity_columns ic
                                        WHERE ic.object_id = c.object_id AND ic.column_id = c.column_id),

                    -- VIEW kolonları depolanmadığı için genellikle computed sayılır; definition çoğu senaryoda NULL kalır.
                    IsComputed      = CONVERT(bit, c.is_computed),
                    ComputedDefinition = cc.definition,

                    CollationName   = c.collation_name,

                    IsRowGuidCol    = CONVERT(bit, COLUMNPROPERTY(c.object_id, c.name, 'IsRowGuidCol')),
                    IsSparse        = CONVERT(bit, COLUMNPROPERTY(c.object_id, c.name, 'IsSparse')),
                    IsHidden        = CONVERT(bit, COLUMNPROPERTY(c.object_id, c.name, 'IsHidden')),

                    -- PK/Unique: VIEW'de PK olmaz; ama indexed view'da unique index = true olabilir
                    IsPrimaryKey = CONVERT(bit,
                        CASE WHEN EXISTS (
                            SELECT 1
                            FROM sys.indexes i
                            JOIN sys.index_columns ix ON i.object_id = ix.object_id AND i.index_id = ix.index_id
                            WHERE i.object_id = o.object_id
                            AND ix.column_id = c.column_id
                            AND i.is_primary_key = 1
                        ) THEN 1 ELSE 0 END),

                    IsUnique = CONVERT(bit,
                        CASE WHEN EXISTS (
                            SELECT 1
                            FROM sys.indexes i
                            JOIN sys.index_columns ix ON i.object_id = ix.object_id AND i.index_id = ix.index_id
                            WHERE i.object_id = o.object_id
                            AND ix.column_id = c.column_id
                            AND i.is_unique = 1
                        ) THEN 1 ELSE 0 END),

                    -- VIEW'lerde FK olmaz → her zaman 0/NULL
                    IsForeignKey = CONVERT(bit,
                        CASE WHEN EXISTS (
                            SELECT 1 FROM sys.foreign_key_columns fkc
                            WHERE fkc.parent_object_id = o.object_id AND fkc.parent_column_id = c.column_id
                        ) THEN 1 ELSE 0 END),

                    ForeignKeyName = (
                        SELECT TOP 1 fk.name
                        FROM sys.foreign_key_columns fkc
                        JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
                        WHERE fkc.parent_object_id = o.object_id AND fkc.parent_column_id = c.column_id
                    ),
                    ForeignKeyRef = (
                        SELECT TOP 1 rs.name + '.' + rt.name + '(' + rc.name + ')'
                        FROM sys.foreign_key_columns fkc
                        JOIN sys.tables rt   ON rt.object_id = fkc.referenced_object_id
                        JOIN sys.schemas rs  ON rs.schema_id = rt.schema_id
                        JOIN sys.columns rc  ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
                        WHERE fkc.parent_object_id = o.object_id AND fkc.parent_column_id = c.column_id
                    ),

                    -- Extended property: VIEW kolonlarına da verilebilir (major_id=view obj id, minor_id=column_id)
                    Description = (
                        SELECT TOP 1 CAST(ep.[value] AS nvarchar(max))
                        FROM sys.extended_properties ep
                        WHERE ep.major_id = c.object_id AND ep.minor_id = c.column_id AND ep.name = 'MS_Description'
                    )

                    FROM sys.objects o
                    JOIN sys.schemas s  ON s.schema_id = o.schema_id
                    JOIN sys.columns c  ON c.object_id = o.object_id
                    JOIN sys.types   ty ON ty.user_type_id = c.user_type_id
                    LEFT JOIN sys.default_constraints dc
                    ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                    LEFT JOIN sys.computed_columns cc
                    ON cc.object_id = c.object_id AND cc.column_id = c.column_id
                    WHERE
                    o.[type] IN ('U','V')                          -- TABLE + VIEW
                    AND (@schema IS NULL OR s.name = @schema)
                    AND (@table  IS NULL OR o.name = @table)
                    ORDER BY s.name, o.name, c.column_id;";

        var rows = conn.Query<ColumnDto>(
            new CommandDefinition(
                sql,
                new { schema = table.Schema, table = table.Name }
            )).ToList();

        return rows;
    }
    public List<RelationshipDto> GetDbTableRelations(TableDto table)
    {

        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();
        var sql = @"
                DECLARE @schema sysname = @p_schema;
                DECLARE @table  sysname = @p_table;

                WITH focus AS (
                    SELECT t.object_id AS obj_id
                    FROM sys.tables t
                    JOIN sys.schemas s ON s.schema_id = t.schema_id
                    WHERE s.name = @schema AND t.name = @table
                )
                -- OUTGOING: this table (parent_object_id) -> referenced table
                SELECT
                Direction = 'Outgoing',
                ForeignKey = fk.name,

                ChildSchema = sch1.name,
                ChildTable  = t1.name,
                ChildColumns =
                    (SELECT STRING_AGG(c1.name, ', ')
                    FROM sys.foreign_key_columns fkc1
                    JOIN sys.columns c1
                    ON c1.object_id = fkc1.parent_object_id AND c1.column_id = fkc1.parent_column_id
                    WHERE fkc1.constraint_object_id = fk.object_id),

                ParentSchema = sch2.name,
                ParentTable  = t2.name,
                ParentColumns =
                    (SELECT STRING_AGG(c2.name, ', ')
                    FROM sys.foreign_key_columns fkc2
                    JOIN sys.columns c2
                    ON c2.object_id = fkc2.referenced_object_id AND c2.column_id = fkc2.referenced_column_id
                    WHERE fkc2.constraint_object_id = fk.object_id),

                OnDelete = fk.delete_referential_action_desc,
                OnUpdate = fk.update_referential_action_desc,

                IsDisabled = fk.is_disabled,
                IsNotTrusted = fk.is_not_trusted,

                ChildColumnsNullableAny =
                    CASE WHEN EXISTS (
                    SELECT 1
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns cc
                        ON cc.object_id = fkc.parent_object_id
                    AND cc.column_id = fkc.parent_column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                        AND cc.is_nullable = 1
                    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,

                -- FK kolonu setini birebir kapsayan UNIQUE index var mı?
                ChildColumnsHaveUniqueIndex =
                    CASE WHEN EXISTS (
                    SELECT 1
                    FROM sys.indexes i
                    WHERE i.object_id = t1.object_id
                        AND i.is_unique = 1
                        AND NOT EXISTS ( -- FK kolonlarından herhangi biri index key'lerinde yoksa ELER
                        SELECT 1
                        FROM sys.foreign_key_columns f2
                        WHERE f2.constraint_object_id = fk.object_id
                            AND NOT EXISTS (
                            SELECT 1
                            FROM sys.index_columns ix
                            WHERE ix.object_id = i.object_id
                                AND ix.index_id  = i.index_id
                                AND ix.is_included_column = 0
                                AND ix.column_id = f2.parent_column_id
                            )
                        )
                        AND ( -- index key kolonu sayısı == FK kolonu sayısı (tam eşleşme)
                        SELECT COUNT(*)
                        FROM sys.index_columns ix
                        WHERE ix.object_id = i.object_id
                            AND ix.index_id  = i.index_id
                            AND ix.is_included_column = 0
                        ) = (
                        SELECT COUNT(*)
                        FROM sys.foreign_key_columns f3
                        WHERE f3.constraint_object_id = fk.object_id
                        )
                    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,

                -- FK setini birebir kapsayan (unique olmak zorunda değil) bir index adı (varsa)
                ChildCoveringIndexName =
                    (SELECT TOP 1 i2.name
                    FROM sys.indexes i2
                    WHERE i2.object_id = t1.object_id
                    AND NOT EXISTS (
                        SELECT 1
                        FROM sys.foreign_key_columns f2
                        WHERE f2.constraint_object_id = fk.object_id
                        AND NOT EXISTS (
                            SELECT 1
                            FROM sys.index_columns ix2
                            WHERE ix2.object_id = i2.object_id
                            AND ix2.index_id  = i2.index_id
                            AND ix2.is_included_column = 0
                            AND ix2.column_id = f2.parent_column_id
                        )
                    )
                    AND (SELECT COUNT(*) FROM sys.index_columns ix3
                            WHERE ix3.object_id = i2.object_id
                            AND ix3.index_id  = i2.index_id
                            AND ix3.is_included_column = 0)
                        = (SELECT COUNT(*) FROM sys.foreign_key_columns f3
                            WHERE f3.constraint_object_id = fk.object_id)
                    ORDER BY i2.is_unique DESC, i2.name ASC)

                FROM focus f
                JOIN sys.foreign_keys fk ON fk.parent_object_id = f.obj_id
                JOIN sys.tables t1       ON t1.object_id = fk.parent_object_id
                JOIN sys.schemas sch1    ON sch1.schema_id = t1.schema_id
                JOIN sys.tables t2       ON t2.object_id = fk.referenced_object_id
                JOIN sys.schemas sch2    ON sch2.schema_id = t2.schema_id

                UNION ALL

                -- INCOMING: other table (parent_object_id) -> this table (referenced)
                SELECT
                Direction = 'Incoming',
                ForeignKey = fk.name,

                ChildSchema = sch1.name,
                ChildTable  = t1.name,
                ChildColumns =
                    (SELECT STRING_AGG(c1.name, ', ')
                    FROM sys.foreign_key_columns fkc1
                    JOIN sys.columns c1
                    ON c1.object_id = fkc1.parent_object_id AND c1.column_id = fkc1.parent_column_id
                    WHERE fkc1.constraint_object_id = fk.object_id),

                ParentSchema = sch2.name,
                ParentTable  = t2.name,
                ParentColumns =
                    (SELECT STRING_AGG(c2.name, ', ')
                    FROM sys.foreign_key_columns fkc2
                    JOIN sys.columns c2
                    ON c2.object_id = fkc2.referenced_object_id AND c2.column_id = fkc2.referenced_column_id
                    WHERE fkc2.constraint_object_id = fk.object_id),

                OnDelete = fk.delete_referential_action_desc,
                OnUpdate = fk.update_referential_action_desc,

                IsDisabled = fk.is_disabled,
                IsNotTrusted = fk.is_not_trusted,

                ChildColumnsNullableAny =
                    CASE WHEN EXISTS (
                    SELECT 1
                    FROM sys.foreign_key_columns fkc
                    JOIN sys.columns cc
                        ON cc.object_id = fkc.parent_object_id
                    AND cc.column_id = fkc.parent_column_id
                    WHERE fkc.constraint_object_id = fk.object_id
                        AND cc.is_nullable = 1
                    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,

                ChildColumnsHaveUniqueIndex =
                    CASE WHEN EXISTS (
                    SELECT 1
                    FROM sys.indexes i
                    WHERE i.object_id = t1.object_id
                        AND i.is_unique = 1
                        AND NOT EXISTS (
                        SELECT 1
                        FROM sys.foreign_key_columns f2
                        WHERE f2.constraint_object_id = fk.object_id
                            AND NOT EXISTS (
                            SELECT 1
                            FROM sys.index_columns ix
                            WHERE ix.object_id = i.object_id
                                AND ix.index_id  = i.index_id
                                AND ix.is_included_column = 0
                                AND ix.column_id = f2.parent_column_id
                            )
                        )
                        AND (
                        SELECT COUNT(*)
                        FROM sys.index_columns ix
                        WHERE ix.object_id = i.object_id
                            AND ix.index_id  = i.index_id
                            AND ix.is_included_column = 0
                        ) = (
                        SELECT COUNT(*)
                        FROM sys.foreign_key_columns f3
                        WHERE f3.constraint_object_id = fk.object_id
                        )
                    ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END,

                ChildCoveringIndexName =
                    (SELECT TOP 1 i2.name
                    FROM sys.indexes i2
                    WHERE i2.object_id = t1.object_id
                    AND NOT EXISTS (
                        SELECT 1
                        FROM sys.foreign_key_columns f2
                        WHERE f2.constraint_object_id = fk.object_id
                        AND NOT EXISTS (
                            SELECT 1
                            FROM sys.index_columns ix2
                            WHERE ix2.object_id = i2.object_id
                            AND ix2.index_id  = i2.index_id
                            AND ix2.is_included_column = 0
                            AND ix2.column_id = f2.parent_column_id
                        )
                    )
                    AND (SELECT COUNT(*) FROM sys.index_columns ix3
                            WHERE ix3.object_id = i2.object_id
                            AND ix3.index_id  = i2.index_id
                            AND ix3.is_included_column = 0)
                        = (SELECT COUNT(*) FROM sys.foreign_key_columns f3
                            WHERE f3.constraint_object_id = fk.object_id)
                    ORDER BY i2.is_unique DESC, i2.name ASC)

                FROM focus f
                JOIN sys.foreign_keys fk ON fk.referenced_object_id = f.obj_id
                JOIN sys.tables t1       ON t1.object_id = fk.parent_object_id
                JOIN sys.schemas sch1    ON sch1.schema_id = t1.schema_id
                JOIN sys.tables t2       ON t2.object_id = fk.referenced_object_id
                JOIN sys.schemas sch2    ON sch2.schema_id = t2.schema_id

                ORDER BY Direction, ChildSchema, ChildTable, ForeignKey;";

        var list = conn.Query<RelationshipDto>(
    new CommandDefinition(sql, new { p_schema = table.Schema, p_table = table.Name })).ToList();

        return list;

    }
    public DatabaseMap GetDbMap()
    {
        var dbMap = new DatabaseMap();
        dbMap.DatabaseName = _DatabaseName;
        dbMap.Tables = new List<TableMap>();
        var tables = GetDbTables(true);
        if (tables != null && tables.Count > 0)
        {
            for (var i = 0; i < tables.Count; i++)
            {
                var table = tables[i];
                var tableColumns = GetDbTableColumns(table);
                var tableRelationships = GetDbTableRelations(table);
                dbMap.Tables.Add(new TableMap
                {
                    Table = table,
                    TableColumns = tableColumns,
                    TableRelationships = tableRelationships
                });
            }
        }
        return dbMap;
    }
    public void CreateAppTables()
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
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
        conn.Close();
    }

    public void ResetAppTable()
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
        DELETE FROM AIFORBI_DB_SUMMARIES
        ";

        conn.Execute(sql);
        conn.Close();
    }

    public string ExecuteRawSqlToJson(string RawSql)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var rows = conn.Query(RawSql);
        conn.Close();

        var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            WriteIndented = false
        });

        return json;

    }

    public SummaryDto AddSummaryToDb(SummaryDto summary)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            INSERT INTO AIFORBI_DB_SUMMARIES (TABLENAME, JSONTXT, AISUMTXT, DATABASENAME)
            VALUES (@TABLENAME, @JSONTXT, @AISUMTXT, @DATABASENAME);
            
            SELECT CAST(SCOPE_IDENTITY() as int);
        ";

        // ExecuteScalar<int> ile dönen ID’yi alıyoruz
        var newId = conn.ExecuteScalar<int>(sql, summary);
        summary.ID = newId; // DTO’ya atayabilirsin

        return summary;
    }
    public SummaryDto? GetTableSummary(string tablename)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"SELECT ID, TABLENAME, JSONTXT, AISUMTXT, DATABASENAME 
                    FROM AIFORBI_DB_SUMMARIES
                    WHERE TABLENAME = @tablename AND DATABASENAME = @dbname";

        return conn.QueryFirstOrDefault<SummaryDto>(sql, new { tablename = tablename, dbname = _DatabaseName });
    }

    public void AddChatHistory(ChatHistoryDto chat)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            INSERT INTO AIFORBI_CHAT_HISTORY (SESSIONID, ROLE, CONTENT, CREATEDAT, ISHTML)
            VALUES (@SessionId, @Role, @Content, @CreatedAt, @IsHtml)
        ";

        conn.Execute(sql, chat);
    }

    public List<ChatHistoryDto> GetChatHistory(string sessionId)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            SELECT ID, SESSIONID, ROLE, CONTENT, CREATEDAT, ISHTML
            FROM AIFORBI_CHAT_HISTORY
            WHERE SESSIONID = @SessionId
            ORDER BY CREATEDAT ASC
        ";

        return conn.Query<ChatHistoryDto>(sql, new { SessionId = sessionId }).ToList();
    }

    // ========== USER AUTHENTICATION METHODS ==========

    public UserDto? GetUserByCredentials(string email, string password)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            SELECT ID, EMAIL, PASSWORD_HASH AS PasswordHash, DISPLAY_NAME AS DisplayName, CREATED_AT AS CreatedAt
            FROM AIFORBI_USERS
            WHERE EMAIL = @Email AND PASSWORD_HASH = @Password
        ";

        return conn.QueryFirstOrDefault<UserDto>(sql, new { Email = email, Password = password });
    }

    public UserDto? GetUserById(int userId)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            SELECT ID, EMAIL, DISPLAY_NAME AS DisplayName, CREATED_AT AS CreatedAt
            FROM AIFORBI_USERS
            WHERE ID = @UserId
        ";

        return conn.QueryFirstOrDefault<UserDto>(sql, new { UserId = userId });
    }

    // ========== CHAT SESSION METHODS ==========

    public ChatSessionDto CreateChatSession(int userId, string sessionId, string? title = null)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            INSERT INTO AIFORBI_CHAT_SESSIONS (SESSION_ID, USER_ID, TITLE, CREATED_AT)
            VALUES (@SessionId, @UserId, @Title, GETDATE());
            
            SELECT CAST(SCOPE_IDENTITY() as int);
        ";

        var newId = conn.ExecuteScalar<int>(sql, new { SessionId = sessionId, UserId = userId, Title = title });
        
        return new ChatSessionDto
        {
            Id = newId,
            SessionId = sessionId,
            UserId = userId,
            Title = title,
            CreatedAt = DateTime.Now
        };
    }

    public List<ChatSessionDto> GetUserChatSessions(int userId)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            SELECT ID, SESSION_ID AS SessionId, USER_ID AS UserId, TITLE, CREATED_AT AS CreatedAt
            FROM AIFORBI_CHAT_SESSIONS
            WHERE USER_ID = @UserId
            ORDER BY CREATED_AT DESC
        ";

        return conn.Query<ChatSessionDto>(sql, new { UserId = userId }).ToList();
    }

    public ChatSessionDto? GetChatSessionBySessionId(string sessionId)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"
            SELECT ID, SESSION_ID AS SessionId, USER_ID AS UserId, TITLE, CREATED_AT AS CreatedAt
            FROM AIFORBI_CHAT_SESSIONS
            WHERE SESSION_ID = @SessionId
        ";

        return conn.QueryFirstOrDefault<ChatSessionDto>(sql, new { SessionId = sessionId });
    }

    public void UpdateChatSessionTitle(string sessionId, string title)
    {
        using var conn = new SqlConnection(_DbConnectionString);
        conn.Open();

        var sql = @"UPDATE AIFORBI_CHAT_SESSIONS SET TITLE = @Title WHERE SESSION_ID = @SessionId";

        conn.Execute(sql, new { SessionId = sessionId, Title = title });
    }
} 
