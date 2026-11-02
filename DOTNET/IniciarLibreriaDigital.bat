@echo off
setlocal
title Libreria Digital - servidor local
pushd "%~dp0"

if not exist "%~dp0ImagenPDF.csproj" (
  echo No encuentro ImagenPDF.csproj junto a este archivo.
  pause
  popd
  exit /b 1
)

echo Iniciando desde el proyecto actual: %~dp0
echo Se usara el SDK indicado por global.json en esta carpeta.
dotnet run --project "%~dp0ImagenPDF.csproj" --configuration Release --no-launch-profile -- --urls "http://0.0.0.0:5170"
set "APP_EXIT=%ERRORLEVEL%"
popd
if not "%APP_EXIT%"=="0" pause
exit /b %APP_EXIT%
