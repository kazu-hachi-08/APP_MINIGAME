# Phase 1: ステージの戦闘コア＋エンドレス撤去

## ゴール

モード選択の「ステージ」から仮のステージ1つ（`1-1`）が始まり、決まった時刻に決まった敵が出てきて、敵の城を叩くとボスが出る。敵の城を落とせば `STAGE CLEAR!`、自城が落ちれば `GAME OVER`。エンドレスのコードは残っていない。

## 読むもの

* INDEX の「決めた前提」「全体設計」
* 仕様書: §2.1 エンドレス、§2.3 勝敗、§3.3 城、§8 エンドレスの敵（**消すもの** の確認用）
* 既存コード: `EnemyWaveDirector.cs` / `EnemyWaveSettings.cs` / `BattleWorld.cs`（`TickEnemyWaves`・`SetEnemyWaves`・`EnemyLevel`） / `BattleSettings.cs` / `CastleState.cs` / `BattleEvent.cs` / `BattleRunner.cs`（`InitializeEndless`・`ApplyField`・`ApplyCastle`） / `PenguinWarsGameManager.cs` と `.Online.cs`（`MatchMode`） / `Tests/Editor/EndlessTests.cs`

## 作るもの

### Battle（純C#）

| ファイル | 内容 |
| --- | --- |
| `EnemySpawnEntry.cs` | 敵の出方1行: `UnitNo` / `StartTime`（条件を満たしてから最初に出るまでの秒） / `Interval` / `Count`（0 = 無限） / `StatMultiplier` / `TriggerCastleHpRatio`（敵城HPがこの割合以下で開始。1.0 = 最初から） / `IsBoss` |
| `StageDefinition.cs` | `Id`（`"1-1"`） / `Chapter` / `Index` / `Name` / `Description` / `FieldLength` / `PlayerCastleHp` / `EnemyCastleHp` / `Entries` / `TargetSeconds`（★3。Phase 2 で使う） / `UnlockNos`（Phase 3 で使う）。ギミックの項目は Phase 4 で足す |
| `StageDefinitions.cs` ＋ `StageDefinitions.Chapter1.cs` | `All`（並び順＝遊ぶ順） / `Find(id)`。このフェーズは仮の `1-1` だけ（壁と安いアタッカーが出て、敵城HP 50% で大型が1体出る程度） |
| `EnemyScriptDirector.cs` | `Tick(dt, enemyCastleHpRatio, spawns)`。各行は「条件を満たした時刻」から自分のタイマーを進める。ボスの行が出たら `BossAppeared` を返せるようにする。倍率は出現時に1回だけかける（`UnitStats.Scaled`） |
| `BattleWorld` | `SetEnemyWaves` → `SetEnemyScript`。`EnemyLevel` を削除。`ElapsedTime`（★3・リザルト用）を持つ。`BattleEventType.EnemyLevelUp` → `BossAppeared`（`Amount` = キャラNo） |
| `BattleSettings` / `CastleState` | `RightCastleInvincible` / `IsInvincible` を削除（敵の城は普通に攻撃できる） |
| 削除 | `EnemyWaveDirector.cs` / `EnemyWaveSettings.cs` |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `BattleRunner` | `InitializeEndless` → `InitializeStage(StageDefinition stage, IReadOnlyList<int> deckNos)`。カタログから `No → UnitStats` の辞書を作り、`EnemyScriptDirector` に渡す。戦場の長さ・城HP は `ApplyField` 相当で反映（オンラインのステージと同じ流れ） |
| `PenguinWarsGameManager` | `MatchMode.Endless` → `Stage`。`s_restartEndless` → `s_restartStageId`（リトライは同じステージの編成発表から）。勝ち: `STAGE CLEAR!`・`クリア mm:ss`、負け: `GAME OVER`・`撃破 N体`。結果の文字は仮でよい（Phase 2 で作り直す） |
| モード選択 | ボタン名「エンドレス」→「ステージ」。押すと `1-1` を開始（ステージ選択画面は Phase 2） |
| 編成 | 仮で今の `DeckRandomizer.PickDeck`（ランダム10体）のまま。Phase 3 で編成画面に置き換える |
| `BattleHud` | `ShowLevelUp` → `ShowBossAppeared`（仮の文字「ボス出現！」） |
| `PenguinWarsBalance` | 「エンドレス」欄（敵レベル・`CastleHpEndless`）を削除 |
| 削除 | `EndlessRecord.cs`、`CastleView.ShowAsGate` と `_gateSprite`、`FieldArtGenerator` の出現ゲートの絵、SceneBuilder の該当の参照 |

