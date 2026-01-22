# Plano de Implementação: Sistema gRPC de Chat com Streaming + Redis

**Versão**: 1.0
**Data**: 2026-01-21
**Tecnologia**: .NET 8.0, gRPC, Redis
**Objetivo**: Demonstrar padrões gRPC avançados com persistência em produção

---

## 📋 Visão Geral

Criar uma solução gRPC completa em .NET 8.0 demonstrando todos os padrões de comunicação (unário, server streaming e bidirectional streaming), com features de nível produção:

- ✅ Deadlines para controle de timeout
- ✅ Interceptadores para logging e métricas
- ✅ Logging estruturado com ILogger
- ✅ **Persistência com Redis**
- ✅ **Repository Pattern**
- ✅ **Graceful shutdown**

---

## 🏗️ Estrutura de Projetos

```
c:\dev\myprojects\gRpc.example\
├── .docs\
│   └── plan\
│       └── implementation-plan.md    # Este arquivo
├── src\
│   ├── GrpcChat.Contracts\           # Contratos compartilhados (.proto)
│   ├── GrpcChat.Server\              # Servidor gRPC (ASP.NET Core)
│   └── GrpcChat.Client\              # Cliente console (demonstração)
└── gRpc.example.slnx                 # Solution file
```

---

## 📦 Arquivos Críticos a Criar

### 1. GrpcChat.Contracts (Projeto de Contratos)

**Arquivos**:
- `GrpcChat.Contracts.csproj` - Projeto .NET 8.0 com Grpc.Tools
- `Protos/chat.proto` - Definição Protocol Buffers

**Mensagens Proto**:
- `User` - Dados do usuário
- `RegisterUserRequest` / `RegisterUserResponse` - Registro de usuário
- `GetUserStatusRequest` / `UserStatus` - Status do usuário
- `JoinRoomRequest` - Entrar em sala de chat
- `ChatMessage` - Mensagem de chat
- `StreamChatRequest` / `StreamChatResponse` - Streaming bidirecional
- `MessageType` (enum) - REGULAR, SYSTEM, JOIN, LEAVE

**Service**:
- `ChatService` com 4 métodos RPC:
  - `RegisterUser` (unary)
  - `GetUserStatus` (unary)
  - `ReceiveMessages` (server streaming)
  - `ChatStream` (bidirectional streaming)

### 2. GrpcChat.Server (Servidor)

**Arquivos principais**:
- `GrpcChat.Server.csproj` - Web SDK com Grpc.AspNetCore e StackExchange.Redis
- `Program.cs` - Configuração ASP.NET Core
- `appsettings.json` - Configurações de logging e Redis

**Camada de Serviço**:
- `Services/ChatService.cs` - Implementação do serviço gRPC (herda de `ChatService.ChatServiceBase`)

**Camada de Persistência (Repository Pattern)**:
- `Repositories/IUserRepository.cs` - Interface para usuários
- `Repositories/UserRepository.cs` - Implementação Redis para usuários
- `Repositories/IChatRepository.cs` - Interface para mensagens
- `Repositories/ChatRepository.cs` - Implementação Redis para mensagens

**Infraestrutura**:
- `Extensions/RedisServiceExtensions.cs` - Extension methods para DI do Redis
- `Interceptors/ServerLoggingInterceptor.cs` - Interceptador para logging e métricas

### 3. GrpcChat.Client (Cliente)

**Arquivos principais**:
- `GrpcChat.Client.csproj` - Console app com Grpc.Net.Client
- `Program.cs` - Configuração DI e entrada da aplicação

**Camada de Serviço**:
- `Services/ChatClientService.cs` - Wrapper dos métodos gRPC com padrões corretos

**Infraestrutura**:
- `Extensions/GrpcClientExtensions.cs` - Registro do GrpcChannel (singleton)
- `Menu/ConsoleMenu.cs` - Menu interativo para demonstração

---

## 🚀 Implementação por Fases

### Fase 1: Fundação (Contratos)

#### 1. Criar estrutura de diretórios
```bash
mkdir -p src/GrpcChat.Contracts/Protos
mkdir -p src/GrpcChat.Server
mkdir -p src/GrpcChat.Client
```

#### 2. Implementar GrpcChat.Contracts

**Criar `.csproj`** com:
```xml
<ItemGroup>
  <Protobuf Include="Protos\chat.proto" GrpcServices="Both" />
</ItemGroup>
```

**Criar `chat.proto`** incluindo:
- Mensagens para registro de usuário, status, chat
- Enum `MessageType` (REGULAR, SYSTEM, JOIN, LEAVE)
- Service com 4 RPC methods (2 unary, 1 server streaming, 1 bidirectional)

**Build** para gerar código C#:
```bash
dotnet build
```

**Verificar**: Código gerado deve existir em `obj/Debug/net8.0/`

---

### Fase 2: Servidor

#### 3. Criar projeto servidor

