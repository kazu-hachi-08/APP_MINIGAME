# ペンギン大戦争 実装計画（INDEX）

仕様書 `Docs/06_PENGUIN_WARS_SPEC.md` を実装するための **一時的な** 計画書。全フェーズ完了後にフォルダごと削除する（Phase 12）。

---

## AIへの指示（「フェーズN進めて」と言われたら）

1. このINDEXを読み、下の「進捗」表で **フェーズNの前のフェーズがすべて完了** になっているか確認する。なっていなければ実装せずユーザーに伝える
2. `phaseNN_*.md`（対象フェーズのファイル）**だけ** を読む。他のフェーズのファイルは読まない（コンテキスト節約）
   * ただし直前フェーズの「引き継ぎメモ」は読む（前フェーズで決まったクラス名・仕様からの変更が書いてある）
3. フェーズファイルの「読むもの」に挙がった仕様書の節・既存コードだけを読む。仕様書は全部読まない
4. 「作るもの」を実装する。「やらないこと」には手を出さない（後のフェーズでやる）
5. 終わったら:
   * フェーズファイルの「引き継ぎメモ」を埋める（作ったファイル・公開API・仕様や計画から変えた点・次フェーズへの注意）
   * 下の「進捗」表を **完了** に更新する
   * ユーザーにフェーズファイルの「ユーザー確認手順」を伝える（AIはUnityエディタを操作できないので、Rebuild・再生確認はユーザーが行う）
6. ブランチ・コミット・PRはユーザーに言われたときだけ行う

計画と実装で食い違いが出たら、勝手に大きく変えずユーザーに確認する。小さな変更（クラス名・数値など）は引き継ぎメモに書けばよい。

---

## 進捗

| Phase | ファイル | 内容 | 終わったら遊べるもの | 状態 |
| --- | --- | --- | --- | --- |
| 1 | [phase01_scene_skeleton.md](phase01_scene_skeleton.md) | シーン骨組み・タイトル登録・戦場・カメラ | タイトルから入れて戦場をスクロールできる | 完了 |
| 2 | [phase02_battle_core.md](phase02_battle_core.md) | 戦闘ロジック（BattleWorld）・3体・敵の湧き・城HP | キーで出撃して敵と殴り合い、城が落ちたら終わる | 完了 |
| 3 | [phase03_economy_ui.md](phase03_economy_ui.md) | さかな・働きペンギン・再生産・キャラボタン | スマホのボタンで出撃できる | 完了 |
| 4 | [phase04_endless.md](phase04_endless.md) | 敵レベル・撃破報酬・ペンギン砲・リザルト・ベスト記録 | エンドレスが1本のゲームとして遊べる | 完了 |
| 5 | [phase05_abilities.md](phase05_abilities.md) | ノックバック・特殊能力5種・キャラ10体 | 10体のランダム編成で遊べる | 完了 |
| 6 | [phase06_pixel_art.md](phase06_pixel_art.md) | ペンギンのドット絵生成（パーツ方式）・城・背景 | 見た目がペンギンになる | 完了 |
| 7 | [phase07_effects_sound.md](phase07_effects_sound.md) | 演出・サウンド | 手触りが整う | 完了 |
| 8 | [phase08_unit_data50.md](phase08_unit_data50.md) | 50体のデータ・数値の計算式・ランダム編成の制約 | 50体から編成される（見た目は一部仮） | 完了 |
| 9 | [phase09_art50.md](phase09_art50.md) | 残り40体の見た目パーツ | 50体すべて見た目が揃う | 完了 |
| 10 | [phase10_online.md](phase10_online.md) | オンライン対戦（編成はランダム） | 友達と城攻め対戦できる | 完了 |
| 11 | [phase11_draft.md](phase11_draft.md) | ドラフト | 対戦前にドラフトできる | 完了 |
| 12 | [phase12_polish.md](phase12_polish.md) | バランス調整・仕様書反映・計画書削除 | 完成 | 未着手 |

状態: 未着手 / 進行中 / 完了

**なぜこの順番か:** 「ロジック → 遊べる → 見た目 → 量 → オンライン」の順。見た目や50体を先に作ると、戦闘ロジックを直すたびに手戻りが出るため。オンラインは最も壊れやすいので、ゲームが固まった後に回す。

