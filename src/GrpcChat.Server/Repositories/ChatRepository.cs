using Google.Protobuf;
using GrpcChat.Contracts;
using StackExchange.Redis;

namespace GrpcChat.Server.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly IDatabase _db;
    private readonly ILogger<ChatRepository> _logger;
    private const int MaxMessagesPerRoom = 100;
    private static readonly JsonFormatter JsonFormatter = JsonFormatter.Default;
    private static readonly JsonParser JsonParser = JsonParser.Default;

    public ChatRepository(IConnectionMultiplexer redis, ILogger<ChatRepository> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> AddMessageAsync(ChatMessage message)
    {
        try
        {
            var key = $"room:{message.RoomId}:messages";
            var json = JsonFormatter.Format(message);

            // Add to the end of the list
            await _db.ListRightPushAsync(key, json);

            // Keep only the last MaxMessagesPerRoom messages
            await _db.ListTrimAsync(key, -MaxMessagesPerRoom, -1);

            _logger.LogDebug("Message {MessageId} added to room {RoomId}",
                message.MessageId, message.RoomId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding message {MessageId} to room {RoomId}",
                message.MessageId, message.RoomId);
            throw;
        }
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(string roomId, int count = 50)
    {
        try
        {
            var key = $"room:{roomId}:messages";
            var messages = new List<ChatMessage>();

            // Get the last 'count' messages
            var values = await _db.ListRangeAsync(key, -count, -1);

            foreach (var value in values)
            {
                if (value.HasValue)
                {
                    try
                    {
                        var message = JsonParser.Parse<ChatMessage>(value!);
                        messages.Add(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize message from room {RoomId}", roomId);
                    }
                }
            }

            _logger.LogInformation("Retrieved {Count} messages from room {RoomId}",
                messages.Count, roomId);

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages from room {RoomId}", roomId);
            return new List<ChatMessage>();
        }
    }
}
