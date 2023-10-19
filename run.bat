@echo off
cd /d %~dp0

:: 実行
%cd%\publish\ExcelAnalysis.exe sample.xlsx item.toml
pause