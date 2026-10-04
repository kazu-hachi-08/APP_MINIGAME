# Phase 10: オンライン対戦（編成はランダム）

## ゴール

モード選択から「部屋を作る / コードで参加」で2人が接続し、お互いにペンギンを出し合って城を攻め合える。最大5分、城HP割合で決着。編成はこのフェーズでは両者ランダム10体（ドラフトは Phase 11）。

## 読むもの

* 仕様書: §1.1 モード、§2.2 オンライン対戦、§2.3 勝敗、§10 オンライン対戦
* 前フェーズまでの引き継ぎメモ（`BattleWorld` / `BattleCommand` / `BattleEvent` / `BattleEventPresenter` の公開API）
* 既存コード:
  * `Common/Scripts/Online/OnlineSession.cs`・`ModeSelectPanel.cs`（公開メソッドとイベント）
  * `Common/Editor/ModeSelectPanelBuilder.cs`（シーンへの組み込み方）
  * `01_Soccer/Scripts/Online/SoccerOnlineLink.cs`（名前付きメッセージの登録・送信・シリアライズのやり方）

## 作るもの

### Battle

| ファイル | 内容 |
| --- | --- |
| `BattleWorld` | 対戦モード: 右も通常の城・右も Wallet / 砲を使う・敵波なし・制限時間 5分・時間切れ判定（城HP割合、同じなら引き分け） |
| `BattleSnapshot.cs` | ゲストへ送る状態（仕様 §10.2 の表）。`BattleWorld` → Snapshot の作成と、バイト列への書き出し・読み込み |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `Scripts/Online/PenguinWarsOnlineLink.cs` | メッセージ名の定義と送受信。ホスト: Snapshot を約15回/秒、イベントを再送ありで送る。ゲスト: コマンドを再送ありで送る |
| `Scripts/Online/GuestBattleView.cs` | ゲストは `BattleWorld` を動かさず、Snapshot を View / UI に流す（位置は補間）。イベントは `BattleEventPresenter` へ |
| `Scripts/Online/RemoteCommandInput.cs` | ホスト: 受け取ったゲストのコマンドを `Side.Right` で `Enqueue` |
| `Scripts/Game/ICommandSink.cs` 等 | UI・キー入力の送り先を「ローカルの World」か「ホストへ送信」に切り替える口 |
| `Scripts/View/SideMirror.cs` | ゲスト画面の左右反転（座標変換を1か所に集約。§10.3） |
| `PenguinWarsGameManager` | モード選択（`ModeSelectPanel`）→ エンドレス / オンライン分岐。切断時は勝ち扱いでリザルト |
| `PenguinWarsSceneBuilder` | ModeSelectPanel の組み込み |

## 実装メモ

* **ゲストの UI は Snapshot の値で表示する**（Phase 3 で「UI は状態を読むだけ」にしてある前提）
* 反転は View 側だけ。`BattleWorld` の座標はホスト基準のまま
* Snapshot はユニット数が多いと大きくなる。1体あたり ID・No・陣営・X(short に量子化)・HP割合(byte)・状態(byte) 程度に抑える
* 出撃ボタンを押してから出るまでゲストは少し遅れる。気になるようならボタンを押した瞬間に「押した」演出だけ先に出す
* 乱数を使うのはホストだけ（能力の確率もホストで判定）

## やらないこと

ドラフト（Phase 11）。名前表示は既存の `SeatNames` があれば使い、無ければやらない。

## 完了条件

* [ ] エンドレスが今まで通り遊べる（デグレしていない）
* [ ] 2台（またはエディタ＋ビルド）で接続し、両者の出撃・働きペンギン・砲が相手画面にも反映される
* [ ] ゲストでも自分の城が左に見える
* [ ] 城を落とす / 時間切れ / 切断 のそれぞれで正しい結果になる
* [x] EditMode テスト: Snapshot の書き出し→読み込みで値が一致 / 時間切れ判定（`OnlineSyncTests`。dotnet で全61件合格）

## ユーザー確認手順

1. `Rebuild PenguinWars` → Windows ビルド（またはブラウザ版）を1つ作り、エディタとビルドで部屋を作る／参加する
2. 他ゲームのオンライン確認と同じ手順で、上の完了条件を確認

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * Battle: `BattleWorld.Versus.cs`（制限時間・時間切れ判定）/ `BattleSnapshot.cs`（`UnitSnapshot` `SideSnapshot` 含む）/ `BattleEventCodec.cs` / `GuestWorldMirror.cs`（ゲストの表示用 World に状態を反転して書き込む・位置の補間）/ `SideMirror.cs` / `AssemblyInfo.cs`（テスト用 `InternalsVisibleTo`）
  * Unity: `Game/ICommandSink.cs` / `Game/PenguinWarsGameManager.Online.cs`（partial）/ `Online/PenguinWarsOnlineLink.cs` / `Online/GuestBattleView.cs` / `Editor/PenguinWarsSceneBuilder.Online.cs`
  * テスト: `Tests/Editor/OnlineSyncTests.cs`
  * 変更: `BattleSettings.TimeLimit`、`BattleEventType.TimeUp`（Side=負けた側、Amount=引き分けなら1）、`WalletState` / `CannonState` / `DeckSlotState` / `UnitStatusEffects` に internal の `Restore`、`PenguinWarsBalance.VersusTimeLimit`（300秒）、`PenguinWarsPhase.ModeSelect`
