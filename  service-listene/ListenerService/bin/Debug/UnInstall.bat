@echo off
:: The following directory is for .NET 2.0
set DOTNETFX2=%SystemRoot%\Microsoft.NET\Framework\v2.0.50727
set PATH=%PATH%;%DOTNETFX2%

echo UnInstalling Service Listening Service...
echo ===================================================
InstallUtil /u ListenerService.exe
echo ===================================================
echo Done.
pause