# Phase 5: 自動プレイによるステージ検証ツール

## ゴール

エディタのメニュー1つで、全ステージを「そのステージの時点で解放されているキャラ」で自動プレイし、勝てるか・何秒かかるか・城がどれだけ残るかを一覧にできる。Phase 6 で18ステージを調整するとき、毎回手で遊ばずに難しさの並びを確かめられる。

**なぜ作るか:** 18ステージを手で遊んで調整すると、1回の数値変更の確認に何十分もかかる。`BattleWorld` は純C#なので、画面なしで何倍速でも回せる。

## 読むもの

* 前フェーズの引き継ぎメモ
* 既存コード: `BattleWorld.cs`（`Step`・`Enqueue`・`CanSpawn`・`GetWallet`） / `BattleCommand.cs` / `CampaignUnlocks.cs` / `DeckRules.cs` / 既存の Editor メニューの書き方（`PenguinWarsSceneBuilder.cs` の `MenuItem`）

## 作るもの

### Battle（純C#）

| ファイル | 内容 |
| --- | --- |
| `SimpleBot.cs` | 左陣営を操作する簡単なCPU。1秒ごとに判断: ① さかなに余裕があり働きペンギンが一定レベル未満なら上げる ② 前線に味方の壁がいなければ一番安い壁 ③ それ以外は出せる中で一番高いキャラ ④ 砲がたまっていて範囲に敵が3体以上なら撃つ。強さを `BotSkill`（判断間隔・働きペンギンを上げる目標）で2段階（ふつう / うまい） |
| `StageSimulator.cs` | `Run(stage, deckNos, bot, seed, maxSeconds)` → `StageResult`。画面なしで固定ステップを回す。決着が付かなければ時間切れとして返す |
| `SimDeckPicker.cs` | 「そのステージを遊ぶ時点で解放されている」キャラから、検証用の10体を選ぶ（役割の偏りが出ないよう、壁3・アタッカー3・遠距離2・妨害/大型2 を目安に高い順） |

### Editor

| ファイル | 内容 |
| --- | --- |
| `Editor/StageSimulationMenu.cs` | `Tools > MiniGame > PenguinWars > Simulate Stages`。全ステージ × bot 2段階 × シード5回を回し、結果を表にして Console と `Temp/PenguinStageSim.md` に出す |

出力の例:

```text
| Stage | 名前 | ふつう 勝率 | ふつう 平均秒 | うまい 勝率 | うまい 平均秒 | 平均 自城HP | ★3目標 |
| 1-1 | はじめての海岸 | 5/5 | 82 | 5/5 | 61 | 92% | 90 |
| 1-6 | ボス: ... | 1/5 | 240 | 4/5 | 190 | 35% | 180 |
```

### Tests（`SimulatorTests.cs`）

* 同じシードなら同じ結果になる（決定的）
* `1-1` は「ふつう」の bot で勝てる（最初のステージで詰まないことの保証）
* 長時間のテストにしない（テストは `1-1` だけ。全ステージはメニューから回す）

## 実装メモ

* 目安にする基準（Phase 6 の調整で使う）:
  * 各章の1〜2番目: ふつう bot が 5/5 勝つ
  * 各章の3〜5番目: ふつう 3/5 以上、うまい 5/5
  * ボスステージ: ふつう 0〜2/5、うまい 3/5 以上（人間が工夫すれば勝てる）
  * ★3の目標タイム: うまい bot の平均秒 × 0.9 くらい（少し工夫が要る）
* bot はあくまで「目安を測る物差し」。強くしすぎない（人間より強い bot に合わせると難しすぎるステージになる）
* 大量に回すので、`BattleEvent` を溜め続けないよう、毎ステップ `DrainEvents` して捨てる

## やらないこと

bot を一人用のCPU対戦相手に使うこと（スコープ外）。ステージのデータ作成（Phase 6）。

## 完了条件

* [x] EditMode テストが通る（簡易ランナーで確認。エディタの Test Runner ではまだ）
* [ ] メニューを実行すると、仮ステージ3つの表が Console と `Temp/PenguinStageSim.md` に出る
* [x] 敵の数値を上げると勝率が下がる（物差しとして反応する）ことを1回確かめる
* [x] 全ステージ×2段階×5回が1分以内に終わる（.NET 上で0.6秒。エディタでの秒数は表の末尾に出る）

## ユーザー確認手順

1. `Tools > MiniGame > PenguinWars > Simulate Stages` を実行して表を見る
2. 手で遊んだ感じと bot の結果が大きく違ったら伝える（bot の判断を直す）

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * Battle: `BotSkill.cs`（bot の強さ） / `SimpleBot.cs`（検証用の bot） / `StageSimulator.cs`（画面なしで1回プレイ） / `SimDeckPicker.cs`（検証用の編成） / `StageSimReport.cs`（全ステージを回して Markdown の表にする）
  * Editor: `StageSimulationMenu.cs`（`Tools > MiniGame > PenguinWars > Simulate Stages`）
  * Tests: `SimulatorTests.cs`（7件）
  * 変更: `PenguinWarsBalance.CreateBattleSettings(versus, seed)` を追加し、`BattleRunner.CreateBaseSettings` はそれを呼ぶだけにした（検証ツールと実機で同じ数値を使うため） / `BattleWorld.CannonReach` を `internal` に（bot が砲の判断に使う）
