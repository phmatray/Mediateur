using System;

namespace Mediateur.Sample;

/// <summary>
/// Query to get a user by ID.
/// </summary>
public sealed record GetUserQuery(Guid UserId) : IRequest<UserDto>;
