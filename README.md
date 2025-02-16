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
   - Check each queue item if it is **stalled (download speed is 0)**, **stuck in metadata downloading** or **failed to be imported**.
     - If it is, the item receives a **strike** and will continue to accumulate strikes every time it meets any of these conditions.
   - Check each queue item if it meets one of the following condition in the download client:
     - **Marked as completed, but 0 bytes have been downloaded** (due to files being blocked by qBittorrent or the **content blocker**).
     - All associated files of are marked as **unwanted/skipped**.
   - If the item **DOES NOT** match the above criteria, it will be skipped.
   - If the item **DOES** match the criteria or has received the **maximum number of strikes**:
     - It will be removed from the *arr's queue and blocked.
     - It will be deleted from the download client.
     - A new search will be triggered for the *arr item.
3. **Download cleaner** will:
   - Run every hour (or configured cron).
   - Automatically clean up downloads that have been seeding for a certain amount of time.

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

### General settings
<details>
  <summary>Click here</summary>
<br>

**`DRY_RUN`**
- When enabled, simulates irreversible operations (like deletions and notifications) without making actual changes.
- Type: Boolean.
- Possible values: `true`, `false`.
- Default: `false`.
- Required: No.

**`LOGGING__LOGLEVEL`**
- Controls the detail level of application logs.
- Type: String.
- Possible values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.
- Default: `Information`.
- Required: No.

**`LOGGING__FILE__ENABLED`**
- Enables logging to a file.
- Type: Boolean.
- Possible values: `true`, `false`.
- Default: `false`.
- Required: No.

**`LOGGING__FILE__PATH`**
- Directory where log files will be saved.
- Type: String.
- Default: Empty.
- Required: No.

**`LOGGING__ENHANCED`**
- Provides more detailed descriptions in logs whenever possible.
- Type: Boolean.
- Possible values: `true`, `false`.
- Default: `true`.
- Required: No.
</details>

#

### Queue Cleaner settings
<details>
  <summary>Click here</summary>
<br>

**`TRIGGERS__QUEUECLEANER`**
- Cron schedule for the queue cleaner job.
- Type: String - [Quartz cron format](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).
- Default: `0 0/5 * * * ?` (every 5 minutes).
- Required: Yes if queue cleaner is enabled.

> [!NOTE]
> - Maximum interval is 6 hours.
> - Ignored if `QUEUECLEANER__RUNSEQUENTIALLY=true` and `CONTENTBLOCKER__ENABLED=true`.

**`QUEUECLEANER__ENABLED`**
- Enables or disables the queue cleaning functionality.
- When enabled, processes all items in the *arr queue.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `true`
- Required: No.

**`QUEUECLEANER__RUNSEQUENTIALLY`**
- Controls whether queue cleaner runs after content blocker instead of in parallel.
- When `true`, streamlines the cleaning process by running immediately after content blocker.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `true`
- Required: No.

**`QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES`**
- Number of strikes before removing a failed import.
- Set to `0` to never remove failed imports.
- A strike is given when an item is stalled, stuck in metadata downloading, or failed to be imported.
- Type: Integer
- Possible values: `0` or greater
- Default: `0`
- Required: No.

**`QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE`**
- Controls whether to ignore failed imports from private trackers.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`QUEUECLEANER__IMPORT_FAILED_DELETE_PRIVATE`**
- Controls whether to delete failed imports from private trackers from the download client.
- Has no effect if `QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE` is `true`.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `QUEUECLEANER__IMPORT_FAILED_DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your tracker account.

**`QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS`**
- Patterns to look for in failed import messages that should be ignored.
- Multiple patterns can be specified using incrementing numbers starting from 0.
- Type: String array
- Default: Empty.
- Required: No.
- Example:
```yaml
QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__0: "title mismatch"
QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__1: "manual import required"
```

**`QUEUECLEANER__STALLED_MAX_STRIKES`**
- Number of strikes before removing a stalled download.
- Set to `0` to never remove stalled downloads.
- A strike is given when download speed is 0.
- Type: Integer
- Possible values: `0` or greater
- Default: `0`
- Required: No.

**`QUEUECLEANER__STALLED_RESET_STRIKES_ON_PROGRESS`**
- Controls whether to remove strikes if any download progress was made since last checked.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`QUEUECLEANER__STALLED_IGNORE_PRIVATE`**
- Controls whether to ignore stalled downloads from private trackers.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`QUEUECLEANER__STALLED_DELETE_PRIVATE`**
- Controls whether to delete stalled private downloads from the download client.
- Has no effect if `QUEUECLEANER__STALLED_IGNORE_PRIVATE` is `true`.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `QUEUECLEANER__STALLED_DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your tracker account.
</details>



#

### Content Blocker settings
<details>
  <summary>Click here</summary>
<br>

