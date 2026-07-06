namespace TcpWireProtocol.Samples.Rpc.Contracts;

/// <summary>Service identifiers carried in the request header.</summary>
public static class Services
{
    /// <summary>Calculator service.</summary>
    public const short CALC = 1;

    /// <summary>Text-transform service.</summary>
    public const short TEXT = 2;
}

/// <summary>Commands of the <see cref="Services.CALC"/> service (payload = two little-endian int32, reply = one int32).</summary>
public static class CalcCommands
{
    /// <summary>Add the two operands.</summary>
    public const short ADD = 1;

    /// <summary>Subtract the second operand from the first.</summary>
    public const short SUBTRACT = 2;

    /// <summary>Multiply the two operands.</summary>
    public const short MULTIPLY = 3;
}

/// <summary>Commands of the <see cref="Services.TEXT"/> service (payload = UTF-8 text, reply = UTF-8 text).</summary>
public static class TextCommands
{
    /// <summary>Uppercase the text.</summary>
    public const short UPPER = 1;

    /// <summary>Reverse the text.</summary>
    public const short REVERSE = 2;
}