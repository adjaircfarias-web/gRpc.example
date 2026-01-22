using GrpcChat.Client.Services;
using Microsoft.Extensions.Logging;

namespace GrpcChat.Client.Menu;

public class ConsoleMenu
{
    private readonly ChatClientService _chatService;
    private readonly ILogger<ConsoleMenu> _logger;

    public ConsoleMenu(
        ChatClientService chatService,
        ILogger<ConsoleMenu> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("=== gRPC Chat Client ===\n");

        while (true)
        {
            DisplayMenu();

            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await RegisterUserFlow();
                        break;
                    case "2":
                        await GetUserStatusFlow();
                        break;
                    case "3":
                        await ReceiveMessagesFlow();
                        break;
                    case "4":
                        await ChatStreamFlow();
                        break;
                    case "5":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1-5.");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing menu option");
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine();
        }
    }

    private void DisplayMenu()
    {
        Console.WriteLine("Choose an option:");
        Console.WriteLine("1. Register User (Unary RPC)");
        Console.WriteLine("2. Get User Status (Unary RPC)");
        Console.WriteLine("3. Receive Messages (Server Streaming)");
        Console.WriteLine("4. Chat Stream (Bidirectional Streaming)");
        Console.WriteLine("5. Exit");

        if (_chatService.CurrentUser != null)
        {
            Console.WriteLine($"\n[Logged in as: {_chatService.CurrentUser.Username} ({_chatService.CurrentUser.UserId})]");
        }

        Console.Write("\nEnter choice: ");
    }

    private async Task RegisterUserFlow()
    {
        Console.Write("Enter username: ");
        var username = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            Console.WriteLine("Invalid username. Username cannot be empty.");
            return;
        }

        var user = await _chatService.RegisterUserAsync(username);

        if (user != null)
        {
            Console.WriteLine($"Registration successful! Your user ID is: {user.UserId}");
        }
        else
        {
            Console.WriteLine("Registration failed. Please try again.");
        }
    }

    private async Task GetUserStatusFlow()
    {
        Console.Write("Enter user ID (or press Enter to use your ID): ");
        var userId = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userId))
        {
            if (_chatService.CurrentUser == null)
            {
                Console.WriteLine("No user ID provided and you are not registered.");
                return;
            }
            userId = _chatService.CurrentUser.UserId;
        }

        await _chatService.GetUserStatusAsync(userId);
    }

    private async Task ReceiveMessagesFlow()
    {
        if (_chatService.CurrentUser == null)
        {
            Console.WriteLine("You must register first (Option 1) before receiving messages.");
            return;
        }

        Console.Write("Enter room ID: ");
        var roomId = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            Console.WriteLine("Invalid room ID. Room ID cannot be empty.");
            return;
        }

        Console.WriteLine("Press Ctrl+C to stop receiving messages...\n");

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\nStopping message stream...");
        };

        await _chatService.ReceiveMessagesAsync(roomId, cts.Token);
    }

    private async Task ChatStreamFlow()
    {
        if (_chatService.CurrentUser == null)
        {
            Console.WriteLine("You must register first (Option 1) before joining a chat.");
            return;
        }

        Console.Write("Enter room ID: ");
        var roomId = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(roomId))
        {
            Console.WriteLine("Invalid room ID. Room ID cannot be empty.");
            return;
        }

        Console.WriteLine("Type 'exit' to leave the chat room, or press Ctrl+C to cancel.\n");

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\nCancelling chat stream...");
        };

        await _chatService.ChatStreamAsync(roomId, cts.Token);
    }
}
