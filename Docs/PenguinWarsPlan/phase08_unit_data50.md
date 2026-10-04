# Phase 8: 50体のデータ・数値の計算式・ランダム編成の制約

## ゴール

50体すべての `PenguinUnitData` が揃い、エンドレスは50体からランダム10体（壁2体以上）で編成される。見た目は Phase 6 の10体以外は「基本ペンギン＋体色だけ」の仮でよい。

## 読むもの

* 仕様書: §5.5 キャラ一覧（全表）、§2.1 ランダム編成のルール、§8.1（出現キャラの決め方）
* 前フェーズの引き継ぎメモ
* `Editor/PenguinUnitAssetGenerator.cs`

## 作るもの

| ファイル | 内容 |
| --- | --- |
| `Scripts/Data/UnitRole.cs` | 役割 enum: 壁 / アタッカー / 遠距離 / 妨害 / 大型 |
| `PenguinUnitData` | `Role` を追加 |
| `Editor/UnitStatFormula.cs` | 「役割 × コスト」から基本数値を出す計算式（下記）。個別調整は倍率で上書き |
| `Editor/UnitDefinitions.cs` | 50体の定義表（No・名前・役割・範囲・能力・コスト・個別倍率・Look）。**データを1か所に集める**（バランス調整がここだけで済むように） |
| `Editor/PenguinUnitAssetGenerator.cs` | 定義表から50体のアセットとカタログを生成・更新（既存アセットは上書き更新して GUID を保つ） |
| `DeckRandomizer` | 役割を見て「壁が最低2体」を満たすよう抽選 |
| `Tests/Editor/` | 抽選の制約テスト、全50体の数値が正（0やマイナスが無い）のテスト |

### 計算式（初期案。数値はここで決めてよい）

* コストを基準に「コスト1あたりの総合力」を揃える考え方
* 役割ごとの配分（例）

| 役割 | HP | 攻撃 | 射程 | 速度 | 再生産 |
| --- | --- | --- | --- | --- | --- |
| 壁 | 高 | 低 | 短 | 普 | 短（2〜4秒） |
| アタッカー | 中 | 高 | 短 | 普〜速 | 中（6〜10秒） |
| 遠距離 | 低 | 中 | 長 | 遅 | 中〜長 |
| 妨害 | 中 | 低 | 中 | 普 | 中〜長 |
| 大型 | 高 | 高 | 中 | 遅 | 長（40〜80秒） |

* 能力持ちは基本値を少し下げる（能力のぶん）

## 実装メモ

* Phase 5 で手入力した10体の数値は、式からの値に置き換わってよい（遊び心地が大きく変わったら倍率で寄せる）
* 仮の見た目: `Look` は体の形・体色だけ指定。パーツは Phase 9 で埋める
* アート生成メニューも50体分回るようにしておく（パーツ未定義は無視して基本体だけ描く）

## やらないこと

残りのパーツ・見た目（Phase 9）、本格的なバランス調整（Phase 12）。

## 完了条件

* [ ] 生成メニューで50体のアセットができる。再実行しても GUID が変わらない（コード上は同じパスに上書きするので変わらない。Unity での確認待ち）
* [x] EditMode テスト: 抽選に壁が2体以上・重複なし / 全キャラの数値が妥当範囲
* [ ] 再生して毎回違う10体で遊べる。敵にもいろいろなキャラが出る（再生での確認待ち）

## ユーザー確認手順

