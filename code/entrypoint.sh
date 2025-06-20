#!/bin/bash
set -e

# Check if user and group exist, fall back to root if not
EFFECTIVE_PUID="$PUID"
EFFECTIVE_PGID="$PGID"

if ! getent passwd "$PUID" > /dev/null 2>&1; then
    echo "Warning: User ID $PUID not found, falling back to root"
    EFFECTIVE_PUID=0
fi

if ! getent group "$PGID" > /dev/null 2>&1; then
    echo "Warning: Group ID $PGID not found, falling back to root"
    EFFECTIVE_PGID=0
fi

# Set umask
umask "$UMASK"

# Change ownership of app directory if not running as root
if [ "$EFFECTIVE_PUID" != "0" ] || [ "$EFFECTIVE_PGID" != "0" ]; then
    mkdir -p /config
    chown -R "$EFFECTIVE_PUID:$EFFECTIVE_PGID" /app
    chown -R "$EFFECTIVE_PUID:$EFFECTIVE_PGID" /config
fi

# Execute the main command as the specified user
if [ "$EFFECTIVE_PUID" = "0" ] && [ "$EFFECTIVE_PGID" = "0" ]; then
    # Running as root, no need for gosu
    exec "$@"
else
    # Use gosu to drop privileges
    exec gosu "$EFFECTIVE_PUID:$EFFECTIVE_PGID" "$@"
fi