**`.csproj`** com:
- Referência ao `GrpcChat.Contracts`
- Pacote `StackExchange.Redis`
- Pacote `Grpc.AspNetCore`

**`appsettings.json`** com:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Grpc": "Debug"
    }
  }
}
```

#### 4. Implementar camada de repositório Redis

**IUserRepository**:
- Métodos: `AddUserAsync`, `GetUserAsync`, `UserExistsAsync`

**UserRepository**:
- Usar `IDatabase` do Redis
- Serialização JSON com `System.Text.Json`
- Chave: `user:{userId}`
- Comandos: `StringSetAsync`, `StringGetAsync`, `KeyExistsAsync`

**IChatRepository**:
- Métodos: `AddMessageAsync`, `GetRecentMessagesAsync`

**ChatRepository**:
- Usar Redis Lists
- Chave: `room:{roomId}:messages`
- Comandos: `ListRightPushAsync`, `ListRangeAsync`
- Manter apenas últimas 100 mensagens (usar `ListTrimAsync`)

**RedisServiceExtensions**:
```csharp
public static IServiceCollection AddRedisServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Registrar IConnectionMultiplexer como singleton
    // Registrar repositórios como scoped
}
```

#### 5. Implementar ServerLoggingInterceptor

**Herdar** de `Grpc.Core.Interceptors.Interceptor`

**Criar classes wrapper**:
- `CountingServerStreamWriter<T>` - Conta mensagens enviadas
- `CountingAsyncStreamReader<T>` - Conta mensagens recebidas

**Implementar 3 métodos**:
1. `UnaryServerHandler` - Log início/fim, duração
2. `ServerStreamingServerHandler` - Log + contagem de mensagens enviadas
3. `DuplexStreamingServerHandler` - Log + contagem de mensagens recebidas/enviadas

**Usar**:
- `ILogger<T>` para logging estruturado
- `Stopwatch` para métricas de duração

#### 6. Implementar ChatService

**Herdar** de `ChatService.ChatServiceBase`

**Injetar** via construtor:
- `IUserRepository`
- `IChatRepository`
- `ILogger<ChatService>`

**Métodos**:

**RegisterUser** (unary):
```csharp
// 1. Gerar GUID para userId
// 2. Criar objeto User
// 3. Salvar via _userRepository.AddUserAsync()
// 4. Retornar RegisterUserResponse
```

**GetUserStatus** (unary):
```csharp
// 1. Buscar via _userRepository.GetUserAsync()
// 2. Se não encontrar, lançar RpcException com StatusCode.NotFound
// 3. Retornar UserStatus
```

**ReceiveMessages** (server streaming):
```csharp
// 1. Buscar histórico via _chatRepository.GetRecentMessagesAsync(roomId, 50)
// 2. Enviar histórico via responseStream.WriteAsync()
// 3. Loop infinito com Task.Delay(2-5s)
// 4. Criar mensagem simulada
// 5. Salvar no Redis via _chatRepository.AddMessageAsync()
// 6. Enviar via responseStream.WriteAsync()
// 7. Verificar context.CancellationToken.IsCancellationRequested
```

**ChatStream** (bidirectional):
```csharp
// 1. await foreach (var request in requestStream.ReadAllAsync())
// 2. Switch no request.RequestCase (Join, Message, LeaveRoom)
// 3. Processar cada tipo de request
// 4. Salvar mensagens via _chatRepository.AddMessageAsync()
// 5. Responder via responseStream.WriteAsync()
```

#### 7. Configurar Program.cs do servidor

```csharp
// 1. builder.Services.AddRedisServices(builder.Configuration)
// 2. builder.Services.AddGrpc(options => {
//      options.Interceptors.Add<ServerLoggingInterceptor>();
//      options.EnableDetailedErrors = true;
//    })
// 3. app.MapGrpcService<ChatService>()
// 4. app.MapGrpcReflectionService() // Apenas em Development
// 5. Health check opcional para Redis
```

---

### Fase 3: Cliente

#### 8. Criar projeto cliente

**`.csproj`** com:
- `OutputType=Exe`
- Referência ao `GrpcChat.Contracts`
- Pacotes: `Grpc.Net.Client`, `Microsoft.Extensions.Hosting`

#### 9. Implementar GrpcClientExtensions

```csharp
public static IServiceCollection AddGrpcChatClient(
    this IServiceCollection services,
    string serverAddress)
{
    // 1. Registrar GrpcChannel como SINGLETON (crítico!)
    services.AddSingleton(sp => {
        return GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions {
            HttpHandler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }
        });
    });

    // 2. Registrar ChatService.ChatServiceClient como singleton
    services.AddSingleton(sp => {
        var channel = sp.GetRequiredService<GrpcChannel>();
        return new ChatService.ChatServiceClient(channel);
    });

    // 3. Registrar ChatClientService
    services.AddSingleton<ChatClientService>();

    return services;
}
```

#### 10. Implementar ChatClientService

**Injetar** via construtor:
- `ChatService.ChatServiceClient`
- `ILogger<ChatClientService>`

**RegisterUserAsync**:
```csharp
// 1. Deadline de 5 segundos
// 2. await _client.RegisterUserAsync(request, deadline: deadline)
// 3. Tratar RpcException com StatusCode.DeadlineExceeded
// 4. Retornar User?
```

**GetUserStatusAsync**:
```csharp
// 1. Deadline de 3 segundos
// 2. await _client.GetUserStatusAsync(request, deadline: deadline)
// 3. Tratar RpcException
// 4. Log do status
```

**ReceiveMessagesAsync** (server streaming):
```csharp
// 1. Deadline de 60 segundos
// 2. using var call = _client.ReceiveMessages(request, deadline, cancellationToken)
// 3. await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
// 4. Log de cada mensagem recebida
// 5. Tratar StatusCode.DeadlineExceeded e StatusCode.Cancelled
```

**ChatStreamAsync** (bidirectional):
```csharp
// 1. using var call = _client.ChatStream(cancellationToken)
// 2. Criar Task.Run para ler respostas em background
//    - await foreach (var response in call.ResponseStream.ReadAllAsync())
//    - Processar response.ResponseCase (Message, SystemMessage, UserStatus)
// 3. Thread principal lê Console.ReadLine()
// 4. Enviar via call.RequestStream.WriteAsync(message)
// 5. await call.RequestStream.CompleteAsync() quando terminar
// 6. await readTask para esperar leitura finalizar
```

#### 11. Implementar ConsoleMenu

**Menu interativo** com 5 opções:
1. Registrar usuário (unary)
2. Buscar status de usuário (unary)
3. Receber mensagens de sala (server streaming)
4. Chat em tempo real (bidirectional)
5. Sair

**Configurar** `Console.CancelKeyPress` para `CancellationTokenSource`

#### 12. Configurar Program.cs do cliente

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Registrar gRPC client
builder.Services.AddGrpcChatClient("https://localhost:5001");

// Registrar menu
builder.Services.AddSingleton<ConsoleMenu>();

var host = builder.Build();

// Executar menu
var menu = host.Services.GetRequiredService<ConsoleMenu>();
await menu.RunAsync();
```

