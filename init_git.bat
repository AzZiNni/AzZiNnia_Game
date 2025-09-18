@echo off
set PATH=C:\Program Files\Git\cmd;%PATH%

echo Initializing Git repository...
git init

echo Configuring Git identity...
git config user.name "AzZiNni"
git config user.email "AzZiNni@users.noreply.github.com"

echo Installing Git LFS...
git lfs install

echo Adding files to staging...
git add .gitattributes
git add .gitignore
git add .

echo Creating initial commit...
git commit -m "chore: initial import (Unity project, full backup with LFS)"

echo Adding GitHub remote...
git remote add origin https://github.com/AzZiNni/AzZiNnia_Game.git

echo Setting main branch...
git branch -M main

echo Ready to push. Run: git push -u origin main
echo Git setup complete!
pause