**`TRIGGERS__CONTENTBLOCKER`**
- Cron schedule for the content blocker job.
- Type: String - [Quartz cron format](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).
- Default: `0 0/5 * * * ?` (every 5 minutes).
- Required: No.

**`CONTENTBLOCKER__ENABLED`**
- Enables or disables the content blocker functionality.
- When enabled, processes all items in the *arr queue and marks unwanted files.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`CONTENTBLOCKER__IGNORE_PRIVATE`**
- Controls whether to ignore downloads from private trackers.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`CONTENTBLOCKER__DELETE_PRIVATE`**
- Controls whether to delete private downloads that have all files blocked from the download client.
- Has no effect if `CONTENTBLOCKER__IGNORE_PRIVATE` is `true`.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `CONTENTBLOCKER__DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your tracker account.
</details>

#

### Download Cleaner settings
<details>
  <summary>Click here</summary>
<br>

**`TRIGGERS__DOWNLOADCLEANER`**
- Cron schedule for the download cleaner job.
- Type: String - [Quartz cron format](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).
- Default: `0 0 * * * ?` (every hour).
- Required: No.

**`DOWNLOADCLEANER__ENABLED`**
- Enables or disables the download cleaner functionality.
- When enabled, automatically cleans up downloads that have been seeding for a certain amount of time.
- Type: Boolean.
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`DOWNLOADCLEANER__DELETE_PRIVATE`**
- Controls whether to delete private downloads.
- Type: Boolean.
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `DOWNLOADCLEANER__DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your tracker account.

**`DOWNLOADCLEANER__CATEGORIES__0__NAME`**
- Name of the category to clean.
- Type: String.
- Default: Empty.
- Required: No.

**`DOWNLOADCLEANER__CATEGORIES__0__MAX_RATIO`**
- Maximum ratio to reach before removing a download.
- Type: Decimal.
- Possible values: `-1` or greater (`-1` means no limit or disabled).
- Default: `-1`
- Required: No.

**`DOWNLOADCLEANER__CATEGORIES__0__MIN_SEED_TIME`**
- Minimum number of hours to seed before removing a download, if the ratio has been met.
- Used with `MAX_RATIO` to ensure a minimum seed time.
- Type: Decimal.
- Possible values: `0` or greater.
- Default: `0`
- Required: No.

**`DOWNLOADCLEANER__CATEGORIES__0__MAX_SEED_TIME`**
- Maximum number of hours to seed before removing a download.
- Type: Decimal.
- Possible values: `-1` or greater (`-1` means no limit or disabled).
- Default: `-1`
- Required: No.

> [!NOTE]
> 1. A download is cleaned when any of (`MAX_RATIO` & `MIN_SEED_TIME`) or `MAX_SEED_TIME` is reached.
> 2. Multiple categories can be specified using this format, where `<NUMBER>` starts from 0:
> ```yaml
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__NAME
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__MAX_RATIO
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__MIN_SEED_TIME
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__MAX_SEED_TIME
> ```
</details>


#

### Download Client settings
<details>
  <summary>Click here</summary>
<br>

**`DOWNLOAD_CLIENT`**
- Specifies which download client is used by *arrs.
- Type: String.
- Possible values: `none`, `qbittorrent`, `deluge`, `transmission`.
- Default: `none`
- Required: No.

> [!NOTE]
> Only one download client can be enabled at a time. If you have more than one download client, you should deploy multiple instances of cleanuperr.

**`QBITTORRENT__URL`**
- URL of the qBittorrent instance.
- Type: String.
- Default: `http://localhost:8080`.
- Required: No.

**`QBITTORRENT__USERNAME`**
- Username for qBittorrent authentication.
- Type: String.
- Default: Empty.
- Required: No.

**`QBITTORRENT__PASSWORD`**
- Password for qBittorrent authentication.
- Type: String.
- Default: Empty.
- Required: No.

**`DELUGE__URL`**
- URL of the Deluge instance.
- Type: String.
- Default: `http://localhost:8112`.
- Required: No.

**`DELUGE__PASSWORD`**
- Password for Deluge authentication.
- Type: String.
- Default: Empty.
- Required: No.

**`TRANSMISSION__URL`**
- URL of the Transmission instance.
- Type: String.
- Default: `http://localhost:9091`.
- Required: No.

**`TRANSMISSION__USERNAME`**
- Username for Transmission authentication.
- Type: String.
- Default: Empty.
- Required: No.

**`TRANSMISSION__PASSWORD`**
- Password for Transmission authentication.
- Type: String.
- Default: Empty.
- Required: No.
</details>

#

### Arr settings
<details>
  <summary>Click here</summary>
<br>

**`SONARR__ENABLED`**
- Enables or disables Sonarr cleanup.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`SONARR__BLOCK__TYPE`**
- Determines how file blocking works for Sonarr.
- Type: String
- Possible values: `blacklist`, `whitelist`
- Default: `blacklist`
- Required: No.

