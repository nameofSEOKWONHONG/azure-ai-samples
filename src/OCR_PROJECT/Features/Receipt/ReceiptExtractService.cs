using Azure;
using Azure.AI.DocumentIntelligence;
using Document.Intelligence.Agent.Features.Receipt.Models;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Receipt;

public interface IReceiptExtractService
{
    Task<ReceiptExtract> ExtractReceiptAsync(BinaryData binary);
}

public class ReceiptExtractService : ServiceBase<ReceiptExtractService>, IReceiptExtractService
{
    private readonly DocumentIntelligenceClient _documentIntelligenceClient;

    public ReceiptExtractService(ILogger<ReceiptExtractService> logger, IDiaSessionContext sessionContext,
        DocumentIntelligenceClient documentIntelligenceClient) : base(logger, sessionContext)
    {
        _documentIntelligenceClient = documentIntelligenceClient;
    }

    public async Task<ReceiptExtract> ExtractReceiptAsync(BinaryData binary)
    {
        var options = new AnalyzeDocumentOptions("prebuilt-receipt", binary);
        options.Features.Add(DocumentAnalysisFeature.QueryFields);
        
        // 커스텀 항목에 대한 추출 기워드 선언
        options.QueryFields.Add("카드번호");
        var operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, options);
        var receipts = operation.Value;

        var extract = new ReceiptExtract();
        
        foreach (AnalyzedDocument receipt in receipts.Documents)
        {
            if (receipt.Fields.TryGetValue("MerchantName", out DocumentField merchantNameField))
            {
                if (merchantNameField.FieldType == DocumentFieldType.String)
                {
                    extract.Merchant = merchantNameField.ValueString;
                }
            }

            if (receipt.Fields.TryGetValue("MerchantAddress", out var merchantAddress))
            {
                if (merchantAddress.FieldType == DocumentFieldType.Address)
                {
                    extract.Address = merchantAddress.ValueString;
                }
            }
            
            if (receipt.Fields.TryGetValue("MerchantPhoneNumber", out var merchantPhoneNumber))
            {
                if (merchantPhoneNumber.FieldType == DocumentFieldType.PhoneNumber)
                {
                    extract.PhoneNumber = merchantPhoneNumber.ValueString;
                }
            }
            
            // 커스텀 추출 항목에 대한 추출
            if (receipt.Fields.TryGetValue("카드번호", out var 카드번호))
            {
                if (카드번호.FieldType == DocumentFieldType.String)
                {
                    extract.CardNumberMasked = 카드번호.ValueString;
                }
            }

            if (receipt.Fields.TryGetValue("TransactionDate", out DocumentField transactionDateField))
            {
                if (transactionDateField.FieldType == DocumentFieldType.Date)
                {
                    extract.TransactionDate = transactionDateField.ValueDate;
                }
            }

            if (receipt.Fields.TryGetValue("TransactionTime", out var transactionTimeField))
            {
                if (transactionTimeField.FieldType == DocumentFieldType.Time)
                {
                    extract.TransactionTime = transactionTimeField.ValueTime;
                }
            }
            
            if (receipt.Fields.TryGetValue("Total", out DocumentField totalField))
            {
                if (totalField.FieldType == DocumentFieldType.Currency)
                {
                    extract.TotalAmountWon = totalField.ValueCurrency.Amount; 
                }
            }
        }

        return extract;
    }
}