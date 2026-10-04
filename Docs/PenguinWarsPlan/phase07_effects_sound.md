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

* [ ] 仕様 §9 の表の全場面に演出がある
* [ ] 敵が大量にいてもカクつかない・音がうるさすぎない
* [ ] 城崩れ演出の後にリザルトが出る

## ユーザー確認手順

1. `Generate PenguinWars Art` → `Rebuild PenguinWars` → 再生してエンドレス後半まで遊ぶ

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
* 公開API（次フェーズが使うもの）:
* 計画・仕様から変えた点:
* 次フェーズへの注意:
