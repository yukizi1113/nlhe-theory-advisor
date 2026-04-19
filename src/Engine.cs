using System;
using System.Collections.Generic;
using System.Globalization;

namespace NLHETheoryAdvisor
{
    static class RecommendationEngine
    {
        public static Recommendation Analyze(ScenarioInput input)
        {
            if (input == null)
            {
                var invalid = new Recommendation();
                invalid.PrimaryAction = "入力エラー";
                invalid.Reasons.Add("状況入力を読み取れませんでした。");
                return invalid;
            }

            if (input.Street == Street.Preflop
                || input.Scenario == ScenarioType.Unopened
                || input.Scenario == ScenarioType.FacingOpen
                || input.Scenario == ScenarioType.Facing3Bet)
            {
                return AnalyzePreflop(input);
            }

            return AnalyzePostflop(input);
        }

        private static Recommendation AnalyzePreflop(ScenarioInput input)
        {
            var recommendation = new Recommendation();
            var lookup = PreflopCharts.Analyze(
                input.HeroPosition,
                input.VillainPosition,
                input.Scenario,
                input.HeroHandCode(),
                input.EffectiveStack,
                input.Players);

            recommendation.PrimaryAction = lookup.Action;
            recommendation.SecondaryAction = lookup.SecondaryAction;
            recommendation.Summary = "Janda 推奨レンジに基づくプリフロップ近似です。";
            recommendation.Confidence = "レンジ準拠";
            recommendation.Reasons.AddRange(lookup.Notes);
            recommendation.TheoryReferences.Add("Part Two: Preflop Play, pp.49-102");
            recommendation.TheoryReferences.Add("Recommended Hand Chart, pp.95-101");
            recommendation.AddMetric("Hero Hand", input.HeroHandCode());
            recommendation.AddMetric("Spot", lookup.SpotLabel);
            recommendation.AddMetric("Stack", FormatNumber(input.EffectiveStack) + " bb");
            recommendation.AddMetric("Players", input.Players.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(lookup.RangeSummary))
            {
                recommendation.AddMetric("Chart", lookup.RangeSummary);
            }
            return recommendation;
        }

        private static Recommendation AnalyzePostflop(ScenarioInput input)
        {
            var recommendation = new Recommendation();
            var board = input.GetBoard();
            var previousBoard = input.GetPreviousStreetBoard();
            var boardAnalysis = PokerEvaluator.AnalyzeBoard(board, previousBoard);
            var handAnalysis = PokerEvaluator.AnalyzeHand(input.HeroCards, board, input.Street);
            var rangeMetrics = BuildRangeMetrics(input, board);
            var rangeShape = InferRangeShape(input, boardAnalysis);
            bool villainCapped = input.Scenario == ScenarioType.CheckedToHero
                || (boardAnalysis.LatestCardIsBlank && rangeMetrics.HeroNutPct > rangeMetrics.VillainNutPct + 4.0);
            bool multiway = input.Players > 2 || input.PotType == PotType.Multiway;
            double potOdds = CalculateCallEquity(input.PotSize, input.FacingBetSize);
            double mdf = CalculateMinimumDefenseFrequency(input.PotSize, input.FacingBetSize);
            double spr = input.GetSpr();

            recommendation.AddMetric("Hero Hand", handAnalysis.DetailName);
            recommendation.AddMetric("Category", handAnalysis.CategoryName);
            recommendation.AddMetric("Board", boardAnalysis.TextureLabel + " / " + boardAnalysis.DetailLabel);
            recommendation.AddMetric("SPR", FormatNumber(spr));
            recommendation.AddMetric("Pot Odds", input.FacingBetSize > 0.0 ? Percent(potOdds) : "-");
            recommendation.AddMetric("MDF", input.FacingBetSize > 0.0 ? Percent(mdf) : "-");
            recommendation.AddMetric("Hero Strong", Percent(rangeMetrics.HeroStrongPct / 100.0));
            recommendation.AddMetric("Villain Strong", Percent(rangeMetrics.VillainStrongPct / 100.0));
            recommendation.AddMetric("Hero Nut", Percent(rangeMetrics.HeroNutPct / 100.0));
            recommendation.AddMetric("Villain Nut", Percent(rangeMetrics.VillainNutPct / 100.0));
            recommendation.AddMetric("Villain Shape", ToJapanese(rangeShape));
            if (!string.IsNullOrWhiteSpace(rangeMetrics.SpotLabel))
            {
                recommendation.AddMetric("Range Spot", rangeMetrics.SpotLabel);
            }

            switch (input.Scenario)
            {
                case ScenarioType.CheckedToHero:
                    ScoreWhenCheckedTo(
                        input,
                        recommendation,
                        handAnalysis,
                        boardAnalysis,
                        rangeMetrics,
                        rangeShape,
                        villainCapped,
                        spr,
                        multiway);
                    break;

                case ScenarioType.FacingBet:
                    ScoreFacingBet(
                        input,
                        recommendation,
                        handAnalysis,
                        boardAnalysis,
                        rangeMetrics,
                        rangeShape,
                        villainCapped,
                        spr,
                        potOdds,
                        mdf,
                        multiway);
                    break;

                case ScenarioType.FacingRaise:
                    ScoreFacingRaise(
                        input,
                        recommendation,
                        handAnalysis,
                        boardAnalysis,
                        rangeShape,
                        spr,
                        multiway);
                    break;

                default:
                    recommendation.PrimaryAction = "チェック";
                    recommendation.Summary = "この状況タイプは簡易フォールバックです。";
                    recommendation.Reasons.Add("現在のバージョンではこの postflop path を簡易扱いしています。");
                    break;
            }

            if (recommendation.Reasons.Count == 0)
            {
                recommendation.Reasons.Add("理論上の近似ロジックから最も自然なラインを返しています。");
            }

            recommendation.Confidence = BuildConfidence(recommendation);
            return recommendation;
        }

