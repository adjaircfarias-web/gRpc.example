# Script para acessar Redis CLI (Windows PowerShell)
# Uso: .\scripts\redis-cli.ps1 [comando]

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Command
)

if ($Command.Count -eq 0) {
    # Modo interativo
    Write-Host "🔌 Connecting to Redis CLI (interactive mode)..." -ForegroundColor Cyan
    Write-Host "💡 Type 'exit' or press Ctrl+C to quit" -ForegroundColor Gray
    Write-Host ""
    docker exec -it grpc-chat-redis redis-cli
} else {
    # Executar comando específico
    docker exec grpc-chat-redis redis-cli $Command
}
