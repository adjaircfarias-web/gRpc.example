using Google.Protobuf;
using GrpcChat.Contracts;
using StackExchange.Redis;

namespace GrpcChat.Server.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDatabase _db;
    private readonly ILogger<UserRepository> _logger;
    private static readonly JsonFormatter JsonFormatter = JsonFormatter.Default;
    private static readonly JsonParser JsonParser = JsonParser.Default;

    public UserRepository(IConnectionMultiplexer redis, ILogger<UserRepository> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> AddUserAsync(User user)
    {
        try
        {
            var key = $"user:{user.UserId}";
            var json = JsonFormatter.Format(user);
            var success = await _db.StringSetAsync(key, json);

            if (success)
            {
                _logger.LogInformation("User {UserId} saved to Redis", user.UserId);
            }
            else
            {
                _logger.LogWarning("Failed to save user {UserId} to Redis", user.UserId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user {UserId} to Redis", user.UserId);
            throw;
        }
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        try
        {
            var key = $"user:{userId}";
            var json = await _db.StringGetAsync(key);

            if (!json.HasValue)
            {
                _logger.LogDebug("User {UserId} not found in Redis", userId);
                return null;
            }

            var user = JsonParser.Parse<User>(json!);
            _logger.LogDebug("User {UserId} retrieved from Redis", userId);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId} from Redis", userId);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(string userId)
    {
        try
        {
            var key = $"user:{userId}";
            var exists = await _db.KeyExistsAsync(key);
            _logger.LogDebug("User {UserId} exists check: {Exists}", userId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} exists in Redis", userId);
            throw;
        }
    }
}
