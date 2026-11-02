@echo off
setlocal
if not exist "%~dp0IniciarVentasOculto.vbs" exit /b 1
wscript.exe //B //NoLogo "%~dp0IniciarVentasOculto.vbs"
exit /b 0