---

### Fase 4: Testes e Refinamento

#### 13. Configurar ambiente Redis

**Opção 1 - Docker** (recomendado):
```bash
docker run -d --name redis-chat -p 6379:6379 redis:alpine
```

**Opção 2 - Local**:
```bash
redis-server
```

**Verificar conexão**:
```bash
redis-cli ping
# Deve retornar: PONG
```

#### 14. Testes funcionais

**Iniciar servidor**:
```bash
cd src/GrpcChat.Server
dotnet run
```
Verificar log: "Connected to Redis at localhost:6379"

**Executar cliente**:
```bash
cd src/GrpcChat.Client
dotnet run
```

**Testar cada opção do menu**:
1. Registrar usuário "Alice"
2. Buscar status usando ID retornado
3. Entrar na sala "general" (observar streaming)
4. Chat bidirecional na sala "test"

**Monitorar Redis**:
```bash
redis-cli
> KEYS user:*                          # Listar usuários
> GET user:{userId}                    # Ver dados do usuário
> LRANGE room:general:messages 0 -1   # Ver mensagens da sala
> MONITOR                              # Monitorar comandos em tempo real
```

#### 15. Testes de persistência

**Cenário 1 - Persistência de usuários**:
1. Registrar usuário "Alice"
2. Parar servidor (Ctrl+C)
3. Reiniciar servidor
4. Buscar status de "Alice" → Deve funcionar (dados persistidos)

**Cenário 2 - Histórico de mensagens**:
1. Entrar na sala "general"
2. Receber algumas mensagens
3. Sair e entrar novamente
4. Verificar que histórico é carregado primeiro

#### 16. Testes de cenários de erro

**Deadline exceeded**:
- Modificar deadline para 1ms em `RegisterUserAsync`
- Verificar `RpcException` com `StatusCode.DeadlineExceeded`
- Verificar log de erro apropriado

**Servidor offline**:
- Parar servidor durante streaming
- Verificar `RpcException` com `StatusCode.Unavailable`

**Graceful shutdown**:
- Ctrl+C durante streaming
- Verificar `StatusCode.Cancelled`
- Logs devem mostrar "stream cancelled"

**Redis offline**:
- Parar Redis: `docker stop redis-chat`
- Tentar operação no servidor
- Verificar tratamento de exceção apropriado
- Reiniciar Redis: `docker start redis-chat`

---

## 💻 Padrões Críticos de Código

### Repository Pattern com Redis

