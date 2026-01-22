using GrpcChat.Server.Extensions;
using GrpcChat.Server.Interceptors;
using GrpcChat.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services with interceptor
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
    options.Interceptors.Add<ServerLoggingInterceptor>();
});

// Add Redis services
builder.Services.AddRedisServices(builder.Configuration);

var app = builder.Build();

// Map gRPC services
app.MapGrpcService<ChatService>();

app.MapGet("/", () => "gRPC Chat Server is running");

app.Run();
