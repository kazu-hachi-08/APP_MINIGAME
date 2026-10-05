# Phase 4: ステージのギミック・編成制限・敵のペンギン砲・出撃前の紹介

## ゴール

ステージごとに「敵の出方」以外の個性を付けられる。ステージ詳細で、出てくる敵と特別ルールが前もって分かる。Phase 6 でステージを量産するときに使う部品がすべてそろう。

## 読むもの

* 前フェーズの引き継ぎメモ
* 仕様書: §3.4 ステージ（なだれ・戦場の長さ・色）、§4.1〜4.4（さかな・働きペンギン・ペンギン砲）
* 既存コード: `PenguinStageData.ApplyTo` / `BattleWorld.Avalanche.cs` / `WalletState.cs`・`WalletTable.cs` / `CannonState.cs` / `BattleRunner.ApplyField`（色・長さの反映）

## 作るもの

### ギミック（`StageDefinition` に項目を足し、`BattleSettings` に反映する）

| ギミック | 項目 | 内容 |
| --- | --- | --- |
| 戦場の見た目 | `TintHex` | 山と地面の色（オンラインのステージと同じ仕組み）。章ごとの雰囲気づくり |
| なだれ | `AvalancheInterval` ほか | オンラインの「なだれの谷」と同じもの（既存の仕組みをそのまま使う） |
| 開始さかな | `StartingFish` | 最初から持っているさかな（多い＝速攻ステージ、0＝通常） |
| 働きペンギンの上限 | `MaxWalletLevel` | これより上げられない（さかなが少ないステージ） |
| 敵のペンギン砲 | `EnemyCannon`（チャージ秒・範囲・ダメージ） | 敵の城が砲を撃つ。ボスステージで使う |
| 編成制限 | `MaxUnitCost` / `BannedRoles` | 「コスト 600 以下のみ」「大型禁止」など。出撃前に編成をチェックし、外れているキャラはそのステージだけ枠を暗くして出せなくする（編成そのものは書き換えない） |

* 敵の砲を撃つ判断は Battle 側の小さいクラス `EnemyCannonAi`: チャージ完了かつ「範囲内に味方が N 体以上 or 自城（敵から見て）に近い味方がいる」で撃つ。N は定義の項目にする
* 編成制限のチェックは `DeckRules` に足す（`IsAllowed(stage, unitStats)`）。Battle の `CanSpawn` でも同じ判定を使う（ボタンの暗さと実際の出撃をそろえる既存の考え方に合わせる）

### 出撃前の紹介

| 場所 | 内容 |
| --- | --- |
| `StageDetailPanel` | 「出てくる敵」に `Entries` の重複なしの顔アイコン（最大8体、ボスは枠を赤く） ／ 特別ルールのラベル（「なだれ」「スタート時 さかな1500」「大型禁止」など。ギミックがあるものだけ） |
| 編成発表 | ステージ名と特別ルールを上に出す。編成制限で出せないキャラは暗く表示 |
| ボス | 定義の `IsBoss` の行で出た敵は、少し大きく（`UnitView` の拡大）・HPバーを画面上部に出す。名前の前に「ボス」 |

### Tests（`GimmickTests.cs`）

* 開始さかな・働きペンギン上限がかかる（上限レベルで `TryLevelUp` が失敗する）
* 編成制限: 制限に当たるキャラは `CanSpawn` が false
* 敵の砲: チャージ前は撃たない / 条件を満たすと撃ち、味方にダメージ
* なだれ: ステージ定義から設定したときに起きる（既存の `AvalancheTests` と同じ確かめ方）

## 実装メモ

* `StageDefinition` は項目が増えるので、全部の項目を引数に取るコンストラクタではなく、オブジェクト初期化子（`new StageDefinition { ... }`）で書けるようにする（定義表を読みやすくするため）
* 色は Battle が `UnityEngine.Color` を持てないので16進文字列で持ち、Unity 側で `ColorUtility.TryParseHtmlString` で変換する
* ギミックは「足す」だけで、既存のオンラインのステージ（`PenguinStageData`）には触らない
* ボスの拡大は表示だけ（当たり判定・射程は変えない）

## やらないこと

ステージのデータ量産（Phase 6）、ボス登場演出・音（Phase 7）。新しい種類の地形（穴・坂など）や天候は作らない。

## 完了条件

* [ ] EditMode テストが通る
* [ ] 仮ステージ3つにそれぞれ別のギミックを入れ、遊んで違いが分かる
* [ ] ステージ詳細に敵の顔と特別ルールが出る
* [ ] 編成制限のステージで、制限に当たるキャラのボタンが暗く出せない
* [ ] 敵の砲が撃たれ、味方がまとめて吹き飛ぶ

## ユーザー確認手順

