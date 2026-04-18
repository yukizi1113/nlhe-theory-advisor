using System;
using System.Collections.Generic;

namespace NLHETheoryAdvisor
{
    static class PokerEvaluator
    {
        public static BoardAnalysis AnalyzeBoard(List<Card> board, List<Card> previousBoard)
        {
            var analysis = new BoardAnalysis();
            analysis.TextureLabel = "Preflop";
            analysis.DetailLabel = "ボード未入力";

            if (board == null || board.Count == 0)
            {
                return analysis;
            }

            var rankCounts = BuildRankCounts(board);
            var suitCounts = BuildSuitCounts(board);
            int maxSuit = MaxValue(suitCounts);
            int connectedness = CountStraightWindows(board);
            int highCards = 0;
            int topRank = 0;

            foreach (var card in board)
            {
                if (card.Rank >= 11)
                {
                    highCards++;
                }
                if (card.Rank > topRank)
                {
                    topRank = card.Rank;
                }
            }

            analysis.HighestBoardRank = topRank;
            analysis.IsPaired = HasPair(rankCounts);
            analysis.IsTwoTone = maxSuit == 2;
            analysis.IsMonotone = maxSuit >= 3;
            analysis.FlushPossible = maxSuit >= 3;
            analysis.StraightPossible = HasStraightPotential(board);

            int wetness = 0;
            if (analysis.IsTwoTone)
            {
                wetness += 18;
            }
            if (analysis.IsMonotone)
            {
                wetness += 28;
            }
            wetness += connectedness * 10;
            wetness += Math.Max(0, highCards - 1) * 4;
            if (!analysis.IsPaired)
            {
                wetness += 8;
            }
            else
            {
                wetness -= 6;
            }

            if (maxSuit >= 4)
            {
                wetness += 12;
            }
            if (HasMadeStraight(board))
            {
                wetness += 12;
            }

            analysis.WetnessScore = Clamp(wetness, 0, 100);
            analysis.IsDry = analysis.WetnessScore < 35;
            analysis.IsWet = analysis.WetnessScore >= 60;

            if (analysis.IsWet)
            {
                analysis.TextureLabel = "ウェット";
            }
            else if (analysis.IsDry)
            {
                analysis.TextureLabel = "ドライ";
            }
            else
            {
                analysis.TextureLabel = "中間";
            }

            analysis.DetailLabel = BuildBoardDetail(board, analysis, connectedness, highCards);
            analysis.LatestCardIsBlank = EvaluateBlank(board, previousBoard);
            return analysis;
        }

        public static HandAnalysis AnalyzeHand(List<Card> heroCards, List<Card> board, Street street)
        {
            var analysis = new HandAnalysis();
            if (heroCards == null || heroCards.Count != 2)
            {
                analysis.CategoryName = "Invalid";
                analysis.DetailName = "ハンド未入力";
                return analysis;
            }

            if (board == null)
            {
                board = new List<Card>();
            }

            if (board.Count == 0)
            {
                string handCode = Card.ToHandCode(heroCards[0], heroCards[1]);
                analysis.CategoryName = "Preflop";
                analysis.DetailName = DescribePreflopHand(heroCards);
                analysis.MadeStrengthScore = heroCards[0].Rank == heroCards[1].Rank ? 65 : 25;
                analysis.DrawScore = 0;
                analysis.ShowdownScore = analysis.MadeStrengthScore;
                analysis.VulnerabilityScore = 0;
                analysis.HasPair = heroCards[0].Rank == heroCards[1].Rank;
                analysis.IsTopPairOrBetter = analysis.HasPair;
                analysis.IsTwoPairPlus = false;
                analysis.IsMonster = false;
                analysis.HasOvercards = false;
                analysis.HasNutBlocker = handCode == "AKs" || handCode == "AQs";
                return analysis;
            }

            var allCards = new List<Card>(heroCards);
            allCards.AddRange(board);

            int categoryRank;
            string categoryName;
            DetermineBestCategory(allCards, out categoryRank, out categoryName);
            analysis.CategoryName = categoryName;
            analysis.DetailName = DescribeMadeHand(heroCards, board, categoryRank);

            analysis.HasFlushDraw = street != Street.River && HasFlushDraw(allCards);
            analysis.HasBackdoorFlushDraw = street == Street.Flop && !analysis.HasFlushDraw && HasBackdoorFlushDraw(allCards);
            analysis.HasOpenEndedStraightDraw = street != Street.River && !HasMadeStraight(allCards) && HasOpenEndedStraightDraw(allCards);
            analysis.HasGutshot = street != Street.River && !analysis.HasOpenEndedStraightDraw && !HasMadeStraight(allCards) && HasGutshot(allCards);
            analysis.HasOvercards = !HasMadePair(categoryRank) && BothOvercards(heroCards, board);
            analysis.HasNutBlocker = HasNutBlocker(heroCards, board, street);

            analysis.MadeStrengthScore = ScoreMadeHand(heroCards, board, categoryRank, analysis.DetailName);
            analysis.DrawScore = ScoreDraws(analysis);
            analysis.ShowdownScore = ScoreShowdownValue(heroCards, board, categoryRank, analysis);
            analysis.VulnerabilityScore = ScoreVulnerability(board, categoryRank, analysis);

            analysis.HasPair = HasMadePair(categoryRank);
            analysis.IsTopPairOrBetter = analysis.MadeStrengthScore >= 60;
            analysis.IsTwoPairPlus = categoryRank >= 2;
            analysis.IsMonster = analysis.MadeStrengthScore >= 82;
            analysis.IsStrongDraw = analysis.DrawScore >= 65;
            analysis.IsBluffCatcher = street != Street.Flop
                && analysis.ShowdownScore >= 40
                && analysis.MadeStrengthScore < 80
                && analysis.DrawScore < 40;

            return analysis;
        }

