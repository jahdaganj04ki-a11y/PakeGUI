@echo off
setlocal
rem Baut die Pake GUI als einzelne Windows-EXE nach dist\win-x64\PakeGUIWin.exe
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
if errorlevel 1 (
  echo.
  echo Build fehlgeschlagen.
  exit /b 1
)
echo.
echo Starten mit: dist\win-x64\PakeGUIWin.exe
endlocal
