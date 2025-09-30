using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mediateur.Sample;

/// <summary>
/// Notification handler that sends an email when a user's email is updated.
/// </summary>
public sealed class EmailNotificationHandler : INotificationHandler<UserEmailUpdatedNotification>
{
    public ValueTask Handle(UserEmailUpdatedNotification notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[Email Service] Sending confirmation email to {notification.NewEmail}");
        return ValueTask.CompletedTask;
    }
}
