using GrpcChat.Client.Extensions;
using GrpcChat.Client.Menu;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Enable HTTP/2 unencrypted support for gRPC over HTTP
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Server address - use HTTP for containerized server
var serverAddress = args.Length > 0 ? args[0] : "http://localhost:5001";

// Add gRPC client services
builder.Services.AddGrpcChatClient(serverAddress);

// Add console menu
builder.Services.AddSingleton<ConsoleMenu>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Connecting to gRPC server at: {ServerAddress}", serverAddress);

// Run the interactive menu
var menu = host.Services.GetRequiredService<ConsoleMenu>();
await menu.RunAsync();
