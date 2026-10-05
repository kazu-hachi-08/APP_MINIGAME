# Phase 7: ステージモードの演出とサウンド

## ゴール

ステージモードの山場（ボス登場・敵城を落とす・★がそろう・仲間が増える）に、それぞれ専用の手触りがある。

## 読むもの

* 前フェーズの引き継ぎメモ
* 仕様書: §9 UI・演出・サウンド
* 既存コード: `BattleEventPresenter.cs` / `PenguinWarsAudio.cs` / `PenguinBgmGenerator.cs` / `CastleCollapse.cs` / `Scripts/View/Effects/`（演出の作り方・プール）/ `BattleHud.cs` / `BattleCamera.cs`（画面揺れ）

## 作るもの

| 場面 | 演出 | 音 |
| --- | --- | --- |
| ボス登場 | 画面上部に赤い帯「WARNING!」が横に流れる（1.5秒）→ カメラが敵の城の前へ寄ってボスを見せてから戻る（0.8秒・操作は止めない）→ 画面揺れ | 警報音（生成SE）。ボスがいる間はプレイBGMを速く・低く（ピッチ変更で済ませる） |
| 敵の城が崩れる | 既存の城崩れを敵の城でも使う ＋ 紙吹雪（`EffectPool` の新しい演出） | 勝利のジングル（生成） |
| ★の獲得 | リザルトで★が左から1つずつポンと出る（0.3秒間隔）。新しく取った★は光って跳ねる | ★ごとに音の高さを上げる |
| ベスト更新 | `NEW RECORD!` が揺れる（既存のエンドレスの表示があれば流用） | — |
| 仲間になった | リザルトの後に1体ずつ「なかまになった！」カード。ペンギンが歩きコマで足踏み、押すと次へ | ファンファーレ（短い生成音） |
| ステージ選択 | 新しく遊べるようになったノードの鍵が外れる演出（初めて開いたときだけ）。遊べる最新ノードが上下にふわふわ | 鍵が外れる音 |
| 章クリア | ボスステージの初クリア後、ステージ選択で次の章へ自動でスクロールし「第2章 ゆきやまの奥」の題字を出す | — |

### 追加・変更するファイル（予定）

* `Scripts/View/Effects/ConfettiEffect.cs`、`Scripts/UI/BossWarningBanner.cs`、`Scripts/UI/StarRevealAnimator.cs`、`Scripts/UI/UnlockRevealPanel.cs`
* `PenguinWarsAudio` に SE を追加（生成音。素材を置けば差し替わる既存の仕組みに乗せる）
* `CampaignProgress` に「鍵を外す演出を見せたステージ」を記録（同じ演出を2回出さないため）

## 実装メモ

* 演出中もゲームの時間は止めない（ボス登場のカメラ移動中も出撃できる）。操作を奪うと理不尽に感じるため
* 演出の長さは `[SerializeField]` に置き、再生しながら調整できるようにする
* ボスが複数いる最終ボスでは、WARNING は毎回出すがカメラ移動は1回目だけ

## やらないこと

新しい絵素材（ボス専用のドット絵など）。音源ファイルの用意（ユーザーが後で置く）。

## 完了条件

* [ ] ボス登場・敵城撃破・★・仲間・鍵の演出がそれぞれ出る（コードは完成。エディタでの再生確認はまだ）
* [x] 演出のあいだも出撃・操作ができる（時間を止める処理・入力を止める処理は入れていない。カメラは操作されたら寄るのをやめる）
* [x] 同じステージを2回クリアしても、仲間・鍵の演出が2回出ない（仲間は初クリアのときだけ。鍵は `CampaignProgress` に記録し、EditMode テストで確認）

## ユーザー確認手順

