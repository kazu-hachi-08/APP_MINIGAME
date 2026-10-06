# Phase 5: オンライン対戦に組み込む

## ゴール

オンライン対戦でドラフトが9ラウンドになり、終わると「じぶんペンギン選択」（3枠から1つ・制限時間あり）が出る。選んだじぶんペンギンが10体目として編成に入り、編成確認で相手のじぶんペンギンも見え、対戦で出撃できる。ホスト・ゲストどちらの画面でも、自分のじぶんペンギンは青、相手のは赤の自分で作った見た目で動く。

## 読むもの

* INDEX の「決めた前提」「対戦の流れ」
* Phase 2・4 の引き継ぎメモ
* 仕様書: §2.2 オンライン対戦、§6 ドラフト、§10 オンライン対戦（メッセージの表）
* 既存コード: `PenguinWarsGameManager.Draft.cs` / `.Online.cs`、`PenguinWarsOnlineLink.cs`、`DraftSession.cs`、`DraftPanel.cs`、`DeckRevealPanel.cs`、`BattleRunner.cs`（`InitializeVersusHost` / ゲストの初期化 / `CollectStatsByNo`）、`GuestWorldMirror.cs`（`findStats` の引き方）

## 作るもの

### ドラフトを9回に

| 変更 | 内容 |
| --- | --- |
| `BeginDraftAsHost` | `DraftSession` のラウンド数を `DeckSize - 1`（定数 `CustomUnitRules.SlotsInDeck = 1` を引く） |
| `DraftPanel` / ゲスト側の `ShowRound` | 合計ラウンドの表示を9に。下の「取ったキャラ」一覧の10個目は「じぶん」の仮マス（何が入るか分かるように） |

### じぶんペンギン選択

| ファイル | 内容 |
| --- | --- |
| `Scripts/UI/CustomPickPanel.cs` | 3枚のカード（歩く絵・名前・役割・コスト・数値の要約・能力）。上に残り秒（`DraftPickTime`＝15秒を流用）。選ぶと「相手を待っています...」。時間切れは `LastPickedSlot`。選んだ枠を `LastPickedSlot` として保存 |
| `Editor/PenguinWarsSceneBuilder.CustomPick.cs` | 画面の生成（`DraftPanel` のカードの作りを流用してよい） |
| `PenguinWarsPhase` | 新しい状態は作らず `Draft` のまま扱う（ドラフトの続きとして切断処理などを共有するため）。必要なら Draft 内の小さな段階フラグで分ける |

### 通信

| メッセージ | 向き | 配信 | 中身 |
| --- | --- | --- | --- |
| `pw.custom`（新規） | ゲスト → ホスト | 再送あり | ゲストが選んだじぶんペンギンの JSON（`CustomUnitDefinition.ToJson`） |
| `pw.deck`（拡張） | ホスト → ゲスト | 再送あり | 今の中身（ホストの編成No・ゲストの編成No・ステージ番号）＋ **ホストのじぶんペンギン JSON・ゲストのじぶんペンギン JSON（ホストが `Sanitize` した後のもの）** |

* ホストは「自分の選択」と「`pw.custom` の受信」の両方がそろうか、選択の制限時間＋猶予 `CustomPickGrace`（3秒）が過ぎたら先へ進む。ゲストの定義が届かなかったときはお手本1（じぶんナイト）を使う
* ホストは受け取った定義を必ず `CustomUnitRules.Sanitize` してから使う（古いビルド・壊れたデータへの備え）
* `pw.deck` のバッファは JSON 2つ分大きくする（名前8文字＋見た目ID で1体 数百バイト。`DeckBufferSize` を見直す）
* JSON 文字列は `FastBufferWriter.WriteValueSafe(string)` で送る

### 試合の初期化

| 変更 | 内容 |
| --- | --- |
| ホスト（`FinishDraftAsHost` の後） | 両者のじぶんペンギンを `CustomUnitFactory.Create(def, 91 / 92)` → `catalog.ClearRuntime()` → `RegisterRuntime` ×2。編成は「ドラフトの9体＋じぶんペンギン」をコスト順に並べる |
| `BattleRunner.InitializeVersusHost` | ドラフトの No 列にじぶんペンギンの No を足して渡す。`CollectStatsByNo` は実行時登録分も含める（ゲストの `GuestWorldMirror` の `findStats` も同じ辞書を使うので、ゲストで 91 / 92 が引ける） |
| ゲスト（`pw.deck` 受信時） | 2つの JSON から同じく `Create` → `RegisterRuntime`。**ゲストは Sanitize し直さず、ホストから届いた値をそのまま使う**（ホストと数値がずれないように） |
| 試合の後 | シーンを読み直すので特別な後片付けは不要。ただし `catalog.ClearRuntime()` を試合の初期化の頭でも呼ぶ（エディタで再生を繰り返したときの残りを消すため） |

### 編成確認

* `DeckRevealPanel` で、じぶんペンギンのマスに小さく「じぶん」の印（自分・相手とも）。名前は自分で付けた名前

## 実装メモ

* `UnitButtonBar` / `UnitViewPool` / `DeckRevealPanel` は `catalog.Get(no)` のままで、実行時登録したじぶんペンギンが引ける（Phase 2）。ここを書き換え始めたら設計からずれているので立ち止まる
* ゲストの画面は左右反転だが、No は入れ替えない。ゲストの画面では No 92（自分）が左・青で描かれ、`RegisterRuntime` で作った左右両方の絵のうち `Side.Left` 側が使われる
* 撃破報酬・ペンギン砲・能力などは `UnitStats` を見るだけなので、追加の対応はいらない
* 相手が選択中に切断したら、ドラフト中の切断と同じ扱い（勝ち）

## やらないこと

ステージ（1人）でじぶんペンギンを使う、じぶんペンギンを2体以上入れる、相手のじぶんペンギンを保存する。

## 完了条件

* [ ] ドラフトが9ラウンドで終わり、じぶんペンギン選択が出る
* [ ] ホスト・ゲストとも、編成確認に自分と相手のじぶんペンギンが出る
* [ ] 対戦でじぶんペンギンを出撃でき、ホスト・ゲストどちらの画面でも見た目・数値が同じ（色だけ自分が青）
* [ ] 時間切れで前回の枠が選ばれる。ゲストが選ばずに放置しても試合が始まる
* [ ] 既存の EditMode テスト（`DraftTests` / `OnlineSyncTests` を含む）が通る。ラウンド数を前提にしたテストは9に合わせて直す

## ユーザー確認手順

1. ビルド（または ParrelSync などで2つ起動）してホスト・ゲストで接続
2. ドラフト9回 → じぶんペンギン選択 → 編成確認で相手のを確認
3. 対戦でお互いのじぶんペンギンを出し合い、見た目・HPバー・攻撃の様子が両方の画面で合っているか
4. 片方は選ばずに時間切れにしてみる

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
* 公開API:
* 計画から変えた点:
* 次フェーズへの注意:
