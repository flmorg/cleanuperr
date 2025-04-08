## Table of contents
- [General settings](#general-settings)
- [Queue Cleaner settings](#queue-cleaner-settings)
- [Content Blocker settings](#content-blocker-settings)
- [Download Cleaner settings](#download-cleaner-settings)
- [Download Client settings](#download-client-settings)
- [Arr settings](#arr-settings)
- [Notification settings](#notification-settings)
- [Advanced settings](#advanced-settings)

#

### General settings

#### **`TZ`**
- The time zone to use.
- Type: String.
- Possible values: Any valid timezone.
- Default: `UTC`.
- Required: No.

#### **`DRY_RUN`**
- When enabled, simulates irreversible operations (like deletions and notifications) without making actual changes.
- Type: Boolean.
- Possible values: `true`, `false`.
- Default: `false`.
- Required: No.

#### **`LOGGING__LOGLEVEL`**
- Controls the detail level of application logs.
- Type: String.
- Possible values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.
- Default: `Information`.
- Required: No.

#### **`LOGGING__FILE__ENABLED`**
- Enables logging to a file.
- Type: Boolean.
- Possible values: `true`, `false`.
- Default: `false`.
- Required: No.

#### **`LOGGING__FILE__PATH`**
- Directory where log files will be saved.
- Type: String.
- Default: Empty.
- Required: No.

#### **`LOGGING__ENHANCED`**
- Provides more detailed descriptions in logs whenever possible.
- Type: Boolean.
- Possible values: `true`, `false`.
- Default: `true`.
- Required: No.
</details>

#

### Queue Cleaner settings

#### **`TRIGGERS__QUEUECLEANER`**
- Cron schedule for the queue cleaner job.
- Type: String - [Quartz cron format](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).
- Default: `0 0/5 * * * ?` (every 5 minutes).
- Required: Yes if queue cleaner is enabled.

> [!NOTE]
> - Maximum interval is 6 hours.
> - Is ignored if `QUEUECLEANER__RUNSEQUENTIALLY=true` and `CONTENTBLOCKER__ENABLED=true`.

#### **`QUEUECLEANER__ENABLED`**
- Enables or disables the queue cleaning functionality.
- When enabled, processes all items in the *arr queue.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `true`
- Required: No.

#### **`QUEUECLEANER__IGNORED_DOWNLOADS_PATH`**
- Local path to the file containing ignored downloads.
- If the contents of the file are changed, they will be reloaded on the next job run.
- Accepted values:
  -  torrent hash
  -  qBitTorrent tag or category
  -  Deluge label
  -  Transmission category (last directory from the save location)
  -  torrent tracker domain
- Each value needs to be on a new line.
- Type: String.
- Default: Empty.
- Required: No.
- Example: `/ignored.txt`.
- Example of file contents:
    ```
    fa800a7d7c443a2c3561d1f8f393c089036dade1
    tv-sonarr
    qbit-tag
    mytracker.com
    ...
    ```
>[!IMPORTANT]
> Some people have experienced problems using Docker where the mounted file would not update inside the container if it was modified on the host. This is a Docker configuration problem and can not be solved by cleanuperr.

#### **`QUEUECLEANER__RUNSEQUENTIALLY`**
- Controls whether queue cleaner runs after content blocker instead of in parallel.
- When `true`, streamlines the cleaning process by running immediately after content blocker.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `true`
- Required: No.

#### **`QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES`**
- Number of strikes before removing a failed import.
- Set to `0` to never remove failed imports.
- A strike is given when an item fails to be imported.
- Type: Integer
- Default: `0`
- Required: No.
> [!NOTE]
> If not set to `0`, the minimum value is `3`.

#### **`QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE`**
- Controls whether to ignore failed imports from private trackers.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`QUEUECLEANER__IMPORT_FAILED_DELETE_PRIVATE`**
- Controls whether to delete failed imports from private trackers from the download client.
- Has no effect if `QUEUECLEANER__IMPORT_FAILED_IGNORE_PRIVATE` is `true`.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `QUEUECLEANER__IMPORT_FAILED_DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account.

#### **`QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS`**
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

#### **`QUEUECLEANER__STALLED_MAX_STRIKES`**
- Number of strikes before removing a stalled download.
- Set to `0` to never remove stalled downloads.
- A strike is given when an item is stalled (not downloading) or stuck while downloading metadata.
- Type: Integer
- Default: `0`
- Required: No.
> [!NOTE]
> If not set to `0`, the minimum value is `3`.

#### **`QUEUECLEANER__STALLED_RESET_STRIKES_ON_PROGRESS`**
- Controls whether to remove the given strikes if any download progress was made since last checked.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`QUEUECLEANER__STALLED_IGNORE_PRIVATE`**
- Controls whether to ignore stalled downloads from private trackers.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`QUEUECLEANER__STALLED_DELETE_PRIVATE`**
- Controls whether stalled downloads from private trackers should be removed from the download client.
- Has no effect if `QUEUECLEANER__STALLED_IGNORE_PRIVATE` is `true`.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `QUEUECLEANER__STALLED_DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account.

#### **`QUEUECLEANER__SLOW_MAX_STRIKES`**
- Number of strikes before removing a slow download.
- Set to `0` to never remove slow downloads.
- A strike is given when an item is slow.
- Type: Integer
- Default: `0`
- Required: No.
> [!NOTE]
> If not set to `0`, the minimum value is `3`.

#### **`QUEUECLEANER__SLOW_RESET_STRIKES_ON_PROGRESS`**
- Controls whether to remove the given strikes if the download speed or estimated time are not slow anymore.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`QUEUECLEANER__SLOW_IGNORE_PRIVATE`**
- Controls whether to ignore slow downloads from private trackers.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`QUEUECLEANER__SLOW_DELETE_PRIVATE`**
- Controls whether slow downloads from private trackers should be removed from the download client.
- Has no effect if `QUEUECLEANER__SLOW_IGNORE_PRIVATE` is `true`.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `QUEUECLEANER__SLOW_DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account.

#### **`QUEUECLEANER__SLOW_MIN_SPEED`**
- The minimum speed a download should have.
- Downloads receive strikes if their speed falls bellow this value.
- If not specified, downloads will not receive strikes for slow download speed.
- Type: String.
- Default: Empty.
- Required: No.
- Value examples: `1.5KB`, `400KB`, `2MB`

#### **`QUEUECLEANER__SLOW_MAX_TIME`**
- The maximum estimated hours a download should take to finish.
- Downloads receive strikes if their estimated finish time is above this value.
- If not specified (or `0`), downloads will not receive strikes for slow estimated finish time.
- Type: Integer.
- Default: `0`.
- Required: No.

#### **`QUEUECLEANER__SLOW_IGNORE_ABOVE_SIZE`**
- Downloads above this size will not be removed for being slow.
- Type: String.
- Default: Empty.
- Required: No.
- Value examples: `10KB`, `200MB`, `3GB`.

#

### Content Blocker settings

#### **`TRIGGERS__CONTENTBLOCKER`**
- Cron schedule for the content blocker job.
- Type: String - [Quartz cron format](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).
- Default: `0 0/5 * * * ?` (every 5 minutes).
- Required: No.

> [!NOTE]
> - Maximum interval is 6 hours.

#### **`CONTENTBLOCKER__ENABLED`**
- Enables or disables the content blocker functionality.
- When enabled, processes all items in the *arr queue and marks unwanted files.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`CONTENTBLOCKER__IGNORED_DOWNLOADS_PATH`**
- Local path to the file containing ignored downloads.
- If the contents of the file are changed, they will be reloaded on the next job run.
- Accepted values:
  -  torrent hash
  -  qBitTorrent tag or category
  -  Deluge label
  -  Transmission category (last directory from the save location)
  -  torrent tracker domain
- Each value needs to be on a new line.
- Type: String.
- Default: Empty.
- Required: No.
- Example: `/ignored.txt`.
- Example of file contents:
    ```
    fa800a7d7c443a2c3561d1f8f393c089036dade1
    tv-sonarr
    qbit-tag
    mytracker.com
    ...
    ```
>[!IMPORTANT]
> Some people have experienced problems using Docker where the mounted file would not update inside the container if it was modified on the host. This is a Docker configuration problem and can not be solved by cleanuperr.

#### **`CONTENTBLOCKER__IGNORE_PRIVATE`**
- Controls whether to ignore downloads from private trackers.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`CONTENTBLOCKER__DELETE_PRIVATE`**
- Controls whether to delete private downloads that have all files blocked from the download client.
- Has no effect if `CONTENTBLOCKER__IGNORE_PRIVATE` is `true`.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `CONTENTBLOCKER__DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account.

#

### Download Cleaner settings

#### **`TRIGGERS__DOWNLOADCLEANER`**
- Cron schedule for the download cleaner job.
- Type: String - [Quartz cron format](https://www.quartz-scheduler.org/documentation/quartz-2.3.0/tutorials/crontrigger.html).
- Default: `0 0 * * * ?` (every hour).
- Required: No.

> [!NOTE]
> - Maximum interval is 6 hours.

#### **`DOWNLOADCLEANER__ENABLED`**
- Enables or disables the download cleaner functionality.
- When enabled, automatically cleans up downloads that have been seeding for a certain amount of time.
- Type: Boolean.
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`DOWNLOADCLEANER__IGNORED_DOWNLOADS_PATH`**
- Local path to the file containing ignored downloads.
- If the contents of the file are changed, they will be reloaded on the next job run.
- Accepted values:
  -  torrent hash
  -  qBitTorrent tag or category
  -  Deluge label
  -  Transmission category (last directory from the save location)
  -  torrent tracker domain
- Each value needs to be on a new line.
- Type: String.
- Default: Empty.
- Required: No.
- Example: `/ignored.txt`.
- Example of file contents:
    ```
    fa800a7d7c443a2c3561d1f8f393c089036dade1
    tv-sonarr
    qbit-tag
    mytracker.com
    ...
    ```
>[!IMPORTANT]
> Some people have experienced problems using Docker where the mounted file would not update inside the container if it was modified on the host. This is a Docker configuration problem and can not be solved by cleanuperr.

#### **`DOWNLOADCLEANER__DELETE_PRIVATE`**
- Controls whether to delete private downloads.
- Type: Boolean.
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

> [!WARNING]
> Setting `DOWNLOADCLEANER__DELETE_PRIVATE=true` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account.

#### **`DOWNLOADCLEANER__CATEGORIES__0__NAME`**
- Name of the category to clean.
- Type: String.
- Default: Empty.
- Required: No.

> [!NOTE]
> The category name must match the category that was set in the *arr.
>
> For qBittorrent, the category name is the name of the download category.
>
> For Deluge, the category name is the name of the label.
>
> For Transmission, the category name is the last directory from the save location.

#### **`DOWNLOADCLEANER__CATEGORIES__0__MAX_RATIO`**
- Maximum ratio to reach before removing a download.
- Type: Decimal.
- Possible values: `-1` or greater (`-1` means no limit or disabled).
- Default: `-1`
- Required: No.

#### **`DOWNLOADCLEANER__CATEGORIES__0__MIN_SEED_TIME`**
- Minimum number of hours to seed before removing a download, if the ratio has been met.
- Used with `MAX_RATIO` to ensure a minimum seed time.
- Type: Decimal.
- Possible values: `0` or greater.
- Default: `0`
- Required: No.

#### **`DOWNLOADCLEANER__CATEGORIES__0__MAX_SEED_TIME`**
- Maximum number of hours to seed before removing a download.
- Type: Decimal.
- Possible values: `-1` or greater (`-1` means no limit or disabled).
- Default: `-1`
- Required: No.

> [!NOTE]
> A download is cleaned when any of (`MAX_RATIO` & `MIN_SEED_TIME`) or `MAX_SEED_TIME` is reached.

> [!NOTE]
> Multiple categories can be specified using this format, where `<NUMBER>` starts from 0:
> ```yaml
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__NAME
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__MAX_RATIO
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__MIN_SEED_TIME
> DOWNLOADCLEANER__CATEGORIES__<NUMBER>__MAX_SEED_TIME
> ```

#

### Download Client settings

#### **`DOWNLOAD_CLIENT`**
- Specifies which download client is used by *arrs.
- Type: String.
- Possible values: `none`, `qbittorrent`, `deluge`, `transmission`, `disabled`.
- Default: `none`
- Required: No.

> [!NOTE]
> Only one download client can be enabled at a time. If you have more than one download client, you should deploy multiple instances of cleanuperr.

> [!IMPORTANT]
> When the download client is set to `disabled`, the queue cleaner will be able to remove items that are failed to be imported even if there is no download client configured. This means that all downloads, including private ones, will be completely removed.
>
> Setting `DOWNLOAD_CLIENT=disabled` means you don't care about seeding, ratio, H&R and potentially losing your private tracker account.

#### **`QBITTORRENT__URL`**
- URL of the qBittorrent instance.
- Type: String.
- Default: `http://localhost:8080`.
- Required: No.

#### **`QBITTORRENT__URL_BASE`**
- Adds a prefix to the qBittorrent url, such as `[QBITTORRENT__URL]/[QBITTORRENT__URL_BASE]/api`.
- Type: String.
- Default: Empty.
- Required: No.

#### **`QBITTORRENT__USERNAME`**
- Username for qBittorrent authentication.
- Type: String.
- Default: Empty.
- Required: No.

#### **`QBITTORRENT__PASSWORD`**
- Password for qBittorrent authentication.
- Type: String.
- Default: Empty.
- Required: No.

#### **`DELUGE__URL`**
- URL of the Deluge instance.
- Type: String.
- Default: `http://localhost:8112`.
- Required: No.

#### **`DELUGE__URL_BASE`**
- Adds a prefix to the deluge json url, such as `[DELUGE__URL]/[DELUGE__URL_BASE]/json`.
- Type: String.
- Default: Empty.
- Required: No.

#### **`DELUGE__PASSWORD`**
- Password for Deluge authentication.
- Type: String.
- Default: Empty.
- Required: No.

#### **`TRANSMISSION__URL`**
- URL of the Transmission instance.
- Type: String.
- Default: `http://localhost:9091`.
- Required: No.

#### **`TRANSMISSION__URL_BASE`**
- Adds a prefix to the Transmission rpc url, such as `[TRANSMISSION__URL]/[TRANSMISSION__URL_BASE]/rpc`.
- Type: String.
- Default: `transmission`.
- Required: No.

#### **`TRANSMISSION__USERNAME`**
- Username for Transmission authentication.
- Type: String.
- Default: Empty.
- Required: No.

#### **`TRANSMISSION__PASSWORD`**
- Password for Transmission authentication.
- Type: String.
- Default: Empty.
- Required: No.

#

### Arr settings

> [!NOTE]
> Multiple instances can be specified for each *arr using this format, where `<NUMBER>` starts from 0:
> ```yaml
> <ARR>__INSTANCES__<NUMBER>__URL
> <ARR>__INSTANCES__<NUMBER>__APIKEY
> ```

#### **`SONARR__ENABLED`**
- Enables or disables Sonarr cleanup.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`SONARR__BLOCK__TYPE`**
- Determines how file blocking works for Sonarr.
- Type: String
- Possible values: `blacklist`, `whitelist`
- Default: `blacklist`
- Required: No.

#### **`SONARR__BLOCK__PATH`**
- Path to the blocklist file (local file or URL).
- Must be JSON compatible.
- Type: String
- Default: Empty.
- Required: No.

> [!NOTE]
> The blocklists support the following patterns:
> ```
> *example            // file name ends with "example"
> example*            // file name starts with "example"
> *example*           // file name has "example" in the name
> example             // file name is exactly the word "example"
> regex:<ANY_REGEX>   // regex that needs to be marked at the start of the line with "regex:"
> ```

> [!NOTE]
> [This blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist), [this permissive blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist_permissive) and [this whitelist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/whitelist) can be used for Sonarr and Radarr.

#### **`SONARR__SEARCHTYPE`**
- Determines what to search for after removing a queue item.
- Type: String
- Possible values: `Episode`, `Season`, `Series`
- Default: `Episode`
- Required: No.

#### **`SONARR__INSTANCES__0__URL`**
- URL of the Sonarr instance.
- Type: String
- Default: `http://localhost:8989`
- Required: No.

#### **`SONARR__INSTANCES__0__APIKEY`**
- API key for the Sonarr instance.
- Type: String
- Default: Empty.
- Required: No.

#### **`RADARR__ENABLED`**
- Enables or disables Radarr cleanup.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`RADARR__BLOCK__TYPE`**
- Determines how file blocking works for Radarr.
- Type: String
- Possible values: `blacklist`, `whitelist`
- Default: `blacklist`
- Required: No.

#### **`RADARR__BLOCK__PATH`**
- Path to the blocklist file (local file or URL).
- Must be JSON compatible.
- Type: String
- Default: Empty.
- Required: No.

> [!NOTE]
> The blocklists support the following patterns:
> ```
> *example            // file name ends with "example"
> example*            // file name starts with "example"
> *example*           // file name has "example" in the name
> example             // file name is exactly the word "example"
> regex:<ANY_REGEX>   // regex that needs to be marked at the start of the line with "regex:"
> ```

> [!NOTE]
> [This blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist), [this permissive blacklist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/blacklist_permissive) and [this whitelist](https://raw.githubusercontent.com/flmorg/cleanuperr/refs/heads/main/whitelist) can be used for Sonarr and Radarr.

#### **`RADARR__INSTANCES__0__URL`**
- URL of the Radarr instance.
- Type: String
- Default: `http://localhost:7878`
- Required: No.

#### **`RADARR__INSTANCES__0__APIKEY`**
- API key for the Radarr instance.
- Type: String
- Default: Empty.
- Required: No.

#### **`LIDARR__ENABLED`**
- Enables or disables Lidarr cleanup.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`LIDARR__BLOCK__TYPE`**
- Determines how file blocking works for Lidarr.
- Type: String
- Possible values: `blacklist`, `whitelist`
- Default: `blacklist`
- Required: No.

#### **`LIDARR__BLOCK__PATH`**
- Path to the blocklist file (local file or URL).
- Must be JSON compatible.
- Type: String
- Default: Empty.
- Required: No.

> [!NOTE]
> The blocklists support the following patterns:
> ```
> *example            // file name ends with "example"
> example*            // file name starts with "example"
> *example*           // file name has "example" in the name
> example             // file name is exactly the word "example"
> regex:<ANY_REGEX>   // regex that needs to be marked at the start of the line with "regex:"
> ```

#### **`LIDARR__INSTANCES__0__URL`**
- URL of the Lidarr instance.
- Type: String
- Default: `http://localhost:8686`
- Required: No.

#### **`LIDARR__INSTANCES__0__APIKEY`**
- API key for the Lidarr instance.
- Type: String
- Default: Empty.
- Required: No.

#

### Notification settings

#### **`NOTIFIARR__API_KEY`**
- Notifiarr API key for sending notifications.
- Requires Notifiarr's [`Passthrough`](https://notifiarr.wiki/en/Website/Integrations/Passthrough) integration to work.
- Type: String
- Default: Empty.
- Required: No.

#### **`NOTIFIARR__CHANNEL_ID`**
- Discord channel ID where notifications will be sent.
- Type: String
- Default: Empty.
- Required: No.

#### **`NOTIFIARR__ON_IMPORT_FAILED_STRIKE`**
- Controls whether to notify when an item receives a failed import strike.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`NOTIFIARR__ON_STALLED_STRIKE`**
- Controls whether to notify when an item receives a stalled download strike. This includes strikes for being stuck while downloading metadata.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`NOTIFIARR__ON_SLOW_STRIKE`**
- Controls whether to notify when an item receives a slow download strike. This includes strikes for having a low download speed or slow estimated finish time.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`NOTIFIARR__ON_QUEUE_ITEM_DELETED`**
- Controls whether to notify when a queue item is deleted.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#### **`NOTIFIARR__ON_DOWNLOAD_CLEANED`**
- Controls whether to notify when a download is cleaned.
- Type: Boolean
- Possible values: `true`, `false`
- Default: `false`
- Required: No.

#

### Advanced settings

#### **`HTTP_MAX_RETRIES`**
- The number of times to retry a failed HTTP call.
- Applies to calls to *arrs, download clients, and other services.
- Type: Integer
- Possible values: `0` or greater
- Default: `0`
- Required: No.

#### **`HTTP_TIMEOUT`**
- The number of seconds to wait before failing an HTTP call.
- Applies to calls to *arrs, download clients, and other services.
- Type: Integer
- Possible values: Greater than `0`.
- Default: `100`
- Required: No.