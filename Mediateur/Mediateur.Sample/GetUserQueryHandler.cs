using System.Threading;
using System.Threading.Tasks;

namespace Mediateur.Sample;

/// <summary>
/// Handler for GetUserQuery.
/// </summary>
public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public ValueTask<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Simulate fetching from a database
        var user = new UserDto(
            request.UserId,
            "John Doe",
            "john.doe@example.com");

        return ValueTask.FromResult(user);
    }
}
