using GrpcChat.Server.Extensions;
using GrpcChat.Server.Interceptors;
using GrpcChat.Server.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use HTTP/2 (required for gRPC)
builder.WebHost.ConfigureKestrel(options =>
{
    // Setup HTTP/2 endpoint without TLS for gRPC
    options.ListenAnyIP(5001, o => o.Protocols = HttpProtocols.Http2);
});

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
