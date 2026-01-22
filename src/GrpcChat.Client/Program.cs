using GrpcChat.Client.Extensions;
using GrpcChat.Client.Menu;
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

// Add console menu
builder.Services.AddSingleton<ConsoleMenu>();

var host = builder.Build();

// Run the interactive menu
var menu = host.Services.GetRequiredService<ConsoleMenu>();
await menu.RunAsync();
