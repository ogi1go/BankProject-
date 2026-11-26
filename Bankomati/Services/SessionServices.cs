
using System.Security.Cryptography;
using System.Text;
using Bankomati.Models;
using Bankomati.Utils;

namespace Bankomati.Services;

internal class SessionServices
{
    private readonly JsonFileUtils fileUtils = new("users.json", "[]");
    private readonly XmlFileUtils sessionFile = new("session.xml");

    private List<Account> accounts = new();

    public Account? CurrentAccount { get; private set; }

    public bool IsLoggedIn => CurrentAccount is not null;

    public SessionServices()
    {
        accounts = fileUtils.Read<List<Account>>() ?? new();

        if (File.Exists("session.xml"))
        {
            try
            {
                SessionData? savedSession = sessionFile.Read<SessionData>();

                if (savedSession is not null)
                {
                    CurrentAccount = accounts.FirstOrDefault(
                        a => a.Id == savedSession.AccountId
                    );
                }
            }
            catch (Exception)
            {
                CurrentAccount = null;

                if (File.Exists("session.xml"))
                    File.Delete("session.xml");
            }
        }
    }

    public Account Register(string name, string phoneNumber, string email, string password)
    {
        Account account = new Account(name, phoneNumber, email, password, false, Status.User);

        if (accounts.Any(a => a.Email.Equals(account.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("An account with this email already exists.");

        if (accounts.Any(a => a.PhoneNumber == account.PhoneNumber))
            throw new InvalidOperationException("An account with this phone number already exists.");

        account.Verified = false;

        accounts.Add(account);
        fileUtils.Write(accounts);

        return account;
    }

    public Account Login(string email, string password)
    {
        if (IsLoggedIn)
            throw new InvalidOperationException("You are already logged in.");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        Account? account = accounts.FirstOrDefault(a =>
            a.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

        if (account is null || !PasswordsMatch(account.Password, password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        CurrentAccount = account;

        sessionFile.Write(new SessionData
        {
            AccountId = account.Id
        });

        return account;
    }

    public void Logout()
    {
        EnsureLoggedIn();

        CurrentAccount = null;

        if (File.Exists("session.xml"))
            File.Delete("session.xml");
    }

    public void ChangePassword(string oldPassword, string newPassword)
    {
        Account account = EnsureLoggedIn();

        if (!PasswordsMatch(account.Password, oldPassword))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        if (PasswordsMatch(account.Password, newPassword))
            throw new ArgumentException("New password must be different.");

        account.Password = newPassword;
        fileUtils.Write(accounts);
    }

    public void MarkEmailVerified()
    {
        Account account = EnsureLoggedIn();

        account.Verified = true;
        fileUtils.Write(accounts);
    }

    private Account EnsureLoggedIn()
    {
        return CurrentAccount ??
               throw new InvalidOperationException("You are not logged in.");
    }

    private static bool PasswordsMatch(string expected, string actual)
    {
        if (actual is null)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(actual));
    }

    public BankCard CreateCard(CardType type)
    {
        Account account = EnsureLoggedIn();

        if (account.Cards.Count >= 10)
            throw new InvalidOperationException("You can have maximum 10 cards!");

        (string number, string cvc) = BankCard.GenerateCardData();

        BankCard card = new BankCard
        {
            Number = number,
            Cvc = cvc,
            Type = type,
            ExpiryDate = DateTime.Today.AddYears(5),
            Balance = 0,
            CreditLimit = type == CardType.Credit ? 1000 : 0,
            Debt = 0
        };

        account.Cards.Add(card);
        fileUtils.Write(accounts);

        return card;
    }

    public List<BankCard> GetCards()
    {
        return EnsureLoggedIn().Cards;
    }

    public BankCard GetCard(int index)
    {
        List<BankCard> cards = GetCards();

        if (index < 1 || index > cards.Count)
            throw new ArgumentException("Invalid card number!");

        return cards[index - 1];
    }

    public void DeleteCard(int index)
    {
        BankCard card = GetCard(index);

        if (card.Type == CardType.Debit && card.Balance > 0)
            throw new InvalidOperationException(
                "Cannot delete a card with a positive balance. Withdraw or transfer the money first.");

        if (card.Type == CardType.Credit && card.Debt > 0)
            throw new InvalidOperationException(
                "Cannot delete a credit card with outstanding debt. Repay it first.");

        CurrentAccount!.Cards.Remove(card);
        fileUtils.Write(accounts);
    }

    public BankCard Deposit(int index, decimal amount)
    {
        ValidateAmount(amount);

        BankCard card = GetCard(index);

        if (card.Type == CardType.Debit)
        {
            card.Balance += amount;
        }
        else
        {
            if (amount > card.Debt)
                throw new InvalidOperationException(
                    $"Amount exceeds your debt ({card.Debt:F2} GEL).");

            card.Debt -= amount;
        }

        fileUtils.Write(accounts);
        return card;
    }

    public BankCard Withdraw(int index, decimal amount)
    {
        ValidateAmount(amount);

        BankCard card = GetCard(index);

        if (card.Type == CardType.Debit)
        {
            if (amount > card.Balance)
                throw new InvalidOperationException(
                    $"Insufficient funds. Balance: {card.Balance:F2} GEL.");

            card.Balance -= amount;
        }
        else
        {
            if (amount > card.AvailableCredit)
                throw new InvalidOperationException(
                    $"Exceeds available credit ({card.AvailableCredit:F2} GEL).");

            card.Debt += amount;
        }

        fileUtils.Write(accounts);
        return card;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Amount can have at most 2 decimal places.");
    }

    public Loan TakeLoan(int debitCardIndex, decimal amount, int months, decimal monthlyIncome, decimal willingMonthlyPayment)
    {
        Account account = EnsureLoggedIn();

        ValidateAmount(amount);
        ValidateAmount(monthlyIncome);
        ValidateAmount(willingMonthlyPayment);

        if (amount < 100 || amount > 50000)
            throw new ArgumentException("Loan amount must be between 100 and 50,000 GEL.");

        if (months < 3 || months > 60)
            throw new ArgumentException("Loan term must be between 3 and 60 months.");

        BankCard card = GetCard(debitCardIndex);

        if (card.Type != CardType.Debit)
            throw new InvalidOperationException("Loan funds can only be deposited to a Debit card.");

        const decimal annualRate = 15.0m;
        decimal requiredMonthlyPayment = Loan.CalculateMonthlyPayment(amount, months, annualRate);

        if (willingMonthlyPayment < requiredMonthlyPayment)
        {
            throw new InvalidOperationException(
                $"Loan rejected: Minimum required monthly payment for this loan is {requiredMonthlyPayment:F2} GEL (at {annualRate}% APR), which is more than you are willing to pay ({willingMonthlyPayment:F2} GEL).");
        }

        if (monthlyIncome < 500)
            throw new InvalidOperationException("Loan rejected: Minimum monthly income must be at least 500.00 GEL.");

        decimal existingMonthlyPayments = account.Loans.Sum(l => l.MonthlyPayment);
        decimal totalMonthlyPayments = existingMonthlyPayments + requiredMonthlyPayment;
        decimal maxAllowedPayment = monthlyIncome * 0.45m;

        if (totalMonthlyPayments > maxAllowedPayment)
        {
            throw new InvalidOperationException(
                $"Loan rejected: High debt-to-income ratio. Your total monthly payments ({totalMonthlyPayments:F2} GEL) exceed 45% of your income ({maxAllowedPayment:F2} GEL).");
        }

        decimal totalToRepay = decimal.Round(requiredMonthlyPayment * months, 2);

        Loan loan = new Loan
        {
            Amount = amount,
            RemainingDebt = totalToRepay,
            Months = months,
            RemainingMonths = months,
            MonthlyPayment = requiredMonthlyPayment,
            AnnualInterestRate = annualRate
        };

        account.Loans.Add(loan);
        card.Balance += amount;

        fileUtils.Write(accounts);
        return loan;
    }

    public Loan PayLoan(int loanIndex, int debitCardIndex)
    {
        Account account = EnsureLoggedIn();

        if (loanIndex < 1 || loanIndex > account.Loans.Count)
            throw new ArgumentException("Invalid loan number!");

        Loan loan = account.Loans[loanIndex - 1];
        BankCard card = GetCard(debitCardIndex);

        if (card.Type != CardType.Debit)
            throw new InvalidOperationException("Loans can only be paid from a Debit card.");

        decimal payment = Math.Min(loan.MonthlyPayment, loan.RemainingDebt);

        if (card.Balance < payment)
            throw new InvalidOperationException($"Insufficient funds on card. Required: {payment:F2} GEL.");

        card.Balance -= payment;
        loan.RemainingDebt -= payment;
        loan.RemainingMonths = Math.Max(0, loan.RemainingMonths - 1);

        if (loan.RemainingDebt <= 0)
        {
            account.Loans.Remove(loan);
        }

        fileUtils.Write(accounts);
        return loan;
    }

    public List<Loan> GetLoans()
    {
        return EnsureLoggedIn().Loans;
    }

    private Account EnsureAdmin()
    {
        Account current = EnsureLoggedIn();

        if (current.Status != Status.Admin)
            throw new UnauthorizedAccessException("Access denied: Admin status required.");

        return current;
    }

    public List<Account> GetAllUsers()
    {
        EnsureAdmin();
        return accounts;
    }

    public Account GetUserByIndex(int index)
    {
        EnsureAdmin();

        if (index < 1 || index > accounts.Count)
            throw new ArgumentOutOfRangeException(nameof(index), "Invalid user number!");

        return accounts[index - 1];
    }

    public void DeleteUser(int index)
    {
        Account admin = EnsureAdmin();
        Account target = GetUserByIndex(index);

        if (target.Id == admin.Id)
            throw new InvalidOperationException("You cannot delete your own admin account.");

        accounts.Remove(target);
        fileUtils.Write(accounts);
    }

    public Account SetUserStatus(int index, Status newStatus)
    {
        Account admin = EnsureAdmin();
        Account target = GetUserByIndex(index);

        if (target.Id == admin.Id && newStatus != Status.Admin)
            throw new InvalidOperationException("You cannot demote yourself from Admin.");

        target.Status = newStatus;
        fileUtils.Write(accounts);
        return target;
    }

    public BankCard DeductUserCardBalance(int userIndex, int cardIndex, decimal amount)
    {
        EnsureAdmin();
        ValidateAmount(amount);

        Account target = GetUserByIndex(userIndex);

        if (cardIndex < 1 || cardIndex > target.Cards.Count)
            throw new ArgumentOutOfRangeException(nameof(cardIndex), "Invalid card number!");

        BankCard card = target.Cards[cardIndex - 1];

        if (card.Type == CardType.Debit)
        {
            if (amount > card.Balance)
                throw new InvalidOperationException(
                    $"Cannot deduct {amount:F2} GEL. Card balance is only {card.Balance:F2} GEL.");

            card.Balance -= amount;
        }
        else
        {
            if (amount > card.AvailableCredit)
                throw new InvalidOperationException(
                    $"Exceeds available credit ({card.AvailableCredit:F2} GEL).");

            card.Debt += amount;
        }

        fileUtils.Write(accounts);
        return card;
    }
}
