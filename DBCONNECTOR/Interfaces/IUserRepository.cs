using DBCONNECTOR.Dtos.Mssql;

namespace DBCONNECTOR.Interfaces;

/// <summary>
/// Repository interface for user-related database operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by email and password credentials.
    /// </summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's password.</param>
    /// <returns>UserDto if found, null otherwise.</returns>
    UserDto? GetByCredentials(string email, string password);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>UserDto if found, null otherwise.</returns>
    UserDto? GetById(int userId);
}
