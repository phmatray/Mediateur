using Microsoft.CodeAnalysis;

namespace Mediateur.Models;

/// <summary>
/// Represents a pipeline attribute applied to a handler.
/// </summary>
internal sealed class PipelineInfo
{
    public INamedTypeSymbol AttributeType { get; }
    public int Order { get; }
    public AttributeData AttributeData { get; }

    public PipelineInfo(INamedTypeSymbol attributeType, int order, AttributeData attributeData)
    {
        AttributeType = attributeType;
        Order = order;
        AttributeData = attributeData;
    }

    public string AttributeTypeName => AttributeType.Name;

    public bool IsLogAttribute => AttributeTypeName == "LogAttribute";
    public bool IsValidateAttribute => AttributeTypeName == "ValidateAttribute";
}
