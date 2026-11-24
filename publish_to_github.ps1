Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Publishing FileForge to GitHub (0-Name-Alex)" -ForegroundColor Cyan
Write-Host "========================================================"

try {
    gh repo create 0-Name-Alex/FileForge --public --source=. --remote=origin --push --description "High-performance Windows file converter with batch processing, low memory governor, and global hotkey access."
    Write-Host "`n[SUCCESS] Successfully published to https://github.com/0-Name-Alex/FileForge" -ForegroundColor Green
} catch {
    Write-Host "`n[INFO] Attempting git push to origin main..." -ForegroundColor Yellow
    git push -u origin main
}
