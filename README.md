# 週末 2Dミニゲームプロジェクト

週末の限られた時間で、2人で2Dミニゲームを作って積み重ねていくプロジェクト。
**小さく作って、遊べる状態にして、次のゲームへ** を基本方針とする。

## ▶ ブラウザで遊ぶ

**https://kazu-hachi-08.github.io/minigame-web/**

* WebGLビルドを別リポジトリ（`minigame-web`）の GitHub Pages で公開している

## ▶ Windows版をダウンロード

**https://github.com/kazu-hachi-08/APP_MINIGAME/releases**

* zipを展開して `MiniGame.exe` を起動する
* 初回に「WindowsによってPCが保護されました」と出たら「詳細情報 → 実行」で起動できる
* `test-` で始まるものはテスト版（Pre-release）
* 公開手順: `Tools/Release/release-windows.ps1` の先頭コメントを参照

---

## ミニゲーム一覧

| # | ゲーム | 仕様書 | コード |
| - | ------ | ------ | ------ |
| 01 | 2Dサッカー | [01_SOCCER_SPEC.md](Docs/01_SOCCER_SPEC.md) | `Assets/_Project/Games/01_Soccer/` |
| 02 | 2D卓球 | [02_TABLE_TENNIS_SPEC.md](Docs/02_TABLE_TENNIS_SPEC.md) | `Assets/_Project/Games/02_TableTennis/` |
| 03 | 2Dモルック | [03_MOLKKY_SPEC.md](Docs/03_MOLKKY_SPEC.md) | `Assets/_Project/Games/03_Molkky/` |
| 04 | 2Dゴルフ | [04_GOLF_SPEC.md](Docs/04_GOLF_SPEC.md) | `Assets/_Project/Games/04_Golf/` |
| 50 | デジタルカードゲーム（THE CHAOS Ⅱ） | [50_CARD_GAME_SPEC.md](Docs/50_CARD_GAME_SPEC.md) | `Assets/_Project/Games/50_CardGame/` |

---

## 技術スタック

| 項目 | 内容 |
| ---- | ---- |
| ゲームエンジン | Unity |
| 言語 | C# |
| グラフィック | 2Dスプライト |
| 対象プラットフォーム | Android / iPhone / Windowsブラウザ（WebGL） |
| ソース管理 | GitHub |
| 開発人数 | 2人 |

## ディレクトリ構成

```text
Assets/_Project/
├── Common/   # 全ミニゲーム共通（Scene / Input / UI / Audio 管理など）
└── Games/    # 各ミニゲーム固有の処理
Docs/         # 仕様書・開発手順
Tools/        # 開発補助ツール
```

* `Common/` には特定のミニゲームに依存するコードを書かない
* 新しいミニゲームは `Games/` 配下にディレクトリを追加し、仕様書を `Docs/` に置く

## 開発環境のセットアップ

[Docs/DEVELOPMENT_SETUP.md](Docs/DEVELOPMENT_SETUP.md) を参照。

---

## 開発ルール

### 基本方針

1. 小さく作る
2. 早く遊べる状態にする
3. 毎週末 Playable Build を残す
4. 必要以上に仕様を増やさない（当初構想の約1/3を目標スコープにする）
5. 1つのミニゲームに時間をかけすぎない

**Playable Build** = 完成版ではなく、その時点の機能で実際に操作・プレイできる状態。グラフィックや演出が未完成でもよい。

実装の優先順位：

```text
必須機能 → ゲームとして成立する機能 → 操作性 → 演出 → 追加要素
```

### Git運用

```text
main
├── feature/#xx   # Issue番号でブランチを切る
└── fix/xxx
```

featureブランチで実装 → 動作確認 → Pull Request → レビュー → main へ Merge

### Unity開発時の注意

Scene / Prefab は2人で同時編集すると競合しやすい。

* 同じ Scene / Prefab を同時に大きく変更しない
* 機能ごとに小さな Prefab に分割する、データは ScriptableObject に切り出す
* 作業範囲（例：Aさん＝Player関連、Bさん＝UI関連）を意識して分担する
