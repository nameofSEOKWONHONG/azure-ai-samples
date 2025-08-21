namespace Document.Intelligence.Agent.Features.Receipt.Models;

public class ReceiptFeatureOption
{
    /// <summary>
    /// (ex: receipt-v1)
    /// </summary>
    public string IndexName { get; init; }
    /// <summary>
    /// (ex: vec-profile)
    /// </summary>
    public string VectorProfile { get; init; }
    /// <summary>
    /// (ex: hnsw-config)
    /// </summary>
    public string AlgorithmConfigurationName { get; init; }
}
