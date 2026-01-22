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
}