1. ユニット生成 → アート生成 → `Rebuild PenguinWars` → 何回か再生

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * `Scripts/Battle/UnitRole.cs`（役割 enum: Wall / Attacker / Ranged / Disruptor / Large）
  * `Scripts/Battle/StatTweak.cs`（個別倍率 Hp / Attack / Range / Speed / Cooldown。既定 1）
  * `Scripts/Battle/UnitDefinition.cs`（定義表の1行。`With(StatTweak)` で個別倍率を付ける）
  * `Scripts/Battle/UnitDefinitions.cs`（**全50体の定義表**。No・名前・役割・コスト・単体/範囲・能力・個別倍率。能力の確率・秒数の定数もここ）
  * `Scripts/Battle/UnitStatFormula.cs`（役割ごとの RoleProfile〔コスト帯・コスト当たり体力/火力・射程・速度・攻撃間隔・発生・ノックバック回数〕から `Calculate(UnitDefinition)` で UnitStats を出す。範囲攻撃は火力×0.75、能力1つごとに体力・火力×0.9）
  * `Editor/UnitLooks.cs`（全キャラの見た目 `Get(no)`。旧 `PenguinUnitAssetGenerator.Looks` をここへ移した）
  * `Tests/Editor/UnitRosterTests.cs`（50体揃っている・コストが役割の帯の中・数値が正で発生＜攻撃間隔・抽選の壁2体以上/重複なし/シードで変わる・大型が役割から選ばれる）
  * 変更: `UnitStats`（`Role` 追加）、`PenguinUnitData`（`_role` 追加・`ToStats` で渡す）、`DeckRandomizer`（`PickDeck(pool, count, minWalls, random)` 追加）、`EnemyWaveDirector`（5の倍数レベルの確定出現を「役割が大型のキャラからランダム」に。大型がいないプールでは最もコストの高いキャラ）、`PenguinWarsBalance`（`_deckMinWalls = 2`）、`BattleRunner`（`PickDeck` を使う）、`PenguinUnitAssetGenerator`（定義表＋計算式＋UnitLooks から50体を書き込む）
* 公開API（次フェーズが使うもの）:
  * `UnitDefinitions.All` / `UnitStatFormula.Calculate` / `UnitStatFormula.IsCostInRoleRange`
  * `DeckRandomizer.PickDeck`（Phase 10 の対戦のランダム編成もこれでよい）
  * `UnitLooks.Get(no)`（Phase 9 はここの辞書にパーツを足す）
  * `PenguinUnitData.Role`
* 計画・仕様から変えた点:
  * `UnitRole`・計算式・定義表は計画では Data / Editor だったが、**純C#の Battle に置いた**。テスト asmdef は Editor フォルダのコード（Assembly-CSharp-Editor）を参照できず、「全50体の数値が正」のテストを書けないため。また `DeckRandomizer`（Battle）が役割を見る必要がある
  * 見た目は定義表に入れず `Editor/UnitLooks.cs` に分けた（`PenguinLook` は Unity の型で Battle から使えないため。数値と見た目のファイルが分かれるので、2人で別々に触ってもぶつからない）
  * 生成メニューは **既存アセットも毎回上書き** する（Phase 5 までは数値を上書きしなかった）。調整は定義表で行い、アセットを Inspector で直しても次の生成・Rebuild で戻る。スプライト欄は触らない
  * おのペンギンは Phase 5 では範囲だったが、仕様書 §5.5 に合わせて単体にした
  * 大型の火力は計画の例より上げた（コスト当たり火力 0.04。0.025 だとコスト600のアタッカーと同じくらいの火力しかなかったため）
  * 仮の見た目: 体色（こおり・ひな・ふぶき・じょおう・ひょうざん等）、横長/縦長（すもう・ムキムキ・ながあし・タワー）、大型は拡大率2（きょだいは3）
* 次フェーズへの注意:
  * テストは Battle と Tests の .cs を集めた一時プロジェクトで `dotnet test` し 50件合格。Unity 側は生成済み csproj を複製して新ファイルを足した一時プロジェクトで `dotnet build` しエラー0（Unity での生成・再生は未確認）
  * 新しいファイルの .meta は Unity が作る
  * 見た目を変えた既存キャラ（No.2〜50 のうち UnitLooks を変えたもの）は、Rebuild だけでは絵が作り直されないことがある（足りない絵があるときだけ全体を作るため）。Phase 9 でも `Generate PenguinWars Art` を実行してもらう
  * 状態異常の秒数・確率は `UnitDefinitions` の定数。役割ごとの基準値は `UnitStatFormula.Profiles`