```csharp
// Interface simples e focada (ISP - SOLID)
public interface IUserRepository
{
    Task<bool> AddUserAsync(User user);
    Task<User?> GetUserAsync(string userId);
    Task<bool> UserExistsAsync(string userId);
}

// Implementação Redis com boas práticas
public class UserRepository : IUserRepository
{
    private readonly IDatabase _db;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IConnectionMultiplexer redis, ILogger<UserRepository> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> AddUserAsync(User user)
    {
        try
        {
            var key = $"user:{user.UserId}";
            var json = JsonSerializer.Serialize(user);
            var success = await _db.StringSetAsync(key, json);

            if (success)
                _logger.LogInformation("User {UserId} saved to Redis", user.UserId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user {UserId} to Redis", user.UserId);
            throw;
        }
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        try
        {
            var key = $"user:{userId}";
            var json = await _db.StringGetAsync(key);

            if (!json.HasValue)
            {
                _logger.LogWarning("User {UserId} not found in Redis", userId);
                return null;
            }

            return JsonSerializer.Deserialize<User>(json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId} from Redis", userId);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(string userId)
    {
        var key = $"user:{userId}";
        return await _db.KeyExistsAsync(key);
    }
}
```

### Injeção de Dependência com Redis

```csharp
public static class RedisServiceExtensions
{
    public static IServiceCollection AddRedisServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis")
            ?? "localhost:6379";

        // SINGLETON - Conexão Redis é cara, reutilizar
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            try
            {
                var connection = ConnectionMultiplexer.Connect(connectionString);
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Connected to Redis at {ConnectionString}", connectionString);
                return connection;
            }
            catch (Exception ex)
            {
                var logger = sp.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Failed to connect to Redis at {ConnectionString}", connectionString);
                throw;
            }
        });

        // SCOPED - Repositórios são stateless, mas seguem padrão de request
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        return services;
    }
}
```

### Server Streaming com Redis

```csharp
public override async Task ReceiveMessages(
    JoinRoomRequest request,
    IServerStreamWriter<ChatMessage> responseStream,
    ServerCallContext context)
{
    _logger.LogInformation(
        "User {UserId} joining room {RoomId} for message stream",
        request.UserId, request.RoomId);

    // FASE 1: Enviar histórico de mensagens do Redis
    var history = await _chatRepository.GetRecentMessagesAsync(request.RoomId, 50);

    _logger.LogInformation(
        "Sending {Count} historical messages from room {RoomId}",
        history.Count, request.RoomId);

    foreach (var msg in history)
    {
        await responseStream.WriteAsync(msg);
    }

    // FASE 2: Continuar com streaming de novas mensagens
    var messageNumber = 1;
    while (!context.CancellationToken.IsCancellationRequested)
    {
        try
        {
            await Task.Delay(
                Random.Shared.Next(2000, 5000),
                context.CancellationToken);

            var message = new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                UserId = "bot",
                Username = "ChatBot",
                RoomId = request.RoomId,
                Content = $"Simulated message #{messageNumber} in room {request.RoomId}",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Type = MessageType.Regular
            };

            // Persistir no Redis ANTES de enviar
            await _chatRepository.AddMessageAsync(message);

            // Enviar ao cliente
            await responseStream.WriteAsync(message);

            messageNumber++;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Message stream cancelled for room {RoomId}",
                request.RoomId);
            break;
        }
    }
}
```

### Server Streaming (Cliente)

```csharp
public async Task ReceiveMessagesAsync(string roomId, CancellationToken cancellationToken)
{
    if (_currentUser == null)
    {
        _logger.LogWarning("Must register before joining a room");
        return;
    }

    try
    {
        var request = new JoinRoomRequest
        {
            UserId = _currentUser.UserId,
            RoomId = roomId
        };

        // Deadline de 60 segundos
        var deadline = DateTime.UtcNow.AddSeconds(60);

        // using para garantir disposal correto
        using var call = _client.ReceiveMessages(
            request,
            deadline: deadline,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Listening for messages in room {RoomId}...", roomId);

        // await foreach com ReadAllAsync() - padrão recomendado
        await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(message.Timestamp);
            _logger.LogInformation(
                "[{Timestamp}] {Username}: {Content}",
                timestamp.ToLocalTime().ToString("HH:mm:ss"),
                message.Username,
                message.Content);
        }
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
    {
        _logger.LogWarning("Message stream timed out after 60 seconds");
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
    {
        _logger.LogInformation("Message stream cancelled by user");
    }
    catch (RpcException ex)
    {
        _logger.LogError(ex, "Error in message stream: {Status}", ex.Status);
    }
}
```

### Bidirectional Streaming (Cliente)

