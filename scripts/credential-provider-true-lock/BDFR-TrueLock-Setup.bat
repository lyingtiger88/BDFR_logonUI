@echo off
setlocal EnableExtensions
title BDFR LogonUI - True Lock Setup

set "ROOT=%~dp0"
set "DLL=%ROOT%BDFR.LogonUI.CredentialProvider.dll"
set "PREFLIGHT=%ROOT%Test-BDFRTrueLockPreflight.ps1"
set "INSTALL=%ROOT%Install-BDFRTrueLock.ps1"
set "ROLLBACK=%ROOT%Rollback-BDFRTrueLock.ps1"
set "CANCEL=%ROOT%Cancel-BDFRTrueLockRollback.ps1"
set "EXPERIENCE=%ProgramFiles%\BDFR\LogonUI\BDFR.LogonUI.Demo.exe"
set "STATE=%ProgramData%\BDFR\LogonUI\CredentialProvider\true-lock-state.json"

:MENU
cls
echo ============================================================
echo              BDFR LogonUI - TRUE LOCK
echo ============================================================
echo.
echo Run this BAT as Administrator.
echo.
echo   1. Preflight check
echo   2. Install / Enable True Lock
echo   3. Roll back True Lock
echo   4. Cancel automatic rollback
echo   5. Exit
echo.
set /p "CHOICE=Select 1-5: "

if "%CHOICE%"=="1" goto PREFLIGHT
if "%CHOICE%"=="2" goto INSTALL_TRUELOCK
if "%CHOICE%"=="3" goto ROLLBACK_TRUELOCK
if "%CHOICE%"=="4" goto CANCEL_ROLLBACK
if "%CHOICE%"=="5" goto END
goto MENU

:PREFLIGHT
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PREFLIGHT%" -ExperiencePath "%EXPERIENCE%"
pause
goto MENU

:INSTALL_TRUELOCK
if not exist "%DLL%" (
  echo Missing Credential Provider DLL.
  pause
  goto MENU
)
if not exist "%EXPERIENCE%" (
  echo BDFR LogonUI is not installed at:
  echo %EXPERIENCE%
  pause
  goto MENU
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%INSTALL%" -DllPath "%DLL%" -ExperiencePath "%EXPERIENCE%" -RollbackMinutes 15 -Confirmation "I-UNDERSTAND-BDFR-TRUE-LOCK-TEST"
echo.
echo First test: do not reboot. Press Win+L.
pause
goto MENU

:ROLLBACK_TRUELOCK
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%ROLLBACK%" -StatePath "%STATE%"
pause
goto MENU

:CANCEL_ROLLBACK
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%CANCEL%"
pause
goto MENU

:END
endlocal
exit /b 0
