$ErrorActionPreference = "Stop"

# Set the working directory to the script's location (Project Root)
Set-Location $PSScriptRoot

# Set Environment Variables matching launch.json
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5253"

Write-Host "--> Configuring Environment: Development"
Write-Host "--> URLS: $env:ASPNETCORE_URLS"
Write-Host "--> Starting AIFORBI..."

# Run the application
dotnet run
