using Domain.Enums;
using Infrastructure.Verticals.Notifications.Models;
using Mapster;
using Microsoft.Extensions.Options;

namespace Infrastructure.Verticals.Notifications.Notifiarr;

public class NotifiarrProvider : NotificationProvider
{
    private readonly NotifiarrConfig _config;
    private readonly INotifiarrProxy _proxy;

    private const string WarningColor = "f0ad4e";
    private const string ImportantColor = "bb2124";

    public NotifiarrProvider(IOptions<NotifiarrConfig> config, INotifiarrProxy proxy)
        : base(config)
    {
        _config = config.Value;
        _proxy = proxy;
    }

    public override string Name => "Notifiarr";

    public override async Task OnFailedImportStrike(FailedImportStrikeNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, WarningColor), _config);
    }
    
    public override async Task OnStalledStrike(StalledStrikeNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, WarningColor), _config);
    }
    
    public override async Task OnQueueItemDelete(QueueItemDeleteNotification notification)
    {
        await _proxy.SendNotification(BuildPayload(notification, ImportantColor), _config);
    }

    private NotifiarrPayload BuildPayload(Notification notification, string color)
    {
        NotifiarrPayload payload = new()
        {
            Discord = new()
            {
                Color = color,
                Text = new()
                {
                    Title = notification.Title,
                    Icon = "https://github.com/flmorg/cleanuperr/blob/main/Logo/48.png?raw=true",
                    Description = notification.Description,
                    Fields = new()
                    {
                        new() { Title = "Instance type", Text = notification.InstanceType.ToString() },
                        new() { Title = "Url", Text = notification.InstanceUrl.ToString() },
                        new() { Title = "Download hash", Text = notification.Hash }
                    }
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
        
        payload.Discord.Text.Fields.AddRange(notification.Fields?.Adapt<List<Field>>() ?? []);

        return payload;
    }
}