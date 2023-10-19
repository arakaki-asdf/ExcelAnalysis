@echo off
cd /d %~dp0

:: win-x64 exe作成
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true --output %cd%\publish
pause