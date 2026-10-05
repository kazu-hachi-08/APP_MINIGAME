# Phase 2: セーブ・ステージ選択・★・リザルト

## ゴール

「ステージ」を押すとステージ選択画面が出て、クリアしたステージの次が開く。クリアすると★が付いて保存され、アプリを閉じても残る。仮ステージ3つ（`1-1`〜`1-3`）を順に遊べる。

## 読むもの

* INDEX の「決めた前提」（★の条件・セーブ）
* 前フェーズの引き継ぎメモ
* 既存コード: `PenguinWarsGameManager.cs`（タイトル → モード選択 → 開始・リザルトの流れ） / `PenguinZukanPanel.cs`・`ZukanCell.cs`・`PenguinWarsSceneBuilder.Zukan.cs`（スクロールするマス一覧の作り方） / `Common` のリザルト表示（`BaseMiniGameManager.FinishGame` の引数）

## 作るもの

### Battle（純C#）

| ファイル | 内容 |
| --- | --- |
| `StageResult.cs` | `Cleared` / `ElapsedSeconds` / `PlayerCastleHpRatio` / `KillCount`。`BattleWorld` から作る `From(world)` |
| `StarRule.cs` | `CountStars(stage, result)` → 0〜3 と、どの★を取ったかの bool 3つ。★2の閾値（50%）は定数ではなく引数か `StageDefinition` の既定値にする（Phase 6 でステージごとに変えたくなるかもしれないため） |
| `CampaignProgress.cs` | ステージID → 取った★（3つの bool。過去最高を OR で残す）・ベストタイム。`IsCleared(id)` / `IsPlayable(id)`（最初のステージ、または1つ前をクリア済み） / `Record(id, starFlags, seconds)` → 「初クリアか」「新しく取った★」を返す / `ToJson()` / `FromJson()`（壊れていたら空で始める） |
| `StageDefinitions.Chapter1.cs` | 仮ステージを `1-1`〜`1-3` に増やす（中身は Phase 6 で作り直すので適当でよい） |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `Scripts/Game/CampaignSave.cs` | `PlayerPrefs` キー `PenguinWars.Campaign` で `CampaignProgress` を読み書き。static クラス（`EndlessRecord` と同じ形） |
| `Scripts/UI/StageSelectPanel.cs` | 章ごとに横並びのノード6個（章はタブ or 左右ボタン）。各ノードに番号・★3つ・鍵。最後に遊んだステージを開いた状態で始める。「もどる」でモード選択へ |
| `Scripts/UI/StageNode.cs` | ノード1つの見た目（クリア済み・遊べる・ロック・ボスステージの形） |
| `Scripts/UI/StageDetailPanel.cs` | ノードを押すと出る: ステージ名・説明・★の条件3行（取ったものは光る）・ベストタイム・「しゅつげき」 |
| `PenguinWarsGameManager` | 流れ: モード選択「ステージ」→ ステージ選択 → 詳細 → 編成発表 → プレイ。終わったら `StageResult` → `StarRule` → `CampaignSave` に記録 |
| リザルト | クリア: タイトル `STAGE CLEAR!`、詳細に `★★☆`・`クリア mm:ss`（ベスト更新なら `NEW RECORD!`）、新しく取った★を書く。失敗: `GAME OVER`。ボタン: 「つぎのステージ」（クリア時・次がある時）/「もういちど」/「ステージ選択」 |
| `PenguinWarsPhase` | `StageSelect` を追加 |
| SceneBuilder | `PenguinWarsSceneBuilder.Stage.cs` を新規（選択パネル・詳細パネル） |

### Tests（`CampaignTests.cs`）

* 最初は `1-1` だけ遊べる / `1-1` クリアで `1-2` が遊べる
* ★は過去最高が残る（★3 → 次に★1でクリアしても★3のまま。別々の回で★2と★3を取ったら両方残る）
* ★2・★3 の判定境界（HPちょうど50%、目標タイムちょうど）
* JSON の往復で同じ内容に戻る / 壊れた JSON・空文字で例外にならない
* 定義表から消えたステージIDがセーブに残っていても落ちない

## 実装メモ

* リザルトの共通ボタン（リトライ・タイトル）で足りなければ、ペンギン大戦争固有のリザルトパネルを作ってよい（`Common` は変えない）
* 「つぎのステージ」「ステージ選択」もリトライと同じく Scene 再読み込み＋ static の行き先指定で実装する（試合の後片付けを書かずに済むため）
* ★3 の目標タイムは `StageDefinition.TargetSeconds`。仮ステージは適当な値でよい
* 章の解放は「前の章のボスステージをクリア」= `IsPlayable` の延長で済む

## やらないこと

編成画面・解放（Phase 3）、敵のプレビュー（Phase 4）、★獲得のアニメーション（Phase 7）。

## 完了条件

* [ ] EditMode テストが通る
* [ ] ステージ選択に `1-1`〜`1-3` が並び、最初は `1-1` だけ遊べる
* [ ] クリアで★が付き、次のステージの鍵が外れる。アプリ（エディタの再生）を止めて再開しても残っている
* [ ] リザルトから「つぎのステージ」「もういちど」「ステージ選択」が動く

## ユーザー確認手順

1. `Rebuild PenguinWars` → 再生 → `1-1` をクリア → 再生を止めて再開 → ★と鍵が残っているか見る
2. セーブを消したいときは `Edit > Clear All PlayerPrefs`（他のゲームの設定も消える点に注意）

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
* 公開API:
* 計画から変えた点:
* 次フェーズへの注意:
