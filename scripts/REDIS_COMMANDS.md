# Redis - Comandos Úteis para gRPC Chat

Guia rápido de comandos Redis para debugging e monitoramento do sistema de chat.

## 🔌 Acessar Redis CLI

### Linux/Mac
```bash
./scripts/redis-cli.sh
```

### Windows PowerShell
```powershell
.\scripts\redis-cli.ps1
```

### Direto com Docker
```bash
docker exec -it grpc-chat-redis redis-cli
```

---

## 👥 Comandos - Usuários

### Listar todos os usuários
```redis
KEYS user:*
```

### Ver dados de um usuário específico
```redis
GET user:{userId}
```
Exemplo:
```redis
GET user:abc-123-def
```

### Verificar se usuário existe
```redis
EXISTS user:{userId}
```

### Contar total de usuários
```bash
# Linux/Mac
./scripts/redis-cli.sh KEYS "user:*" | wc -l

# Windows PowerShell
(.\scripts\redis-cli.ps1 KEYS "user:*").Count
```

### Deletar um usuário
```redis
DEL user:{userId}
```

---

## 💬 Comandos - Mensagens de Chat

### Listar todas as salas
```redis
KEYS room:*:messages
```

### Ver todas as mensagens de uma sala
```redis
LRANGE room:{roomId}:messages 0 -1
```
Exemplo:
```redis
LRANGE room:general:messages 0 -1
```

### Ver últimas N mensagens de uma sala
```redis
LRANGE room:{roomId}:messages -10 -1
```
Exemplo (últimas 10 mensagens):
```redis
LRANGE room:general:messages -10 -1
```

### Contar mensagens em uma sala
```redis
LLEN room:{roomId}:messages
```

### Ver primeira mensagem de uma sala
```redis
LINDEX room:{roomId}:messages 0
```

### Ver última mensagem de uma sala
```redis
LINDEX room:{roomId}:messages -1
```

### Deletar todas as mensagens de uma sala
```redis
DEL room:{roomId}:messages
```

---

## 🔍 Comandos - Monitoramento

### Monitorar comandos em tempo real
```redis
MONITOR
```
⚠️ Pressione `Ctrl+C` para parar

### Ver informações do servidor Redis
```redis
INFO
```

### Ver apenas estatísticas
```redis
INFO stats
```

### Ver memória usada
```redis
INFO memory
```

### Ver total de chaves
```redis
DBSIZE
```

### Testar conexão
```redis
PING
```
Deve retornar: `PONG`

### Ver configuração Redis
```redis
CONFIG GET *
```

### Ver configuração específica
```redis
CONFIG GET appendonly
CONFIG GET maxmemory-policy
```

---

## 🧹 Comandos - Limpeza

### Limpar TODOS os dados (CUIDADO!)
```redis
FLUSHDB
```

### Limpar dados de teste específicos
```redis
# Deletar todos os usuários
DEL user:*

# Deletar sala específica
DEL room:test:messages

# Deletar múltiplas chaves com padrão (requer loop)
# Exemplo via script bash:
redis-cli KEYS "room:test*" | xargs redis-cli DEL
```

---

## 📊 Comandos - Debug e Análise

### Ver tipo de dado de uma chave
```redis
TYPE user:{userId}
TYPE room:{roomId}:messages
```

### Ver tempo de vida (TTL) de uma chave
```redis
TTL user:{userId}
```
Retornos:
- `-1`: Sem TTL (permanente)
- `-2`: Chave não existe
- `N`: Expira em N segundos

### Definir TTL em uma chave (segundos)
```redis
EXPIRE user:{userId} 3600
```

### Remover TTL de uma chave
```redis
PERSIST user:{userId}
```

### Pesquisar chaves com padrão
```redis
SCAN 0 MATCH user:* COUNT 100
```

### Ver tamanho em bytes de uma chave
```redis
MEMORY USAGE user:{userId}
MEMORY USAGE room:{roomId}:messages
```

---

## 🚀 Comandos Úteis - Scripts

