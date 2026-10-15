namespace Cleanuparr.Domain.Exceptions;

public sealed class CustomValidationException : Exception
{
    public CustomValidationException()
    {
    }

    public CustomValidationException(string message) : base(message)
    {
    }

    public CustomValidationException(string message, Exception inner) : base(message, inner)
    {
    }
}