**`SONARR__BLOCK__PATH`**
- Path to the blocklist file (local file or URL).
- Must be JSON compatible.
- Type: String
- Default: Empty.
- Required: No.

**`SONARR__SEARCHTYPE`**
- Determines what to search for after removing a queue item.
- Type: String
- Possible values: `Episode`, `Season`, `Series`
- Default: `Episode`
- Required: No.

**`SONARR__INSTANCES__0__URL`**
- URL of the Sonarr instance.
- Type: String
- Default: `http://localhost:8989`
- Required: No.

**`SONARR__INSTANCES__0__APIKEY`**
- API key for the Sonarr instance.
- Type: String
- Default: Empty.
- Required: No.

**`RADARR__ENABLED`**
- Enables or disables Radarr cleanup.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`RADARR__BLOCK__TYPE`**
- Determines how file blocking works for Radarr.
- Type: String
- Possible values: `blacklist`, `whitelist`
- Default: `blacklist`
- Required: No.

**`RADARR__BLOCK__PATH`**
- Path to the blocklist file (local file or URL).
- Must be JSON compatible.
- Type: String
- Default: Empty.
- Required: No.

**`RADARR__INSTANCES__0__URL`**
- URL of the Radarr instance.
- Type: String
- Default: `http://localhost:7878`
- Required: No.

**`RADARR__INSTANCES__0__APIKEY`**
- API key for the Radarr instance.
- Type: String
- Default: Empty.
- Required: No.

**`LIDARR__ENABLED`**
- Enables or disables Lidarr cleanup.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`LIDARR__BLOCK__TYPE`**
- Determines how file blocking works for Lidarr.
- Type: String
- Possible values: `blacklist`, `whitelist`
- Default: `blacklist`
- Required: No.

**`LIDARR__BLOCK__PATH`**
- Path to the blocklist file (local file or URL).
- Must be JSON compatible.
- Type: String
- Default: Empty.
- Required: No.

**`LIDARR__INSTANCES__0__URL`**
- URL of the Lidarr instance.
- Type: String
- Default: `http://localhost:8686`
- Required: No.

**`LIDARR__INSTANCES__0__APIKEY`**
- API key for the Lidarr instance.
- Type: String
- Default: Empty.
- Required: No.

> [!NOTE]
> 1. Multiple instances can be specified for each *arr using this format, where `<NUMBER>` starts from 0:
> ```yaml
> <ARR>__INSTANCES__<NUMBER>__URL
> <ARR>__INSTANCES__<NUMBER>__APIKEY
> ```
> 2. The blocklists (blacklist/whitelist) support the following patterns:
> ```
> *example            // file name ends with "example"
> example*            // file name starts with "example"
> *example*           // file name has "example" in the name
> example             // file name is exactly the word "example"
> regex:<ANY_REGEX>   // regex that needs to be marked at the start of the line with "regex:"
> ```
> 3. [This blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist) and [this whitelist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/whitelist) can be used for Sonarr and Radarr, but they are not suitable for other *arrs.
</details>

#

### Notification settings
<details>
  <summary>Click here</summary>
<br>

**`NOTIFIARR__API_KEY`**
- Notifiarr API key for sending notifications.
- Requires Notifiarr's [`Passthrough`](https://notifiarr.wiki/en/Website/Integrations/Passthrough) integration to work.
- Type: String
- Default: Empty.
- Required: No.

**`NOTIFIARR__CHANNEL_ID`**
- Discord channel ID where notifications will be sent.
- Type: String
- Default: Empty.
- Required: No.

**`NOTIFIARR__ON_IMPORT_FAILED_STRIKE`**
- Controls whether to notify when an item receives a failed import strike.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`NOTIFIARR__ON_STALLED_STRIKE`**
- Controls whether to notify when an item receives a stalled download strike.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`NOTIFIARR__ON_QUEUE_ITEM_DELETED`**
- Controls whether to notify when a queue item is deleted.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

**`NOTIFIARR__ON_DOWNLOAD_CLEANED`**
- Controls whether to notify when a download is cleaned.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.
</details>

#

### Advanced settings
<details>
  <summary>Click here</summary>
<br>

**`HTTP_MAX_RETRIES`**
- The number of times to retry a failed HTTP call.
- Applies to calls to *arrs, download clients, and other services.
- Type: Integer
- Possible values: `0` or greater
- Default: `0`
- Required: No.

**`HTTP_TIMEOUT`**
- The number of seconds to wait before failing an HTTP call.
- Applies to calls to *arrs, download clients, and other services.
- Type: Integer
- Possible values: Greater than `0`
- Default: `100`
- Required: No.
</details>

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
- [Sonarr](https://github.com/Sonarr/Sonarr) & [Radarr](https://github.com/Radarr/Radarr)

# Buy me a coffee
If I made your life just a tiny bit easier, consider buying me a coffee!

<a href="https://buymeacoffee.com/flaminel" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>