        private static string DescribePreflopHand(List<Card> heroCards)
        {
            if (heroCards[0].Rank == heroCards[1].Rank)
            {
                return "Pocket Pair";
            }
            if (heroCards[0].Suit == heroCards[1].Suit)
            {
                return "Suited Non-Pair";
            }
            return "Offsuit Non-Pair";
        }

        private static string DescribeMadeHand(List<Card> heroCards, List<Card> board, int categoryRank)
        {
            if (categoryRank == 8) return "Straight Flush";
            if (categoryRank == 7) return "Quads";
            if (categoryRank == 6) return "Full House";
            if (categoryRank == 5) return "Flush";
            if (categoryRank == 4) return "Straight";
            if (categoryRank == 3)
            {
                if (heroCards[0].Rank == heroCards[1].Rank && CountRank(board, heroCards[0].Rank) == 1)
                {
                    return "Set";
                }
                return "Trips";
            }
            if (categoryRank == 2)
            {
                return "Two Pair";
            }
            if (categoryRank == 1)
            {
                if (heroCards[0].Rank == heroCards[1].Rank)
                {
                    int pairRank = heroCards[0].Rank;
                    int topBoard = HighestRank(board);
                    if (pairRank > topBoard)
                    {
                        return "Overpair";
                    }
                    if (pairRank >= SecondHighestRank(board))
                    {
                        return "Pocket Pair";
                    }
                    return "Underpair";
                }

                int matchA = CountRank(board, heroCards[0].Rank);
                int matchB = CountRank(board, heroCards[1].Rank);
                int top = HighestRank(board);
                int second = SecondHighestRank(board);

                if ((matchA > 0 && heroCards[0].Rank == top) || (matchB > 0 && heroCards[1].Rank == top))
                {
                    return "Top Pair";
                }
                if ((matchA > 0 && heroCards[0].Rank == second) || (matchB > 0 && heroCards[1].Rank == second))
                {
                    return "Second Pair";
                }
                return "Weak Pair";
            }

            if (BothOvercards(heroCards, board))
            {
                return "Ace / King High Overcards";
            }
            return "High Card";
        }

        private static int ScoreMadeHand(List<Card> heroCards, List<Card> board, int categoryRank, string detail)
        {
            switch (categoryRank)
            {
                case 8: return 100;
                case 7: return 99;
                case 6: return 97;
                case 5: return 92;
                case 4: return 88;
                case 3: return detail == "Set" ? 90 : 84;
                case 2: return 78;
                case 1:
                    if (detail == "Overpair") return 72;
                    if (detail == "Top Pair")
                    {
                        int kicker = heroCards[0].Rank == HighestRank(board) ? heroCards[1].Rank : heroCards[0].Rank;
                        return kicker >= 11 ? 68 : 62;
                    }
                    if (detail == "Second Pair") return 54;
                    if (detail == "Pocket Pair") return 48;
                    if (detail == "Weak Pair") return 44;
                    return 38;
                default:
                    return 10;
            }
        }

