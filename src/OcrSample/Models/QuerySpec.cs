namespace OcrSample.Models;

public class QuerySpec
{
    public string? Brand { get; set; }
    public string? Branch { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public long? MinWon { get; set; }
    public long? MaxWon { get; set; }
    public string[] Keywords { get; set; }
    public bool UseHybrid { get; set; } = false;
    public int? TopK { get; set; } = 10;
    public int? Page { get; set; } = 1;
    public SortMode Sort { get; set; } = SortMode.Latest;

}

public enum SortMode
{
    Relevance,     // search.score desc (하이브리드/벡터일 때 권장)
    Latest,        // trxAt desc
    AmountDesc,    // totalWon desc
    AmountAsc      // totalWon asc
}