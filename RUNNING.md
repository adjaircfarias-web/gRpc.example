# Guia de Execução - gRPC Chat System

Este guia apresenta o passo a passo para executar o projeto de forma containerizada.

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado e rodando
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (apenas para o cliente)
- Git (para clonar o repositório)

## Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                        Docker Network                            │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │     Redis       │◄───│   gRPC Server   │◄──── Client (local) │
│  │   :6379         │    │     :5001       │                     │
│  └─────────────────┘    └─────────────────┘                     │
│           │                                                      │
│  ┌─────────────────┐                                            │
│  │ Redis Commander │  (opcional - profile: dev)                 │
│  │     :8081       │                                            │
│  └─────────────────┘                                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## Opção 1: Execução Rápida (Recomendada)

### Passo 1: Subir os containers (Redis + Server)

```bash
# Na raiz do projeto (onde está o docker-compose.yml)
docker-compose up -d
```

### Passo 2: Verificar se os containers estão rodando

```bash
docker-compose ps
```

Saída esperada:
```
NAME                 STATUS                   PORTS
grpc-chat-redis      running (healthy)        0.0.0.0:6379->6379/tcp
grpc-chat-server     running                  0.0.0.0:5001->5001/tcp
```

### Passo 3: Ver logs do servidor

```bash
docker-compose logs -f server
```

### Passo 4: Executar o cliente (em outro terminal)

```bash
cd src/GrpcChat.Client
dotnet run
```

### Passo 5: Usar o menu interativo

```
=== gRPC Chat Client ===

Choose an option:
1. Register User (Unary RPC)
2. Get User Status (Unary RPC)
3. Receive Messages (Server Streaming)
4. Chat Stream (Bidirectional Streaming)
5. Exit

Enter choice: 1
Enter username: Alice
```

### Passo 6: Parar os containers

```bash
docker-compose down
```

---

## Opção 2: Execução com Redis Commander (Dev)

Para visualizar os dados no Redis via interface web:

```bash
# Subir com profile dev (inclui Redis Commander)
docker-compose --profile dev up -d
```

Acesse: http://localhost:8081

---

## Opção 3: Apenas Redis (Desenvolvimento Local)

Se quiser rodar apenas o Redis e executar o servidor localmente:

### Passo 1: Subir apenas o Redis

```bash
docker-compose up -d redis
```

### Passo 2: Executar o servidor localmente

```bash
cd src/GrpcChat.Server
dotnet run
```

### Passo 3: Executar o cliente (outro terminal)

```bash
cd src/GrpcChat.Client
dotnet run
```

---

## Comandos Úteis

### Docker

```bash
# Ver status dos containers
docker-compose ps

# Ver logs em tempo real
docker-compose logs -f

# Ver logs de um serviço específico
docker-compose logs -f server
docker-compose logs -f redis

# Reiniciar um serviço
docker-compose restart server

# Parar tudo
docker-compose down

# Parar e remover volumes (limpa dados do Redis)
docker-compose down -v

# Rebuild do servidor (após mudanças no código)
docker-compose build server
docker-compose up -d server
```

### Redis CLI

```bash
# Conectar ao Redis via Docker
docker exec -it grpc-chat-redis redis-cli

# Comandos Redis úteis
PING                              # Testar conexão
KEYS *                            # Listar todas as chaves
KEYS user:*                       # Listar usuários
GET user:{userId}                 # Ver dados de um usuário
LRANGE room:general:messages 0 -1 # Ver mensagens de uma sala
LLEN room:general:messages        # Contar mensagens
FLUSHDB                           # Limpar todos os dados
MONITOR                           # Ver comandos em tempo real
```

### .NET

```bash
# Build de toda a solução
dotnet build

# Executar servidor
cd src/GrpcChat.Server && dotnet run

# Executar cliente
cd src/GrpcChat.Client && dotnet run

# Executar com watch (hot reload)
cd src/GrpcChat.Server && dotnet watch run
```

---

## Testando as Funcionalidades

### 1. Registrar Usuário (Unary RPC)

```
Enter choice: 1
Enter username: Alice
Registration successful! Your user ID is: abc-123-def
```

### 2. Buscar Status (Unary RPC)

```
Enter choice: 2
Enter user ID (or press Enter to use your ID):
User Alice (abc-123-def) is online. Last seen: 2026-01-22 10:30:15
```

### 3. Receber Mensagens (Server Streaming)

```
Enter choice: 3
Enter room ID: general
Press Ctrl+C to stop receiving messages...

[10:30:20] ChatBot: Simulated message #1 in room general
[10:30:23] ChatBot: Simulated message #2 in room general
^C
Stopping message stream...
```

### 4. Chat Bidirecional (Bidirectional Streaming)

```
Enter choice: 4
Enter room ID: general

[SYSTEM] Welcome to room general!
hello
[10:31:05] Alice: hello
[10:31:05] ChatBot: Hello Alice! How can I help you?
exit
[SYSTEM] You left room general
```

---

## Verificando Persistência no Redis

Após criar usuários e enviar mensagens, verifique no Redis:

```bash
# Conectar ao Redis
docker exec -it grpc-chat-redis redis-cli

# Listar usuários
KEYS user:*

# Ver dados de um usuário
GET user:abc-123-def

# Ver mensagens da sala "general"
LRANGE room:general:messages 0 -1

# Contar mensagens
LLEN room:general:messages
```

---

## Troubleshooting

### Erro: "Server unavailable"

1. Verifique se os containers estão rodando:
   ```bash
   docker-compose ps
   ```

2. Verifique os logs do servidor:
   ```bash
   docker-compose logs server
   ```

3. Verifique se a porta 5001 está livre:
   ```bash
   netstat -an | grep 5001
   ```

### Erro: "Failed to connect to Redis"

1. Verifique se o Redis está saudável:
   ```bash
   docker exec -it grpc-chat-redis redis-cli ping
   ```

2. Verifique a connection string no docker-compose.yml

### Cliente não conecta ao servidor containerizado

O cliente usa `http://localhost:5001` por padrão. Se estiver usando HTTPS:
- Altere para `http://localhost:5001` no arquivo `Program.cs` do cliente
- Ou configure certificados TLS no servidor

### Rebuild após mudanças

```bash
docker-compose build --no-cache server
docker-compose up -d server
```

---

## Portas Utilizadas

| Serviço         | Porta | Descrição                    |
|-----------------|-------|------------------------------|
| Redis           | 6379  | Banco de dados               |
| gRPC Server     | 5001  | API gRPC                     |
| Redis Commander | 8081  | UI Redis (profile: dev)      |

---

## Próximos Passos

1. Implementar testes automatizados (US-015, US-016, US-017)
2. Configurar CI/CD com GitHub Actions
3. Adicionar Kubernetes manifests para produção
4. Configurar TLS/mTLS para comunicação segura
