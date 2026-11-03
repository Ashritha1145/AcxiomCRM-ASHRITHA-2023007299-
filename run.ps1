$env:PATH = "C:\Users\AshuS\AppData\Local\Microsoft\dotnet;$env:PATH"
Set-Location "C:\Users\AshuS\OneDrive\Desktop\ACXIOM\AcxiomCRM"
Write-Host "Starting AcxiomCRM on http://localhost:5218 ..." -ForegroundColor Green
dotnet run --urls "http://localhost:5218"