```csharp
public async Task ChatStreamAsync(string roomId, CancellationToken cancellationToken)
{
    if (_currentUser == null)
    {
        _logger.LogWarning("Must register before joining chat");
        return;
    }

    try
    {
        // Sem deadline para bidirectional - controlado pelo usuário
        using var call = _client.ChatStream(cancellationToken: cancellationToken);

        // BACKGROUND TASK para ler respostas do servidor
        var readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
                {
                    switch (response.ResponseCase)
                    {
                        case StreamChatResponse.ResponseOneofCase.Message:
                            var msg = response.Message;
                            var timestamp = DateTimeOffset.FromUnixTimeSeconds(msg.Timestamp);
                            Console.WriteLine(
                                $"[{timestamp:HH:mm:ss}] {msg.Username}: {msg.Content}");
                            break;

                        case StreamChatResponse.ResponseOneofCase.SystemMessage:
                            Console.WriteLine($"[SYSTEM] {response.SystemMessage}");
                            break;

                        case StreamChatResponse.ResponseOneofCase.UserStatus:
                            var status = response.UserStatus;
                            Console.WriteLine(
                                $"[STATUS] {status.Username} is {(status.IsOnline ? "online" : "offline")}");
                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Response reader cancelled");
            }
        }, cancellationToken);

        // Enviar join request
        await call.RequestStream.WriteAsync(new StreamChatRequest
        {
            Join = new JoinRoomRequest
            {
                UserId = _currentUser.UserId,
                RoomId = roomId
            }
        });

        _logger.LogInformation("Joined chat room {RoomId}. Type messages (or 'exit' to leave):", roomId);

        // THREAD PRINCIPAL lê input do usuário
        while (!cancellationToken.IsCancellationRequested)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                await call.RequestStream.WriteAsync(new StreamChatRequest
                {
                    LeaveRoom = roomId
                });
                break;
            }

            var message = new ChatMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                UserId = _currentUser.UserId,
                Username = _currentUser.Username,
                RoomId = roomId,
                Content = input,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Type = MessageType.Regular
            };

            await call.RequestStream.WriteAsync(new StreamChatRequest
            {
                Message = message
            });
        }

        // GRACEFUL SHUTDOWN
        // 1. Avisar que não vai enviar mais mensagens
        await call.RequestStream.CompleteAsync();

        // 2. Aguardar todas as respostas serem processadas
        await readTask;

        _logger.LogInformation("Exited chat room");
    }
    catch (RpcException ex)
    {
        _logger.LogError(ex, "Error in chat stream: {Status}", ex.Status);
    }
}
```

### GrpcChannel Singleton (CRÍTICO!)

```csharp
// ✅ CORRETO - Singleton para reutilização da conexão HTTP/2
services.AddSingleton(sp =>
{
    return GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
    {
        // Configurações opcionais
        MaxReceiveMessageSize = 5 * 1024 * 1024, // 5 MB
        MaxSendMessageSize = 5 * 1024 * 1024,    // 5 MB

        // Para desenvolvimento (aceitar certificados self-signed)
        HttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        }
    });
});

// ❌ INCORRETO - Scoped/Transient cria múltiplas conexões HTTP/2 (extremamente caro!)
services.AddScoped(sp => GrpcChannel.ForAddress("https://localhost:5001"));
```

---

## 📦 Dependências NuGet

### GrpcChat.Contracts
```xml
<PackageReference Include="Grpc.Tools" Version="2.60.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Google.Protobuf" Version="3.25.1" />
<PackageReference Include="Grpc.Core.Api" Version="2.60.0" />
```

### GrpcChat.Server
```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.60.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.1" />
<PackageReference Include="StackExchange.Redis" Version="2.7.33" />
```

### GrpcChat.Client
```xml
<PackageReference Include="Grpc.Net.Client" Version="2.60.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.1" />
```

---

## ✅ Verificação End-to-End

### Como Executar

#### 1. Iniciar Redis
```bash
docker run -d --name redis-chat -p 6379:6379 redis:alpine
```
Ou se tiver Redis instalado localmente:
```bash
redis-server
```

#### 2. Iniciar servidor gRPC
```bash
cd c:\dev\myprojects\gRpc.example\src\GrpcChat.Server
dotnet run
```

Aguardar log: `info: Connected to Redis at localhost:6379`

#### 3. Executar cliente
```bash
cd c:\dev\myprojects\gRpc.example\src\GrpcChat.Client
dotnet run
```

#### 4. (Opcional) Monitorar Redis
```bash
redis-cli
> MONITOR
```

### Cenários de Teste

#### 1. Unary RPC
**Passos**:
1. Escolher opção 1: "Registrar usuário"
2. Digitar username: "Alice"
3. Anotar o `userId` retornado

**Verificar**:
- Log do servidor: `info: User registered: Alice ({userId})`
- Log do cliente: `info: Successfully registered as Alice with ID {userId}`

**Próximo teste**:
1. Escolher opção 2: "Buscar status de usuário"
2. Digitar o `userId` de Alice
3. Verificar status "online"

#### 2. Server Streaming
**Passos**:
1. Escolher opção 3: "Receber mensagens de sala"
2. Digitar room ID: "general"
3. Observar mensagens chegando a cada 2-5 segundos
4. Aguardar pelo menos 5 mensagens
5. Pressionar Ctrl+C para cancelar

