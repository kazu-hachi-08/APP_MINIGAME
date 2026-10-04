# Phase 4: エンドレス完成（敵レベル・撃破報酬・ペンギン砲・リザルト・ベスト記録）

## ゴール

エンドレスモードが「始まって・だんだん厳しくなって・終わって・記録が残る」1本のゲームとして遊べる。

## 読むもの

* 仕様書: §2.1 エンドレス、§2.3 勝敗、§4.4 ペンギン砲、§8 エンドレスの敵
* 前フェーズの引き継ぎメモ

## 作るもの

### Battle

| ファイル | 内容 |
| --- | --- |
| `EnemyWaveDirector.cs` | `SimpleEnemySpawner` を置き換え。30秒ごとにレベルUP、出現間隔・出現キャラ（コスト上限）・倍率（仕様 §8.1）。5の倍数レベルで大型確定（大型がまだ無ければ最もコストの高いキャラ） |
| `UnitStats` | 体力・攻撃に倍率をかけたコピーを作る `Scaled(multiplier)` |
| `CannonState.cs` | チャージ（0→40秒）・`TryFire()`。効果範囲内の敵にダメージ（このフェーズはダメージのみ。ノックバックは Phase 5 で追加） |
| `BattleCommand` | `FireCannon(side)` を追加 |
| `BattleWorld` | 撃破時に倒した側へ報酬（コストの50%、倍率なし）。`EnemyLevelUp` / `CannonFired` イベント。撃破数を記録 |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `PenguinWarsBalance` | 敵レベル・砲・報酬率の数値を追加 |
| `Scripts/UI/CannonButton.cs` | 右下。チャージゲージ、満タンで光る（仮: 色変化）。Space キー |
| `Scripts/Game/EndlessRecord.cs` | ベスト生存時間の `PlayerPrefs` 保存・読込 |
| `Scripts/Game/DeckRandomizer.cs`（Battle 側に置いてよい） | カタログから重複なしで10体抽選。今は3体しかないので全員 |
| `Scripts/UI/DeckIntroPanel.cs` | 編成発表（2秒）→ START! |
| `PenguinWarsGameManager` | 結果: タイトル GAME OVER、`生存 mm:ss / 撃破 N体`、ベスト更新なら `NEW RECORD!`。右端は出現ゲート（攻撃対象外、§2.1） |
| `BattleHud` | 敵レベルUP表示「LEVEL 5!」 |

## 実装メモ

* エンドレスの敵側は城なし。`BattleWorld` に「右側の城は無敵・攻撃対象外」のモードを持たせる（オンラインでは通常の城）
* 編成ランダムの「壁2体以上」制約は Phase 8（役割データが揃ってから）
* 倍率をかけるのは出現時に1回だけ（毎フレーム計算しない）

## やらないこと

ノックバック（Phase 5）、演出・音（Phase 7）。

## 完了条件

* [x] EditMode テスト: 30秒でレベルが上がる / 出現キャラがコスト上限内 / 撃破でさかなが増える / 砲はチャージ前に撃てず、撃つと範囲内の敵だけにダメージ
* [ ] 再生して、数分遊ぶと敵が明らかに強くなり、いつか負ける
* [ ] リザルトに生存時間・撃破数、2回目以降でベスト比較が出る。リトライ・タイトルに戻れる

## ユーザー確認手順

1. `Rebuild PenguinWars` → 再生して1プレイ
2. 敵の強くなり方がおかしければ `PenguinWarsBalance.asset` の数値を変えて試す

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * `Scripts/Battle/`: `EnemyWaveDirector` / `EnemyWaveSettings` / `CannonState` / `DeckRandomizer` / `BattleWorld.Combat.cs`（`BattleWorld` を partial に分け、ユニット行動・砲・ダメージ・撃破報酬をこちらへ移した）。`UnitStats.Scaled`・`BattleCommand.FireCannon`・`BattleEvent`（`EnemyLevelUp` / `CannonFired`）・`BattleSettings`（報酬率・砲）を追加。`SimpleEnemySpawner` は削除
  * `Scripts/Game/`: `EndlessRecord`（static クラス）、`PenguinWarsGameManager`（編成発表 → START! → リザルト）、`BattleRunner`（ランダム編成・敵レベル湧き）、`KeyboardCommandInput`（Space）
  * `Scripts/UI/`: `CannonButton` / `DeckIntroPanel`、`BattleHud`（`ShowLevelUp`）、`UnitButton`（`IconColor` を static 公開）
  * `Scripts/Data/PenguinWarsBalance`: 報酬率・砲・編成人数・敵レベルの数値、`CreateEnemyWaveSettings()`
  * `Editor/PenguinWarsSceneBuilder.Intro.cs`（編成発表パネル）、`.Hud.cs`（LEVEL表示）、`.Controls.cs`（砲ボタン）
  * `Tests/Editor/EndlessTests.cs`（9件）。`BattleWorldTests` から `SimpleEnemySpawner` のテストを削除
* 公開API（次フェーズが使うもの）:
  * `BattleWorld.SetEnemyWaves(director)` / `EnemyLevel` / `GetCannon(side)` / `GetKillCount(side)`
  * `CannonState`: `Charge` / `ChargeTime` / `ChargeRatio` / `IsReady` / `TryFire()`
  * `EnemyWaveDirector`: `Level` / `SpawnInterval` / `CostLimit` / `StatMultiplier` / `Tick(dt, spawns)`（レベルUPで true）
  * `DeckRandomizer.Pick(pool, count, random)`（ジェネリック）
  * `BattleEvent`: `Died.Amount` = 倒した側に入った報酬、`EnemyLevelUp.Amount` = 新レベル、`CannonFired.Side` = 撃った側・`X` = 届いた先端
  * `BattleWorld.Combat.cs` の `FireCannon` / `DamageUnit` がノックバックを足す場所（Phase 5）
* 計画・仕様から変えた点:
  * 湧きは `BattleRunner` ではなく `BattleWorld` が `Step` の中で進める（`EnemyLevelUp` イベントを World から出すため）。敵は編成スロットを使わず直接出す（右陣営の `SetDeck` は不要になった）
  * 倍率は仕様どおり `1.0 + レベル × 0.15` なので、レベル1でも 1.15 倍
  * 大型確定は「最もコストの高いキャラ」（コスト上限は無視）。役割データは Phase 8
  * 湧きが場の上限30体を超えた分は捨てる（溜めない）
  * 砲は城には当たらない。右陣営（CPU）も砲を持つが撃たない
  * リザルトの詳細: 更新時 `NEW RECORD!（前回ベスト mm:ss）`（初回は `NEW RECORD!` のみ）、それ以外 `ベスト mm:ss`
  * 城が崩れる演出（1.5秒）はまだ無し（Phase 7）。砲の「光る」は色変化（オレンジ）
  * 編成発表パネルのタイトルは「今回の編成」
* 次フェーズへの注意:
  * テストは `dotnet test`（Battle と Tests の .cs を集めたプロジェクト）で 28件合格。Unity 側は生成済み csproj（Assembly-CSharp / -Editor）を複製して PenguinWars のファイルを足した一時プロジェクトで `dotnet build` し、エラー0を確認（Unity エディタでのコンパイル・Rebuild・再生は未確認）
  * ランダム編成は全キャラから。まだ3体なので並び順が毎回変わるだけ
  * ベスト記録をリセットしたいときは PlayerPrefs の `PenguinWars.Endless.BestSeconds` を消す
