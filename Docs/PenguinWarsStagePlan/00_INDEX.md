# ペンギン大戦争 ステージモード 実装計画（INDEX）

一人用のエンドレスモードを廃止し、「ステージ制（決まった敵の出方で敵の城を落とす）」に置き換えるための **一時的な** 計画書。全フェーズ完了後にフォルダごと削除する（Phase 8）。

仕様書 `Docs/06_PENGUIN_WARS_SPEC.md` はまだエンドレスの記述のまま。ステージモードの仕様は **この計画書が正** とし、Phase 8 で仕様書に反映する。

> 旧計画 `Docs/PenguinWarsPlan/` の Phase 12（バランス調整・仕様書反映・計画書削除）の残りは、ユーザーと決めて **この計画の Phase 8 に吸収** した。Phase 8 で旧計画フォルダ `Docs/PenguinWarsPlan/` も一緒に削除する。

---

## AIへの指示（「ステージ計画のフェーズN進めて」と言われたら）

1. このINDEXを読み、下の「進捗」表で **フェーズNの前のフェーズがすべて完了** になっているか確認する。なっていなければ実装せずユーザーに伝える
2. `phaseNN_*.md`（対象フェーズのファイル）**だけ** を読む。他のフェーズのファイルは読まない（コンテキスト節約）
   * ただし直前フェーズの「引き継ぎメモ」は読む
3. フェーズファイルの「読むもの」に挙がった既存コード・仕様書の節だけを読む
4. 「作るもの」を実装する。「やらないこと」には手を出さない
5. 終わったら:
   * フェーズファイルの「引き継ぎメモ」を埋める（作ったファイル・公開API・計画から変えた点・次フェーズへの注意）
   * 下の「進捗」表を **完了** に更新する
   * ユーザーにフェーズファイルの「ユーザー確認手順」を伝える（AIはUnityエディタを操作できないので、Rebuild・再生確認はユーザーが行う）
6. ブランチ・コミット・PRはユーザーに言われたときだけ行う

計画と実装で食い違いが出たら、勝手に大きく変えずユーザーに確認する。小さな変更（クラス名・数値など）は引き継ぎメモに書けばよい。

---

## 進捗

| Phase | ファイル | 内容 | 終わったら遊べるもの | 状態 |
| --- | --- | --- | --- | --- |
| 1 | [phase01_stage_core.md](phase01_stage_core.md) | 敵の出方スクリプト・敵の城・ボス出現条件・エンドレス撤去 | 仮のステージ1つで敵の城を落とせる | 完了 |
| 2 | [phase02_progress_select.md](phase02_progress_select.md) | セーブ・ステージ選択画面・★・リザルト | 3ステージを順にクリアして★が残る | 未着手 |
| 3 | [phase03_unlock_deck.md](phase03_unlock_deck.md) | キャラ解放・編成画面・ずかんのロック表示 | クリアで仲間が増え、自分で編成できる | 未着手 |
| 4 | [phase04_gimmicks_intro.md](phase04_gimmicks_intro.md) | ステージのギミック・編成制限・敵のペンギン砲・出撃前の紹介 | ステージごとに違う遊び方ができる | 未着手 |
| 5 | [phase05_sim_tool.md](phase05_sim_tool.md) | 自動プレイでステージを検証するツール | エディタのメニューで全ステージの難しさが一覧できる | 未着手 |
| 6 | [phase06_stage_data.md](phase06_stage_data.md) | 全3章18ステージのデータ・解放の割り振り・調整 | 最初から最後までステージモードを遊び切れる | 未着手 |
| 7 | [phase07_effects.md](phase07_effects.md) | ボス登場・クリア・★獲得・解放の演出とサウンド | 手触りが整う | 未着手 |
| 8 | [phase08_polish.md](phase08_polish.md) | あそびかた・仕様書反映・計画書削除 | 完成 | 未着手 |

状態: 未着手 / 進行中 / 完了

**なぜこの順番か:** 「戦闘ロジック → 1周遊べる → 育成（解放・編成） → ステージの個性 → 検証ツール → 量産 → 演出」の順。ステージを量産（Phase 6）する前に、ギミックの種類（Phase 4）と難しさを測る道具（Phase 5）をそろえておく。先に18ステージを作ると、仕組みを変えるたびに全ステージを直すことになるため。

---

## 決めた前提（変えたい場合は Phase 1 を始める前にユーザーと決め直す）

