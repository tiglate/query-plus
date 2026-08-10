namespace QueryPlus.Application.Abstractions;

/// <summary>
/// Outbound failure/missed-trigger notifications for the Jobs module. Implemented in
/// Infrastructure (SMTP via MailKit) - exactly the "Future: email, file storage, external APIs"
/// the AddInfrastructure doc-comment anticipates.
/// </summary>
public interface INotificationSender
{
    Task SendAsync(
        IReadOnlyCollection<string> toAddresses,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
