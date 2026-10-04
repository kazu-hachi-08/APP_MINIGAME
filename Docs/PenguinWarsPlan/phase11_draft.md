# Phase 11: ドラフト

## ゴール

オンライン対戦の開始前に、各自に提示された3体から1体を選ぶのを10回繰り返して編成を作る。終わったらお互いの10体を見せ合ってから試合開始。

## 読むもの

* 仕様書: §6 ドラフト、§2.2 オンライン対戦
* Phase 10 の引き継ぎメモ（メッセージの送り方・コマンドの送り先）
* `Scripts/Online/PenguinWarsOnlineLink.cs`

## 作るもの

### Battle（純C#。通信を知らない）

| ファイル | 内容 |
| --- | --- |
| `DraftSession.cs` | ラウンド管理・提示の抽選（自分が取ったキャラは出さない・相手との被りは可）・選択の受付・時間切れでランダム・両者選び終わったら次ラウンド・10体揃ったら完了 |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `Scripts/UI/DraftPanel.cs` | 中央に3枚のカード（見た目・名前・コスト・役割・能力）、上にラウンドと残り秒、下に取ったキャラ一覧 |
| `Scripts/UI/DeckRevealPanel.cs` | 編成確認（お互いの10体、3秒） |
| `PenguinWarsOnlineLink` | ドラフト用メッセージ: 提示（ホスト→ゲスト）・選択（ゲスト→ホスト）・ラウンド進行・完了（すべて再送あり） |
| `PenguinWarsGameManager` | オンライン時: 接続 → `Draft` → 編成確認 → `Intro` → `Playing` |

## 実装メモ

* 抽選・時間計測はホストだけ。ゲストの残り秒表示はホストから届いたラウンド開始時刻を基準に自分で減らす（毎秒送らない）
* 相手の選択はドラフト中に見せない
* カード表示は `UnitButton` のアイコンと同じスプライトを使う
* 能力の表示名は仕様 §5.3 の名前（ふっとばす 等）

## やらないこと

ソロでのドラフト（仕様ではソロはランダム）。

## 完了条件

* [x] EditMode テスト: 10ラウンドで10体 / 自分の取ったキャラが再提示されない / 時間切れで自動選択 / 片方だけ選んだ状態では次に進まない（`DraftTests`。dotnet で全68件合格）
* [ ] 2台でドラフト → 編成確認 → 試合が通しでできる。試合でドラフトした10体が使われる

## ユーザー確認手順

1. `Rebuild PenguinWars` → Phase 10 と同じ方法で2台接続して通しで確認

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * Battle: `DraftSession.cs`（ラウンド・候補の抽選・選択・時間切れの自動選択。陣営はホスト基準 Left = ホスト / Right = ゲスト）
  * Unity: `Game/PenguinWarsGameManager.Draft.cs`（partial。ドラフトの進行と編成確認）/ `UI/DraftPanel.cs` / `UI/DraftCard.cs` / `UI/DeckRevealPanel.cs` / `UI/UnitLabels.cs`（役割・能力の表示名）/ `Editor/PenguinWarsSceneBuilder.Draft.cs`
  * テスト: `Tests/Editor/DraftTests.cs`
  * 変更: `BattleRunner.InitializeVersusHost(leftDeckNos, rightDeckNos)`（ランダム編成をやめてドラフト結果を受け取る）・`CollectAllUnitNos()` 追加、`PenguinWarsBalance.DraftOfferCount`（3）/ `DraftPickTime`（15秒）、`PenguinWarsOnlineLink` にドラフト用メッセージ
* メッセージ名（追加分）:
  * `pw.draft`（ホスト→ゲスト・再送あり）: `int` ラウンド（0始まり）/ `int[]` ゲストの候補 / `int[]` ゲストがここまでに取ったキャラ（時間切れでホストが決めた分も一覧に出すため毎回全部送る）
  * `pw.pick`（ゲスト→ホスト・再送あり）: `int` ラウンド / `int` 候補の何番目か。ラウンドが今と違えば捨てる
  * 完了は既存の `pw.deck` を兼ねる（届いたらゲストは編成確認へ）
* 流れ: 接続 → `pw.ready` → ホストが `DraftSession` を作り `Phase = Draft` → ラウンドごとに自分の画面と `pw.draft` → 両者選んだら（または15秒で自動選択）次へ → 10体揃ったら `InitializeVersusHost` → `pw.deck` → 両者 `BeginIntro`（対戦は `DeckRevealPanel`、エンドレスは今まで通り `DeckIntroPanel`）
* 計画・仕様から変えた点:
  * 両者が選び終わったら待ち時間なしで次のラウンドへ進む（選んだ後は「相手を待っています...」を表示）
  * 候補は「自分が取ったキャラを除く全キャラ」から純ランダム。ランダム編成のような「壁を最低2体」の制約はドラフトには付けていない（仕様 §6 に無いため）
  * 編成確認は上に自分・下に相手の10体ずつ（名前は `SeatNames`）。絵は試合と同じく自分 = 青・相手 = 赤
  * カードには役割の横に「範囲 / 単体」も出した（仕様 §5.5 の表にある項目で、選ぶときの判断材料になるため）
  * PC のキーボードでのカード選択は付けていない（マウスでクリックできるため）
* 次フェーズへの注意:
  * Unity エディタでの Rebuild・2台での通し確認は未実施（dotnet で Battle テスト全68件合格、Assembly-CSharp / Assembly-CSharp-Editor の複製でコンパイルエラー0）
  * 既存の `PenguinWarsBalance.asset` には新しい2項目が無いが、Unity が初期値（3 / 15秒）で読む
  * Phase 9・10 で増えたファイルの一部（`PenguinWarsSceneBuilder.Online.cs` など）と今回の新規ファイルに `.meta` がまだ無い。Unity を開くと作られるので、2人で GUID がずれないよう生成された `.meta` もコミットする
  * ドラフト中に相手が切断した場合は、試合中と同じく勝ち扱いでリザルトへ（城HPの表示は空）
