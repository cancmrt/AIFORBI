using DBCONNECTOR.Dtos.Mssql;

namespace DBCONNECTOR.Interfaces;

/// <summary>
/// Repository interface for chat-related database operations.
/// </summary>
public interface IChatRepository
{
    /// <summary>
    /// Adds a chat message to history.
    /// </summary>
    void AddChatHistory(ChatHistoryDto chat);

    /// <summary>
    /// Retrieves chat history for a specific session.
    /// </summary>
    List<ChatHistoryDto> GetChatHistory(string sessionId);

    /// <summary>
    /// Creates a new chat session for a user.
    /// </summary>
    ChatSessionDto CreateSession(int userId, string sessionId, string? title = null);

    /// <summary>
    /// Retrieves all chat sessions for a user.
    /// </summary>
    List<ChatSessionDto> GetUserSessions(int userId);

    /// <summary>
    /// Retrieves a specific chat session by session ID.
    /// </summary>
    ChatSessionDto? GetSessionBySessionId(string sessionId);

    /// <summary>
    /// Updates the title of a chat session.
    /// </summary>
    void UpdateSessionTitle(string sessionId, string title);
}