1. `Rebuild PenguinWars` → 仮ステージ3つを遊ぶ
2. Phase 6 で作りたいステージのアイデア（「こういうギミックが欲しい」）があればこの時点で伝える。足すならこのフェーズで足す

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * Battle: `EnemyCannonSettings.cs`（敵の砲の数値） / `EnemyCannonAi.cs`（撃つ判断） / `BattleWorld.Gimmicks.cs`（陣営ごとの砲の数値・敵の砲を撃つ・編成制限の判定）
  * UI: `BossHpBar.cs`（画面上部のボスHPバー）
  * Editor: `PenguinWarsSceneBuilder.StageDetail.cs`（ステージ詳細を `.StageSelect.cs` から分けた。300行を超えるため）
  * Tests: `GimmickTests.cs`（15件）
  * 変更: `StageDefinition`（ギミック項目・`ApplyTo`・`DistinctEnemyNos`・`IsBossUnit`） / `BattleSettings`（`StartingFish`・`MaxWalletLevel`・`DeckMaxUnitCost`・`DeckBannedRoles`・`EnemyCannon`） / `WalletState`（上限レベル・開始さかな） / `UnitState.IsBoss` / `BattleWorld`（`CanSpawn` に編成制限、`Step` に敵の砲） / `BattleWorld.Combat`（砲の範囲・ダメージを陣営ごとに） / `DeckRules.IsAllowed` / `StageDefinitions.Chapter1`（仮ステージにギミック） / `BattleRunner`（`stage.ApplyTo`・色） / `StageLabels.Rules` / `StageDetailPanel`（敵の顔・特別ルール・制限に当たる編成を暗く） / `DeckIntroPanel`（ステージ名・特別ルール・制限に当たるキャラを暗く） / `UnitView`（ボスを 1.3 倍） / SceneBuilder の `.Hud.cs`（ボスHPバー）・`.Intro.cs`（ルールの行）
* 公開API:
  * `StageDefinition.ApplyTo(settings)` … 戦場の長さ・城HP・ギミックを `BattleSettings` に写す（検証ツールもこれを使えばよい）
  * `StageDefinition.DistinctEnemyNos()` / `IsBossUnit(no)` / `HasDeckRestriction`
  * `DeckRules.IsAllowed(stage, unitStats)` / `IsAllowed(stage, unitNo)`（定義表のコスト・役割で判定） / `IsAllowed(maxCost, bannedRoles, unitStats)`
  * `BattleWorld.IsAllowedByRules(side, slot)` … 左陣営だけ制限がかかる。`CanSpawn` もこれを見る
  * `EnemyCannonAi.ShouldFire(cannon, isReady, units, side, castleX, fieldLength)`
  * `UnitState.IsBoss`（表示専用）
  * `StageLabels.Rules(stage)` … 「なだれ ／ スタート時 さかな1000 ／ コスト300以下のみ」。ギミックが無ければ空文字
  * `DeckIntroPanel.Show(deck, title, rules, isAllowed)`
* `StageDefinition` の全項目と既定値:

  | 項目 | 既定値 | 意味 |
  | --- | --- | --- |
  | `Id` / `Chapter` / `Index` / `Name` / `Description` | なし | 「1-3」など。`Index` 6 がボスステージ |
  | `FieldLength` | 30 | 30 より長くしない（地面の絵） |
  | `PlayerCastleHp` / `EnemyCastleHp` | 3000 / 3000 | |
  | `Entries` | 空 | 敵の出方（`EnemySpawnEntry`） |
  | `TargetSeconds` | 0 | ★3 の目標タイム |
  | `SafeHpRatio` | 0.5 | ★2 の自城HP割合 |
  | `UnlockNos` | 空 | 初クリアで解放 |
  | `TintHex` | null（白） | 山と地面の色 `"#RRGGBB"`。読めなければ白＋警告 |
  | `AvalancheInterval` | 0（なし） | なだれの間隔。範囲 0.4〜0.6・予告5秒・ダメージ150 は `BattleSettings` の既定値のまま |
  | `StartingFish` | 0 | 最初のさかな。上限を超えてよい（超えている間は増えない・撃破報酬も入らない。使って下回ったらまた増える） |
  | `MaxWalletLevel` | 0（なし） | 働きペンギンの上限レベル。上限でボタンは「MAX」 |
  | `EnemyCannon` | null（撃たない） | `ChargeTime` 30 / `RangeRatio` 0.5 / `Damage` 150 / `MinTargets` 3 / `DangerRatio` 0.15 |
  | `MaxUnitCost` | 0（なし） | これより高いコストは出せない |
  | `BannedRoles` | 空 | この役割は出せない |

* 仮ステージのギミック（Phase 6 で作り直す）: `1-1` 開始さかな1000・水色 / `1-2` なだれ30秒・働きLv4まで・夕方色 / `1-3` コスト300以下のみ・敵の砲（`MinTargets` 4）・紫
* 計画から変えた点:
  * 敵の砲の「自城に近い味方がいる」は `DangerRatio`（敵の城から戦場のこの割合以内）で決める。N は `MinTargets`
  * 編成制限に当たる保存編成は **そのまま出撃させ、その枠だけ暗く出せない**（自動で外さない・出撃も止めない）。ステージ詳細の編成の帯でも暗く出すので、出撃前に気づける。編成画面（`DeckEditPanel`）は制限を知らない（どのステージ用でもない編成のため）
  * 開始さかな・上限レベルは `BattleSettings` で両陣営にかかる（ステージの敵はさかなを使わないので影響なし。対戦は 0 のまま）
  * ボスの「名前の前に『ボス』」は画面上部のHPバーの見出しに出す（頭上には名前を出していないため）
  * ステージ詳細の箱を高さ 1000 → 1040 にして、文字を少し小さくして詰めた
* 次フェーズへの注意:
  * 検証ツール（Phase 5）は `BattleSettings` を作って `stage.ApplyTo` → `new BattleWorld` → `SetEnemyScript` の順にすればギミック込みで再現できる（`BattleRunner.InitializeStage` と同じ）。`WalletTable` などは `PenguinWarsBalance` 相当の値を入れること（既定値は仕様書の初期案）
  * 敵の顔は最大8種類まで（9種類目からは出ない）
  * Unity 側のコードは前フェーズと同じ一時プロジェクトで `dotnet build`（Battle・Tests・Assembly-CSharp・Editor）が通り、EditMode テストは簡易ランナーで 145 件通過・1 件 Ignore（Phase 6 で外す網羅テスト）を確認した（エディタでの Rebuild・再生確認はまだ）
