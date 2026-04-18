using System;
using System.Collections.Generic;
using System.Text;

namespace NLHETheoryAdvisor
{
    sealed class RangeDefinition
    {
        public string Name { get; private set; }
        public string RangeText { get; private set; }
        public HashSet<string> Hands { get; private set; }
        public HashSet<string> MixedHands { get; private set; }

        public RangeDefinition(string name, string rangeText)
        {
            Name = name;
            RangeText = rangeText;
            Hands = new HashSet<string>(StringComparer.Ordinal);
            MixedHands = new HashSet<string>(StringComparer.Ordinal);
            ParseRange(rangeText);
        }

        public bool Contains(string handCode)
        {
            return Hands.Contains(handCode);
        }

        public bool IsMixed(string handCode)
        {
            return MixedHands.Contains(handCode);
        }

        private void ParseRange(string rangeText)
        {
            var pieces = rangeText.Split(',');
            foreach (var rawPiece in pieces)
            {
                var piece = rawPiece.Trim();
                if (piece.Length == 0)
                {
                    continue;
                }

                bool mixed = piece.IndexOf('*') >= 0;
                piece = piece.Replace("*", string.Empty);
                foreach (var handCode in ExpandToken(piece))
                {
                    Hands.Add(handCode);
                    if (mixed)
                    {
                        MixedHands.Add(handCode);
                    }
                }
            }
        }

        private static IEnumerable<string> ExpandToken(string token)
        {
            token = token.Replace(" ", string.Empty);
            if (token.Length == 0)
            {
                yield break;
            }

            if (token.IndexOf('-') < 0)
            {
                yield return NormalizeHandCode(token);
                yield break;
            }

            var parts = token.Split('-');
            if (parts.Length != 2)
            {
                yield return NormalizeHandCode(token);
                yield break;
            }

            var start = NormalizeHandCode(parts[0]);
            var end = NormalizeHandCode(parts[1]);
            int[] order = new[] { 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            if (start.Length == 2 && end.Length == 2)
            {
                int startIndex = IndexOfRank(order, Card.CharToRank(start[0]));
                int endIndex = IndexOfRank(order, Card.CharToRank(end[0]));
                for (int i = startIndex; i <= endIndex; i++)
                {
                    char rankChar = Card.RankToChar(order[i]);
                    yield return new string(new[] { rankChar, rankChar });
                }
                yield break;
            }

            if (start.Length == 3 && end.Length == 3 && start[2] == end[2])
            {
                int startHigh = Card.CharToRank(start[0]);
                int startLow = Card.CharToRank(start[1]);
                int endHigh = Card.CharToRank(end[0]);
                int endLow = Card.CharToRank(end[1]);
                char suitedness = start[2];

                if (startHigh == endHigh)
                {
                    int startIndex = IndexOfRank(order, startLow);
                    int endIndex = IndexOfRank(order, endLow);
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        yield return string.Concat(Card.RankToChar(startHigh), Card.RankToChar(order[i]), suitedness);
                    }
                    yield break;
                }

                if (startLow == endLow)
                {
                    int startIndex = IndexOfRank(order, startHigh);
                    int endIndex = IndexOfRank(order, endHigh);
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        yield return string.Concat(Card.RankToChar(order[i]), Card.RankToChar(startLow), suitedness);
                    }
                    yield break;
                }
            }

            yield return NormalizeHandCode(start);
            yield return NormalizeHandCode(end);
        }

