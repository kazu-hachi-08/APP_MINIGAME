# 第3弾ミニゲーム：デジタルカードゲーム（THE CHAOS Ⅱ） 仕様書

別プロジェクトから移植したゲームで、仕様書の規模が大きいため、本ファイルには各仕様書へのリンクのみを置く。

コード: `Assets/_Project/Games/50_CardGame/`

---

## 仕様書

| No. | 仕様書 | 内容 |
| --- | --- | --- |
| 00 | [ゲーム概要](50_CardGame/spec/00-overview.md) | コンセプト、優先順位、やらないこと |
| 01 | [対戦ルール](50_CardGame/spec/01-rules.md) | ターン進行、戦闘、勝敗 |
| 02 | [カード効果の表現（効果DSL）](50_CardGame/spec/02-card-effects.md) | 効果DSL、キーワード能力 |
| 03 | [クラス設計と初期カードセット](50_CardGame/spec/03-cards.md) | クラス設計、カード一覧 |
| 04 | [画面仕様](50_CardGame/spec/04-screens.md) | 画面フローと各画面の要素 |
| 05 | [オンライン対戦](50_CardGame/spec/05-online.md) | オンライン対戦の仕組み |
| 06 | [ロードマップ](50_CardGame/spec/06-roadmap.md) | 開発フェーズと現在地 |
| 07 | [ドラフト](50_CardGame/spec/07-draft.md) | ドラフトモード |

---

## 関連資料

### 設計判断（ADR）

- [ADR-0001: ゲームロジックを Unity から分離した純粋 C# ライブラリにする](50_CardGame/adr/0001-core-unity-separation.md)
- [ADR-0002: オンライン対戦は Unity Relay + ホスト権威の決定論ロックステップで実現する](50_CardGame/adr/0002-online-relay-lockstep.md)

### アート

- [カードイラスト生成用リスト](50_CardGame/art/card-art-prompts.md)（[CSV](50_CardGame/art/card-art-prompts.csv)）
- [枠とフィールド背景の生成用プロンプト](50_CardGame/art/frame-and-field-prompts.md)
- [Gemini に依頼する素材リスト](50_CardGame/art/gemini-requests.md)

### プレイテスト記録

- [2026-09-21: 5 クラス化直後の初回計測](50_CardGame/playtest/2026-09-21-five-classes.md)
- [2026-09-22: 第 1 回バランス調整](50_CardGame/playtest/2026-09-22-balance-pass1.md)
- [2026-09-23: 竜の卵の作り直し](50_CardGame/playtest/2026-09-23-dragon-egg.md)

### AI開発ガイド（移植元）

- [CARDGAME_CLAUDE.md](50_CardGame/CARDGAME_CLAUDE.md)
  - 移植元プロジェクト用のガイドのため、Git運用・リポジトリ構成などは本プロジェクトの `CLAUDE.md` を優先する