**Verificar**:
- Mensagens exibidas no formato: `[HH:mm:ss] ChatBot: Simulated message #N`
- Log do interceptador no servidor mostra: `Completed server streaming call. Messages sent: N`
- Graceful shutdown sem erros

**Verificar no Redis**:
```bash
redis-cli
> LRANGE room:general:messages 0 -1
```
Deve mostrar as mensagens salvas.

#### 3. Bidirectional Streaming
**Passos**:
1. Escolher opção 4: "Chat em tempo real"
2. Digitar room ID: "test"
3. Aguardar mensagem: `[SYSTEM] Welcome to room test!`
4. Digitar: "hello"
5. Observar resposta do bot
6. Digitar várias mensagens
7. Digitar: "exit"

**Verificar**:
- Mensagens enviadas aparecem imediatamente
- Respostas do bot quando digitar "hello"
- Log do interceptador mostra: `Received: N, Sent: M`
- Saída limpa sem exceções

#### 4. Deadline Exceeded
**Modificar código** temporariamente em `ChatClientService.RegisterUserAsync`:
```csharp
var deadline = DateTime.UtcNow.AddMilliseconds(1); // Forçar timeout
```

**Executar** e registrar usuário.

**Verificar**:
- `RpcException` com `StatusCode.DeadlineExceeded`
- Log: `error: Registration timed out`

**Reverter** código após teste.

### Logs Esperados

#### Servidor (Interceptor)
```
info: GrpcChat.Server.Extensions.RedisServiceExtensions[0]
      Connected to Redis at localhost:6379
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: GrpcChat.Server.Interceptors.ServerLoggingInterceptor[0]
      Starting unary call /chat.ChatService/RegisterUser from ipv4:127.0.0.1:xxxxx
info: GrpcChat.Server.Services.ChatService[0]
      User registered: Alice (abc-123-def)
info: GrpcChat.Server.Interceptors.ServerLoggingInterceptor[0]
      Completed unary call /chat.ChatService/RegisterUser in 15ms
info: GrpcChat.Server.Interceptors.ServerLoggingInterceptor[0]
      Starting server streaming call /chat.ChatService/ReceiveMessages from ipv4:127.0.0.1:xxxxx
info: GrpcChat.Server.Services.ChatService[0]
      User abc-123-def joining room general for message stream
info: GrpcChat.Server.Services.ChatService[0]
      Sending 0 historical messages from room general
info: GrpcChat.Server.Interceptors.ServerLoggingInterceptor[0]
      Completed server streaming call /chat.ChatService/ReceiveMessages. Duration: 30245ms, Messages sent: 8
info: GrpcChat.Server.Interceptors.ServerLoggingInterceptor[0]
      Starting bidirectional streaming call /chat.ChatService/ChatStream from ipv4:127.0.0.1:xxxxx
info: GrpcChat.Server.Services.ChatService[0]
      Bidirectional chat stream started for ipv4:127.0.0.1:xxxxx
info: GrpcChat.Server.Interceptors.ServerLoggingInterceptor[0]
      Completed bidirectional streaming call /chat.ChatService/ChatStream. Duration: 45123ms, Received: 5, Sent: 6
```

#### Cliente
```
info: GrpcChat.Client.Services.ChatClientService[0]
      Successfully registered as Alice with ID abc-123-def
info: GrpcChat.Client.Services.ChatClientService[0]
      User Alice is online, Last seen: 2026-01-21T14:23:10-03:00
info: GrpcChat.Client.Services.ChatClientService[0]
      Listening for messages in room general...
info: GrpcChat.Client.Services.ChatClientService[0]
      [14:23:15] ChatBot: Simulated message #1 in room general
info: GrpcChat.Client.Services.ChatClientService[0]
      [14:23:18] ChatBot: Simulated message #2 in room general
info: GrpcChat.Client.Services.ChatClientService[0]
      [14:23:22] ChatBot: Simulated message #3 in room general
```

---

## ✔️ Checklist de Qualidade

### Build e Compilação
- [ ] Todos os projetos compilam sem warnings
- [ ] Código gerado do .proto existe em `obj/Debug/net8.0/`
- [ ] Solução completa builda com `dotnet build`

### Infraestrutura
- [ ] Redis está rodando e acessível em `localhost:6379`
- [ ] Servidor inicia em `https://localhost:5001` e conecta ao Redis
- [ ] Log mostra "Connected to Redis"

### Dependency Injection
- [ ] `GrpcChannel` é singleton (verificar registration)
- [ ] `IConnectionMultiplexer` é singleton (verificar registration)
- [ ] Repositórios são scoped (não singleton)
- [ ] `ChatService.ChatServiceClient` é singleton

