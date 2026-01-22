# Script para parar Redis (Windows PowerShell)
# Uso: .\scripts\stop-redis.ps1 [-RemoveData]

param(
    [switch]$RemoveData
)

Write-Host "🛑 Stopping gRPC Chat Redis..." -ForegroundColor Cyan

if ($RemoveData) {
    Write-Host "⚠️  WARNING: This will remove all Redis data!" -ForegroundColor Yellow
    $confirm = Read-Host "Are you sure? (yes/no)"

    if ($confirm -eq "yes") {
        docker compose down -v
        Write-Host "✅ Redis stopped and data removed" -ForegroundColor Green
    } else {
        Write-Host "❌ Cancelled" -ForegroundColor Red
        exit 0
    }
} else {
    docker compose down
    Write-Host "✅ Redis stopped (data preserved)" -ForegroundColor Green
    Write-Host "💡 Tip: Use '.\scripts\stop-redis.ps1 -RemoveData' to remove data" -ForegroundColor Gray
}

Write-Host ""
Write-Host "📋 To start Redis again:" -ForegroundColor Cyan
Write-Host "  .\scripts\start-redis.ps1            # Start Redis only" -ForegroundColor Gray
Write-Host "  .\scripts\start-redis.ps1 -WithUI    # Start Redis + UI" -ForegroundColor Gray
