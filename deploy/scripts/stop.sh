#!/bin/bash
# Stops QueryPlus as a whole: the two Jobs module timers, then the Api service. Does not disable
# either (they'll come back on the next boot/start.sh if they were enabled) - this is a plain
# stop, not an uninstall. Installed to /opt/queryplus/bin/stop.sh by `make install`; source lives
# at deploy/scripts/stop.sh in the repo.
set -euo pipefail

if [ "$(id -u)" != "0" ]; then
    echo "error: must run as root (try: sudo $0)" >&2
    exit 1
fi

echo "Stopping queryplus-scheduler-sync.timer and queryplus-scheduler-sync-watchdog.timer..."
systemctl stop queryplus-scheduler-sync-watchdog.timer
systemctl stop queryplus-scheduler-sync.timer

echo "Stopping queryplus-api.service..."
systemctl stop queryplus-api.service

echo
echo "Stopped. In-flight jobs launched via systemd-run --scope are independent transient units and"
echo "are NOT killed by this - they keep running until they finish on their own. Use"
echo "'systemctl list-units queryplus-job-*' to see any still active."
