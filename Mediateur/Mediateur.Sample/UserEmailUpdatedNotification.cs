using System;

namespace Mediateur.Sample;

/// <summary>
/// Notification that a user's email was updated.
/// </summary>
public sealed record UserEmailUpdatedNotification(Guid UserId, string OldEmail, string NewEmail) : INotification;