| 項目 | 決めたこと | 理由 |
| --- | --- | --- |
| モード構成 | 一人用は「ステージ」だけ。エンドレスは完全に削除する（コード・記録・出現ゲートの絵も） | 2つのモードを保守し続けないため |
| ステージ数 | 3章 × 6ステージ = 18。各章の6番目はボスステージ | 解放（40体）を配り切れて、週末数回で作れる量 |
| 勝ち負け | 敵の城を落とせばクリア。自城が落ちたら失敗。制限時間はなし | にゃんこ大戦争と同じ。時間切れは★3の条件で使う |
| ★ | ★1: クリア / ★2: 自城HP 50% 以上でクリア / ★3: ステージごとの目標タイム以内でクリア | 3つとも結果から機械的に判定でき、別のプレイ方針（守り・速攻）を促せる |
| ★の使い道 | 記録と見せびらかしのみ（ステージ解放は「前のステージをクリア」で判定） | ★集めを必須にすると詰まる人が出るため |
| ボス | 敵城HPが決めた割合を下回ったら出る（にゃんこと同じ「城を叩くと出てくる」）。時間で出るものも作れる | 「攻め込んだ瞬間に山場が来る」流れを作る |
| キャラ解放 | 最初は10体。ステージを初めてクリアしたときに解放する（合計40体を18ステージに配る） | 育っていく楽しさ。50体を最初から見せると選べないため |
| オンライン対戦 | 解放に関係なく全50体のまま（ドラフトも変えない） | 友達同士で進み具合の差を出さないため |
| 編成 | 解放済みのキャラから自分で10体選ぶ。最後に使った編成を保存する | ステージに合わせて考えるのがステージ制の面白さ |
| ステージのデータ | 純C#の定義表（`Scripts/Battle/StageDefinitions.*.cs`、1章1ファイル）。アセットは作らない | キャラの定義表（`UnitDefinitions`）と同じ方式。テスト・検証ツールから直接読め、2人で別の章を触ってもぶつからない |
| セーブ | `PlayerPrefs` に JSON 1本（`PenguinWars.Campaign`）。ステージは番号ではなく ID（`"1-3"` など）で記録 | ステージの並び替え・追加でセーブが壊れないため |
| 難易度選択 | なし（ステージの並びそのものが難易度） | スコープ制御 |

---

## 全体設計（全フェーズ共通）

### ロジックと表示を分ける（既存方針のまま）

敵の出方・ボスの条件・★の判定はすべて純C#（`Scripts/Battle/`、`MiniGame.PenguinWars.Battle`）に置き、EditMode テストで確かめる。MonoBehaviour は「ステージを選ぶ・結果を見せる」だけにする。

```text
StageDefinitions（純C#の定義表。1章1ファイル）
   │  StageDefinition（戦場・城HP・敵の出方・★条件・解放キャラ・ギミック）
   ▼
BattleRunner.InitializeStage(stage, deckNos)
   │  BattleSettings に反映 ＋ EnemyScriptDirector を BattleWorld に渡す
   ▼
BattleWorld.Step  … 敵の出現・ボス出現条件はここで進む（エンドレスの湧きと同じ場所）
   ▼
PenguinWarsGameManager … 勝敗 → StageResult（★判定）→ CampaignProgress に保存 → リザルト
```

### 主な新規クラス（予定。名前は実装時に変えてよい。変えたら引き継ぎメモへ）

| 場所 | クラス | 役割 |
| --- | --- | --- |
| Battle | `StageDefinition` / `EnemySpawnEntry` | 1ステージの定義 / 敵の出方1行 |
| Battle | `StageDefinitions`（partial: `.Chapter1.cs` 〜 `.Chapter3.cs`） | 全ステージの定義表 |
| Battle | `EnemyScriptDirector` | 定義に従って敵を出す（`EnemyWaveDirector` の後継） |
| Battle | `StageResult` / `StarRule` | クリア結果から★を計算 |
| Battle | `CampaignProgress` | クリア状況・★・解放キャラ・最後の編成（純C#。保存形式は JSON 文字列） |
| Game | `CampaignSave` | `CampaignProgress` と `PlayerPrefs` の読み書き |
| UI | `StageSelectPanel` / `StageNode` / `StageDetailPanel` | ステージ選択 |
| UI | `DeckEditPanel` | 編成画面 |
| Editor | `StageSimulator` メニュー | 自動プレイ検証（Phase 5） |

### 規約（既存計画と同じ）

* SceneBuilder は `partial class` で分割（ステージ選択は `PenguinWarsSceneBuilder.Stage.cs`、編成は `.Deck.cs`）
* 1ファイル300行を目安に、超えそうなら役割で分ける
* 数値は定義表・`PenguinWarsBalance`・`[SerializeField]` に置く（マジックナンバー禁止）
* `Battle` 内の乱数は `System.Random`（シード指定可能）
* 共通基盤（`Assets/_Project/Common/`）は変更しない。必要になったらユーザーに確認する

### 参考にする既存コード

| 目的 | ファイル |
| --- | --- |
| 湧きの仕組み（置き換え元） | `Scripts/Battle/EnemyWaveDirector.cs`、`BattleWorld.cs` の `TickEnemyWaves` |
| 定義表の書き方 | `Scripts/Battle/UnitDefinitions.cs` |
| ステージで設定を上書きする書き方 | `Scripts/Data/PenguinStageData.cs` の `ApplyTo` |
| 試合の初期化 | `Scripts/Game/BattleRunner.cs`（`InitializeEndless` / `InitializeVersusHost`） |
| 進行とリザルト | `Scripts/Game/PenguinWarsGameManager.cs` |
| 一覧UI（スクロール・マス） | `Scripts/UI/PenguinZukanPanel.cs` / `ZukanCell.cs`、`Editor/PenguinWarsSceneBuilder.Zukan.cs` |
| PlayerPrefs 保存 | `Scripts/Game/EndlessRecord.cs`（Phase 1 で削除するので、先に書き方だけ見る） |
