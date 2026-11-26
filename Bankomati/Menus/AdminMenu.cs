
using System.Globalization;
using Bankomati.Models;
using Bankomati.Services;

namespace Bankomati.Menus;

internal class AdminMenu
{
    private readonly SessionServices _session;

    public AdminMenu(SessionServices session)
    {
        _session = session;
    }

    public void Show()
    {
        bool back = false;

        while (!back)
        {
            Console.Clear();
            Console.WriteLine("==========================================");
            Console.WriteLine("              ADMIN PANEL                 ");
            Console.WriteLine("==========================================");
            Console.WriteLine("1. View all users");
            Console.WriteLine("2. Delete user");
            Console.WriteLine("3. Deduct money from user's card");
            Console.WriteLine("4. Change user status (User / Admin)");
            Console.WriteLine("0. Back");

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
                        PrintUsersTable();
                        break;

                    case 2:
                        DeleteUserFlow();
                        break;

                    case 3:
                        DeductBalanceFlow();
                        break;

                    case 4:
                        ChangeStatusFlow();
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

    private void PrintUsersTable()
    {
        List<Account> users = _session.GetAllUsers();

        Console.WriteLine("\n=== Registered Users ===");

        if (users.Count == 0)
        {
            Console.WriteLine("No users found.");
            return;
        }

        Console.WriteLine($"{"#",-3} | {"Name",-18} | {"Email",-26} | {"Phone",-15} | {"Status",-6} | {"Ver",-3} | {"Cards",-5} | {"Total Debit (GEL)",16}");
        Console.WriteLine(new string('-', 115));

        for (int i = 0; i < users.Count; i++)
        {
            Account u = users[i];

            decimal totalDebit = u.Cards
                .Where(c => c.Type == CardType.Debit)
                .Sum(c => c.Balance);

            string verifiedMark = u.Verified ? "Yes" : "No";

            Console.WriteLine(
                $"{i + 1,-3} | " +
                $"{Truncate(u.Name, 18),-18} | " +
                $"{Truncate(u.Email, 26),-26} | " +
                $"{Truncate(u.PhoneNumber, 15),-15} | " +
                $"{u.Status,-6} | " +
                $"{verifiedMark,-3} | " +
                $"{u.Cards.Count,-5} | " +
                $"{totalDebit,16:F2}");
        }
    }

    private void DeleteUserFlow()
    {
        PrintUsersTable();

        int userIndex = ReadInt("\nEnter user # to delete: ");
        Account target = _session.GetUserByIndex(userIndex);

        Console.Write($"Are you sure you want to delete user '{target.Name}' ({target.Email})? (y/n): ");

        string answer = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

        if (answer != "y")
        {
            Console.WriteLine("Cancelled.");
            return;
        }

        _session.DeleteUser(userIndex);
        Console.WriteLine("\nUser deleted successfully!");
    }

    private void DeductBalanceFlow()
    {
        PrintUsersTable();

        int userIndex = ReadInt("\nEnter user #: ");
        Account target = _session.GetUserByIndex(userIndex);

        if (target.Cards.Count == 0)
        {
            Console.WriteLine($"\nUser '{target.Name}' has no cards.");
            return;
        }

        Console.WriteLine($"\n=== Cards of {target.Name} ===");

        for (int i = 0; i < target.Cards.Count; i++)
        {
            BankCard c = target.Cards[i];

            if (c.Type == CardType.Debit)
                Console.WriteLine($"{i + 1}. Debit  | {c.MaskedNumber} | Balance: {c.Balance:F2} GEL");
            else
                Console.WriteLine($"{i + 1}. Credit | {c.MaskedNumber} | Debt: {c.Debt:F2}/{c.CreditLimit:F2} GEL");
        }

        int cardIndex = ReadInt("\nEnter card #: ");
        decimal amount = ReadDecimal("Amount to deduct (GEL): ");

        BankCard updatedCard = _session.DeductUserCardBalance(userIndex, cardIndex, amount);

        Console.WriteLine("\nBalance updated successfully!");

        if (updatedCard.Type == CardType.Debit)
            Console.WriteLine($"New Debit Balance: {updatedCard.Balance:F2} GEL");
        else
            Console.WriteLine($"New Credit Debt: {updatedCard.Debt:F2} GEL (Available: {updatedCard.AvailableCredit:F2} GEL)");
    }

    private void ChangeStatusFlow()
    {
        PrintUsersTable();

        int userIndex = ReadInt("\nEnter user #: ");
        Account target = _session.GetUserByIndex(userIndex);

        Console.WriteLine($"\nCurrent status of '{target.Name}': {target.Status}");
        Console.WriteLine("1. User");
        Console.WriteLine("2. Admin");

        int statusChoice = ReadInt("Choose new status: ");

        Status newStatus = statusChoice switch
        {
            1 => Status.User,
            2 => Status.Admin,
            _ => throw new ArgumentException("Invalid status option!")
        };

        Account updated = _session.SetUserStatus(userIndex, newStatus);

        Console.WriteLine($"\nStatus for '{updated.Name}' updated to {updated.Status}!");
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? "";

        return maxLength > 1 ? value[..(maxLength - 1)] + "…" : value[..maxLength];
    }

    private static int ReadInt(string prompt)
    {
        Console.Write(prompt);

        if (!int.TryParse(Console.ReadLine(), out int val))
            throw new ArgumentException("Invalid integer format!");

        return val;
    }

    private static decimal ReadDecimal(string prompt)
    {
        Console.Write(prompt);

        string input = (Console.ReadLine() ?? "").Trim().Replace(',', '.');

        if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal val))
            throw new ArgumentException("Invalid amount format!");

        return val;
    }
}
