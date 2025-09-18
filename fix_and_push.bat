@echo off
echo ========================================
echo Fixing large files and pushing to GitHub
echo ========================================
echo.

set PATH=C:\Program Files\Git\cmd;%PATH%

echo Adding .asset files to Git LFS tracking...
git add .gitattributes

echo Migrating existing .asset files to LFS...
git lfs migrate import --include="*.asset" --everything

echo Adding all files again...
git add .

echo Creating new commit with LFS assets...
git commit -m "fix: migrate large .asset files to Git LFS"

echo Pushing to GitHub (this may take a while)...
git push -u origin main --force

echo.
if %ERRORLEVEL% EQU 0 (
    echo ========================================
    echo SUCCESS! Project pushed to GitHub with LFS
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
