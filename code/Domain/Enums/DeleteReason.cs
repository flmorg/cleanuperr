namespace Domain.Enums;

public enum DeleteReason
{
    None,
    Stalled,
    ImportFailed,
    DownloadingMetadata,
    SlowSpeed,
    SlowTime,
    AllFilesSkipped,
    AllFilesSkippedByQBit,
    AllFilesBlocked,
}