@echo off
cd /d %~dp0

%cd%\publish\ExcelAnalysis.exe sample.xlsx item.toml
pause