        private static void ScoreWhenCheckedTo(
            ScenarioInput input,
            Recommendation recommendation,
            HandAnalysis hand,
            BoardAnalysis board,
            RangeMetrics ranges,
            RangeShape rangeShape,
            bool villainCapped,
            double spr,
            bool multiway)
        {
            var scores = CreateScoreMap(new[]
            {
                ActionClass.Check, ActionClass.BetSmall, ActionClass.BetMedium,
                ActionClass.BetLarge, ActionClass.Overbet, ActionClass.Jam
            });

            AddScore(scores, ActionClass.Check, 32);
            AddScore(scores, ActionClass.BetSmall, 18);
            AddScore(scores, ActionClass.BetMedium, 18);
            AddScore(scores, ActionClass.BetLarge, 12);
            AddScore(scores, ActionClass.Overbet, 4);

            if (hand.IsMonster)
            {
                AddScore(scores, ActionClass.BetLarge, 36);
                AddScore(scores, ActionClass.BetMedium, 16);
                EnsureReason(recommendation, "レンジ上位の強ハンドなので value を強く取りにいきます。");

                if (board.IsDry && input.Street == Street.Flop && input.HeroHasPosition && hand.VulnerabilityScore < 35)
                {
                    AddScore(scores, ActionClass.Check, 18);
                    EnsureReason(recommendation, "乾いた flop のナッツ級は check back で後続 street に raise を遅らせるラインも成立します。");
                    AddTheoryReference(recommendation, "Delaying a Raise on a Dry Board, p.140");
                }

                if (board.IsWet || hand.VulnerabilityScore >= 55)
                {
                    AddScore(scores, ActionClass.BetLarge, 18);
                    AddScore(scores, ActionClass.Check, -10);
                    EnsureReason(recommendation, "ウェットで脆い強ハンドは今の street で大きく取る方が理論に沿います。");
                    AddTheoryReference(recommendation, "Delaying a Raise on a Wet Flop, p.143");
                }

                if ((input.Street == Street.Turn || input.Street == Street.River) && villainCapped)
                {
                    AddScore(scores, ActionClass.Overbet, 26);
                    EnsureReason(recommendation, "相手レンジが capped / condensed なら blank runout で overbet が有効です。");
                    AddTheoryReference(recommendation, "Overbetting the River, pp.369-372");
                }
            }
            else if (hand.MadeStrengthScore >= 68)
            {
                AddScore(scores, ActionClass.BetMedium, 28);
                AddScore(scores, ActionClass.BetLarge, board.IsWet ? 14 : 6);
                EnsureReason(recommendation, "2 street 以上の value を取りやすい made hand です。");

                if (!board.IsWet && input.HeroHasPosition && input.Street == Street.Flop && hand.ShowdownScore >= 60)
                {
                    AddScore(scores, ActionClass.Check, 12);
                    EnsureReason(recommendation, "乾いた board の medium-strong hand は check back で later street thin value に回す候補もあります。");
                    AddTheoryReference(recommendation, "Thought Process for Deciding Whether to Bet or Check in Position, pp.186-188");
                }
            }
            else if (hand.MadeStrengthScore >= 48)
            {
                if (board.IsWet || hand.VulnerabilityScore >= 50)
                {
                    AddScore(scores, ActionClass.BetSmall, 18);
                    AddScore(scores, ActionClass.BetMedium, 8);
                    EnsureReason(recommendation, "脆い marginal made hand は flop から薄く取りつつ保護する価値があります。");
                }
                else
                {
                    AddScore(scores, ActionClass.Check, 22);
                    EnsureReason(recommendation, "脆くない marginal made hand は check back で later street thin value に回しやすいです。");
                }

                if ((input.Street == Street.Turn || input.Street == Street.River) && villainCapped)
                {
                    AddScore(scores, ActionClass.BetSmall, 16);
                    EnsureReason(recommendation, "相手が capped なら small bet で wide weak range から拾いやすくなります。");
                }
            }
            else if (hand.IsStrongDraw)
            {
                AddScore(scores, ActionClass.BetMedium, 20);
                AddScore(scores, ActionClass.BetLarge, hand.ShowdownScore < 20 ? 10 : 4);
                EnsureReason(recommendation, "showdown value が薄く equity を保ちやすい draw は早い street の bluff 候補です。");
                AddTheoryReference(recommendation, "Bluffing with the Right Hands on the Flop, p.177");
            }
            else if (hand.DrawScore >= 30)
            {
                AddScore(scores, ActionClass.BetSmall, 16);
                AddScore(scores, ActionClass.Check, hand.ShowdownScore > 25 ? 8 : 0);
                EnsureReason(recommendation, "中程度の draw は小さく fold equity を取りにいくか、showdown value があれば check が混ざります。");
            }
            else
            {
                if (hand.ShowdownScore >= 30)
                {
                    AddScore(scores, ActionClass.Check, 26);
                    EnsureReason(recommendation, "showdown value を持つ弱い hand は無理に bluff に回しません。");
                }
                else if (villainCapped && hand.HasNutBlocker && input.Street == Street.River)
                {
                    AddScore(scores, ActionClass.BetLarge, 14);
                    AddScore(scores, ActionClass.Overbet, 10);
                    EnsureReason(recommendation, "river の blocker は bluff の主要条件です。call range を block できるなら bluff 候補になります。");
                    AddTheoryReference(recommendation, "Utilizing Removal Effects, pp.360-364");
                }
                else
                {
                    AddScore(scores, ActionClass.Check, 14);
                }
            }

            if (rangeShape == RangeShape.Polarized)
            {
                AddScore(scores, ActionClass.BetSmall, 10);
                AddScore(scores, ActionClass.BetLarge, -4);
                EnsureReason(recommendation, "相手レンジが polarized なら small bet で free card を防ぎつつ raise の威力を落とせます。");
                AddTheoryReference(recommendation, "Post-flop Bet Sizing at a Glance, pp.106-110");
            }
            else if (rangeShape == RangeShape.Condensed)
            {
                AddScore(scores, ActionClass.BetLarge, 12);
                AddScore(scores, ActionClass.Overbet, 8);
                EnsureReason(recommendation, "相手レンジが condensed なら大きい sizing が medium strength に圧力をかけやすいです。");
                AddTheoryReference(recommendation, "Playing the Turn in Position, p.290");
            }

            if (multiway)
            {
                AddScore(scores, ActionClass.BetLarge, -10);
                AddScore(scores, ActionClass.Overbet, -15);
                AddScore(scores, ActionClass.Check, 12);
                EnsureReason(recommendation, "multiway では value threshold を上げ、bluff と thin value は減らします。");
                AddTheoryReference(recommendation, "Multiway Pots, pp.378-388");
            }

            if (spr <= 2.2 && (hand.MadeStrengthScore >= 72 || hand.IsStrongDraw))
            {
                AddScore(scores, ActionClass.Jam, 18);
                AddScore(scores, ActionClass.BetLarge, 8);
                EnsureReason(recommendation, "SPR が低いので大きい投入のコストが下がっています。");
            }

            FinalizeAction(recommendation, input, scores, "チェックをもらった場面の理論近似です。");
        }

