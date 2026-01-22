#!/bin/bash

# Script para acessar Redis CLI
# Uso: ./scripts/redis-cli.sh [comando]

if [ -z "$1" ]; then
    # Modo interativo
    echo "🔌 Connecting to Redis CLI (interactive mode)..."
    echo "💡 Type 'exit' or press Ctrl+C to quit"
    echo ""
    docker exec -it grpc-chat-redis redis-cli
else
    # Executar comando específico
    docker exec grpc-chat-redis redis-cli "$@"
fi
