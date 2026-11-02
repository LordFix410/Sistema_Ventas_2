@echo off
setlocal
if not exist "%~dp0logs" mkdir "%~dp0logs"
> "%~dp0logs\detener-servidor.flag" echo stop
echo Se solicito detener el servidor y el tunel. Espera unos segundos.
timeout /t 3 >nul
if exist "%~dp0logs\detener-servidor.flag" (
    echo El lanzador aun esta cerrando los procesos. Revisa logs\lanzador.log si tarda.
) else (
    echo El servidor y el tunel se detuvieron.
)
pause
