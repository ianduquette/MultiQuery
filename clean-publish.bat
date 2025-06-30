@echo off
echo Cleaning MultiQuery publish directories...

if exist "publish" (
    rmdir /s /q "publish"
    echo ✓ Removed publish directory and all contents
) else (
    echo ✓ No publish directory found (already clean)
)

echo.
echo Cleanup complete!
pause