# Theory Notes

このアプリは `Applications of No-Limit Hold'em` をそのまま solver 化したものではない。  
実装上は以下の理論を優先している。

## Core

- pot odds と MDF を起点にする
- ただし実戦判断は単体 hand ではなく `range vs range` で見る
- 高エクイティ hand が必ず高 EV とは限らない

## Preflop

- IP は flat 防衛が増える
- OOP は 3bet / 4bet 防衛が増える
- AK や QQ は常に最優先で re-raise する hand ではない

## Flop IP

- flop bet に対しては 60〜70% 程度の防衛が必要な spot が多い
- 乾いた flop で超強ハンドは delayed raise を取り得る
- ウェット flop では strong but vulnerable hand を raise 寄りにする

## Betting IP After Check

- まず 2〜3 street value を取れるかを考える
- 脆い marginal made hand は flop から取りにいく
- 脆くない marginal made hand は check back して later street thin value
- showdown value のない draw / overcard は早い street の bluff 候補

## OOP

- flop は bet / check-call / check-raise の配分が要点
- check-calling が難しい texture では check-raise を増やす
- 相手が any two で利益を出せないようにする

## Turn

- flop より bluff:value 比は value 寄り
- 相手が condensed なら large bet
- 相手が polarized なら small bet
- OOP draw は check-call より bet / check-raise の方が扱いやすい

## River

- equity が EV に直結しやすい street
- IP で thin value は call されたとき 50% 超勝てることが目安
- bluff は blocker / removal effect を強く重視
- missed draw が乏しいときは最弱の made hand を bluff 化する

## Overbet

- capped / condensed range に対して blank runout なら overbet が強い
- split が多い board や自分が calling range を block しすぎる spot では overbet を抑える

## Stack Depth

- shallow: absolute equity 重視
- deep: nut potential 重視
