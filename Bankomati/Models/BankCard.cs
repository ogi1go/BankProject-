using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Bankomati.Models;

public enum CardType
{
    Debit,
    Credit
}

internal class BankCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [JsonPropertyName("number")]
    public string Number { get; set; } = "";

    [JsonPropertyName("cvc")]
    public string Cvc { get; set; } = "";
    [JsonPropertyName("type")]
    public CardType Type { get; set; }
    [JsonPropertyName("expiry_date")]
    public DateTime ExpiryDate { get; set; }
    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
    [JsonPropertyName("credit_limit")]
    public decimal CreditLimit { get; set; }
    [JsonPropertyName("debt")]
    public decimal Debt { get; set; }

    [JsonIgnore]
    public decimal AvailableCredit => CreditLimit - Debt;
    [JsonPropertyName("masked_number")]
    public string MaskedNumber =>
        "**** **** **** " + Number[^4..];

    
    public static (string number, string cvc) GenerateCardData()
    {
        string number = "";
        for (int i = 0; i < 16; i++)
        {
            number += RandomNumberGenerator.GetInt32(0, 10);
        }

        string cvc = "";
        for (int i = 0; i < 3; i++)
        {
            cvc += RandomNumberGenerator.GetInt32(0, 10);
        }

        return (number, cvc);
    }
}