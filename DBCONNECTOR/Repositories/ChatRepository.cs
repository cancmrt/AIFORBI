#nullable enable
using Dapper;
using Microsoft.Data.SqlClient;
using DBCONNECTOR.Dtos.Mssql;
using DBCONNECTOR.Interfaces;

namespace DBCONNECTOR.Repositories;

/// <summary>
/// SQL Server implementation of IChatRepository.
/// </summary>
public class ChatRepository : IChatRepository
{
    private readonly string _connectionString;

    public ChatRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public void AddChatHistory(ChatHistoryDto chat)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            INSERT INTO AIFORBI_CHAT_HISTORY (SESSIONID, ROLE, CONTENT, CREATEDAT, ISHTML)
            VALUES (@SessionId, @Role, @Content, @CreatedAt, @IsHtml)";

        conn.Execute(sql, chat);
    }

    public List<ChatHistoryDto> GetChatHistory(string sessionId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT ID, SESSIONID AS SessionId, ROLE, CONTENT, CREATEDAT AS CreatedAt, ISHTML AS IsHtml
            FROM AIFORBI_CHAT_HISTORY
            WHERE SESSIONID = @SessionId
            ORDER BY CREATEDAT ASC";

        return conn.Query<ChatHistoryDto>(sql, new { SessionId = sessionId }).ToList();
    }

    public ChatSessionDto CreateSession(int userId, string sessionId, string? title = null)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            INSERT INTO AIFORBI_CHAT_SESSIONS (SESSION_ID, USER_ID, TITLE, CREATED_AT)
            VALUES (@SessionId, @UserId, @Title, GETDATE());
            
            SELECT CAST(SCOPE_IDENTITY() as int);";

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

    public List<ChatSessionDto> GetUserSessions(int userId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT ID, SESSION_ID AS SessionId, USER_ID AS UserId, TITLE, CREATED_AT AS CreatedAt
            FROM AIFORBI_CHAT_SESSIONS
            WHERE USER_ID = @UserId
            ORDER BY CREATED_AT DESC";

        return conn.Query<ChatSessionDto>(sql, new { UserId = userId }).ToList();
    }

    public ChatSessionDto? GetSessionBySessionId(string sessionId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT ID, SESSION_ID AS SessionId, USER_ID AS UserId, TITLE, CREATED_AT AS CreatedAt
            FROM AIFORBI_CHAT_SESSIONS
            WHERE SESSION_ID = @SessionId";

        return conn.QueryFirstOrDefault<ChatSessionDto>(sql, new { SessionId = sessionId });
    }

    public void UpdateSessionTitle(string sessionId, string title)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"UPDATE AIFORBI_CHAT_SESSIONS SET TITLE = @Title WHERE SESSION_ID = @SessionId";

        conn.Execute(sql, new { SessionId = sessionId, Title = title });
    }
}
