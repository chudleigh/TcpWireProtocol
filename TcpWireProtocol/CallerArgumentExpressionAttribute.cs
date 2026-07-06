#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill: <c>netstandard2.1</c> lacks this attribute type. When it is present, the C#
/// compiler fills the annotated parameter with the caller's argument expression text —
/// this lets <see cref="TcpWireProtocol.ThrowHelper"/> capture the original parameter
/// name on <c>netstandard2.1</c> just as it does on modern targets.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
    public string ParameterName { get; } = parameterName;
}
#endif
