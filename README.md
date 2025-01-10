# <img width="24px" src="./Logo/256.png" alt="cleanuperr"></img> cleanuperr

cleanuperr is a tool for automating the cleanup of unwanted or blocked files in Sonarr, Radarr, and supported download clients like qBittorrent. It removes incomplete or blocked downloads, updates queues, and enforces blacklists or whitelists to manage file selection. After removing blocked content, cleanuperr can also trigger a search to replace the deleted shows/movies.

cleanuperr was created primarily to address malicious files, such as `*.lnk` or `*.zipx`, that were getting stuck in Sonarr/Radarr and required manual intervention. Some of the reddit posts that made cleanuperr come to life can be found [here](https://www.reddit.com/r/sonarr/comments/1gqnx16/psa_sonarr_downloaded_a_virus/), [here](https://www.reddit.com/r/sonarr/comments/1gqwklr/sonar_downloaded_a_mkv_file_which_looked_like_a/), [here](https://www.reddit.com/r/sonarr/comments/1gpw2wa/downloaded_waiting_to_import/) and [here](https://www.reddit.com/r/sonarr/comments/1gpi344/downloads_not_importing_no_files_found/).

The tool supports both qBittorrent's built-in exclusion features and its own blocklist-based system. Binaries for all platforms are provided, along with Docker images for easy deployment.

Refer to the [Environment variables](#Environment-variables) section for detailed configuration instructions and the [Setup](#Setup) section for an in-depth explanation of the cleanup process.

## Key features
- Marks unwanted files as skip/unwanted in the download client.
- Automatically strikes stalled or stuck downloads. 
- Removes and blocks downloads that reached the maximum number of strikes or are marked as unwanted by the download client or by cleanuperr and triggers a search for removed downloads.

## Important note

Only the **latest versions** of the following apps are supported, or earlier versions that have the same API as the latest version:
- qBittorrent
- Deluge
- Transmission
- Sonarr
- Radarr

This tool is actively developed and still a work in progress. Join the Discord server if you want to reach out to me quickly (or just stay updated on new releases) so we can squash those pesky bugs together:

> https://discord.gg/sWggpnmGNY

# How it works

1. **Content blocker** will:
   - Run every 5 minutes (or configured cron).
   - Process all items in the *arr queue.
   - Find the corresponding item from the download client for each queue item.
   - Mark the files that were found in the queue as **unwanted/skipped** if:
     - They **are listed in the blacklist**, or
     - They **are not included in the whitelist**.
2. **Queue cleaner** will:
   - Run every 5 minutes (or configured cron).
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

1. Set both `QUEUECLEANER_ENABLED` and `CONTENTBLOCKER_ENABLED` to `true` in your environment variables.
2. Configure and enable either a **blacklist** or a **whitelist** as described in the [Environment variables](#Environment-variables) section.
3. Once configured, cleanuperr will perform the following tasks:
   - Execute the **content blocker** job, as explained in the [How it works](#how-it-works) section.
   - Execute the **queue cleaner** job, as explained in the [How it works](#how-it-works) section.

## Using cleanuperr just for failed *arr imports (works for Usenet users as well)

1. Set `QUEUECLEANER_ENABLED` to `true`.
2. Set `QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES` to a desired value.
3. Set `DOWNLOAD_CLIENT` to `none`.

**No other action involving a download client would work (e.g. content blocking, removing stalled downloads etc.).**

## Usage

### Docker compose yaml

```
version: "3.3"
services:
  cleanuperr:
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
      - QUEUECLEANER__STALLED_MAX_STRIKES=5

      - CONTENTBLOCKER__ENABLED=true
      - CONTENTBLOCKER__BLACKLIST__ENABLED=true
      - CONTENTBLOCKER__BLACKLIST__PATH=https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist
      # OR
      # - CONTENTBLOCKER__WHITELIST__ENABLED=true
      # - CONTENTBLOCKER__WHITELIST__PATH=https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/whitelist

      - DOWNLOAD_CLIENT=qBittorrent
      - QBITTORRENT__URL=http://localhost:8080
      - QBITTORRENT__USERNAME=user
      - QBITTORRENT__PASSWORD=pass
      # OR
      # - DOWNLOAD_CLIENT=deluge
      # - DELUGE__URL=http://localhost:8112
      # - DELUGE__PASSWORD=testing
      # OR
      # - DOWNLOAD_CLIENT=transmission
      # - TRANSMISSION__URL=http://localhost:9091
      # - TRANSMISSION__USERNAME=test
      # - TRANSMISSION__PASSWORD=testing
      # OR
      # - DOWNLOAD_CLIENT=none

      - SONARR__ENABLED=true
      - SONARR__SEARCHTYPE=Episode
      - SONARR__INSTANCES__0__URL=http://localhost:8989
      - SONARR__INSTANCES__0__APIKEY=secret1
      - SONARR__INSTANCES__1__URL=http://localhost:8990
      - SONARR__INSTANCES__1__APIKEY=secret2

      - RADARR__ENABLED=true
      - RADARR__INSTANCES__0__URL=http://localhost:7878
      - RADARR__INSTANCES__0__APIKEY=secret3
      - RADARR__INSTANCES__1__URL=http://localhost:7879
      - RADARR__INSTANCES__1__APIKEY=secret4
    image: ghcr.io/flmorg/cleanuperr:latest
    restart: unless-stopped
```

### Environment variables

| Variable | Required | Description | Default value |
|---|---|---|---|
| LOGGING__LOGLEVEL | No | Can be `Verbose`, `Debug`, `Information`, `Warning`, `Error` or `Fatal` | `Information` |
| LOGGING__FILE__ENABLED | No | Enable or disable logging to file | false |
| LOGGING__FILE__PATH | No | Directory where to save the log files | empty |
| LOGGING__ENHANCED | No | Enhance logs whenever possible<br>A more detailed description is provided [here](variables.md#LOGGING__ENHANCED) | true |
|||||
| TRIGGERS__QUEUECLEANER | Yes if queue cleaner is enabled | [Quartz cron trigger](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html)<br>Can be a max of 6h interval<br>**Is ignored if `QUEUECLEANER__RUNSEQUENTIALLY=true` and `CONTENTBLOCKER__ENABLED=true`** | 0 0/5 * * * ? |
| TRIGGERS__CONTENTBLOCKER | Yes if content blocker is enabled | [Quartz cron trigger](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html)<br>Can be a max of 6h interval | 0 0/5 * * * ? |
|||||
| QUEUECLEANER__ENABLED | No | Enable or disable the queue cleaner | true |
| QUEUECLEANER__RUNSEQUENTIALLY | No | If set to true, the queue cleaner will run after the content blocker instead of running in parallel, streamlining the cleaning process | true |
| QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES | No | After how many strikes should a failed import be removed<br>0 means never | 0 |
| QUEUECLEANER__STALLED_MAX_STRIKES | No | After how many strikes should a stalled download be removed<br>0 means never | 0 |
|||||
| CONTENTBLOCKER__ENABLED | No | Enable or disable the content blocker | false |
| CONTENTBLOCKER__BLACKLIST__ENABLED | Yes if content blocker is enabled and whitelist is not enabled | Enable or disable the blacklist | false |
| CONTENTBLOCKER__BLACKLIST__PATH | Yes if blacklist is enabled | Path to the blacklist (local file or url)<br>Needs to be json compatible | empty |
| CONTENTBLOCKER__WHITELIST__ENABLED | Yes if content blocker is enabled and blacklist is not enabled | Enable or disable the whitelist | false |
| CONTENTBLOCKER__WHITELIST__PATH | Yes if whitelist is enabled | Path to the whitelist (local file or url)<br>Needs to be json compatible | empty |
|||||
| DOWNLOAD_CLIENT | No | Download client that is used by *arrs<br>Can be `qbittorrent`, `deluge`, `transmission` or `none` | `qbittorrent` |
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
|||||
| SONARR__ENABLED | No | Enable or disable Sonarr cleanup  | true |
| SONARR__SEARCHTYPE | No | What to search for after removing a queue item<br>Can be `Episode`, `Season` or `Series` | `Episode` |
| SONARR__INSTANCES__0__URL | No | First Sonarr instance url | http://localhost:8989 |
| SONARR__INSTANCES__0__APIKEY | No | First Sonarr instance API key | empty |
|||||
| RADARR__ENABLED | No | Enable or disable Radarr cleanup  | false |
| RADARR__INSTANCES__0__URL | No | First Radarr instance url | http://localhost:8989 |
| RADARR__INSTANCES__0__APIKEY | No | First Radarr instance API key | empty |

#
### To be noted

1. The blacklist and the whitelist can not be both enabled at the same time.
2. The queue cleaner and content blocker can be enabled or disabled separately, if you want to run only one of them.
3. Only one download client can be enabled at a time. If you have more than one download client, you should deploy multiple instances of cleanuperr.
4. The blocklists (blacklist/whitelist) should have a single pattern on each line and supports the following:
```
*example      // file name ends with "example"
example*      // file name starts with "example"
*example*     // file name has "example" in the name
example       // file name is exactly the word "example"
regex:<ANY_REGEX>   // regex that needs to be marked at the start of the line with "regex:"
```
5. Multiple Sonarr/Radarr instances can be specified using this format, where `<NUMBER>` starts from 0:
```
SONARR__INSTANCES__<NUMBER>__URL
SONARR__INSTANCES__<NUMBER>__APIKEY
```

#

### Binaries (if you're not using Docker)

1. Download the binaries from [releases](https://github.com/flmorg/cleanuperr/releases).
2. Extract them from the zip file.
3. Edit **appsettings.json**. The paths from this json file correspond with the docker env vars, as described [above](#environment-variables).

### Run as a Windows Service

Check out this stackoverflow answer on how to do it: https://stackoverflow.com/a/15719678

# Credits
Special thanks for inspiration go to:
- [ThijmenGThN/swaparr](https://github.com/ThijmenGThN/swaparr)
- [ManiMatter/decluttarr](https://github.com/ManiMatter/decluttarr)
- [PaeyMoopy/sonarr-radarr-queue-cleaner](https://github.com/PaeyMoopy/sonarr-radarr-queue-cleaner)
- [Sonarr](https://github.com/Sonarr/Sonarr) & [Radarr](https://github.com/Radarr/Radarr) for the logo

# Buy me a coffee
If I made your life just a tiny bit easier, consider buying me a coffee!

<a href="https://buymeacoffee.com/flaminel" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>