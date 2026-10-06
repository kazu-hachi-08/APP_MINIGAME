# Phase 3: 作成画面（3枠・名前・見た目パーツ・プレビュー・保存）

## ゴール

ペンギン大戦争のタイトルに「じぶんペンギン」ボタンがあり、作成画面で3枠を切り替えて、名前と見た目（体の形・体色・頭・手・背中）を変えられる。大きなプレビューが歩き・攻撃を繰り返し、「ほぞん」で保存される。強さの欄（のうりょくタブ）は Phase 4 で作るので、このフェーズでは役割・コスト・数値を **表示だけ** する。

## 読むもの

* INDEX の「決めた前提」「仕様（プリセット）」
* Phase 2 の引き継ぎメモ
* 仕様書: §2.0 タイトル（ずかん・設定の開き方）、§7.2 パーツの表
* 既存コード: `PenguinWarsTitlePanel.cs` / `ZukanDetailPanel.cs`（大きい絵のコマ送り `UnitSpriteAnimator`） / `DeckEditPanel.cs`（「けってい」「もどる」の扱い） / `UnitLabels.cs`、`Editor/PenguinWarsSceneBuilder.Title.cs` / `.Zukan.cs` / `.Deck.cs`
* `Docs/USER_NAME_PLAN.md`（名前入力の作り方）

## 画面

```text
┌──────────────────────────────────────────────┐
│ じぶんペンギン      [ 1 ][ 2 ][ 3 ]   ★対戦の前に3つから選べます │
│ ┌──────────┐  [ みため ][ のうりょく ]                 │
│ │          │  名前: [じぶんナイト      ]                │
│ │  大きい絵  │  体の形   ◀  ふつう      ▶                │
│ │ 歩く→攻撃  │  体の色   ◀  くろしろ    ▶                │
│ │          │  あたま   ◀  ヘルメット  ▶                │
│ └──────────┘  て       ◀  けんとたて  ▶                │
│ アタッカー コスト400   せなか   ◀  あおマント  ▶                │
│ 体力 380 攻撃 52 …     [ おまかせ ]                          │
│                    [ もどる ]            [ ほぞん ]            │
└──────────────────────────────────────────────┘
```

* 枠ボタン（1〜3）で切り替え。切り替え・もどるで未保存の変更があれば「ほぞんしますか？（ほぞん / すてる）」を出す
* 「ほぞん」で `CustomUnitSave.Save`。「もどる」でタイトルへ
* プレビューは「歩く（1.5秒）→ 振りかぶる → 攻撃」を繰り返す（`ZukanDetailPanel` と同じ見せ方）。青（左陣営）の絵
* パーツ行は ◀▶ で順に回す。頭・手・背中は先頭に「なし」を入れる。表示名は日本語（`PartLabels`）
* 「おまかせ」は5部位をランダムに選ぶ（名前・強さは変えない）
* 名前は最大8文字。空なら保存時に「じぶんペンギン」
* 左下の役割・コスト・数値は Phase 1 の計算結果をそのまま表示（このフェーズでは変えられない）

## 作るもの

| ファイル | 内容 |
| --- | --- |
| `Scripts/UI/CustomUnitPanel.cs` | 画面全体。枠の切り替え・タブ・保存・未保存の確認・プレビュー更新。編集中は `CustomUnitDefinition` のコピーを持つ |
| `Scripts/UI/CustomLookTab.cs` | みためタブ（名前入力・5行の◀▶・おまかせ）。値が変わったら `Changed` イベント |
| `Scripts/UI/CustomPartRow.cs` | ◀ 名前 ▶ の1行（5行で使い回す） |
| `Scripts/UI/PartLabels.cs` | パーツID → 日本語名（例: `sword_shield` → けんとたて）。無い ID は ID のまま出す（パーツを足したとき画面が壊れないように） |
| `Scripts/UI/CustomUnitPreview.cs` | `CustomUnitFactory` で作ったデータの絵をコマ送り。作り直すたびに前のものを片付ける |
| `PenguinWarsTitlePanel`（変更） | 「じぶんペンギン」ボタンを足す（ずかんの隣） |
| `Editor/PenguinWarsSceneBuilder.Custom.cs` | 画面の生成。タブの中身は `CustomLookTab` / `CustomStatsTab`（Phase 4）を別の親オブジェクトに分けておく |

## 実装メモ

* プレビューはカタログに登録しなくてよい（`CustomUnitFactory` の戻り値をそのまま使う）。登録が要るのは対戦だけ
* 未保存の確認は `UIDialogBuilder`（共通）で作れればそれで。無ければ同じパネル内に小さい確認を作る（Common は変えない）
* 枠の数字ボタンには、その枠のじぶんペンギンの小さいアイコンを出すと選びやすい（余裕があれば）
* スマホで押しやすいよう ◀▶ は大きめに

## やらないこと

