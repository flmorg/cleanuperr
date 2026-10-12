using Cleanuparr.Domain.Enums;
using Shouldly;
using Xunit;

namespace Cleanuparr.Persistence.Tests.Models.Configuration;

public sealed class DownloadClientTypeNameExtensionsTests
{
    [Fact]
    public void SupportsAuthField_QBittorrent_SupportsApiKey()
    {
        DownloadClientTypeName.qBittorrent.SupportsAuthField(DownloadClientAuthField.ApiKey).ShouldBeTrue();
    }

    [Fact]
    public void SupportsAuthField_QBittorrent_StillSupportsUsernameAndPassword()
    {
        DownloadClientTypeName.qBittorrent.SupportsAuthField(DownloadClientAuthField.Username).ShouldBeTrue();
        DownloadClientTypeName.qBittorrent.SupportsAuthField(DownloadClientAuthField.Password).ShouldBeTrue();
    }

    [Fact]
    public void RequiresAuthField_QBittorrentApiKey_IsFalse()
    {
        DownloadClientTypeName.qBittorrent.RequiresAuthField(DownloadClientAuthField.ApiKey).ShouldBeFalse();
    }

    [Fact]
    public void SupportsApiKeyAndCredentials_QBittorrent_IsTrue()
    {
        DownloadClientTypeName.qBittorrent.SupportsApiKeyAndCredentials().ShouldBeTrue();
    }

    [Fact]
    public void SupportsApiKeyAndCredentials_Sabnzbd_IsFalse()
    {
        DownloadClientTypeName.Sabnzbd.SupportsApiKeyAndCredentials().ShouldBeFalse();
    }

    [Fact]
    public void SupportsApiKeyAndCredentials_Transmission_IsFalse()
    {
        DownloadClientTypeName.Transmission.SupportsApiKeyAndCredentials().ShouldBeFalse();
    }
}
