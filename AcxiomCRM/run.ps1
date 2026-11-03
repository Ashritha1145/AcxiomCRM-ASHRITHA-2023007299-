$env:PATH = "C:\Users\AshuS\AppData\Local\Microsoft\dotnet;$env:PATH"
Set-Location $PSScriptRoot
Write-Host "Starting AcxiomCRM on http://localhost:5218 ..." -ForegroundColor Green
dotnet run --urls "http://localhost:5218"
