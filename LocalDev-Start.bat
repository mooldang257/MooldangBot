@echo off
setlocal
title MooldangBot Local Dev Starting...

echo [MooldangBot] Starting local infrastructure...
docker-compose up -d db redis rabbitmq
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to start infrastructure containers.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [MooldangBot] Starting local UI (Studio)...
echo (Keep this window open to maintain the UI session. Ctrl+C to stop.)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up studio

if %ERRORLEVEL% neq 0 (
    echo.
    echo [INFO] Studio service has stopped or failed.
)

pause@

