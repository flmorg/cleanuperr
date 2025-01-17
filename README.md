_Love this project? Give it a ⭐️ and let others know!_

# <img width="24px" src="./Logo/256.png" alt="cleanuperr"></img> cleanuperr

[![Discord](https://img.shields.io/discord/1306721212587573389?color=7289DA&label=Discord&style=for-the-badge&logo=discord)](https://discord.gg/sWggpnmGNY)

cleanuperr is a tool for automating the cleanup of unwanted or blocked files in Sonarr, Radarr, and supported download clients like qBittorrent. It removes incomplete or blocked downloads, updates queues, and enforces blacklists or whitelists to manage file selection. After removing blocked content, cleanuperr can also trigger a search to replace the deleted shows/movies.

cleanuperr was created primarily to address malicious files, such as `*.lnk` or `*.zipx`, that were getting stuck in Sonarr/Radarr and required manual intervention. Some of the reddit posts that made cleanuperr come to life can be found [here](https://www.reddit.com/r/sonarr/comments/1gqnx16/psa_sonarr_downloaded_a_virus/), [here](https://www.reddit.com/r/sonarr/comments/1gqwklr/sonar_downloaded_a_mkv_file_which_looked_like_a/), [here](https://www.reddit.com/r/sonarr/comments/1gpw2wa/downloaded_waiting_to_import/) and [here](https://www.reddit.com/r/sonarr/comments/1gpi344/downloads_not_importing_no_files_found/).

> [!IMPORTANT]
> **Features:**
> - Strike system to mark stalled or downloads stuck in metadata downloading.
> - Remove and block downloads that reached a maximum number of strikes.
> - Remove downloads blocked by qBittorrent or by cleanuperr's **content blocker**.
> - Trigger a search for downloads removed from the *arrs.
> - Clean up downloads that have been seeding for a certain amount of time.
> - Notify on strike or download removal.
> - Ignore certain torrent hashes, categories, tags or trackers from processing.

cleanuperr supports both qBittorrent's built-in exclusion features and its own blocklist-based system. Binaries for all platforms are provided, along with Docker images for easy deployment.

> [!WARNING]
> This tool is actively developed and still a work in progress, so using the `latest` Docker tag may result in breaking changes. Join the Discord server if you want to reach out to me quickly (or just stay updated on new releases) so we can squash those pesky bugs together:
>
> https://discord.gg/sWggpnmGNY

## Table of contents:
- [Naming choice](#naming-choice)
- [Quick Start](#quick-start)
- [How it works](#how-it-works)
  - [Content blocker](#1-content-blocker-will)
  - [Queue cleaner](#2-queue-cleaner-will)
  - [Download cleaner](#3-download-cleaner-will)
- [Setup](#setup-examples)
- [Usage](#usage)
  - [Docker](#docker)
    - [Environment Variables](#environment-variables)
    - [Docker Compose](#docker-compose-example)
  - [Windows](#windows)
  - [Linux](#linux)
  - [MacOS](#macos)
  - [FreeBSD](#freebsd)
- [Credits](#credits)

## Naming choice

I've had people asking why it's `cleanuperr` and not `cleanuparr` and that I should change it. This name was intentional.

I've seen a few discussions on this type of naming and I've decided that I didn't deserve the `arr` moniker since `cleanuperr` is not a fork of `NZB.Drone` and it does not have any affiliation with the arrs. I still wanted to keep the naming style close enough though, to suggest a correlation between them. 

## Quick Start

> [!NOTE]
>
> 1. **Docker (Recommended)**  
> Pull the Docker image from `ghcr.io/flmorg/cleanuperr:latest`.
>
> 2. **Unraid (for Unraid users)**  
> Use the Unraid Community App.
>
> 3. **Manual Installation (if you're not using Docker)**  
> Go to [Windows](#windows), [Linux](#linux) or [MacOS](#macos).

> [!TIP]
> Refer to the [Environment variables](#environment-variables) section for detailed configuration instructions and the [Setup examples](#setup-examples) section for an in-depth explanation of the cleanup process.


> [!IMPORTANT]
> Only the **latest versions** of the following apps are supported, or earlier versions that have the same API as the latest version:
> - qBittorrent
> - Deluge
> - Transmission
> - Sonarr
> - Radarr
> - Lidarr

# How it works

#### 1. **Content blocker** will:
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
#### 2. **Queue cleaner** will:
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
#### 3. **Download cleaner** will:
   - Run every hour (or configured cron).
   - Automatically clean up downloads that have been seeding for a certain amount of time.

# Setup examples

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
2. Configure and enable either a **blacklist** or a **whitelist** as described in the [Arr variables](variables.md#Arr-settings) section.
3. Once configured, cleanuperr will perform the following tasks:
   - Execute the **content blocker** job, as explained in the [How it works](#how-it-works) section.
   - Execute the **queue cleaner** job, as explained in the [How it works](#how-it-works) section.

## Using cleanuperr just for failed *arr imports (works for Usenet users as well)

1. Set `QUEUECLEANER__ENABLED` to `true`.
2. Set `QUEUECLEANER__IMPORT_FAILED_MAX_STRIKES` to a desired value.
3. Optionally set failed import message patterns to ignore using `QUEUECLEANER__IMPORT_FAILED_IGNORE_PATTERNS__<NUMBER>`.
4. Set `DOWNLOAD_CLIENT` to `none`.

> [!WARNING]
> When `DOWNLOAD_CLIENT=none`, no other action involving a download client would work (e.g. content blocking, removing stalled downloads, excluding private trackers).

## Usage

### <img src="https://raw.githubusercontent.com/FortAwesome/Font-Awesome/6.x/svgs/brands/docker.svg" height="20" style="vertical-align: middle;"> <span style="vertical-align: middle;">Docker</span>


### **Environment variables**

**Jump to:**
- [General settings](variables.md#general-settings)
- [Queue Cleaner settings](variables.md#queue-cleaner-settings)
- [Content Blocker settings](variables.md#content-blocker-settings)
- [Download Cleaner settings](variables.md#download-cleaner-settings)
- [Download Client settings](variables.md#download-client-settings)
- [Arr settings](variables.md#arr-settings)
- [Notification settings](variables.md#notification-settings)
- [Advanced settings](variables.md#advanced-settings)

### Docker compose example

> [!NOTE]
> 
> This example contains all settings and should be modified to fit your needs.

```
version: "3.3"
services:
  cleanuperr:
    image: ghcr.io/flmorg/cleanuperr:latest
    restart: unless-stopped
    volumes:
      - ./cleanuperr/logs:/var/logs
      - ./cleanuperr/ignored.txt:/ignored.txt
    environment:
      - TZ=America/New_York
      - DRY_RUN=false

      - LOGGING__LOGLEVEL=Information
      - LOGGING__FILE__ENABLED=false
      - LOGGING__FILE__PATH=/var/logs/
      - LOGGING__ENHANCED=true

      - TRIGGERS__QUEUECLEANER=0 0/5 * * * ?
      - TRIGGERS__CONTENTBLOCKER=0 0/5 * * * ?
      - TRIGGERS__DOWNLOADCLEANER=0 0 * * * ?

      - QUEUECLEANER__ENABLED=true
      - QUEUECLEANER__IGNORED_DOWNLOADS_PATH=/ignored.txt
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
      - CONTENTBLOCKER__IGNORED_DOWNLOADS_PATH=/ignored.txt
      - CONTENTBLOCKER__IGNORE_PRIVATE=false
      - CONTENTBLOCKER__DELETE_PRIVATE=false

      - DOWNLOADCLEANER__ENABLED=true
      - DOWNLOADCLEANER__IGNORED_DOWNLOADS_PATH=/ignored.txt
      - DOWNLOADCLEANER__DELETE_PRIVATE=false
      - DOWNLOADCLEANER__CATEGORIES__0__NAME=tv-sonarr
      - DOWNLOADCLEANER__CATEGORIES__0__MAX_RATIO=-1
      - DOWNLOADCLEANER__CATEGORIES__0__MIN_SEED_TIME=0
      - DOWNLOADCLEANER__CATEGORIES__0__MAX_SEED_TIME=240
      - DOWNLOADCLEANER__CATEGORIES__1__NAME=radarr
      - DOWNLOADCLEANER__CATEGORIES__1__MAX_RATIO=-1
      - DOWNLOADCLEANER__CATEGORIES__1__MIN_SEED_TIME=0
      - DOWNLOADCLEANER__CATEGORIES__1__MAX_SEED_TIME=240

      - DOWNLOAD_CLIENT=none
      # OR
      # - DOWNLOAD_CLIENT=qBittorrent
      # - QBITTORRENT__URL=http://localhost:8080
      # - QBITTORRENT__URL_BASE=myCustomPath
      # - QBITTORRENT__USERNAME=user
      # - QBITTORRENT__PASSWORD=pass
      # OR
      # - DOWNLOAD_CLIENT=deluge
      # - DELUGE__URL_BASE=myCustomPath
      # - DELUGE__URL=http://localhost:8112
      # - DELUGE__PASSWORD=testing
      # OR
      # - DOWNLOAD_CLIENT=transmission
      # - TRANSMISSION__URL=http://localhost:9091
      # - TRANSMISSION__URL_BASE=myCustomPath
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

      - NOTIFIARR__ON_IMPORT_FAILED_STRIKE=true
      - NOTIFIARR__ON_STALLED_STRIKE=true
      - NOTIFIARR__ON_QUEUE_ITEM_DELETED=true
      - NOTIFIARR__ON_DOWNLOAD_CLEANED=true
      - NOTIFIARR__API_KEY=notifiarr_secret
      - NOTIFIARR__CHANNEL_ID=discord_channel_id
```

### <img src="https://raw.githubusercontent.com/FortAwesome/Font-Awesome/6.x/svgs/brands/windows.svg" height="20" style="vertical-align: middle;"> <span style="vertical-align: middle;">Windows</span>

1. Download the zip file from [releases](https://github.com/flmorg/cleanuperr/releases).
2. Extract the zip file into `C:\example\directory`.
3. Edit **appsettings.json**. The paths from this json file correspond with the docker env vars, as described [here](#environment-variables).
4. Execute `cleanuperr.exe`.

> [!TIP]
> ### Run as a Windows Service
> Check out this stackoverflow answer on how to do it: https://stackoverflow.com/a/15719678

### <img src="https://raw.githubusercontent.com/FortAwesome/Font-Awesome/6.x/svgs/brands/linux.svg" height="20" style="vertical-align: middle;"> <span style="vertical-align: middle;">Linux</span>

1. Download the zip file from [releases](https://github.com/flmorg/cleanuperr/releases).
2. Extract the zip file into `/example/directory`.
3. Edit **appsettings.json**. The paths from this json file correspond with the docker env vars, as described [here](#environment-variables).
4. Open a terminal and execute these commands:
    ```
    cd /example/directory
    chmod +x cleanuperr
    ./cleanuperr
    ```

### <img src="https://raw.githubusercontent.com/FortAwesome/Font-Awesome/6.x/svgs/brands/apple.svg" height="20" style="vertical-align: middle;"> <span style="vertical-align: middle;">MacOS</span>

1. Download the zip file from [releases](https://github.com/flmorg/cleanuperr/releases).
2. Extract the zip file into `/example/directory`.
3. Edit **appsettings.json**. The paths from this json file correspond with the docker env vars, as described [here](#environment-variables).
4. Open a terminal and execute these commands:
    ```
    cd /example/directory
    chmod +x cleanuperr
    ./cleanuperr
    ```

> [!IMPORTANT]
> Some people have experienced problems when trying to execute cleanuperr on MacOS because the system actively blocked the file for not being signed.
> As per [this](), you may need to also execute this command:
> ```
> codesign --sign - --force --preserve-metadata=entitlements,requirements,flags,runtime /example/directory/cleanuperr
> ```

### <img src="https://raw.githubusercontent.com/FortAwesome/Font-Awesome/6.x/svgs/brands/freebsd.svg" height="20" style="vertical-align: middle;"> <span style="vertical-align: middle;">FreeBSD</span>

1. Installation:
    ```
    # install dependencies
    pkg install -y git icu libinotify libunwind wget

    # set up the dotnet SDK
    cd ~
    wget -q https://github.com/Thefrank/dotnet-freebsd-crossbuild/releases/download/v9.0.104-amd64-freebsd-14/dotnet-sdk-9.0.104-freebsd-x64.tar.gz
    export DOTNET_ROOT=$(pwd)/.dotnet
    mkdir -p "$DOTNET_ROOT" && tar zxf dotnet-sdk-9.0.104-freebsd-x64.tar.gz -C "$DOTNET_ROOT"
    export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools

    # download NuGet dependencies
    mkdir -p /tmp/nuget
    wget -q -P /tmp/nuget/ https://github.com/Thefrank/dotnet-freebsd-crossbuild/releases/download/v9.0.104-amd64-freebsd-14/Microsoft.AspNetCore.App.Runtime.freebsd-x64.9.0.3.nupkg
    wget -q -P /tmp/nuget/ https://github.com/Thefrank/dotnet-freebsd-crossbuild/releases/download/v9.0.104-amd64-freebsd-14/Microsoft.NETCore.App.Host.freebsd-x64.9.0.3.nupkg
    wget -q -P /tmp/nuget/ https://github.com/Thefrank/dotnet-freebsd-crossbuild/releases/download/v9.0.104-amd64-freebsd-14/Microsoft.NETCore.App.Runtime.freebsd-x64.9.0.3.nupkg

    # add NuGet source
    dotnet nuget add source /tmp/nuget --name tmp

    # add GitHub NuGet source
    # a PAT (Personal Access Token) can be generated here https://github.com/settings/tokens
    dotnet nuget add source --username <YOUR_USERNAME> --password <YOUR_PERSONAL_ACCESS_TOKEN> --store-password-in-clear-text --name flmorg https://nuget.pkg.github.com/flmorg/index.json
    ```
2. Building:
    ```
    # clone the project
    git clone https://github.com/flmorg/cleanuperr.git
    cd cleanuperr

    # build and publish the app
    dotnet publish code/Executable/Executable.csproj -c Release --self-contained -o artifacts /p:PublishSingleFile=true

    # move the files to permanent destination
    mv artifacts/cleanuperr /example/directory/
    mv artifacts/appsettings.json /example/directory/
    ```
3. Edit **appsettings.json**. The paths from this json file correspond with the docker env vars, as described [here](#environment-variables).
4. Run the app:
    ```
    cd /example/directory
    chmod +x cleanuperr
    ./cleanuperr
    ```

# Credits
Special thanks for inspiration go to:
- [ThijmenGThN/swaparr](https://github.com/ThijmenGThN/swaparr)
- [ManiMatter/decluttarr](https://github.com/ManiMatter/decluttarr)
- [PaeyMoopy/sonarr-radarr-queue-cleaner](https://github.com/PaeyMoopy/sonarr-radarr-queue-cleaner)
- [Sonarr](https://github.com/Sonarr/Sonarr) & [Radarr](https://github.com/Radarr/Radarr)

# Buy me a coffee
If I made your life just a tiny bit easier, consider buying me a coffee!

<a href="https://buymeacoffee.com/flaminel" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>
