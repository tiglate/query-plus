#!/bin/bash
# Starts QueryPlus as a whole: the Api service, and the two Jobs module timers. Does not change
# what's enabled at boot (that's set once by `make install`/`enable-api`/`enable-timers`) - this
# just brings everything up right now. Installed to /opt/queryplus/bin/start.sh by `make install`
# (see install-scripts in the repo Makefile); deploy/scripts/start.sh in the repo is the source.
set -euo pipefail

if [ "$(id -u)" != "0" ]; then
    echo "error: must run as root (try: sudo $0)" >&2
    exit 1
fi

echo "Starting queryplus-api.service..."
systemctl start queryplus-api.service

echo "Starting queryplus-scheduler-sync.timer and queryplus-scheduler-sync-watchdog.timer..."
systemctl start queryplus-scheduler-sync.timer
systemctl start queryplus-scheduler-sync-watchdog.timer

echo
systemctl --no-pager status queryplus-api.service queryplus-scheduler-sync.timer \
    queryplus-scheduler-sync-watchdog.timer || true
