using System.Net.Mail;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Bankomati.Models;

enum Status
{
    User,
    Admin
}

internal class Account
{

    //<summary>
    // Private fields for storing account information
    //</summary>
    private string _name = string.Empty;
    private string _phoneNumber = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;

    private static readonly Regex NameRegex =
        new(@"^[\p{L}][\p{L}\s\-']*$", RegexOptions.Compiled);

    private static readonly Regex PhoneRegex =
        new(@"^\+?\d{9,15}$", RegexOptions.Compiled);

    public Guid Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name cannot be empty.");

            value = value.Trim();

            if (value.Length < 2 || value.Length > 50)
                throw new ArgumentException("Name must be between 2 and 50 characters.");

            if (!NameRegex.IsMatch(value))
                throw new ArgumentException("Invalid name format.");

            _name = value;
        }
    }

    [JsonPropertyName("loans")]
    public List<Loan> Loans { get; set; } = new();
    public List<BankCard> Cards { get; set; } = new();

    public string PhoneNumber
    {
        get => _phoneNumber;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Phone number cannot be empty.");

            var normalized = Regex.Replace(value, @"[\s\-()]", "");

            if (!PhoneRegex.IsMatch(normalized))
                throw new ArgumentException("Invalid phone number.");

            _phoneNumber = normalized;
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email cannot be empty.");

            value = value.Trim();

            if (value.Length > 254 || !IsValidEmail(value))
                throw new ArgumentException("Invalid email.");

            _email = value.ToLowerInvariant();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Password cannot be empty.");

            _password = value;
        }
    }

    public bool Verified { get; set; }
    public Status Status { get; set; }
    public Account(string name, string phoneNumber, string email, string password, bool verified, Status status)
        : this(Guid.NewGuid(), name, phoneNumber, email, password, verified, status)
    {
    }

    [JsonConstructor]
    public Account(Guid id, string name, string phoneNumber, string email, string password, bool verified, Status status)
    {
        Id = id;
        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;
        Password = password;
        Verified = verified;
        Status = status;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);

            return address.Address == email
                   && email.Contains('.', StringComparison.Ordinal)
                   && email.IndexOf('@') < email.LastIndexOf('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}