param (
    [string]$StackName,
    [string]$PrimaryRegion,
    [string]$SecondaryRegion,
    [int]$PrimaryWeight,
    [int]$SecondaryWeight
)

# Load environment variables from .env file if it exists
# First check in current directory, then try project root
$envFile = ".\.env"
if (-not (Test-Path -Path $envFile)) {
    $envFile = "..\..\\.env"
}

if (Test-Path -Path $envFile) {
    Get-Content $envFile | ForEach-Object {
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
    Write-Host "Loaded environment variables from $envFile"
} else {
    Write-Host "No .env file found. Using existing environment variables."
}

# Set default values if not provided
if (-not $StackName) { 
    if ($env:APPLICATION_NAME) {
        $StackName = "$($env:APPLICATION_NAME)-route53-setup"
    } else {
        $StackName = "global-sqs-demo-route53-setup"
    }
}
if (-not $PrimaryRegion) { $PrimaryRegion = $env:AWS_REGION_PRIMARY }
if (-not $PrimaryRegion) { $PrimaryRegion = "us-east-1" }
if (-not $SecondaryRegion) { $SecondaryRegion = $env:AWS_REGION_SECONDARY }
if (-not $SecondaryRegion) { $SecondaryRegion = "us-west-2" }

# Convert weight strings to integers
if (-not $PrimaryWeight) { 
    if ($env:ROUTE53_PRIMARY_WEIGHT) {
        $PrimaryWeight = [int]$env:ROUTE53_PRIMARY_WEIGHT
    } else {
        $PrimaryWeight = 90
    }
}

if (-not $SecondaryWeight) { 
    if ($env:ROUTE53_SECONDARY_WEIGHT) {
        $SecondaryWeight = [int]$env:ROUTE53_SECONDARY_WEIGHT
    } else {
        $SecondaryWeight = 10
    }
}

# Get the current stack parameters
Write-Host "Getting current stack parameters..."
$stack = aws cloudformation describe-stacks --stack-name $StackName | ConvertFrom-Json
$parameters = $stack.Stacks[0].Parameters

# Extract the required parameters
$hostedZoneId = ($parameters | Where-Object { $_.ParameterKey -eq "HostedZoneId" }).ParameterValue
$domainName = ($parameters | Where-Object { $_.ParameterKey -eq "DomainName" }).ParameterValue

# Update the stack with normal weights
Write-Host "Resetting Route53 weights: Primary=$PrimaryWeight, Secondary=$SecondaryWeight"
aws cloudformation update-stack `
    --stack-name $StackName `
    --use-previous-template `
    --parameters `
        ParameterKey=HostedZoneId,ParameterValue=$hostedZoneId `
        ParameterKey=DomainName,ParameterValue=$domainName `
        ParameterKey=PrimaryRegion,ParameterValue=$PrimaryRegion `
        ParameterKey=SecondaryRegion,ParameterValue=$SecondaryRegion `
        ParameterKey=PrimaryWeight,ParameterValue=$PrimaryWeight `
        ParameterKey=SecondaryWeight,ParameterValue=$SecondaryWeight

Write-Host "Waiting for stack update to complete..."
aws cloudformation wait stack-update-complete --stack-name $StackName

Write-Host "Failover reset completed successfully!"