using System;

namespace Mediateur;

/// <summary>
/// Base attribute for all pipeline behaviors.
/// Pipeline attributes are applied to handler classes to add cross-cutting concerns.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public abstract class PipelineAttribute : Attribute
{
    /// <summary>
    /// Order of execution. Lower values execute first (outer wrapper).
    /// Default is 0. If multiple attributes have the same order, they execute in declaration order.
    /// </summary>
    public int Order { get; set; } = 0;
}
