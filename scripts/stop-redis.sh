#!/bin/bash

# Script para parar Redis
# Uso: ./scripts/stop-redis.sh [--remove-data]

set -e

echo "🛑 Stopping gRPC Chat Redis..."

if [ "$1" == "--remove-data" ]; then
    echo "⚠️  WARNING: This will remove all Redis data!"
    read -p "Are you sure? (yes/no): " confirm

    if [ "$confirm" == "yes" ]; then
        docker compose down -v
        echo "✅ Redis stopped and data removed"
    else
        echo "❌ Cancelled"
        exit 0
    fi
else
    docker compose down
    echo "✅ Redis stopped (data preserved)"
    echo "💡 Tip: Use './scripts/stop-redis.sh --remove-data' to remove data"
fi

echo ""
echo "📋 To start Redis again:"
echo "  ./scripts/start-redis.sh            # Start Redis only"
echo "  ./scripts/start-redis.sh --with-ui  # Start Redis + UI"
