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
            sb.AppendLine("pot odds と MDF は出発点だが、MDF は固定の防衛義務ではなく");
            sb.AppendLine("相手のブラフを自動利益にしないための基準として扱う。");
            sb.AppendLine("高エクイティ手が常に高 EV ではない。単体 hand よりも");
            sb.AppendLine("range 対 range と equity realization を重視する。");
            sb.AppendLine();
            sb.AppendLine("2. プリフロップ");
            sb.AppendLine("IP/OOP を単純に IP=コール多め、OOP=再レイズ多めとは置かない。");
            sb.AppendLine("call / 3bet / 4bet はポジション、相手レンジの極性、スタック深さで混ぜる。");
            sb.AppendLine("AK や QQ のような強いが超 premium ではない手は");
            sb.AppendLine("特に IP ではコールが高 EV になる spot も多い。");
            sb.AppendLine();
            sb.AppendLine("3. フロップ IP");
            sb.AppendLine("防衛頻度は bet size 依存で、60〜70% は多くの board の近似にすぎない。");
            sb.AppendLine("乾いた board では低め、dynamic board や position が強い spot では広めに守りやすい。");
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
            sb.AppendLine("check-calling が難しい texture では check-raise を増やすが、");
            sb.AppendLine("showdown value のある中強度 hand まで一律に攻撃へ寄せない。");
            sb.AppendLine();
            sb.AppendLine("6. Turn");
            sb.AppendLine("turn は street が 1 枚減るので bluff:value 比は flop より value 寄り。");
            sb.AppendLine("IP では相手が condensed なら large bet、polarized なら small bet が機能しやすい。");
            sb.AppendLine("OOP の draw は低 showdown value なら攻撃ラインに回しやすいが、");
            sb.AppendLine("実現性の高い draw は check-call も十分残る。");
            sb.AppendLine();
            sb.AppendLine("7. River");
            sb.AppendLine("river は equity が EV に最も直結する。");
            sb.AppendLine("IP の thin value は call されたとき 50% 超が出発点だが、");
            sb.AppendLine("実戦では raise の可能性と bet/check の比較 EV まで見る。");
            sb.AppendLine("removal effect は river bluff の中心概念で、");
            sb.AppendLine("call range を block し fold range を unblock するハンドから bluff する。");
            sb.AppendLine("missed draw が少ないときは弱い made hand を bluff 候補にできるが、");
            sb.AppendLine("最終的には blocker の質を優先する。");
            sb.AppendLine();
            sb.AppendLine("8. Overbet");
            sb.AppendLine("blank runout で相手 range が capped / condensed なら overbet が有効。");
            sb.AppendLine("逆に split が多い board や自分が call range を block しすぎる spot では overbet を落とす。");
            sb.AppendLine();
            sb.AppendLine("9. Stack Depth");
            sb.AppendLine("浅い stack では absolute equity と top pair 系の即時価値を重視。");
            sb.AppendLine("深い stack では nut potential と implied odds を重視し、");
            sb.AppendLine("Axs / suited connector の価値が上がる。");
            sb.AppendLine();
            sb.AppendLine("10. このアプリの前提");
            sb.AppendLine("完全 solver ではなく、Janda 本の理論を基にした均衡寄りヒューリスティック。");
            sb.AppendLine("推奨は MDF, pot odds, board texture, SPR, position, blocker,");
            sb.AppendLine("range shape, preflop chart をまとめて近似している。");
            return sb.ToString();
        }
    }
}
