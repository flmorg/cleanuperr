namespace Data.Enums;

public enum DeleteReason
{
    None,
    Stalled,
    FailedImport,
    DownloadingMetadata,
    SlowSpeed,
    SlowTime,
    AllFilesSkipped,
    AllFilesSkippedByQBit,
    AllFilesBlocked,
}