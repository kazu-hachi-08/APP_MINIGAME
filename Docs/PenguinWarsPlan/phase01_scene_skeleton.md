# Phase 1: シーン骨組み・タイトル登録・戦場・カメラ

## ゴール

タイトルの「ペンギン大戦争」ボタンから横画面のシーンに入り、仮の戦場（地面と左右の城）をドラッグで横スクロールできる。START! 表示の後、画面上部の経過時間が進む。

## 読むもの

* 仕様書: §1 概要、§2.4 ゲーム状態、§3 戦場、§7.1 画面レイアウト（図だけ）
* INDEX の「全体設計」
* 既存コード:
  * `Common/Scripts/Core/BaseMiniGameManager.cs`
  * `Common/Scripts/Scene/SceneNames.cs` / `ScreenOrientationApplier.cs`
  * `Common/Editor/TitleSceneBuilder.cs` の `CreateGameSelectButton` 呼び出し箇所（grep）
  * `05_LifeGame/Editor/LifeGameSceneBuilder.cs` の MenuItem・シーン保存・BuildSettings 登録部分（grep で該当メソッドだけ）

## 作るもの

| ファイル | 内容 |
| --- | --- |
| `Common/Scripts/Scene/SceneNames.cs` | `PenguinWars = "PenguinWarsScene"` を追加 |
| `Common/Scripts/Scene/ScreenOrientationApplier.cs` | `PenguinWars` を横画面のケースに追加 |
| `Common/Editor/TitleSceneBuilder.cs` | 「ペンギン大戦争」ボタンを追加（色は水色系） |
| `Scripts/Data/PenguinWarsBalance.cs` | ScriptableObject。この時点では `fieldLength`(30)・`castleHpEndless`(3000)・`castleHpVersus`(5000) だけ |
| `Scripts/Game/PenguinWarsGameManager.cs` | `BaseMiniGameManager` 継承。`Intro`（START! 1秒）→ `Playing` の遷移と経過時間 |
| `Scripts/Game/PenguinWarsPhase.cs` | enum `Draft / Intro / Playing / Finished`（仕様 §2.4） |
| `Scripts/View/BattleCamera.cs` | ドラッグ・←→キーでX方向スクロール。戦場の端でクランプ。開始時は左城側 |
| `Scripts/View/CastleView.cs` | 城の見た目（仮: 色付き四角）とHPバー表示の受け口 |
| `Scripts/UI/BattleHud.cs` | 経過時間 / 残り時間の表示、中央メッセージ（START! 等） |
| `Editor/PenguinWarsSceneBuilder.cs` (+ `.Field.cs` / `.Hud.cs`) | `Tools > MiniGame > Rebuild PenguinWars`。カメラ・地面・城2つ・HUD・GameManager を生成しシーン保存・BuildSettings登録 |
| `Data/PenguinWarsBalance.asset` | SceneBuilder 内で無ければ生成 |

* `Scripts/Battle/` の asmdef とフォルダ、`Tests/Editor/` の asmdef もこのフェーズで空で作っておく（Phase 2 ですぐ使う）

## 実装メモ

* 画面に映るのは戦場の約半分（仕様 §3.2）。カメラの orthographicSize とアスペクトから可視幅を計算してクランプする
* ドラッグはUIボタン上では反応させない（Phase 3 でボタンが入るため。`EventSystem.IsPointerOverGameObject` で弾く）
* Pause は基底クラスの仕組みをそのまま使う

## やらないこと

ユニット・出撃・お金・敵（Phase 2〜）。見た目の作り込み（Phase 6）。

## 完了条件

* [ ] コンパイルエラーなし
* [ ] Rebuild でシーンが生成され、BuildSettings に入る
* [ ] タイトルからシーンに入れて、横画面になる
* [ ] ドラッグ / ←→ で左右の城の間をスクロールでき、端で止まる
* [ ] START! の後、経過時間が増える。ポーズできる

## ユーザー確認手順

1. `Tools > MiniGame > Rebuild Title` と `Tools > MiniGame > Rebuild PenguinWars` を実行
2. TitleScene から再生 → ペンギン大戦争 → 上の完了条件を確認

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * 共通: `SceneNames.PenguinWars` / `ScreenOrientationApplier`（横画面）/ `TitleSceneBuilder`（Btn_Game_PenguinWars）
  * `Scripts/Battle/`: `MiniGame.PenguinWars.Battle.asmdef`（noEngineReferences）, `Side.cs`
  * `Scripts/Data/PenguinWarsBalance.cs` / `Scripts/Game/PenguinWarsGameManager.cs`, `PenguinWarsPhase.cs`
  * `Scripts/View/BattleCamera.cs`, `CastleView.cs`, `PlaceholderSprite.cs` / `Scripts/UI/BattleHud.cs`
  * `Editor/PenguinWarsSceneBuilder.cs`（+ `.Field.cs` / `.Hud.cs`）
  * `Tests/Editor/`: `MiniGame.PenguinWars.Battle.Tests.asmdef`, `SideTests.cs`
* 公開API（次フェーズが使うもの）:
  * `Side`（Left/Right）と拡張 `Forward()`（+1/-1）・`Opponent()`。namespace `MiniGame.PenguinWars.Battle`
  * `PenguinWarsBalance.FieldLength / CastleHpEndless / CastleHpVersus`
  * `PenguinWarsGameManager.Phase`（Intro→Playing）/ `ElapsedTime`。進行は `TickIntro` / `TickPlaying`（Playing 中の処理は `TickPlaying` に足す）
  * `CastleView.SetHp(current, max)` / `HideHp()`。城の原点は足元（X, 0）
  * `BattleCamera.Initialize(fieldLength)`
  * `BattleHud.SetElapsed(sec)` / `SetRemaining(sec)` / `ShowMessage(text)` / `HideMessage()`
  * `PlaceholderSprite`: 付けると実行時に 1x1 の白い四角（1ワールド単位）を貼る。大きさはスケール、色は SpriteRenderer.color
* 計画・仕様から変えた点:
  * UI上かどうかの判定は `IsPointerOverGameObject` ではなく、LifeGame の `BoardCamera` と同じ GraphicRaycaster へのレイキャストにした（タッチで押した瞬間のフレームに不正確なため）
  * Battle asmdef が空だと警告が出るので、Phase 2 で使う `Side` を先に入れた（テスト2本付き）
  * エンドレス前提なので、右城は HP バーを隠している（出現ゲート扱い）。見た目は仮の赤い四角のまま
* 次フェーズへの注意:
  * 座標: 地面の上端 Y=0 にユニットを立たせる。左城 X=0、右城 X=fieldLength。城の体は幅3・高さ4（`PenguinWarsSceneBuilder.Field.cs` の定数）
  * カメラは `_visibleWidth`（16）から orthographicSize を毎フレーム逆算している。カメラ Y=1.5 なので画面下部は地面（出撃ボタンを重ねる想定）
  * 城の位置は Rebuild 時の `FieldLength` で置く。FieldLength を変えたら Rebuild が必要
  * 戦闘の時間は `Time.deltaTime`（ポーズで止まる）で進めている。Phase 2 の BattleRunner も同様にすればポーズに追従する
