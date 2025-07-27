@echo off
echo Publishing TradeDataEXP for Windows...

set OUTPUT_DIR=publish\win-x64

echo Cleaning previous build...
if exist %OUTPUT_DIR% rmdir /s /q %OUTPUT_DIR%

echo Restoring packages...
dotnet restore

echo Publishing self-contained application...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o %OUTPUT_DIR%

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo   BUILD SUCCESSFUL!
    echo ========================================
    echo.
    echo Published to: %OUTPUT_DIR%
    echo Executable: %OUTPUT_DIR%\TradeDataEXP.exe
    echo.
    echo You can now distribute the contents of the '%OUTPUT_DIR%' folder.
    echo.
    pause
) else (
    echo.
    echo ========================================
    echo   BUILD FAILED!
    echo ========================================
    echo.
    pause
)
