namespace Hl7ToCcda.Core.Exceptions;

public sealed class InvalidHl7MessageException : Hl7ToCcdaException
{
    public InvalidHl7MessageException(string message, Exception? innerException = null)
        : base("InvalidHl7Message", message, innerException)
    {
    }
}
