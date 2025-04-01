namespace Domain.Enums;

public enum DeleteReason
{
    None,
    Stalled,
    ImportFailed,
    DownloadingMetadata,
    SlowSpeed,
    ExceededEstimatedTime,
    AllFilesSkipped,
    AllFilesSkippedByQBit,
    AllFilesBlocked,
}