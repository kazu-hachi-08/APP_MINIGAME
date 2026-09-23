# 02. カード効果の表現(効果DSL)

> ステータス: v1.0(Core 実装済み)/ 最終更新: 2026-09-21
> 方針: カードは C# を書かずにデータ(JSON)で定義する。ここに無い効果を作りたくなったら、まず本書に語彙を追加する。

## カード定義の形式

カードは `Core/CardGame.Core/Data/cards/*.json` に 1 枚 1 オブジェクトで定義する(ファイル分割はクラス単位)。

```json
{
  "id": "N001",
  "name": "見習い剣士",
  "class": "Neutral",
  "type": "Follower",
  "cost": 1,
  "attack": 1,
  "health": 2,
  "keywords": [],
  "effects": [
    { "trigger": "Fanfare", "actions": [ { "type": "Draw", "count": 1 } ] }
  ],
  "text": "ファンファーレ: カードを1枚引く。",
  "flavor": "剣を握って三日目。"
}
```

| フィールド | 必須 | 説明 |
|---|---|---|
| id | ○ | 一意 ID。クラス接頭辞 + 3 桁(N=Neutral, K=Knight, M=Mage, U=Necromancer, D=Druid, G=Dragon, T=トークン, X=テスト専用) |
| name | ○ | 表示名 |
| class | ○ | `Neutral` またはクラス名(03-cards.md 参照) |
| type | ○ | `Follower` / `Spell` / `Amulet` |
| cost | ○ | 0〜10 |
| attack / health | Follower のみ | 1 以上(攻撃 0 は可) |
| countdown | Amulet 任意 | 指定するとカウントダウンアミュレット |
| keywords | | キーワード能力の配列 |
| effects | | トリガー + アクション列 |
| text | ○ | 画面表示用テキスト(効果から自動生成せず手書き。ズレは rules-reviewer が検出) |
| flavor | | フレーバーテキスト |

## キーワード能力(初期セット)

| キーワード | コード | 効果 |
|---|---|---|
| 守護 | `Ward` | 相手はこのフォロワー以外(リーダー含む)を攻撃できない |
| 疾走 | `Storm` | 場に出たターンにリーダー・フォロワーを攻撃できる |
| 突進 | `Rush` | 場に出たターンにフォロワーを攻撃できる(リーダーは不可) |
| 必殺 | `Bane` | このフォロワーが**戦闘で**ダメージを与えたフォロワーを破壊する(攻撃側・防御側どちらでも発動。効果ダメージには反応しない) |
| ドレイン | `Drain` | このフォロワーが**戦闘で**与えたダメージ分、自分のリーダーを回復する(超過ダメージも含む。上限 20) |

フェーズ5候補: `Aura`(オーラ: 効果で選択されない)、`Ambush`(潜伏)、`Evolve` 時効果。

## トリガー

| トリガー | 発動タイミング | 対象カード |
|---|---|---|
| `Fanfare` | 手札からプレイして場に出たとき(効果で召喚された場合は発動しない) | Follower / Amulet |
| `LastWords` | 場から破壊されたとき(カウントダウン 0 も含む) | Follower / Amulet |
| `Spell` | スペルをプレイしたとき(スペル専用。省略可でスペルは暗黙にこのトリガー) | Spell |
| `TurnStart` | 自分のターン開始時(01-rules.md「開始フェーズ」の 4。ドロー・カウントダウンの後)。自分の場のカードの効果を**左から順に**キューへ積んで解決する | Follower / Amulet |

`TurnStart` の効果は `Select*` 対象を使えない(自動で決まる対象のみ)。ターン開始時に自身を破壊したカードのラストワードは、同じターン開始処理の中で解決される(効果で出たフォロワーはそのターン攻撃できない。突進・疾走を除く)。

フェーズ5候補: `TurnEnd`, `OnAttack`, `OnDamaged`, `OnAllySummoned`, `OnEvolve`

## アクション

各アクションは `type` と固有パラメータを持つ。`actions` 配列は上から順に完全に解決する。

| type | パラメータ | 説明 |
|---|---|---|
| `Damage` | target, amount | 対象に amount ダメージ(アミュレットはダメージを受けない) |
| `Heal` | target, amount | 対象の体力を amount 回復(上限まで) |
| `Draw` | count | 自分がカードを count 枚引く |
| `Destroy` | target | 対象フォロワー/アミュレットを破壊(リーダーが対象なら何もしない) |
| `Buff` | target, attack, health | 対象フォロワーを +attack/+health(負値可。体力 0 以下で破壊) |
| `Summon` | cardId, count | 指定カードのフォロワーを count 体、自分の場の**右端**に出す(場が満杯なら出せる分だけ。ファンファーレは発動しない) |
| `GainPP` | amount | このターンの PP を amount 回復(最大 PP は超えない) |
| `RampPP` | amount | 最大 PP を amount 増やし(上限 10)、このターンの PP も同じだけ増やす(永続的な PP 加速。ドルイド・竜族用) |

