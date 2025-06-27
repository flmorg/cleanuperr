namespace Cleanuparr.Infrastructure.Features.Notifications.Notifiarr;

public class NotifiarrException : Exception
{
    public NotifiarrException(string message) : base(message)
    {
    }

    public NotifiarrException(string message, Exception innerException) : base(message, innerException)
    {
    }
}