* 公開API（次フェーズが使うもの）:
  * `BattleRunner.InitializeEndless()` / `InitializeVersusHost()` / `InitializeGuest(myDeckNos, opponentDeckNos, sink)`。ドラフトで編成を決めるなら、`InitializeVersusHost` にデッキを渡す形に変えるのが最小（今は中でランダムに決めている）
  * `PenguinWarsOnlineLink.Begin(isHost)` / `Stop()` / `SendDecks(left, right)` / `Submit(command)`、イベント `GuestReady`（ホスト）・`DecksReceived(hostLeft, hostRight)` `SnapshotReceived` `EventsReceived`（ゲスト）
  * `BattleWorld.RemainingTime` / `HasTimeLimit` / `IsDraw`
  * 流れ: 接続 → ゲストが `pw.ready` → ホストが編成を決めて `pw.deck` → 両者 `BeginIntro(_versusDeckIntroDuration)`。ドラフトはこの `pw.ready` 受信後〜`pw.deck` 送信の間に挟めばよい（`PenguinWarsPhase.ModeSelect` の間）
* メッセージ名一覧:
  * `pw.ready`（ゲスト→ホスト・再送あり）: 中身なし。ゲストがハンドラ登録を終えた合図
  * `pw.deck`（ホスト→ゲスト・再送あり）: `int[]` ホスト左の編成No / `int[]` ホスト右の編成No
  * `pw.snap`（ホスト→ゲスト・再送なし UnreliableSequenced・約15回/秒）: `BattleSnapshot.ToBytes()`
  * `pw.evt`（ホスト→ゲスト・再送あり ReliableFragmentedSequenced）: 1フレーム分の `BattleEventCodec.ToBytes()`
  * `pw.cmd`（ゲスト→ホスト・再送あり）: `byte` コマンド種類 / `byte` 枠番号。陣営はホストが `Side.Right` に付け直す
* 計画・仕様から変えた点:
  * **反転のしかた**: ゲストは「表示用の BattleWorld（Step しない）」を持ち、届いた状態を陣営入れ替え＋X 裏返しで書き込む。これでゲストの自分も `Side.Left` になり、UI（ボタン・財布・砲）・UnitView・BattleEventPresenter・勝敗判定がエンドレスと同じコードのまま動く。ゲストの自分の絵も青（左陣営の色）、相手が赤になる（§7.2「味方は青系・相手は赤系」と同じ）
  * `SideMirror` は計画の `View/` ではなく `Battle/` に置いた（状態とイベントの反転を Battle 側で行うため）
  * `RemoteCommandInput` は作らず、`PenguinWarsOnlineLink.ReceiveCommand` で直接 `BattleRunner.Enqueue(…Side.Right…)` している（数行のため）
  * ゲストのコマンド送信は `BattleRunner.Enqueue` が `ICommandSink`（= Link）に回す。UI・キー入力は変更なし
  * 編成発表はお互いの10体ではなく自分の10体だけ（DeckIntroPanel をそのまま使用）。§2.2 の「編成確認（お互いの10体）」は Phase 11 で
  * 引き分けのタイトル `DRAW` は `BaseMiniGameManager.FinishGame` が対応していないので、`FinishAsDraw` で同じ手順を自前で踏んでいる（共通基盤は変更していない）
  * リザルトのスコア欄は「自分の名前 72% - 40% 相手の名前」（城の残りHP割合。名前は `SeatNames`）
  * リトライ: エンドレスは static フラグでモード選択を飛ばして即開始、対戦はモード選択から
  * オンライン中のポーズはサッカーと同じく試合を止めずにダイアログだけ出す
* 次フェーズへの注意:
  * Unity エディタでの Rebuild・2台での接続確認は未実施（dotnet で Battle テスト全61件合格、Assembly-CSharp / Assembly-CSharp-Editor の複製でコンパイルエラー0）
  * ルートの `.csproj` は Phase 9・10 の新ファイルが入っておらず古い。Unity を開けば再生成される
  * ゲストは `PenguinWarsPhase.ModeSelect` の間に届いた `pw.deck` だけを受け付ける。ドラフトを挟むなら、ドラフト用の段階（`Draft`）から編成発表に進むよう `HandleDecksReceived` / `HandleGuestReady` の条件を直す
  * ゲストの撃破数（`GetKillCount`）は同期していない（対戦のリザルトでは使っていない）
  * ユニットIDは状態では下位16ビットだけ送っている（場にいる間の見分けにしか使わないため）