        private static void ScoreFacingBet(
            ScenarioInput input,
            Recommendation recommendation,
            HandAnalysis hand,
            BoardAnalysis board,
            RangeMetrics ranges,
            RangeShape rangeShape,
            bool villainCapped,
            double spr,
            double potOdds,
            double mdf,
            bool multiway)
        {
            var scores = CreateScoreMap(new[]
            {
                ActionClass.Fold, ActionClass.Call, ActionClass.RaiseSmall,
                ActionClass.RaiseLarge, ActionClass.Jam
            });

            AddScore(scores, ActionClass.Fold, 18);
            AddScore(scores, ActionClass.Call, 18);

            double betRatio = input.PotSize > 0.0 ? input.FacingBetSize / input.PotSize : 0.0;
            EnsureReason(recommendation, "MDF と pot odds は出発点ですが、固定の防衛義務ではなく相手レンジと bluff の質で前後します。");
            AddTheoryReference(recommendation, "Defending by Calling, pp.99-104");

            if (hand.IsMonster)
            {
                AddScore(scores, ActionClass.Call, 16);
                AddScore(scores, ActionClass.RaiseLarge, 24);
                EnsureReason(recommendation, "トップレンジなので継続は当然で、value raise が第一候補です。");

                if (input.Street == Street.Flop && input.HeroHasPosition && board.IsDry && hand.VulnerabilityScore < 35)
                {
                    AddScore(scores, ActionClass.Call, 18);
                    AddScore(scores, ActionClass.RaiseLarge, -8);
                    EnsureReason(recommendation, "乾いた flop では超強い hand を call 止めして turn / river で raise を遅らせるラインが有力です。");
                    AddTheoryReference(recommendation, "Delaying a Raise on a Dry Board, p.140");
                }

                if (board.IsWet || hand.VulnerabilityScore >= 55)
                {
                    AddScore(scores, ActionClass.RaiseLarge, 18);
                    EnsureReason(recommendation, "ウェット texture では outdraw を防ぎつつ今の street で value を回収します。");
                    AddTheoryReference(recommendation, "Delaying a Raise on a Wet Flop, p.143");
                }

                if (spr <= 2.2)
                {
                    AddScore(scores, ActionClass.Jam, 16);
                }
            }
            else if (hand.MadeStrengthScore >= 68)
            {
                AddScore(scores, ActionClass.Call, 20);
                AddScore(scores, ActionClass.RaiseLarge, board.IsWet ? 16 : 6);
                EnsureReason(recommendation, "strong made hand は基本継続で、脆いときは raise 寄りです。");
            }
            else if (hand.IsStrongDraw)
            {
                AddScore(scores, ActionClass.Call, 14);
                AddScore(scores, ActionClass.RaiseSmall, 16);
                AddScore(scores, ActionClass.RaiseLarge, 10);
                EnsureReason(recommendation, "8+ outs 級の draw は raise 候補です。showdown value が薄いほど raise が増えます。");
                AddTheoryReference(recommendation, "Defending by Raising — The Value to Bluff Raising Ratio on the Flop, p.120");
                if (!input.HeroHasPosition && input.Street == Street.Turn)
                {
                    AddScore(scores, ActionClass.Call, hand.ShowdownScore >= 18 ? 8 : 2);
                    AddScore(scores, ActionClass.RaiseLarge, hand.ShowdownScore < 18 ? 6 : 2);
                    EnsureReason(recommendation, "OOP turn の draw は一律に raise / jam へ寄せず、実現性の高い draw は call も残ります。");
                    AddTheoryReference(recommendation, "Playing Draws Out of Position on the Turn, pp.286-291");
                }
            }
            else if (hand.MadeStrengthScore >= 48)
            {
                if (potOdds <= 0.30 && !multiway)
                {
                    AddScore(scores, ActionClass.Call, 18);
                    EnsureReason(recommendation, "必要勝率が低く、bluff catcher として call を保持できます。");
                }
                else
                {
                    AddScore(scores, ActionClass.Fold, 12);
                }

                if (rangeShape == RangeShape.Polarized)
                {
                    AddScore(scores, ActionClass.Call, 10);
                    EnsureReason(recommendation, "相手が polarized なら bluff catcher の call 頻度は上がります。");
                }
            }
            else if (hand.DrawScore >= 28)
            {
                if (potOdds <= 0.28 && !multiway)
                {
                    AddScore(scores, ActionClass.Call, 10);
                }
                else
                {
                    AddScore(scores, ActionClass.Fold, 10);
                }
            }
            else
            {
                AddScore(scores, ActionClass.Fold, 24);
            }

            if (input.Street == Street.River)
            {
                if (hand.IsBluffCatcher && hand.HasNutBlocker && potOdds <= 0.30 && !multiway)
                {
                    AddScore(scores, ActionClass.Call, 12);
                    EnsureReason(recommendation, "river では blocker が call / fold の境界を大きく動かします。");
                    AddTheoryReference(recommendation, "Utilizing Removal Effects, pp.360-364");
                }

                if (!hand.IsMonster && betRatio > 1.0)
                {
                    AddScore(scores, ActionClass.Fold, 12);
                }
            }

            if (betRatio < 0.5)
            {
                AddScore(scores, ActionClass.Call, 8);
            }
            else if (betRatio > 0.9)
            {
                AddScore(scores, ActionClass.Fold, hand.MadeStrengthScore < 70 ? 8 : 0);
                AddScore(scores, ActionClass.RaiseLarge, hand.IsMonster ? 8 : 0);
            }

            if (rangeShape == RangeShape.Condensed)
            {
                AddScore(scores, ActionClass.RaiseLarge, 8);
            }

            if (multiway)
            {
                AddScore(scores, ActionClass.Fold, 16);
                AddScore(scores, ActionClass.RaiseSmall, -14);
                AddScore(scores, ActionClass.RaiseLarge, -10);
                EnsureReason(recommendation, "multiway では bluff catcher と draw の防衛をかなり絞ります。");
                AddTheoryReference(recommendation, "Bluff Catchers in Multiway Pots, p.381");
            }

            FinalizeAction(recommendation, input, scores, "ベットに直面した場面の理論近似です。");
        }

