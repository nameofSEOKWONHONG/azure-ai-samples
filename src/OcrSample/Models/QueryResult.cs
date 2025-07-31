namespace OcrSample.Models;

public class QueryResult
{
    public string Id { get; set; }
    public string Merchant { get; set; }
    public DateTime TrxAt { get; set; }
    public long TotalWon { get; set; }
    public double? Score { get; set; }

    public override string ToString()
    {
        return $"[{Id}] {Merchant} / {TrxAt} / {TotalWon}원 / match_score={Score}";
    }
} 