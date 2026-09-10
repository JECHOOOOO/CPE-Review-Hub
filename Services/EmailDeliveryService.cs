using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CPE_Review_Hub.Services;

public sealed class EmailOptions
{
    public string Mode { get; set; } = "Smtp";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderAddress { get; set; } = "";
    public string SenderName { get; set; } = "COLMS: CentOs Learning Management System";
    public string Username { get; set; } = "";
    public string AppPassword { get; set; } = "";
    public string PickupDirectory { get; set; } = "";
}

public interface IEmailDeliveryService
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public sealed class EmailDeliveryService(IOptions<EmailOptions> options, IWebHostEnvironment environment,
    ILogger<EmailDeliveryService> logger) : IEmailDeliveryService
{
    private readonly EmailOptions settings = options.Value;

    public bool IsConfigured => settings.Mode.Equals("Pickup", StringComparison.OrdinalIgnoreCase)
        ? environment.IsDevelopment() && !string.IsNullOrWhiteSpace(settings.PickupDirectory)
        : !string.IsNullOrWhiteSpace(settings.Host) && settings.Port is > 0 and <= 65535 &&
          MailAddress.TryCreate(settings.SenderAddress, out _) && !string.IsNullOrWhiteSpace(settings.Username) &&
          !string.IsNullOrWhiteSpace(settings.AppPassword);

    public async Task SendAsync(string recipient, string subject, string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) throw new InvalidOperationException("Email delivery is not configured.");
        if (!MailAddress.TryCreate(recipient, out var recipientAddress))
            throw new InvalidOperationException("The recipient email address is invalid.");

        if (settings.Mode.Equals("Pickup", StringComparison.OrdinalIgnoreCase))
        {
            var directory = Path.GetFullPath(settings.PickupDirectory);
            Directory.CreateDirectory(directory);
            var content = $"To: {recipientAddress.Address}\r\nSubject: {subject}\r\nContent-Type: text/html; charset=utf-8\r\n\r\n{htmlBody}";
            await File.WriteAllTextAsync(Path.Combine(directory, $"{Guid.NewGuid():N}.eml"), content,
                cancellationToken);
            logger.LogInformation("Account email written to the development pickup directory.");
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.SenderAddress, settings.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(recipientAddress);
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.Username, settings.AppPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 15000
        };
        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Account email accepted by the configured SMTP server.");
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException)
        {
            // A restricted Windows Event Log must never prevent registration cleanup.
            try { logger.LogWarning("Email delivery failed ({ErrorType}).", exception.GetType().Name); }
            catch (Exception) { /* SMTP failure remains the error returned to the caller. */ }
            throw new InvalidOperationException("Email delivery failed.", exception);
        }
    }
}
