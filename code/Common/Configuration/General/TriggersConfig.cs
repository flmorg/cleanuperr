﻿namespace Common.Configuration.General;

public sealed class TriggersConfig
{
    public const string SectionName = "Triggers";
    
    public required string QueueCleaner { get; init; }
    
    public required string ContentBlocker { get; init; }
    
    public required string DownloadCleaner { get; init; }
}