using System;

namespace Mediateur;

/// <summary>
/// Pipeline attribute that adds validation before handler execution.
/// The generator will emit code to validate the request using DataAnnotations.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ValidateAttribute : PipelineAttribute
{
}
