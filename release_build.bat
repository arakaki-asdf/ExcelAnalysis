@echo off
cd /d %~dp0

dotnet publish -c Release -r win-x64 --sc -p:PublishReadyToRun=true /p:PublishSingleFile=true --output %cd%\publish
pause