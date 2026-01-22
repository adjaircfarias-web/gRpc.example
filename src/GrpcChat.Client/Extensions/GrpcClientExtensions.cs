using Grpc.Net.Client;
using GrpcChat.Client.Services;
using GrpcChat.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcChat.Client.Extensions;

public static class GrpcClientExtensions
{
    public static IServiceCollection AddGrpcChatClient(
        this IServiceCollection services,
        string serverAddress)
    {
        // Register GrpcChannel as SINGLETON (critical for HTTP/2 connection reuse)
        services.AddSingleton(_ =>
        {
            return GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpHandler = new HttpClientHandler
                {
                    // Accept self-signed certificates in development
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                }
            });
        });

        // Register gRPC client stub as singleton
        services.AddSingleton(sp =>
        {
            var channel = sp.GetRequiredService<GrpcChannel>();
            return new ChatService.ChatServiceClient(channel);
        });

        // Register wrapper service
        services.AddSingleton<ChatClientService>();

        return services;
    }
}