### Tests

* `EndlessTests.cs` を削除し、`StageScriptTests.cs` を作る
  * `StartTime` 前は出ない / `Interval` ごとに `Count` 回出て止まる / `Count = 0` は出続ける
  * `TriggerCastleHpRatio` 0.5 の行は、敵城HPが 50% を下回るまで出ない・下回ってから `StartTime` 後に出る
  * ボスが出ると `BossAppeared` イベントが1回だけ出る
  * 敵の城は攻撃でHPが減り、0 で `Loser == Side.Right`
  * 倍率が体力・攻撃にかかっている
  * 場の上限30体を超えた湧きは捨てられる（今のエンドレスの動作を引き継ぐ）
* `StageDefinitionTests.cs`: 全ステージの ID が重複しない / `UnitNo` がすべて 1〜50 / 敵城HP・戦場の長さが正

## 実装メモ

* 敵はこれまでどおりさかな・再生産なしで湧く（`RightSpawnsFree`）
* 敵城HPの割合は `BattleWorld` が `TickEnemyScript` を呼ぶときに渡す（Director に `BattleWorld` を持たせない。テストで単体で動かすため）
* 「条件付きの行」は一度条件を満たしたら、城HPが回復しなくても戻らない（城は回復しないが、念のため一方通行にする）
* `GuideDemoDirector`（あそびかたのデモ）が `RightCastleInvincible` や敵レベルを使っていないか確認し、使っていたら置き換える
* オンライン対戦のコードには触らない（`MatchMode` の名前変更に伴う修正だけ）

## やらないこと

ステージ選択画面・★・セーブ（Phase 2）、解放・編成画面（Phase 3）、ギミック（Phase 4）、演出（Phase 7）、仕様書の更新（Phase 8）。

## 完了条件

* [ ] EditMode テストがすべて通る（既存の他テストも壊れていない）
* [ ] `Endless` / `EnemyWave` / `Gate` で grep して、コードに残っていない（計画書・仕様書は除く）
* [ ] 再生して「ステージ」→ `1-1` が始まり、決まった敵が出てくる。敵の城にHPバーが出て、叩くと減る
* [ ] 敵城HPを半分まで削るとボスが出て「ボス出現！」が出る
* [ ] 敵の城を落とすと `STAGE CLEAR!`、負けると `GAME OVER`。リトライで同じステージから始まる
* [ ] オンライン対戦が今までどおり動く

## ユーザー確認手順

1. `Rebuild PenguinWars` → 再生
2. タイトル → スタート → 「ステージ」で1回クリア、1回わざと負ける
3. 部屋を作る → 別のウィンドウ（ブラウザ版など）で参加して、対戦が始まることを確かめる

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * Battle: `EnemySpawnEntry.cs` / `StageDefinition.cs` / `StageDefinitions.cs` / `StageDefinitions.Chapter1.cs`（仮の `1-1`「はじまりの氷原」） / `EnemyScriptDirector.cs`（同じファイルに出撃1体分の `EnemySpawn` 構造体）
  * Tests: `StageScriptTests.cs` / `StageDefinitionTests.cs` / `CannonRewardTests.cs`（`EndlessTests` のうちエンドレスと無関係な撃破報酬・砲・ランダム編成のテストを移した）
  * 削除: `EnemyWaveDirector.cs` / `EnemyWaveSettings.cs` / `EndlessRecord.cs` / `EndlessTests.cs` / `Sprites/Gate.png`（＋ .meta）
