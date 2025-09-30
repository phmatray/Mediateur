using System;

namespace Mediateur;

/// <summary>
/// Optional marker attribute for request handlers.
/// The generator will discover handlers by interface implementation,
/// but this attribute can be used for explicit marking.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HandlerAttribute : Attribute
{
}
