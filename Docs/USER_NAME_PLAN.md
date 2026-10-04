# ユーザー名機能 実装計画書

初回起動時にユーザー名を入力し、各ミニゲームで「P1」「YOU」「HOME」などと出ている自分の欄をその名前で表示する。
オンライン対戦では相手の名前も自動で受け取って表示する。

- 対象: サッカー / 卓球 / モルック / ゴルフ / 人生ゲーム
- 対象外: カードゲーム（50_CardGame）

---

## 1. 決定済みの仕様

| 項目 | 決定内容 |
|---|---|
| 入力タイミング | 初回起動時、タイトル画面で名前が未設定ならダイアログを出す |
| 変更 | 不可（一度決めたらそのまま） |
| 文字数 | 最大6文字。前後の空白を除いて空なら決定できない |
| 保存先 | `PlayerPrefs`（端末ごと。ブラウザ版はブラウザごと） |
| ローカル多人数（1台を回す） | P1 だけユーザー名。P2〜P4 は今まで通り「P2」「P3」… |
| 表記 | 「P1 パワー型」→「たろう パワー型」。オンラインの「（あなた）」は残す |
| オンライン | 接続時に裏で名前を自動交換して表示する（ユーザー操作なし） |
| 名前交換の実装場所 | 共通の `OnlineSession` に集約。各ゲームは「席Nの名前」を参照するだけ |
| 卓球 | YOU → 自分の名前、RIVAL → 相手の名前。NPC はそのまま |
| サッカー | オンライン: HOME / AWAY → それぞれの名前。CPU戦: HOME → 自分の名前、AWAY はそのまま |

## 2. 未決事項（対話で決める）

推奨案を仮置きしている。決まったらこの表を更新する。

| # | 項目 | 推奨案（仮） |
|---|---|---|
| Q1 | ブラウザ版での日本語入力 | Unity の InputField は WebGL で日本語IMEがほぼ使えないため、ブラウザ版だけ jslib で `window.prompt` を出して入力させる（既存 `WebBrowser.jslib` と同じ方式） |
| Q2 | 決定時の確認 | 変更不可なので「この名前でいい？（あとから変更できません）」の確認ダイアログを1回挟む（既存 `ShowConfirmDialog` を流用） |
| Q3 | ゴルフ等で P1 を NPC にした場合 | 席0が NPC なら名前は使わず「P1」のまま（NPC にユーザー名が付くのは不自然なため） |
| Q4 | 使えない文字 | `<` `>` は除去する（ゴルフのリッチテキスト `<color>` が壊れるため）。それ以外は制限しない |
| Q5 | 同名対策（オンライン） | 何もしない。席の色と「（あなた）」で見分けられる |
| Q6 | タイトル画面に名前を出すか | 出さない（スコープ外） |
| Q7 | 名前が届く前に相手が切断/タイムアウト | 名前が届かなければ従来の「P2」「RIVAL」等にフォールバック |
| Q8 | 卓球の結果文言 | 「相手の勝ち」→「{相手名}の勝ち」、「YOUR SERVE」→「{名前} SERVE」も置き換える |

---

## 3. 全体設計

```
Common/Scripts/Profile/
  UserProfile.cs        … 名前の保存・読み込み・検証（PlayerPrefs）
  SeatNames.cs          … 「席N の表示名」表。ローカル/オンラインで中身を切り替える
  NameEntryDialog.cs    … 初回の名前入力ダイアログ
Common/Plugins/WebGL/
  NameInput.jslib       … ブラウザ版の日本語入力（Q1）
Common/Scripts/Online/
  OnlineSession.cs      … 接続時に名前を交換し SeatNames へ入れる
```

**なぜ `SeatNames` を挟むか:** ゴルフ（`GolfPlayerColors.Name`）と人生ゲーム（`LifeTexts.PlayerName`）は席番号から名前を作る static 関数を多数の箇所から呼んでいる。この1関数の中身を `SeatNames.Get(seat)` に差し替えるだけで全表示が切り替わり、呼び出し側を触らずに済む。

```csharp
// イメージ（実装時に確定）
public static class SeatNames
{
    // ローカル: [ユーザー名, "P2", "P3", "P4"]  オンライン: 交換した全員の名前
    public static string Get(int seat);
    public static void UseLocal();                 // 各ゲームのオフライン開始時
    public static void UseOnline(string[] names);  // OnlineSession が名前交換後に呼ぶ
}
```

---

## 4. フェーズ別計画

各フェーズの終わりで「遊べる状態」を保つ。フェーズ単位でコミット/PRを分ける。

### Phase 1: 共通基盤（名前の入力と保存）

**ゴール:** 初回起動で名前を入力でき、2回目以降は聞かれない。ゲーム側の表示はまだ変わらない。

