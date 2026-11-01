@echo off
setlocal
set "APP_DIR=%~dp0publicado"

if not exist "%APP_DIR%\ImagenPDF.dll" (
  echo No encuentro publicado\ImagenPDF.dll.
  echo Publica la aplicacion con: dotnet publish -c Release -o publicado
  pause
  exit /b 1
)

pushd "%APP_DIR%"
dotnet ImagenPDF.dll --urls "http://0.0.0.0:5170"
set "APP_EXIT=%ERRORLEVEL%"
popd
exit /b %APP_EXIT%
