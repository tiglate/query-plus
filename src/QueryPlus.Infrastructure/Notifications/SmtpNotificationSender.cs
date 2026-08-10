using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using QueryPlus.Application.Abstractions;

namespace QueryPlus.Infrastructure.Notifications;

public sealed class SmtpNotificationSender(IOptions<SmtpOptions> options) : INotificationSender
{
    public async Task SendAsync(
        IReadOnlyCollection<string> toAddresses,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (toAddresses.Count == 0)
        {
            return;
        }

        var smtp = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.FromAddress));
        foreach (var address in toAddresses)
        {
            message.To.Add(MailboxAddress.Parse(address));
        }

        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        var secureSocketOptions = smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(smtp.Host, smtp.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
