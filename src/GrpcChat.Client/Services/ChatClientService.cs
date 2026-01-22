using Grpc.Core;
using GrpcChat.Contracts;
using Microsoft.Extensions.Logging;

namespace GrpcChat.Client.Services;

public class ChatClientService
{
    private readonly ChatService.ChatServiceClient _client;
    private readonly ILogger<ChatClientService> _logger;
    private User? _currentUser;

    public User? CurrentUser => _currentUser;

    public ChatClientService(
        ChatService.ChatServiceClient client,
        ILogger<ChatClientService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<User?> RegisterUserAsync(string username)
    {
        try
        {
            var request = new RegisterUserRequest { Username = username };
            var deadline = DateTime.UtcNow.AddSeconds(5);

            _logger.LogInformation("Registering user: {Username}", username);

            var response = await _client.RegisterUserAsync(
                request,
                deadline: deadline);

            if (response.Success)
            {
                _currentUser = response.User;
                _logger.LogInformation(
                    "Successfully registered as {Username} with ID {UserId}",
                    username, response.User.UserId);
                return response.User;
            }

            _logger.LogWarning("Registration failed: {Message}", response.Message);
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            _logger.LogError("Registration timed out after 5 seconds");
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogError("Server unavailable. Please check if the server is running.");
            return null;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "RPC error during registration: {Status}", ex.Status);
            return null;
        }
    }

    public async Task GetUserStatusAsync(string userId)
    {
        try
        {
            var request = new GetUserStatusRequest { UserId = userId };
            var deadline = DateTime.UtcNow.AddSeconds(3);

            _logger.LogInformation("Getting status for user: {UserId}", userId);

            var response = await _client.GetUserStatusAsync(
                request,
                deadline: deadline);

            var lastSeen = DateTimeOffset.FromUnixTimeSeconds(response.LastSeen);

            _logger.LogInformation(
                "User {Username} ({UserId}) is {Status}. Last seen: {LastSeen}",
                response.Username,
                response.UserId,
                response.IsOnline ? "online" : "offline",
                lastSeen.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            _logger.LogError("Get user status timed out after 3 seconds");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            _logger.LogWarning("User not found: {UserId}", userId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogError("Server unavailable. Please check if the server is running.");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "RPC error getting user status: {Status}", ex.Status);
        }
    }

    public async Task ReceiveMessagesAsync(string roomId, CancellationToken cancellationToken)
    {
        if (_currentUser == null)
        {
            _logger.LogWarning("Must register before joining a room");
            return;
        }

        try
        {
            var request = new JoinRoomRequest
            {
                UserId = _currentUser.UserId,
                RoomId = roomId
            };

            var deadline = DateTime.UtcNow.AddSeconds(60);

            using var call = _client.ReceiveMessages(
                request,
                deadline: deadline,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Listening for messages in room {RoomId}...", roomId);
            _logger.LogInformation("Press Ctrl+C to stop receiving messages.");

            await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                var timestamp = DateTimeOffset.FromUnixTimeSeconds(message.Timestamp);
                var formattedTime = timestamp.ToLocalTime().ToString("HH:mm:ss");

                Console.WriteLine($"[{formattedTime}] {message.Username}: {message.Content}");
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            _logger.LogWarning("Message stream timed out after 60 seconds");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            _logger.LogInformation("Message stream cancelled by user");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message stream cancelled by user");
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogError("Server unavailable. Please check if the server is running.");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error in message stream: {Status}", ex.Status);
        }
    }

    public async Task ChatStreamAsync(string roomId, CancellationToken cancellationToken)
    {
        if (_currentUser == null)
        {
            _logger.LogWarning("Must register before joining chat");
            return;
        }

        try
        {
            using var call = _client.ChatStream(cancellationToken: cancellationToken);

            // BACKGROUND TASK for reading responses
            var readTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
                    {
                        switch (response.ResponseCase)
                        {
                            case StreamChatResponse.ResponseOneofCase.Message:
                                var msg = response.Message;
                                var timestamp = DateTimeOffset.FromUnixTimeSeconds(msg.Timestamp);
                                Console.WriteLine(
                                    $"[{timestamp.ToLocalTime():HH:mm:ss}] {msg.Username}: {msg.Content}");
                                break;

                            case StreamChatResponse.ResponseOneofCase.SystemMessage:
                                Console.WriteLine($"[SYSTEM] {response.SystemMessage}");
                                break;

                            case StreamChatResponse.ResponseOneofCase.UserStatus:
                                var status = response.UserStatus;
                                Console.WriteLine(
                                    $"[STATUS] {status.Username} is {(status.IsOnline ? "online" : "offline")}");
                                break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Response reader cancelled");
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    _logger.LogDebug("Response stream cancelled");
                }
            }, cancellationToken);

            // Send join request
            await call.RequestStream.WriteAsync(new StreamChatRequest
            {
                Join = new JoinRoomRequest
                {
                    UserId = _currentUser.UserId,
                    RoomId = roomId
                }
            });

            _logger.LogInformation("Joined chat room {RoomId}. Type messages (or 'exit' to leave):", roomId);

            // MAIN THREAD reads user input
            while (!cancellationToken.IsCancellationRequested)
            {
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    // Send leave room request
                    await call.RequestStream.WriteAsync(new StreamChatRequest
                    {
                        LeaveRoom = roomId
                    });
                    break;
                }

                // Send chat message
                var message = new ChatMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    UserId = _currentUser.UserId,
                    Username = _currentUser.Username,
                    RoomId = roomId,
                    Content = input,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Type = MessageType.Regular
                };

                await call.RequestStream.WriteAsync(new StreamChatRequest
                {
                    Message = message
                });
            }

            // GRACEFUL SHUTDOWN
            await call.RequestStream.CompleteAsync();

            // Wait for read task to complete
            await readTask;

            _logger.LogInformation("Exited chat room {RoomId}", roomId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            _logger.LogError("Server unavailable. Please check if the server is running.");
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error in chat stream: {Status}", ex.Status);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat stream cancelled");
        }
    }
}
