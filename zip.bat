@echo off
cd /d %~dp0

set version=v1.0.0
set zip_data=excel-analysis

:: zip用フォルダ作成
mkdir %zip_data%
mkdir %zip_data%\data
mkdir %zip_data%\publish
xcopy /y data %zip_data%\data
xcopy /y publish\ExcelAnalysis.exe %zip_data%\publish
copy /y run.bat %zip_data%\run.bat

:: zip化
powershell compress-archive %zip_data%\* %zip_data%-%version%.zip -Force
rmdir /s /q %zip_data%
pause