---

## 全体設計（全フェーズ共通。各フェーズはこれに従う）

### 最重要: ロジックと表示を分ける

戦闘はすべて純C#の `BattleWorld` が計算し、MonoBehaviour は「入力を渡す」「状態を描く」だけにする。

```text
入力（ボタン / キー / CPU / オンラインのゲスト）
   │  BattleCommand（出撃・働きペンギン・ペンギン砲）
   ▼
BattleWorld.Step(dt)          ← 純C#。ユニット・城・お金・砲をすべて持つ
   │  状態（UnitState のリスト等）＋ BattleEvent（出撃・ヒット・撃破…）
   ▼
View（UnitView / CastleView / HUD / 音・演出）
```

**なぜ:** オンライン（Phase 10）ではホストだけが `BattleWorld` を動かし、ゲストは届いた状態を View に流すだけにする。最初からこの形にしておけば、Phase 10 は「通信を足すだけ」で済む。また EditMode テストで戦闘を検証できる。

### フォルダ

```text
Assets/_Project/Games/06_PenguinWars/
├ Scenes/PenguinWarsScene.unity          … SceneBuilderで生成（手で編集しない）
├ Data/                                   … ScriptableObjectアセット
├ Scripts/
│ ├ Battle/   MiniGame.PenguinWars.Battle.asmdef  … 純C#の戦闘ロジック（MonoBehaviour禁止）
│ ├ Data/     PenguinUnitData / PenguinUnitCatalog / PenguinWarsBalance（ScriptableObject）
│ ├ Game/     PenguinWarsGameManager / BattleRunner など（MonoBehaviour）
│ ├ View/     UnitView / CastleView / BattleCamera / スプライト生成 など
│ ├ UI/       ボタン・HUD・パネル
│ └ Online/   （Phase 10〜）
├ Editor/     SceneBuilder / アセット生成 / アート生成
└ Tests/Editor/  MiniGame.PenguinWars.Battle.Tests.asmdef
```

* namespace: `MiniGame.PenguinWars`（Battle は `MiniGame.PenguinWars.Battle`）
* `Battle` asmdef は ScriptableObject を参照しない。SO → `UnitStats`（純C#の構造体/クラス）に変換して渡す（テストでSOを作らずに済むため）
* `Battle` 内の乱数は `System.Random`（シード指定可能）を使い、`UnityEngine.Random` は使わない

### 規約

* SceneBuilder は1ファイルが巨大にならないよう `partial class` で分割する（`PenguinWarsSceneBuilder.cs` / `.Field.cs` / `.Hud.cs` …）。AIが一度に読む量を減らすため
* 1ファイル300行を目安に、超えそうなら役割で分ける
* 数値は `PenguinWarsBalance` か `[SerializeField]` に置く（CLAUDE.md のマジックナンバー禁止）
* 戦闘の時間進行は固定ステップ（1/30秒）。`BattleRunner` が `Update` で溜めた時間を固定ステップで `Step` する
* 座標: 戦場は X のみ。左城 X=0、右城 X=`fieldLength`。`Side.Left` は右へ、`Side.Right` は左へ進む
* 共通基盤（`Assets/_Project/Common/`）を変更するのは「タイトル登録」「画面向き」「シーン名」だけ。それ以外が必要になったらユーザーに確認する

### 参考にする既存コード（必要なフェーズで読む）

| 目的 | ファイル |
| --- | --- |
| ゲームマネージャーの基底 | `Assets/_Project/Common/Scripts/Core/BaseMiniGameManager.cs` |
| SceneBuilder の書き方・BuildSettings登録 | `Assets/_Project/Games/05_LifeGame/Editor/LifeGameSceneBuilder.cs`（大きいので該当箇所だけ grep して読む） |
| 純C#ロジック＋テストのasmdef構成 | `Assets/_Project/Games/04_Golf/Scripts/Simulation/` と `Tests/Editor/` |
| オンライン（名前付きメッセージ・ホスト権威） | `Assets/_Project/Games/01_Soccer/Scripts/Online/SoccerOnlineLink.cs` |
| ドット絵のコード生成 | `Assets/_Project/Games/01_Soccer/Editor/SoccerArtGenerator.cs` |
