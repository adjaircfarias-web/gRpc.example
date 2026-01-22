using GrpcChat.Contracts;

namespace GrpcChat.Server.Repositories;

public interface IUserRepository
{
    Task<bool> AddUserAsync(User user);
    Task<User?> GetUserAsync(string userId);
    Task<bool> UserExistsAsync(string userId);
}
