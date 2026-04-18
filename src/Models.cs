using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NLHETheoryAdvisor
{
    enum Position
    {
        UTG,
        MP,
        CO,
        BTN,
        SB,
        BB
    }

    enum Street
    {
        Preflop,
        Flop,
        Turn,
        River
    }

    enum PotType
    {
        SingleRaised,
        ThreeBet,
        FourBet,
        Multiway
    }

    enum ScenarioType
    {
        Unopened,
        FacingOpen,
        Facing3Bet,
        CheckedToHero,
        FacingBet,
        FacingRaise
    }

    enum OpponentProfile
    {
        TheoryBalanced,
        TightPassive,
        LooseAggressive
    }

    enum RangeShape
    {
        Auto,
        Polarized,
        Condensed,
        Balanced
    }

    enum ActionClass
    {
        Fold,
        Check,
        Call,
        BetSmall,
        BetMedium,
        BetLarge,
        Overbet,
        RaiseSmall,
        RaiseLarge,
        Jam,
        OpenRaise,
        ThreeBet,
        FourBet
    }

    sealed class Card : IEquatable<Card>
    {
        public int Rank { get; private set; }
        public char Suit { get; private set; }

        public Card(int rank, char suit)
        {
            Rank = rank;
            Suit = char.ToLowerInvariant(suit);
        }

        public bool Equals(Card other)
        {
            return other != null && Rank == other.Rank && Suit == other.Suit;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Card);
        }

        public override int GetHashCode()
        {
            return Rank.GetHashCode() ^ Suit.GetHashCode();
        }

        public override string ToString()
        {
            return string.Concat(RankToChar(Rank), char.ToUpperInvariant(Suit));
        }

        public static bool TryParseCards(string text, int expectedCount, out List<Card> cards, out string error)
        {
            cards = new List<Card>();
            error = string.Empty;

            if (expectedCount == 0)
            {
                return true;
            }

            var normalized = NormalizeCardText(text);
            if (normalized.Length == 0)
            {
                error = "カード入力が空です。";
                return false;
            }

            if ((normalized.Length % 2) != 0)
            {
                error = "カード入力の文字数が不正です。例: AhKd";
                return false;
            }

            for (int i = 0; i < normalized.Length; i += 2)
            {
                Card card;
                if (!TryParseSingle(normalized.Substring(i, 2), out card))
                {
                    error = "カード入力を解釈できません: " + normalized.Substring(i, 2);
                    return false;
                }
                cards.Add(card);
            }

            if (cards.Count != expectedCount)
            {
                error = string.Format(CultureInfo.InvariantCulture, "{0} 枚入力してください。", expectedCount);
                return false;
            }

            var seen = new HashSet<Card>();
            foreach (var card in cards)
            {
                if (seen.Contains(card))
                {
                    error = "同じカードが重複しています。";
                    return false;
                }
                seen.Add(card);
            }

            return true;
        }

        public static bool TryParseSingle(string token, out Card card)
        {
            card = null;
            if (string.IsNullOrWhiteSpace(token) || token.Length != 2)
            {
                return false;
            }

            int rank = CharToRank(token[0]);
            char suit = char.ToLowerInvariant(token[1]);
            if (rank < 2 || "cdhs".IndexOf(suit) < 0)
            {
                return false;
            }

            card = new Card(rank, suit);
            return true;
        }

        public static string ToHandCode(Card a, Card b)
        {
            if (a == null || b == null)
            {
                return string.Empty;
            }

            if (a.Rank == b.Rank)
            {
                char pair = RankToChar(a.Rank);
                return new string(new[] { pair, pair });
            }

            Card high = a.Rank > b.Rank ? a : b;
            Card low = a.Rank > b.Rank ? b : a;
            char suitedness = high.Suit == low.Suit ? 's' : 'o';
            return string.Concat(RankToChar(high.Rank), RankToChar(low.Rank), suitedness);
        }

        public static string NormalizeCardText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c) || c == ',' || c == ';' || c == '|' || c == '/')
                {
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static int CharToRank(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'A': return 14;
                case 'K': return 13;
                case 'Q': return 12;
                case 'J': return 11;
                case 'T': return 10;
                case '9': return 9;
                case '8': return 8;
                case '7': return 7;
                case '6': return 6;
                case '5': return 5;
                case '4': return 4;
                case '3': return 3;
                case '2': return 2;
                default: return -1;
            }
        }

        public static char RankToChar(int rank)
        {
            switch (rank)
            {
                case 14: return 'A';
                case 13: return 'K';
                case 12: return 'Q';
                case 11: return 'J';
                case 10: return 'T';
                case 9: return '9';
                case 8: return '8';
                case 7: return '7';
                case 6: return '6';
                case 5: return '5';
                case 4: return '4';
                case 3: return '3';
                case 2: return '2';
                default: return '?';
            }
        }
    }

    sealed class ScenarioInput
    {
        public Position HeroPosition { get; set; }
        public Position VillainPosition { get; set; }
        public Street Street { get; set; }
        public PotType PotType { get; set; }
        public ScenarioType Scenario { get; set; }
        public OpponentProfile OpponentProfile { get; set; }
        public RangeShape RangeShape { get; set; }
        public int Players { get; set; }
        public bool HeroHasPosition { get; set; }
        public bool HeroWasPreflopAggressor { get; set; }
        public double PotSize { get; set; }
        public double FacingBetSize { get; set; }
        public double EffectiveStack { get; set; }
        public List<Card> HeroCards { get; set; }
        public List<Card> FlopCards { get; set; }
        public Card TurnCard { get; set; }
        public Card RiverCard { get; set; }

        public ScenarioInput()
        {
            Players = 2;
            HeroCards = new List<Card>();
            FlopCards = new List<Card>();
            PotSize = 10.0;
            FacingBetSize = 0.0;
            EffectiveStack = 100.0;
        }

        public List<Card> GetBoard()
        {
            var board = new List<Card>();
            if (Street == Street.Preflop)
            {
                return board;
            }

            if (FlopCards != null)
            {
                board.AddRange(FlopCards);
            }

            if (Street == Street.Turn || Street == Street.River)
            {
                if (TurnCard != null)
                {
                    board.Add(TurnCard);
                }
            }

            if (Street == Street.River && RiverCard != null)
            {
                board.Add(RiverCard);
            }

            return board;
        }

        public List<Card> GetPreviousStreetBoard()
        {
            var board = new List<Card>();
            if (Street == Street.Turn)
            {
                board.AddRange(FlopCards);
                return board;
            }

            if (Street == Street.River)
            {
                board.AddRange(FlopCards);
                if (TurnCard != null)
                {
                    board.Add(TurnCard);
                }
            }
            return board;
        }

        public double GetSpr()
        {
            if (PotSize <= 0.0)
            {
                return 0.0;
            }
            return EffectiveStack / PotSize;
        }

        public string HeroHandCode()
        {
            if (HeroCards == null || HeroCards.Count != 2)
            {
                return string.Empty;
            }
            return Card.ToHandCode(HeroCards[0], HeroCards[1]);
        }
    }

    sealed class BoardAnalysis
    {
        public string TextureLabel { get; set; }
        public string DetailLabel { get; set; }
        public int WetnessScore { get; set; }
        public bool IsDry { get; set; }
        public bool IsWet { get; set; }
        public bool IsPaired { get; set; }
        public bool IsTwoTone { get; set; }
        public bool IsMonotone { get; set; }
        public bool FlushPossible { get; set; }
        public bool StraightPossible { get; set; }
        public bool LatestCardIsBlank { get; set; }
        public int HighestBoardRank { get; set; }
    }

    sealed class HandAnalysis
    {
        public string CategoryName { get; set; }
        public string DetailName { get; set; }
        public int MadeStrengthScore { get; set; }
        public int DrawScore { get; set; }
        public int ShowdownScore { get; set; }
        public int VulnerabilityScore { get; set; }
        public bool HasPair { get; set; }
        public bool IsTopPairOrBetter { get; set; }
        public bool IsTwoPairPlus { get; set; }
        public bool IsMonster { get; set; }
        public bool HasFlushDraw { get; set; }
        public bool HasBackdoorFlushDraw { get; set; }
        public bool HasOpenEndedStraightDraw { get; set; }
        public bool HasGutshot { get; set; }
        public bool HasOvercards { get; set; }
        public bool HasNutBlocker { get; set; }
        public bool IsBluffCatcher { get; set; }
        public bool IsStrongDraw { get; set; }
    }

    sealed class RangeMetrics
    {
        public double HeroStrongPct { get; set; }
        public double VillainStrongPct { get; set; }
        public double HeroNutPct { get; set; }
        public double VillainNutPct { get; set; }
        public double HeroDrawPct { get; set; }
        public double VillainDrawPct { get; set; }
        public int HeroComboCount { get; set; }
        public int VillainComboCount { get; set; }
        public string SpotLabel { get; set; }
        public string HeroSummary { get; set; }
        public string VillainSummary { get; set; }
    }

    sealed class Recommendation
    {
        public string PrimaryAction { get; set; }
        public string SecondaryAction { get; set; }
        public string Summary { get; set; }
        public string Confidence { get; set; }
        public List<string> Reasons { get; private set; }
        public List<string> TheoryReferences { get; private set; }
        public List<KeyValuePair<string, string>> Metrics { get; private set; }

        public Recommendation()
        {
            PrimaryAction = string.Empty;
            SecondaryAction = string.Empty;
            Summary = string.Empty;
            Confidence = "理論近似";
            Reasons = new List<string>();
            TheoryReferences = new List<string>();
            Metrics = new List<KeyValuePair<string, string>>();
        }

        public void AddMetric(string name, string value)
        {
            Metrics.Add(new KeyValuePair<string, string>(name, value));
        }
    }

    sealed class Combo
    {
        public Card CardA { get; private set; }
        public Card CardB { get; private set; }

        public Combo(Card cardA, Card cardB)
        {
            CardA = cardA;
            CardB = cardB;
        }

        public bool Intersects(IEnumerable<Card> cards)
        {
            foreach (var card in cards)
            {
                if (CardA.Equals(card) || CardB.Equals(card))
                {
                    return true;
                }
            }
            return false;
        }

        public List<Card> ToList()
        {
            return new List<Card> { CardA, CardB };
        }
    }

    static class PositionHelper
    {
        public static bool IsBlind(Position position)
        {
            return position == Position.SB || position == Position.BB;
        }

        public static string ToJapanese(Position position)
        {
            switch (position)
            {
                case Position.UTG: return "UTG";
                case Position.MP: return "MP";
                case Position.CO: return "CO";
                case Position.BTN: return "BTN";
                case Position.SB: return "SB";
                case Position.BB: return "BB";
                default: return position.ToString();
            }
        }

        public static bool IsIpAgainstOpen(Position actor, Position opener)
        {
            if (actor == Position.SB || actor == Position.BB)
            {
                return false;
            }

            if (opener == Position.SB)
            {
                return actor == Position.BB;
            }

            return GetOrder(actor) > GetOrder(opener);
        }

        public static int GetOrder(Position position)
        {
            switch (position)
            {
                case Position.UTG: return 0;
                case Position.MP: return 1;
                case Position.CO: return 2;
                case Position.BTN: return 3;
                case Position.SB: return 4;
                case Position.BB: return 5;
                default: return 0;
            }
        }
    }
}
