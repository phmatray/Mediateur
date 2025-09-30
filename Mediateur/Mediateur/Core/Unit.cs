using System;

namespace Mediateur;

/// <summary>
/// Represents a void type, since void is not a valid return type in C#.
/// Used for requests that don't return a value.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    /// <summary>
    /// Default and only value of the Unit type.
    /// </summary>
    public static readonly Unit Value = default;

    /// <summary>
    /// Compares the current object with another object of the same type.
    /// </summary>
    public int CompareTo(Unit other) => 0;

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Indicates whether this instance and a specified object are equal.
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns the string representation of the Unit value.
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(Unit first, Unit second) => true;

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(Unit first, Unit second) => false;
}
