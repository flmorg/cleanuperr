namespace Data.Models.Deluge.Exceptions;

public sealed class DelugeLoginException : DelugeClientException
{
    public DelugeLoginException() : base("login failed")
    {
    }
}