        private static void ScoreFacingRaise(
            ScenarioInput input,
            Recommendation recommendation,
            HandAnalysis hand,
            BoardAnalysis board,
            RangeShape rangeShape,
            double spr,
            bool multiway)
        {
            var scores = CreateScoreMap(new[]
            {
                ActionClass.Fold, ActionClass.Call, ActionClass.Jam
            });

            AddScore(scores, ActionClass.Fold, 22);
            AddScore(scores, ActionClass.Call, 14);

            if (hand.IsMonster)
            {
                AddScore(scores, ActionClass.Call, 18);
                AddScore(scores, ActionClass.Jam, 20);
                EnsureReason(recommendation, "raise に対して top of range なので再度強く継続します。");
            }
            else if (hand.MadeStrengthScore >= 72 || hand.IsStrongDraw)
            {
                AddScore(scores, ActionClass.Call, 12);
                if (spr <= 2.0)
                {
                    AddScore(scores, ActionClass.Jam, 16);
                }
                EnsureReason(recommendation, "強い value / draw は SPR が低いほど jam 寄りになります。");
            }
            else
            {
                AddScore(scores, ActionClass.Fold, 18);
                EnsureReason(recommendation, "中位以下の made hand と弱 draw は raise を受けると継続しづらいです。");
            }

            if (multiway)
            {
                AddScore(scores, ActionClass.Fold, 10);
            }

            FinalizeAction(recommendation, input, scores, "raise に直面した場面の理論近似です。");
        }

