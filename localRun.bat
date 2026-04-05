@echo off
pushd "%~dp0"
echo [MoolDangBot] Synchronizing Local Test Environment...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\localRun.ps1" %*
if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Script execution failed.
    pause
)
popd
