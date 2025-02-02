_Love this project? Give it a ⭐️ and let others know!_

# <img width="24px" src="./Logo/256.png" alt="cleanuperr"></img> cleanuperr

[![Discord](https://img.shields.io/discord/1306721212587573389?color=7289DA&label=Discord&style=for-the-badge&logo=discord)](https://discord.gg/sWggpnmGNY)

cleanuperr is a tool for automating the cleanup of unwanted or blocked files in Sonarr, Radarr, and supported download clients like qBittorrent. It removes incomplete or blocked downloads, updates queues, and enforces blacklists or whitelists to manage file selection. After removing blocked content, cleanuperr can also trigger a search to replace the deleted shows/movies.

cleanuperr was created primarily to address malicious files, such as `*.lnk` or `*.zipx`, that were getting stuck in Sonarr/Radarr and required manual intervention. Some of the reddit posts that made cleanuperr come to life can be found [here](https://www.reddit.com/r/sonarr/comments/1gqnx16/psa_sonarr_downloaded_a_virus/), [here](https://www.reddit.com/r/sonarr/comments/1gqwklr/sonar_downloaded_a_mkv_file_which_looked_like_a/), [here](https://www.reddit.com/r/sonarr/comments/1gpw2wa/downloaded_waiting_to_import/) and [here](https://www.reddit.com/r/sonarr/comments/1gpi344/downloads_not_importing_no_files_found/).

The tool supports both qBittorrent's built-in exclusion features and its own blocklist-based system. Binaries for all platforms are provided, along with Docker images for easy deployment.

#

> [!NOTE]
> ### Quick Start
>
> 1. **Docker (Recommended)**  
> Pull the Docker image from `ghcr.io/flmorg/cleanuperr:latest`.
>
> 2. **Unraid (for Unraid users)**  
> Use the Unraid Community App.
>
> 3. **Manual Installation (if you're not using Docker)**  
> More details [here](#binaries-if-youre-not-using-docker).

> [!TIP]
> Refer to the [Environment variables](#Environment-variables) section for detailed configuration instructions and the [Setup](#Setup) section for an in-depth explanation of the cleanup process.

## Key features
- Marks unwanted files as skip/unwanted in the download client.
- Automatically strikes stalled or stuck downloads. 
- Removes and blocks downloads that reached the maximum number of strikes or are marked as unwanted by the download client or by cleanuperr and triggers a search for removed downloads.

> [!IMPORTANT]
> Only the **latest versions** of the following apps are supported, or earlier versions that have the same API as the latest version:
> - qBittorrent
> - Deluge
> - Transmission
> - Sonarr
> - Radarr
> - Lidarr

This tool is actively developed and still a work in progress, so using the `latest` Docker tag may result in breaking changes. Join the Discord server if you want to reach out to me quickly (or just stay updated on new releases) so we can squash those pesky bugs together:

> https://discord.gg/sWggpnmGNY

# How it works

1. **Content blocker** will:
   - Run every 5 minutes (or configured cron).
   - Process all items in the *arr queue.
   - Find the corresponding item from the download client for each queue item.
   - Mark the files that were found in the queue as **unwanted/skipped** if:
     - They **are listed in the blacklist**, or
     - They **are not included in the whitelist**.
   - If **all files** of a download **are unwanted**:
     - It will be removed from the *arr's queue and blocked.
     - It will be deleted from the download client.
     - A new search will be triggered for the *arr item.
2. **Queue cleaner** will:
   - Run every 5 minutes (or configured cron, or right after `content blocker`).
   - Process all items in the *arr queue.
   - Check each queue item if it is **stalled (download speed is 0)**, **stuck in matadata downloading** or **failed to be imported**.
     - If it is, the item receives a **strike** and will continue to accumulate strikes every time it meets any of these conditions.
   - Check each queue item if it meets one of the following condition in the download client:
     - **Marked as completed, but 0 bytes have been downloaded** (due to files being blocked by qBittorrent or the **content blocker**).
     - All associated files of are marked as **unwanted/skipped**.
   - If the item **DOES NOT** match the above criteria, it will be skipped.
   - If the item **DOES** match the criteria or has received the **maximum number of strikes**:
     - It will be removed from the *arr's queue and blocked.
     - It will be deleted from the download client.
     - A new search will be triggered for the *arr item.

# Setup

## Using qBittorrent's built-in feature (works only with qBittorrent)

1. Go to qBittorrent -> Options -> Downloads -> make sure `Excluded file names` is checked -> Paste an exclusion list that you have copied.
   - [blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist), or
   - [permissive blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist_permissive), or
   - create your own
2. qBittorrent will block files from being downloaded. In the case of malicious content, **nothing is downloaded and the torrent is marked as complete**.
3. Start **cleanuperr** with `QUEUECLEANER__ENABLED` set to `true`.
4. The **queue cleaner** will perform a cleanup process as described in the [How it works](#how-it-works) section.

## Using cleanuperr's blocklist (works with all supported download clients)

1. Set both `QUEUECLEANER__ENABLED` and `CONTENTBLOCKER__ENABLED` to `true` in your environment variables.
2. Configure and enable either a **blacklist** or a **whitelist** as described in the [Arr variables](#Arr-variables) section.
3. Once configured, cleanuperr will perform the following tasks:
   - Execute the **content blocker** job, as explained in the [How it works](#how-it-works) section.
   - Execute the **queue cleaner** job, as explained in the [How it works](#how-it-works) section.

## Using cleanuperr just for failed *arr imports (works for Usenet users as well)

1. Set `QUEUECLEANER__ENABLED` to `true`.
2. Set `QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES` to a desired value.
3. Optionally set failed import message patterns to ignore using `QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__<NUMBER>`.
4. Set `DOWNLOAD_CLIENT` to `none`.

**No other action involving a download client would work (e.g. content blocking, removing stalled downloads, excluding private trackers).**

## Usage

### Docker compose yaml

```
version: "3.3"
services:
  cleanuperr:
    image: ghcr.io/flmorg/cleanuperr:latest
    restart: unless-stopped
    volumes:
      - ./cleanuperr/logs:/var/logs
    environment:
      - LOGGING__LOGLEVEL=Information
      - LOGGING__FILE__ENABLED=false
      - LOGGING__FILE__PATH=/var/logs/
      - LOGGING__ENHANCED=true

      - TRIGGERS__QUEUECLEANER=0 0/5 * * * ?
      - TRIGGERS__CONTENTBLOCKER=0 0/5 * * * ?

      - QUEUECLEANER__ENABLED=true
      - QUEUECLEANER__RUNSEQUENTIALLY=true
      - QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES=5
      - QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE=false
      - QUEUECLEANER__IMPORT_FAILED_DELETE_PRIVATE=false
      # - QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__0=title mismatch
      # - QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__1=manual import required
      - QUEUECLEANER__STALLED_MAX_STRIKES=5
      - QUEUECLEANER__STALLED_RESET_STRIKES_ON_PROGRESS=false
      - QUEUECLEANER__STALLED_IGNORE_PRIVATE=false
      - QUEUECLEANER__STALLED_DELETE_PRIVATE=false

      - CONTENTBLOCKER__ENABLED=true
      - CONTENTBLOCKER__IGNORE_PRIVATE=false
      - CONTENTBLOCKER__DELETE_PRIVATE=false

      - DOWNLOAD_CLIENT=none
      # OR
      # - DOWNLOAD_CLIENT=qBittorrent
      # - QBITTORRENT__URL=http://localhost:8080
      # - QBITTORRENT__USERNAME=user
      # - QBITTORRENT__PASSWORD=pass
      # OR
      # - DOWNLOAD_CLIENT=deluge
      # - DELUGE__URL=http://localhost:8112
      # - DELUGE__PASSWORD=testing
      # OR
      # - DOWNLOAD_CLIENT=transmission
      # - TRANSMISSION__URL=http://localhost:9091
      # - TRANSMISSION__USERNAME=test
      # - TRANSMISSION__PASSWORD=testing

      - SONARR__ENABLED=true
      - SONARR__SEARCHTYPE=Episode
      - SONARR__BLOCK__TYPE=blacklist
      - SONARR__BLOCK__PATH=https://example.com/path/to/file.txt
      - SONARR__INSTANCES__0__URL=http://localhost:8989
      - SONARR__INSTANCES__0__APIKEY=secret1
      - SONARR__INSTANCES__1__URL=http://localhost:8990
      - SONARR__INSTANCES__1__APIKEY=secret2

      - RADARR__ENABLED=true
      - RADARR__BLOCK__TYPE=blacklist
      - RADARR__BLOCK__PATH=https://example.com/path/to/file.txt
      - RADARR__INSTANCES__0__URL=http://localhost:7878
      - RADARR__INSTANCES__0__APIKEY=secret3
      - RADARR__INSTANCES__1__URL=http://localhost:7879
      - RADARR__INSTANCES__1__APIKEY=secret4

      - LIDARR__ENABLED=true
      - LIDARR__BLOCK__TYPE=blacklist
      - LIDARR__BLOCK__PATH=https://example.com/path/to/file.txt
      - LIDARR__INSTANCES__0__URL=http://radarr:8686
      - LIDARR__INSTANCES__0__APIKEY=secret5
      - LIDARR__INSTANCES__1__URL=http://radarr:8687
      - LIDARR__INSTANCES__1__APIKEY=secret6

      # - NOTIFIARR__ON_IMPORT_FAILED_STRIKE=false
      # - NOTIFIARR__ON_STALLED_STRIKE=false
      # - NOTIFIARR__ON_QUEUE_ITEM_DELETE=false
      # - NOTIFIARR__API_KEY=notifiarr_secret
      # - NOTIFIARR__CHANNEL_ID=discord_channel_id
```

## Environment variables

### General variables
<details>
  <summary>Click here</summary>

| Variable | Required | Description | Default value |
|---|---|---|---|
| LOGGING__LOGLEVEL | No | Can be `Verbose`, `Debug`, `Information`, `Warning`, `Error` or `Fatal`. | `Information` |
| LOGGING__FILE__ENABLED | No | Enable or disable logging to file. | false |
| LOGGING__FILE__PATH | No | Directory where to save the log files. | empty |
| LOGGING__ENHANCED | No | Enhance logs whenever possible.<br>A more detailed description is provided. [here](variables.md#LOGGING__ENHANCED) | true |
|||||
| TRIGGERS__QUEUECLEANER | Yes if queue cleaner is enabled | - [Quartz cron trigger](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).<br>- Can be a max of 6h interval.<br>- **Is ignored if `QUEUECLEANER__RUNSEQUENTIALLY=true` and `CONTENTBLOCKER__ENABLED=true`**. | 0 0/5 * * * ? |
| TRIGGERS__CONTENTBLOCKER | Yes if content blocker is enabled | - [Quartz cron trigger](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).<br>- Can be a max of 6h interval. | 0 0/5 * * * ? |
|||||
| QUEUECLEANER__ENABLED | No | Enable or disable the queue cleaner. | true |
| QUEUECLEANER__RUNSEQUENTIALLY | No | If set to true, the queue cleaner will run after the content blocker instead of running in parallel, streamlining the cleaning process. | true |
| QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES | No | - After how many strikes should a failed import be removed.<br>- 0 means never. | 0 |
| QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE | No | Whether to ignore failed imports from private trackers. | false |
| QUEUECLEANER__IMPORT_FAILED_DELETE_PRIVATE | No | - Whether to delete failed imports of private downloads from the download client.<br>- Does not have any effect if `QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE` is `true`.<br>- **Set this to `true` if you don't care about seeding, ratio, H&R and potentially losing your tracker account.** | false |
| QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__0 | No | - First pattern to look for when an import is failed.<br>- If the specified message pattern is found, the item is skipped. | empty |
| QUEUECLEANER__STALLED_MAX_STRIKES | No | - After how many strikes should a stalled download be removed.<br>- 0 means never. | 0 |
| QUEUECLEANER__STALLED_RESET_STRIKES_ON_PROGRESS | No | Whether to remove strikes if any download progress was made since last checked. | false |
| QUEUECLEANER__STALLED_IGNORE_PRIVATE | No | Whether to ignore stalled downloads from private trackers. | false |
| QUEUECLEANER__STALLED_DELETE_PRIVATE | No | - Whether to delete stalled private downloads from the download client.<br>- Does not have any effect if `QUEUECLEANER__STALLED_IGNORE_PRIVATE` is `true`.<br>- **Set this to `true` if you don't care about seeding, ratio, H&R and potentially losing your tracker account.** | false |
|||||
| CONTENTBLOCKER__ENABLED | No | Enable or disable the content blocker. | false |
| CONTENTBLOCKER__IGNORE_PRIVATE | No | Whether to ignore downloads from private trackers. | false |
| CONTENTBLOCKER__DELETE_PRIVATE | No | - Whether to delete private downloads that have all files blocked from the download client.<br>- Does not have any effect if `CONTENTBLOCKER__IGNORE_PRIVATE` is `true`.<br>- **Set this to `true` if you don't care about seeding, ratio, H&R and potentially losing your tracker account.** | false |
</details>

### Download client variables
<details>
  <summary>Click here</summary>

| Variable | Required | Description | Default value |
|---|---|---|---|
| DOWNLOAD_CLIENT | No | Download client that is used by *arrs<br>Can be `qbittorrent`, `deluge`, `transmission` or `none` | `none` |
| QBITTORRENT__URL | No | qBittorrent instance url | http://localhost:8112 |
| QBITTORRENT__USERNAME | No | qBittorrent user | empty |
| QBITTORRENT__PASSWORD | No | qBittorrent password | empty |
|||||
| DELUGE__URL | No | Deluge instance url | http://localhost:8080 |
| DELUGE__PASSWORD | No | Deluge password | empty |
|||||
| TRANSMISSION__URL | No | Transmission instance url | http://localhost:9091 |
| TRANSMISSION__USERNAME | No | Transmission user | empty |
| TRANSMISSION__PASSWORD | No | Transmission password | empty |
</details>

### Arr variables
<details>
  <summary>Click here</summary>

| Variable | Required | Description | Default value |
|---|---|---|---|
| SONARR__ENABLED | No | Enable or disable Sonarr cleanup  | false |
| SONARR__BLOCK__TYPE | No | Block type<br>Can be `blacklist` or `whitelist` | `blacklist` |
| SONARR__BLOCK__PATH | No | Path to the blocklist (local file or url)<br>Needs to be json compatible | empty |
| SONARR__SEARCHTYPE | No | What to search for after removing a queue item<br>Can be `Episode`, `Season` or `Series` | `Episode` |
| SONARR__INSTANCES__0__URL | No | First Sonarr instance url | http://localhost:8989 |
| SONARR__INSTANCES__0__APIKEY | No | First Sonarr instance API key | empty |
|||||
| RADARR__ENABLED | No | Enable or disable Radarr cleanup  | false |
| RADARR__BLOCK__TYPE | No | Block type<br>Can be `blacklist` or `whitelist` | `blacklist` |
| RADARR__BLOCK__PATH | No | Path to the blocklist (local file or url)<br>Needs to be json compatible | empty |
| RADARR__INSTANCES__0__URL | No | First Radarr instance url | http://localhost:7878 |
| RADARR__INSTANCES__0__APIKEY | No | First Radarr instance API key | empty |
|||||
| LIDARR__ENABLED | No | Enable or disable LIDARR cleanup  | false |
| LIDARR__BLOCK__TYPE | No | Block type<br>Can be `blacklist` or `whitelist` | `blacklist` |
| LIDARR__BLOCK__PATH | No | Path to the blocklist (local file or url)<br>Needs to be json compatible | empty |
| LIDARR__INSTANCES__0__URL | No | First LIDARR instance url | http://localhost:8686 |
| LIDARR__INSTANCES__0__APIKEY | No | First LIDARR instance API key | empty |
</details>

### Notifications variables
<details>
  <summary>Click here</summary>

| Variable | Required | Description | Default value |
|---|---|---|---|
| NOTIFIARR__ON_IMPORT_FAILED_STRIKE | No | Notify on failed import strike.  | false |
| NOTIFIARR__ON_STALLED_STRIKE | No | Notify on stalled download strike. | false |
| NOTIFIARR__ON_QUEUE_ITEM_DELETE | No | Notify on deleting a queue item. | false |
| NOTIFIARR__API_KEY | No | Notifiarr API key.<br>Requires Notifiarr's `Passthrough` integration to work. | empty |
| NOTIFIARR__CHANNEL_ID | No | Discord channel id for notifications. | empty |
</details>


### Advanced variables
<details>
  <summary>Click here</summary>

| Variable | Required | Description | Default value |
|---|---|---|---|
| HTTP_MAX_RETRIES | No | The number of times to retry a failed HTTP call (to *arrs, download clients etc.) | 0 |
| HTTP_TIMEOUT | No | The number of seconds to wait before failing an HTTP call (to *arrs, download clients etc.) | 100 |
</details>

#

> [!NOTE]
> 1. The queue cleaner and content blocker can be enabled or disabled separately, if you want to run only one of them.
> 2. Only one download client can be enabled at a time. If you have more than one download client, you should deploy multiple instances of cleanuperr.
> 3. The blocklists (blacklist/whitelist) should have a single pattern on each line and supports the following:
> ```
> *example            // file name ends with "example"
> example*            // file name starts with "example"
> *example*           // file name has "example" in the name
> example             // file name is exactly the word "example"
> regex:<ANY_REGEX>   // regex that needs to be marked at the start of the line with "regex:"
> ```
> 4. Multiple Sonarr/Radarr/Lidarr instances can be specified using this format, where `<NUMBER>` starts from `0`:
> ```
> SONARR__INSTANCES__<NUMBER>__URL
> SONARR__INSTANCES__<NUMBER>__APIKEY
> ```
> 5. Multiple failed import patterns can be specified using this format, where `<NUMBER>` starts from 0:
> ```
> QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__<NUMBER>
> ```
> 6. [This blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist) and [this whitelist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/whitelist) can be used for Sonarr and Radarr, but they are not suitable for other *arrs.

#

### Binaries (if you're not using Docker)

1. Download the binaries from [releases](https://github.com/flmorg/cleanuperr/releases).
2. Extract them from the zip file.
3. Edit **appsettings.json**. The paths from this json file correspond with the docker env vars, as described [above](#environment-variables).

> [!TIP]
> ### Run as a Windows Service
> Check out this stackoverflow answer on how to do it: https://stackoverflow.com/a/15719678

# Credits
Special thanks for inspiration go to:
- [ThijmenGThN/swaparr](https://github.com/ThijmenGThN/swaparr)
- [ManiMatter/decluttarr](https://github.com/ManiMatter/decluttarr)
- [PaeyMoopy/sonarr-radarr-queue-cleaner](https://github.com/PaeyMoopy/sonarr-radarr-queue-cleaner)
- [Sonarr](https://github.com/Sonarr/Sonarr) & [Radarr](https://github.com/Radarr/Radarr) for the logo

# Buy me a coffee
If I made your life just a tiny bit easier, consider buying me a coffee!

<a href="https://buymeacoffee.com/flaminel" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>