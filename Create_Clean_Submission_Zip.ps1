# PowerShell script to create clean submission zip excluding generated folders
Write-Host "Creating clean submission ZIP for Aonix Assessment..." -ForegroundColor Cyan

$sourceDir = $PSScriptRoot
$zipFileName = "VR_Technical_Training_Bay_Clean_Project.zip"
$zipPath = Join-Path $sourceDir $zipFileName

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
    Write-Host "Removed existing zip file." -ForegroundColor Yellow
}

# Excluded folders as requested in assessment brief
$excludePatterns = @(
    "Library",
    "Temp",
    "Logs",
    "obj",
    "UserSettings",
    ".utmp",
    "Assets_dst",
    ".vs",
    ".git",
    "Builds"
)

# Use 7-Zip or PowerShell Compress-Archive
# Build file list
$itemsToZip = Get-ChildItem -Path $sourceDir | Where-Object {
    $name = $_.Name
    $exclude = $false
    foreach ($pattern in $excludePatterns) {
        if ($name -eq $pattern -or $name.EndsWith(".zip")) {
            $exclude = $true
            break
        }
    }
    -not $exclude
}

Write-Host "Items to include in archive:" -ForegroundColor Green
$itemsToZip | ForEach-Object { Write-Host " + $($_.Name)" }

Compress-Archive -Path ($itemsToZip.FullName) -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "`nSUCCESS! Clean ZIP created at:" -ForegroundColor Green
Write-Host "$zipPath" -ForegroundColor White
$zipItem = Get-Item $zipPath
Write-Host ("File Size: {0:N2} MB" -f ($zipItem.Length / 1MB)) -ForegroundColor Cyan
