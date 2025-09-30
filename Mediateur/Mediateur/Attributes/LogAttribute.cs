using System;

namespace Mediateur;

/// <summary>
/// Pipeline attribute that adds logging around handler execution.
/// The generator will emit code to log request execution time and completion status.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class LogAttribute : PipelineAttribute
{
    /// <summary>
    /// Whether to log request parameters. Default is false for security.
    /// </summary>
    public bool LogParameters { get; set; } = false;

    /// <summary>
    /// Whether to log response. Default is false for security.
    /// </summary>
    public bool LogResponse { get; set; } = false;
}
