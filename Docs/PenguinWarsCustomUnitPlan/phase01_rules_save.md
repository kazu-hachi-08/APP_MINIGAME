# Phase 1: じぶんペンギンのきまり・数値計算・セーブ（純C#）

## ゴール

じぶんペンギン1体の定義（役割・コスト・強化レベル・能力・見た目ID）から、既存キャラと同じ `UnitStats` が作れる。コストの段階で「触れる数値・強化ポイント・能力枠・範囲攻撃」が決まり、きまりに合わない定義は `Sanitize` で直せる。3枠のプリセットを JSON で保存・読み込みできる。画面はまだ作らない。

## 読むもの

* INDEX の「決めた前提」と「仕様」（コストの段階の表・プリセットの表）
* 既存コード: `UnitStatFormula.cs` / `UnitDefinition.cs` / `StatTweak.cs` / `UnitDefinitions.cs`（先頭の能力の定数とヘルパー） / `UnitAbility*.cs` / `CampaignProgress.cs`（JSON の書き方） / `MiniJson.cs` / `Scripts/Game/CampaignSave.cs`

## 作るもの

### Battle（純C#）

| ファイル | 内容 |
| --- | --- |
| `CustomStatLevels.cs` | 強化レベル5つ（`Hp` / `Attack` / `Range` / `Speed` / `Cooldown`。int）。`ToTweak()` で `StatTweak` に変換（レベル1つ＝10%。再生産は「+1 で ×0.9」と逆向き） |
| `CustomUnitDefinition.cs` | `Name` / `Role` / `Cost` / `Levels` / `IsAreaAttack` / `Abilities`（`List<UnitAbilityType>`） / 見た目ID 5つ（`Body` / `BodyColor` / `Head` / `Hand` / `Back`。Battle は `PenguinLook` を参照できないので文字列で持つ）。`Clone()`、`ToJsonObject()` / `FromJsonObject()` |
| `CustomUnitRules.cs` | 下の「公開API」。数値はすべてこのクラスの定数 |
| `CustomUnitPresets.cs` | 3枠（`Slots`）と `LastPickedSlot`（0〜2）。`CreateDefault()` は INDEX のお手本3体。`ToJson()` / `FromJson()`（`{"version":1,"lastPick":0,"slots":[{...},{...},{...}]}`）。読めない・壊れた・枠が足りない JSON はお手本で埋める |
| `UnitStatFormula.cs`（変更） | 役割のコスト帯を返す `CostRange(role, out min, out max)` を公開 |
| `UnitDefinitions.cs`（変更） | 能力の確率・時間の定数とヘルパー（`Knock` / `Freeze` …）を `internal` か `public` にして、じぶんペンギンでも同じ値を使う（2か所に同じ数を書かないため） |

**`CustomUnitRules` の公開API（予定）**

| API | 内容 |
| --- | --- |
| `LeftNo = 91` / `RightNo = 92` | 対戦でのキャラ No（INDEX「対戦の流れ」） |
| `NameMaxLength = 8` / `MinLevel = -2` / `MaxLevel = 2`（計画時は 3。引き継ぎメモ参照） / `LevelStep = 0.1f` | |
| `CostStep(role)` | 壁 10、ほか 50 |
| `Tier(role, cost)` | 1〜3（帯の位置 t で決める。INDEX の表） |
| `Points(tier)` / `AbilitySlots(tier)` / `CanUseArea(tier)` / `IsStatUnlocked(tier, stat)` | INDEX の表どおり |
| `SpentPoints(levels)` | レベルの合計（下げた分はマイナス） |
| `IsValid(def)` | コストが帯の中・刻みどおり、レベルが範囲内・ポイント以内・触れない数値は 0、能力が枠以内・重複なし、範囲は段階3のみ、名前の長さ |
| `Sanitize(def)` | 直した **コピー** を返す（INDEX「コストを下げて段階が下がったら」の順で直す。コストは帯の中・刻みに丸める。名前が空なら「じぶんペンギン」、長ければ切る）。見た目IDは触らない（Battle はパーツの一覧を知らないため。知らないパーツは絵の合成側が描かずに進む） |
| `ToUnitDefinition(def, no)` | `UnitDefinition`（名前・役割・コスト・範囲・能力・`With(levels.ToTweak())`）に変換。`UnitStatFormula.Calculate` に渡せば `UnitStats` |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `Scripts/Game/CustomUnitSave.cs` | `Load()` / `Save(presets)`。キー `PenguinWars.CustomUnits`。保存のたびに `PlayerPrefs.Save()`（`CampaignSave` と同じ書き方） |

