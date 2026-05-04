
$uid = 'c74931e68d4d90ce9f11d6f343c1d54c'
Write-Host '1. Fetching Roulettes for UID...'
$response = Invoke-RestMethod -Uri "http://localhost:8080/api/admin/roulette/$uid" -Method Get
if ($response.isSuccess) {
    $rId = $response.data.data[0].id
    Write-Host "Found FuncRouletteMain ID: $rId"
    
    Write-Host '2. Triggering Spin Test (Scenario A)...'
    $spinResp = Invoke-RestMethod -Uri "http://localhost:8080/api/admin/roulette/$uid/$rId/test" -Method Post
    
    if ($spinResp.isSuccess) {
        $spinId = $spinResp.data[0].spinId
        Write-Host "Spin successful! SpinId: $spinId | Item: $($spinResp.data[0].name)"
        
        Write-Host '3. Simulating Animation Sync and Completion...'
        Start-Sleep -Seconds 2
        
        $completeBody = @{ spinId = $spinId } | ConvertTo-Json
        $compResp = Invoke-RestMethod -Uri "http://localhost:8080/api/admin/roulette/complete" -Method Post -ContentType 'application/json' -Body $completeBody
        
        if ($compResp.isSuccess) {
            Write-Host 'Simulation Completed successfully!'
        } else {
            Write-Host 'Completion call failed.'
        }
    } else {
        Write-Host 'Test spin failed.'
    }
} else {
    Write-Host 'Failed to fetch roulettes.'
}

