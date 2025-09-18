@echo off
echo ========================================
echo Pushing Unity project to GitHub
echo ========================================
echo.

set PATH=C:\Program Files\Git\cmd;%PATH%

echo Checking Git status...
git status
echo.

echo Pushing to GitHub repository: AzZiNni/AzZiNnia_Game
echo This may take several minutes due to large files...
echo.

git push -u origin main

echo.
if %ERRORLEVEL% EQU 0 (
    echo ========================================
    echo SUCCESS! Project pushed to GitHub
    echo Repository: https://github.com/AzZiNni/AzZiNnia_Game
    echo ========================================
) else (
    echo ========================================
    echo ERROR occurred during push
    echo Check the output above for details
    echo ========================================
)

echo.
echo Press any key to close...
pause >nul
