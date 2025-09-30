using System.Threading;
using System.Threading.Tasks;

namespace Mediateur;

/// <summary>
/// Handler for a request with a response.
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response</returns>
    ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Handler for a request with no response (void).
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest
{
}
