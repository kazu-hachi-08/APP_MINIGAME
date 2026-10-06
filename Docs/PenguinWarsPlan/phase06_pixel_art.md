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
* 生成した PNG は `100_PenguinWars/Sprites/` に置く。生成物はコミットする（他ゲームと同じ）
* Phase 9 で追加するパーツが入るよう、パーツは enum ではなく ID 文字列 → パターン辞書にしておくと追加が楽

## やらないこと

残り40体分のパーツ（Phase 9）、エフェクト類（Phase 7）。

## 完了条件

* [ ] 生成メニューで PNG が出力され、Sprite として import される（Unity エディタでの確認待ち）
* [x] 10体が見た目で区別できる。敵は赤系（生成ロジックを dotnet で動かしたプレビュー画像で確認）
* [ ] 歩き・攻撃・ノックバックでコマが変わる（再生での確認待ち）
* [ ] 城・ゲート・背景・ボタンアイコンが仮の四角ではなくなる（再生での確認待ち）

## ユーザー確認手順

1. `Generate PenguinWars Art` → `Rebuild PenguinWars` → 再生
2. 見た目の好みはここで指摘する（パターン定義の修正で直る）

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
  * `Scripts/Data/PenguinLook.cs`（体の形・体色・頭・手・背中・拡大率を ID 文字列で持つ。`IsDefault`）
  * `Scripts/View/UnitSpriteSet.cs`（1キャラ1陣営分。`PenguinFrame` enum〔Walk0 / Walk1 / AttackWindup / AttackStrike / Knockback〕も同居。`HeadHeight` は HPバーの高さ用に生成時に測る）
  * `Editor/Art/`: `PenguinArtGenerator`（メニュー `Generate PenguinWars Art`、`EnsureGenerated`）/ `PenguinFrameComposer`（基本体＋パーツの重ね描き、コマごとのずらし）/ `PenguinBodyPatterns` / `PenguinPartPatterns` / `PenguinPalette` / `FieldArtGenerator`（城青赤・ゲート・地面・山・空）/ `PenguinSpriteWriter`（PNG 書き出しとインポート設定。計画に無いが、キャラと戦場で共通のため分けた）
  * 変更: `PenguinUnitData`（`_look`・`_leftSprites`・`_rightSprites`・`GetSprites(Side)`）、`PenguinUnitAssetGenerator`（No→見た目の表 `Looks`）、`UnitView`（コマ選択・右陣営は flipX・HPバーを頭の上へ）、`UnitViewPool`（カタログから絵を引いて View に渡す）、`CastleView`（`HideHp` → `ShowAsGate`。右の城はエンドレスでゲートの絵に差し替え）、`UnitButton` / `UnitButtonBar` / `DeckIntroPanel`（アイコンを立ち姿のスプライトに。仮色の `UnitButton.IconColor` は削除）、SceneBuilder（`.Field` は空・山・地面・城をスプライトで、`.Battle` はユニットの見本を SpriteRenderer に、Rebuild 時に `EnsureGenerated`）
* 公開API（次フェーズが使うもの）:
  * `PenguinUnitData.GetSprites(side).Icon` / `.Get(PenguinFrame)`（Phase 11 のドラフトのカードに使える）
  * `PenguinUnitData.Look`
  * `FieldArtGenerator.CastlePath(side)` などのパス、`PenguinSpriteWriter.PixelsPerUnit = 16`
* 用意したパーツID一覧（Phase 9 で使う）:
  * 体の形: `basic`（16x19）/ `wide`（22x17）/ `tall`（16x26）
  * 体色: `standard` / `ice` / `gray`
  * 頭: `helmet` ／ 手: `axe` / `glove` / `bow` ／ 背中・乗り物: `freezer`（体の奥）/ `bike`（足元に付き、体を5ドット持ち上げて足を隠す）
  * 拡大率: 1〜3（PNG は 32x32 のまま Pixels Per Unit を 16/拡大率 にして大きく見せる）
  * パターンの書き方: 体は右向き・最後の1行が足。目印 `H`=頭・`A`=手・`R`=背中。パーツは `Anchor`（列, 上からの行）を体の目印に重ねる。色文字は `PenguinPalette`（`k`=体の暗い部分、`t`/`u`=チーム色 ← 敵味方で差し替わる）
* 計画・仕様から変えた点:
  * `UnitSpriteSet` は ScriptableObject にせず、`PenguinUnitData` の中に左右2つ持たせた（キャラNo から1回で引けて、アセットも増えない）。生成メニューが参照を書き込む。参照は .meta の GUID 固定なので、作り直しても差分は出ない
  * 1コマ1PNG（`Sprites/Units/Unit_001_Left_Walk0.png` など。10体で100枚）。Unity 6 ではスプライトシートの分割 API が非推奨のため
  * コマの作り方: 歩き2コマ目=体を1ドット跳ねて足を前へ、攻撃=のけぞって持ち物を振り上げる → 前に乗り出して振り下ろす（持ち物のずらし）、ノックバック=歩き1コマ目を後ろ向きに90度倒した絵
  * 攻撃後の硬直は最初の 0.25 秒だけ振り下ろしのコマ、その後は立ち姿（`UnitView._strikeHoldSeconds`）。止められている間は足踏みしない
  * 状態異常の色（止まる=水色・遅い=灰色）は絵に色を掛ける形で残した。攻撃発生待ちの「白っぽくする」仮表現は攻撃コマができたので削除
  * 見た目は「新規作成時」か「既存アセットの見た目が既定（基本ペンギン）のまま」のときだけ書き込む（数値と同じく、手で変えた見た目を消さないため）
  * 空は単色だったカメラ背景に加え、グラデーションの空・遠くの山を敷いた
* 次フェーズへの注意:
  * コンパイルは前フェーズと同じく生成済み csproj を複製した一時プロジェクトで `dotnet build` しエラー0を確認。絵は生成ロジックを dotnet で動かしてプレビュー画像で確認（Unity エディタでの生成・Rebuild・再生は未確認）
  * `UnitView` は攻撃コマの判定に `UnitState.ActionTimer` と `Stats.AttackInterval / Windup` を使う。Phase 10 でゲストに同期するときは状態（歩き/発生待ち/硬直/ノックバック）に加えて残り時間も送るか、ゲスト側は硬直中ずっと立ち姿にするなど割り切る
  * Phase 7 の演出（ヒットエフェクト・煙など）は `PenguinSpriteWriter.Save` で同じように PNG を作れる
  * Phase 9: 新しいパーツは `PenguinPartPatterns` / `PenguinBodyPatterns` に1項目、見た目の指定は `PenguinUnitAssetGenerator.Looks` に1行足して `Generate PenguinWars Art`