### gRPC e Streaming
- [ ] Interceptador registrado globalmente no servidor
- [ ] Deadlines configurados em chamadas do cliente
- [ ] `CancellationToken` propagado em todos os métodos de streaming
- [ ] `CompleteAsync()` chamado antes de disposal em bidirectional
- [ ] `using` utilizado para calls de streaming

### Logging e Observabilidade
- [ ] Logs estruturados com `ILogger` (não `Console.WriteLine`)
- [ ] Interceptador loga início/fim de calls
- [ ] Interceptador loga duração e contagem de mensagens
- [ ] Parâmetros estruturados nos logs (`{UserId}`, não concatenação)

### Persistência
- [ ] Dados persistem no Redis entre reinicializações do servidor
- [ ] Histórico de mensagens é carregado corretamente
- [ ] Chaves Redis seguem padrão: `user:{userId}`, `room:{roomId}:messages`

### Error Handling
- [ ] `RpcException` tratada com `StatusCode` específico
- [ ] Tratamento de `DeadlineExceeded`
- [ ] Tratamento de `Cancelled`
- [ ] Tratamento de exceção quando Redis está offline

### Código e Boas Práticas
- [ ] Nullable reference types habilitado e respeitado
- [ ] Async/await usado corretamente (sem `.Result` ou `.Wait()`)
- [ ] Repository Pattern implementado corretamente
- [ ] Interfaces segregadas (ISP)
- [ ] Extension methods para configuração (DRY)

---

## 🏭 Notas de Produção

### O que está implementado (production-ready)

✅ **Interceptadores** para logging e métricas
✅ **Deadlines** para prevenir conexões indefinidas
✅ **Gerenciamento correto de GrpcChannel** (singleton)
✅ **Graceful shutdown** com `CompleteAsync()`
✅ **Tratamento de erros** com `RpcException` e `StatusCode`
✅ **Logging estruturado** com `ILogger`
✅ **Cancellation tokens** em todos os streams
✅ **Persistência com Redis** (StackExchange.Redis)
✅ **Repository Pattern** para separação de responsabilidades
✅ **Injeção de dependência** correta (singleton para conexão, scoped para repositórios)
✅ **Async/await** em todas operações I/O
✅ **Resource management** com `using` pattern

### O que falta para produção real

⚠️ **Autenticação e autorização** (JWT em metadata)
⚠️ **SSL com certificados válidos** (remover `DangerousAcceptAnyServerCertificateValidator`)
⚠️ **Broadcasting real** de mensagens via Redis Pub/Sub (atual só ecoa de volta)
⚠️ **Retry policies** com exponential backoff para operações Redis
⚠️ **Health checks** para Redis e gRPC endpoints
⚠️ **Métricas** com OpenTelemetry ou Prometheus
⚠️ **Connection resilience** para Redis (reconexão automática)
⚠️ **TTL (Time To Live)** configurável para dados no Redis
⚠️ **Paginação adequada** para histórico de mensagens
⚠️ **Rate limiting** para prevenir abuse
⚠️ **Distributed tracing** (correlation IDs)

---

## 🗄️ Estrutura Redis (Chaves e Tipos)

### Usuários

**Chave**: `user:{userId}`
**Tipo**: String (JSON serializado)
**Exemplo**:
```
user:abc-123-def → {"UserId":"abc-123-def","Username":"Alice","RegisteredAt":1737488400}
```
**TTL**: Opcional (ex: 24h para limpeza automática de usuários inativos)
**Comandos**:
```redis
SET user:abc-123-def '{"UserId":"abc-123-def","Username":"Alice",...}'
GET user:abc-123-def
EXISTS user:abc-123-def
KEYS user:*
```

### Mensagens de Chat

**Chave**: `room:{roomId}:messages`
**Tipo**: List (FIFO com RPUSH/LPOP)
**Exemplo**:
```
room:general:messages → [msg1, msg2, msg3, ...]
```
**Limitação**: Manter apenas últimas 100 mensagens por sala
**Comandos**:
```redis
RPUSH room:general:messages '{"MessageId":"...","Content":"Hello",...}'
LRANGE room:general:messages 0 -1      # Todas mensagens
LRANGE room:general:messages -50 -1    # Últimas 50 mensagens
LTRIM room:general:messages -100 -1    # Manter apenas últimas 100
LLEN room:general:messages             # Contar mensagens
```

### Alternativa: Sorted Sets (mais avançado)

**Chave**: `room:{roomId}:messages`
**Tipo**: Sorted Set (score = timestamp Unix)
**Vantagem**: Permite buscar mensagens por intervalo de tempo
**Comandos**:
```redis
ZADD room:general:messages 1737488400 '{"MessageId":"...","Content":"Hello",...}'
ZRANGE room:general:messages 0 -1           # Todas mensagens (ordem cronológica)
ZREVRANGE room:general:messages 0 49        # Últimas 50 mensagens
ZRANGEBYSCORE room:general:messages 1737488400 1737492000  # Por intervalo de tempo
ZCARD room:general:messages                  # Contar mensagens
ZREMRANGEBYRANK room:general:messages 0 -101 # Manter apenas últimas 100
```

