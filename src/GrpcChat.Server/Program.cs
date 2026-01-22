using GrpcChat.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC services
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
});

// Add Redis services
builder.Services.AddRedisServices(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "gRPC Chat Server is running");

app.Run();
