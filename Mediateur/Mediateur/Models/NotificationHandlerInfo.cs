using Microsoft.CodeAnalysis;

namespace Mediateur.Models;

/// <summary>
/// Represents a discovered notification handler with its metadata.
/// </summary>
internal sealed class NotificationHandlerInfo
{
    public INamedTypeSymbol HandlerType { get; }
    public INamedTypeSymbol NotificationType { get; }

    public NotificationHandlerInfo(
        INamedTypeSymbol handlerType,
        INamedTypeSymbol notificationType)
    {
        HandlerType = handlerType;
        NotificationType = notificationType;
    }

    public string HandlerTypeName => HandlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    public string NotificationTypeName => NotificationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