        private static void FinalizeAction(
            Recommendation recommendation,
            ScenarioInput input,
            Dictionary<ActionClass, int> scores,
            string summary)
        {
            ActionClass best = ActionClass.Check;
            ActionClass second = ActionClass.Check;
            int bestScore = int.MinValue;
            int secondScore = int.MinValue;

            foreach (var kv in scores)
            {
                if (kv.Value > bestScore)
                {
                    second = best;
                    secondScore = bestScore;
                    best = kv.Key;
                    bestScore = kv.Value;
                }
                else if (kv.Value > secondScore)
                {
                    second = kv.Key;
                    secondScore = kv.Value;
                }
            }

            recommendation.PrimaryAction = RenderAction(best, input);
            recommendation.SecondaryAction = second != best && (bestScore - secondScore) <= 10
                ? "代替: " + RenderAction(second, input)
                : string.Empty;
            recommendation.Summary = summary;
            recommendation.AddMetric("Primary Score", bestScore.ToString(CultureInfo.InvariantCulture));
            recommendation.AddMetric("Second Score", secondScore.ToString(CultureInfo.InvariantCulture));
        }

        private static string RenderAction(ActionClass action, ScenarioInput input)
        {
            switch (action)
            {
                case ActionClass.Fold: return "フォールド";
                case ActionClass.Check: return "チェック";
                case ActionClass.Call: return "コール";
                case ActionClass.BetSmall: return input.Street == Street.River ? "33% pot ベット" : "33〜40% pot ベット";
                case ActionClass.BetMedium: return "60〜75% pot ベット";
                case ActionClass.BetLarge: return "80〜100% pot ベット";
                case ActionClass.Overbet: return "125〜175% pot オーバーベット";
                case ActionClass.RaiseSmall: return input.Street == Street.Flop ? "小さめ raise (2.5〜2.8x)" : "小さめ raise";
                case ActionClass.RaiseLarge: return input.Street == Street.Flop ? "大きめ raise (3.2〜3.8x)" : "大きめ raise";
                case ActionClass.Jam: return "オールイン / ジャム";
                case ActionClass.OpenRaise: return "オープン";
                case ActionClass.ThreeBet: return "3ベット";
                case ActionClass.FourBet: return "4ベット";
                default: return action.ToString();
            }
        }

