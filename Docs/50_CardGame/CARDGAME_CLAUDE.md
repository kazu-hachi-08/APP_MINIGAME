# CardGame プロジェクト(タイトル: THE CHAOS Ⅱ)

シャドウバース型の 1v1 デジタルカードゲーム(ダークファンタジー世界観、写実寄りの絵柄)。
スマホ(Android / iOS)向け。友人に配って遊んでもらう規模。AI 対戦とオンライン対戦の両方を実装する。

## 役割分担

- **オーナー(ユーザー)**: 方針決定・実機テスト・フィードバック。コードは基本書かない。
- **Claude**: 仕様策定の相談相手、実装、テスト、Git 操作すべて。全権限を委ねられている。
- 迷ったら「まず一般的なテンプレで形にし、後からオリジナリティを足す」方向に倒す。

## リポジトリ構成

```
CLAUDE.md            このファイル
Docs/50_CardGame/spec/  仕様書(日本語)。実装より先に更新する
Docs/50_CardGame/adr/   設計判断の記録(Architecture Decision Records)
Core/                純粋 C# のゲームエンジン(Unity 非依存、netstandard2.1)
  CardGame.Core/       ルール・状態・効果解決・AI
  CardGame.Core.Tests/ NUnit テスト(dotnet test で実行)
  CardGame.Cli/        CLI 対局ツール(playtest 用)
Unity/               Unity プロジェクト(6000.6.x)。Core を参照する
.claude/skills/      定型作業(add-card, playtest, spec, build-apk)
.claude/agents/      レビュー用サブエージェント
```

## 鉄則

1. **仕様が先**: ルール・カード効果・画面の変更は、必ず `Docs/50_CardGame/spec/` を先に更新してから実装する。仕様書と実装が食い違ったら仕様書側が正。
2. **Core は Unity を知らない**: `Core/` に `UnityEngine` を一切参照させない。Core は決定論的(seed 付き RNG、順序依存のないコレクションを使わない)。
3. **Core にはテストを付ける**: ルール・カード効果は `Core/CardGame.Core.Tests/` で検証してから Unity に載せる。`dotnet test Core` が通らない状態でコミットしない。
4. **カードはデータ**: カード効果は `Docs/50_CardGame/spec/02-card-effects.md` の効果DSLで表現し、C# にカード固有ロジックを書かない。DSL で表現できない効果は、まず DSL の拡張を仕様に提案する。
5. **スコープを守る**: `Docs/50_CardGame/spec/06-roadmap.md` の現在フェーズ外の機能は勝手に足さない。提案は歓迎、実装は合意後。
6. **オーナーが触れる形にする**: フェーズ3以降は「実機で触れるビルド」を出すことを優先。凝った演出より動くこと。

## コーディング規約

- 言語: C# / 識別子は英語、コメントとドキュメントは日本語。
- Core: `namespace CardGame.Core.*`。public API には `///` ドキュメントコメント(日本語)。
- Nullable 有効、`var` は型が自明なときのみ。
- Unity 側: MonoBehaviour は薄く。ロジックは Core に、表示だけ Unity に。
- テスト名: `メソッド_条件_期待結果` の形式(例: `Attack_WardExists_MustTargetWard`)。

## Git 運用

- ブランチ: `main` に直接コミット(単独開発のため)。大きな実験のみ `feat/*` ブランチ。
- コミットは日本語で「何を・なぜ」。1コミット1論点。
- 仕様書のみの変更も普通にコミットする。
- コミットの末尾に `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` を付ける。

## よく使うコマンド

```bash
# Core のテスト
dotnet test Core

# CLI で AI 同士を対局(playtest)
dotnet run --project Core/CardGame.Cli -- selfplay --games 100

# Unity: コンパイル確認 + プロジェクト設定(冪等)
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Unity -executeMethod CardGame.Unity.Editor.ProjectSetup.Apply -logFile <log>

# Unity: Android APK(IL2CPP/ARM64、数分かかる) ※ /build-apk skill 参照
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Unity -buildTarget Android -executeMethod CardGame.Unity.Editor.BuildScript.BuildAndroid -logFile <log>

# Unity: Windows 版を作って自動操作+スクリーンショットで画面確認(Claude 用)
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath Unity -executeMethod CardGame.Unity.Editor.BuildScript.BuildWindows -logFile <log>
./dist/windows/CardGame.exe -autoplay-human -shots <dir> -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile <log>
```

## 現在のフェーズ

`Docs/50_CardGame/spec/06-roadmap.md` を参照。作業開始時に必ず確認する。

## 用語(日本語 ↔ コード)

| 日本語 | コード | 備考 |
|---|---|---|
| リーダー | Leader | プレイヤー本体、HP 20 |
| PP | PlayPoint | マナ相当 |
| フォロワー | Follower | 攻撃力/体力を持つ |
| スペル | Spell | 使い切り |
| アミュレット | Amulet | 場に残る |
| 守護 | Ward | |
| 疾走 | Storm | |
| 突進 | Rush | |
| 必殺 | Bane | |
| ドレイン | Drain | |
| ファンファーレ | Fanfare | 手札からプレイ時 |
| ラストワード | LastWords | 破壊時 |
| カウントダウン | Countdown | アミュレット用 |
| 進化 | Evolve | フェーズ5 |
| 場 | Board | 最大 5 体 |
| 墓場 | Graveyard | |

