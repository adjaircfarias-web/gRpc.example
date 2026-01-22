using GrpcChat.Client.Extensions;
using GrpcChat.Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add gRPC client services
builder.Services.AddGrpcChatClient("https://localhost:5001");

var host = builder.Build();

// Get the chat service and run
var chatService = host.Services.GetRequiredService<ChatClientService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("gRPC Chat Client started. Server: https://localhost:5001");
logger.LogInformation("Client is ready. Menu will be implemented in US-014.");

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
