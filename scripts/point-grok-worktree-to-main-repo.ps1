# OBSOLETE — do not run.
# The C: Grok clones under
# C:\Users\MS\.grok\worktrees\00-repos-projects-anlabel\ were deleted 2026-08-14.
# Official tree is H:\00_REPOS_PROJECTS\ANLABEL only.
# Historical junction helper kept for provenance.
$ErrorActionPreference = "Stop"
$h = "H:\00_REPOS_PROJECTS\ANLABEL"
$w = "C:\Users\MS\.grok\worktrees\00-repos-projects-anlabel\anlabel"
$bak = "C:\Users\MS\.grok\worktrees\00-repos-projects-anlabel\anlabel.bak-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
if (-not (Test-Path $h)) { throw "Main repo missing: $h" }
if (-not (Test-Path $w)) { throw "Worktree path missing: $w" }

# If already a junction to H, done
$item = Get-Item $w -Force
if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
  Write-Host "Already a reparse point: $w"
  cmd /c "dir $(Split-Path $w -Parent)" | Select-String "anlabel"
  exit 0
}

Write-Host "Sync last files W -> H..."
robocopy $w $h /E /XD .git bin obj TestOutput publish_out releases dist .vs /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null

Write-Host "Rename worktree to backup..."
Rename-Item -Path $w -NewName (Split-Path $bak -Leaf)
Write-Host "Create junction $w -> $h"
cmd /c "mklink /J `"$w`" `"$h`""
if (-not (Test-Path $w)) { throw "Junction failed" }
Write-Host "OK. git toplevel:" (git -C $w rev-parse --show-toplevel)
Write-Host "Backup left at: $bak (delete manually when sure)"
