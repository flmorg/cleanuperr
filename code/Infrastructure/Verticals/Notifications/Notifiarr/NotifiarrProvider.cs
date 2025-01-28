using Infrastructure.Verticals.Notifications.Models;
using Mapster;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.Notifications.Notifiarr;

public class NotifiarrProvider : NotificationProvider
{
    private readonly NotifiarrConfig _config;
    private readonly INotifiarrProxy _proxy;

    public NotifiarrProvider(IOptions<NotifiarrConfig> config, INotifiarrProxy proxy)
        : base(config)
    {
        _config = config.Value;
        _proxy = proxy;
    }

    public override string Name => "Notifiarr";

    public override async Task OnFailedImportStrike(FailedImportStrikeNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, "f0ad4e"), _config);
    }
    
    public override async Task OnStalledStrike(StalledStrikeNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, "f0ad4e"), _config);
    }
    
    public override async Task OnQueueItemDelete(QueueItemDeleteNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, "bb2124"), _config);
    }

    private NotifiarrPayload BuildPayload(Notification notification, string color)
    {
        return new()
        {
            Discord = new()
            {
                Color = color,
                Text = new()
                {
                    Title = notification.Title,
                    Icon = "https://github.com/flmorg/cleanuperr/blob/main/Logo/48.png?raw=true",
                    Description = notification.Description,
                    Fields = notification.Fields?.Select(x => x.Adapt<Field>()).ToList() ?? []
                },
                Ids = new Ids
                {
                    Channel = _config.ChannelId
                },
                Images = new()
                {
                    Thumbnail = new Uri("https://github.com/flmorg/cleanuperr/blob/main/Logo/48.png?raw=true"),
                    Image = notification.Image
                }
            }
        };
    }
}