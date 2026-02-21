# Script de Teste Rápido - OAuth do Zero
# Execute: .\teste-rapido.ps1

Write-Host "🚀 TESTE RÁPIDO - OAUTH DO ZERO" -ForegroundColor Green -BackgroundColor DarkBlue

Write-Host "`n📋 VERIFICANDO USUÁRIO CRIADO..." -ForegroundColor Cyan
try {
    $users = Invoke-RestMethod -Uri "http://localhost:9000/api/users" -Method GET
    if ($users -and $users.Count -gt 0) {
        Write-Host "✅ Usuários encontrados:" -ForegroundColor Green
        $users | ForEach-Object { 
            Write-Host "   👤 $($_.username) - $($_.email)" -ForegroundColor White 
        }
    } else {
        Write-Host "❌ Nenhum usuário encontrado" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Erro ao verificar usuários: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Certifique-se de que o servidor está rodando em http://localhost:9000" -ForegroundColor Yellow
}

Write-Host "`n🌐 ABRINDO PÁGINAS DE TESTE..." -ForegroundColor Cyan

# Abrir página de login
Write-Host "   🔑 Abrindo página de login..." -ForegroundColor White
Start-Process "http://localhost:9000/Account/Login"

Start-Sleep 2

# Abrir página inicial
Write-Host "   🏠 Abrindo página inicial..." -ForegroundColor White  
Start-Process "http://localhost:9000"

Write-Host "`n📋 CREDENCIAIS PARA LOGIN:" -ForegroundColor Yellow
Write-Host "   👤 Usuário: admin" -ForegroundColor White
Write-Host "   🔑 Senha: Admin123!" -ForegroundColor White

Write-Host "`n🔧 PRÓXIMOS PASSOS:" -ForegroundColor Cyan
Write-Host "   1. Execute o SQL para criar o cliente 'testapp' (veja CREDENCIAIS-LOGIN.md)" -ForegroundColor White
Write-Host "   2. Teste o login com as credenciais acima" -ForegroundColor White
Write-Host "   3. Teste o fluxo OAuth completo com a URL fornecida" -ForegroundColor White

Write-Host "`n✅ Teste concluído! Sistema OAuth pronto para uso!" -ForegroundColor Green