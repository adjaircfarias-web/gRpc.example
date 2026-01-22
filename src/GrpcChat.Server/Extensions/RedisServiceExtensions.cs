using StackExchange.Redis;

namespace GrpcChat.Server.Extensions;

public static class RedisServiceExtensions
{
    public static IServiceCollection AddRedisServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();

            try
            {
                var connection = ConnectionMultiplexer.Connect(connectionString);
                logger.LogInformation("Connected to Redis at {ConnectionString}", connectionString);
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Redis at {ConnectionString}", connectionString);
                throw;
            }
        });

        return services;
    }
}
