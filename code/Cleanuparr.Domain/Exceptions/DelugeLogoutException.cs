namespace Data.Models.Deluge.Exceptions;

public sealed class DelugeLogoutException : DelugeClientException
{
    public DelugeLogoutException() : base("logout failed")
    {
    }
}