﻿using System.Security;

namespace Common.Configuration;

public sealed record DelugeConfig : IConfig
{
    public const string SectionName = "Deluge";
    
    public required bool Enabled { get; init; }
    
    public Uri? Url { get; init; }
    
    public string? Password { get; init; }
    
    public void Validate()
    {
        if (!Enabled)
        {
            return;
        }

        if (Url is null)
        {
            throw new ArgumentNullException(nameof(Url));
        }

        if (string.IsNullOrEmpty(Password))
        {
            throw new ArgumentNullException(nameof(Password));
        }
    }
}