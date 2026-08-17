param(
    [string]$FileKey = "zdN71qfzrYV6pPt1b2FRRc",
    [string]$Token = $env:FIGMA_PAT,
    [int]$Scale = 2
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "Figma token is required. Pass -Token or set the FIGMA_PAT environment variable."
}

$iconsDir = Join-Path $PSScriptRoot "..\src\ANLAbel.App\Icons"
if (-not (Test-Path $iconsDir)) {
    New-Item -ItemType Directory -Path $iconsDir -Force | Out-Null
}

$nodeMap = [ordered]@{
    "6:12"  = "new"
    "6:18"  = "open"
    "6:22"  = "save"
    "6:28"  = "undo"
    "6:32"  = "redo"
    "6:36"  = "revisions"
    "6:43"  = "printer_status"
    "7:4"   = "folder"
    "7:13"  = "import_excel"
    "7:24"  = "update_excel"
    "7:36"  = "printer_setup"
    "7:45"  = "preview"
    "7:53"  = "print_current"
    "7:60"  = "print_all_rows"
    "7:66"  = "print_history"
    "7:71"  = "export_excel"
    "7:79"  = "test_print"
    "7:89"  = "panels"
    "7:95"  = "snap_objects"
    "7:103" = "snap_grid"
    "7:121" = "delete_selection"
    "7:132" = "help"
}

$headers = @{ "X-Figma-Token" = $Token }
$keys = @($nodeMap.Keys)
$idList = [string]::Join(",", $keys)

Write-Host "Connecting to Figma REST API to export $($nodeMap.Count) icon nodes (Scale: ${Scale}x)..."
$imgUrl = "https://api.figma.com/v1/images/{0}?ids={1}&format=png&scale={2}" -f $FileKey, $idList, $Scale
$res = Invoke-RestMethod -Uri $imgUrl -Headers $headers

if ($res.err) {
    throw "Figma API error: $($res.err)"
}

foreach ($nodeId in $nodeMap.Keys) {
    $iconName = $nodeMap[$nodeId]
    $downloadUrl = $res.images.$nodeId
    if (-not [string]::IsNullOrWhiteSpace($downloadUrl)) {
        $destPath = Join-Path $iconsDir "$iconName.png"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $destPath
        Write-Host "  -> Synced from Figma [$nodeId]: $iconName.png"
    } else {
        Write-Warning "Figma did not return image URL for node $nodeId ($iconName)"
    }
}

Write-Host "Figma icon sync completed successfully!"
