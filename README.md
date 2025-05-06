_Love this project? Give it a ⭐️ and let others know!_

# <img width="24px" src="./Logo/256.png" alt="cleanuperr"></img> Cleanuperr

[![Discord](https://img.shields.io/discord/1306721212587573389?color=7289DA&label=Discord&style=for-the-badge&logo=discord)](https://discord.gg/SCtMCgtsc4)

Cleanuperr is a tool for automating the cleanup of unwanted or blocked files in Sonarr, Radarr, and supported download clients like qBittorrent. It removes incomplete or blocked downloads, updates queues, and enforces blacklists or whitelists to manage file selection. After removing blocked content, Cleanuperr can also trigger a search to replace the deleted shows/movies.

Cleanuperr was created primarily to address malicious files, such as `*.lnk` or `*.zipx`, that were getting stuck in Sonarr/Radarr and required manual intervention. Some of the reddit posts that made Cleanuperr come to life can be found [here](https://www.reddit.com/r/sonarr/comments/1gqnx16/psa_sonarr_downloaded_a_virus/), [here](https://www.reddit.com/r/sonarr/comments/1gqwklr/sonar_downloaded_a_mkv_file_which_looked_like_a/), [here](https://www.reddit.com/r/sonarr/comments/1gpw2wa/downloaded_waiting_to_import/) and [here](https://www.reddit.com/r/sonarr/comments/1gpi344/downloads_not_importing_no_files_found/).

> [!IMPORTANT]
> **Features:**
> - Strike system to mark stalled or downloads stuck in metadata downloading.
> - Remove and block downloads that reached a maximum number of strikes.
> - Remove and block downloads that have a low download speed or high estimated completion time.
> - Remove downloads blocked by qBittorrent or by Cleanuperr's **content blocker**.
> - Trigger a search for downloads removed from the *arrs.
> - Clean up downloads that have been seeding for a certain amount of time.
> - Notify on strike or download removal.
> - Ignore certain torrent hashes, categories, tags or trackers from being processed by Cleanuperr.

Cleanuperr supports both qBittorrent's built-in exclusion features and its own blocklist-based system. Binaries for all platforms are provided, along with Docker images for easy deployment.

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

# Docs

Docs can be found [here](https://flmorg.github.io/cleanuperr/).

# <img width="24px" src="./Logo/256.png" alt="Cleanuperr"> Cleanuperr <svg width="14px" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 384 512"><!--!Font Awesome Free 6.7.2 by @fontawesome - https://fontawesome.com License - https://fontawesome.com/license/free Copyright 2025 Fonticons, Inc.--><path d="M376.6 84.5c11.3-13.6 9.5-33.8-4.1-45.1s-33.8-9.5-45.1 4.1L192 206 56.6 43.5C45.3 29.9 25.1 28.1 11.5 39.4S-3.9 70.9 7.4 84.5L150.3 256 7.4 427.5c-11.3 13.6-9.5 33.8 4.1 45.1s33.8 9.5 45.1-4.1L192 306 327.4 468.5c11.3 13.6 31.5 15.4 45.1 4.1s15.4-31.5 4.1-45.1L233.7 256 376.6 84.5z"/></svg> Huntarr <img width="24px" src="https://github.com/plexguide/Huntarr.io/blob/main/frontend/static/logo/256.png?raw=true" alt Huntarr></img>

Think of **Cleanuperr** as the janitor of your server; it keeps your download queue spotless, removes clutter, and blocks malicious files. Now imagine combining that with **Huntarr**, the compulsive librarian who finds missing and upgradable media to complete your collection

While **Huntarr** fills in the blanks and improves what you already have, **Cleanuperr** makes sure that only clean downloads get through. If you're aiming for a reliable and self-sufficient setup, **Cleanuperr** and **Huntarr** will take your automated media stack to another level.

<span style="font-size:24px"> ➡️ [**Huntarr**](https://github.com/plexguide/Huntarr.io) ![Huntarr](https://img.shields.io/github/stars/plexguide/Huntarr.io?style=social)</span>

# Credits
Special thanks for inspiration go to:
- [ThijmenGThN/swaparr](https://github.com/ThijmenGThN/swaparr)
- [ManiMatter/decluttarr](https://github.com/ManiMatter/decluttarr)
- [PaeyMoopy/sonarr-radarr-queue-cleaner](https://github.com/PaeyMoopy/sonarr-radarr-queue-cleaner)
- [Sonarr](https://github.com/Sonarr/Sonarr) & [Radarr](https://github.com/Radarr/Radarr)

# Buy me a coffee
If I made your life just a tiny bit easier, consider buying me a coffee!

<a href="https://buymeacoffee.com/flaminel" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>
