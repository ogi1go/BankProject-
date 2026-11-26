using Bankomati.Models;
using Bankomati.Services;

namespace Bankomati.Menus;
//<summary>
// user's card menu , sadac you can create card, list cards check balance,  card details, deposit, delete ,withdraw and etc.
//</summary>
internal class CardsMenu
{
    private readonly SessionServices _session;

    public CardsMenu(SessionServices session)
    {
        _session = session;
    }

    public void Show()
    {
        bool back = false;

        while (!back)
        {
            Console.Clear();

            Console.WriteLine("=== My Cards ===");
            Console.WriteLine("1. Create card");
            Console.WriteLine("2. List my cards");
            Console.WriteLine("3. Check card balance");
            Console.WriteLine("4. Card details");
            Console.WriteLine("5. Delete card");
            Console.WriteLine("6. Deposit money");
            Console.WriteLine("7. Withdraw money");
            Console.WriteLine("8. Loans & Credits");
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
                        {
                            Console.WriteLine("\nChoose card type:");
                            Console.WriteLine("1. Debit card");
                            Console.WriteLine("2. Credit card");

                            Console.Write("Choose: ");

                            if (!int.TryParse(Console.ReadLine(), out int type))
                                throw new ArgumentException("Invalid card type!");

                            CardType cardType = type switch
                            {
                                1 => CardType.Debit,
                                2 => CardType.Credit,
                                _ => throw new ArgumentException("Invalid card type!")
                            };

                            var card = _session.CreateCard(cardType);

                            Console.WriteLine("\nCard created successfully!");
                            Console.WriteLine($"Number: {card.MaskedNumber}");
                            Console.WriteLine($"Type: {card.Type}");
                            break;
                        }

                    case 2:
                        {
                            var cards = _session.GetCards();

                            Console.WriteLine("\n=== Your Cards ===");

                            if (cards.Count == 0)
                            {
                                Console.WriteLine("You don't have any cards.");
                                break;
                            }

                            for (int i = 0; i < cards.Count; i++)
                            {
                                Console.WriteLine(
                                    $"{i + 1}. {cards[i].Type} - {cards[i].MaskedNumber}");
                            }

                            break;
                        }

                    case 3:
                        {
                            var cards = _session.GetCards();

                            if (cards.Count == 0)
                            {
                                Console.WriteLine("\nYou don't have any cards.");
                                break;
                            }

                            Console.WriteLine("\n=== Your Cards ===");
                            for (int i = 0; i < cards.Count; i++)
                            {
                                Console.WriteLine(
                                    $"{i + 1}. {cards[i].Type} - {cards[i].MaskedNumber}");
                            }

                            Console.Write("\nEnter card number from list: ");

                            if (!int.TryParse(Console.ReadLine(), out int index))
                                throw new ArgumentException("Invalid input!");

                            var card = _session.GetCard(index);

                            Console.WriteLine("\n=== Card Balance ===");

                            if (card.Type == CardType.Debit)
                                Console.WriteLine($"Balance: {card.Balance:F2} GEL");
                            else
                            {
                                Console.WriteLine($"Credit limit: {card.CreditLimit:F2} GEL");
                                Console.WriteLine($"Debt: {card.Debt:F2} GEL");
                                Console.WriteLine($"Available: {card.AvailableCredit:F2} GEL");
                            }

                            break;
                        }

                    case 4:
                        {
                            var cards = _session.GetCards();

                            if (cards.Count == 0)
                            {
                                Console.WriteLine("\nYou don't have any cards.");
                                break;
                            }

                            Console.WriteLine("\n=== Your Cards ===");
                            for (int i = 0; i < cards.Count; i++)
                            {
                                Console.WriteLine(
                                    $"{i + 1}. {cards[i].Type} - {cards[i].MaskedNumber}");
                            }

                            Console.Write("\nEnter card number from list: ");

                            if (!int.TryParse(Console.ReadLine(), out int index))
                                throw new ArgumentException("Invalid input!");

                            var card = _session.GetCard(index);

                            Console.WriteLine("\n=== Card Details ===");
                            Console.WriteLine($"ID: {card.Id}");
                            Console.WriteLine($"Number: {card.Number}");  
                            Console.WriteLine($"CVC: {card.Cvc}");       
                            Console.WriteLine($"Type: {card.Type}");
                            Console.WriteLine($"Expires: {card.ExpiryDate:MM/yyyy}");

                            if (card.Type == CardType.Debit)
                                Console.WriteLine($"Balance: {card.Balance:F2} GEL");
                            else
                            {
                                Console.WriteLine($"Credit limit: {card.CreditLimit:F2} GEL");
                                Console.WriteLine($"Debt: {card.Debt:F2} GEL");
                                Console.WriteLine($"Available credit: {card.AvailableCredit:F2} GEL");
                            }

                            break;
                        }

                    case 5:
                        {
                            var cards = _session.GetCards();

                            if (cards.Count == 0)
                            {
                                Console.WriteLine("\nYou don't have any cards.");
                                break;
                            }

                            Console.WriteLine("\n=== Your Cards ===");
                            for (int i = 0; i < cards.Count; i++)
                            {
                                Console.WriteLine(
                                    $"{i + 1}. {cards[i].Type} - {cards[i].MaskedNumber}");
                            }

                            Console.Write("\nEnter card number to delete: ");

                            if (!int.TryParse(Console.ReadLine(), out int index))
                                throw new ArgumentException("Invalid input!");

                            var card = _session.GetCard(index);

                            Console.Write(
                                $"Delete {card.Type} card {card.MaskedNumber}? (y/n): ");

                            string answer = Console.ReadLine()?.Trim().ToLower() ?? "";

                            if (answer != "y")
                            {
                                Console.WriteLine("Cancelled.");
                                break;
                            }

                            _session.DeleteCard(index);

                            Console.WriteLine("\nCard deleted successfully!");
                            break;
                        }

                    case 6:
                        {
                            var cards = _session.GetCards();

                            if (cards.Count == 0)
                            {
                                Console.WriteLine("\nYou don't have any cards.");
                                break;
                            }

                            PrintCards(cards);

                            Console.Write("\nEnter card number: ");

                            if (!int.TryParse(Console.ReadLine(), out int index))
                                throw new ArgumentException("Invalid input!");

                            var card = _session.GetCard(index);

                            if (card.Type == CardType.Credit)
                                Console.WriteLine($"(Credit card: deposit repays debt. Current debt: {card.Debt:F2} GEL)");

                            decimal amount = ReadAmount("Amount to deposit (GEL): ");

                            card = _session.Deposit(index, amount);

                            Console.WriteLine("\nDeposit successful!");
                            PrintBalance(card);
                            break;
                        }

                    case 7:
                        {
                            var cards = _session.GetCards();

                            if (cards.Count == 0)
                            {
                                Console.WriteLine("\nYou don't have any cards.");
                                break;
                            }

                            PrintCards(cards);

                            Console.Write("\nEnter card number: ");

                            if (!int.TryParse(Console.ReadLine(), out int index))
                                throw new ArgumentException("Invalid input!");

                            var card = _session.GetCard(index);
                            PrintBalance(card);

                            decimal amount = ReadAmount("Amount to withdraw (GEL): ");

                            card = _session.Withdraw(index, amount);

                            Console.WriteLine("\nWithdrawal successful!");
                            PrintBalance(card);
                            break;
                        }

                    case 8:
                        {
                            LoansMenu loansMenu = new LoansMenu(_session);
                            loansMenu.Show();
                            break;
                        }
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
                Console.WriteLine($"Error: {ex.Message}");
            }
             
            if (!back)
            {
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }
    }

    private static void PrintCards(List<BankCard> cards)
    {
        Console.WriteLine("\n=== Your Cards ===");

        for (int i = 0; i < cards.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {cards[i].Type} - {cards[i].MaskedNumber}");
        }
    }

    private static void PrintBalance(BankCard card)
    {
        if (card.Type == CardType.Debit)
            Console.WriteLine($"Balance: {card.Balance:F2} GEL");
        else
        {
            Console.WriteLine($"Credit limit: {card.CreditLimit:F2} GEL");
            Console.WriteLine($"Debt: {card.Debt:F2} GEL");
            Console.WriteLine($"Available: {card.AvailableCredit:F2} GEL");
        }
    }

    private static decimal ReadAmount(string prompt)
    {
        Console.Write(prompt);

        string input = (Console.ReadLine() ?? "").Trim().Replace(',', '.');

        if (!decimal.TryParse(
                input,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal amount))
            throw new ArgumentException("Invalid amount!");

        return amount;
    }
}