#!/bin/bash

# Script para iniciar Redis com Docker Compose
# Uso: ./scripts/start-redis.sh [--with-ui]

set -e

echo "🚀 Starting gRPC Chat Redis..."

if [ "$1" == "--with-ui" ]; then
    echo "📊 Starting Redis + Redis Commander (UI)..."
    docker compose --profile dev up -d
    echo ""
    echo "✅ Redis started on port 6379"
    echo "✅ Redis Commander UI: http://localhost:8081"
else
    echo "📦 Starting Redis only..."
    docker compose up -d redis
    echo ""
    echo "✅ Redis started on port 6379"
    echo "💡 Tip: Use './scripts/start-redis.sh --with-ui' to start with Redis Commander UI"
fi

echo ""
echo "🔍 Checking Redis health..."
sleep 2

if docker exec grpc-chat-redis redis-cli ping > /dev/null 2>&1; then
    echo "✅ Redis is healthy and responding to PING"
else
    echo "❌ Redis is not responding. Check logs with: docker compose logs redis"
    exit 1
fi

echo ""
echo "📋 Useful commands:"
echo "  docker compose ps                           # View status"
echo "  docker compose logs -f redis                # View logs"
echo "  docker exec grpc-chat-redis redis-cli ping  # Test connection"
echo "  docker compose down                         # Stop services"
echo "  docker compose down -v                      # Stop and remove data"
