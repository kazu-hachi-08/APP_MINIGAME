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

* [ ] EditMode テスト: 出撃したユニットが前進する / 射程内で止まって攻撃する / HP 0 で消える / 範囲攻撃が複数に当たる / 敵城にダメージが入る / 上限30体を超えて出撃しない
* [ ] 再生して 1〜3 キーで出撃、敵と戦闘、自城が落ちたら終了する

## ユーザー確認手順

1. `Tools > MiniGame > Generate PenguinWars Units`（名前は実装時に決定）→ `Rebuild PenguinWars`
2. Test Runner（EditMode）で `BattleWorldTests` を実行
3. 再生して完了条件を確認

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
* 公開API（次フェーズが使うもの）:
* 計画・仕様から変えた点:
* 次フェーズへの注意:
