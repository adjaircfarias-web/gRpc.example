using Grpc.Core;
using GrpcChat.Contracts;
using GrpcChat.Server.Repositories;

namespace GrpcChat.Server.Services;

public class ChatService : Contracts.ChatService.ChatServiceBase
{
    private readonly IUserRepository _userRepository;
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IUserRepository userRepository,
        IChatRepository chatRepository,
        ILogger<ChatService> logger)
    {
        _userRepository = userRepository;
        _chatRepository = chatRepository;
        _logger = logger;
    }

    public override async Task<RegisterUserResponse> RegisterUser(
        RegisterUserRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Registering user with username: {Username}", request.Username);

        var userId = Guid.NewGuid().ToString();
        var user = new User
        {
            UserId = userId,
            Username = request.Username,
            RegisteredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var success = await _userRepository.AddUserAsync(user);

        if (success)
        {
            _logger.LogInformation("User registered successfully: {Username} ({UserId})",
                request.Username, userId);

            return new RegisterUserResponse
            {
                User = user,
                Success = true,
                Message = "User registered successfully"
            };
        }

        _logger.LogWarning("Failed to register user: {Username}", request.Username);

        return new RegisterUserResponse
        {
            Success = false,
            Message = "Failed to register user"
        };
    }

    public override async Task<UserStatus> GetUserStatus(
        GetUserStatusRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("Getting status for user: {UserId}", request.UserId);

        var user = await _userRepository.GetUserAsync(request.UserId);

        if (user == null)
        {
            _logger.LogWarning("User not found: {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
        }

        _logger.LogInformation("User status retrieved: {Username} ({UserId})",
            user.Username, user.UserId);

        return new UserStatus
        {
            UserId = user.UserId,
            Username = user.Username,
            IsOnline = true,
            LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}
