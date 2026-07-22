@echo off
setlocal enabledelayedexpansion

set "WORK_DIR=%~dp0"
set "QUEUE_DIR=%WORK_DIR%.queue"
set "OUT_FILE=%WORK_DIR%.output.txt"
set "PID_FILE=%WORK_DIR%.pid.txt"

echo running > "%PID_FILE%"
echo. > "%OUT_FILE%"

:LOOP

if not exist "%PID_FILE%" goto END
set /p PID_STATUS=<"%PID_FILE%"
if "%PID_STATUS%"=="stopped" goto END

for %%F in ("%QUEUE_DIR%\*.txt") do (
    set "CMD_FILE=%%F"
    
    for /f "usebackq delims=" %%I in ("%%F") do (
        %%I >> "%OUT_FILE%" 2>&1
    )
    
    del /f /q "%%F" >nul 2>&1
)

ping -n 1 127.0.0.1 >nul

goto LOOP

:END
if exist "%PID_FILE%" del /f /q "%PID_FILE%"
exit