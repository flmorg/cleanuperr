using Newtonsoft.Json;

namespace Cleanuparr.Domain.Entities.Deluge.Response;

public sealed record DelugeTorrentExtended : DelugeTorrent
{
    [JsonProperty(PropertyName = "total_done")]
    public long TotalDone { get; set; }

    [JsonProperty(PropertyName = "total_payload_download")]
    public long TotalPayloadDownload { get; set; }

    [JsonProperty(PropertyName = "total_uploaded")]
    public long TotalUploaded { get; set; }

    [JsonProperty(PropertyName = "next_announce")]
    public int NextAnnounce { get; set; }

    [JsonProperty(PropertyName = "tracker_status")]
    public string TrackerStatus { get; set; }

    [JsonProperty(PropertyName = "num_pieces")]
    public int NumPieces { get; set; }

    [JsonProperty(PropertyName = "piece_length")]
    public long PieceLength { get; set; }

    [JsonProperty(PropertyName = "is_auto_managed")]
    public bool IsAutoManaged { get; set; }

    [JsonProperty(PropertyName = "active_time")]
    public long ActiveTime { get; set; }

    [JsonProperty(PropertyName = "seeding_time")]
    public long SeedingTime { get; set; }

    [JsonProperty(PropertyName = "time_since_transfer")]
    public long TimeSinceTransfer { get; set; }

    [JsonProperty(PropertyName = "seed_rank")]
    public int SeedRank { get; set; }

    [JsonProperty(PropertyName = "last_seen_complete")]
    public long LastSeenComplete { get; set; }

    [JsonProperty(PropertyName = "completed_time")]
    public long CompletedTime { get; set; }

    [JsonProperty(PropertyName = "owner")] public string Owner { get; set; }

    [JsonProperty(PropertyName = "public")]
    public bool Public { get; set; }

    [JsonProperty(PropertyName = "shared")]
    public bool Shared { get; set; }

    [JsonProperty(PropertyName = "queue")] public int Queue { get; set; }

    [JsonProperty(PropertyName = "total_wanted")]
    public long TotalWanted { get; set; }

    [JsonProperty(PropertyName = "state")] public string State { get; set; }

    [JsonProperty(PropertyName = "progress")]
    public float Progress { get; set; }

    [JsonProperty(PropertyName = "num_seeds")]
    public int NumSeeds { get; set; }

    [JsonProperty(PropertyName = "total_seeds")]
    public int TotalSeeds { get; set; }

    [JsonProperty(PropertyName = "num_peers")]
    public int NumPeers { get; set; }

    [JsonProperty(PropertyName = "total_peers")]
    public int TotalPeers { get; set; }

    [JsonProperty(PropertyName = "download_payload_rate")]
    public long DownloadPayloadRate { get; set; }

    [JsonProperty(PropertyName = "upload_payload_rate")]
    public long UploadPayloadRate { get; set; }

    [JsonProperty(PropertyName = "eta")] public long Eta { get; set; }

    [JsonProperty(PropertyName = "distributed_copies")]
    public float DistributedCopies { get; set; }

    [JsonProperty(PropertyName = "time_added")]
    public int TimeAdded { get; set; }

    [JsonProperty(PropertyName = "tracker_host")]
    public string TrackerHost { get; set; }

    [JsonProperty(PropertyName = "download_location")]
    public string DownloadLocation { get; set; }

    [JsonProperty(PropertyName = "total_remaining")]
    public long TotalRemaining { get; set; }

    [JsonProperty(PropertyName = "max_download_speed")]
    public long MaxDownloadSpeed { get; set; }

    [JsonProperty(PropertyName = "max_upload_speed")]
    public long MaxUploadSpeed { get; set; }

    [JsonProperty(PropertyName = "seeds_peers_ratio")]
    public float SeedsPeersRatio { get; set; }
}