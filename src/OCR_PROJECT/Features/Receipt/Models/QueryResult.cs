namespace Document.Intelligence.Agent.Features.Receipt.Models;

public class QueryResult
{
    public string Id { get; set; }
    public string Merchant { get; set; }
    public string BizNo { get; set; }
    public string CardNo { get; set; }
    public DateTime TrxAt { get; set; }
    public long TotalWon { get; set; }
    public double? Score { get; set; }

    public override string ToString()
    {
        return $"[{Id}] {Merchant}({BizNo}) / {TrxAt} / {TotalWon}원 / CardNo={CardNo} / match_score={Score}";
    }
} 