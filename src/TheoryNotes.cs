using System.Text;

namespace NLHETheoryAdvisor
{
    static class TheoryNotes
    {
        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Applications of No-Limit Hold'em を実装用に要約したノート");
            sb.AppendLine();
            sb.AppendLine("1. 基本");
            sb.AppendLine("pot odds と MDF を起点にしつつ、実戦判断は常に range 対 range で考える。");
            sb.AppendLine("高エクイティ手が常に高 EV ではない。将来ストリートで equity を保てるか、");
            sb.AppendLine("相手の calling / raising range に対してどれだけ強く残るかが重要。");
            sb.AppendLine();
            sb.AppendLine("2. プリフロップ");
            sb.AppendLine("IP ではコール防衛、OOP では再レイズ防衛が増える。");
            sb.AppendLine("3bet / 4bet は下位レンジを強く罰するが、AK や QQ のような強いが超 premium ではない手は");
            sb.AppendLine("常に再レイズ最優先ではない。");
            sb.AppendLine();
            sb.AppendLine("3. フロップ IP");
            sb.AppendLine("ベットに直面したら 60〜70% 程度の防衛が必要になる場面が多い。");
            sb.AppendLine("乾いたボードでは超強ハンドをコール止めして turn / river で raise を遅らせる。");
            sb.AppendLine("ウェットなボードでは strong but vulnerable hand を今の street で raise して保護する。");
            sb.AppendLine();
            sb.AppendLine("4. チェックをもらった IP");
            sb.AppendLine("value hand は 2〜3 street 取れるかを先に考える。");
            sb.AppendLine("脆い marginal made hand は flop から取りに行く。");
            sb.AppendLine("脆くない marginal made hand は check back して later street で thin value に回す。");
            sb.AppendLine("showdown value の乏しい draw / overcard は早い street で bluff 候補。");
            sb.AppendLine();
            sb.AppendLine("5. OOP");
            sb.AppendLine("OOP は flop の bet / check-call / check-raise の配分が重要。");
            sb.AppendLine("check-calling が難しい texture ほど check-raise を増やし、");
            sb.AppendLine("IP 側が any two で利益を出せないようにする。");
            sb.AppendLine();
            sb.AppendLine("6. Turn");
            sb.AppendLine("turn は street が 1 枚減るので bluff:value 比は flop より value 寄り。");
            sb.AppendLine("IP では相手が condensed なら large bet、polarized なら small bet が機能しやすい。");
            sb.AppendLine("OOP の draw は check-call より bet / check-raise に回した方が理論的に扱いやすい。");
            sb.AppendLine();
            sb.AppendLine("7. River");
            sb.AppendLine("river は equity が EV に最も直結する。");
            sb.AppendLine("IP で thin value を打つなら call されたとき 50% 超で勝つ必要がある。");
            sb.AppendLine("removal effect は river bluff の中心概念で、");
            sb.AppendLine("call range を block し fold range を unblock するハンドから bluff する。");
            sb.AppendLine("missed draw が少ないときは、最弱の made hand を bluff に回す。");
            sb.AppendLine();
            sb.AppendLine("8. Overbet");
            sb.AppendLine("blank runout で相手 range が capped / condensed なら overbet が有効。");
            sb.AppendLine("逆に split が多い board や自分が call range を block しすぎる spot では overbet を落とす。");
            sb.AppendLine();
            sb.AppendLine("9. Stack Depth");
            sb.AppendLine("浅い stack では absolute equity を重視。");
            sb.AppendLine("深い stack では nut potential を重視し、Axs / suited connector の価値が上がる。");
            sb.AppendLine();
            sb.AppendLine("10. このアプリの前提");
            sb.AppendLine("完全 solver ではなく、Janda 本の理論を基にした均衡寄りヒューリスティック。");
            sb.AppendLine("推奨は MDF, pot odds, board texture, SPR, position, blocker, preflop chart をまとめて近似している。");
            return sb.ToString();
        }
    }
}
