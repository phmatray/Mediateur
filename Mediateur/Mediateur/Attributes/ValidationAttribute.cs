using System;

namespace Mediateur;

/// <summary>
/// Indicates that validation should be performed before the request is handled.
/// Validation logic should be implemented in the pipeline behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ValidationAttribute : PipelineAttribute
{
    /// <summary>
    /// Gets or sets a value indicating whether to stop execution on validation failure.
    /// </summary>
    public bool StopOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the error message format.
    /// </summary>
    public string? ErrorMessageFormat { get; set; }
}

/// <summary>
/// Marks a property as required for validation.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RequiredValidationAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Marks a string property with minimum and maximum length constraints.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class StringLengthAttribute : Attribute
{
    /// <summary>
    /// Gets the minimum length.
    /// </summary>
    public int MinLength { get; }

    /// <summary>
    /// Gets the maximum length.
    /// </summary>
    public int MaxLength { get; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringLengthAttribute"/> class.
    /// </summary>
    /// <param name="minLength">Minimum length</param>
    /// <param name="maxLength">Maximum length</param>
    public StringLengthAttribute(int minLength, int maxLength)
    {
        MinLength = minLength;
        MaxLength = maxLength;
    }
}

/// <summary>
/// Marks a numeric property with minimum and maximum value constraints.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class RangeAttribute : Attribute
{
    /// <summary>
    /// Gets the minimum value.
    /// </summary>
    public object Minimum { get; }

    /// <summary>
    /// Gets the maximum value.
    /// </summary>
    public object Maximum { get; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class.
    /// </summary>
    /// <param name="minimum">Minimum value</param>
    /// <param name="maximum">Maximum value</param>
    public RangeAttribute(int minimum, int maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeAttribute"/> class.
    /// </summary>
    /// <param name="minimum">Minimum value</param>
    /// <param name="maximum">Maximum value</param>
    public RangeAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }
}