**Recomendação**: Usar **Sorted Sets** para produção (mais flexível).

---

## 🎯 Boas Práticas Implementadas

### 1. Separation of Concerns (SoC)
Repository Pattern separa lógica de persistência do serviço gRPC. `ChatService` não sabe que está usando Redis.

### 2. Dependency Injection
Uso correto de lifetimes:
- **Singleton**: `IConnectionMultiplexer`, `GrpcChannel` (conexões caras)
- **Scoped**: Repositórios (seguem lifecycle de request)
- **Transient**: Evitado (desnecessário aqui)

### 3. Interface Segregation Principle (ISP)
Interfaces pequenas e focadas:
- `IUserRepository` - apenas operações de usuário
- `IChatRepository` - apenas operações de chat

### 4. Async/Await
Todas operações I/O são assíncronas. Nunca usar `.Result` ou `.Wait()`.

### 5. Structured Logging
Parâmetros estruturados:
```csharp
_logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
// ✅ Permite filtragem e agregação

// ❌ Evitar concatenação
_logger.LogInformation($"User {userId} joined room {roomId}");
```

### 6. Error Handling
- Try-catch em boundaries (repositories, interceptors)
- `RpcException` para erros de negócio com `StatusCode` apropriado
- Log de exceções com contexto

### 7. Configuration Management
Connection strings em `appsettings.json`, não hardcoded:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### 8. Resource Management
Uso de `using` para disposal correto:
```csharp
using var call = _client.ReceiveMessages(request);
// ✅ Garantido disposal mesmo com exceção
```

### 9. Graceful Shutdown
Bidirectional streaming:
```csharp
await call.RequestStream.CompleteAsync();  // Avisar que terminou de enviar
await readTask;                            // Aguardar todas respostas
// ✅ Shutdown limpo sem perder mensagens
```

### 10. Single Responsibility Principle (SRP)
Cada classe tem uma responsabilidade:
- `ChatService` - lógica de negócio gRPC
- `UserRepository` - persistência de usuários
- `ServerLoggingInterceptor` - cross-cutting concern de logging
- `RedisServiceExtensions` - configuração de DI

---

## 🎓 Conceitos Demonstrados

### gRPC
- ✅ Unary RPC (request-response simples)
- ✅ Server Streaming (servidor envia múltiplas respostas)
- ✅ Bidirectional Streaming (ambos enviam/recebem simultaneamente)
- ✅ Protocol Buffers (definição de contrato)
- ✅ Interceptors (cross-cutting concerns)
- ✅ Deadlines (timeout management)
- ✅ CancellationToken (graceful shutdown)
- ✅ Error handling com StatusCode

### .NET
- ✅ Dependency Injection (lifetimes corretos)
- ✅ Extension methods (configuração limpa)
- ✅ ILogger (structured logging)
- ✅ IConfiguration (configuration management)
- ✅ Async/await (non-blocking I/O)
- ✅ using pattern (resource disposal)
- ✅ Nullable reference types

### Redis
- ✅ StackExchange.Redis (cliente oficial)
- ✅ IConnectionMultiplexer (singleton para performance)
- ✅ IDatabase (thread-safe operations)
- ✅ Strings (JSON serialization)
- ✅ Lists (message queues)
- ✅ Key patterns (namespacing)

### Patterns
- ✅ Repository Pattern
- ✅ Dependency Injection
- ✅ Extension Methods
- ✅ Builder Pattern (fluent configuration)
- ✅ Singleton Pattern
- ✅ Factory Pattern (client creation)

### SOLID
- ✅ Single Responsibility Principle
- ✅ Open/Closed Principle (extensible via interfaces)
- ✅ Liskov Substitution Principle (interfaces substituíveis)
- ✅ Interface Segregation Principle
- ✅ Dependency Inversion Principle (depende de abstrações)

---

## 📝 Conclusão

Este projeto demonstra todos os conceitos fundamentais de **gRPC streaming** com **persistência Redis**, seguindo **boas práticas de desenvolvimento .NET**, mantendo **simplicidade sem sacrificar qualidade**.

É um excelente ponto de partida para:
- Aprender gRPC avançado (todos os padrões de comunicação)
- Entender persistência com Redis
- Aplicar SOLID e Design Patterns
- Preparar para ambientes de produção

**Próximos passos sugeridos**:
1. Implementar autenticação com JWT
2. Adicionar Redis Pub/Sub para broadcasting real
3. Implementar health checks
4. Adicionar OpenTelemetry para observabilidade
5. Deploy em Kubernetes

---

**Versão**: 1.0
**Última atualização**: 2026-01-21
**Autor**: Claude Code (Anthropic)
**Licença**: MIT
