
using Bankomati.Models;
using Bankomati.Services;
using Bankomati.Menus;

//<summary>
// This is the main entry point of the Bankomati application.
// Main menu
//</summary>
SessionServices session = new SessionServices();
EmailVerificationService emailVerification = new EmailVerificationService();

int choice = -1;

while (choice != 0)
{
    Console.Clear();

    if (!session.IsLoggedIn)
    {
        Console.WriteLine("=== Mega Good Bankomat ===");
        Console.WriteLine("1. Register");
        Console.WriteLine("2. Login");
        Console.WriteLine("0. Leave");

        Console.Write("\nChoose: ");

        if (!int.TryParse(Console.ReadLine(), out choice))
        {
            Console.WriteLine("Invalid choice!");
            choice = -1;
            
        }

        try
        {
            switch (choice)
            {
                case 1:
                    Register(session);
                    break;

                case 2:
                    Login(session);
                    break;

                case 0:
                    Console.WriteLine("Goodbye!");
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
    }
    else
    {
        var account = session.CurrentAccount!;

        Console.WriteLine($"=== Welcome, {account.Name}! ===");

        if (!account.Verified)
            Console.WriteLine("IMPORTANT: Your email is not verified!");

        Console.WriteLine("1. Account information");
        Console.WriteLine("2. Change password");
        Console.WriteLine("3. My Cards");
    

        if (!account.Verified)
            Console.WriteLine("4. Verify Email (IMPORTANT)");

        Console.WriteLine("6. Logout");
        Console.WriteLine("0. Leave");

        Console.Write("\nChoose: ");

        if (!int.TryParse(Console.ReadLine(), out choice))
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
                    Console.WriteLine("\n=== Account Information ===");
                    Console.WriteLine($"Name: {account.Name}");
                    Console.WriteLine($"Email: {account.Email}");
                    Console.WriteLine($"Phone: {account.PhoneNumber}");
                    Console.WriteLine($"You are: {account.Status}");
                    Console.WriteLine($"ID: {account.Id}");
                    Console.WriteLine($"Verified: {(account.Verified ? "Yes" : "No")}");
                    break;

                case 2:
                    ChangePassword(session);
                    break;

                case 3:
                    new CardsMenu(session).Show();
                    break;

                case 4:
                    if (!account.Verified)
                        VerifyEmail(session, emailVerification);
                    else
                        Console.WriteLine("Your email is already verified!");
                    break;

                case 6:
                    session.Logout();
                    Console.WriteLine("Successfully logged out!");
                    break;

                case 52:
                    {
                        if (!session.IsLoggedIn)
                            throw new InvalidOperationException("You need to log in first.");

                        if (session.CurrentAccount!.Status != Status.Admin)
                            throw new UnauthorizedAccessException("Access denied: this menu is for Admins only.");

                        var adminMenu = new AdminMenu(session);
                        adminMenu.Show();
                        break;
                    }

                case 0:
                    Console.WriteLine("Goodbye!");
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
    }

    if (choice != 0)
    {
        Console.WriteLine("\nPress any button to return to menu...");
        Console.ReadKey();
    }
}

static void Register(SessionServices session)
{
    Console.WriteLine("\n=== Registration ===");

    Console.Write("Name: ");
    string name = Console.ReadLine() ?? "";

    Console.Write("Phone: ");
    string phone = Console.ReadLine() ?? "";

    Console.Write("Email: ");
    string email = Console.ReadLine() ?? "";

    Console.Write("Password: ");
    string password = Console.ReadLine() ?? "";

    var account = session.Register(name, phone, email, password);

    Console.WriteLine($"\nSuccessfully registered, {account.Name}!");
    Console.WriteLine("Login and verify your email to complete registration.");
}

static void Login(SessionServices session)
{
    Console.WriteLine("\n=== Login ===");

    Console.Write("Email: ");
    string email = Console.ReadLine() ?? "";

    Console.Write("Password: ");
    string password = Console.ReadLine() ?? "";

    var account = session.Login(email, password);

    Console.WriteLine($"\nWelcome back, {account.Name}!");
}

static void ChangePassword(SessionServices session)
{
    Console.WriteLine("\n=== Change Password ===");

    Console.Write("Old password: ");
    string oldPassword = Console.ReadLine() ?? "";

    Console.Write("New password: ");
    string newPassword = Console.ReadLine() ?? "";

    session.ChangePassword(oldPassword, newPassword);

    Console.WriteLine("Password changed successfully!");
}

static void VerifyEmail(
    SessionServices session,
    EmailVerificationService emailVerification)
{
    var account = session.CurrentAccount!;

    Console.WriteLine("\n=== Email Verification ===");
    Console.WriteLine("Please wait...");
    emailVerification.SendCodeAsync(account.Email).Wait()   ;

    Console.WriteLine($"Verification code sent to {account.Email}.");

    Console.Write("Enter the 6-digit code: ");
    string code = Console.ReadLine() ?? "";

    if (emailVerification.VerifyCode(account.Email, code))
    {
        session.MarkEmailVerified();
        Console.WriteLine("Email verified successfully!");
    }
    else
    {
        Console.WriteLine("Invalid or expired verification code!");
    }
}
