
using System;

namespace DBCONNECTOR.Dtos.Mssql;

public class ChatHistoryDto
{
    public int Id { get; set; }
    public string? SessionId { get; set; }
    public string? Role { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsHtml { get; set; }
}
