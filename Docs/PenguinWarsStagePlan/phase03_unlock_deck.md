# Phase 3: キャラ解放・編成画面・ずかんのロック表示

## ゴール

最初は10体だけ使える。ステージを初めてクリアすると決まったキャラが仲間になり、リザルトで知らされる。ステージ選択から編成画面を開き、解放済みのキャラから10体を選べる。編成は保存され、次のステージでもそのまま使われる。

## 読むもの

* INDEX の「決めた前提」（解放・編成・オンラインは全50体のまま）
* 前フェーズの引き継ぎメモ
* 仕様書: §2.0 タイトル（ずかん）、§2.1 の「コストの低い順に並べる」
* 既存コード: `DeckRandomizer.cs` / `PenguinZukanPanel.cs`・`ZukanCell.cs`・`ZukanListOptions.cs` / `UnitButtonBar.cs` / `DraftCard.cs`（キャラの見た目＋情報のカードの作り方）

## 作るもの

### Battle（純C#）

| ファイル | 内容 |
| --- | --- |
| `CampaignUnlocks.cs` | 初期解放10体の No（定数配列）。`IsUnlocked(progress, no)`＝初期10体 or クリア済みステージの `UnlockNos` に含まれる。解放状態はセーブに持たず、クリア状況から毎回計算する（定義表で解放先を変えても矛盾しないため） |
| `CampaignProgress` | 最後に使った編成（No 10個）を保存。`Record` の戻り値に「このクリアで新しく解放されたキャラ」を追加 |
| `DeckRules.cs` | `IsValid(deckNos, unlocked)`: ちょうど10体・重複なし・全員解放済み。足りない時の補完 `FillDefault`（解放済みからコストの低い順に足す） |
| `StageDefinitions.Chapter1.cs` | 仮ステージ3つに `UnlockNos` を2体ずつ入れる |

初期10体の候補: 各役割（壁・アタッカー・遠距離・妨害）から安いキャラを2〜3体ずつ。大型は含めない（大型はボスステージの報酬にする）。具体的な No は `UnitDefinitions` を見て決め、引き継ぎメモに書く。

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `Scripts/UI/DeckEditPanel.cs` | 上に編成10枠（5×2。プレイ中のボタンと同じ並び）、下に解放済みキャラの一覧（ずかんと同じマス・並べ替え）。一覧を押すと空いている枠へ、枠を押すと外す。未解放キャラは一覧に出さない。「けってい」で保存。10体そろわないと決定できない |
| ステージ選択 | 画面の隅に「へんせい」ボタン。`StageDetailPanel` にも今の編成10体の小さいアイコンを出し、押すと編成画面へ |
| 編成発表 | ランダム編成をやめ、保存した編成をコストの低い順に並べて使う（並べ方は今までどおり） |
| リザルト | 新しく仲間になったキャラを「なかまになった！」として1体ずつ絵と名前で出す（演出は Phase 7。このフェーズは並べるだけ） |
| ずかん | 未解放キャラは黒いシルエット＋「？？？」。詳細は開けない（もしくは「ステージ 2-3 でなかまになる」とだけ出す）。オンラインのドラフトには影響させない |
| SceneBuilder | `PenguinWarsSceneBuilder.Deck.cs` を新規 |

### Tests（`UnlockTests.cs` / `DeckRulesTests.cs`）

* 初期10体は解放済み・他は未解放 / ステージをクリアするとその `UnlockNos` が解放される
* 初期10体と全ステージの `UnlockNos` を合わせると、No 1〜50 がちょうど1回ずつ出てくる（Phase 6 で全ステージがそろうまでは「重複がない」だけ確かめ、全員そろう方は Phase 6 で有効にする）
* `DeckRules.IsValid` の境界 / 保存された編成に未解放のキャラが混ざっていたら（定義表の変更などで）外して補完される

## 実装メモ

* 編成画面とずかんのマスの見た目は共通にしたい。`ZukanCell` を使い回せるならそれで、無理なら似た小さいクラスを作る（大きな共通化はしない）
* 一覧はスマホで押しやすい大きさにする（ずかんの8列より少ない6列を目安に）
* 解放の判定をセーブに持たないので、データを入れ替えたときに「解放済みだったキャラが消える」ことはあり得る。開発中はそれで構わない

## やらないこと

編成制限（コスト上限・役割禁止。Phase 4）、解放演出（Phase 7）、キャラのレベルアップ（スコープ外）。

## 完了条件

* [ ] EditMode テストが通る
* [ ] セーブを消して始めると、編成画面に10体しか出ない
* [ ] `1-1` を初クリアするとリザルトに新しい仲間が出て、編成画面に増えている。2回目のクリアでは出ない
* [ ] 編成を変えて再生を止め、再開しても編成が残っている
* [ ] ずかんで未解放キャラがシルエットになる。オンラインのドラフトでは全50体から出る

## ユーザー確認手順

