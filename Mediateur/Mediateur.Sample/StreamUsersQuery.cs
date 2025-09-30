namespace Mediateur.Sample;

/// <summary>
/// Query that streams users from a data source.
/// Demonstrates modern async streaming API.
/// </summary>
public sealed record StreamUsersQuery(int PageSize = 10) : IStreamRequest<UserDto>;
