[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $GeneratorArguments
)

$ErrorActionPreference = 'Stop'
$ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$GeneratorPath = Join-Path $ScriptDirectory 'generate.py'

function Find-PythonLauncher {
    $py = Get-Command py -ErrorAction SilentlyContinue
    if ($null -ne $py) {
        return @($py.Source, '-3')
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -ne $python) {
        return @($python.Source)
    }

    return $null
}

if (-not (Test-Path -LiteralPath $GeneratorPath -PathType Leaf)) {
    Write-Error "Cannot launch the logic-map generator: generate.py was not found at '$GeneratorPath'."
    exit 1
}

$Python = Find-PythonLauncher
if ($null -eq $Python) {
    $answer = Read-Host 'Python was not found. Install Python with winget now? [y/N]'
    if ($answer -notmatch '^(?i:y|yes)$') {
        Write-Host 'The logic-map generator was not launched because Python is unavailable.' -ForegroundColor Yellow
        exit 1
    }

    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if ($null -eq $winget) {
        Write-Error 'Python cannot be installed automatically because winget is unavailable. Install Python, then run this script again.'
        exit 1
    }

    Write-Host 'Installing Python...'
    & $winget.Source install --exact --id Python.Python.3.14 --source winget --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Python installation failed (exit code $LASTEXITCODE)."
        exit $LASTEXITCODE
    }

    $Python = Find-PythonLauncher
    if ($null -eq $Python) {
        Write-Error 'Python was installed, but it is not available in this PowerShell session yet. Open a new PowerShell window and run this script again.'
        exit 1
    }
}

Write-Host 'Launching the logic-map generator...'
$PythonArguments = @()
if ($Python.Count -gt 1) {
    $PythonArguments = $Python[1..($Python.Count - 1)]
}

& $Python[0] @PythonArguments $GeneratorPath @GeneratorArguments
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host 'The logic-map generator completed successfully.' -ForegroundColor Green
}
else {
    Write-Host "The logic-map generator failed (exit code $exitCode)." -ForegroundColor Red
}

exit $exitCode
