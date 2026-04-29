# setup-secrets.ps1
# Quick setup script for User Secrets configuration

Write-Host "Setting up User Secrets for RedisGUI..." -ForegroundColor Cyan

$serverPath = "src/RedisGUI.Server"

if (-not (Test-Path $serverPath)) {
    Write-Host "Error: Could not find $serverPath" -ForegroundColor Red
    Write-Host "Please run this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

Push-Location $serverPath

try {
    # Initialize user secrets
    Write-Host "`nInitializing User Secrets..." -ForegroundColor Yellow
    dotnet user-secrets init

    # Generate and set Cryptography Key
    Write-Host "Generating Cryptography Key..." -ForegroundColor Yellow
    $cryptoBytes = New-Object Byte[] 32
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($cryptoBytes)
    $cryptoKey = [Convert]::ToBase64String($cryptoBytes)
    dotnet user-secrets set "Cryptography:SecurityKey" $cryptoKey

    # Generate and set JWT Secret
    Write-Host "Generating JWT Secret..." -ForegroundColor Yellow
    $jwtBytes = New-Object Byte[] 64
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($jwtBytes)
    $jwtKey = [Convert]::ToBase64String($jwtBytes)
    dotnet user-secrets set "JwtSettings:SecretKey" $jwtKey

    # Prompt for database connection strings
    Write-Host "`n=== Database Configuration ===" -ForegroundColor Cyan
    Write-Host "Enter your MySQL/MariaDB connection string:" -ForegroundColor Yellow
    Write-Host "(Example: server=localhost;database=redis-gui;user=admin;password=admin;)" -ForegroundColor Gray
    $mysqlConn = Read-Host "MySQL Connection"
    
    if ([string]::IsNullOrWhiteSpace($mysqlConn)) {
        $mysqlConn = "server=localhost;database=redis-gui;user=admin;password=admin;"
        Write-Host "Using default MySQL connection string" -ForegroundColor Gray
    }
    dotnet user-secrets set "Database:ConnectionString" $mysqlConn

    Write-Host "`nEnter your MS SQL connection string (or press Enter to skip):" -ForegroundColor Yellow
    Write-Host "(Example: Data Source=127.0.0.1; Initial Catalog=redis-gui; User Id=sa; Password=Password123; TrustServerCertificate=True;)" -ForegroundColor Gray
    $mssqlConn = Read-Host "MS SQL Connection"
    
    if (-not [string]::IsNullOrWhiteSpace($mssqlConn)) {
        dotnet user-secrets set "Database:ConnectionStringMSQL" $mssqlConn
    }

    Write-Host "`n=== Configuration Complete ===" -ForegroundColor Green
    Write-Host "✓ User secrets configured successfully!" -ForegroundColor Green
    Write-Host "`nConfigured secrets:" -ForegroundColor Cyan
    dotnet user-secrets list

    Write-Host "`nYou can now run the application with:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project src/RedisGUI.Server" -ForegroundColor White

} catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}