### Bash Script: Listar usuários com detalhes
```bash
#!/bin/bash
echo "=== Usuários Registrados ==="
for key in $(docker exec grpc-chat-redis redis-cli KEYS "user:*"); do
    echo ""
    echo "Key: $key"
    docker exec grpc-chat-redis redis-cli GET "$key" | jq .
done
```

### PowerShell Script: Listar usuários com detalhes
```powershell
Write-Host "=== Usuários Registrados ===" -ForegroundColor Cyan
$keys = .\scripts\redis-cli.ps1 KEYS "user:*"
foreach ($key in $keys) {
    Write-Host ""
    Write-Host "Key: $key" -ForegroundColor Yellow
    $json = .\scripts\redis-cli.ps1 GET $key
    $json | ConvertFrom-Json | ConvertTo-Json
}
```

### Bash Script: Estatísticas de salas
```bash
#!/bin/bash
echo "=== Estatísticas de Salas ==="
for key in $(docker exec grpc-chat-redis redis-cli KEYS "room:*:messages"); do
    count=$(docker exec grpc-chat-redis redis-cli LLEN "$key")
    echo "$key: $count mensagens"
done
```

### PowerShell Script: Estatísticas de salas
```powershell
Write-Host "=== Estatísticas de Salas ===" -ForegroundColor Cyan
$rooms = .\scripts\redis-cli.ps1 KEYS "room:*:messages"
foreach ($room in $rooms) {
    $count = .\scripts\redis-cli.ps1 LLEN $room
    Write-Host "$room: $count mensagens" -ForegroundColor Yellow
}
```

---

## 🎯 Cenários Comuns

### Cenário 1: Verificar se sistema está funcionando
```redis
# 1. Testar conexão
PING

# 2. Ver total de chaves
DBSIZE

# 3. Listar usuários
KEYS user:*

# 4. Listar salas
KEYS room:*
```

### Cenário 2: Debug de usuário específico
```redis
# Substituir {userId} pelo ID real
EXISTS user:{userId}
GET user:{userId}
TTL user:{userId}
```

### Cenário 3: Analisar sala de chat
```redis
# Substituir {roomId} pelo ID real
LLEN room:{roomId}:messages          # Total de mensagens
LRANGE room:{roomId}:messages 0 4    # Primeiras 5 mensagens
LRANGE room:{roomId}:messages -5 -1  # Últimas 5 mensagens
```

### Cenário 4: Limpar dados de teste
```redis
# Limpar sala específica
DEL room:test:messages

# Limpar usuário específico
DEL user:test-user-id

# Limpar TUDO (CUIDADO!)
FLUSHDB
```

---

## 📚 Recursos

### Documentação Redis
- [Redis Commands](https://redis.io/commands)
- [Redis Data Types](https://redis.io/docs/data-types/)
- [Redis Persistence](https://redis.io/docs/management/persistence/)

### Redis Commander (Web UI)
Se iniciado com `--profile dev`:
- URL: http://localhost:8081
- Interface gráfica para visualizar e editar dados
- Não requer comandos CLI

### Ferramentas Recomendadas
- **RedisInsight**: GUI desktop oficial do Redis
- **redis-cli**: Cliente CLI nativo
- **Redis Commander**: Web UI (já incluído no docker-compose)

---

## ⚠️ Avisos Importantes

### Performance
- `KEYS *` é **bloqueante** - use `SCAN` em produção
- `MONITOR` impacta performance - use apenas para debug
- `FLUSHDB` é **irreversível** - cuidado em produção

### Segurança
- Nunca exponha porta Redis (6379) publicamente
- Use senha em produção: `requirepass`
- Configure bind address apropriadamente

### Boas Práticas
- Use TTL para dados temporários
- Monitore uso de memória
- Faça backup antes de `FLUSHDB`
- Use transações (MULTI/EXEC) quando necessário

---

**Última atualização**: 2026-01-21
**Redis Version**: 7.2-alpine
**Projeto**: gRPC Chat System
