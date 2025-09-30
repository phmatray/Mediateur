using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Mediateur.Models;

/// <summary>
/// Represents a discovered request handler with its metadata.
/// </summary>
internal sealed class RequestHandlerInfo
{
    public INamedTypeSymbol HandlerType { get; }
    public INamedTypeSymbol RequestType { get; }
    public ITypeSymbol ResponseType { get; }
    public bool IsVoidRequest { get; }
    public List<PipelineInfo> Pipelines { get; }

    public RequestHandlerInfo(
        INamedTypeSymbol handlerType,
        INamedTypeSymbol requestType,
        ITypeSymbol responseType,
        bool isVoidRequest)
    {
        HandlerType = handlerType;
        RequestType = requestType;
        ResponseType = responseType;
        IsVoidRequest = isVoidRequest;
        Pipelines = new List<PipelineInfo>();
    }

    public string HandlerTypeName => HandlerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    public string RequestTypeName => RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    public string ResponseTypeName => ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>
    /// Gets a safe identifier for use in generated code (removes generics, special chars).
    /// </summary>
    public string SafeRequestTypeName => RequestType.Name.Replace("<", "").Replace(">", "").Replace(",", "");
}
