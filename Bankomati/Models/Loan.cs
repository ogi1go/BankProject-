using System.Text.Json.Serialization;

namespace Bankomati.Models;

public class Loan
{
    //<summary>
    // json properties just for nice name in json 
    //</summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("remainingDebt")]
    public decimal RemainingDebt { get; set; }

    [JsonPropertyName("months")]
    public int Months { get; set; }

    [JsonPropertyName("remainingMonths")]
    public int RemainingMonths { get; set; }

    [JsonPropertyName("monthlyPayment")]
    public decimal MonthlyPayment { get; set; }

    [JsonPropertyName("annualInterestRate")]
    public decimal AnnualInterestRate { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    public static decimal CalculateMonthlyPayment(decimal amount, int months, decimal annualRatePercent)
    {
        if (annualRatePercent == 0)
            return decimal.Round(amount / months, 2);

        double monthlyRate = (double)(annualRatePercent / 100m / 12m);
        double factor = Math.Pow(1 + monthlyRate, months);
        double payment = (double)amount * (monthlyRate * factor) / (factor - 1);

        return decimal.Round((decimal)payment, 2);
    }
}