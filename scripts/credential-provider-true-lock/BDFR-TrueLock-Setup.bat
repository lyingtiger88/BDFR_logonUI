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
set "PACKAGE_EXPERIENCE=%ROOT%BDFR.LogonUI.Demo.exe"
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
echo   5. Diagnostics
echo   6. Exit
echo.
set /p "CHOICE=Select 1-6: "

if "%CHOICE%"=="1" goto PREFLIGHT
if "%CHOICE%"=="2" goto INSTALL_TRUELOCK
if "%CHOICE%"=="3" goto ROLLBACK_TRUELOCK
if "%CHOICE%"=="4" goto CANCEL_ROLLBACK
if "%CHOICE%"=="5" goto DIAGNOSTICS
if "%CHOICE%"=="6" goto END
goto MENU

:PREFLIGHT
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PREFLIGHT%" -ExperiencePath "%PACKAGE_EXPERIENCE%"
pause
goto MENU

:INSTALL_TRUELOCK
if not exist "%DLL%" (
  echo Missing Credential Provider DLL.
  pause
  goto MENU
)
if not exist "%ROOT%BDFR.LogonUI.Demo.exe" (
  echo Missing bundled BDFR.LogonUI.Demo.exe.
  pause
  goto MENU
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%INSTALL%" -DllPath "%DLL%" -ExperiencePath "%EXPERIENCE%" -PackagedExperiencePath "%ROOT%BDFR.LogonUI.Demo.exe" -RollbackMinutes 15 -Confirmation "I-UNDERSTAND-BDFR-TRUE-LOCK-TEST"
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

:DIAGNOSTICS
cls
echo ============================================================
echo                BDFR TRUE LOCK DIAGNOSTICS
echo ============================================================
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "Write-Host '--- BDFR settings ---'; Get-ItemProperty 'HKLM:\SOFTWARE\BDFR\LogonUI' -ErrorAction SilentlyContinue; Write-Host ''; Write-Host '--- Default Credential Provider ---'; Get-ItemProperty 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' -Name DefaultCredentialProvider -ErrorAction SilentlyContinue; Write-Host ''; Write-Host '--- BDFR Provider Registration ---'; Get-Item 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{55C142FF-F7C7-4A89-B6F1-1D64855CD366}' -ErrorAction SilentlyContinue; Write-Host ''; Write-Host '--- Experience ---'; Get-Item '%EXPERIENCE%' -ErrorAction SilentlyContinue | Select-Object FullName,Length,LastWriteTime; Write-Host ''; Write-Host '--- Secure launch log ---'; $p='$env:ProgramData\BDFR\LogonUI\CredentialProvider\secure-experience-launch.log'; if(Test-Path $p){Get-Content $p -Tail 40}else{Write-Host 'No secure launch log yet.'}"
echo.
pause
goto MENU

:END
endlocal
exit /b 0
