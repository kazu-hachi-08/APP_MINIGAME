# Windowsビルドをzip化し、GitHub Releasesに公開する。
#
# 使い方（リポジトリのルートで実行）:
#   .\Tools\Release\release-windows.ps1                  # テスト版（test-20260927-1530 のような日時タグ、Pre-release扱い）
#   .\Tools\Release\release-windows.ps1 -Version v0.1
#   .\Tools\Release\release-windows.ps1 -Version v0.2 -Notes "ゴルフのホール追加"
#
# 前提:
#   - UnityでWindows向けに Builds/Windows/ へビルド済みであること
#   - gh CLI がインストール済みで `gh auth login` 済みであること
#     （未導入ならzip作成までで止まり、手動アップロード手順を表示する）

param(
    [string]$Version,
    [string]$Notes = "zipを展開して exe を起動してください。`n初回に「WindowsによってPCが保護されました」と出たら「詳細情報 → 実行」で起動できます。",
    [string]$BuildDir = "Builds/Windows",
    [string]$ZipBaseName = "APP_MINIGAME_Windows"
)

$ErrorActionPreference = "Stop"

# バージョン未指定はテスト版扱い。Pre-releaseにすることで releases/latest は正式版を指したままにする
$isTest = [string]::IsNullOrEmpty($Version)
if ($isTest) { $Version = "test-" + (Get-Date -Format "yyyyMMdd-HHmm") }

# スクリプトの置き場所から辿ることで、どのフォルダで実行しても同じ結果にする
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$buildPath = if ([IO.Path]::IsPathRooted($BuildDir)) { $BuildDir } else { Join-Path $repoRoot $BuildDir }
$zipPath = Join-Path $repoRoot "Builds/${ZipBaseName}_$Version.zip"

if (-not (Get-ChildItem -Path $buildPath -Filter *.exe -ErrorAction SilentlyContinue)) {
    Write-Error "$buildPath に exe がありません。先にUnityで Windows ビルドを出力してください。"
}

# *_DoNotShip / *_ButDontShipItWithYourGame はデバッグ用で配布不要（Unity自身がそう命名している）
# .で始まるフォルダ（DLした配布物を展開した .Release など）はビルド成果物ではない。
# 混ざると次のzipに前回のzipが入れ子で入り、展開時にパスが長すぎてエラーになる
$items = Get-ChildItem -Path $buildPath | Where-Object {
    $_.Name -notlike "*DoNotShip*" -and $_.Name -notlike "*DontShip*" -and $_.Name -notlike ".*"
}

New-Item -ItemType Directory -Force -Path (Split-Path $zipPath) | Out-Null
if (Test-Path $zipPath) { Remove-Item $zipPath }
Write-Host "zip作成中: $zipPath"
$items | Compress-Archive -DestinationPath $zipPath

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "gh CLI が見つからないため、アップロードは手動で行ってください:"
    Write-Host "  1. https://github.com/kazu-hachi-08/APP_MINIGAME/releases/new を開く"
    Write-Host "  2. タグに $Version を入力し、$zipPath をドラッグ＆ドロップして Publish"
    if ($isTest) { Write-Host "     （テスト版なので「Set as a pre-release」にチェック）" }
    exit 0
}

$ghArgs = @($Version, $zipPath, "--title", $Version, "--notes", $Notes)
if ($isTest) { $ghArgs += "--prerelease" }
gh release create @ghArgs
# ghの失敗は例外にならないため明示的に止める（公開失敗時に古いテスト版まで消さないように）
if ($LASTEXITCODE -ne 0) { Write-Error "リリース作成に失敗しました。" }
Write-Host "公開完了: https://github.com/kazu-hachi-08/APP_MINIGAME/releases/tag/$Version"

# テスト版は最新1件だけ残し、一覧とタグが溜まり続けないようにする
if ($isTest) {
    $oldTags = gh release list --limit 100 --json tagName --jq '.[].tagName' |
        Where-Object { $_ -like "test-*" -and $_ -ne $Version }
    foreach ($tag in $oldTags) {
        Write-Host "古いテスト版を削除: $tag"
        gh release delete $tag --cleanup-tag --yes
    }
}
