using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mediateur.Sample;

/// <summary>
/// Notification handler that logs audit trail when a user's email is updated.
/// </summary>
public sealed class AuditNotificationHandler : INotificationHandler<UserEmailUpdatedNotification>
{
    public ValueTask Handle(UserEmailUpdatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Audit Service] User {notification.UserId} changed email from {notification.OldEmail} to {notification.NewEmail}");
        return ValueTask.CompletedTask;
    }
}