        private static Dictionary<ActionClass, int> CreateScoreMap(IEnumerable<ActionClass> actions)
        {
            var dict = new Dictionary<ActionClass, int>();
            foreach (var action in actions)
            {
                dict[action] = 0;
            }
            return dict;
        }

        private static void AddScore(Dictionary<ActionClass, int> scores, ActionClass action, int value)
        {
            if (!scores.ContainsKey(action))
            {
                scores[action] = 0;
            }
            scores[action] += value;
        }

        private static RangeMetrics BuildRangeMetrics(ScenarioInput input, List<Card> board)
        {
            var metrics = new RangeMetrics();
            metrics.SpotLabel = string.Empty;
            metrics.HeroSummary = "n/a";
            metrics.VillainSummary = "n/a";

            RangeDefinition heroRange = null;
            RangeDefinition villainRange = null;
            string spotLabel;
            ResolveRanges(input, out heroRange, out villainRange, out spotLabel);
            metrics.SpotLabel = spotLabel;
            if (heroRange == null || villainRange == null)
            {
                return metrics;
            }

            var blocked = new List<Card>();
            blocked.AddRange(board);
            blocked.AddRange(input.HeroCards);

            var heroCombos = PreflopCharts.ExpandToCombos(heroRange, blocked);
            var villainCombos = PreflopCharts.ExpandToCombos(villainRange, blocked);

            metrics.HeroComboCount = heroCombos.Count;
            metrics.VillainComboCount = villainCombos.Count;

            double heroStrongPct;
            double heroNutPct;
            double heroDrawPct;
            double villainStrongPct;
            double villainNutPct;
            double villainDrawPct;

            AccumulateComboMetrics(board, heroCombos, input.Street, out heroStrongPct, out heroNutPct, out heroDrawPct);
            AccumulateComboMetrics(board, villainCombos, input.Street, out villainStrongPct, out villainNutPct, out villainDrawPct);

            metrics.HeroStrongPct = heroStrongPct;
            metrics.HeroNutPct = heroNutPct;
            metrics.HeroDrawPct = heroDrawPct;
            metrics.VillainStrongPct = villainStrongPct;
            metrics.VillainNutPct = villainNutPct;
            metrics.VillainDrawPct = villainDrawPct;

            metrics.HeroSummary = string.Format(
                CultureInfo.InvariantCulture,
                "strong {0}, nut {1}, draw {2}",
                Percent(metrics.HeroStrongPct / 100.0),
                Percent(metrics.HeroNutPct / 100.0),
                Percent(metrics.HeroDrawPct / 100.0));
            metrics.VillainSummary = string.Format(
                CultureInfo.InvariantCulture,
                "strong {0}, nut {1}, draw {2}",
                Percent(metrics.VillainStrongPct / 100.0),
                Percent(metrics.VillainNutPct / 100.0),
                Percent(metrics.VillainDrawPct / 100.0));
            return metrics;
        }