        private static int ScoreDraws(HandAnalysis analysis)
        {
            int score = 0;
            if (analysis.HasFlushDraw)
            {
                score += 36;
            }
            else if (analysis.HasBackdoorFlushDraw)
            {
                score += 10;
            }

            if (analysis.HasOpenEndedStraightDraw)
            {
                score += 30;
            }
            else if (analysis.HasGutshot)
            {
                score += 16;
            }

            if (analysis.HasOvercards)
            {
                score += 14;
            }

            if (analysis.HasNutBlocker)
            {
                score += 8;
            }

            return Clamp(score, 0, 100);
        }

        private static int ScoreShowdownValue(List<Card> heroCards, List<Card> board, int categoryRank, HandAnalysis analysis)
        {
            if (analysis.MadeStrengthScore >= 60)
            {
                return analysis.MadeStrengthScore;
            }

            if (categoryRank == 1)
            {
                return analysis.MadeStrengthScore + 5;
            }

            int bestHeroRank = Math.Max(heroCards[0].Rank, heroCards[1].Rank);
            if (bestHeroRank == 14)
            {
                return 22;
            }
            if (bestHeroRank == 13)
            {
                return 16;
            }
            return 8;
        }

        private static int ScoreVulnerability(List<Card> board, int categoryRank, HandAnalysis analysis)
        {
            var suitCounts = BuildSuitCounts(board);
            int maxSuit = MaxValue(suitCounts);
            int vulnerability = 0;

            if (categoryRank >= 5 || categoryRank == 7 || categoryRank == 8)
            {
                vulnerability = 8;
            }
            else if (categoryRank == 4)
            {
                vulnerability = 18;
            }
            else if (categoryRank == 3)
            {
                vulnerability = 28;
            }
            else if (categoryRank == 2)
            {
                vulnerability = 42;
            }
            else if (analysis.DetailName == "Overpair")
            {
                vulnerability = 54;
            }
            else if (analysis.DetailName == "Top Pair")
            {
                vulnerability = 58;
            }
            else if (analysis.HasPair)
            {
                vulnerability = 48;
            }
            else
            {
                vulnerability = 18;
            }

            if (analysis.HasFlushDraw || analysis.HasOpenEndedStraightDraw)
            {
                vulnerability += 10;
            }
            if (maxSuit >= 3)
            {
                vulnerability += 8;
            }
            if (CountStraightWindows(board) >= 2)
            {
                vulnerability += 8;
            }
            return Clamp(vulnerability, 0, 100);
        }

        private static void DetermineBestCategory(List<Card> cards, out int categoryRank, out string categoryName)
        {
            categoryRank = 0;
            categoryName = "High Card";

            var rankCounts = BuildRankCounts(cards);
            var suitCounts = BuildSuitCounts(cards);
            List<Card> flushCards = null;

            foreach (var kv in suitCounts)
            {
                if (kv.Value >= 5)
                {
                    flushCards = new List<Card>();
                    foreach (var card in cards)
                    {
                        if (card.Suit == kv.Key)
                        {
                            flushCards.Add(card);
                        }
                    }
                    break;
                }
            }

            if (flushCards != null && GetStraightHigh(flushCards) > 0)
            {
                categoryRank = 8;
                categoryName = "Straight Flush";
                return;
            }

            if (HasCount(rankCounts, 4))
            {
                categoryRank = 7;
                categoryName = "Four of a Kind";
                return;
            }

            if (HasCount(rankCounts, 3) && (HasCount(rankCounts, 2) || CountExact(rankCounts, 3) >= 2))
            {
                categoryRank = 6;
                categoryName = "Full House";
                return;
            }

            if (flushCards != null)
            {
                categoryRank = 5;
                categoryName = "Flush";
                return;
            }

            if (GetStraightHigh(cards) > 0)
            {
                categoryRank = 4;
                categoryName = "Straight";
                return;
            }

            if (HasCount(rankCounts, 3))
            {
                categoryRank = 3;
                categoryName = "Trips";
                return;
            }

            if (CountExact(rankCounts, 2) >= 2)
            {
                categoryRank = 2;
                categoryName = "Two Pair";
                return;
            }

            if (CountExact(rankCounts, 2) == 1)
            {
                categoryRank = 1;
                categoryName = "Pair";
            }
        }

