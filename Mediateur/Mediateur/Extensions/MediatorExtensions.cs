using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mediateur;

/// <summary>
/// Extension methods for <see cref="IMediator"/> to provide a cleaner, more fluent API.
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// Sends a request and returns a result, wrapping exceptions in a failure result.
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="mediator">The mediator</param>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the response or error</returns>
    public static async ValueTask<Result<TResponse>> SendSafe<TResponse>(
        this IMediator mediator,
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await mediator.Send(request, cancellationToken);
            return Result<TResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<TResponse>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Sends a void request and returns a result, wrapping exceptions in a failure result.
    /// </summary>
    /// <param name="mediator">The mediator</param>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result indicating success or failure</returns>
    public static async ValueTask<Result> SendSafe(
        this IMediator mediator,
        IRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await mediator.Send(request, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Publishes a notification and suppresses exceptions.
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="mediator">The mediator</param>
    /// <param name="notification">The notification</param>
    /// <param name="onError">Optional error handler</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async ValueTask PublishSafe<TNotification>(
        this IMediator mediator,
        TNotification notification,
        Action<Exception>? onError = null,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        try
        {
            await mediator.Publish(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }

    /// <summary>
    /// Sends multiple requests in parallel and returns all results.
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="mediator">The mediator</param>
    /// <param name="requests">The requests to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Array of results</returns>
    public static async ValueTask<TResponse[]> SendMany<TResponse>(
        this IMediator mediator,
        IEnumerable<IRequest<TResponse>> requests,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<ValueTask<TResponse>>();

        foreach (var request in requests)
        {
            tasks.Add(mediator.Send(request, cancellationToken));
        }

        var results = new TResponse[tasks.Count];
        for (int i = 0; i < tasks.Count; i++)
        {
            results[i] = await tasks[i];
        }

        return results;
    }

    /// <summary>
    /// Sends a request with automatic retry on failure.
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="mediator">The mediator</param>
    /// <param name="request">The request</param>
    /// <param name="maxRetries">Maximum number of retries</param>
    /// <param name="delayMs">Delay between retries in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response</returns>
    public static async ValueTask<TResponse> SendWithRetry<TResponse>(
        this IMediator mediator,
        IRequest<TResponse> request,
        int maxRetries = 3,
        int delayMs = 100,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await mediator.Send(request, cancellationToken);
            }
            catch when (attempt < maxRetries)
            {
                await Task.Delay(delayMs * (attempt + 1), cancellationToken);
            }
        }

        // This should never be reached due to the throw in the catch
        throw new InvalidOperationException("Retry logic failed");
    }

    /// <summary>
    /// Sends a request with a timeout.
    /// </summary>
    /// <typeparam name="TResponse">Response type</typeparam>
    /// <param name="mediator">The mediator</param>
    /// <param name="request">The request</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response</returns>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    public static async ValueTask<TResponse> SendWithTimeout<TResponse>(
        this IMediator mediator,
        IRequest<TResponse> request,
        int timeoutMs,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        try
        {
            return await mediator.Send(request, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Request timed out after {timeoutMs}ms");
        }
    }

    /// <summary>
    /// Converts an async enumerable to a list.
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    /// <param name="source">The async enumerable</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of items</returns>
    public static async ValueTask<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        return list;
    }
}