        private static void ResolveRanges(
            ScenarioInput input,
            out RangeDefinition heroRange,
            out RangeDefinition villainRange,
            out string spotLabel)
        {
            heroRange = null;
            villainRange = null;
            spotLabel = string.Empty;

            if (input.PotType == PotType.SingleRaised || input.PotType == PotType.Multiway)
            {
                if (input.HeroWasPreflopAggressor)
                {
                    heroRange = PreflopCharts.GetOpeningRange(input.HeroPosition);
                    villainRange = PreflopCharts.GetColdCallRange(input.VillainPosition, input.HeroPosition);
                    spotLabel = PositionHelper.ToJapanese(input.HeroPosition) + " open vs " + PositionHelper.ToJapanese(input.VillainPosition) + " flat";
                }
                else
                {
                    heroRange = PreflopCharts.GetColdCallRange(input.HeroPosition, input.VillainPosition);
                    villainRange = PreflopCharts.GetOpeningRange(input.VillainPosition);
                    spotLabel = PositionHelper.ToJapanese(input.HeroPosition) + " flat vs " + PositionHelper.ToJapanese(input.VillainPosition) + " open";
                }
                return;
            }

            if (input.PotType == PotType.ThreeBet)
            {
                if (input.HeroWasPreflopAggressor)
                {
                    heroRange = PreflopCharts.GetThreeBetRange(input.HeroPosition, input.VillainPosition);
                    var defend = PreflopCharts.GetFacingThreeBetRange(input.VillainPosition, input.HeroPosition);
                    villainRange = defend == null ? null : defend.FlatRange;
                    spotLabel = PositionHelper.ToJapanese(input.HeroPosition) + " 3bet vs " + PositionHelper.ToJapanese(input.VillainPosition) + " defend";
                }
                else
                {
                    villainRange = PreflopCharts.GetThreeBetRange(input.VillainPosition, input.HeroPosition);
                    var defendHero = PreflopCharts.GetFacingThreeBetRange(input.HeroPosition, input.VillainPosition);
                    heroRange = defendHero == null ? null : defendHero.FlatRange;
                    spotLabel = PositionHelper.ToJapanese(input.HeroPosition) + " defend vs " + PositionHelper.ToJapanese(input.VillainPosition) + " 3bet";
                }
            }
        }