        private static int IndexOfRank(int[] order, int rank)
        {
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == rank)
                {
                    return i;
                }
            }
            return 0;
        }

        private static string NormalizeHandCode(string token)
        {
            token = token.Trim().ToUpperInvariant().Replace(" ", string.Empty);
            if (token.Length == 2)
            {
                return token;
            }
            if (token.Length != 3)
            {
                return token;
            }

            int rankA = Card.CharToRank(token[0]);
            int rankB = Card.CharToRank(token[1]);
            char suitedness = char.ToLowerInvariant(token[2]);

            if (rankB > rankA)
            {
                char a = token[0];
                char b = token[1];
                token = string.Concat(b, a, suitedness);
            }
            else
            {
                token = string.Concat(token[0], token[1], suitedness);
            }
            return token;
        }
    }

    sealed class FacingThreeBetRange
    {
        public RangeDefinition FlatRange { get; private set; }
        public RangeDefinition FourBetRange { get; private set; }
        public string Label { get; private set; }

        public FacingThreeBetRange(string label, string flatRange, string fourBetRange)
        {
            Label = label;
            FlatRange = new RangeDefinition(label + " Flat", flatRange);
            FourBetRange = new RangeDefinition(label + " 4Bet", fourBetRange);
        }
    }

    sealed class PreflopLookupResult
    {
        public string Action { get; set; }
        public string SecondaryAction { get; set; }
        public string SpotLabel { get; set; }
        public string RangeSummary { get; set; }
        public List<string> Notes { get; private set; }

        public PreflopLookupResult()
        {
            Action = string.Empty;
            SecondaryAction = string.Empty;
            SpotLabel = string.Empty;
            RangeSummary = string.Empty;
            Notes = new List<string>();
        }
    }

    static class PreflopCharts
    {
        private static readonly Dictionary<Position, RangeDefinition> OpeningRanges =
            new Dictionary<Position, RangeDefinition>();

        private static readonly Dictionary<string, RangeDefinition> ColdCallRanges =
            new Dictionary<string, RangeDefinition>(StringComparer.Ordinal);

        private static readonly Dictionary<string, RangeDefinition> ThreeBetRanges =
            new Dictionary<string, RangeDefinition>(StringComparer.Ordinal);

        private static readonly Dictionary<string, FacingThreeBetRange> FacingThreeBetRanges =
            new Dictionary<string, FacingThreeBetRange>(StringComparer.Ordinal);

        static PreflopCharts()
        {
            OpeningRanges[Position.UTG] = new RangeDefinition(
                "UTG Open",
                "AA-33, AKo-AJo, KQo, AKs-ATs, KQs-KTs, QJs-QTs, JTs-J9s, T9s, 98s, 87s, 76s, 65s");
            OpeningRanges[Position.MP] = new RangeDefinition(
                "MP Open",
                "AA-22, AKo-ATo, KQo, AKs-A7s, A5s, KQs-KTs, QJs-QTs, JTs-J9s, T9s-T8s, 98s-97s, 87s-86s, 76s-75s, 65s, 54s");
            OpeningRanges[Position.CO] = new RangeDefinition(
                "CO Open",
                "AA-22, AKo-ATo, KQo-KJo, QJo, AKs-A2s, KQs-K6s, QJs-Q7s, JTs-J8s, T9s-T8s, 98s-97s, 87s-86s, 76s-75s, 65s-64s, 54s");
            OpeningRanges[Position.BTN] = new RangeDefinition(
                "BTN Open",
                "AA-22, AKo-A2o, KQo-K7o, QJo-Q9o, JTo-J9o, T9o-T8o, 98o, 87o, AKs-A2s, KQs-K2s, QJs-Q2s, JTs-J5s, T9s-T6s, 98s-96s, 87s-85s, 76s-74s, 65s-64s, 54s-53s, 43s");
            OpeningRanges[Position.SB] = new RangeDefinition(
                "SB Open",
                "AA-22, AKo-A7o, KQo-K9o, QJo-Q9o, JTo-J9o, T9o, 98o, AKs-A2s, KQs-K2s, QJs-Q4s, JTs-J7s, T9s-T7s, 98s-97s, 87s-86s, 76s-75s, 65s-64s, 54s");

            AddColdCall(Position.MP, Position.UTG, "QQ-55, AKo-AQo, AQs-ATs, KQs-KJs, QJs, JTs, T9s, 98s, 87s");
            AddColdCall(Position.CO, Position.UTG, "QQ-44, AKo-AQo, AQs-ATs, KQs-KJs, QJs, JTs, T9s, 98s, 87s, 76s, 65s");
            AddColdCall(Position.BTN, Position.UTG, "QQ-33, AKo-AQo, AQs-ATs, KQs-KTs, QJs-QTs, JTs-J9s, T9s, 98s, 87s, 76s, 65s, 54s");
            AddColdCall(Position.SB, Position.UTG, "QQ-88, AKo*, AQs, KQs");
            AddColdCall(Position.BB, Position.UTG, "QQ-44, AKo-AQo, AQs-ATs, KQs-KJs, QJs, JTs");

            AddColdCall(Position.CO, Position.MP, "JJ-44, AKo-AQo, AQs-ATs, KQs-KTs, QJs-QTs, JTs, T9s, 98s, 87s, 76s");
            AddColdCall(Position.BTN, Position.MP, "JJ-33, AKo-AQo, AQs-ATs, KQs-KTs, QJs-QTs, JTs-J9s, T9s, 98s, 87s, 76s, 65s, 54s");
            AddColdCall(Position.SB, Position.MP, "JJ-77, AKo*-AQo, AQs, KQs");
            AddColdCall(Position.BB, Position.MP, "JJ-22, AQo, AQs-ATs, KQs-KJs, QJs, JTs, T9s, 98s, 87s");

            AddColdCall(Position.BTN, Position.CO, "AA*, TT-22, AKo*-AJo, KQo, AQs-A8s, KQs-KTs, QJs-QTs, JTs-J9s, T9s-T8s, 98s-97s, 87s-86s, 76s-75s, 65s, 54s");
            AddColdCall(Position.SB, Position.CO, "TT-88, AQo-AJo, KQo, AJs-ATs, KQs-KJs, QJs");
            AddColdCall(Position.BB, Position.CO, "TT-22, AQo-AJo, KQo, AJs-ATs, KJs-KTs, QJs-QTs, JTs-J9s, T9s, 98s");

            AddColdCall(Position.SB, Position.BTN, "99-66, KTo, QJo-QTo, A9s-A8s, KTs-K9s, QJs-QTs, JTs, T9s");
            AddColdCall(Position.BB, Position.BTN, "99-33, A9o-A2o, KTo-K7o, QJo-Q8o, JTo-J9o, T9o, 98o, A8s-A2s, KTs-K5s, QJs-Q7s, JTs-J8s, T9s-T8s");
            AddColdCall(Position.BB, Position.SB, "TT-22, ATo-A2o, KJo-K7o, QJo-Q8o, JTo-J8o, T9o-T8o, 98o-97o, 87o, 76o, ATs-A2s, KJs-K2s, QJs-Q2s, JTs-J4s, T9s-T5s, 98s-95s, 87s-85s, 76s-74s, 65s-64s, 54s-53s, 43s");

            ThreeBetRanges["UTG|IP"] = new RangeDefinition("3Bet IP vs UTG", "AA-KK, AJo, KQo, AKs, A5s-A4s");
            ThreeBetRanges["UTG|BLINDS"] = new RangeDefinition("3Bet Blinds vs UTG", "AA-KK, AKo*, 44-33, AKs, T9s, 98s, 87s, 76s, 65s");
            ThreeBetRanges["MP|IP"] = new RangeDefinition("3Bet IP vs MP", "AA-QQ, AJo, KQo, AKs, A5s-A4s, T8s, 97s");
            ThreeBetRanges["MP|SB"] = new RangeDefinition("3Bet SB vs MP", "AA-QQ, 66-44, AKo, AKs-AQs, JTs, T9s, 98s, 87s, 76s");
            ThreeBetRanges["MP|BB"] = new RangeDefinition("3Bet BB vs MP", "AA-QQ, AKo, AKs-AQs, QTs, J9s, T8s, 98s-97s, 87s, 76s, 65s, 54s");
            ThreeBetRanges["CO|IP"] = new RangeDefinition("3Bet IP vs CO", "AA*-JJ, AKo*, ATo, KJo, QJo, AKs, A7s-A2s");
            ThreeBetRanges["CO|SB"] = new RangeDefinition("3Bet SB vs CO", "AA-JJ, 55-44, AKo, AKs-AQs, KTs, QTs, JTs-J9s, T9s-T8s, 98s-97s, 87s, 76s, 65s, 54s");
            ThreeBetRanges["CO|BB"] = new RangeDefinition("3Bet BB vs CO", "AA-JJ, 44-22, AKo, AKs-AQs, A5s-A4s, K9s, Q9s, T8s, 97s, 87s-86s, 76s-75s, 65s-64s, 54s");
            ThreeBetRanges["BTN|SB"] = new RangeDefinition("3Bet SB vs BTN", "AA-TT, 55-33, AKo-ATo, KQo-KJo, AKs-ATs, A7s-A2s, KQs-KJs, K8s-K4s, Q9s-Q8s, J9s-J8s, T8s, 98s-97s, 87s-86s, 76s-75s, 65s-64s, 54s");
            ThreeBetRanges["BTN|BB"] = new RangeDefinition("3Bet BB vs BTN", "AA-TT, 22, AKo-ATo, KQo-KJo, AKs-A9s, KQs-KJs, K4s-K2s, Q6s-Q2s, J7s-J6s, T7s, 98s-96s, 87s-85s, 76s-75s, 65s-64s, 54s-53s, 43s");

            FacingThreeBetRanges["UTG|IP"] = new FacingThreeBetRange(
                "UTG vs IP 3Bet",
                "KK-TT*, AKo-AQo*, AKs-AQs, KQs",
                "AA, 98s, 87s, 76s");
            FacingThreeBetRanges["UTG|OOP"] = new FacingThreeBetRange(
                "UTG vs OOP 3Bet",
                "KK-TT*, AKo-AQo*, AKs-AQs, KQs",
                "AA, AJs, ATs, 76s");
            FacingThreeBetRanges["MP|IP"] = new FacingThreeBetRange(
                "MP vs IP 3Bet",
                "QQ-TT, AKo-AQo*, AKs-AQs, KQs",
                "AA-KK, AKs, 98s, 87s, 76s, 65s, 54s");
            FacingThreeBetRanges["MP|OOP"] = new FacingThreeBetRange(
                "MP vs OOP 3Bet",
                "AA*, QQ-TT, AKo-AQo, AKs-AJs, KQs, QJs",
                "AA*-KK, 98s, 87s, 76s, 65s");
            FacingThreeBetRanges["CO|IP"] = new FacingThreeBetRange(
                "CO vs IP 3Bet",
                "AA, JJ-99, AKo*-AQo, KQo, AQs-AJs, KQs-KJs, QJs",
                "KK-QQ, AKo*, AKs, T9s, 98s, 87s, 76s, 65s");
            FacingThreeBetRanges["CO|OOP"] = new FacingThreeBetRange(
                "CO vs OOP 3Bet",
                "JJ-99, AKo-AJo, KQo, AQs-ATs, KQs-KTs, QJs-QTs, JTs, T9s, 98s",
                "AA-QQ, AKs, A8s-A5s, 87s, 76s");
            FacingThreeBetRanges["BTN|ANY"] = new FacingThreeBetRange(
                "BTN vs 3Bet",
                "AA, TT-77, AQo-ATo, KQo-KTo, QJo, AQs-A7s, A5s-A2s, KQs-K9s, QJs-Q9s, JTs-J9s, T9s-T8s, 98s-97s, 87s, 76s, 65s",
                "KK-JJ, AKo, AKs, A6s, A4s-A2s, K8s-K4s, Q8s-Q7s");
            FacingThreeBetRanges["SB|BB"] = new FacingThreeBetRange(
                "SB vs BB 3Bet",
                "AA, TT-77, AQo-ATo, KQo-KJo, AQs-A9s, KQs-KTs, QJs-QTs, JTs-J9s, T9s, 98s",
                "KK-JJ, AKo, AKs, T9s-T8s, 98s-97s, 87s, 76s, 65s, 54s");
        }

        public static PreflopLookupResult Analyze(Position hero, Position villain, ScenarioType scenario, string handCode, double effectiveStackBb, int players)
        {
            var result = new PreflopLookupResult();
            string normalizedHand = NormalizeHandInput(handCode);

            if (scenario == ScenarioType.Unopened)
            {
                var openRange = GetOpeningRange(hero);
                result.SpotLabel = PositionHelper.ToJapanese(hero) + " RFI";
                result.RangeSummary = openRange == null ? string.Empty : openRange.RangeText;
                if (openRange != null && openRange.Contains(normalizedHand))
                {
                    result.Action = hero == Position.BTN ? "オープン 2.5bb" : "オープン 3.5bb";
                    if (openRange.IsMixed(normalizedHand))
                    {
                        result.SecondaryAction = "混合頻度でフォールド";
                    }
                    result.Notes.Add("Janda の推奨 RFI レンジに入っています。");
                }
                else
                {
                    result.Action = "フォールド";
                    result.Notes.Add("推奨オープンレンジ外です。");
                }
                AddAdjustments(result, effectiveStackBb, players);
                return result;
            }

            if (scenario == ScenarioType.FacingOpen)
            {
                var flatRange = GetColdCallRange(hero, villain);
                var threeBetRange = GetThreeBetRange(hero, villain);
                result.SpotLabel = PositionHelper.ToJapanese(hero) + " vs " + PositionHelper.ToJapanese(villain) + " Open";
                result.RangeSummary = BuildSpotSummary(flatRange, threeBetRange);

                bool canFlat = flatRange != null && flatRange.Contains(normalizedHand);
                bool canThreeBet = threeBetRange != null && threeBetRange.Contains(normalizedHand);
                bool mixed = (flatRange != null && flatRange.IsMixed(normalizedHand))
                    || (threeBetRange != null && threeBetRange.IsMixed(normalizedHand))
                    || (canFlat && canThreeBet);

                if (canFlat && canThreeBet)
                {
                    result.Action = "混合: コール";
                    result.SecondaryAction = "混合: 3ベット";
                    result.Notes.Add("コールと 3 ベットの両方に入る混合ハンドです。");
                }
                else if (canThreeBet)
                {
                    result.Action = PositionHelper.IsIpAgainstOpen(hero, villain) ? "3ベット 3x 前後" : "3ベット 4x 前後";
                    if (mixed)
                    {
                        result.SecondaryAction = "一部コール";
                    }
                    result.Notes.Add("コールドコールよりリレイズで下位レンジをより強く罰するハンドです。");
                }
                else if (canFlat)
                {
                    result.Action = "コール";
                    if (mixed)
                    {
                        result.SecondaryAction = "一部 3ベット";
                    }
                    result.Notes.Add("フラットレンジに含まれます。");
                }
                else
                {
                    result.Action = "フォールド";
                    result.Notes.Add("推奨のコール / 3 ベットレンジ外です。");
                }

                AddAdjustments(result, effectiveStackBb, players);
                return result;
            }

            if (scenario == ScenarioType.Facing3Bet)
            {
                var defendRange = GetFacingThreeBetRange(hero, villain);
                result.SpotLabel = defendRange == null
                    ? PositionHelper.ToJapanese(hero) + " facing 3bet"
                    : defendRange.Label;
                result.RangeSummary = defendRange == null
                    ? string.Empty
                    : string.Format("{0}\r\n4bet: {1}", defendRange.FlatRange.RangeText, defendRange.FourBetRange.RangeText);

                bool canFlat = defendRange != null && defendRange.FlatRange.Contains(normalizedHand);
                bool canFourBet = defendRange != null && defendRange.FourBetRange.Contains(normalizedHand);
                bool mixed = (defendRange != null && defendRange.FlatRange.IsMixed(normalizedHand))
                    || (defendRange != null && defendRange.FourBetRange.IsMixed(normalizedHand))
                    || (canFlat && canFourBet);

                if (canFlat && canFourBet)
                {
                    result.Action = "混合: コール";
                    result.SecondaryAction = "混合: 4ベット";
                    result.Notes.Add("コールと 4 ベットの混合ゾーンです。");
                }
                else if (canFourBet)
                {
                    result.Action = effectiveStackBb <= 40.0 ? "4ベット / 実質コミット" : "4ベット";
                    if (mixed)
                    {
                        result.SecondaryAction = "一部コール";
                    }
                    result.Notes.Add("理論上の 4 ベット継続レンジです。");
                }
                else if (canFlat)
                {
                    result.Action = "コール";
                    if (mixed)
                    {
                        result.SecondaryAction = "一部 4ベット";
                    }
                    result.Notes.Add("ポストフロップを見に行く防衛レンジです。");
                }
                else
                {
                    result.Action = "フォールド";
                    result.Notes.Add("推奨防衛レンジ外です。");
                }

                result.Notes.Add("本書では IP ではコール防衛が増え、OOP では再レイズ防衛が増えます。");
                AddAdjustments(result, effectiveStackBb, players);
                return result;
            }

            result.Action = "プリフロップ照会対象外";
            return result;
        }

        public static RangeDefinition GetOpeningRange(Position position)
        {
            RangeDefinition range;
            return OpeningRanges.TryGetValue(position, out range) ? range : null;
        }

        public static RangeDefinition GetColdCallRange(Position caller, Position opener)
        {
            RangeDefinition range;
            return ColdCallRanges.TryGetValue(Key(caller, opener), out range) ? range : null;
        }

        public static RangeDefinition GetThreeBetRange(Position threeBettor, Position opener)
        {
            string key = null;
            if (opener == Position.UTG)
            {
                key = PositionHelper.IsBlind(threeBettor) ? "UTG|BLINDS" : "UTG|IP";
            }
            else if (opener == Position.MP)
            {
                if (threeBettor == Position.SB)
                {
                    key = "MP|SB";
                }
                else if (threeBettor == Position.BB)
                {
                    key = "MP|BB";
                }
                else
                {
                    key = "MP|IP";
                }
            }
            else if (opener == Position.CO)
            {
                if (threeBettor == Position.BTN)
                {
                    key = "CO|IP";
                }
                else if (threeBettor == Position.SB)
                {
                    key = "CO|SB";
                }
                else if (threeBettor == Position.BB)
                {
                    key = "CO|BB";
                }
            }
            else if (opener == Position.BTN)
            {
                if (threeBettor == Position.SB)
                {
                    key = "BTN|SB";
                }
                else if (threeBettor == Position.BB)
                {
                    key = "BTN|BB";
                }
            }

            RangeDefinition range;
            return key != null && ThreeBetRanges.TryGetValue(key, out range) ? range : null;
        }

        public static FacingThreeBetRange GetFacingThreeBetRange(Position opener, Position threeBettor)
        {
            string key = null;
            if (opener == Position.UTG || opener == Position.MP || opener == Position.CO)
            {
                key = PositionHelper.IsBlind(threeBettor)
                    ? PositionHelper.ToJapanese(opener) + "|OOP"
                    : PositionHelper.ToJapanese(opener) + "|IP";
            }
            else if (opener == Position.BTN)
            {
                key = "BTN|ANY";
            }
            else if (opener == Position.SB && threeBettor == Position.BB)
            {
                key = "SB|BB";
            }

            FacingThreeBetRange range;
            return key != null && FacingThreeBetRanges.TryGetValue(key, out range) ? range : null;
        }

        public static List<Combo> ExpandToCombos(RangeDefinition range, IEnumerable<Card> blockedCards)
        {
            var combos = new List<Combo>();
            if (range == null)
            {
                return combos;
            }

            foreach (var handCode in range.Hands)
            {
                foreach (var combo in ExpandHandCodeToCombos(handCode))
                {
                    if (combo.Intersects(blockedCards))
                    {
                        continue;
                    }
                    combos.Add(combo);
                }
            }
            return combos;
        }

        public static List<Combo> ExpandHandCodeToCombos(string handCode)
        {
            var combos = new List<Combo>();
            string normalized = NormalizeHandInput(handCode);
            char[] suits = new[] { 'c', 'd', 'h', 's' };

            if (normalized.Length == 2)
            {
                int rank = Card.CharToRank(normalized[0]);
                for (int i = 0; i < suits.Length; i++)
                {
                    for (int j = i + 1; j < suits.Length; j++)
                    {
                        combos.Add(new Combo(new Card(rank, suits[i]), new Card(rank, suits[j])));
                    }
                }
                return combos;
            }

            if (normalized.Length != 3)
            {
                return combos;
            }

            int highRank = Card.CharToRank(normalized[0]);
            int lowRank = Card.CharToRank(normalized[1]);
            char suitedness = char.ToLowerInvariant(normalized[2]);

            if (suitedness == 's')
            {
                foreach (char suit in suits)
                {
                    combos.Add(new Combo(new Card(highRank, suit), new Card(lowRank, suit)));
                }
                return combos;
            }

            if (suitedness == 'o')
            {
                foreach (char suitA in suits)
                {
                    foreach (char suitB in suits)
                    {
                        if (suitA == suitB)
                        {
                            continue;
                        }
                        combos.Add(new Combo(new Card(highRank, suitA), new Card(lowRank, suitB)));
                    }
                }
                return combos;
            }

            foreach (char suitA2 in suits)
            {
                foreach (char suitB2 in suits)
                {
                    if (highRank == lowRank && suitA2 == suitB2)
                    {
                        continue;
                    }
                    if (highRank != lowRank || suitA2 != suitB2)
                    {
                        combos.Add(new Combo(new Card(highRank, suitA2), new Card(lowRank, suitB2)));
                    }
                }
            }
            return combos;
        }

        public static string NormalizeHandInput(string handText)
        {
            if (string.IsNullOrWhiteSpace(handText))
            {
                return string.Empty;
            }

            string normalized = handText.Trim().Replace(" ", string.Empty);
            List<Card> cards;
            string error;
            if (Card.TryParseCards(normalized, 2, out cards, out error))
            {
                return Card.ToHandCode(cards[0], cards[1]);
            }

            normalized = normalized.ToUpperInvariant();
            if (normalized.Length == 3)
            {
                return NormalizeHandCode(normalized);
            }
            if (normalized.Length == 2)
            {
                return NormalizeHandCode(normalized);
            }
            return normalized;
        }

        private static string NormalizeHandCode(string token)
        {
            token = token.Trim().ToUpperInvariant().Replace(" ", string.Empty);
            if (token.Length == 2)
            {
                return token;
            }
            if (token.Length != 3)
            {
                return token;
            }

            int rankA = Card.CharToRank(token[0]);
            int rankB = Card.CharToRank(token[1]);
            char suitedness = char.ToLowerInvariant(token[2]);

            if (rankB > rankA)
            {
                return string.Concat(token[1], token[0], suitedness);
            }
            return string.Concat(token[0], token[1], suitedness);
        }

        private static void AddColdCall(Position caller, Position opener, string rangeText)
        {
            ColdCallRanges[Key(caller, opener)] = new RangeDefinition(
                PositionHelper.ToJapanese(caller) + " Flat vs " + PositionHelper.ToJapanese(opener),
                rangeText);
        }

        private static string Key(Position first, Position second)
        {
            return PositionHelper.ToJapanese(first) + "|" + PositionHelper.ToJapanese(second);
        }

        private static void AddAdjustments(PreflopLookupResult result, double effectiveStackBb, int players)
        {
            if (players > 2)
            {
                result.Notes.Add("マルチウェイ前提ではトップペア系よりナッツメイク型を優先して少しタイト化します。");
            }

            if (effectiveStackBb > 0.0 && effectiveStackBb < 50.0)
            {
                result.Notes.Add("浅いスタックでは高エクイティのハンド価値が上がり、スーテッドコネクターの深い含み益は落ちます。");
            }
            else if (effectiveStackBb >= 150.0)
            {
                result.Notes.Add("深いスタックではナッツを作れる Axs / suited connector の価値が上がり、薄い bluff catcher は相対的に落ちます。");
            }
        }

        private static string BuildSpotSummary(RangeDefinition flatRange, RangeDefinition threeBetRange)
        {
            var sb = new StringBuilder();
            if (flatRange != null)
            {
                sb.Append("Call: ");
                sb.Append(flatRange.RangeText);
            }
            if (threeBetRange != null)
            {
                if (sb.Length > 0)
                {
                    sb.Append("\r\n");
                }
                sb.Append("3bet: ");
                sb.Append(threeBetRange.RangeText);
            }
            return sb.ToString();
        }
    }
}
