using System;
using System.Runtime.CompilerServices;

namespace TcpWireProtocol;

/// <summary>
/// Guard-clause helpers with a single call site across all target frameworks.
/// On modern runtimes they forward to the BCL <c>ArgumentNullException.ThrowIfNull</c> /
/// <c>ArgumentOutOfRangeException.ThrowIf*</c> methods (.NET 6/8+); on <c>netstandard2.1</c>,
/// where those static helpers do not exist, they implement the same checks inline.
/// The parameter name is captured via <see cref="CallerArgumentExpressionAttribute"/> so
/// the thrown exception carries the original argument expression on every target.
/// </summary>
internal static class ThrowHelper
{
    /// <summary>Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is <c>null</c>.</summary>
    public static void ThrowIfNull(object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NETSTANDARD2_1
        if (argument is null) { throw new ArgumentNullException(paramName); }
#else
        ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
    public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NETSTANDARD2_1
        if (value < 0) { throw new ArgumentOutOfRangeException(paramName, value, "Value must be non-negative."); }
#else
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#endif
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative or zero.</summary>
    public static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if NETSTANDARD2_1
        if (value <= 0) { throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive."); }
#else
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
#endif
    }
}