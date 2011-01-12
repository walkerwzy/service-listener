@echo off
:: The following directory is for .NET 2.0
set DOTNETFX2=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727
set PATH=%PATH%;%DOTNETFX2%

echo Installing Service Listening Service...
echo ===================================================
InstallUtil /i ListenerService.exe
echo ===================================================
echo Start Service Listening Service...
sc start "Service Listener"
echo Done.
pause