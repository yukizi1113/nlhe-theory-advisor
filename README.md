# NLHE Theory Advisor

`Applications of No-Limit Hold'em` の理論を土台にした Windows フォームアプリです。  
軽い単体 WinForms 構成で、現在の状況を入れると推奨アクションを即座に返します。

## 何をするアプリか

- プリフロップは Janda の推奨表に沿って `open / flat / 3bet / 4bet` を照会
- ポストフロップは `pot odds / MDF / SPR / board texture / position / blocker / preflop range` をまとめて評価
- `check / call / fold / bet / raise / overbet / jam` の推奨を表示
- 理由と理論参照をあわせて出力

## 重要な前提

これは完全な GTO solver ではありません。  
本書の考え方を高速に入力できる形へ落とし込んだ、`均衡寄りのヒューリスティック・アドバイザー` です。

特に以下を重視しています。

- IP では call 防衛、OOP では再レイズ防衛が増える
- 乾いた board では超強ハンドの delayed raise が成立する
- ウェット board では strong but vulnerable hand を今の street で大きく取りにいく
- 相手 range が capped / condensed なら turn / river の large bet や overbet が機能しやすい
- river bluff は removal effect を強く重視する

## 画面構成

- `状況入力`: テーブル状況、ハンド、board、ベット額、SPR 前提
- `推奨`: 主推奨、代替ライン、根拠、指標
- `プリフロップ表`: Janda のチャート照会。13x13 グリッドをクリックして確認可能
- `理論ノート`: 実装で使った理論メモ

## ビルド

Windows の .NET Framework 4 系 `csc.exe` を使います。

```bat
build.bat
```

成功するとルートに `NLHETheoryAdvisor.exe` が出力されます。

## 使い方

1. `Street` と `Scenario` を選ぶ
2. `Hero Cards` と board を入力する
3. `Pot Size`, `Facing Bet`, `Effective Stack` を bb で入れる
4. `解析` を押す

### 用語

- `Villain Position`
  - ヘッズアップなら相手の位置
  - 3人以上なら、今その判断の基準にしたい主対象の相手。通常は現在ベットしている相手、または最後に強くアクションした相手
- `Street`
  - 現在の段階。`Preflop / Flop / Turn / River`
- `Scenario`
  - 今まさに何に直面しているか
  - `Checked To Hero`: 自分までチェックで回ってきた
  - `Facing Bet`: 相手からベットされた
  - `Facing Raise`: 自分のベット後にレイズ返しされた
- `Facing Bet`
  - その street で、今コールするために必要な額
- `Effective Stack`
  - 今後実際に取り切れる上限。通常は自分と主対象 Villain の残りスタックの小さい方

### プリフロップ表の見方

- 上三角は suited
- 下三角は offsuit
- 対角線は pocket pair
- セルをクリックすると右側に詳細が出る
- `Hand` 欄には `AKo`, `A5s`, `AsKd` のどれでも入力可能

## 実装メモ

- `src/PreflopCharts.cs`
  - Janda の推奨プリフロップ表
- `src/Evaluator.cs`
  - ハンド分類、draw 判定、board texture 判定
- `src/Engine.cs`
  - 推奨ロジック本体
- `src/MainForm.cs`
  - WinForms UI
