# Phase 6: ペンギンのドット絵生成（パーツ方式）・城・背景

## ゴール

Phase 5 の10体がペンギンの見た目になる（味方=青系 / 敵=赤系の色違い）。歩き・攻撃・ノックバックのコマが切り替わる。城・出現ゲート・背景・キャラボタンのアイコンも本番の見た目になる。

## 読むもの

* 仕様書: §7.2 ペンギンの見た目、§7.3 城、§5.5 キャラ一覧の「見た目」列（Phase 5 の10体分）
* 前フェーズの引き継ぎメモ
* 既存コード: `01_Soccer/Editor/SoccerArtGenerator.cs`（PNG を書き出して Sprite として import する流れだけ。全部は読まない）

## 作るもの

| ファイル | 内容 |
| --- | --- |
| `Scripts/Data/PenguinLook.cs` | 見た目指定（シリアライズ可能な class）: 体の形 / 体色 / 頭パーツ / 手パーツ / 背中・乗り物パーツ / 拡大率 |
| `PenguinUnitData` | `Look` を追加 |
| `Editor/Art/PenguinArtGenerator.cs` | `Tools > MiniGame > Generate PenguinWars Art`。全キャラ×陣営2色×コマ（歩き2・攻撃2・ノックバック1）を PNG に書き出す |
| `Editor/Art/PenguinBodyPatterns.cs` | 体の形ごとのドットパターン（文字列配列で定義） |
| `Editor/Art/PenguinPartPatterns.cs` | 頭・手・背中パーツのドットパターンと取り付け位置 |
| `Editor/Art/PenguinPalette.cs` | 味方（青）/ 敵（赤）のパレット。体色指定と組み合わせて色を決める |
| `Editor/Art/FieldArtGenerator.cs` | 氷の城（青・赤）・洞窟ゲート・地面・空の背景 |
| `Scripts/View/UnitSpriteSet.cs` | 1キャラ1陣営分のスプライト参照（ScriptableObject か カタログに持たせる） |
| `UnitView` | 状態（Walk / Windup・攻撃 / Knockback）でコマ切替。右陣営は左右反転 |
| `UnitButton` | アイコンを本番スプライトに |

## 実装メモ

* **パーツは「基本体＋重ね描き」**。1キャラを丸ごと描かない（Phase 9 で40体を足すため）
* ドットの基本サイズは 32×32（大型は拡大率で 2倍・3倍に引き伸ばす。FilterMode = Point）
* 敵味方の違いは「黒い部分 → 青黒 / 赤黒」のパレット差し替えだけにする
* 生成した PNG は `06_PenguinWars/Sprites/` に置く。生成物はコミットする（他ゲームと同じ）
* Phase 9 で追加するパーツが入るよう、パーツは enum ではなく ID 文字列 → パターン辞書にしておくと追加が楽

## やらないこと

残り40体分のパーツ（Phase 9）、エフェクト類（Phase 7）。

## 完了条件

* [ ] 生成メニューで PNG が出力され、Sprite として import される
* [ ] 10体が見た目で区別できる。敵は赤系
* [ ] 歩き・攻撃・ノックバックでコマが変わる
* [ ] 城・ゲート・背景・ボタンアイコンが仮の四角ではなくなる

## ユーザー確認手順

1. `Generate PenguinWars Art` → `Rebuild PenguinWars` → 再生
2. 見た目の好みはここで指摘する（パターン定義の修正で直る）

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
* 公開API（次フェーズが使うもの）:
* 用意したパーツID一覧（Phase 9 で使う）:
* 計画・仕様から変えた点:
* 次フェーズへの注意:
