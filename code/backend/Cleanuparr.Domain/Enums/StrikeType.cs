namespace Cleanuparr.Domain.Enums;

public enum StrikeType
{
    Stalled,
    DownloadingMetadata,
    FailedImport,
    SlowSpeed,
    SlowTime,
    DeadTorrent,
    Errored,

    /// <summary>
    /// Text this build does not know.
    /// </summary>
    Unknown = EnumSentinel.UnknownValue,
}
