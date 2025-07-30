@echo off
cd ..
echo Starting the pulling and compiling process
timeout 2
set current_branch=
for /F "delims=" %%n in ('git branch --show-current') do set "current_branch=%%n"
if "%current_branch%"=="" echo Not a git branch! && goto :EOF
git stash
git checkout master
git pull
dotnet build -c Release
dotnet publish -c Release --no-build -r win-x64 -p:PublishSingleFile=true -p:PublishReadyToRun=true -o .\build
git checkout %current_branch%
git stash apply
cd .\build
.\HandballBackend.exe -l false -u -b