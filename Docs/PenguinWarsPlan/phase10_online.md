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
* [ ] EditMode テスト: Snapshot の書き出し→読み込みで値が一致 / 時間切れ判定

## ユーザー確認手順

1. `Rebuild PenguinWars` → Windows ビルド（またはブラウザ版）を1つ作り、エディタとビルドで部屋を作る／参加する
2. 他ゲームのオンライン確認と同じ手順で、上の完了条件を確認

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
* 公開API（次フェーズが使うもの）:
* メッセージ名一覧:
* 計画・仕様から変えた点:
* 次フェーズへの注意:
