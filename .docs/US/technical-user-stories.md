# User Stories Técnicas - Sistema gRPC de Chat com Streaming + Redis

**Projeto**: GrpcChat
**Versão**: 1.0
**Data**: 2026-01-21
**Epic**: Implementação de sistema gRPC com streaming e persistência Redis

---

## 📋 Índice de User Stories

### Fase 1: Fundação (Contratos)
- [US-001](#us-001) - Criar estrutura de projetos
- [US-002](#us-002) - Definir contratos Protocol Buffers

### Fase 2: Servidor - Persistência
- [US-003](#us-003) - Implementar Repository Pattern para Redis
- [US-004](#us-004) - Implementar UserRepository com Redis
- [US-005](#us-005) - Implementar ChatRepository com Redis

### Fase 3: Servidor - gRPC Services
- [US-006](#us-006) - Implementar ServerLoggingInterceptor
- [US-007](#us-007) - Implementar métodos unários do ChatService
- [US-008](#us-008) - Implementar Server Streaming no ChatService
- [US-009](#us-009) - Implementar Bidirectional Streaming no ChatService

### Fase 4: Cliente
- [US-010](#us-010) - Implementar configuração de GrpcChannel
- [US-011](#us-011) - Implementar chamadas unárias no cliente
- [US-012](#us-012) - Implementar Server Streaming no cliente
- [US-013](#us-013) - Implementar Bidirectional Streaming no cliente
- [US-014](#us-014) - Implementar menu interativo do cliente

### Fase 5: Testes e Qualidade
- [US-015](#us-015) - Configurar ambiente de testes com Redis
- [US-016](#us-016) - Implementar testes de persistência
- [US-017](#us-017) - Implementar testes de cenários de erro

---

## Fase 1: Fundação (Contratos)

### US-001
**Título**: Criar estrutura de projetos da solução gRPC

**Como** desenvolvedor
**Quero** ter a estrutura básica dos 3 projetos criada
**Para que** possa começar a desenvolver os componentes individuais

#### Critérios de Aceitação
- [ ] Diretório `src/GrpcChat.Contracts` criado
- [ ] Diretório `src/GrpcChat.Server` criado
- [ ] Diretório `src/GrpcChat.Client` criado
- [ ] Arquivo `GrpcChat.Contracts.csproj` criado com target framework `net8.0`
- [ ] Arquivo `GrpcChat.Server.csproj` criado como Web SDK
- [ ] Arquivo `GrpcChat.Client.csproj` criado como Console app
- [ ] Pacotes NuGet básicos adicionados a cada projeto
- [ ] Todos os projetos compilam sem erros

#### Tarefas Técnicas
1. Criar estrutura de pastas em `src/`
2. Executar `dotnet new classlib -n GrpcChat.Contracts -f net8.0`
3. Executar `dotnet new web -n GrpcChat.Server -f net8.0`
4. Executar `dotnet new console -n GrpcChat.Client -f net8.0`
5. Adicionar pacotes NuGet básicos:
   - Contracts: `Grpc.Tools`, `Google.Protobuf`, `Grpc.Core.Api`
   - Server: `Grpc.AspNetCore`, `StackExchange.Redis`
   - Client: `Grpc.Net.Client`, `Microsoft.Extensions.Hosting`
6. Habilitar `ImplicitUsings` e `Nullable` em todos os projetos

#### Definição de Pronto (DoD)
- Todos os 3 projetos compilam com `dotnet build`
- Nenhum warning de compilação
- Solution file criado e todos os projetos referenciados
- Estrutura de pastas conforme arquitetura definida

#### Estimativa
**Pontos**: 2
**Tempo**: 30 minutos

---

### US-002
**Título**: Definir contratos Protocol Buffers (.proto)

**Como** desenvolvedor
**Quero** ter todos os contratos gRPC definidos em Protocol Buffers
**Para que** servidor e cliente compartilhem a mesma definição de interface

#### Critérios de Aceitação
- [ ] Arquivo `Protos/chat.proto` criado
- [ ] Namespace C# definido como `GrpcChat.Contracts`
- [ ] Mensagens `User`, `RegisterUserRequest`, `RegisterUserResponse` definidas
- [ ] Mensagens `GetUserStatusRequest`, `UserStatus` definidas
- [ ] Mensagens `JoinRoomRequest`, `ChatMessage` definidas
- [ ] Mensagens `StreamChatRequest`, `StreamChatResponse` com `oneof` definidas
- [ ] Enum `MessageType` com valores REGULAR, SYSTEM, JOIN, LEAVE
- [ ] Service `ChatService` com 4 métodos RPC definidos
- [ ] Código C# gerado automaticamente no build

#### Tarefas Técnicas
1. Criar pasta `Protos/` em GrpcChat.Contracts
2. Criar arquivo `chat.proto` com syntax proto3
3. Definir mensagens para operações unárias:
   ```protobuf
   message User {
     string user_id = 1;
     string username = 2;
     int64 registered_at = 3;
   }

   message RegisterUserRequest {
     string username = 1;
   }

   message RegisterUserResponse {
     User user = 1;
     bool success = 2;
     string message = 3;
   }
   ```
4. Definir mensagens para streaming:
   ```protobuf
   message ChatMessage {
     string message_id = 1;
     string user_id = 2;
     string username = 3;
     string room_id = 4;
     string content = 5;
     int64 timestamp = 6;
     MessageType type = 7;
   }
   ```
5. Definir service com todos os métodos:
   ```protobuf
   service ChatService {
     rpc RegisterUser (RegisterUserRequest) returns (RegisterUserResponse);
     rpc GetUserStatus (GetUserStatusRequest) returns (UserStatus);
     rpc ReceiveMessages (JoinRoomRequest) returns (stream ChatMessage);
     rpc ChatStream (stream StreamChatRequest) returns (stream StreamChatResponse);
   }
   ```
6. Configurar `.csproj` para gerar código:
   ```xml
   <ItemGroup>
     <Protobuf Include="Protos\chat.proto" GrpcServices="Both" />
   </ItemGroup>
   ```
7. Executar build e verificar código gerado

#### Definição de Pronto (DoD)
- Arquivo `.proto` válido e sem erros de sintaxe
- Build gera classes C# em `obj/Debug/net8.0/`
- Classes `ChatService.ChatServiceBase` e `ChatService.ChatServiceClient` acessíveis
- Todas as mensagens compilam sem erros
- Numeração de campos sequencial e sem gaps

#### Estimativa
**Pontos**: 3
**Tempo**: 1 hora

---

## Fase 2: Servidor - Persistência

### US-003
**Título**: Implementar configuração de DI para Redis

**Como** desenvolvedor
**Quero** ter uma configuração centralizada de Redis via Dependency Injection
**Para que** o servidor possa conectar e reutilizar a conexão corretamente

#### Critérios de Aceitação
- [ ] Classe `RedisServiceExtensions` criada
- [ ] Extension method `AddRedisServices(IConfiguration)` implementado
- [ ] `IConnectionMultiplexer` registrado como singleton
- [ ] Connection string lida de `appsettings.json`
- [ ] Fallback para `localhost:6379` se não configurado
- [ ] Log de conexão bem-sucedida
- [ ] Tratamento de erro se Redis não disponível

#### Tarefas Técnicas
1. Criar pasta `Extensions/` em GrpcChat.Server
2. Criar arquivo `RedisServiceExtensions.cs`
3. Implementar extension method:
   ```csharp
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
                   logger.LogError(ex, "Failed to connect to Redis");
                   throw;
               }
           });

           return services;
       }
   }
   ```
4. Adicionar connection string em `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "Redis": "localhost:6379"
     }
   }
   ```
5. Registrar no `Program.cs`:
   ```csharp
   builder.Services.AddRedisServices(builder.Configuration);
   ```

#### Definição de Pronto (DoD)
- Extension method compila sem erros
- Servidor inicia e loga "Connected to Redis"
- `IConnectionMultiplexer` injetável em outras classes
- Singleton verificado (mesma instância em múltiplas injeções)
- Falha graciosamente se Redis offline (com log de erro)

#### Estimativa
**Pontos**: 2
**Tempo**: 45 minutos

---

### US-004
**Título**: Implementar UserRepository com Redis

**Como** desenvolvedor
**Quero** ter um repositório para gerenciar usuários no Redis
**Para que** possa persistir e recuperar dados de usuários

#### Critérios de Aceitação
- [ ] Interface `IUserRepository` criada
- [ ] Classe `UserRepository` implementada
- [ ] Método `AddUserAsync(User)` salva usuário no Redis
- [ ] Método `GetUserAsync(userId)` recupera usuário do Redis
- [ ] Método `UserExistsAsync(userId)` verifica existência
- [ ] Serialização JSON implementada
- [ ] Chave Redis no formato `user:{userId}`
- [ ] Logging estruturado em operações
- [ ] Tratamento de exceções

#### Tarefas Técnicas
1. Criar pasta `Repositories/` em GrpcChat.Server
2. Criar interface `IUserRepository.cs`:
   ```csharp
   public interface IUserRepository
   {
       Task<bool> AddUserAsync(User user);
       Task<User?> GetUserAsync(string userId);
       Task<bool> UserExistsAsync(string userId);
   }
   ```
3. Criar classe `UserRepository.cs`:
   ```csharp
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
               _logger.LogError(ex, "Error saving user {UserId}", user.UserId);
               throw;
           }
       }

       // Implementar GetUserAsync e UserExistsAsync...
   }
   ```
4. Registrar no `RedisServiceExtensions`:
   ```csharp
   services.AddScoped<IUserRepository, UserRepository>();
   ```
5. Escrever testes unitários básicos

#### Definição de Pronto (DoD)
- Interface e implementação compilam
- Repositório registrado no DI como scoped
- Todas as 3 operações funcionam corretamente
- Dados persistem no Redis (verificável com `redis-cli`)
- Logs estruturados em todas as operações
- Exceções tratadas e logadas
- Testes unitários passam

#### Estimativa
**Pontos**: 3
**Tempo**: 1.5 horas

---

### US-005
**Título**: Implementar ChatRepository com Redis

**Como** desenvolvedor
**Quero** ter um repositório para gerenciar mensagens de chat no Redis
**Para que** possa persistir histórico de conversas

#### Critérios de Aceitação
- [ ] Interface `IChatRepository` criada
- [ ] Classe `ChatRepository` implementada
- [ ] Método `AddMessageAsync(ChatMessage)` salva mensagem no Redis
- [ ] Método `GetRecentMessagesAsync(roomId, count)` recupera histórico
- [ ] Redis Lists usado para armazenamento
- [ ] Chave Redis no formato `room:{roomId}:messages`
- [ ] Limitação de 100 mensagens por sala (LTRIM)
- [ ] Serialização JSON implementada
- [ ] Logging estruturado

#### Tarefas Técnicas
1. Criar interface `IChatRepository.cs`:
   ```csharp
   public interface IChatRepository
   {
       Task<bool> AddMessageAsync(ChatMessage message);
       Task<List<ChatMessage>> GetRecentMessagesAsync(string roomId, int count = 50);
   }
   ```
2. Criar classe `ChatRepository.cs`:
   ```csharp
   public class ChatRepository : IChatRepository
   {
       private readonly IDatabase _db;
       private readonly ILogger<ChatRepository> _logger;
       private const int MaxMessagesPerRoom = 100;

       public ChatRepository(IConnectionMultiplexer redis, ILogger<ChatRepository> logger)
       {
           _db = redis.GetDatabase();
           _logger = logger;
       }

       public async Task<bool> AddMessageAsync(ChatMessage message)
       {
           try
           {
               var key = $"room:{message.RoomId}:messages";
               var json = JsonSerializer.Serialize(message);

               // Adicionar ao final da lista
               await _db.ListRightPushAsync(key, json);

               // Manter apenas últimas 100 mensagens
               await _db.ListTrimAsync(key, -MaxMessagesPerRoom, -1);

               _logger.LogDebug("Message added to room {RoomId}", message.RoomId);
               return true;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error adding message to room {RoomId}", message.RoomId);
               throw;
           }
       }

       public async Task<List<ChatMessage>> GetRecentMessagesAsync(string roomId, int count = 50)
       {
           try
           {
               var key = $"room:{roomId}:messages";
               var messages = new List<ChatMessage>();

               // Pegar últimas N mensagens
               var values = await _db.ListRangeAsync(key, -count, -1);

               foreach (var value in values)
               {
                   if (value.HasValue)
                   {
                       var message = JsonSerializer.Deserialize<ChatMessage>(value!);
                       if (message != null)
                           messages.Add(message);
                   }
               }

               _logger.LogInformation("Retrieved {Count} messages from room {RoomId}", messages.Count, roomId);
               return messages;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error retrieving messages from room {RoomId}", roomId);
               return new List<ChatMessage>();
           }
       }
   }
   ```
3. Registrar no `RedisServiceExtensions`:
   ```csharp
   services.AddScoped<IChatRepository, ChatRepository>();
   ```

#### Definição de Pronto (DoD)
- Interface e implementação compilam
- Repositório registrado no DI como scoped
- Mensagens são salvas e recuperadas corretamente
- LTRIM funciona (apenas 100 mensagens mantidas)
- Dados verificáveis com `redis-cli LRANGE`
- Logs estruturados
- Testes unitários passam

#### Estimativa
**Pontos**: 3
**Tempo**: 1.5 horas

---

## Fase 3: Servidor - gRPC Services

### US-006
**Título**: Implementar ServerLoggingInterceptor

**Como** desenvolvedor
**Quero** ter um interceptador que logue todas as chamadas gRPC
**Para que** possa monitorar duração, contagem de mensagens e erros

#### Critérios de Aceitação
- [ ] Classe `ServerLoggingInterceptor` criada
- [ ] Herda de `Grpc.Core.Interceptors.Interceptor`
- [ ] Método `UnaryServerHandler` implementado
- [ ] Método `ServerStreamingServerHandler` implementado
- [ ] Método `DuplexStreamingServerHandler` implementado
- [ ] Classes wrapper `CountingServerStreamWriter` e `CountingAsyncStreamReader` criadas
- [ ] Logs incluem: método, peer, duração, contagem de mensagens
- [ ] Usa `Stopwatch` para medir tempo
- [ ] Usa `ILogger` estruturado

#### Tarefas Técnicas
1. Criar pasta `Interceptors/` em GrpcChat.Server
2. Criar classes wrapper:
   ```csharp
   internal class CountingServerStreamWriter<T> : IServerStreamWriter<T>
   {
       private readonly IServerStreamWriter<T> _inner;
       private readonly Action _onWrite;

       public CountingServerStreamWriter(IServerStreamWriter<T> inner, Action onWrite)
       {
           _inner = inner;
           _onWrite = onWrite;
       }

       public WriteOptions? WriteOptions
       {
           get => _inner.WriteOptions;
           set => _inner.WriteOptions = value;
       }

       public async Task WriteAsync(T message)
       {
           _onWrite();
           await _inner.WriteAsync(message);
       }
   }

   internal class CountingAsyncStreamReader<T> : IAsyncStreamReader<T>
   {
       private readonly IAsyncStreamReader<T> _inner;
       private readonly Action _onRead;

       public CountingAsyncStreamReader(IAsyncStreamReader<T> inner, Action onRead)
       {
           _inner = inner;
           _onRead = onRead;
       }

       public T Current => _inner.Current;

       public async Task<bool> MoveNext(CancellationToken cancellationToken)
       {
           var result = await _inner.MoveNext(cancellationToken);
           if (result)
               _onRead();
           return result;
       }
   }
   ```
3. Criar `ServerLoggingInterceptor.cs`:
   ```csharp
   public class ServerLoggingInterceptor : Interceptor
   {
       private readonly ILogger<ServerLoggingInterceptor> _logger;

       public ServerLoggingInterceptor(ILogger<ServerLoggingInterceptor> logger)
       {
           _logger = logger;
       }

       public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
           TRequest request,
           ServerCallContext context,
           UnaryServerMethod<TRequest, TResponse> continuation)
       {
           var stopwatch = Stopwatch.StartNew();
           var method = context.Method;
           var peer = context.Peer;

           _logger.LogInformation("Starting unary call {Method} from {Peer}", method, peer);

           try
           {
               var response = await continuation(request, context);
               stopwatch.Stop();
               _logger.LogInformation("Completed unary call {Method} in {Duration}ms", method, stopwatch.ElapsedMilliseconds);
               return response;
           }
           catch (Exception ex)
           {
               stopwatch.Stop();
               _logger.LogError(ex, "Error in unary call {Method} after {Duration}ms", method, stopwatch.ElapsedMilliseconds);
               throw;
           }
       }

       // Implementar ServerStreamingServerHandler e DuplexStreamingServerHandler...
   }
   ```
4. Registrar no `Program.cs`:
   ```csharp
   builder.Services.AddGrpc(options =>
   {
       options.Interceptors.Add<ServerLoggingInterceptor>();
       options.EnableDetailedErrors = true;
   });
   ```

#### Definição de Pronto (DoD)
- Interceptor compila sem erros
- Registrado globalmente no servidor
- Logs aparecem para todas as chamadas
- Contagem de mensagens precisa em streaming
- Duração medida corretamente
- Exceções logadas apropriadamente
- Testes de integração passam

#### Estimativa
**Pontos**: 5
**Tempo**: 2 horas

---

### US-007
**Título**: Implementar métodos unários do ChatService

**Como** desenvolvedor
**Quero** ter os métodos unários RegisterUser e GetUserStatus implementados
**Para que** clientes possam registrar usuários e consultar status

#### Critérios de Aceitação
- [ ] Classe `ChatService` criada herdando de `ChatService.ChatServiceBase`
- [ ] Método `RegisterUser` implementado
- [ ] Método `GetUserStatus` implementado
- [ ] `IUserRepository` injetado via construtor
- [ ] GUID gerado para userId em RegisterUser
- [ ] Usuário salvo no Redis via repositório
- [ ] GetUserStatus retorna dados do Redis
- [ ] `RpcException` com `StatusCode.NotFound` se usuário não existe
- [ ] Logging estruturado em ambos os métodos

#### Tarefas Técnicas
1. Criar pasta `Services/` em GrpcChat.Server
2. Criar `ChatService.cs`:
   ```csharp
   public class ChatService : Contracts.ChatService.ChatServiceBase
   {
       private readonly IUserRepository _userRepository;
       private readonly IChatRepository _chatRepository;
       private readonly ILogger<ChatService> _logger;

       public ChatService(
           IUserRepository userRepository,
           IChatRepository chatRepository,
           ILogger<ChatService> logger)
       {
           _userRepository = userRepository;
           _chatRepository = chatRepository;
           _logger = logger;
       }

       public override async Task<RegisterUserResponse> RegisterUser(
           RegisterUserRequest request,
           ServerCallContext context)
       {
           var userId = Guid.NewGuid().ToString();
           var user = new User
           {
               UserId = userId,
               Username = request.Username,
               RegisteredAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
           };

           var success = await _userRepository.AddUserAsync(user);

           if (success)
           {
               _logger.LogInformation("User registered: {Username} ({UserId})", request.Username, userId);
               return new RegisterUserResponse
               {
                   User = user,
                   Success = true,
                   Message = "User registered successfully"
               };
           }

           _logger.LogWarning("Failed to register user: {Username}", request.Username);
           return new RegisterUserResponse
           {
               Success = false,
               Message = "Failed to register user"
           };
       }

       public override async Task<UserStatus> GetUserStatus(
           GetUserStatusRequest request,
           ServerCallContext context)
       {
           var user = await _userRepository.GetUserAsync(request.UserId);

           if (user == null)
           {
               _logger.LogWarning("User not found: {UserId}", request.UserId);
               throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
           }

           return new UserStatus
           {
               UserId = user.UserId,
               Username = user.Username,
               IsOnline = true,
               LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
           };
       }
   }
   ```
3. Registrar no `Program.cs`:
   ```csharp
   app.MapGrpcService<ChatService>();
   ```

#### Definição de Pronto (DoD)
- Ambos os métodos compilam
- RegisterUser cria usuário e salva no Redis
- GetUserStatus recupera usuário do Redis
- NotFound retornado para usuário inexistente
- Logs estruturados em todas as operações
- Service mapeado no Program.cs
- Testável com cliente gRPC

#### Estimativa
**Pontos**: 3
**Tempo**: 1 hora

---

### US-008
**Título**: Implementar Server Streaming no ChatService

**Como** desenvolvedor
**Quero** ter o método ReceiveMessages implementado com server streaming
**Para que** clientes possam receber mensagens de uma sala em tempo real

#### Critérios de Aceitação
- [ ] Método `ReceiveMessages` implementado
- [ ] Histórico de mensagens carregado do Redis primeiro
- [ ] Loop infinito com `Task.Delay(2-5s)` para simular mensagens
- [ ] Novas mensagens salvas no Redis antes de enviar
- [ ] `context.CancellationToken` verificado
- [ ] `OperationCanceledException` tratada graciosamente
- [ ] Logging de início, fim e cancelamento
- [ ] Mensagens enviadas via `responseStream.WriteAsync()`

#### Tarefas Técnicas
1. Implementar método em `ChatService.cs`:
   ```csharp
   public override async Task ReceiveMessages(
       JoinRoomRequest request,
       IServerStreamWriter<ChatMessage> responseStream,
       ServerCallContext context)
   {
       _logger.LogInformation("User {UserId} joining room {RoomId} for message stream",
           request.UserId, request.RoomId);

       // FASE 1: Enviar histórico
       var history = await _chatRepository.GetRecentMessagesAsync(request.RoomId, 50);
       _logger.LogInformation("Sending {Count} historical messages from room {RoomId}",
           history.Count, request.RoomId);

       foreach (var msg in history)
       {
           await responseStream.WriteAsync(msg);
       }

       // FASE 2: Streaming de novas mensagens
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
               _logger.LogInformation("Message stream cancelled for room {RoomId}", request.RoomId);
               break;
           }
       }
   }
   ```

#### Definição de Pronto (DoD)
- Método compila sem erros
- Histórico carregado e enviado primeiro
- Novas mensagens geradas a cada 2-5 segundos
- Mensagens salvas no Redis antes de enviar
- Cancellation token respeitado
- Graceful shutdown sem exceções
- Logs estruturados
- Testável com cliente gRPC

#### Estimativa
**Pontos**: 5
**Tempo**: 2 horas

---

### US-009
**Título**: Implementar Bidirectional Streaming no ChatService

**Como** desenvolvedor
**Quero** ter o método ChatStream implementado com streaming bidirecional
**Para que** clientes possam enviar e receber mensagens simultaneamente

#### Critérios de Aceitação
- [ ] Método `ChatStream` implementado
- [ ] `await foreach` usado para ler `requestStream.ReadAllAsync()`
- [ ] Switch no `request.RequestCase` para processar diferentes tipos
- [ ] Join, Message e LeaveRoom tratados
- [ ] Mensagens salvas no Redis via repositório
- [ ] Respostas enviadas via `responseStream.WriteAsync()`
- [ ] Simulação de resposta de bot quando "hello" detectado
- [ ] Logging de início, fim e quantidade de mensagens

#### Tarefas Técnicas
1. Implementar método em `ChatService.cs`:
   ```csharp
   public override async Task ChatStream(
       IAsyncStreamReader<StreamChatRequest> requestStream,
       IServerStreamWriter<StreamChatResponse> responseStream,
       ServerCallContext context)
   {
       var clientId = context.Peer;
       _logger.LogInformation("Bidirectional chat stream started for {Client}", clientId);

       try
       {
           await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
           {
               switch (request.RequestCase)
               {
                   case StreamChatRequest.RequestOneofCase.Join:
                       await HandleJoinRoom(request.Join, responseStream);
                       break;

                   case StreamChatRequest.RequestOneofCase.Message:
                       await HandleChatMessage(request.Message, responseStream);
                       break;

                   case StreamChatRequest.RequestOneofCase.LeaveRoom:
                       await HandleLeaveRoom(request.LeaveRoom, responseStream);
                       break;

                   default:
                       _logger.LogWarning("Unknown request type received");
                       break;
               }
           }
       }
       catch (OperationCanceledException)
       {
           _logger.LogInformation("Chat stream cancelled for {Client}", clientId);
       }
   }

   private async Task HandleJoinRoom(
       JoinRoomRequest request,
       IServerStreamWriter<StreamChatResponse> responseStream)
   {
       _logger.LogInformation("User {UserId} joining room {RoomId}",
           request.UserId, request.RoomId);

       var response = new StreamChatResponse
       {
           SystemMessage = $"Welcome to room {request.RoomId}!"
       };

       await responseStream.WriteAsync(response);
   }

   private async Task HandleChatMessage(
       ChatMessage message,
       IServerStreamWriter<StreamChatResponse> responseStream)
   {
       // Salvar mensagem
       await _chatRepository.AddMessageAsync(message);

       // Echo message back
       var response = new StreamChatResponse
       {
           Message = message
       };
       await responseStream.WriteAsync(response);

       // Simular bot response para "hello"
       if (message.Content.Contains("hello", StringComparison.OrdinalIgnoreCase))
       {
           await Task.Delay(500);

           var botResponse = new StreamChatResponse
           {
               Message = new ChatMessage
               {
                   MessageId = Guid.NewGuid().ToString(),
                   UserId = "bot",
                   Username = "ChatBot",
                   RoomId = message.RoomId,
                   Content = $"Hello {message.Username}! How can I help you?",
                   Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                   Type = MessageType.Regular
               }
           };

           await responseStream.WriteAsync(botResponse);
       }
   }

   private async Task HandleLeaveRoom(
       string roomId,
       IServerStreamWriter<StreamChatResponse> responseStream)
   {
       _logger.LogInformation("User leaving room {RoomId}", roomId);

       var response = new StreamChatResponse
       {
           SystemMessage = $"You left room {roomId}"
       };

       await responseStream.WriteAsync(response);
   }
   ```

#### Definição de Pronto (DoD)
- Método compila sem erros
- Todos os 3 tipos de request processados
- Mensagens salvas no Redis
- Bot responde a "hello"
- await foreach funciona corretamente
- Cancellation tratada graciosamente
- Logs estruturados
- Testável com cliente gRPC

#### Estimativa
**Pontos**: 8
**Tempo**: 3 horas

---

## Fase 4: Cliente

### US-010
**Título**: Implementar configuração de GrpcChannel

**Como** desenvolvedor
**Quero** ter o GrpcChannel configurado como singleton
**Para que** a conexão HTTP/2 seja reutilizada eficientemente

#### Critérios de Aceitação
- [ ] Classe `GrpcClientExtensions` criada
- [ ] Extension method `AddGrpcChatClient(string)` implementado
- [ ] `GrpcChannel` registrado como singleton
- [ ] `ChatService.ChatServiceClient` registrado como singleton
- [ ] `HttpHandler` configurado para aceitar certificados self-signed (dev)
- [ ] `ChatClientService` registrado no DI
- [ ] Configuração aplicada em `Program.cs`

#### Tarefas Técnicas
1. Criar pasta `Extensions/` em GrpcChat.Client
2. Criar `GrpcClientExtensions.cs`:
   ```csharp
   public static class GrpcClientExtensions
   {
       public static IServiceCollection AddGrpcChatClient(
           this IServiceCollection services,
           string serverAddress)
       {
           // Registrar GrpcChannel como SINGLETON (crítico!)
           services.AddSingleton(sp =>
           {
               return GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
               {
                   HttpHandler = new HttpClientHandler
                   {
                       ServerCertificateCustomValidationCallback =
                           HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                   }
               });
           });

           // Registrar gRPC client stub
           services.AddSingleton(sp =>
           {
               var channel = sp.GetRequiredService<GrpcChannel>();
               return new ChatService.ChatServiceClient(channel);
           });

           // Registrar wrapper service
           services.AddSingleton<ChatClientService>();

           return services;
       }
   }
   ```
3. Usar em `Program.cs`:
   ```csharp
   builder.Services.AddGrpcChatClient("https://localhost:5001");
   ```

#### Definição de Pronto (DoD)
- Extension method compila
- GrpcChannel é singleton (verificado)
- Cliente registrado e injetável
- Conexão estabelecida com servidor
- Certificados self-signed aceitos em dev
- Documentação inline sobre singleton

#### Estimativa
**Pontos**: 2
**Tempo**: 30 minutos

---

### US-011
**Título**: Implementar chamadas unárias no cliente

**Como** desenvolvedor
**Quero** ter os métodos RegisterUserAsync e GetUserStatusAsync implementados
**Para que** possa testar operações unárias gRPC

#### Critérios de Aceitação
- [ ] Classe `ChatClientService` criada
- [ ] Método `RegisterUserAsync(string username)` implementado
- [ ] Método `GetUserStatusAsync(string userId)` implementado
- [ ] Deadlines configurados (5s para register, 3s para status)
- [ ] `RpcException` tratada com diferentes StatusCode
- [ ] `StatusCode.DeadlineExceeded` tratado especificamente
- [ ] Usuário atual armazenado após registro
- [ ] Logging estruturado

#### Tarefas Técnicas
1. Criar pasta `Services/` em GrpcChat.Client
2. Criar `ChatClientService.cs`:
   ```csharp
   public class ChatClientService
   {
       private readonly ChatService.ChatServiceClient _client;
       private readonly ILogger<ChatClientService> _logger;
       private User? _currentUser;

       public ChatClientService(
           ChatService.ChatServiceClient client,
           ILogger<ChatClientService> logger)
       {
           _client = client;
           _logger = logger;
       }

       public async Task<User?> RegisterUserAsync(string username)
       {
           try
           {
               var request = new RegisterUserRequest { Username = username };
               var deadline = DateTime.UtcNow.AddSeconds(5);

               var response = await _client.RegisterUserAsync(
                   request,
                   deadline: deadline);

               if (response.Success)
               {
                   _currentUser = response.User;
                   _logger.LogInformation(
                       "Successfully registered as {Username} with ID {UserId}",
                       username, response.User.UserId);
                   return response.User;
               }

               _logger.LogWarning("Registration failed: {Message}", response.Message);
               return null;
           }
           catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
           {
               _logger.LogError("Registration timed out after 5 seconds");
               return null;
           }
           catch (RpcException ex)
           {
               _logger.LogError(ex, "RPC error during registration: {Status}", ex.Status);
               return null;
           }
       }

       public async Task GetUserStatusAsync(string userId)
       {
           try
           {
               var request = new GetUserStatusRequest { UserId = userId };
               var deadline = DateTime.UtcNow.AddSeconds(3);

               var response = await _client.GetUserStatusAsync(
                   request,
                   deadline: deadline);

               _logger.LogInformation(
                   "User {Username} is {Status}, Last seen: {LastSeen}",
                   response.Username,
                   response.IsOnline ? "online" : "offline",
                   DateTimeOffset.FromUnixTimeSeconds(response.LastSeen));
           }
           catch (RpcException ex)
           {
               _logger.LogError(ex, "Failed to get user status: {Status}", ex.Status);
           }
       }
   }
   ```

#### Definição de Pronto (DoD)
- Ambos os métodos compilam
- Deadlines configurados corretamente
- RpcException tratada apropriadamente
- Logs estruturados
- Testável com servidor rodando
- Usuário persistido após registro

#### Estimativa
**Pontos**: 3
**Tempo**: 1 hora

---

### US-012
**Título**: Implementar Server Streaming no cliente

**Como** desenvolvedor
**Quero** ter o método ReceiveMessagesAsync implementado
**Para que** possa receber mensagens de uma sala em streaming

#### Critérios de Aceitação
- [ ] Método `ReceiveMessagesAsync(roomId, cancellationToken)` implementado
- [ ] `await foreach` com `ReadAllAsync()` usado
- [ ] `using var call` para disposal correto
- [ ] Deadline de 60 segundos configurado
- [ ] `CancellationToken` propagado
- [ ] `StatusCode.DeadlineExceeded` e `StatusCode.Cancelled` tratados
- [ ] Mensagens logadas com timestamp formatado
- [ ] Verificação se usuário registrado

#### Tarefas Técnicas
1. Adicionar método em `ChatClientService.cs`:
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

           var deadline = DateTime.UtcNow.AddSeconds(60);

           using var call = _client.ReceiveMessages(
               request,
               deadline: deadline,
               cancellationToken: cancellationToken);

           _logger.LogInformation("Listening for messages in room {RoomId}...", roomId);

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

#### Definição de Pronto (DoD)
- Método compila sem erros
- await foreach funciona corretamente
- Histórico recebido primeiro
- Novas mensagens aparecem em tempo real
- Deadline e cancellation funcionam
- Logs formatados corretamente
- Testável com servidor rodando

#### Estimativa
**Pontos**: 5
**Tempo**: 1.5 horas

---

### US-013
**Título**: Implementar Bidirectional Streaming no cliente

**Como** desenvolvedor
**Quero** ter o método ChatStreamAsync implementado
**Para que** possa enviar e receber mensagens simultaneamente

#### Critérios de Aceitação
- [ ] Método `ChatStreamAsync(roomId, cancellationToken)` implementado
- [ ] Background task criado com `Task.Run` para leitura
- [ ] `await foreach` usado no background task
- [ ] Thread principal lê `Console.ReadLine()`
- [ ] Mensagens enviadas via `RequestStream.WriteAsync()`
- [ ] `CompleteAsync()` chamado antes de sair
- [ ] Background task aguardado antes de retornar
- [ ] Switch no `ResponseCase` para processar diferentes respostas
- [ ] Comando "exit" implementado
- [ ] Graceful shutdown garantido

#### Tarefas Técnicas
1. Adicionar método em `ChatClientService.cs`:
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
           using var call = _client.ChatStream(cancellationToken: cancellationToken);

           // BACKGROUND TASK para ler respostas
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
           await call.RequestStream.CompleteAsync();
           await readTask;

           _logger.LogInformation("Exited chat room");
       }
       catch (RpcException ex)
       {
           _logger.LogError(ex, "Error in chat stream: {Status}", ex.Status);
       }
   }
   ```

#### Definição de Pronto (DoD)
- Método compila sem erros
- Background task funciona corretamente
- Mensagens enviadas e recebidas simultaneamente
- Bot responde a "hello"
- exit funciona graciosamente
- CompleteAsync chamado corretamente
- readTask aguardado
- Testável com servidor rodando

#### Estimativa
**Pontos**: 8
**Tempo**: 3 horas

---

### US-014
**Título**: Implementar menu interativo do cliente

**Como** usuário
**Quero** ter um menu interativo no console
**Para que** possa testar todas as funcionalidades facilmente

#### Critérios de Aceitação
- [ ] Classe `ConsoleMenu` criada
- [ ] Menu com 5 opções exibido
- [ ] Opção 1: Registrar usuário
- [ ] Opção 2: Buscar status de usuário
- [ ] Opção 3: Receber mensagens (server streaming)
- [ ] Opção 4: Chat em tempo real (bidirectional)
- [ ] Opção 5: Sair
- [ ] `Console.CancelKeyPress` configurado para Ctrl+C
- [ ] `CancellationTokenSource` criado e propagado
- [ ] Input do usuário validado

#### Tarefas Técnicas
1. Criar pasta `Menu/` em GrpcChat.Client
2. Criar `ConsoleMenu.cs`:
   ```csharp
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
               Console.WriteLine("\nChoose an option:");
               Console.WriteLine("1. Register User (Unary RPC)");
               Console.WriteLine("2. Get User Status (Unary RPC)");
               Console.WriteLine("3. Receive Messages (Server Streaming)");
               Console.WriteLine("4. Chat Stream (Bidirectional Streaming)");
               Console.WriteLine("5. Exit");
               Console.Write("\nEnter choice: ");

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
                           Console.WriteLine("Invalid choice");
                           break;
                   }
               }
               catch (Exception ex)
               {
                   _logger.LogError(ex, "Error executing menu option");
               }
           }
       }

       private async Task RegisterUserFlow()
       {
           Console.Write("Enter username: ");
           var username = Console.ReadLine();

           if (string.IsNullOrWhiteSpace(username))
           {
               Console.WriteLine("Invalid username");
               return;
           }

           await _chatService.RegisterUserAsync(username);
       }

       private async Task GetUserStatusFlow()
       {
           Console.Write("Enter user ID: ");
           var userId = Console.ReadLine();

           if (string.IsNullOrWhiteSpace(userId))
           {
               Console.WriteLine("Invalid user ID");
               return;
           }

           await _chatService.GetUserStatusAsync(userId);
       }

       private async Task ReceiveMessagesFlow()
       {
           Console.Write("Enter room ID: ");
           var roomId = Console.ReadLine();

           if (string.IsNullOrWhiteSpace(roomId))
           {
               Console.WriteLine("Invalid room ID");
               return;
           }

           Console.WriteLine("Press Ctrl+C to stop receiving messages...");

           using var cts = new CancellationTokenSource();
           Console.CancelKeyPress += (s, e) =>
           {
               e.Cancel = true;
               cts.Cancel();
           };

           await _chatService.ReceiveMessagesAsync(roomId, cts.Token);
       }

       private async Task ChatStreamFlow()
       {
           Console.Write("Enter room ID: ");
           var roomId = Console.ReadLine();

           if (string.IsNullOrWhiteSpace(roomId))
           {
               Console.WriteLine("Invalid room ID");
               return;
           }

           using var cts = new CancellationTokenSource();
           Console.CancelKeyPress += (s, e) =>
           {
               e.Cancel = true;
               cts.Cancel();
           };

           await _chatService.ChatStreamAsync(roomId, cts.Token);
       }
   }
   ```
3. Configurar `Program.cs`:
   ```csharp
   var builder = Host.CreateApplicationBuilder(args);

   builder.Logging.ClearProviders();
   builder.Logging.AddConsole();
   builder.Logging.SetMinimumLevel(LogLevel.Information);

   builder.Services.AddGrpcChatClient("https://localhost:5001");
   builder.Services.AddSingleton<ConsoleMenu>();

   var host = builder.Build();

   var menu = host.Services.GetRequiredService<ConsoleMenu>();
   await menu.RunAsync();
   ```

#### Definição de Pronto (DoD)
- Menu exibido corretamente
- Todas as 5 opções funcionam
- Input validado
- Ctrl+C funciona em streaming
- Fluxo de saída limpo
- Testável end-to-end

#### Estimativa
**Pontos**: 3
**Tempo**: 1.5 horas

---

## Fase 5: Testes e Qualidade

### US-015
**Título**: Configurar ambiente de testes com Redis

**Como** desenvolvedor
**Quero** ter um ambiente de testes com Redis containerizado
**Para que** possa validar persistência e funcionalidades

#### Critérios de Aceitação
- [ ] Docker Compose configurado ou instruções Docker fornecidas
- [ ] Redis rodando em localhost:6379
- [ ] Comando de verificação (`redis-cli ping`) documentado
- [ ] Script de limpeza de dados (`FLUSHDB`) criado
- [ ] Comandos úteis documentados (KEYS, GET, LRANGE, MONITOR)
- [ ] README com instruções de setup

#### Tarefas Técnicas
1. Criar `docker-compose.yml` (opcional):
   ```yaml
   version: '3.8'
   services:
     redis:
       image: redis:alpine
       container_name: redis-chat
       ports:
         - "6379:6379"
       volumes:
         - redis-data:/data
   volumes:
     redis-data:
   ```
2. Criar script `start-redis.sh`:
   ```bash
   #!/bin/bash
   docker run -d --name redis-chat -p 6379:6379 redis:alpine
   echo "Redis started on port 6379"
   echo "Test with: redis-cli ping"
   ```
3. Criar `TESTING.md` com comandos úteis:
   ```markdown
   # Comandos úteis Redis

   ## Verificar conexão
   redis-cli ping

   ## Listar usuários
   redis-cli KEYS "user:*"

   ## Ver dados de usuário
   redis-cli GET user:{userId}

   ## Ver mensagens de sala
   redis-cli LRANGE room:general:messages 0 -1

   ## Limpar todos os dados
   redis-cli FLUSHDB

   ## Monitorar comandos em tempo real
   redis-cli MONITOR
   ```

#### Definição de Pronto (DoD)
- Redis rodando via Docker
- Ping retorna PONG
- Dados persistem entre reinicializações
- Documentação completa
- Scripts testados

#### Estimativa
**Pontos**: 2
**Tempo**: 30 minutos

---

### US-016
**Título**: Implementar testes de persistência

**Como** desenvolvedor
**Quero** validar que dados persistem no Redis
**Para que** garanta que o sistema funciona após reinicializações

#### Critérios de Aceitação
- [ ] Teste de persistência de usuário implementado
- [ ] Teste de persistência de mensagens implementado
- [ ] Teste de histórico de mensagens implementado
- [ ] Teste de limitação de 100 mensagens implementado
- [ ] Verificação manual com redis-cli documentada
- [ ] Cenários de teste documentados

#### Tarefas Técnicas
1. Criar documento `test-scenarios.md`:
   ```markdown
   # Cenários de Teste - Persistência

   ## Teste 1: Persistência de Usuário
   **Objetivo**: Validar que usuários persistem após reiniciar servidor

   **Passos**:
   1. Iniciar Redis e Servidor
   2. Registrar usuário "Alice"
   3. Anotar userId retornado
   4. Parar servidor (Ctrl+C)
   5. Reiniciar servidor
   6. Buscar status de "Alice" usando userId

   **Resultado Esperado**: Status retornado com sucesso

   **Verificação Redis**:
   ```bash
   redis-cli GET user:{userId}
   ```

   ---

   ## Teste 2: Histórico de Mensagens
   **Objetivo**: Validar que mensagens são salvas e recuperadas

   **Passos**:
   1. Entrar na sala "general"
   2. Aguardar 5 mensagens
   3. Sair da sala (Ctrl+C)
   4. Entrar novamente na sala "general"

   **Resultado Esperado**: Histórico das 5 mensagens exibido primeiro

   **Verificação Redis**:
   ```bash
   redis-cli LRANGE room:general:messages 0 -1
   redis-cli LLEN room:general:messages
   ```

   ---

   ## Teste 3: Limitação de 100 Mensagens
   **Objetivo**: Validar que LTRIM mantém apenas 100 mensagens

   **Passos**:
   1. Gerar 150 mensagens em sala "test"
   2. Verificar quantidade no Redis

   **Resultado Esperado**: Apenas 100 mensagens mantidas

   **Verificação Redis**:
   ```bash
   redis-cli LLEN room:test:messages
   # Deve retornar: 100
   ```
   ```

#### Definição de Pronto (DoD)
- Todos os cenários testados manualmente
- Resultados documentados
- Redis verificado com comandos
- Screenshots ou logs capturados
- Aprovação do PO

#### Estimativa
**Pontos**: 3
**Tempo**: 1.5 horas

---

### US-017
**Título**: Implementar testes de cenários de erro

**Como** desenvolvedor
**Quero** validar tratamento de erros e edge cases
**Para que** garanta robustez do sistema

#### Critérios de Aceitação
- [ ] Teste de deadline exceeded implementado
- [ ] Teste de servidor offline implementado
- [ ] Teste de Redis offline implementado
- [ ] Teste de graceful shutdown implementado
- [ ] Logs de erro verificados
- [ ] Exceções tratadas apropriadamente

#### Tarefas Técnicas
1. Criar documento `error-scenarios.md`:
   ```markdown
   # Cenários de Teste - Tratamento de Erros

   ## Teste 1: Deadline Exceeded
   **Objetivo**: Validar timeout em operações longas

   **Passos**:
   1. Modificar código temporariamente:
      ```csharp
      var deadline = DateTime.UtcNow.AddMilliseconds(1); // Forçar timeout
      ```
   2. Tentar registrar usuário

   **Resultado Esperado**:
   - RpcException com StatusCode.DeadlineExceeded
   - Log: "Registration timed out"

   **Reverter código após teste**

   ---

   ## Teste 2: Servidor Offline
   **Objetivo**: Validar comportamento quando servidor não disponível

   **Passos**:
   1. Iniciar cliente
   2. Parar servidor
   3. Tentar registrar usuário

   **Resultado Esperado**:
   - RpcException com StatusCode.Unavailable
   - Log: "Failed to connect to server"

   ---

   ## Teste 3: Redis Offline
   **Objetivo**: Validar tratamento de falha de persistência

   **Passos**:
   1. Parar Redis: `docker stop redis-chat`
   2. Tentar registrar usuário

   **Resultado Esperado**:
   - Exception logada no servidor
   - Cliente recebe erro apropriado

   **Cleanup**: `docker start redis-chat`

   ---

   ## Teste 4: Graceful Shutdown
   **Objetivo**: Validar encerramento limpo de streams

   **Passos**:
   1. Iniciar streaming (opção 3 ou 4)
   2. Pressionar Ctrl+C

   **Resultado Esperado**:
   - Log: "stream cancelled"
   - Nenhuma exception não tratada
   - Saída limpa do programa

   **Verificar Logs**:
   - Servidor: "Completed ... call. Duration: Xms"
   - Cliente: "Message stream cancelled by user"
   ```

#### Definição de Pronto (DoD)
- Todos os 4 cenários testados
- Logs capturados e validados
- Exceções tratadas corretamente
- Documentação completa
- Sistema se recupera graciosamente

#### Estimativa
**Pontos**: 5
**Tempo**: 2 horas

---

## 📊 Resumo de Estimativas

### Por Fase

| Fase | User Stories | Pontos | Tempo Estimado |
|------|-------------|--------|----------------|
| Fase 1: Fundação | 2 | 5 | 1.5h |
| Fase 2: Servidor - Persistência | 3 | 8 | 3.75h |
| Fase 3: Servidor - gRPC | 4 | 21 | 8h |
| Fase 4: Cliente | 5 | 21 | 7.5h |
| Fase 5: Testes | 3 | 10 | 4h |
| **TOTAL** | **17** | **65** | **24.75h** |

### Por Complexidade

| Complexidade | Quantidade | Pontos |
|--------------|-----------|--------|
| Pequena (2-3 pts) | 7 | 17 |
| Média (5 pts) | 4 | 20 |
| Grande (8 pts) | 3 | 24 |

---

## 📝 Convenções e Padrões

### Commits
- Formato: `[US-XXX] Descrição curta`
- Exemplo: `[US-007] Implement RegisterUser and GetUserStatus methods`

### Branches
- Formato: `feature/us-XXX-descricao-curta`
- Exemplo: `feature/us-007-unary-methods`

### Pull Requests
- Título: Mesmo da US
- Descrição: Link para US + checklist de DoD

### Code Review
- Verificar todos os critérios de aceitação
- Validar DoD antes de aprovar
- Testar manualmente quando aplicável

---

**Versão**: 1.0
**Última atualização**: 2026-01-21
**Metodologia**: Scrum/Kanban híbrido
**Sprint sugerida**: 2 semanas (32.5 pontos por sprint)
