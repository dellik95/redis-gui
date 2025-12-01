# читаємо package.json
$packageJsonPath = Join-Path (Get-Location) "./src/RedisGUI.Client/package.json"

if (-not (Test-Path $packageJsonPath)) {
    Write-Host "❌ Не знайдено package.json у поточній теці!"
    exit 1
}

$package = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
$appName = $package.name

if (-not $appName) {
    Write-Host "❌ Не знайдено поле 'name' у package.json!"
    exit 1
}

Write-Host "📦 Назва застосунку (package.json → name): $appName"

# Формуємо шлях до PFX
$certDir = Join-Path $env:APPDATA "ASP.NET\https"
$pfxPath = Join-Path $certDir "$appName.pfx"

# створити каталог якщо потрібно
if (-not (Test-Path $certDir)) {
    New-Item $certDir -ItemType Directory | Out-Null
}

Write-Host "📄 PFX буде створено тут:"
Write-Host "   $pfxPath"

# генеруємо pfx сертифікат
Write-Host "`n🔧 Створюємо dev-certificate..."
dotnet dev-certs https -ep "$pfxPath" -p "P@ssw0rd!"

# довіряємо сертифікату
Write-Host "🔐 Додаємо у Trusted Root..."
dotnet dev-certs https --trust

Write-Host "`n✅ Готово!"
Write-Host "Файл сертифіката:"
Write-Host "   $pfxPath"
