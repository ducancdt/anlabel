<#
.SYNOPSIS
  Drives the real ANLAbel window through the plan 13 file-lifecycle cases (F01, F04, F06, F07)
  with UI Automation. Needs an interactive desktop session; it is an acceptance check, not
  part of the Fast gate.
.DESCRIPTION
  App data is redirected to a temporary folder (ANLABEL_DATA_ROOT), so the operator's logs,
  preferences and recovery copies are never touched. Scenarios:
    A  edit -> close -> "Lưu thay đổi?" -> Hủy keeps the app open -> Không lưu exits, file unchanged
    B  edit -> wait for the 60 s autosave -> kill the process -> restart -> "Tìm thấy bản phục hồi"
       -> open: the document comes back unsaved, the original file is untouched
    C  close the recovered document -> Lưu: the file is written, .bak exists, no recovery copy left
.EXAMPLE
  powershell -NoProfile -STA -ExecutionPolicy Bypass -File scripts/Invoke-ANLAbelLifecycleSmoke.ps1
#>
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\src\ANLAbel.App\bin\Debug\net8.0-windows\ANLAbel.App.exe'),
    [string]$Template = (Join-Path $PSScriptRoot '..\src\ANLAbel.App\TemplateLibrary\01_part_40x20_barcode.anlabel')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$root = Join-Path $env:TEMP ("anl-smoke-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$env:ANLABEL_DATA_ROOT = Join-Path $root 'data'
$doc = Join-Path $root 'smoke.anlabel'
Copy-Item $Template $doc
$recoveredDir = Join-Path $env:ANLABEL_DATA_ROOT 'Local\ANLAbel\Recovered'
$results = [ordered]@{}
$script:p = $null

function Wait-Until([scriptblock]$cond, [int]$seconds) {
    $deadline = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $deadline) { if (& $cond) { return $true }; Start-Sleep -Milliseconds 250 }
    return $false
}
function Find-Dialog() {
    # Search only this process's top-level windows. Walking every application on
    # the desktop is slow under load and can fail while another window changes;
    # such an error only means "not found yet" and the caller's wait retries.
    try {
        $pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $script:p.Id)
        $idCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, 'OperatorDialog')
        foreach ($window in [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)) {
            if ($window.Current.AutomationId -eq 'OperatorDialog') { return $window }
            $inner = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $idCond)
            if ($null -ne $inner) { return $inner }
        }
    } catch [System.Runtime.InteropServices.COMException], [System.Windows.Automation.ElementNotAvailableException] {
    }
    return $null
}
function Wait-Dialog([int]$seconds, [string]$failure) {
    $script:dialog = $null
    if (-not (Wait-Until { $script:dialog = Find-Dialog; $null -ne $script:dialog } $seconds)) { throw $failure }
    return $script:dialog
}
function Invoke-Choice($dialog, [string]$id) {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::AutomationIdProperty, "OperatorDialog.Choice.$id")
    $button = $dialog.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($null -eq $button) { throw "Choice $id not found" }
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}
function Start-App([string[]]$arguments) {
    $script:p = if ($arguments) { Start-Process -FilePath $Exe -ArgumentList $arguments -PassThru } else { Start-Process -FilePath $Exe -PassThru }
    if (-not (Wait-Until { $script:p.Refresh(); $script:p.MainWindowHandle -ne 0 } 30)) { throw 'Main window did not appear' }
    return [System.Windows.Automation.AutomationElement]::FromHandle($script:p.MainWindowHandle)
}
function Add-TextObject($win) {
    $btnCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)
    foreach ($b in $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)) {
        if ($b.Current.HelpText -like 'Chữ (một dòng*') { $b.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); return }
    }
    throw 'Text tool not found'
}
try {
    # Scenario A: cancel then discard at the close prompt.
    $win = Start-App @('"' + $doc + '"')
    [void](Wait-Until { $win.Current.Name -like 'smoke*' } 20)
    $results['A_title_after_open'] = $win.Current.Name
    Add-TextObject $win
    [void](Wait-Until { $win.Current.Name -like 'smoke`**' } 10)
    $results['A_title_dirty'] = $win.Current.Name
    $wp = $win.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
    $wp.Close()
    Invoke-Choice (Wait-Dialog 10 'Save prompt did not appear') 'cancel'
    Start-Sleep -Seconds 1
    $script:p.Refresh()
    $results['A_alive_after_cancel'] = -not $script:p.HasExited
    $wp.Close()
    Invoke-Choice (Wait-Dialog 10 'Save prompt did not appear again') 'discard'
    $results['A_exited_after_discard'] = Wait-Until { $script:p.Refresh(); $script:p.HasExited } 15
    $results['A_document_unchanged'] = ((Get-FileHash $doc).Hash -eq (Get-FileHash $Template).Hash)
    # Scenario B: crash after an autosave, then recover at the next start.
    $win = Start-App @('"' + $doc + '"')
    [void](Wait-Until { $win.Current.Name -like 'smoke*' } 20)
    Add-TextObject $win
    [void](Wait-Until { $win.Current.Name -like 'smoke`**' } 10)
    $results['B_title_dirty'] = $win.Current.Name
    $results['B_autosave_written'] = Wait-Until { (Test-Path $recoveredDir) -and @(Get-ChildItem $recoveredDir -Filter 'session-*.anlabel').Count -eq 1 } 90
    Stop-Process -Id $script:p.Id -Force
    [void](Wait-Until { $script:p.HasExited } 10)
    $win = Start-App @()
    $offer = Wait-Dialog 20 'Recovery offer did not appear'
    $results['B_offer_shown'] = $null -ne $offer
    $results['B_offer_title'] = $offer.Current.Name
    Invoke-Choice $offer 'open'
    [void](Wait-Until { $win.Current.Name -like 'smoke`**' } 20)
    $results['B_title_after_recovery'] = $win.Current.Name
    $results['B_original_untouched'] = ((Get-FileHash $doc).Hash -eq (Get-FileHash $Template).Hash)
    # Scenario C: save through the close prompt.
    $win.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
    Invoke-Choice (Wait-Dialog 10 'Save prompt did not appear') 'save'
    $results['C_exited_after_save'] = Wait-Until { $script:p.Refresh(); $script:p.HasExited } 20
    $results['C_document_saved'] = ((Get-FileHash $doc).Hash -ne (Get-FileHash $Template).Hash)
    $results['C_backup_written'] = Test-Path ($doc + '.bak')
    $results['C_recovery_files_left'] = if (Test-Path $recoveredDir) { @(Get-ChildItem $recoveredDir -File).Count } else { 0 }
}
finally {
    if ($script:p -and -not $script:p.HasExited) { Stop-Process -Id $script:p.Id -Force }
    $results.GetEnumerator() | ForEach-Object { '{0} = {1}' -f $_.Key, $_.Value }
    Remove-Item -Recurse -Force $root -ErrorAction SilentlyContinue
}