## Unity 側の作法

- Unity エディタは GUI で開かない前提。コンパイル確認・設定・ビルドは全てバッチモード(上記コマンド)。
- 画面確認は Windows ビルド + `DevAutoplay`(`-autoplay` = AI 観戦、`-autoplay-human` = 人間操作の模擬)で
  スクリーンショットを撮り、Read で目視する。UI を変えたら必ずこれで確認してからオーナーに渡す。
  他に `-preview-cards ID,ID`(カードの 3 サイズ表示)、`-preview-list`(カード一覧画面)、`-preview-draft`(ドラフトの一連の流れ)がある。
  オンラインは `-online-host` / `-online-guest`(`-online-draft` を足すとドラフトのデッキで接続)。
- UI は `Ui` ヘルパーでコード構築する。シーン・プレハブを手で編集しない(差分が読めないため)。
- Unity の C# は 9.0 相当。`record` / file-scoped namespace / `init` 以外の新機能は使わない。
  各ファイル冒頭に `#nullable enable`。
- `bin~` `obj~` は Unity が無視するフォルダ名。Core の .meta ファイルはコミットする。
- ログの grep: `error CS`, `[BuildScript]`, `[ProjectSetup]`, `Exception`。

## リポジトリと公開先

- ソース: https://github.com/jiantailangdasen6-rgb/cardgame(private、`origin`)
- ブラウザ版の公開: https://github.com/jiantailangdasen6-rgb/cardgame-web(public、`gh-pages` に WebGL ビルドのみ)
  → https://jiantailangdasen6-rgb.github.io/cardgame-web/ 。`bash scripts/deploy-pages.sh` で更新(/deploy-web)
- GitHub CLI: `"C:/Program Files/GitHub CLI/gh.exe"`(アカウント jiantailangdasen6-rgb)
- UGS(Relay): Project ID は ProjectSettings に設定済み

## 画像生成(ローカル ComfyUI)

- 導入先: `D:\tools\ComfyUI_windows_portable`(リポジトリ外)。モデル(`models/checkpoints/`):
  - `DreamShaperXL_Turbo_v2_1.safetensors`(ハースストーン風。カードイラスト・リーダー・HsParts。8 手順・CFG 2)
  - `Juggernaut-XL_v9_RunDiffusionPhoto_v2.safetensors`(写実寄り。旧イラストと一部の UI 素材)
- 起動 / 停止: `powershell -ExecutionPolicy Bypass -File scripts/comfy-start.ps1 [-Stop]`(http://127.0.0.1:8188、ローカルのみ)
- カード生成: `dotnet run --project Tools/ArtGen -- --provider local --only K010 --force`(既定はハースストーン風。SDXL 832×1216 → 512×1024 JPEG)
  - 題材は `Tools/ArtGen/subjects.json`(人物を描かせたくない題材は `no character` を含める)。検品で選び直したシードは `seed-overrides.json`
  - 生成後は必ず全枚を拡大して検品する(剣が 2 本・鞘が剣になる・題材と違う人物が出る、が起きやすい)
  - 旧い写実のイラストは `art-archive/`(ビルド外)
- 生成中は GPU を占有する。Unity のビルドと同時に回さない

## 音(SE / BGM)

- 音源はすべて `Tools/AudioGen` で合成する(外部素材を使わない = 権利関係が発生しない)。
  `dotnet run --project Tools/AudioGen [-- --only se|bgm]` → `Unity/Assets/Resources/Audio/{se,bgm}/*.wav`
- 音源の実体は物理モデル寄りの合成(撥弦 Karplus-Strong / 息を混ぜた笛 / 太鼓 / 鐘 / 金属)+ 簡易リバーブ。`Tools/AudioGen/Synth.cs`
- 再生は `Assets/Scripts/UI/Audio.cs`(効果音のプール + BGM のクロスフェード)。音量は PlayerPrefs に保存
- WAV は `AudioImporterSettings` が自動で Vorbis 圧縮にする(BGM はストリーミング、WebGL は展開)

## 容量(WebGL、2026-09-23 に 41MB → 約 15MB)

- 日本語フォントは画面に出る文字だけの 1 ファイル + カード名用(解星 徳明、カード名の文字だけ)(`bash scripts/subset-font.sh`)。**文言に新しい漢字を足したら実行し直す**(無い字は表示されない)。
  Windows 版は OS のフォントで欠けた字を補ってしまうので、字の欠けはブラウザ版で確認する(2 分割 + フォールバックは WebGL で効かなかった)
- 画像の取り込み設定は `Unity/Assets/Editor/CardArtImporter.cs`。部品(HsParts)は**縦横 4 の倍数・ミップマップなし**でないと無圧縮になる
- 圧縮は Gzip + 展開フォールバック(Brotli は Pages で読み込みに失敗した)、コード削減は Medium(`Unity/Assets/link.xml` で Core と Newtonsoft.Json を保護)、Unity ロゴの起動画面なし
- ビルドに入れない旧素材は `art-archive/`
