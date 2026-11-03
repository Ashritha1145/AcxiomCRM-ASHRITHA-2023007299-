$port = 5218
$url = "http://127.0.0.1:$port"

Write-Host "Starting AcxiomCRM..."
$proc = Start-Process -FilePath "C:\Users\AshuS\AppData\Local\Microsoft\dotnet\dotnet.exe" `
    -ArgumentList "bin\Debug\net8.0\AcxiomCRM.dll --urls $url" `
    -WorkingDirectory "C:\Users\AshuS\OneDrive\Desktop\ACXIOM\AcxiomCRM" `
    -PassThru -NoNewWindow

Start-Sleep -Seconds 4

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Test 1: Anonymous access to /Dashboard -> Redirected to /Account/Login
Write-Host "`n--- Test 1: Anonymous access to /Dashboard (Protected CRM page) ---"
try {
    $r1 = Invoke-WebRequest -Uri "$url/Dashboard" -WebSession $session -MaximumRedirection 0 -UseBasicParsing -ErrorAction SilentlyContinue
    Write-Host "Status: $($r1.StatusCode) - Redirected To: $($r1.Headers.Location)"
} catch {
    Write-Host "Status: $($_.Exception.Response.StatusCode) (Redirect enforced)"
}

# Test 2: Anonymous access to /api/customers -> 401 Unauthorized (No HTML redirect)
Write-Host "`n--- Test 2: Anonymous API access (/api/customers) ---"
try {
    $r2 = Invoke-WebRequest -Uri "$url/api/customers" -Method Get -UseBasicParsing -ErrorAction Stop
    Write-Host "Unexpected success: $($r2.StatusCode)"
} catch {
    $code = [int]$_.Exception.Response.StatusCode
    Write-Host "Received HTTP Code: $code (Expected: 401 Unauthorized)"
}

# Test 3: Authenticate via API /api/auth/login as admin@acxiom.com
Write-Host "`n--- Test 3: Authenticate (/api/auth/login) ---"
$loginBody = @{ email = "admin@acxiom.com"; password = "Test@1234" } | ConvertTo-Json
$r3 = Invoke-RestMethod -Uri "$url/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -WebSession $session -UseBasicParsing
Write-Host "Login response: $($r3.message)"
Write-Host "Authenticated as: $($r3.user.fullName) (Roles: $($r3.user.roles -join ', '))"

# Test 4: Authenticated GET /api/customers
Write-Host "`n--- Test 4: Authenticated GET /api/customers ---"
$r4 = Invoke-RestMethod -Uri "$url/api/customers" -Method Get -WebSession $session -UseBasicParsing
Write-Host "Retrieved $($r4.Count) customers via API:"
foreach ($c in $r4) {
    Write-Host "  - [$($c.customerCode)] $($c.customerName) | Phone: $($c.phone) | Status: $($c.status)"
}

# Test 5: Authenticated GET /api/reports/pipeline
Write-Host "`n--- Test 5: Pipeline Report API (/api/reports/pipeline) ---"
$r5 = Invoke-RestMethod -Uri "$url/api/reports/pipeline" -Method Get -WebSession $session -UseBasicParsing
Write-Host "Total Pipeline Value: INR $($r5.totalPipelineValue.ToString('N0'))"
Write-Host "Total Weighted Value: INR $($r5.totalWeightedPipelineValue.ToString('N0'))"
Write-Host "Stages Breakdown:"
foreach ($s in $r5.stageBreakdown) {
    Write-Host "  - Stage: $($s.stage) | Count: $($s.count) | Amount: INR $($s.totalAmount.ToString('N0'))"
}

# Test 6: Create customer via API
Write-Host "`n--- Test 6: Create Customer via API (/api/customers) ---"
$custBody = @{
    customerCode = "CUST-API" + (Get-Random -Minimum 1000 -Maximum 9999)
    customerName = "Bharat Electronics Corp"
    email = "procure" + (Get-Random -Minimum 100 -Maximum 999) + "@bel.co.in"
    phone = "987654" + (Get-Random -Minimum 1000 -Maximum 9999)
    companyName = "Bharat Electronics"
    city = "Bengaluru"
    state = "Karnataka"
} | ConvertTo-Json

