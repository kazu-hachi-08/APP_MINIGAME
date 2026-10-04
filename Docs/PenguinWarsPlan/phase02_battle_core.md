# Phase 2: 戦闘ロジック（BattleWorld）・3体・敵の湧き・城HP

## ゴール

PCの数字キー 1〜3 で味方ペンギン（仮の四角）を出撃でき、右から一定間隔で敵が湧く。ユニットは前進し、射程に入ると攻撃し合い、HP 0 で消える。自城HPが0になったら GAME OVER（仮のリザルト）。お金・再生産はまだ無い（キーを押せば出る）。

**このフェーズが全体の土台。** INDEX「全体設計」のロジック／表示分離を必ず守る。

## 読むもの

* 仕様書: §3.1 レーン、§5.1 データの持ち方、§5.2 行動
* INDEX の「全体設計」
* 前フェーズの引き継ぎメモ
* 既存コード: `04_Golf/Scripts/Simulation/*.asmdef` と `04_Golf/Tests/Editor/*.asmdef`（asmdef の書き方だけ）

## 作るもの

### Battle（純C#・`MiniGame.PenguinWars.Battle`）

| ファイル | 内容 |
| --- | --- |
| `Side.cs` | `Left` / `Right`。`Direction`（+1 / -1）と `Opponent` の拡張メソッド |
| `UnitStats.cs` | 仕様 §5.1 の数値部分（HP・攻撃・射程・間隔・発生・速度・範囲かどうか・コスト・再生産）。能力はまだ無し |
| `UnitState.cs` | 実行時の1体: `Id`（連番）・`UnitNo`・`Side`・`X`・`Hp`・`Action`（Walk / Windup / Cooldown / Dead）・タイマー |
| `CastleState.cs` | `Side`・`X`・`Hp`・`MaxHp` |
| `BattleCommand.cs` | `Spawn(side, slotIndex)` 等。このフェーズは Spawn だけ |
| `BattleEvent.cs` | `Spawned` / `Hit` / `Died` / `CastleDestroyed`（View・音・同期用。毎ステップ溜めて外から取り出す） |
| `BattleWorld.cs` | 状態一式・`Enqueue(command)`・`Step(dt)`・`DrainEvents()`。デッキ（slot → UnitStats）を陣営ごとに持つ |
| `UnitCombat.cs` | 射程判定・対象選び（単体=一番近い敵 / 範囲=射程内全員）・ダメージ。`BattleWorld` から呼ぶ static ヘルパー |
| `SimpleEnemySpawner.cs` | 一定間隔で `Right` 側に出撃させる仮の湧き（Phase 4 で置き換える） |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `Scripts/Data/PenguinUnitData.cs` | ScriptableObject。`ToStats()` で `UnitStats` に変換 |
| `Scripts/Data/PenguinUnitCatalog.cs` | `PenguinUnitData` のリスト、`Get(no)` |
| `Scripts/Game/BattleRunner.cs` | `BattleWorld` を持ち、固定ステップ（1/30秒）で `Step`。`Playing` 中だけ進める。イベントを View に配る |
| `Scripts/Game/KeyboardCommandInput.cs` | 1〜5キー → `Spawn(Left, slot)`（Phase 3 で UI ボタンと並存） |
| `Scripts/View/UnitView.cs` | 1体の表示（仮: 陣営色の四角＋HPバー）。`UnitState` を受けて位置を更新 |
| `Scripts/View/UnitViewPool.cs` | `UnitState.Id` ごとに `UnitView` を生成・再利用・破棄 |
| `Editor/PenguinUnitAssetGenerator.cs` | 仮の3体（No1 ペンギン=壁 / No11 おのペンギン=アタッカー / No23 ゆみペンギン=遠距離）のアセットとカタログを生成するメニュー |
| `Tests/Editor/BattleWorldTests.cs` | 下記の完了条件をテストで確認 |

* GameManager: 自城HP 0 のイベントで `FinishGame(false, ...)`（リザルトは仮）

## 実装メモ

* **ユニット同士は重なってよい**（にゃんこと同じ。押し合いはしない）。前進を止めるのは「射程内に敵がいるとき」だけ
* 城も攻撃対象。「一番近い敵」の候補に敵城を含める
* 攻撃の流れ: 射程に敵が入る → `Windup` 秒待つ → その瞬間の射程内にダメージ → `AttackInterval` から `Windup` を引いた時間待つ → 再判定（本家と同じく発生前に敵が消えたら空振り）
* 死亡ユニットは `Dead` にして同ステップ末でリストから除く（View が倒れ演出できるようにイベントだけ出す）
* ユニット数上限 30（仕様 §4.3）もここで入れる
* 仮3体の数値（目安）

| No | HP | 攻撃 | 射程 | 間隔 | 発生 | 速度 | 範囲 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 100 | 8 | 1.4 | 1.2 | 0.3 | 1.0 | 単体 |
| 11 | 200 | 40 | 1.5 | 1.5 | 0.5 | 1.0 | 範囲 |
| 23 | 120 | 30 | 3.5 | 2.5 | 0.6 | 0.8 | 単体 |

## やらないこと

お金・再生産・UIボタン（Phase 3）、敵レベル（Phase 4）、ノックバック・能力（Phase 5）。

## 完了条件

* [x] EditMode テスト: 出撃したユニットが前進する / 射程内で止まって攻撃する / HP 0 で消える / 範囲攻撃が複数に当たる / 敵城にダメージが入る / 上限30体を超えて出撃しない
* [ ] 再生して 1〜3 キーで出撃、敵と戦闘、自城が落ちたら終了する

