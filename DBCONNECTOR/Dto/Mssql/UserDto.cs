
namespace DBCONNECTOR.Dtos.Mssql;

public class UserDto
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}
