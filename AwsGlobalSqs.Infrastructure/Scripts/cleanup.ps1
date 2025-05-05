param (
    [string]$PrimaryRegion,
    [string]$SecondaryRegion,
    [string]$ApplicationName
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
if (-not $PrimaryRegion) { $PrimaryRegion = $env:AWS_REGION_PRIMARY }
if (-not $PrimaryRegion) { $PrimaryRegion = "us-east-1" }
if (-not $SecondaryRegion) { $SecondaryRegion = $env:AWS_REGION_SECONDARY }
if (-not $SecondaryRegion) { $SecondaryRegion = "us-west-2" }
if (-not $ApplicationName) { $ApplicationName = $env:APPLICATION_NAME }
if (-not $ApplicationName) { $ApplicationName = "global-sqs-demo" }

Write-Host "Starting cleanup of AWS resources..."
Write-Host "Application Name: $ApplicationName"
Write-Host "Primary Region: $PrimaryRegion"
Write-Host "Secondary Region: $SecondaryRegion"

# Delete Route53 setup
Write-Host "Deleting Route53 setup..."
aws cloudformation delete-stack --stack-name "$ApplicationName-route53-setup"
Write-Host "Waiting for Route53 stack deletion to complete..."
aws cloudformation wait stack-delete-complete --stack-name "$ApplicationName-route53-setup"

# Delete SQS queues in secondary region
Write-Host "Deleting SQS queues in $SecondaryRegion..."
aws cloudformation delete-stack --stack-name "$ApplicationName-sqs-queues-$SecondaryRegion" --region $SecondaryRegion
Write-Host "Waiting for SQS stack deletion in $SecondaryRegion to complete..."
aws cloudformation wait stack-delete-complete --stack-name "$ApplicationName-sqs-queues-$SecondaryRegion" --region $SecondaryRegion

# Delete SQS queues in primary region
Write-Host "Deleting SQS queues in $PrimaryRegion..."
aws cloudformation delete-stack --stack-name "$ApplicationName-sqs-queues-$PrimaryRegion" --region $PrimaryRegion
Write-Host "Waiting for SQS stack deletion in $PrimaryRegion to complete..."
aws cloudformation wait stack-delete-complete --stack-name "$ApplicationName-sqs-queues-$PrimaryRegion" --region $PrimaryRegion

# Delete IAM roles
Write-Host "Deleting IAM roles..."
aws cloudformation delete-stack --stack-name "$ApplicationName-iam-roles"
Write-Host "Waiting for IAM roles stack deletion to complete..."
aws cloudformation wait stack-delete-complete --stack-name "$ApplicationName-iam-roles"

Write-Host "Cleanup completed successfully!"