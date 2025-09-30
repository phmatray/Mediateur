using System.Threading;
using System.Threading.Tasks;

namespace Mediateur;

/// <summary>
/// Handler for a notification.
/// Multiple handlers can handle the same notification.
/// </summary>
/// <typeparam name="TNotification">Notification type</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    ValueTask Handle(TNotification notification, CancellationToken cancellationToken);
}
