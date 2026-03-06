using System;

namespace Hl7ToCcda.Core.Exceptions;

public abstract class Hl7ToCcdaException : Exception
{
    protected Hl7ToCcdaException(string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