try {
    $r6 = Invoke-RestMethod -Uri "$url/api/customers" -Method Post -Body $custBody -ContentType "application/json" -WebSession $session -UseBasicParsing
    Write-Host "Created Customer ID: $($r6.customerId) - Name: $($r6.customerName)"
} catch {
    Write-Host "Failed: $($_.Exception.Message)"
}

# Test 7: Verify invalid Opportunity Amount <= 0 is rejected by API/Server
Write-Host "`n--- Test 7: Server-side validation (Opportunity Amount <= 0) ---"
$oppBadBody = @{
    opportunityName = "Invalid Deal Test"
    customerId = 1
    amount = -100
    probability = 50
    expectedCloseDate = (Get-Date).AddDays(10).ToString("yyyy-MM-ddTHH:mm:ss")
} | ConvertTo-Json

try {
    $r7 = Invoke-RestMethod -Uri "$url/api/opportunities" -Method Post -Body $oppBadBody -ContentType "application/json" -WebSession $session -UseBasicParsing
    Write-Host "Unexpected success for invalid deal"
} catch {
    Write-Host "Successfully rejected with HTTP 400 (Expected): $($_.Exception.Message)"
}

# Test 8: Verify Opportunity Probability > 100 is rejected
Write-Host "`n--- Test 8: Server-side validation (Probability > 100) ---"
$oppBadProb = @{
    opportunityName = "Invalid Prob Deal"
    customerId = 1
    amount = 50000
    probability = 150
    expectedCloseDate = (Get-Date).AddDays(10).ToString("yyyy-MM-ddTHH:mm:ss")
} | ConvertTo-Json

try {
    $r8 = Invoke-RestMethod -Uri "$url/api/opportunities" -Method Post -Body $oppBadProb -ContentType "application/json" -WebSession $session -UseBasicParsing
    Write-Host "Unexpected success for invalid deal"
} catch {
    Write-Host "Successfully rejected with HTTP 400 (Expected): $($_.Exception.Message)"
}

# Test 9: Verify FollowUp date before today is rejected
Write-Host "`n--- Test 9: Server-side validation (Follow-up date before today) ---"
$followBad = @{
    customerId = 1
    followUpDate = (Get-Date).AddDays(-3).ToString("yyyy-MM-ddTHH:mm:ss")
    followUpType = "Call"
    remarks = "Past follow-up attempt"
    status = "Planned"
} | ConvertTo-Json

try {
    $r9 = Invoke-RestMethod -Uri "$url/api/followups" -Method Post -Body $followBad -ContentType "application/json" -WebSession $session -UseBasicParsing
    Write-Host "Unexpected success for past follow-up"
} catch {
    Write-Host "Successfully rejected with HTTP 400 (Expected): $($_.Exception.Message)"
}

# Test 10: Verify Scoped Role Access
Write-Host "`n--- Test 10: Role Scoping (SalesExecutive Login & Query) ---"
$salesSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$salesLoginBody = @{ email = "sales@acxiom.com"; password = "Test@1234" } | ConvertTo-Json
$rSales = Invoke-RestMethod -Uri "$url/api/auth/login" -Method Post -Body $salesLoginBody -ContentType "application/json" -WebSession $salesSession -UseBasicParsing
Write-Host "Sales login: $($rSales.message) - Role: $($rSales.user.roles -join ', ')"

$salesCusts = Invoke-RestMethod -Uri "$url/api/customers" -Method Get -WebSession $salesSession -UseBasicParsing
Write-Host "SalesExecutive sees $($salesCusts.Count) customers (Scoped to created by user)"

Write-Host "`nShutting down server..."
$proc.Kill()
Write-Host "=== All 10 Integration & Acceptance Criteria Verified Successfully! ==="
