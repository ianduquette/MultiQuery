@echo off
setlocal enabledelayedexpansion

echo ========================================
echo    MultiQuery Cross-Platform Publisher
echo ========================================
echo.

REM Check if dotnet CLI is available
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: dotnet CLI not found. Please install .NET 8.0 SDK.
    echo Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK Version: 
dotnet --version
echo.

REM Clean previous builds
echo [1/5] Cleaning previous builds...
if exist "publish" (
    rmdir /s /q "publish" 2>nul
)
mkdir "publish" 2>nul
mkdir "publish\windows-x64" 2>nul
mkdir "publish\linux-x64" 2>nul
echo     ✓ Cleaned publish directories
echo.

REM Build Windows x64
echo [2/5] Building Windows x64 executable...
dotnet publish MultiQuery/MultiQuery.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=true ^
    -p:PublishReadyToRun=true ^
    -o publish/windows-x64 ^
    --verbosity quiet

if errorlevel 1 (
    echo     ✗ Windows build failed
    pause
    exit /b 1
)
echo     ✓ Windows build completed
echo.

REM Build Linux x64 (cross-compiled from Windows)
echo [3/5] Building Linux x64 executable (cross-compiled)...
dotnet publish MultiQuery/MultiQuery.csproj ^
    -c Release ^
    -r linux-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=true ^
    -p:PublishReadyToRun=true ^
    -o publish/linux-x64 ^
    --verbosity quiet

if errorlevel 1 (
    echo     ✗ Linux build failed
    pause
    exit /b 1
)
echo     ✓ Linux build completed
echo.

REM Verify builds and get file sizes
echo [4/5] Verifying builds...

set "win_exe=publish\windows-x64\multiquery.exe"
set "linux_exe=publish\linux-x64\multiquery"

if not exist "%win_exe%" (
    echo     ✗ Windows executable not found: %win_exe%
    pause
    exit /b 1
)

if not exist "%linux_exe%" (
    echo     ✗ Linux executable not found: %linux_exe%
    pause
    exit /b 1
)

REM Get file sizes
for %%A in ("%win_exe%") do set "win_size=%%~zA"
for %%A in ("%linux_exe%") do set "linux_size=%%~zA"

REM Convert bytes to MB
set /a "win_mb=!win_size! / 1048576"
set /a "linux_mb=!linux_size! / 1048576"

echo     ✓ Both executables verified
echo.

REM Display results
echo [5/5] Build Summary:
echo ========================================
echo.
echo Windows Executable:
echo   Location: %win_exe%
echo   Size:     !win_mb! MB (!win_size! bytes)
echo.
echo Linux Executable:
echo   Location: %linux_exe%
echo   Size:     !linux_mb! MB (!linux_size! bytes)
echo.
echo ========================================
echo 🎉 SUCCESS: Both platforms ready for distribution!
echo ========================================
echo.
echo Distribution Instructions:
echo • Windows: Copy 'multiquery.exe' to target Windows machine
echo • Linux:   Copy 'multiquery' to target Linux machine and run 'chmod +x multiquery'
echo.
echo Both executables are self-contained and require no additional dependencies.
echo.
echo 📋 PATH Installation:
echo For detailed instructions on adding MultiQuery to your system PATH,
echo see INSTALLATION.md in the project root directory.
echo.
echo 🆕 New Features:
echo • Automatic path resolution for SQL and environment files
echo • Files are resolved relative to your current working directory
echo • Absolute paths continue to work for full compatibility
echo • Use --verbose to see detailed path resolution information
echo.

REM Optional: Open publish folder
set /p "open_folder=Open publish folder? (y/n): "
if /i "!open_folder!"=="y" (
    explorer publish
)

echo.
echo Press any key to exit...
pause >nul