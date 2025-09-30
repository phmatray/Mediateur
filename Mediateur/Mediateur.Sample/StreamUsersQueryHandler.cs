using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mediateur.Sample;

/// <summary>
/// Handler for StreamUsersQuery that demonstrates async streaming.
/// </summary>
public sealed class StreamUsersQueryHandler : IStreamRequestHandler<StreamUsersQuery, UserDto>
{
    public async IAsyncEnumerable<UserDto> Handle(
        StreamUsersQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Simulate streaming data from a database
        for (int i = 1; i <= request.PageSize; i++)
        {
            // Check cancellation
            cancellationToken.ThrowIfCancellationRequested();

            // Simulate async data fetch
            await Task.Delay(100, cancellationToken);

            yield return new UserDto(
                Guid.NewGuid(),
                $"User {i}",
                $"user{i}@example.com");
        }
    }
}