のうりょくタブの中身（Phase 4）、対戦での選択（Phase 5）、色の自由指定（体色は既存の4色から選ぶだけ）、パーツの追加。

## 完了条件

* [ ] タイトル →「じぶんペンギン」で画面が開く。初回はお手本3体が入っている
* [ ] パーツを変えるとプレビューがすぐ変わる。大型（じぶんまじん）は大きく出る
* [ ] 「ほぞん」して再生を止め、再開しても残っている
* [ ] 保存しないで枠を切り替えると確認が出る

## ユーザー確認手順

1. `Rebuild PenguinWars` → 再生 → タイトルの「じぶんペンギン」
2. 3枠それぞれ見た目を変えて保存 → 再生し直して残っているか
3. パーツの日本語名で分かりにくいものがあれば伝える

## 引き継ぎメモ（実装後に記入）

* 作ったファイル（`Assets/_Project/Games/100_PenguinWars/` 以下）:
  * 新規: `Scripts/UI/CustomUnitPanel.cs` / `CustomLookTab.cs` / `CustomPartRow.cs` / `CustomUnitPreview.cs` / `PartLabels.cs`、`Editor/PenguinWarsSceneBuilder.Custom.cs`
  * 変更: `PenguinWarsTitlePanel`（`_customButton` / `_customPanel`）、`Editor/PenguinWarsSceneBuilder.Title.cs`（下のボタンを5つに。幅 380→350、`じぶんペンギン` はずかんの右隣）、`UnitSpriteAnimator`（`WalkAndAttackFrames` を追加）、`ZukanDetailPanel`（並びを `WalkAndAttackFrames` に置き換え・`FormatStats` を public に）
* 公開API:
  * `CustomUnitPanel.Show()` / `Hide()`（開くたびに `CustomUnitSave.Load()` し直す。最初は枠1）
  * `CustomLookTab.Bind(def)`（def を直接書き換える）・イベント `LookChanged` / `NameChanged`
  * `CustomPartRow`: `Slot` / `Value` / `SetValue(id)` / `Randomize()` / イベント `Changed`（◀▶ のときだけ）
  * `CustomUnitPreview.Show(def) → PenguinUnitData`（中で `Sanitize` → `CustomUnitFactory.Create(…, LeftNo)`。前のデータは `DestroyRuntime`）/ `Release()`
  * `PartLabels.Slot(slot)` / `Part(slot, id)`（`aurora` は体色と背中の両方にあるので部位ごとに引く。知らない ID は ID のまま）
  * `UnitSpriteAnimator.WalkAndAttackFrames`、`ZukanDetailPanel.FormatStats(stats)`
* 計画から変えた点:
  * `CustomLookTab` のイベントは `Changed` 1つでなく `LookChanged` / `NameChanged` の2つ（名前を1文字打つたびに絵を作り直さないため）
  * のうりょくタブは「のうりょくは じゅんびちゅう」の文字だけの仮（`StatsTab` オブジェクト。`CustomUnitPanel._statsTabRoot`）
  * 「ほぞん」は変更が無いと押せない（保存済みかが見て分かるように）。保存すると `Sanitize` 後の内容（空の名前 → じぶんペンギン）を画面に戻す
  * 未保存の確認は共通の `UIManager.ShowConfirmDialog`（ほぞん / すてる）。Common は変更していない
  * 大型の大きさ: UI の Image は PPU を見ないので、プレビューの絵の枠を `基準 230 × 拡大率` にして足元から上へ伸ばしている
  * 名前入力はブラウザ版だけ共通の `WebNamePrompt`（window.prompt）を使う（ユーザー名入力と同じ作り）
  * 枠ボタンの小さいアイコンは作っていない（枠の番号と色だけ）
  * AI は Unity の生成 csproj を一時的にコピー・直して dotnet でコンパイルが通ることだけ確かめた（エラー0。新しいファイルの警告0）
* 次フェーズへの注意:
  * のうりょくタブは `CustomStatsTab` を `StatsTab` の下に作り、`CustomUnitPanel` から `Bind(_editing)` と変更イベントを受けるようにする。変更時は `RefreshPreview()`（数値の表示も一緒に更新される）と `MarkDirty()` を呼べばよい。役割を変えると大型かどうかで絵の大きさが変わるので、役割の変更でも `RefreshPreview()` が要る
  * `SceneBuilder.Custom.cs` の `CreateCustomStatsTabPlaceholder` を置き換える。タブの領域は `CustomTabRootSize`（1000×640）
  * 確認ダイアログを出している間に枠を切り替えると、その後の流れは「ほぞん → 次の枠」「すてる → 次の枠」。どちらも `LoadSlot` で編集中のコピーを作り直す
  * Phase 2 の一時ファイル `Editor/CustomUnitDebugMenu.cs` はまだ残っている（未追跡）。確認が済んだら消す
