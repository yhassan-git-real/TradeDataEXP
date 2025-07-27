@echo off
echo Building TradeDataEXP...
dotnet restore
dotnet build --configuration Release

if %ERRORLEVEL% EQU 0 (
    echo Build successful!
    echo.
    echo Starting TradeDataEXP...
    dotnet run --configuration Release
) else (
    echo Build failed!
    pause
)
