# Phase 5: ノックバック・特殊能力5種・キャラ10体

## ゴール

ユニットがノックバックし、5種の特殊能力が働く。キャラが10体になり、エンドレスはランダム10体編成で遊べる。

## 読むもの

* 仕様書: §5.3 特殊能力、§5.4 ノックバック、§5.5 キャラ一覧（下の10体の行だけ）
* 前フェーズの引き継ぎメモ

## 作るもの

### Battle

| ファイル | 内容 |
| --- | --- |
| `UnitAbility.cs` | 能力の種類 enum と パラメータ（確率・秒数）。`UnitStats` に能力リストと `KnockbackCount` を追加 |
| `UnitStatusEffects.cs` | 1体が受けている「止まる」「遅い」の残り時間。重ねがけは時間の上書き（長い方） |
| `KnockbackRule.cs` | HP しきい値をまたいだらノックバック（後ろに 1.5、0.5秒行動不能、攻撃キャンセル）。ふんばるは無効 |
| `AbilityResolver.cs` | ヒット時に ふっとばす / 止める / 遅くする の確率判定（`BattleWorld` の `System.Random` を使う）。城キラーは城へのダメージ×3 |
| `CannonState` | 効果にノックバック1回分を追加 |
| `BattleEvent` | `Knockback` / `StatusApplied` を追加 |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `PenguinUnitData` | 能力・ノックバック回数の項目を追加 |
| `Editor/PenguinUnitAssetGenerator.cs` | 10体分に拡張（下表） |
| `UnitView` | ノックバック中は後ろに跳ねる（仮: 位置補間のみ）、止まる=青っぽく、遅い=灰色っぽく色を変える |

### このフェーズの10体（役割と能力が一通り揃う組み合わせ）

| No | 名前 | 役割 | 能力 |
| --- | --- | --- | --- |
| 1 | ペンギン | 壁 | — |
| 2 | かべペンギン | 壁 | — |
| 4 | ヘルメットペンギン | 壁 | ふんばる |
| 11 | おのペンギン | アタッカー | — |
| 13 | ボクサーペンギン | アタッカー | ふっとばす |
| 19 | バイクペンギン | アタッカー | 城キラー |
| 23 | ゆみペンギン | 遠距離 | — |
| 26 | のっぽペンギン | 遠距離 | — |
| 34 | れいとうペンギン | 妨害 | 止める |
| 43 | きょだいペンギン | 大型 | ふんばる |

数値はこの時点では手で入れてよい（Phase 8 で計算式に置き換える）。

## 実装メモ

* 止められている間は移動も攻撃タイマーも止まる。遅いは移動速度だけ半分
* ノックバックで射程外に出たら、復帰後にまた前進から
* `DeckRandomizer` を10体から10体抽選に（＝全員。並び順だけランダム）

## やらないこと

ドット絵（Phase 6）、50体（Phase 8）。

## 完了条件

* [x] EditMode テスト: しきい値ごとにノックバック / ふんばるはノックバックしない / 止める中は動かない / 遅くする中は速度半分 / 城キラーは城だけ3倍 / 確率0%・100%で期待通り
* [ ] 再生してノックバックや状態異常が見て分かる

## ユーザー確認手順

1. ユニット生成メニュー → `Rebuild PenguinWars` → 再生
2. Test Runner で全テスト実行

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * `Scripts/Battle/`: `UnitAbility`（`UnitAbilityType` enum ＋ 確率・秒数）/ `UnitStatusEffects`（`UnitStatusType` enum も同居）/ `KnockbackRule` / `AbilityResolver` / `BattleWorld.Status.cs`（ノックバック開始・進行、ヒット時の能力判定）
  * 変更: `UnitStats`（`KnockbackCount` 既定1・`Abilities`・`HasAbility` / `TryGetAbility`）、`UnitState`（`UnitAction.Knockback`・`Status`）、`BattleEvent`（`Knockback` / `StatusApplied`）、`BattleSettings`（ノックバック距離・時間、遅くする倍率、城キラー倍率、`RandomSeed`）、`BattleWorld`（`System.Random` を持つ）、`BattleWorld.Combat.cs`（止まる中は何もしない・遅い中は速度×倍率・城キラー・砲でノックバック・HPしきい値でノックバック）
  * `Scripts/Data/PenguinAbilityEntry.cs`（インスペクタ用の能力1つ分。`UnitAbility` は readonly struct でシリアライズできないため）、`PenguinUnitData`（`_knockbackCount` 既定3・`_abilities`）、`PenguinWarsBalance`（ノックバック・能力の数値）
  * `BattleRunner`（新しい設定を渡す・乱数シード）、`UnitView`（止まる=水色・遅い=灰色に寄せる。止まるを優先）
  * `Editor/PenguinUnitAssetGenerator.cs`（10体。`UnitDef` にノックバック回数と能力を追加）
  * `Tests/Editor/AbilityTests.cs`（13件）
* 公開API（次フェーズが使うもの）:
  * `UnitState.Action == UnitAction.Knockback` / `UnitState.Status.IsFrozen` / `IsSlowed`（Phase 6 のアニメ「ノックバック1コマ」、Phase 10 の同期で状態として送る）
  * `BattleEvent.Knockback`（やられた側・位置）、`BattleEvent.StatusApplied`（`Amount` = `(int)UnitStatusType`）→ Phase 7 の演出・音
  * `UnitStats.HasAbility(type)` / `TryGetAbility(type, out ability)`（Phase 8 の編成制約・Phase 11 のカード表示で能力名を出すときに使える）
* 計画・仕様から変えた点:
  * ノックバックは「ロジックが 0.5 秒かけて 1.5 を等速で戻す」方式。View は X を写すだけで跳ねて見える（仮表現。Y方向のジャンプは無し）。オンラインでも X を送るだけで済む
  * 飛ばされている最中の追加ノックバックは無視。自城より後ろには下がらない
  * ふんばるは HPしきい値・ふっとばす・ペンギン砲のすべてで飛ばされない
  * HP 0 のときはノックバックせずその場で倒れる（仕様 §5.4 のとおり。しきい値に HP 0 は含めない）
  * 1回の攻撃で複数のしきい値をまたいでもノックバックは1回
  * 止まる中は攻撃発生待ち・攻撃後の硬直のタイマーも止まる。状態異常の時間はノックバック中も減る
  * 能力の数値（仮）: ボクサー ふっとばす30%、れいとう 止める50%・2秒。ヘルメット・きょだいはふんばる（ノックバック回数1）
  * `DeckRandomizer` は変更不要だった（プール数 ≦ 人数なら全員を並べ替えるので、10体から10体＝並び順だけランダム）
* 次フェーズへの注意:
  * テストは `dotnet test`（Battle と Tests の .cs を集めた一時プロジェクト）で 41件合格。Unity 側は生成済み csproj を複製して PenguinWars のファイルを足した一時プロジェクトで `dotnet build` し、エラー0を確認（Unity エディタでのコンパイル・Rebuild・再生は未確認）
  * キャラアセットは「既にあれば上書きしない」ので、Phase 4 までに作られた `Unit_001/011/023` はそのまま残る。ノックバック回数は初期値の3になる（ゆみの表の値は2）。表どおりにしたければ `Data/Units/` のその3つを消して `Generate PenguinWars Units` し直す
  * 状態異常の色は `UnitView` のインスペクタで調整できる（Phase 7 で演出に置き換える想定）