## ユーザー確認手順

1. `Tools > MiniGame > Generate PenguinWars Units`（名前は実装時に決定）→ `Rebuild PenguinWars`
2. Test Runner（EditMode）で `BattleWorldTests` を実行
3. 再生して完了条件を確認

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * `Scripts/Battle/`: `UnitStats` / `UnitState`（+ `UnitAction`）/ `CastleState` / `BattleCommand` / `BattleEvent` / `BattleSettings` / `BattleWorld` / `UnitCombat` / `SimpleEnemySpawner`
  * `Scripts/Data/`: `PenguinUnitData` / `PenguinUnitCatalog`（`PenguinWarsBalance` に `SpawnOffset`・`MaxUnitsPerSide` を追加）
  * `Scripts/Game/`: `BattleRunner` / `KeyboardCommandInput`（`PenguinWarsGameManager` を更新）
  * `Scripts/View/`: `UnitView` / `UnitViewPool` / `HpBarView`（`CastleView` は `HpBarView` を使う形に変更）
  * `Editor/`: `PenguinUnitAssetGenerator`（メニュー `Tools > MiniGame > Generate PenguinWars Units`）/ `PenguinWarsSceneBuilder.Battle.cs`
  * `Data/Units/Unit_001・011・023.asset` と `Data/PenguinUnitCatalog.asset`（Generate / Rebuild 時に生成）
  * `Tests/Editor/BattleWorldTests.cs`（11件）
* 公開API（次フェーズが使うもの）:
  * `BattleWorld(BattleSettings)` / `SetDeck(side, IReadOnlyList<UnitStats>)` / `Enqueue(BattleCommand)` / `Step(dt)` / `DrainEvents(List<BattleEvent>)` / `Units` / `GetCastle(side)` / `CountUnits(side)` / `IsFinished` / `Loser`
  * `BattleCommand.Spawn(side, slot)`。コマンドは次の `Step` の先頭でまとめて処理。デッキ範囲外の slot・上限超えは黙って無視
  * `BattleEvent`: `Type`（Spawned / Hit / Died / CastleDestroyed）・`Side`（Hit/Died/CastleDestroyed は「やられた側」）・`UnitId`（城は `BattleEvent.CastleId` = -1）・`X`・`Amount`（ダメージ）
  * `UnitState`: `Id` / `UnitNo` / `Side` / `Stats` / `X` / `Hp` / `HpRatio` / `Action` / `ActionTimer`。setter は `internal`（Battle の外から書き換えない）
  * `BattleRunner.Initialize()` / `SetRunning(bool)` / `Enqueue(command)` / `IsRunning` / `event EventRaised(BattleEvent)`
  * `PenguinUnitCatalog.Get(no)` / `Units`、`PenguinUnitData.ToStats()` / `No` / `DisplayName`
  * `PenguinUnitAssetGenerator.EnsureAssets()`: 既存アセットは上書きせず、カタログは `Data/Units/` から No 順で集め直す。`Rebuild PenguinWars` からも呼ぶので、Generate を先に押さなくても動く
  * `BattleHud.FormatTime(sec)` を public static にした（リザルトの生存時間用）
* 計画・仕様から変えた点:
  * 計画にない `BattleSettings`（戦場の長さ・城HP・出現ゲートかどうか・出撃位置・上限）を足した。BattleWorld をテストで小さい戦場にして作るため
  * 出現ゲートは `CastleState.IsInvincible`。攻撃対象にならず、ユニットはゲートの X で止まる（射程で止まらない）
  * 射程判定は「前方距離 0〜Range」。後ろに回り込んだ敵は狙わない
  * 単体攻撃で敵ユニットと城が同じ距離ならユニットを優先
  * 範囲攻撃は射程内なら城にも当たる
  * Cooldown が終わったステップは Walk に戻るだけで、射程の再判定は次のステップ（1/30秒遅れ。体感差なし）
  * ユニット上限 30 は陣営ごと
  * 城HPの表示は GameManager ではなく `BattleRunner` が毎フレーム `CastleView` に流す（城の参照も Runner に移した）
  * 仮の見た目: 陣営色（青/赤）の四角。攻撃発生待ち（Windup）中だけ白っぽくなる。3体の区別は見た目では付かない（Phase 6）
  * 仮3体のコスト・再生産（Phase 3 用の初期値）: No1=75/2秒、No11=300/6秒、No23=450/8秒
  * 敵は左と同じ3体の編成から `SimpleEnemySpawner` が4秒ごとにランダムで湧く（間隔は `BattleRunner._enemySpawnInterval`）
* 次フェーズへの注意:
  * 編成は `BattleRunner._deckUnitNos`（{1, 11, 23}）。1〜3キーがこの順に対応
  * お金・再生産を入れるときは `BattleWorld.TrySpawn` にチェックを足す（ロジック側で判定する。オンラインでゲストの出撃もホストが判定するため）
  * 固定ステップ 1/30 秒・1フレーム最大5ステップ（`BattleRunner` の定数）。ポーズ中は `SetRunning(false)` で止め、キー入力も捨てる
  * View の位置は 30Hz で更新しているので、速いユニットでカクつくなら補間を検討
  * Unity が開いていてバッチモードが使えなかったため、テストは `dotnet test`（Battle と Tests の .cs だけを集めたプロジェクト）で実行して 13件合格を確認。Unity 側スクリプトのコンパイルは未確認
