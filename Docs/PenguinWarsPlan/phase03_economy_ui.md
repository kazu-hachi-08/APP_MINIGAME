# Phase 3: さかな・働きペンギン・再生産・キャラボタン

## ゴール

画面下のボタンで出撃できる。出撃にはさかなが必要で、時間で増える。働きペンギンでレベルを上げると上限と増える速さが上がる。出撃後は再生産時間が経つまで同じキャラを出せない。ボタンは5個×2ページ。

## 読むもの

* 仕様書: §4.1 お金、§4.2 働きペンギン、§4.3 出撃、§7.1 画面レイアウト
* 前フェーズの引き継ぎメモ
* Phase 2 で作った `BattleWorld` / `BattleCommand` / `UnitStats`（公開部分）

## 作るもの

### Battle

| ファイル | 内容 |
| --- | --- |
| `WalletState.cs` | 陣営ごとの さかな・レベル・上限・増加速度。`Tick(dt)`・`TrySpend(cost)`・`TryLevelUp()` |
| `WalletTable.cs` | レベルごとの上限・速度・必要額（`PenguinWarsBalance` の表を Battle 用に写したもの） |
| `DeckSlotState.cs` | スロットごとの再生産残り時間 |
| `BattleCommand` | `LevelUpWallet(side)` を追加。`Spawn` はさかな・再生産・上限30をチェックして失敗なら何もしない |
| `BattleWorld` | 両陣営に Wallet とスロット状態を持たせる。敵（CPU）側は Wallet を使わない設定にできるようにする（エンドレスの敵は §8 のルールで湧くため） |

### Unity 側

| ファイル | 内容 |
| --- | --- |
| `PenguinWarsBalance` | 働きペンギン表（仕様 §4.2）を追加 |
| `Scripts/UI/UnitButton.cs` | 1ボタン: アイコン（仮: 色四角）・コスト・再生産ゲージ・出せないとき暗く |
| `Scripts/UI/UnitButtonBar.cs` | 5個×2ページ、`[⇄]` で切り替え。押したら `Spawn(Left, slot)` |
| `Scripts/UI/WalletButton.cs` | 左下。レベル・さかな `420/1100`・次レベル必要額 |
| `KeyboardCommandInput` | Tab=ページ切替、1〜5 は表示中ページのスロット、Q=働きペンギン |
| `Editor/PenguinWarsSceneBuilder.Hud.cs` | 上記UIを生成 |

* デッキはまだ3体（スロット 0〜2）。残りスロットは空ボタン表示

## 実装メモ

* UI は毎フレーム `BattleWorld` の状態を読んで表示するだけ。UI からお金を直接いじらない（Phase 10 でゲスト画面にも同じ UI を使うため）
* ボタンは `SafeAreaFitter`（共通）の内側に置く
* スマホで押しやすいサイズ（目安: 画面高さの 15%）

## やらないこと

撃破報酬・ペンギン砲（Phase 4）。アイコンの本番絵（Phase 6）。

## 完了条件

* [x] EditMode テスト: さかなが時間で増え上限で止まる / 足りないと出撃できない / 再生産中は出撃できない / レベルアップで上限と速度が変わる・最大レベルで止まる
* [ ] ボタン・キーの両方で出撃・ページ切替・働きペンギンが動く
* [ ] 出せないボタンが暗くなり、再生産ゲージが見える

## ユーザー確認手順

1. `Rebuild PenguinWars` → 再生
2. スマホ表示の確認は Game ビューを横長（例 1920×1080 / 2340×1080）にして確認

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * `Scripts/Battle/`: `WalletTable` / `WalletState` / `DeckSlotState`（`BattleCommand`・`BattleSettings`・`BattleWorld` を更新）
  * `Scripts/UI/`: `UnitButton` / `UnitButtonBar` / `WalletButton`
  * `Scripts/Game/KeyboardCommandInput`（Tab・Q を追加。UI 部品経由で操作するよう変更）、`BattleRunner`（`World` 公開・Wallet 設定）、`PenguinWarsBalance`（働きペンギン表）
  * `Editor/PenguinWarsSceneBuilder.Controls.cs`（下部の操作UI。計画では `.Hud.cs` だったが、300行を超えそうなので分けた）
  * `Tests/Editor/EconomyTests.cs`（7件）
* 公開API（次フェーズが使うもの）:
  * `BattleWorld.GetWallet(side)` / `GetSlot(side, slot)` / `GetDeck(side)` / `CanSpawn(side, slot)`（ボタンの暗さと実際の出撃で同じ判定）
  * `WalletState`: `Fish`（int・切り捨て）/ `Level` / `Cap` / `RatePerSecond` / `LevelUpCost` / `IsMaxLevel` / `CanLevelUp` / `TrySpend` / `TryLevelUp` / `Add(amount)`（撃破報酬用。上限で止まる。Phase 4 で使う）
  * `DeckSlotState`: `Remaining` / `Duration` / `IsReady` / `RemainingRatio`
  * `BattleCommand.LevelUpWallet(side)`
  * `BattleSettings.WalletTable`（既定は仕様 §4.2 の表）/ `RightSpawnsFree`（true で右はさかな・再生産チェックなし）
  * `PenguinWarsBalance.CreateWalletTable()`
  * `BattleRunner.World`（UI が読むだけ）
  * `UnitButtonBar.SpawnVisibleSlot(i)` / `TogglePage()` / `SlotsPerPage`、`WalletButton.LevelUp()`
* 計画・仕様から変えた点:
  * 開始時のさかなは 0（仕様に記載なし。本家と同じ）。最初の出撃（No1=75）まで約2.5秒
  * エンドレスの敵は `RightSpawnsFree = true` でさかな・再生産を無視（Wallet オブジェクト自体は両陣営にある）
  * 働きペンギンのレベルアップはイベントを出していない（音・演出を足すとき Phase 7 で `BattleEvent` を追加）
  * ページ切替ボタンの表記は `切替 1/2`（`⇄` は既定フォントに無い可能性があるため）
  * 仮アイコンはキャラNoから色相を決めた色四角＋名前（長い名前は Best Fit で縮小）
* 次フェーズへの注意:
  * 画面右下（出撃ボタン列の右、X≒1500〜1890）が空いているので、ペンギン砲ボタンはそこに置ける
  * 撃破報酬は `GetWallet(side).Add(cost / 2)`。右陣営の撃破で左に入れる処理を `BattleWorld` の死亡処理に足す
  * テストは前フェーズと同じく `dotnet test`（Battle と Tests の .cs を集めたプロジェクト）で 20件合格。Unity 側スクリプトは生成済み csproj を複製して新ファイルを足した一時プロジェクトで `dotnet build` し、エラー0を確認（Unity エディタでのコンパイル・Rebuild・再生は未確認）