### Tests（`CustomUnitRulesTests.cs` / `CustomUnitPresetsTests.cs`）

* 段階の境目（各役割の帯の下限・1/3・2/3・上限）
* `Sanitize`: 段階3 → 1 にコストを下げると、射程・速度・再生産のレベルが0、能力が外れ、範囲が切れる / ポイント超過は全レベル0 / コストの刻み・帯の外を丸める / 名前の長さ
* `IsValid(Sanitize(x))` が常に true（いろいろな壊れた定義で）
* お手本3体はそのまま `IsValid`
* `ToUnitDefinition` → `Calculate`: レベル0なら同じ役割・コストの既存の式の値と一致 / 体力+1 で体力が約 1.1 倍 / 再生産+1 で短くなる
* JSON の往復で中身が変わらない / 壊れた JSON・枠2つの JSON はお手本で埋まる
* **バランスの目安**: 各役割で「ポイントを全部体力と攻撃に振った最強の組み合わせ」の 体力×火力÷コスト² が、同じ役割の既存キャラの最大値の 1.3 倍以内（超えたらポイント・倍率を下げる。数値はユーザーと相談して決めてよい）

## 実装メモ

* `UnitAbilityType` → `UnitAbility`（確率・時間つき）への変換は `CustomUnitRules` に1か所。ふっとばすの確率だけ役割で変わる（妨害 50%・ほか 30%）
* 役割キラーなど「確率なし」の能力もそのまま選べる
* `CustomStatLevels` は「どの数値か」を enum（`CustomStat`）で引けるようにしておくと、Phase 4 の画面で5行を同じコードで回せる

## やらないこと

画面（Phase 3・4）、絵（Phase 2）、通信（Phase 5）。見た目IDの正しさのチェック（Phase 2 以降の Unity 側で行う）。

## 完了条件

* [x] EditMode テストが通る（既存のテストも全部）
* [x] お手本3体の `UnitStats`（体力・攻撃・射程・速度・再生産）を引き継ぎメモに表で書いた

## ユーザー確認手順

1. Test Runner で EditMode テストを流す
2. 引き継ぎメモのお手本3体の数値を見て、強すぎ・弱すぎと感じたら伝える（段階のポイント数・1レベルの倍率は `CustomUnitRules` の定数で変えられる）

## 引き継ぎメモ（実装後に記入）

* 作ったファイル（`Assets/_Project/Games/100_PenguinWars/` 以下）:
  * `Scripts/Battle/CustomStatLevels.cs`（`CustomStat` enum も同じファイル）
  * `Scripts/Battle/CustomUnitDefinition.cs` / `CustomUnitRules.cs` / `CustomUnitPresets.cs`
  * `Scripts/Game/CustomUnitSave.cs`
  * `Tests/Editor/CustomUnitRulesTests.cs` / `CustomUnitPresetsTests.cs`
  * 変更: `UnitStatFormula.cs`（`CostRange` を追加）、`UnitDefinitions.cs`（能力の確率・秒数の定数と `Knock` / `Freeze` / `Slow` などのヘルパーを `internal` にした）