| 手順 | 内容 | 対象ファイル |
|---|---|---|
| 1-1 | `UserProfile`（`HasName` / `Name` / `TrySave(string)`、6文字・空欄・`<>` の検証） | 新規 `Common/Scripts/Profile/UserProfile.cs` |
| 1-2 | `NameEntryDialog`（入力欄＋決定ボタン、閉じるボタンなし） | 新規 `Common/Scripts/Profile/NameEntryDialog.cs` |
| 1-3 | タイトル起動時に未設定ならダイアログを表示、確定まで他操作をブロック | `Common/Scripts/Title/TitleController.cs` |
| 1-4 | シーンビルダーにダイアログ生成を追加 | `Common/Editor/TitleSceneBuilder.cs`（必要なら `UIDialogBuilder.cs`） |
| 1-5 | ブラウザ版の入力（Q1） | 新規 `Common/Plugins/WebGL/NameInput.jslib` + C# 側呼び出し |
| 1-6 | 開発用メニュー「ユーザー名をリセット」（変更不可なので動作確認用に必要） | 新規 Editor スクリプト |

**確認:** エディタ / Android / ブラウザ(Windows) で、初回入力 → 再起動しても聞かれない → リセットメニューで再度聞かれる。

### Phase 2: オフライン表示の置き換え

**ゴール:** NPC戦・ローカル多人数で、自分（P1）の欄がユーザー名になる。

| 手順 | 内容 | 対象ファイル |
|---|---|---|
| 2-1 | `SeatNames`（`UseLocal` / `Get`）を追加 | 新規 `Common/Scripts/Profile/SeatNames.cs` |
| 2-2 | ゴルフ: `GolfPlayerColors.Name` を `SeatNames.Get` 経由に。設定画面の「P1 / 人間」ラベルも | `Games/04_Golf/Scripts/View/GolfPlayerColors.cs`, `UI/GolfSetupPanel.cs` 他 |
| 2-3 | 人生ゲーム: `LifeTexts.PlayerName` を `SeatNames.Get` 経由に | `Games/05_LifeGame/Scripts/UI/LifeTexts.cs` |
| 2-4 | モルック: `$"P{i + 1}"` 直書き4箇所を `SeatNames.Get` に | `MolkkyGameManager.cs`, `UI/CharacterSelectPanel.cs`, `UI/PlayerSetupPanel.cs` |
| 2-5 | 卓球: YOU / YOUR SERVE → ユーザー名 | `Games/02_TableTennis/Scripts/TableTennisGameManager.cs` |
| 2-6 | サッカー: CPU戦の HOME → ユーザー名 | `Games/01_Soccer/Scripts/SoccerGameManager.cs` |
| 2-7 | 各ゲームのオフライン開始時に `SeatNames.UseLocal()` を呼ぶ | 各 `*GameManager.cs` |
| 2-8 | 6文字の名前で崩れる箇所のフォントサイズ/幅を調整 | 各 `*SceneBuilder.cs`（必要な箇所のみ） |

**確認:** 6文字の名前（例「ああああああ」）で全5ゲームを1回ずつ遊び、手番バナー・スコア・リザルト・勝利演出が崩れないこと。P1 を NPC にしたとき（Q3）。

**実装メモ（Phase 3 以降で前提にすること）:**
- `UseLocal` を呼ぶ場所: ゴルフ / 人生ゲーム / モルックは各設定パネルの再描画時（P1 の人間/NPC 切替に追従するため）、卓球 / サッカーは `OnGameReady` の先頭
- `OnlineSession.PrepareAsync` で `SeatNames.UseOnline(null)` を呼び、オンライン中は全席「P1」「P2」… に戻している。Phase 3 では名前交換後に `UseOnline(names)` を呼べばよい
- 卓球の YOU / YOUR SERVE、サッカーの HOME はオフラインのときだけユーザー名。オンラインは従来表記のまま（Phase 4 で置き換え）

### Phase 3: オンラインの名前交換（共通）

**ゴール:** オンライン接続時に全員の名前が `SeatNames` に入る。ゲーム側の表示は Phase 2 の仕組みでそのまま切り替わる。

| 手順 | 内容 |
|---|---|
| 3-1 | 1対1の部屋: 接続直後にお互い名前メッセージ（`session.name`）を送る。相手の名前を受け取ってから `OnPeerConnected` を発火する |
| 3-2 | 3人以上の部屋: クライアントは接続時にホストへ名前を送る。ホストは席順に名前を集め、既存の開始メッセージ（`session.start`）に全員分の名前を載せて配る |
| 3-3 | 受け取った名前で `SeatNames.UseOnline(names)` を呼ぶ |
| 3-4 | 名前が届かないまま一定時間経ったら「P2」等で開始（Q7） |

