namespace Domain.Deluge.Exceptions;

public class DelugeClientException : Exception
{
    public DelugeClientException(string message) : base(message)
    {
    }
}