1. `Edit > Clear All PlayerPrefs` → 再生 → 編成画面を開いて10体だけか確認
2. `1-1` をクリアして解放 → 編成に入れて `1-2` を遊ぶ
3. 初期10体の顔ぶれで `1-1` が楽しいかを見て、変えたければ伝える

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * Battle: `CampaignUnlocks.cs`（初期10体・解放判定） / `DeckRules.cs`（編成のきまり・補完・並べ替え）
  * UI: `DeckEditPanel.cs`
  * Editor: `PenguinWarsSceneBuilder.Deck.cs`
  * Tests: `UnlockTests.cs` / `DeckRulesTests.cs`
  * 変更: `CampaignProgress`（`LastDeckNos`・`StageRecordChange.NewUnlockNos`・JSON に `"deck":[…]`） / `StageDefinitions.Chapter1`（仮の `UnlockNos`） / `ZukanCell`（`SetLocked` / `SetDimmed`。編成画面でも使う） / `UnitSpriteAnimator.SetTint` / `PenguinZukanPanel`（開くたびにセーブを読んでシルエット） / `StageSelectPanel`（「へんせい」ボタン・編成画面） / `StageDetailPanel`（今の編成10体の帯。押すと編成画面） / `StageResultPanel`（右側に「なかまになった！」最大4体） / `PenguinWarsGameManager.Stage`（保存した編成で出撃） / SceneBuilder の `.StageSelect.cs`・`.Zukan.cs`（スクロール生成を列数・上余白の引数付きにした）
* 公開API:
  * `CampaignUnlocks.InitialNos` / `IsUnlocked(progress, no)` / `UnlockedNos(progress)`（No 順） / `FindUnlockStage(no)`（初期・未割り当ては null。ずかんのヒントや Phase 6 の割り振り確認用）
  * `DeckRules.DeckSize`（=10） / `IsValid(deck, unlocked)` / `FillDefault(deck, unlocked)`（未解放・重複・定義表に無い No を外して、解放済みからコストの低い順に補完） / `ByCost(nos)` / `CurrentDeck(progress)`（出撃に使う編成。保存→補完→コスト順）
  * `CampaignProgress.LastDeckNos`（未決定なら空） / `Record(...)` の戻り値 `NewUnlockNos`（初クリア時、記録前に未解放だったものだけ）
  * `DeckEditPanel.Show(progress, onClosed)`（「けってい」で `CampaignSave.Save` まで行う。「もどる」は変更を捨てる）
  * `StageResultPanel.Show(title, stars, detail, unlockNos, onNext, onRetry, onSelect)`
* 初期10体の No: 1 ペンギン / 3 ゆきだま / 6 ひな（壁3） / 11 おの / 12 さかなけん / 17 にんじゃ（アタッカー3） / 23 ゆみ / 25 つりざお（遠距離2） / 37 ねばねば / 38 ハリセン（妨害2）。大型なし
* 仮ステージの解放: `1-1` → 2 かべ・13 ボクサー / `1-2` → 24 ゆきなげ・41 ふうせん / `1-3` → 7 ダンボール・14 すもう（Phase 6 で作り直す）
* 計画から変えた点:
  * ずかんの未解放キャラは「詳細を開けない」ほうにした（ヒント文は出さない。`FindUnlockStage` は用意済みなので、出したくなったら `ZukanCell` に足すだけ）。左上の数値も「？？？」にする（並べ替えは本当の値で並ぶ）
  * 編成画面の一覧は「並べ替え（ドロップダウン）だけ」。ずかんの昇順/降順・特性しぼりこみは付けていない（解放済みは最大40体で、探すのに困らないため）
  * 一覧のマスをもう一度押すと枠から外れる（枠を押しても外れる）。枠は常にコストの低い順に詰める
  * 「もどる」は保存しない（9体の編成が保存されて、次の出撃で勝手に補完されるのを避けるため）
  * 編成をまだ決めていない・保存した編成が使えないときは `DeckRules.CurrentDeck` が自動で補う（初回は初期10体そのまま）
  * `PenguinWarsBalance` の `_deckMinWalls` とランダム編成（`BattleRunner.PickRandomDeckNos`）を削除。`DeckRandomizer.PickDeck` は既存テストだけが使っているので残した
  * 編成の人数は `DeckRules.DeckSize`（定数 10）。`PenguinWarsBalance.DeckSize` はドラフト用に残した（同じ値にしておく）
* 次フェーズへの注意:
  * 編成制限（Phase 4）は `DeckRules.IsValid` にステージを渡す形で足すのが自然。`DeckEditPanel` は今は `IsValid(_deck, _unlockedNos)` だけで「けってい」を判定している。出撃前に制限を満たさない保存編成をどうするか（自動で外す／出撃させない）は決めておく
  * 敵の紹介（Phase 4）は `StageDetailPanel` の「しゅつげき」→ `StartStage` の間に挟む。詳細パネルの高さは 1000 で、もう縦に余裕はない
  * 解放演出（Phase 7）は `StageResultPanel.ShowUnlocks` が並べているだけ。1ステージ5体以上の解放は表示が4体で切れる（Phase 6 の割り振りで超えないこと）
  * 全50体の網羅テスト `InitialAndStageUnlocks_CoverAllUnitsExactlyOnce` は `[Ignore]` 中。Phase 6 で外す
  * Unity 側のコードは、Unity 生成の csproj から参照を写した一時プロジェクトで `dotnet build` し、EditMode テストは反射で NUnit を呼ぶ簡易ランナーで 130 件通過を確認した（エディタでの Rebuild・再生確認はまだ）
