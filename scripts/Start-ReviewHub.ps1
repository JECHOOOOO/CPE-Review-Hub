# Starts the existing MVC project, not the separate React prototype.
$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectDirectory 'CPE-Review-Hub.csproj'
$settingsFile = Join-Path $projectDirectory 'Properties\launchSettings.json'

try {
    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        throw 'The .NET 10 SDK is required. Install it, then double-click this launcher again.'
    }

    $sdks = & $dotnet.Source --list-sdks
    if ($LASTEXITCODE -ne 0 -or -not ($sdks -match '^10\.')) {
        throw 'The .NET 10 SDK was not found. Install the SDK, not just the runtime.'
    }

    $settings = Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json
    $url = ($settings.profiles.http.applicationUrl -split ';')[0]
    $address = [Uri]$url
    if (-not $address.IsLoopback -or $address.Scheme -ne 'http') {
        throw 'The http launch profile must use a local HTTP address.'
    }

    # Do not open an unrelated application or start a duplicate on this port.
    $listeners = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
    if ($listeners | Where-Object { $_.Port -eq $address.Port }) {
        Write-Host "Port $($address.Port) is already in use." -ForegroundColor Yellow
        Write-Host "If CPE Review Hub is already running, open $url in your browser."
        Write-Host 'Otherwise, stop the application using that port and try again.'
        exit 1
    }

    Set-Location -LiteralPath $projectDirectory
    Write-Host 'Starting CPE Review Hub...' -ForegroundColor Cyan
    Write-Host 'The browser will open when the server is ready.'
    Write-Host 'Keep this window open while using the system. Press Ctrl+C to stop.'
    Write-Host ''

    $browserOpened = $false
    # Keep dotnet in this console so its build errors and server logs stay visible.
    # Native stderr may contain warnings; do not turn those into terminating errors.
    $ErrorActionPreference = 'Continue'
    & $dotnet.Source run --project $projectFile --launch-profile http 2>&1 | ForEach-Object {
        $line = $_.ToString()
        Write-Host $line
        if (-not $browserOpened -and $line -match 'Now listening on:') {
            $browserOpened = $true
            Write-Host "Open $url if your browser does not launch." -ForegroundColor Green
            Start-Process -FilePath $url -ErrorAction Continue
        }
    }
    $serverExitCode = $LASTEXITCODE
    if ($serverExitCode -ne 0) {
        Write-Host 'The app could not run. Check the error messages above.' -ForegroundColor Red
    }
    exit $serverExitCode
}
catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
