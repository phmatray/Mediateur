using System.Collections.Generic;

namespace Mediateur;

/// <summary>
/// Marker interface to represent a streaming request.
/// </summary>
/// <typeparam name="TResponse">Streaming response type</typeparam>
public interface IStreamRequest<out TResponse>
{
}
