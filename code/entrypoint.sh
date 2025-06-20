#!/bin/bash
set -e

# Create group if it doesn't exist
if ! getent group "$PGID" > /dev/null 2>&1; then
    echo "Creating group with GID $PGID"
    groupadd -g "$PGID" appgroup
fi

# Create user if it doesn't exist
if ! getent passwd "$PUID" > /dev/null 2>&1; then
    echo "Creating user with UID $PUID"
    useradd -u "$PUID" -g "$PGID" -s /bin/bash -M appuser
fi

# Set umask
umask "$UMASK"

# Change ownership of app directory if not running as root
if [ "$PUID" != "0" ] || [ "$PGID" != "0" ]; then
    mkdir -p /config
    chown -R "$PUID:$PGID" /app
    chown -R "$PUID:$PGID" /config
fi

# Execute the main command as the specified user
if [ "$PUID" = "0" ] && [ "$PGID" = "0" ]; then
    # Running as root, no need for gosu
    exec "$@"
else
    # Use gosu to drop privileges
    exec gosu "$PUID:$PGID" "$@"
fi