フェーズ5候補: `AddToHand`, `Discard`, `Transform`, `SetCountdown`, `Choose`(選択肢)

## 対象(target)

| target | 選択 | 意味 |
|---|---|---|
| `EnemyLeader` | 自動 | 相手リーダー |
| `AllyLeader` | 自動 | 自分リーダー |
| `Self` | 自動 | このカード自身(場にいるとき) |
| `SelectEnemyFollower` | プレイヤー | 相手フォロワー 1 体を選ぶ |
| `SelectAllyFollower` | プレイヤー | 自分フォロワー 1 体を選ぶ |
| `SelectFollower` | プレイヤー | いずれかのフォロワー 1 体を選ぶ |
| `SelectEnemy` | プレイヤー | 相手リーダーまたは相手フォロワー 1 体を選ぶ |
| `AllEnemyFollowers` | 自動 | 相手フォロワー全て |
| `AllAllyFollowers` | 自動 | 自分フォロワー全て(Self を含む) |
| `AllFollowers` | 自動 | 全フォロワー |
| `RandomEnemyFollower` | 自動(乱数) | 相手フォロワーからランダム 1 体 |
| `RandomAllyFollower` | 自動(乱数) | 自分フォロワーからランダム 1 体(Self 除く) |

### 対象選択のルール
- 1 枚のカードで `Select*` を使えるのは **1 種類まで**。複数アクションが `Select*` を持つ場合、同じ対象を共有する
- スペルの `Select*` に正当な対象がいない → **プレイ不可**
- フォロワー/アミュレットの Fanfare の `Select*` に正当な対象がいない → **その Fanfare 効果全体(全アクション)をスキップ**して場に出る(例:「味方 1 体を破壊し、自身を+3/+3」は味方がいなければ強化も起きない)
- `Random*` / `All*` に対象がいない → 何もしない

## 解決順序(重要。Core の実装と一致させる)

1. アクションを配列順に 1 つずつ解決する
2. 各アクションの後に **死亡チェック**: 体力 0 以下のフォロワー・カウントダウン 0 のアミュレット・破壊指定されたものを、**手番プレイヤーの場 → 相手の場**の順、それぞれ左から順に墓場へ送り、それぞれの `LastWords` を **キューの末尾**に積む
3. 現在のアクション列が全て終わったら、キューの先頭から次の効果を解決する(再帰しない)
4. リーダー HP の勝敗判定は、キューが空になった時点で行う(両者 0 以下なら引き分け)

## text の書き方(表記ゆれ防止)

- キーワードは「**守護**」のように単体で先頭に列挙し、改行して効果本文
- トリガーは「ファンファーレ: 」「ラストワード: 」で始める
- 数値は半角。「ダメージ」「回復」「引く」「破壊する」「+1/+1 する」を使う
- 例: `守護\nファンファーレ: 相手のフォロワー1体に2ダメージ。`

## サンプル(実カードから 5 種類)

```json
[
  { "id": "N002", "name": "森の番人", "class": "Neutral", "type": "Follower", "cost": 2, "attack": 1, "health": 3,
    "keywords": ["Ward"], "effects": [], "text": "守護" },

  { "id": "M012", "name": "火球", "class": "Mage", "type": "Spell", "cost": 3,
    "effects": [ { "trigger": "Spell", "actions": [ { "type": "Damage", "target": "SelectEnemy", "amount": 3 } ] } ],
    "text": "相手のリーダーかフォロワー1体に3ダメージ。" },

  { "id": "N004", "name": "旅の吟遊詩人", "class": "Neutral", "type": "Follower", "cost": 3, "attack": 2, "health": 2,
    "effects": [ { "trigger": "LastWords", "actions": [ { "type": "Draw", "count": 1 } ] } ],
    "text": "ラストワード: カードを1枚引く。" },

  { "id": "N005", "name": "軍旗", "class": "Neutral", "type": "Amulet", "cost": 2, "countdown": 2,
    "effects": [ { "trigger": "LastWords", "actions": [ { "type": "Buff", "target": "AllAllyFollowers", "attack": 1, "health": 1 } ] } ],
    "text": "カウントダウン 2\nラストワード: 自分のフォロワー全てを+1/+1する。" },

  { "id": "N006", "name": "狼の群れ", "class": "Neutral", "type": "Follower", "cost": 4, "attack": 2, "health": 2,
    "keywords": ["Rush"],
    "effects": [ { "trigger": "Fanfare", "actions": [ { "type": "Summon", "cardId": "T001", "count": 1 } ] } ],
    "text": "突進\nファンファーレ: 狼(2/2)を1体出す。" }
]
```

トークン(効果で出されるだけのカード)は `id` を `T` 始まりにし、デッキには入れられない(`token: true`)。
