using GrpcChat.Contracts;

namespace GrpcChat.Server.Repositories;

public interface IChatRepository
{
    Task<bool> AddMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetRecentMessagesAsync(string roomId, int count = 50);
}
