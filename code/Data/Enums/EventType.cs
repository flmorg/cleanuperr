namespace Data.Enums;

public enum EventType
{
    FailedImportStrike,
    StalledStrike,
    DownloadingMetadataStrike,
    SlowSpeedStrike,
    SlowTimeStrike,
    QueueItemDeleted,
    DownloadCleaned,
    CategoryChanged
}