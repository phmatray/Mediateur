namespace Mediateur;

/// <summary>
/// Marker interface to represent a request with a response.
/// </summary>
/// <typeparam name="TResponse">Response type</typeparam>
public interface IRequest<out TResponse>
{
}

/// <summary>
/// Marker interface to represent a request with no response (void).
/// </summary>
public interface IRequest : IRequest<Unit>
{
}
