namespace Domain.Enums;

public enum DeleteReason
{
    None,
    Stalled,
    ImportFailed,
    DownloadingMetadata,
    AllFilesSkipped,
    AllFilesSkippedByQBit,
    AllFilesBlocked,
}