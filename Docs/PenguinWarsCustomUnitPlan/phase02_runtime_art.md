# Phase 2: ドット絵の合成を実行時へ・実行時のキャラデータ登録

## ゴール

見た目ID（体・体色・頭・手・背中）から、実行時に5コマ×2陣営の `UnitSpriteSet` が作れる。作ったデータを `PenguinUnitCatalog` に実行時登録すると、`catalog.Get(no)` で既存キャラと同じように取れる。既存50体の PNG は Rebuild しても1枚も変わらない。

## 読むもの

* INDEX の「全体設計」
* Phase 1 の引き継ぎメモ
* 仕様書: §7.2 ペンギンの見た目
* 既存コード: `Editor/Art/PenguinArtGenerator.cs` / `PenguinFrameComposer.cs` / `PenguinBodyPatterns.cs` / `PenguinPartPatterns*.cs` / `PenguinPalette.cs` / `PenguinSpriteWriter.cs`（PPU とピボットの決め方だけ）、`Scripts/View/UnitSpriteSet.cs`、`Scripts/Data/PenguinUnitData.cs` / `PenguinUnitCatalog.cs` / `PenguinLook.cs`

## 作るもの

### ドット絵の合成を移す

| 変更 | 内容 |
| --- | --- |
| `Editor/Art/` → `Scripts/Art/` | `PenguinFrameComposer` / `PenguinBodyPatterns` / `PenguinPartPatterns`（partial 全部） / `PenguinPalette` を移す。**`git mv` で `.meta` ごと移す**（GUID を保つ）。namespace は `MiniGame.PenguinWars.Art` などに変える |
| 残すもの | `PenguinArtGenerator` / `PenguinSpriteWriter` / `FieldArtGenerator` / `EffectArtGenerator` は Editor のまま（PNG を書く・AssetDatabase を使うため）。移した合成を呼ぶように using を直す |
| 移す前の確認 | 移すファイルに `UnityEditor` の参照が無いこと（調べた時点では無い）。あれば Editor に残す部分と分ける |

### 実行時の絵とデータ

| ファイル | 内容 |
| --- | --- |
| `Scripts/Art/PenguinPartCatalog.cs`（新規） | 部位ごとの選べる ID の一覧（体の形・体色・頭・手・背中）。`PenguinBodyPatterns.Shapes` / パレット / パーツの辞書のキーから作る。`IsKnown(slot, id)`。Phase 3 の◀▶と、見た目IDの掃除に使う |
| `Scripts/View/RuntimeUnitSprites.cs`（新規） | `Build(PenguinLook look, Side side) → UnitSpriteSet`。`PenguinFrameComposer.Compose` の結果を `Texture2D`（`FilterMode.Point`・`TextureWrapMode.Clamp`）に入れ、`Sprite.Create`（PPU = 16 / 拡大率、ピボット = 足元中央）。頭の高さは `PenguinArtGenerator` と同じ測り方（測る処理は合成側へ寄せて両方から使う） |
| `UnitSpriteSet`（変更） | 実行時に作るためのコンストラクタ（`Sprite[] frames, float headHeight`）を足す |
| `PenguinUnitData`（変更） | `CreateRuntime(UnitStats stats, string name, PenguinLook look, UnitSpriteSet left, UnitSpriteSet right)`（`ScriptableObject.CreateInstance` で作り、private フィールドに直接入れる static メソッド） |
| `PenguinUnitCatalog`（変更） | `RegisterRuntime(PenguinUnitData)` / `ClearRuntime()`。`Get(no)` は実行時登録を先に見る。実行時登録は `[NonSerialized]` の辞書（アセットに残さない）。`Units`（50体の一覧）には **含めない**（ずかん・ドラフトの候補に混ざらないように） |
| `Scripts/Game/CustomUnitFactory.cs`（新規） | `Create(CustomUnitDefinition def, int no) → PenguinUnitData`。Phase 1 の `ToUnitDefinition` → `Calculate` → 見た目ID を `PenguinPartCatalog` で掃除（知らない ID は空）→ 大型なら拡大率2 → 両陣営の絵 → `CreateRuntime`。作成画面（Phase 3）と対戦（Phase 5）の両方がこれを使う |

### 後片付け

* 作った `Texture2D` / `Sprite` / `PenguinUnitData` は `ClearRuntime` で `Destroy` する（作成画面でパーツを変えるたびに作り直すので、捨てないとメモリが増え続ける）

### Tests

* 純C#ではないので EditMode テストは任意。作るなら「`RuntimeUnitSprites.Build` が5コマ全部そろった `UnitSpriteSet` を返す」「`RegisterRuntime` した No が `Get` で取れ、`ClearRuntime` で消える」（`MiniGame.PenguinWars` を参照できるテスト asmdef が要るなら、無理に作らず再生確認で済ませる）

## 実装メモ

* 移動で一番怖いのは「絵が変わる」こと。移した直後に `Rebuild PenguinWars` → `git status` で `Sprites/Units/` に差分が出ないことを確かめる（同じ定義からは同じ PNG になるので、差分0が正しい）
* パーツの辞書が private なら、キー一覧を返す読み取り専用プロパティを足す
* 1体あたり 32×32 × 5コマ × 2陣営 = 10枚の小さいテクスチャなので、作成画面で◀▶を押すたびに作り直しても重くない。重ければ「押してから 0.1 秒待って作る」でよい

## やらないこと

作成画面（Phase 3）、対戦での登録（Phase 5）、パーツの日本語名（Phase 3）。新しいパーツの追加。

## 完了条件

* [ ] `Rebuild PenguinWars` 後に `Sprites/Units/` の PNG に差分が無い
* [ ] 既存の EditMode テストが通る
* [ ] 仮のデバッグ用コード（例: 再生中に右クリックメニュー、またはテスト用の一時 MonoBehaviour）で、お手本の じぶんナイト を `CustomUnitFactory` で作って `RegisterRuntime` し、`BattleRunner.SpawnDemoUnit` などで戦場に出すと、歩く・攻撃する・ノックバックする絵が出る（確認後、仮コードは消す）

## ユーザー確認手順

1. `Tools > MiniGame > Rebuild PenguinWars` → Git の差分で PNG が変わっていないことを確認
2. 再生 → ずかん・ステージ・あそびかたのデモで既存キャラの絵がいつもどおりか
3. （仮コードがあれば）じぶんナイトが戦場で動くのを見る

## 引き継ぎメモ（実装後に記入）

* 作ったファイル:
* 公開API:
* 計画から変えた点:
* 次フェーズへの注意:
