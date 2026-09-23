# カードゲーム 素材生成ツール

カードゲーム(`Assets/_Project/Games/50_CardGame/`)の画像・音・フォントを作るツール。移植元プロジェクトから持ってきたもの。

> **注意: 出力先・入力先のパスが移植元の構成のまま**
> 各ツールは移植元の `Unity/Assets/Resources/...` や `Tools/ArtGen/...` を参照しており、本リポジトリでは動かない。
> 実際の素材の置き場所は `Assets/_Project/Games/50_CardGame/Resources/`。使う前にパスを直すこと。
> また、リポジトリのルート判定は `CLAUDE.md` の有無で行っている(`FindRepoRoot`)。

## 画像生成(ArtGen / ローカル ComfyUI)

- ComfyUI の導入先: `D:\tools\ComfyUI_windows_portable`(リポジトリ外)。モデル(`models/checkpoints/`):
  - `DreamShaperXL_Turbo_v2_1.safetensors`(ハースストーン風。カードイラスト・リーダー・HsParts。8 手順・CFG 2)
  - `Juggernaut-XL_v9_RunDiffusionPhoto_v2.safetensors`(写実寄り。旧イラストと一部の UI 素材)
- 起動 / 停止: `powershell -ExecutionPolicy Bypass -File Tools/CardGame/scripts/comfy-start.ps1 [-Stop]`(http://127.0.0.1:8188、ローカルのみ)
- カード生成: `dotnet run --project Tools/CardGame/ArtGen -- --provider local --only K010 --force`
  - 既定はハースストーン風。SDXL 832×1216 → 512×1024 JPEG
  - 入力のプロンプトは `Docs/50_CardGame/art/card-art-prompts.csv`
  - 題材は `ArtGen/subjects.json`(人物を描かせたくない題材は `no character` を含める)。検品で選び直したシードは `ArtGen/seed-overrides.json`
  - 生成後は必ず全枚を拡大して検品する(剣が 2 本・鞘が剣になる・題材と違う人物が出る、が起きやすい)
- 生成中は GPU を占有する。Unity のビルドと同時に回さない

## 音(AudioGen)

- 音源はすべて合成で作る(外部素材を使わない = 権利関係が発生しない)
- `dotnet run --project Tools/CardGame/AudioGen [-- --only se|bgm]` → `Resources/Audio/{se,bgm}/*.wav`
- 合成方式は物理モデル寄り(撥弦 Karplus-Strong / 息を混ぜた笛 / 太鼓 / 鐘 / 金属)+ 簡易リバーブ。`AudioGen/Synth.cs`
- 再生は `Scripts/UI/Audio.cs`(効果音のプール + BGM のクロスフェード)。音量は PlayerPrefs に保存
- WAV は `Editor/AudioImporterSettings.cs` が自動で Vorbis 圧縮にする(BGM はストリーミング)

## フォント(subset-font)

- 日本語フォントは画面に出る文字だけに絞っている(本文用 + カード名用の解星 徳明)。`bash Tools/CardGame/scripts/subset-font.sh`
- **文言に新しい漢字を足したら実行し直す**(無い字は表示されない)
- Windows 版は OS のフォントで欠けた字を補ってしまうので、字の欠けは実機で確認する

## 画像の取り込み設定

- `Editor/CardArtImporter.cs` が自動で設定する
- 部品(HsParts)は**縦横 4 の倍数・ミップマップなし**でないと無圧縮になる
