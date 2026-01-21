
namespace DBCONNECTOR.Dtos.Mssql;

public class ChatSessionDto
{
    public int Id { get; set; }
    public string? SessionId { get; set; }
    public int UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
}
