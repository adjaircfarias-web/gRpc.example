# Script para iniciar Redis com Docker Compose (Windows PowerShell)
# Uso: .\scripts\start-redis.ps1 [-WithUI]

param(
    [switch]$WithUI
)

Write-Host "🚀 Starting gRPC Chat Redis..." -ForegroundColor Cyan

if ($WithUI) {
    Write-Host "📊 Starting Redis + Redis Commander (UI)..." -ForegroundColor Yellow
    docker compose --profile dev up -d
    Write-Host ""
    Write-Host "✅ Redis started on port 6379" -ForegroundColor Green
    Write-Host "✅ Redis Commander UI: http://localhost:8081" -ForegroundColor Green
} else {
    Write-Host "📦 Starting Redis only..." -ForegroundColor Yellow
    docker compose up -d redis
    Write-Host ""
    Write-Host "✅ Redis started on port 6379" -ForegroundColor Green
    Write-Host "💡 Tip: Use '.\scripts\start-redis.ps1 -WithUI' to start with Redis Commander UI" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🔍 Checking Redis health..." -ForegroundColor Cyan
Start-Sleep -Seconds 2

$pingResult = docker exec grpc-chat-redis redis-cli ping 2>$null

if ($pingResult -eq "PONG") {
    Write-Host "✅ Redis is healthy and responding to PING" -ForegroundColor Green
} else {
    Write-Host "❌ Redis is not responding. Check logs with: docker compose logs redis" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "📋 Useful commands:" -ForegroundColor Cyan
Write-Host "  docker compose ps                           # View status" -ForegroundColor Gray
Write-Host "  docker compose logs -f redis                # View logs" -ForegroundColor Gray
Write-Host "  docker exec grpc-chat-redis redis-cli ping  # Test connection" -ForegroundColor Gray
Write-Host "  docker compose down                         # Stop services" -ForegroundColor Gray
Write-Host "  docker compose down -v                      # Stop and remove data" -ForegroundColor Gray
