using System;

namespace Mediateur;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// </summary>
/// <typeparam name="TValue">The type of value on success</typeparam>
public readonly struct Result<TValue>
{
    private readonly TValue? _value;
    private readonly string? _error;

    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value if successful, otherwise default.
    /// </summary>
    public TValue? Value => IsSuccess ? _value : default;

    /// <summary>
    /// Gets the error message if failed, otherwise null.
    /// </summary>
    public string? Error => IsFailure ? _error : null;

    private Result(TValue value, string? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>A successful result</returns>
    public static Result<TValue> Success(TValue value) =>
        new(value, null, true);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error message</param>
    /// <returns>A failed result</returns>
    public static Result<TValue> Failure(string error) =>
        new(default!, error ?? "Unknown error", false);

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    /// <typeparam name="TResult">Result type</typeparam>
    /// <param name="onSuccess">Function to execute on success</param>
    /// <param name="onFailure">Function to execute on failure</param>
    /// <returns>The result of the matched function</returns>
    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return IsSuccess
            ? onSuccess(_value!)
            : onFailure(_error!);
    }

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);

    /// <summary>
    /// Deconstructs the result into success flag and value/error.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out TValue? value, out string? error)
    {
        isSuccess = IsSuccess;
        value = Value;
        error = Error;
    }
}

/// <summary>
/// Represents the result of an operation without a return value.
/// </summary>
public readonly struct Result
{
    private readonly string? _error;

    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if failed, otherwise null.
    /// </summary>
    public string? Error => IsFailure ? _error : null;

    private Result(string? error, bool isSuccess)
    {
        _error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success() => new(null, true);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The error message</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string error) => new(error ?? "Unknown error", false);

    /// <summary>
    /// Matches the result to one of two actions based on success or failure.
    /// </summary>
    /// <param name="onSuccess">Action to execute on success</param>
    /// <param name="onFailure">Action to execute on failure</param>
    public void Match(Action onSuccess, Action<string> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(_error!);
    }

    /// <summary>
    /// Deconstructs the result into success flag and error.
    /// </summary>
    public void Deconstruct(out bool isSuccess, out string? error)
    {
        isSuccess = IsSuccess;
        error = Error;
    }
}