        private static bool HasFlushDraw(List<Card> cards)
        {
            var suitCounts = BuildSuitCounts(cards);
            return MaxValue(suitCounts) == 4;
        }

        private static bool HasBackdoorFlushDraw(List<Card> cards)
        {
            var suitCounts = BuildSuitCounts(cards);
            return MaxValue(suitCounts) == 3;
        }

        private static bool HasOpenEndedStraightDraw(List<Card> cards)
        {
            var ranks = BuildStraightRankSet(cards);
            for (int start = 1; start <= 10; start++)
            {
                bool r1 = ranks.Contains(start);
                bool r2 = ranks.Contains(start + 1);
                bool r3 = ranks.Contains(start + 2);
                bool r4 = ranks.Contains(start + 3);
                if (r1 && r2 && r3 && r4)
                {
                    return true;
                }
            }

            for (int low = 1; low <= 10; low++)
            {
                int count = 0;
                bool[] present = new bool[5];
                for (int i = 0; i < 5; i++)
                {
                    present[i] = ranks.Contains(low + i);
                    if (present[i]) count++;
                }
                if (count == 4 && (!present[0] || !present[4]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasGutshot(List<Card> cards)
        {
            var ranks = BuildStraightRankSet(cards);
            for (int low = 1; low <= 10; low++)
            {
                int count = 0;
                bool[] present = new bool[5];
                for (int i = 0; i < 5; i++)
                {
                    present[i] = ranks.Contains(low + i);
                    if (present[i]) count++;
                }
                if (count == 4 && present[0] && present[4] && (!present[1] || !present[2] || !present[3]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasNutBlocker(List<Card> heroCards, List<Card> board, Street street)
        {
            if (street == Street.Preflop)
            {
                return false;
            }

            var suitCounts = BuildSuitCounts(board);
            char dominantSuit = '\0';
            int maxSuit = 0;
            foreach (var kv in suitCounts)
            {
                if (kv.Value > maxSuit)
                {
                    maxSuit = kv.Value;
                    dominantSuit = kv.Key;
                }
            }

            if (maxSuit >= 3 && dominantSuit != '\0')
            {
                foreach (var card in heroCards)
                {
                    if (card.Suit == dominantSuit && (card.Rank == 14 || card.Rank == 13))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool BothOvercards(List<Card> heroCards, List<Card> board)
        {
            int topBoard = HighestRank(board);
            return heroCards[0].Rank > topBoard && heroCards[1].Rank > topBoard;
        }

        private static bool EvaluateBlank(List<Card> board, List<Card> previousBoard)
        {
            if (previousBoard == null || previousBoard.Count == 0 || board == null || board.Count <= previousBoard.Count)
            {
                return false;
            }

            var latestCard = board[board.Count - 1];
            int beforeDanger = BoardDangerScore(previousBoard);
            int afterDanger = BoardDangerScore(board);

            if (CountRank(previousBoard, latestCard.Rank) > 0)
            {
                return false;
            }

            if (afterDanger - beforeDanger > 6)
            {
                return false;
            }

            if (latestCard.Rank >= HighestRank(previousBoard))
            {
                return false;
            }

            return true;
        }

        public static int BoardDangerScore(List<Card> board)
        {
            if (board == null || board.Count == 0)
            {
                return 0;
            }

            var suitCounts = BuildSuitCounts(board);
            var rankCounts = BuildRankCounts(board);
            int score = MaxValue(suitCounts) * 10;
            score += CountStraightWindows(board) * 12;
            if (HasPair(rankCounts))
            {
                score -= 5;
            }
            if (HasMadeStraight(board))
            {
                score += 10;
            }
            return score;
        }

        private static string BuildBoardDetail(List<Card> board, BoardAnalysis analysis, int connectedness, int highCards)
        {
            var parts = new List<string>();
            if (analysis.IsPaired) parts.Add("paired");
            if (analysis.IsMonotone) parts.Add("flush-heavy");
            else if (analysis.IsTwoTone) parts.Add("two-tone");
            if (connectedness >= 2) parts.Add("connected");
            if (highCards >= 2) parts.Add("high-card");

            if (parts.Count == 0)
            {
                parts.Add("static");
            }
            return string.Join(", ", parts.ToArray());
        }

        private static Dictionary<int, int> BuildRankCounts(IEnumerable<Card> cards)
        {
            var dict = new Dictionary<int, int>();
            foreach (var card in cards)
            {
                if (!dict.ContainsKey(card.Rank))
                {
                    dict[card.Rank] = 0;
                }
                dict[card.Rank]++;
            }
            return dict;
        }

        private static Dictionary<char, int> BuildSuitCounts(IEnumerable<Card> cards)
        {
            var dict = new Dictionary<char, int>();
            foreach (var card in cards)
            {
                if (!dict.ContainsKey(card.Suit))
                {
                    dict[card.Suit] = 0;
                }
                dict[card.Suit]++;
            }
            return dict;
        }

        private static bool HasPair(Dictionary<int, int> rankCounts)
        {
            foreach (var kv in rankCounts)
            {
                if (kv.Value >= 2)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasCount(Dictionary<int, int> rankCounts, int required)
        {
            foreach (var kv in rankCounts)
            {
                if (kv.Value >= required)
                {
                    return true;
                }
            }
            return false;
        }

        private static int CountExact(Dictionary<int, int> rankCounts, int exactCount)
        {
            int count = 0;
            foreach (var kv in rankCounts)
            {
                if (kv.Value == exactCount)
                {
                    count++;
                }
            }
            return count;
        }

        private static int CountRank(IEnumerable<Card> cards, int rank)
        {
            int count = 0;
            foreach (var card in cards)
            {
                if (card.Rank == rank)
                {
                    count++;
                }
            }
            return count;
        }

        private static int HighestRank(IEnumerable<Card> cards)
        {
            int highest = 0;
            foreach (var card in cards)
            {
                if (card.Rank > highest)
                {
                    highest = card.Rank;
                }
            }
            return highest;
        }

        private static int SecondHighestRank(List<Card> cards)
        {
            int highest = 0;
            int second = 0;
            foreach (var card in cards)
            {
                if (card.Rank > highest)
                {
                    second = highest;
                    highest = card.Rank;
                }
                else if (card.Rank > second && card.Rank != highest)
                {
                    second = card.Rank;
                }
            }
            return second;
        }

        private static int MaxValue(Dictionary<char, int> suitCounts)
        {
            int max = 0;
            foreach (var kv in suitCounts)
            {
                if (kv.Value > max)
                {
                    max = kv.Value;
                }
            }
            return max;
        }

        private static int GetStraightHigh(IEnumerable<Card> cards)
        {
            var ranks = BuildStraightRankSet(cards);
            for (int high = 14; high >= 5; high--)
            {
                bool ok = true;
                for (int offset = 0; offset < 5; offset++)
                {
                    int needed = high - offset;
                    if (needed == 1)
                    {
                        needed = 14;
                    }
                    if (!ranks.Contains(needed) && !(needed == 14 && ranks.Contains(1)))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    return high;
                }
            }

            if (ranks.Contains(14) && ranks.Contains(5) && ranks.Contains(4) && ranks.Contains(3) && ranks.Contains(2))
            {
                return 5;
            }
            return 0;
        }

        private static HashSet<int> BuildStraightRankSet(IEnumerable<Card> cards)
        {
            var set = new HashSet<int>();
            foreach (var card in cards)
            {
                set.Add(card.Rank);
                if (card.Rank == 14)
                {
                    set.Add(1);
                }
            }
            return set;
        }

        private static bool HasMadeStraight(IEnumerable<Card> cards)
        {
            return GetStraightHigh(cards) > 0;
        }

        private static bool HasStraightPotential(IEnumerable<Card> cards)
        {
            return CountStraightWindows(new List<Card>(cards)) >= 1;
        }

        private static int CountStraightWindows(List<Card> cards)
        {
            var ranks = BuildStraightRankSet(cards);
            int windows = 0;
            for (int low = 1; low <= 10; low++)
            {
                int count = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (ranks.Contains(low + i))
                    {
                        count++;
                    }
                }
                if (count >= 3)
                {
                    windows++;
                }
            }
            return windows;
        }

        private static bool HasMadePair(int categoryRank)
        {
            return categoryRank >= 1;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
