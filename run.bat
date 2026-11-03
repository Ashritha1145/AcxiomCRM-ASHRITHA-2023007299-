@echo off
set "PATH=C:\Users\AshuS\AppData\Local\Microsoft\dotnet;%PATH%"
cd /d "%~dp0AcxiomCRM" 2>nul || cd /d "%~dp0"
echo Starting AcxiomCRM on http://localhost:5218 ...
dotnet run --urls "http://localhost:5218"
pause
