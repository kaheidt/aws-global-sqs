# PowerShell script to run the producer application with environment variables

# Load environment variables from .env file if it exists
if (Test-Path -Path ".\.env") {
    Get-Content ".\.env" | ForEach-Object {
        if ($_ -match "^\s*([^#][^=]+)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            # Remove quotes if present
            if ($value -match '^"(.*)"$' -or $value -match "^'(.*)'$") {
                $value = $matches[1]
            }
            [Environment]::SetEnvironmentVariable($key, $value)
        }
    }
    Write-Host "Loaded environment variables from .env file"
} else {
    Write-Host "No .env file found. Using existing environment variables."
}

# Run the producer application from its directory to ensure appsettings.json is found
Set-Location -Path .\AwsGlobalSqs.Producer
dotnet run
Set-Location -Path ..