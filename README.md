# NLHE Theory Advisor

`Applications of No-Limit Hold'em` の理論を土台にした Windows フォームアプリです。  
`twitcas-recorder` のように軽い単体 WinForms 構成で、現在の状況を入れると推奨アクションを即座に返します。

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
- `プリフロップ表`: Janda のチャート照会
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

## 実装メモ

- `src/PreflopCharts.cs`
  - Janda の推奨プリフロップ表
- `src/Evaluator.cs`
  - ハンド分類、draw 判定、board texture 判定
- `src/Engine.cs`
  - 推奨ロジック本体
- `src/MainForm.cs`
  - WinForms UI