* 公開API:
  * `StageDefinitions.All`（遊ぶ順） / `StageDefinitions.Find(id)`（無ければ null）
  * `new EnemyScriptDirector(entries, IReadOnlyDictionary<int, UnitStats> statsByNo)` / `int Tick(dt, enemyCastleHpRatio, List<EnemySpawn> spawns)` … 戻り値はこのステップで初めて出たボスのキャラNo（無ければ 0）
  * `BattleWorld.SetEnemyScript(director)` / `BattleWorld.ElapsedTime`（Step で進む試合時間。HUD の「経過」とクリアタイムもこれ）
  * `BattleEventType.BossAppeared`（`Amount` = キャラNo。旧 `EnemyLevelUp` と同じ並び位置なのでオンラインの符号化は変わらない）
  * `BattleRunner.InitializeStage(stage, deckNos)` / `BattleRunner.PickRandomDeckNos()`（Phase 3 までの仮の編成）
  * `BattleHud.ShowBossAppeared()`（文字は `_bossAppearedText`）
  * GameManager: `StartStage(id)` / `EndStage(loser)` / `ShowCustomResult(title, isVictory, score, detail)`（VICTORY!/GAME OVER 以外のタイトル用。引き分けもこれを使う）
* 計画から変えた点:
  * **ボスの行は場の上限（30体）を無視して必ず出す**（捨てると山場が来ないため）。`BossAppeared` はボスの行の最初の1体のときだけ出る
  * 敵の出現は1ステップに1行1体まで（間隔 0 の行で無限ループしないように）
  * `CastleState` のコンストラクタから無敵フラグの引数を削除
  * `PenguinWarsGameManager.ElapsedTime` を削除し、`BattleWorld.ElapsedTime` に一本化
  * HUD の時間表示は「生存 mm:ss」→「経過 mm:ss」
  * ボスの音は仮で旧レベルアップ音（`PenguinWarsAudio.PlayLevelUp`）のまま。HUD のお知らせ欄の名前（`_levelUpLabel` / `LevelUpLabel`）もそのまま
  * `PenguinWarsBalance` の「エンドレス」欄は「ランダム編成（仮）」欄に改名し、編成人数・最低壁数だけ残した（Phase 3 で不要になる）。`CastleHpEndless` 削除後、`CreateBaseSettings` の城HPは常に対戦用の値（ステージは定義表で上書き）
  * あそびかたの「出撃と勝ち方」の説明からエンドレスの一文を削除。`GuideDemoDirector` は無敵城・敵レベルを使っていなかったので変更なし
* 次フェーズへの注意:
  * ステージの戦場の長さは `ApplyField(null, stage.FieldLength)` で反映し、色は白（`CurrentStage` は対戦用の `PenguinStageData` なので一人用では null）
  * リトライは `s_restartStageId` に今のステージIDを入れてシーンを読み直す。Phase 2 でステージ選択から始めるときも `StartStage(id)` を呼べばよい
  * 結果の文字（`STAGE CLEAR!`・`クリア mm:ss / 撃破 N体`、詳細欄はステージ名）は仮。★判定は `world.ElapsedTime` と `GetCastle(Side.Left)` の残りHPから作れる
  * `PenguinWarsBalance.asset` に旧エンドレスの値がシリアライズされたまま残るが、無害（次にアセットを保存したときに消える）
  * Battle 層とテストは Unity なしでも `dotnet test`（Battle/*.cs と Tests/Editor/*.cs を含めた net8.0 + NUnit のプロジェクト）で確かめられた。Unity 側のコードはエディタでのコンパイル確認が必要
