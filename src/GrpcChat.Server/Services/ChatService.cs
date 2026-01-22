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

    public override async Task ReceiveMessages(
        JoinRoomRequest request,
        IServerStreamWriter<ChatMessage> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("User {UserId} joining room {RoomId} for message stream",
            request.UserId, request.RoomId);

        // Phase 1: Send historical messages
        var history = await _chatRepository.GetRecentMessagesAsync(request.RoomId, 50);
        _logger.LogInformation("Sending {Count} historical messages from room {RoomId}",
            history.Count, request.RoomId);

        foreach (var msg in history)
        {
            await responseStream.WriteAsync(msg);
        }

        // Phase 2: Stream new messages
        var messageNumber = 1;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait 2-5 seconds before generating a new message
                await Task.Delay(
                    Random.Shared.Next(2000, 5000),
                    context.CancellationToken);

                var message = new ChatMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    UserId = "bot",
                    Username = "ChatBot",
                    RoomId = request.RoomId,
                    Content = $"Simulated message #{messageNumber} in room {request.RoomId}",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Type = MessageType.Regular
                };

                // Persist to Redis BEFORE sending
                await _chatRepository.AddMessageAsync(message);

                // Send to client
                await responseStream.WriteAsync(message);

                _logger.LogDebug("Sent message #{MessageNumber} to room {RoomId}",
                    messageNumber, request.RoomId);

                messageNumber++;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Message stream cancelled for user {UserId} in room {RoomId}",
                    request.UserId, request.RoomId);
                break;
            }
        }

        _logger.LogInformation("Message stream ended for user {UserId} in room {RoomId}. Total messages sent: {Count}",
            request.UserId, request.RoomId, messageNumber - 1);
    }
}