* 公開API:
  * `new StageSimulator(createBaseSettings, statsByNo).Run(stage, deckNos, skill, seed, maxSeconds)` → `StageResult`。時間切れは `Cleared == false` かつ `PlayerCastleHpRatio > 0`
  * `StageSimulator.CreateDefault()`（`BattleSettings` の既定値＋定義表の数値。テスト用） / `StageSimulator.DefinitionStats()`（定義表から計算したキャラの数値。カタログのアセットと同じ値）
  * `SimDeckPicker.Pick(stage)` / `Pick(availableNos, stage)` / `AvailableNos(stage)`（初期10体＋前のステージの解放キャラ）
  * `StageSimReport.Build(simulator, stages, runsPerSkill, maxSeconds, onProgress)` → Markdown の表
  * `BotSkill.Normal`（判断1.5秒ごと・働きLv3まで） / `BotSkill.Skilled`（0.5秒ごと・Lv5まで）
* 計画から変えた点:
  * bot の判断を変えた。計画どおり（「前線に壁がいなければ一番安い壁」＋「出せる中で一番高いキャラ」）だと、安い壁を出し続けてさかなが貯まらず、強いキャラが一度も出なかった。今は **生きている壁が3体未満なら一番安い壁 → それ以外は壁以外で今出せる一番高いキャラ**
  * bot の判断間隔をシードで ±25% 揺らす（`SimpleBot(skill, seed)`）。揺らさないとシード5回がほぼ同じ結果になり、勝率の意味がなかった（戦闘の乱数は能力の発動だけなので差が出ない）
  * 表に「ふつう／うまい」それぞれの自城HP（勝った回の平均）と「★3目安」（うまい平均秒×0.9）の列を足した。勝率には時間切れの回数を「（時間切れN）」で添える（負けとは直し方が違うため）
  * 打ち切りは 600 秒（`StageSimulationMenu.MaxSeconds`）。表の末尾にかかった秒数を出す
  * 表の組み立ては Editor ではなく Battle（`StageSimReport`）に置いた。Unity を開かずに同じ表を出して確かめるため
* 確かめたこと（Unity を開かずに一時プロジェクトで `dotnet build`・簡易ランナー）:
  * EditMode テスト 152 件通過・1 件 Ignore（Phase 4 の網羅テストのまま）。Battle・Tests・Assembly-CSharp・Editor ともビルドが通る
  * 仮ステージ3つの表（BattleSettings 既定値で。メニューは Balance の値を使う）:

    | Stage | ふつう 勝率 | ふつう 平均秒 | うまい 勝率 | うまい 平均秒 | ★3目標 | ★3目安 |
    | --- | --- | --- | --- | --- | --- | --- |
    | 1-1 | 5/5 | 90 | 5/5 | 72 | 120 | 65 |
    | 1-2 | 5/5 | 120 | 5/5 | 103 | 150 | 92 |
    | 1-3 | 0/5（時間切れ4） | - | 0/5（時間切れ5） | - | 180 | - |

  * 物差しとしての反応: 1-2 の敵の倍率を ×1 / ×1.5 / ×2 にすると、勝率は 5/5 → 3/5 → 0/5 と下がる
  * 3ステージ×2段階×5回で約0.6秒（.NET 上）。18ステージでも数秒の見込み
* 次フェーズへの注意:
  * **仮ステージ 1-3 は bot では勝てない。** 原因は敵の砲（ダメージ150・範囲0.5）。コスト300以下のキャラは HP が 80〜190 なので、戦場の真ん中を越えて4体以上かたまると一撃で全滅し、敵の城まで届かない（砲を外すと うまい 5/5）。Phase 6 で作り直すときは、敵の砲のダメージを編成で出せるキャラの HP より低くするか、`MinTargets` を上げる
  * 調整の流れ: 定義表を変える → メニューを実行 → `Temp/PenguinStageSim.md` を見る。目安は「実装メモ」の基準。★3 の目標タイムは表の「★3目安」をそのまま入れればよい
  * bot は「人間が工夫すれば勝てる」かどうかは測れない（砲を撃つタイミングを見て攻める、などはしない）。ボスステージは うまい bot で勝てれば十分、勝てなくても手で遊んで確かめる
  * 編成は役割の目安（壁3・アタッカー3・遠距離2・妨害/大型2）で高い順。編成制限に当たるキャラは選ばないので、10体に満たないことがある（1-3 は8体）
