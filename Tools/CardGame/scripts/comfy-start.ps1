# ローカルの ComfyUI(D:\tools\ComfyUI_windows_portable)をバックグラウンドで起動する。
# 使い方: powershell -File scripts/comfy-start.ps1 [-Stop]
# 起動後は http://127.0.0.1:8188 で API が使える(Tools/ArtGen の --provider local)。
param([switch]$Stop)

$root = "D:\tools\ComfyUI_windows_portable"
$python = Join-Path $root "python_embeded\python.exe"
$main = Join-Path $root "ComfyUI\main.py"
$log = Join-Path $root "comfy.log"

if ($Stop) {
    Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like "*ComfyUI\main.py*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
    "ComfyUI を停止した"
    exit 0
}

try { $r = Invoke-WebRequest -Uri "http://127.0.0.1:8188/system_stats" -UseBasicParsing -TimeoutSec 3; if ($r.StatusCode -eq 200) { "ComfyUI は起動済み"; exit 0 } } catch {}

if (-not (Test-Path $python)) { Write-Error "ComfyUI が見つからない: $root"; exit 1 }
# --windows-standalone-build は portable 版の既定。--listen は付けない(ローカルのみ)
Start-Process -FilePath $python -ArgumentList @("-s", $main, "--windows-standalone-build", "--port", "8188") -WorkingDirectory (Join-Path $root "ComfyUI") -WindowStyle Hidden -RedirectStandardOutput $log -RedirectStandardError "$log.err"
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Seconds 2
    try { $r = Invoke-WebRequest -Uri "http://127.0.0.1:8188/system_stats" -UseBasicParsing -TimeoutSec 3; if ($r.StatusCode -eq 200) { "ComfyUI 起動完了(http://127.0.0.1:8188)"; exit 0 } } catch {}
}
Write-Error "ComfyUI の起動を確認できなかった。$log を確認"
exit 1