1. `Rebuild PenguinWars` → セーブを消して `1-1`〜`1-6` を遊ぶ
2. 演出の長さ・うるささの感想を伝える（数値は Inspector で直せる）

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * View: `BossEntrancePresenter.cs`（ボス登場の流れ: WARNING 帯＋警報 → 1回目だけカメラが寄って戻る → 画面揺れ。ボスがいる間だけBGMをボス用にする） / `Effects/ConfettiEffect.cs`（紙吹雪1枚）
  * UI: `BossWarningBanner.cs`（赤い帯と流れる WARNING!） / `StarRevealAnimator.cs`（★を0.3秒ごとにポン。新しい★は光って跳ねる） / `UnlockRevealPanel.cs`（なかまになった！のカード。歩きコマで足踏み、押すと次へ） / `UiWobble.cs`（NEW RECORD! を揺らす）
  * Editor: `PenguinWarsSceneBuilder.StageResult.cs`（リザルトの生成を `.StageSelect.cs` から分けて、★3つ・NEW RECORD・仲間カードを足した）
  * 変更: `BattleCamera`（`Peek(x, 秒)`。スクロール・←→キーで中断） / `BattleEventPresenter`（ボス登場を `BossEntrancePresenter` に渡す・相手の城が崩れ始めたら紙吹雪と勝利ジングル） / `PenguinWarsAudio`（SE 5種: 警報・勝利・★・ファンファーレ・鍵。ボスBGM） / `PenguinBgmGenerator.CreateBoss`（戦闘曲を1.2倍速・3半音下げ） / `StageResultPanel`（`Show(StageResultContent, ...)` に変えた） / `StageSelectPanel`・`StageNode`（鍵外し・ふわふわ・章送り） / `StageLabels.Chapter`（「第2章 ゆきやまの奥」） / `CampaignProgress`（`NeedsUnlockReveal` / `MarkUnlockRevealed`。保存キー `revealed`） / `BossHpBar.FindBoss` を public に / `BattleHud` の仮の「ボス出現！」を削除 / `EffectArtGenerator`（`Confetti.png`）
* 計画から変えた点:
  * **ボスBGMはピッチ変更ではなく別のクリップに切り替える。** 共通の `AudioManager` にBGMのピッチを変える口が無く（Common は変えない方針）、ピッチを上げると「速く・高く」になって「低く」にできないため。生成音のときは `PenguinBgmGenerator.CreateBoss`（速く・低く）を鳴らす。`Audio/PenguinWars_BossBgm.*` を置けば Rebuild で差し込まれる。戦闘曲だけ素材を置いてボス曲が無いときは切り替えない（雰囲気が急に変わるため）
  * 勝利ジングルと紙吹雪は「城が崩れ終わったとき」ではなく「崩れ始めたとき」に出す。崩れ終わるとすぐリザルトが画面を覆い、紙吹雪が見えないため。クリア時の共通SE（`SeId.GameClear`）はジングルと重なるので鳴らさない（失敗の `GameOver` はそのまま）
  * 画面揺れはカメラが寄り切ったところ（寄り始めてから0.25秒）で揺らす。戻った後に揺らすと何が起きたか分かりにくいため
  * 鍵の演出は「ステージ選択を開いたとき」に出す。リザルトの「つぎのステージ」から直接遊んだステージは、遊び始めたときに演出済みとして記録する（後で出ても意味がないため）。最初のステージ（1-1）は最初から遊べるので出さない
  * 章送りは「鍵を外す次のステージが今の章より先の章にある」ときに出す（＝ボスを初めて倒してステージ選択に戻ったとき）。章の名前は `StageLabels` に置いた（定義表には章の名前の欄が無く、出すのは表示だけなので）。章のラベルも「第2章 ゆきやまの奥」にした
  * ステージ選択のマスは「並び用の外側」と「見た目とボタンの Body」に分けた（`HorizontalLayoutGroup` が位置を決めるので、ふわふわは Body だけを動かす）
  * ★はリザルトで3つの別々の文字にした（1つずつ跳ねさせるため）
* 次フェーズへの注意:
  * 仕様書 §9 の表に足すもの: ボス登場（WARNING・カメラ・警報・ボスBGM）・敵城撃破（紙吹雪・勝利ジングル）・★・NEW RECORD!・仲間カード・鍵・章送り。SE は「生成する6種」から11種になった。BGM の決まった名前に `PenguinWars_BossBgm` が増えた
  * 演出の長さ・数は Inspector で調整できる: `Effects`（`BossEntrancePresenter`・`BattleEventPresenter` の紙吹雪）、`SafeArea/BossWarning`、`StageResultPanel/Box/Stars`、`StageResultPanel/UnlockReveal`、`StageSelectPanel`（章送り）、`NodeTemplate`（ふわふわ・鍵）。Scene は Rebuild で作り直すので、気に入った値はコードの既定値に移すこと
  * Phase 6 までにセーブがある人は、初めてステージ選択を開いたときに遊べる全ステージの鍵演出がまとめて出る（今開いている章の分だけ見える）。一度だけなので対処していない
  * ビルド確認は Phase 6 と同じく一時プロジェクトで `dotnet build`（Battle・Tests・Assembly-CSharp・Editor）が通り、EditMode テストは簡易ランナーで 157 件すべて通過（追加4件: 鍵の演出の記録）。エディタでの Rebuild・再生確認はまだ
