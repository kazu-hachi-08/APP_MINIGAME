# ADR-0001: ゲームロジックを Unity から分離した純粋 C# ライブラリにする

日付: 2026-09-21 / ステータス: 採択

## 背景
Claude が実装・検証を担い、オーナーは実機テストでフィードバックする体制。
Claude は Unity エディタを対話的に操作できないため、ルールの正しさを Unity 無しで検証できる必要がある。
さらに AI 対戦・オンライン対戦・リプレイはいずれも「同じロジックを別の入力で回す」ことで実現したい。

## 決定
- `Core/CardGame.Core`(netstandard2.1)に全ゲームロジックを置き、`UnityEngine` を参照しない
- `Core/CardGame.Core.Tests`(NUnit)で `dotnet test` により検証する
- Unity プロジェクトは Core のソースを参照する(asmdef で分離)
- Core は決定論的に動作する(シード付き乱数、順序が保証されるコレクション)
- Core の入力は「コマンド」、出力は「イベント列」に統一する

## 結果
- 良: Unity 起動なしでテスト・playtest が回る。オンライン同期・リプレイが設計上ほぼ無料
- 悪: Unity 特有の便利機能(ScriptableObject でのカード定義など)を Core で使えない → JSON で定義する
- 悪: Core の API 設計に一手間かかる(イベント設計)
