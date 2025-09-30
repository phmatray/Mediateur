using System;

namespace Mediateur.Sample;

/// <summary>
/// Command to update a user's email.
/// </summary>
public sealed record UpdateEmailCommand(Guid UserId, string NewEmail) : IRequest;
