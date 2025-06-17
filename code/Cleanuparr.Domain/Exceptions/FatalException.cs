namespace Cleanuparr.Domain.Exceptions;

public class FatalException : Exception
{
    public FatalException()
    {
    }

    public FatalException(string message) : base(message)
    {
    }
}