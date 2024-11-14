namespace Domain.Models.Deluge.Exceptions;

public class DelugeClientException : Exception
{
    public DelugeClientException(string message) : base(message)
    {
    }
}