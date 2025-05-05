param (
    [string]$HostedZoneId,
    [string]$DomainName,
    [string]$PrimaryRegion,
    [string]$SecondaryRegion,
    [int]$PrimaryWeight,
    [int]$SecondaryWeight,
    [string]$QueueName,
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
            Write-Host "Set environment variable $key to $value"
        }
    }
    Write-Host "Loaded environment variables from $envFile"
} else {
    Write-Host "No .env file found. Using existing environment variables."
}

# Set default values if not provided
if (-not $HostedZoneId) { $HostedZoneId = $env:ROUTE53_HOSTED_ZONE_ID }
Write-Host "HostedZoneId: $HostedZoneId"
if (-not $DomainName) { $DomainName = $env:ROUTE53_DNS_NAME }
if (-not $DomainName) { $DomainName = "sqs-global.example.com" }
Write-Host "DomainName: $DomainName"
if (-not $PrimaryRegion) { $PrimaryRegion = $env:AWS_REGION_PRIMARY }
if (-not $PrimaryRegion) { $PrimaryRegion = "us-east-1" }
Write-Host "PrimaryRegion: $PrimaryRegion"
if (-not $SecondaryRegion) { $SecondaryRegion = $env:AWS_REGION_SECONDARY }
if (-not $SecondaryRegion) { $SecondaryRegion = "us-west-2" }
Write-Host "SecondaryRegion: $SecondaryRegion"

# Convert weight strings to integers
if (-not $PrimaryWeight) { 
    if ($env:ROUTE53_PRIMARY_WEIGHT) {
        $PrimaryWeight = [int]$env:ROUTE53_PRIMARY_WEIGHT
    } else {
        $PrimaryWeight = 90
    }
}
Write-Host "PrimaryWeight: $PrimaryWeight"

if (-not $SecondaryWeight) { 
    if ($env:ROUTE53_SECONDARY_WEIGHT) {
        $SecondaryWeight = [int]$env:ROUTE53_SECONDARY_WEIGHT
    } else {
        $SecondaryWeight = 10
    }
}
Write-Host "SecondaryWeight: $SecondaryWeight"

if (-not $QueueName) { $QueueName = $env:SQS_QUEUE_NAME }
if (-not $QueueName) { $QueueName = "global-sqs-demo-queue" }
Write-Host "QueueName: $QueueName"
if (-not $ApplicationName) { $ApplicationName = $env:APPLICATION_NAME }
if (-not $ApplicationName) { $ApplicationName = "global-sqs-demo" }
Write-Host "ApplicationName: $ApplicationName"

# Validate parameters
if ([string]::IsNullOrEmpty($HostedZoneId)) {
    Write-Error "HostedZoneId is required. Set it via ROUTE53_HOSTED_ZONE_ID environment variable or pass it as a parameter."
    exit 1
}

# Deploy IAM roles
Write-Host "Deploying IAM roles..."
aws cloudformation deploy `
    --template-file ../CloudFormation/iam-roles.yaml `
    --stack-name "$ApplicationName-iam-roles" `
    --parameter-overrides ApplicationName=$ApplicationName `
    --capabilities CAPABILITY_NAMED_IAM

# Deploy SQS queues in primary region
Write-Host "Deploying SQS queues in $PrimaryRegion..."
aws cloudformation deploy `
    --template-file ../CloudFormation/sqs-queues.yaml `
    --stack-name "$ApplicationName-sqs-queues-$PrimaryRegion" `
    --parameter-overrides QueueName=$QueueName `
    --region $PrimaryRegion

# Deploy SQS queues in secondary region
Write-Host "Deploying SQS queues in $SecondaryRegion..."
aws cloudformation deploy `
    --template-file ../CloudFormation/sqs-queues.yaml `
    --stack-name "$ApplicationName-sqs-queues-$SecondaryRegion" `
    --parameter-overrides QueueName=$QueueName `
    --region $SecondaryRegion

# Deploy Route53 setup
Write-Host "Deploying Route53 setup..."
aws cloudformation deploy `
    --template-file ../CloudFormation/route53-setup.yaml `
    --stack-name "$ApplicationName-route53-setup" `
    --parameter-overrides `
        HostedZoneId=$HostedZoneId `
        DomainName=$DomainName `
        PrimaryRegion=$PrimaryRegion `
        SecondaryRegion=$SecondaryRegion `
        PrimaryWeight=$PrimaryWeight `
        SecondaryWeight=$SecondaryWeight

Write-Host "Deployment completed successfully!"