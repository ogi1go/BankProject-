
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace Bankomati.Services;

internal class EmailVerificationService
{
    private readonly string _senderEmail = "iliajijelava1337@gmail.com";
    private readonly string _appPassword = "wujv mvuj omzx mdtz";

    private readonly Dictionary<string, VerificationData> _codes = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task SendCodeAsync(string recipientEmail, CancellationToken cancellationToken = default)
    {
        string email = recipientEmail.Trim().ToLowerInvariant();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_codes.TryGetValue(email, out VerificationData? existing) &&
                DateTime.UtcNow - existing.LastSent < TimeSpan.FromSeconds(60))
            {
                throw new InvalidOperationException("Please wait before requesting another code.");
            }

            string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            using MailMessage message = new MailMessage();
            message.From = new MailAddress(_senderEmail);
            message.To.Add(email);
            message.Subject = "Bankomati - Email Verification";
            message.Body = $"Your verification code is: {code}\n\nThis code expires in 10 minutes.";

            using SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(_senderEmail, _appPassword);

            await smtp.SendMailAsync(message, cancellationToken);

            _codes[email] = new VerificationData
            {
                Code = code,
                Expiry = DateTime.UtcNow.AddMinutes(10),
                LastSent = DateTime.UtcNow,
                Attempts = 0
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool VerifyCode(string recipientEmail, string enteredCode)
    {
        string email = recipientEmail.Trim().ToLowerInvariant();

        if (!_codes.TryGetValue(email, out VerificationData? data))
            return false;

        if (DateTime.UtcNow > data.Expiry || data.Attempts >= 5)
        {
            _codes.Remove(email);
            return false;
        }

        data.Attempts++;

        if (data.Code != enteredCode.Trim())
            return false;

        _codes.Remove(email);
        return true;
    }

    private sealed class VerificationData
    {
        public string Code { get; set; } = "";
        public DateTime Expiry { get; set; }
        public DateTime LastSent { get; set; }
        public int Attempts { get; set; }
    }
}
