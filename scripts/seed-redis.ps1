# ==============================================================================
# RedisGUI Demo Data Seeder
# Populates local/docker Redis with sample keys across DB 0, DB 1, DB 2
# ==============================================================================

param(
    [string]$HostName = "127.0.0.1",
    [int]$Port = 6379,
    [string]$Password = ""
)

Write-Host "Connecting to Redis at $HostName:$Port..." -ForegroundColor Cyan

function Run-RedisCmd {
    param([int]$Db, [string]$Command)
    $authArg = if ($Password) { "-a $Password" } else { "" }
    docker exec -i redisgui-redis redis-cli -n $Db $authArg $Command 2>$null
}

# DB 0: Primary rich dataset
Write-Host "Seeding DB 0 (Strings, Hashes, Lists, Sets, ZSets, Streams)..." -ForegroundColor Yellow
Run-RedisCmd 0 'SET user:1001:profile "{\"id\":1001,\"name\":\"Alex Smith\",\"email\":\"alex.smith@example.com\",\"role\":\"Senior Engineer\",\"status\":\"active\"}"'
Run-RedisCmd 0 'SET banner:maintenance "Scheduled maintenance on Sunday 02:00 UTC" EX 45'
Run-RedisCmd 0 'SET app:settings:theme "dark"'
Run-RedisCmd 0 'SET counter:pageviews:homepage "14829"'

Run-RedisCmd 0 'HSET company:info name "TechCorp Global" founded "2018" headquarters "Kyiv, Ukraine" employees "250"'
Run-RedisCmd 0 'HSET server:stats:node-1 cpu "28.4%" memory_free_mb "4096" uptime_days "42" region "eu-central-1"'

Run-RedisCmd 0 'DEL queue:jobs:high_priority'
Run-RedisCmd 0 'RPUSH queue:jobs:high_priority "job_export_report_#9124" "job_send_welcome_email_#9125" "job_process_payout_#9126"'

Run-RedisCmd 0 'DEL users:online'
Run-RedisCmd 0 'SADD users:online "user:1001" "user:1042" "user:2088" "user:3104" "user:4096"'

Run-RedisCmd 0 'DEL leaderboard:gamers'
Run-RedisCmd 0 'ZADD leaderboard:gamers 9420 "shadow_ninja" 8750 "cyber_samurai" 8120 "pixel_queen"'

Run-RedisCmd 0 'DEL ratelimit:ip:192.168.1.1'
Run-RedisCmd 0 'ZADD ratelimit:ip:192.168.1.1 1700000001 "req_1" 1700000002 "req_2"'
Run-RedisCmd 0 'EXPIRE ratelimit:ip:192.168.1.1 60'

Run-RedisCmd 0 'XADD stream:audit:logs * event login user admin ip 127.0.0.1 status success'
Run-RedisCmd 0 'XADD stream:audit:logs * event key_create user alex key banner:maintenance'

# DB 1: Cache & Sessions
Write-Host "Seeding DB 1 (Sessions & Carts)..." -ForegroundColor Yellow
Run-RedisCmd 1 'SET session:token:f8a7c2e1 "{\"userId\":1001,\"ip\":\"192.168.1.50\"}" EX 300'
Run-RedisCmd 1 'HSET cart:user:1001 item:prod_881 2 item:prod_332 1 coupon SUMMER2026'
Run-RedisCmd 1 'SADD tags:trending "angular22" "dotnet10" "redis7" "signals"'

# DB 2: Sensors & Exchange Rates
Write-Host "Seeding DB 2 (IoT Sensors & Currency Rates)..." -ForegroundColor Yellow
Run-RedisCmd 2 'XADD sensors:iot:temperature * sensor_id temp_sensor_kitchen value_c 22.4 battery 98%'
Run-RedisCmd 2 'HSET cache:exchange_rates USD 41.50 EUR 44.80 GBP 53.20 PLN 10.45'

Write-Host "Seeding complete! 15+ demo keys created across DB 0, DB 1, DB 2." -ForegroundColor Green
