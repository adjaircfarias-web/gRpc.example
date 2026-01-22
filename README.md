# gRPC Chat System with Streaming + Redis

Sistema de chat gRPC completo demonstrando todos os padrões de comunicação (unário, server streaming e bidirectional streaming) com persistência Redis.

## 🚀 Quick Start

### Pré-requisitos

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (para Redis)
- [Git](https://git-scm.com/)

### 1. Iniciar Redis

```bash
# Iniciar apenas Redis
docker compose up -d redis

# Ou iniciar Redis + Redis Commander (UI web)
docker compose --profile dev up -d
```

**Verificar**:
```bash
# Testar conexão Redis
docker exec grpc-chat-redis redis-cli ping
# Deve retornar: PONG

# Acessar Redis Commander (se iniciado com --profile dev)
# http://localhost:8081
```

### 2. Compilar projetos

```bash
# Na raiz do repositório
dotnet restore
dotnet build
```

### 3. Executar servidor

```bash
cd src/GrpcChat.Server
dotnet run
```

Aguarde o log: `info: Connected to Redis at localhost:6379`

### 4. Executar cliente (em outro terminal)

```bash
cd src/GrpcChat.Client
dotnet run
```

## 📋 Usando o sistema

### Menu do Cliente

```
=== gRPC Chat Client ===

Choose an option:
1. Register User (Unary RPC)
2. Get User Status (Unary RPC)
3. Receive Messages (Server Streaming)
4. Chat Stream (Bidirectional Streaming)
5. Exit
```

### Fluxo de teste recomendado

1. **Registrar usuário** (opção 1)
   - Digite um username (ex: "Alice")
   - Anote o `userId` retornado

2. **Buscar status** (opção 2)
   - Digite o `userId` de Alice
   - Verifique que está "online"

3. **Server Streaming** (opção 3)
   - Digite room ID: "general"
   - Observe mensagens chegando a cada 2-5 segundos
   - Pressione `Ctrl+C` para parar

4. **Bidirectional Streaming** (opção 4)
   - Digite room ID: "test"
   - Digite mensagens e pressione Enter
   - Digite "hello" para receber resposta do bot
   - Digite "exit" para sair

## 🗄️ Comandos úteis Redis

```bash
# Listar todos os usuários
docker exec grpc-chat-redis redis-cli KEYS "user:*"

# Ver dados de um usuário
docker exec grpc-chat-redis redis-cli GET "user:{userId}"

# Ver mensagens de uma sala
docker exec grpc-chat-redis redis-cli LRANGE "room:general:messages" 0 -1

# Contar mensagens em uma sala
docker exec grpc-chat-redis redis-cli LLEN "room:general:messages"

# Monitorar comandos Redis em tempo real
docker exec grpc-chat-redis redis-cli MONITOR

# Limpar todos os dados
docker exec grpc-chat-redis redis-cli FLUSHDB
```

## 🐳 Comandos Docker Compose

### Iniciar serviços

```bash
# Apenas Redis
docker compose up -d redis

# Redis + Redis Commander (UI)
docker compose --profile dev up -d

# Ver logs
docker compose logs -f redis
```

### Parar e limpar

```bash
# Parar serviços
docker compose down

# Parar e remover volumes (ATENÇÃO: apaga dados do Redis)
docker compose down -v

# Reiniciar Redis
docker compose restart redis
```

### Health check

```bash
# Verificar status dos serviços
docker compose ps

# Verificar saúde do Redis
docker compose exec redis redis-cli ping
```

## 📁 Estrutura do Projeto

```
gRpc.example/
├── .docs/
│   ├── plan/
│   │   └── implementation-plan.md      # Plano detalhado de implementação
│   └── US/
│       └── technical-user-stories.md   # User Stories técnicas
├── src/
│   ├── GrpcChat.Contracts/             # Contratos Protocol Buffers
│   │   └── Protos/
│   │       └── chat.proto
│   ├── GrpcChat.Server/                # Servidor gRPC + Redis
│   │   ├── Services/
│   │   │   └── ChatService.cs
│   │   ├── Repositories/
│   │   │   ├── UserRepository.cs
│   │   │   └── ChatRepository.cs
│   │   ├── Interceptors/
│   │   │   └── ServerLoggingInterceptor.cs
│   │   └── Extensions/
│   │       └── RedisServiceExtensions.cs
│   └── GrpcChat.Client/                # Cliente console
│       ├── Services/
│       │   └── ChatClientService.cs
│       ├── Menu/
│       │   └── ConsoleMenu.cs
│       └── Extensions/
│           └── GrpcClientExtensions.cs
├── docker-compose.yml                   # Orquestração Redis
└── README.md                            # Este arquivo
```

## 🎯 Conceitos Demonstrados

### gRPC
- ✅ Unary RPC (request-response)
- ✅ Server Streaming (servidor envia múltiplas respostas)
- ✅ Bidirectional Streaming (ambos enviam/recebem simultaneamente)
- ✅ Interceptors (logging e métricas)
- ✅ Deadlines (timeout management)
- ✅ Graceful shutdown

### .NET
- ✅ Dependency Injection (lifetimes corretos)
- ✅ Extension methods (configuração limpa)
- ✅ ILogger (structured logging)
- ✅ Async/await (non-blocking I/O)
- ✅ Repository Pattern

### Redis
- ✅ StackExchange.Redis (cliente oficial)
- ✅ IConnectionMultiplexer (singleton)
- ✅ Strings (JSON serialization)
- ✅ Lists (message queues)
- ✅ TTL e LTRIM (data management)

### Patterns & Practices
- ✅ Repository Pattern
- ✅ Dependency Injection
- ✅ SOLID Principles
- ✅ Graceful Shutdown
- ✅ Error Handling

## 🧪 Testes

### Teste de Persistência

1. Registrar usuário "Alice"
2. Parar servidor (`Ctrl+C`)
3. Reiniciar servidor
4. Buscar status de "Alice" → Deve funcionar ✅

### Teste de Histórico

1. Entrar na sala "general"
2. Aguardar 5 mensagens
3. Sair (`Ctrl+C`)
4. Entrar novamente → Histórico deve ser exibido primeiro ✅

### Teste de Error Handling

1. Parar Redis: `docker compose stop redis`
2. Tentar registrar usuário → Erro tratado graciosamente ✅
3. Reiniciar Redis: `docker compose start redis`

## 📝 Configuração

### appsettings.json (Server)

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

### Variáveis de Ambiente (Docker)

```bash
# Sobrescrever connection string
export ConnectionStrings__Redis="redis:6379"
```

## 🔧 Troubleshooting

### Redis não conecta

```bash
# Verificar se Redis está rodando
docker compose ps

# Ver logs do Redis
docker compose logs redis

# Testar conexão manual
docker exec grpc-chat-redis redis-cli ping
```

### Servidor não inicia

```bash
# Verificar se porta 5001 está em uso
netstat -an | grep 5001

# Ver logs do servidor
cd src/GrpcChat.Server
dotnet run --verbosity detailed
```

### Cliente não conecta ao servidor

```bash
# Verificar URL em GrpcClientExtensions.cs
# Deve ser: https://localhost:5001

# Verificar certificados SSL
# Para desenvolvimento, certificados self-signed são aceitos
```

## 📚 Documentação

- [Plano de Implementação](.docs/plan/implementation-plan.md) - Arquitetura e padrões detalhados
- [User Stories Técnicas](.docs/US/technical-user-stories.md) - 17 USs com critérios de aceitação

## 🤝 Contribuindo

1. Ler [User Stories](.docs/US/technical-user-stories.md)
2. Criar branch: `feature/us-XXX-descricao`
3. Seguir [Plano de Implementação](.docs/plan/implementation-plan.md)
4. Commit: `[US-XXX] Descrição`
5. Criar Pull Request

## 📄 Licença

MIT License - Projeto educacional para demonstração de gRPC avançado com Redis.

---

**Stack**: .NET 8.0 | gRPC | Redis | Docker
**Padrões**: Repository Pattern | SOLID | Dependency Injection
**Status**: ✅ Production-ready (com exceções documentadas)