- 対象: `Common/Scripts/Online/OnlineSession.cs` のみ
- 注意: `session.start` は今 `int×2` 固定サイズなので、名前分バッファを広げる。名前は `FixedString64Bytes`（6文字×UTF-8最大4バイトで収まる）
- 注意: 1対1では「どちらが席0か」を決める必要がある → ホスト=席0、ゲスト=席1（サッカーの HOME/AWAY、卓球の自分/相手と対応）

**確認:** エディタ2つ（既存の匿名ID分離済み）で 1対1・3人部屋の両方を接続し、ログで双方の名前が入っていること。

**実装メモ（Phase 4 以降で前提にすること）:**
- 1対1は「ゲストが名乗る → ホストが返事で名乗る」の順。ホストから先に送ると、ゲスト側のハンドラ登録前に届いて捨てられることがあるため
- 名前は `FixedString64Bytes` ではなく `string` をそのまま書き込み（`FastBufferWriter` を伸長可能にして対応）
- 席: ホスト=0、ゲスト=1。3人以上は参加順（`session.start` に全員分の名前を載せる）
- 名前待ちは3秒でタイムアウトし、届かなかった席は空欄（=「P{n}」表示）。タイムアウト後に遅れて届いた名前は無視
- ログ: 成功時 `[OnlineSession] 名前交換完了: A / B`、タイムアウト時は Warning

### Phase 4: オンライン表示の置き換え

**ゴール:** オンライン対戦で相手の画面に自分の名前が出る。

| 手順 | 内容 | 対象ファイル |
|---|---|---|
| 4-1 | 卓球: RIVAL → 相手の名前、「相手の勝ち」→「{相手名}の勝ち」（Q8） | `TableTennisGameManager.cs` |
| 4-2 | サッカー: HOME / AWAY → それぞれの名前（自分側の YOU 付与は残すか要確認） | `SoccerGameManager.cs` |
| 4-3 | モルック: オンラインの `LocalPlayerName = "あなた"` を「{名前}（あなた）」形式に揃える | `MolkkyGameManager.cs`, `UI/PlayerSetupPanel.cs` |
| 4-4 | ゴルフ / 人生ゲーム: Phase 2 で `SeatNames` 経由になっているので、表示確認のみ | — |

**確認:** スマホ×ブラウザで各ゲーム1試合ずつ。双方の画面で名前が正しい側に出ること。

**実装メモ:**
- `SeatNames.Get(seat, fallback)` を追加。名前が届かなかった席は卓球なら YOU / RIVAL、サッカーなら HOME / AWAY に戻す（Q7。「P2」より従来表記の方が自然なため）
- 卓球: 自分の席 = ホストなら0、ゲストなら1。サーブ表示は「{名前} SERVE」に統一し、負けたときは「{相手名}の勝ち」（NPC戦は従来どおり「NPCの勝ち」）
- サッカー: 自分側の「(YOU)」は残した（決定済み仕様「（あなた）は残す」に合わせる）。例「たろう(YOU) 1 - 0 はなこ」
- モルック: 自分の席は「{名前}（あなた）」。セットアップ画面の席ラベルはベストフィット済みなので11文字でも収まる
- ゴルフ / 人生ゲーム: コード変更なし（Phase 2 の `SeatNames` 経由で自動的に切り替わる）

### Phase 5: 仕上げ

- 仕様書（`Docs/0X_*_SPEC.md`）の「P1」表記に関する記述を更新
- `MiniGameRebuildAll` でシーン再生成 → 差分確認してコミット
- 既存ユーザー（すでに遊んでいた端末）は次回起動時に入力ダイアログが出ることを確認

**実装メモ:**
- 仕様書 01〜05 の表示名の記述（スコア・席名・結果文言・オンラインの「（あなた）」・既知の制限の「名前は入力できない」）を更新
- 既存ユーザー: 保存キー `UserProfile.Name` は今回新設なので、既存端末は未設定扱い → タイトルでダイアログが出る（コード上で確認）
- Phase 1〜2 のシーンビルダー変更は Rebuild All まで Scene に反映されない。特に TitleScene に `NameEntryDialog` が入るまで入力ダイアログは出ない

---

## 5. リスク・注意点

| リスク | 対策 |
|---|---|
| ブラウザ版で日本語が打てない | Q1 の jslib 方式。Phase 1 で最優先で確認する |
| 6文字の名前で既存レイアウトがはみ出る | Phase 2-8 で最長名テスト。既存の「1行に収まるよう縮める」設定の範囲内で調整 |
| オンラインで名前が届く前に試合が始まる | 3-1 で受信後に `OnPeerConnected` を出す。3-4 でタイムアウト |
| 名前に `<color>` 等が入りリッチテキストが壊れる | Q4 で `<>` を除去 |
| Scene / Prefab の競合 | UI はシーンビルダー経由で生成済みの方針を踏襲。手作業で Scene を編集しない |
