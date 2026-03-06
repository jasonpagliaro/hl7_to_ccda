namespace Hl7ToCcda.Core.Exceptions;

public sealed class InsufficientClinicalContentException : Hl7ToCcdaException
{
    public InsufficientClinicalContentException(string message)
        : base("InsufficientClinicalContent", message)
    {
    }
}
