namespace Hl7ToCcda.Core.Exceptions;

public sealed class UnsupportedMessageTypeException : Hl7ToCcdaException
{
    public UnsupportedMessageTypeException(string message)
        : base("UnsupportedMessageType", message)
    {
    }
}
