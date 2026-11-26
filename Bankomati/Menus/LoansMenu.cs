using System.Globalization;
using Bankomati.Models;
using Bankomati.Services;

namespace Bankomati.Menus;

internal class LoansMenu
{
    private readonly SessionServices _session;

    public LoansMenu(SessionServices session)
    {
        _session = session;
    }

    public void Show()
    {
        bool back = false;

        while (!back)
        {
            Console.Clear();
            Console.WriteLine("=== Loans & Credits ===");
            Console.WriteLine("1. Apply for a loan");
            Console.WriteLine("2. My active loans");
            Console.WriteLine("3. Make monthly loan payment");
            Console.WriteLine("0. Back to menu");

            Console.Write("\nChoose: ");

            if (!int.TryParse(Console.ReadLine(), out int choice))
            {
                Console.WriteLine("Invalid choice!");
                Console.ReadKey();
                continue;
            }

            try
            {
                switch (choice)
                {
                    case 1:
                        ApplyForLoan();
                        break;

                    case 2:
                        ListLoans();
                        break;

                    case 3:
                        MakePayment();
                        break;

                    case 0:
                        back = true;
                        break;

                    default:
                        Console.WriteLine("Unknown option!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }

            if (!back)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private void ApplyForLoan()
    {
        var cards = _session.GetCards();
        if (!cards.Any(c => c.Type == CardType.Debit))
            throw new InvalidOperationException("You need at least one Debit card to receive loan funds.");

        Console.WriteLine("\n=== Loan Application (15% Annual Rate) ===");

        decimal amount = ReadDecimal("Desired loan amount (100 - 50,000): ");
        int months = ReadInt("Loan term in months (3 - 60): ");
        decimal income = ReadDecimal("Your net monthly income: ");
        decimal willingPayment = ReadDecimal("How much can you pay per month minimum: (): ");

        decimal estimatedPayment = Loan.CalculateMonthlyPayment(amount, months, 15.0m);
        Console.WriteLine($"\nCalculated bank monthly payment: {estimatedPayment:F2} ");

        Console.WriteLine("\nSelect Debit card to receive funds:");
        for (int i = 0; i < cards.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {cards[i].Type} - {cards[i].MaskedNumber} (Balance: {cards[i].Balance:F2} )");
        }

        int cardIndex = ReadInt("\nEnter card number: ");

        var loan = _session.TakeLoan(cardIndex, amount, months, income, willingPayment);

        Console.WriteLine("\nCongratulations! Your loan has been APPROVED!");
        Console.WriteLine($"Loan Amount: {loan.Amount:F2}");
        Console.WriteLine($"Term: {loan.Months} months");
        Console.WriteLine($"Monthly Payment: {loan.MonthlyPayment:F2}");
        Console.WriteLine($"Total to Repay: {loan.RemainingDebt:F2}");
    }

    private void ListLoans()
    {
        var loans = _session.GetLoans();
        Console.WriteLine("\n=== Your Active Loans ===");

        if (loans.Count == 0)
        {
            Console.WriteLine("You have no active loans.");
            return;
        }

        for (int i = 0; i < loans.Count; i++)
        {
            var l = loans[i];
            Console.WriteLine($"{i + 1}. Principal: {l.Amount:F2} GEL | Remaining Debt: {l.RemainingDebt:F2} GEL | Monthly: {l.MonthlyPayment:F2} GEL | Months left: {l.RemainingMonths}/{l.Months}");
        }
    }

    private void MakePayment()
    {
        var loans = _session.GetLoans();
        if (loans.Count == 0)
        {
            Console.WriteLine("\nYou have no active loans to pay.");
            return;
        }

        ListLoans();
        int loanIndex = ReadInt("\nSelect loan number to pay: ");

        var cards = _session.GetCards();
        Console.WriteLine("\nSelect Debit card to pay from:");
        for (int i = 0; i < cards.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {cards[i].Type} - {cards[i].MaskedNumber} (Balance: {cards[i].Balance:F2} GEL)");
        }

        int cardIndex = ReadInt("\nEnter card number: ");

        var updatedLoan = _session.PayLoan(loanIndex, cardIndex);

        if (updatedLoan.RemainingDebt <= 0)
            Console.WriteLine("\nLoan fully repaid! Congratulations!");
        else
            Console.WriteLine($"\nPayment successful! Remaining debt: {updatedLoan.RemainingDebt:F2} GEL ({updatedLoan.RemainingMonths} months left).");
    }

    private static decimal ReadDecimal(string prompt)
    {
        Console.Write(prompt);
        string input = (Console.ReadLine() ?? "").Trim().Replace(',', '.');

        if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
            throw new ArgumentException("Invalid number format!");

        return value;
    }

    private static int ReadInt(string prompt)
    {
        Console.Write(prompt);
        if (!int.TryParse(Console.ReadLine(), out int value))
            throw new ArgumentException("Invalid integer format!");

        return value;
    }
}