        private static void AccumulateComboMetrics(
            List<Card> board,
            List<Combo> combos,
            Street street,
            out double strongPct,
            out double nutPct,
            out double drawPct)
        {
            int strong = 0;
            int nut = 0;
            int draw = 0;
            foreach (var combo in combos)
            {
                var hand = PokerEvaluator.AnalyzeHand(combo.ToList(), board, street);
                if (hand.MadeStrengthScore >= 65)
                {
                    strong++;
                }
                if (hand.MadeStrengthScore >= 82 || (hand.IsTwoPairPlus && board.Count >= 4))
                {
                    nut++;
                }
                if (hand.DrawScore >= 35)
                {
                    draw++;
                }
            }

            if (combos.Count == 0)
            {
                strongPct = 0.0;
                nutPct = 0.0;
                drawPct = 0.0;
                return;
            }

            strongPct = 100.0 * strong / combos.Count;
            nutPct = 100.0 * nut / combos.Count;
            drawPct = 100.0 * draw / combos.Count;
        }

        private static RangeShape InferRangeShape(ScenarioInput input, BoardAnalysis board)
        {
            if (input.RangeShape != RangeShape.Auto)
            {
                return input.RangeShape;
            }

            if (input.Scenario == ScenarioType.CheckedToHero)
            {
                if (input.Street == Street.Turn || input.Street == Street.River)
                {
                    return RangeShape.Condensed;
                }
                return RangeShape.Balanced;
            }

            if (input.Scenario == ScenarioType.FacingRaise)
            {
                return RangeShape.Polarized;
            }

            if (input.Scenario == ScenarioType.FacingBet)
            {
                if (input.Street == Street.River || input.FacingBetSize >= input.PotSize * 0.75)
                {
                    return RangeShape.Polarized;
                }
                if (board.IsWet)
                {
                    return RangeShape.Balanced;
                }
                return RangeShape.Condensed;
            }

            return RangeShape.Balanced;
        }

        private static double CalculateCallEquity(double pot, double callAmount)
        {
            if (callAmount <= 0.0)
            {
                return 0.0;
            }
            return callAmount / (pot + callAmount);
        }

        private static double CalculateMinimumDefenseFrequency(double pot, double betAmount)
        {
            if (betAmount <= 0.0)
            {
                return 0.0;
            }
            return pot / (pot + betAmount);
        }

        private static void EnsureReason(Recommendation recommendation, string reason)
        {
            foreach (var existing in recommendation.Reasons)
            {
                if (existing == reason)
                {
                    return;
                }
            }
            recommendation.Reasons.Add(reason);
        }

        private static void AddTheoryReference(Recommendation recommendation, string reference)
        {
            foreach (var existing in recommendation.TheoryReferences)
            {
                if (existing == reference)
                {
                    return;
                }
            }
            recommendation.TheoryReferences.Add(reference);
        }

        private static string BuildConfidence(Recommendation recommendation)
        {
            string best = string.Empty;
            string second = string.Empty;
            foreach (var metric in recommendation.Metrics)
            {
                if (metric.Key == "Primary Score")
                {
                    best = metric.Value;
                }
                else if (metric.Key == "Second Score")
                {
                    second = metric.Value;
                }
            }

            int bestScore;
            int secondScore;
            if (int.TryParse(best, out bestScore) && int.TryParse(second, out secondScore))
            {
                if (bestScore - secondScore >= 18)
                {
                    return "高";
                }
                if (bestScore - secondScore >= 9)
                {
                    return "中";
                }
            }
            return "低〜中";
        }

        private static string Percent(double ratio)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0}%", ratio * 100.0);
        }

        private static string FormatNumber(double value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##}", value);
        }

        private static string ToJapanese(RangeShape shape)
        {
            switch (shape)
            {
                case RangeShape.Polarized: return "Polarized";
                case RangeShape.Condensed: return "Condensed";
                case RangeShape.Balanced: return "Balanced";
                default: return "Auto";
            }
        }
    }
}
