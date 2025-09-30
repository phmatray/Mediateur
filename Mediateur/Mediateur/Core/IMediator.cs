using System.Threading;
using System.Threading.Tasks;

namespace Mediateur;

/// <summary>
/// Mediator interface for sending requests and publishing notifications.
/// The implementation is generated at compile-time by the source generator.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Send a request and get a response.
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a request with no response (void).
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    ValueTask Send(IRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a notification to all handlers.
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="notification">The notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
