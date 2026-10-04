# Phase 7: 演出・サウンド

## ゴール

出撃・ヒット・撃破・ペンギン砲・レベルUP・城崩れに演出と音が付き、手触りが整う。

## 読むもの

* 仕様書: §9 UI・演出・サウンド、§2.1（城崩れ演出 1.5秒）
* 前フェーズの引き継ぎメモ
* 既存コード: `Common/Scripts/Audio/AudioManager.cs` の公開メソッド（`PlaySe` / `RegisterSe` / `PlaySeClip`）と `ProceduralSe.cs` の使い方。他ゲームの `*Audio.cs`（例 `05_LifeGame/Scripts/LifeAudio.cs`）を1つだけ参考に

## 作るもの

| ファイル | 内容 |
| --- | --- |
| `Scripts/View/BattleEventPresenter.cs` | `BattleEvent` を受けて演出と音に振り分ける唯一の入口（Phase 10 でゲストもイベントを受けてここに流す） |
| `Scripts/View/Effects/SpawnSmokeEffect.cs` | 出撃時の煙 |
| `Scripts/View/Effects/HitSparkEffect.cs` | ヒット時の白い小エフェクト |
| `Scripts/View/Effects/SoulRiseEffect.cs` | 撃破時に魂が昇る＋獲得さかな数字 |
| `Scripts/View/Effects/CannonBeamEffect.cs` | 青いビーム＋画面揺れ（`BattleCamera` に Shake を追加） |
| `Scripts/View/CastleCollapse.cs` | 城が揺れて崩れる（1.5秒）。終了処理はこの演出の後に |
| `Scripts/Game/PenguinWarsAudio.cs` | 出撃ポンッ・ヒットペチッ・撃破・砲・レベルUP・BGM |
| `Editor/Art/EffectArtGenerator.cs` | 煙・火花・魂のドット絵（Phase 6 の仕組みを流用） |

## 実装メモ

* エフェクトはプールする（エンドレス後半は1秒に何十回もヒットするため）
* ヒット音は同時発音数を絞る（同じフレームの同じ音は1回）
* 撃破の数字はプレイヤー側（自分が倒した）ときだけ出す

## やらないこと

新しいゲームルール。BGM の作曲（既存の `BgmId` から選ぶ）。

## 完了条件

* [x] 仕様 §9 の表の全場面に演出がある（コード上。出撃=煙＋ポンッ / ヒット=火花＋ペチッ / ノックバック=後ろに跳ねる / 撃破=魂＋獲得さかな / 砲=ビーム＋画面揺れ / LEVEL UP=表示＋ジングル / 城崩れ=揺れて潰れる＋煙＋地響き）
* [ ] 敵が大量にいてもカクつかない・音がうるさすぎない（再生での確認待ち）
* [ ] 城崩れ演出の後にリザルトが出る（再生での確認待ち）

## ユーザー確認手順

1. `Generate PenguinWars Art` → `Rebuild PenguinWars` → 再生してエンドレス後半まで遊ぶ

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * `Scripts/View/BattleEventPresenter.cs`（BattleEvent → 演出・音。`Present(BattleEvent)` は public。LEVEL UP 表示も GameManager からここへ移した）
  * `Scripts/View/Effects/`: `PooledEffect`（一定時間動いて自分で非表示になる土台）/ `EffectPool<T>`（非表示のものを使い回す。上限を超えたら null＝出さない）/ `SpawnSmokeEffect` / `HitSparkEffect` / `SoulRiseEffect`（魂＋TextMesh の数字と影）/ `CannonBeamEffect`
  * `Scripts/View/CastleCollapse.cs`（城と同じ GameObject。`Play(Action onFinished)`・`Duration`）
  * `Scripts/Game/PenguinWarsAudio.cs`（SE 6種を生成。素材を差し込める欄あり。同じ音は最短間隔で間引く〔ヒット 0.08秒・他 0.04秒〕）
  * `Editor/Art/EffectArtGenerator.cs`（煙・火花・魂・ビームを `Sprites/Effects/` に。`Generate PenguinWars Art` と Rebuild 時の `EnsureGenerated` から呼ばれる）
  * `Editor/PenguinWarsSceneBuilder.Effects.cs`（`Effects` オブジェクトに Presenter・Audio・演出の見本）
  * 変更: `BattleCamera`（`Shake(duration, strength)`・`LookAt(x)`。揺れはスクロール位置に混ざらないよう毎フレーム足して外す）、`UnitView`（ノックバック中に放物線で跳ねる `_knockbackHopHeight`）、`PenguinWarsGameManager`（自城崩壊で `BeginFinish`〔時間停止・BGM停止〕→ Presenter の `CastleCollapsed` でリザルト）、SceneBuilder（城に `CastleCollapse`、`.Effects` の呼び出し）、`PenguinArtGenerator`（演出の絵も生成）
* 公開API（次フェーズが使うもの）:
  * `BattleEventPresenter.Present(BattleEvent)` … Phase 10 でゲストは届いたイベントをここに流す。`CastleCollapsed`（Action<Side>）で崩れ終わりを通知
  * `PenguinWarsAudio.PlayBgm()` / `StopBgm()`、各 `PlayXxx()`
  * `BattleCamera.Shake` / `LookAt`
* 計画・仕様から変えた点:
  * BGM: プロジェクトに BGM 素材が無く、どのゲームも `BgmId` にクリップを登録していないため、`PenguinWarsAudio._bgmClip` の差し込み欄だけ用意した（未設定なら鳴らさない）。`PlayBgm(BgmId)` は未登録だと毎回警告が出るので使っていない
  * 出撃のポンッは自分の出撃だけ鳴らす（敵の湧きまで鳴らすと操作の手応えが埋もれるため）。煙は両方に出る
  * 城崩れはカメラを崩れる城へ移して見せ、画面も揺らす
  * 状態異常（止まる・遅い）は Phase 6 の色付けのまま（専用演出は作っていない）
* 次フェーズへの注意:
  * コンパイルは前フェーズと同じく一時プロジェクトで `dotnet build` しエラー0を確認。Unity での生成・再生は未確認
  * Presenter の `_localSide` は Left 固定（撃破の数字・出撃音の判定用）。Phase 10 でゲストは Right にする（readonly を外して設定メソッドを足す）
  * ビームの根元は「左=0 / 右=`Balance.FieldLength`」で求めている（BattleWorld を見ないのでゲストでも動く）
  * `UnitView` の跳ねる高さは「ノックバックに入った瞬間の `ActionTimer`」を全体の長さとして使う。ゲストに `ActionTimer` を送らない場合は跳ねない（位置は動くので許容範囲）
  * 演出の数の上限・位置・揺れの強さは `Effects` の Presenter、音量・間引き間隔は `PenguinWarsAudio` の Inspector で調整できる
