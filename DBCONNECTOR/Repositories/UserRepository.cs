#nullable enable
using Dapper;
using Microsoft.Data.SqlClient;
using DBCONNECTOR.Dtos.Mssql;
using DBCONNECTOR.Interfaces;

namespace DBCONNECTOR.Repositories;

/// <summary>
/// SQL Server implementation of IUserRepository.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public UserDto? GetByCredentials(string email, string password)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT ID, EMAIL, PASSWORD_HASH AS PasswordHash, DISPLAY_NAME AS DisplayName, CREATED_AT AS CreatedAt
            FROM AIFORBI_USERS
            WHERE EMAIL = @Email AND PASSWORD_HASH = @Password";

        return conn.QueryFirstOrDefault<UserDto>(sql, new { Email = email, Password = password });
    }

    public UserDto? GetById(int userId)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        const string sql = @"
            SELECT ID, EMAIL, DISPLAY_NAME AS DisplayName, CREATED_AT AS CreatedAt
            FROM AIFORBI_USERS
            WHERE ID = @UserId";

        return conn.QueryFirstOrDefault<UserDto>(sql, new { UserId = userId });
    }
}
