@echo off
chcp 65001 > nul
title LabStock - Медицинская лаборатория

if exist "%~dp0bin\Release\net8.0-windows\win-x64\publish\LabStock.exe" (
    start "" "%~dp0bin\Release\net8.0-windows\win-x64\publish\LabStock.exe"
) else if exist "%~dp0bin\Debug\net8.0-windows\LabStock.exe" (
    start "" "%~dp0bin\Debug\net8.0-windows\LabStock.exe"
) else (
    dotnet run --project "%~dp0LabStock.csproj"
)
exit