* 公開API:
  * `CustomStatLevels`: `Hp` / `Attack` / `Range` / `Speed` / `Cooldown`、`Get(stat)` / `Set(stat, level)` / `Reset()` / `Clone()` / `ToTweak()`、`AllStats`（5行を回す用）
  * `CustomUnitDefinition`: `Name` / `Role` / `Cost` / `Levels` / `IsAreaAttack` / `Abilities` / `Body` / `BodyColor` / `Head` / `Hand` / `Back`（空文字 = パーツなし）、`Clone()`、`ToJson()` / `AppendJson(builder)` / `FromJson(string)`（壊れていたら null） / `FromJsonObject(dict)`
  * `CustomUnitRules`: 計画どおり（`LeftNo` / `RightNo` / `DefaultName` / `NameMaxLength` / `MinLevel` / `MaxLevel` / `LevelStep` / `CostStep` / `Tier` / `Points` / `AbilitySlots` / `CanUseArea` / `IsStatUnlocked` / `SpentPoints` / `IsValid` / `Sanitize` / `ToUnitDefinition`）。ほかに `ToAbility(type, role)`、`DefaultBody` / `DefaultBodyColor`、`MinTier` / `MaxTier` を追加
  * `CustomUnitPresets`: `SlotCount` / `Slots`（読み取り専用） / `SetSlot(i, def)`（入れるときに Sanitize） / `LastPickedSlot`（0〜2に丸める） / `CreateDefault()` / `CreateSample(i)` / `ToJson()` / `FromJson()`
  * `CustomUnitSave`: `Load()` / `Save(presets)`（キー `PenguinWars.CustomUnits`）
* お手本3体の数値（`UnitStatFormula.Calculate` の結果。かっこ内は同じ役割・コストの既存キャラ）:

  | 枠 | 名前 | 体力 | 攻撃 | 射程 | 速度 | 再生産 |
  | --- | --- | --- | --- | --- | --- | --- |
  | 1 | じぶんナイト（アタッカー 400） | 280 | 46 | 1.5 | 1.21 | 8 秒 |
  | 2 | じぶんアーチャー（遠距離 800） | 220 | 65 | 4.25 | 0.8 | 12.6 秒 |
  | 3 | じぶんまじん（大型 3000・範囲） | 2590 | 267 | 3.03 | 0.6 | 70 秒 |

  参考: No20 ドリル（400）250/42/1.5/1.1/8、No31 ロケット（800）220/54/4.25/0.8/14、No44 ロボ（3000）2160/267/2.75/0.6/70
* 計画から変えた点:
  * **`MaxLevel` を 3 → 2 にした**（ユーザーと相談して決定）。+3 のままだと、ほかの数値を -2 にしてポイントを作り「体力+3・攻撃+3」で体力×火力が素の値の 1.69倍になったため。+2 なら最大 1.44倍で、既存キャラの個別倍率の最大（ペンギンタワーの体力1.4倍）と同じくらい
  * **バランスの目安の比べる相手を変えた**: 「既存キャラの最大値の1.3倍」ではなく「役割の素の値（レベル0・能力なし・単体）の 1.5倍以内」（`Balance_BestHpAttackBuild_StaysNearFormulaValue`）。大型・妨害の既存キャラは全員が範囲攻撃か能力持ちでその分減額されているため、レベル0・単体のじぶんペンギンでも既存の最大値を 1.33倍超えてしまい、元の比べ方では判定できなかった
  * JSON の強化レベルは CustomStat の順の配列（`"levels":[体力,攻撃,射程,速度,再生産]`）、能力・役割は enum の番号で書く
  * `CustomUnitPresets.FromJson` は読んだ枠を `Sanitize` する（後のビルドできまりを変えても古いセーブがそのまま使えるように）
  * 名前は `Sanitize` で前後の空白も削る
* 次フェーズへの注意:
  * `new CustomUnitDefinition()` のコストは 0（帯の外）。画面で新しく作るときは `CustomUnitRules.Sanitize` を通すと役割の下限になる
  * 見た目IDはチェックしていない。知らないIDの扱いは Phase 2 の絵の合成側で決める
  * `UnitDefinitions` の能力ヘルパーは `internal`（Battle の中からだけ使える）。Battle の外では `CustomUnitRules.ToAbility` を使う
  * 新しい .cs の `.meta` は Unity を開いたときに作られる。コミットのときは `.meta` も一緒に入れる
  * Unity が開いたままだとバッチモードでテストを流せないので、AI は Battle と Tests を dotnet + NUnit でコンパイルして確かめた（220件通過）。Unity の Test Runner での確認はユーザーが行う
