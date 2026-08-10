#!/usr/bin/env bash
# Interactive account selection for the bare-metal systemd deployment (see the repo root
# Makefile's `accounts-wizard`/`install` targets). Lets the operator pick DEPLOY_USER, API_USER,
# and API_GROUP, creates them (useradd/groupadd) if they don't exist yet, and persists the choice
# to queryplus.local.mk so the rest of `make install` (run as yourself - it escalates to root via
# sudo internally, which is how this script itself ends up running as root) - and every later
# `make` invocation - uses the same values without re-prompting or retyping them.
#
# Why a wizard at all: `check-accounts` previously just told the operator the exact useradd/
# groupadd commands to run and expected them to do it manually, exit, and re-run `make install` -
# one more manual step between "ran the installer" and "have a working app."
#
# Why this runs via a re-exec'd sub-make (see the `install` target), not inline in one recipe:
# GNU Make resolves `-include queryplus.local.mk` once, at parse time, before any target's recipe
# runs. Writing new DEPLOY_USER/API_USER/API_GROUP values into that file from within a recipe
# does NOT change what $(DEPLOY_USER) etc. resolve to for the REST of that same `make` invocation
# - only a fresh `make` process re-reads the file. So `install`'s recipe runs this wizard in one
# sub-make invocation, then a SEPARATE sub-make invocation (which re-parses queryplus.local.mk
# with the fresh values) for the actual install steps.
#
# Idempotent: if DEPLOY_USER/API_USER/API_GROUP already resolve to real, existing OS accounts,
# this exits immediately without prompting. Set FORCE_RECONFIGURE=1 to always run interactively.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LOCAL_MK="$REPO_ROOT/queryplus.local.mk"
BACKTITLE="QueryPlus Setup"

DEPLOY_USER="${DEPLOY_USER:-queryplus-deploy}"
API_USER="${API_USER:-queryplus-api}"
API_GROUP="${API_GROUP:-$API_USER}"

account_exists() { id -u "$1" >/dev/null 2>&1; }
group_exists() { getent group "$1" >/dev/null 2>&1; }

all_ready() {
    account_exists "$DEPLOY_USER" && account_exists "$API_USER" && group_exists "$API_GROUP"
}

if all_ready && [ "${FORCE_RECONFIGURE:-0}" != "1" ]; then
    echo "Accounts already resolved and exist: DEPLOY_USER=$DEPLOY_USER, API_USER=$API_USER,"
    echo "API_GROUP=$API_GROUP - skipping. Run 'sudo make configure-accounts' to change them."
    exit 0
fi

if [ ! -t 0 ]; then
    echo "error: account selection needs a terminal (stdin is not a tty)." >&2
    echo "Either run 'make install' interactively (as yourself, not root), or pre-create the" >&2
    echo "accounts and set DEPLOY_USER/API_USER/API_GROUP via queryplus.local.mk or the command" >&2
    echo "line first." >&2
    exit 1
fi

# dialog is preferred over whiptail - see configure.sh's copy of this same block for the full
# rationale (confirmed by actually rendering both: dialog visibly distinguishes the focused
# button, whiptail's newt renders focused/unfocused byte-for-byte identically; dialog also enables
# real xterm mouse tracking automatically, confirmed via the `\e[?1006;1000h`/`l` sequences it
# emits around each dialog).
TUI=""
if command -v dialog >/dev/null 2>&1; then
    TUI=dialog
elif command -v whiptail >/dev/null 2>&1; then
    TUI=whiptail
    echo "Using whiptail (dialog not found) - button focus won't be visually indicated and there's" >&2
    echo "no mouse support. Install dialog for both: sudo apt-get install dialog" >&2
elif command -v apt-get >/dev/null 2>&1; then
    echo "Neither dialog nor whiptail is installed - installing dialog..."
    apt-get install -y dialog
    TUI=dialog
fi
if [ -z "$TUI" ] || ! command -v "$TUI" >/dev/null 2>&1; then
    echo "error: neither dialog nor whiptail is available, and dialog could not be auto-installed" >&2
    echo "(no apt-get). Install one of them first, e.g.: sudo apt-get install dialog" >&2
    exit 1
fi

# Fixed color scheme (classic DOS/Turbo-Pascal-blue), independent of whatever theme the
# operator's own terminal emulator applies - see configure.sh's copy of this same block for the
# full rationale and how each key name/color value was verified against the real binaries.
DIALOGRC_IS_TEMP=0
setup_theme() {
    if [ "$TUI" = "dialog" ]; then
        [ -n "${DIALOGRC:-}" ] && return
        local rc
        rc=$(mktemp)
        dialog --create-rc "$rc" 2>/dev/null
        sed -i \
            -e 's/^screen_color = .*/screen_color = (WHITE,BLUE,ON)/' \
            -e 's/^shadow_color = .*/shadow_color = (BLACK,BLACK,ON)/' \
            -e 's/^dialog_color = .*/dialog_color = (WHITE,BLUE,ON)/' \
            -e 's/^title_color = .*/title_color = (YELLOW,BLUE,ON)/' \
            -e 's/^border_color = .*/border_color = (CYAN,BLUE,ON)/' \
            -e 's/^border2_color = .*/border2_color = (CYAN,BLUE,ON)/' \
            -e 's/^button_active_color = .*/button_active_color = (BLACK,CYAN,ON)/' \
            -e 's/^button_inactive_color = .*/button_inactive_color = (WHITE,BLUE,OFF)/' \
            -e 's/^button_key_active_color = .*/button_key_active_color = (RED,CYAN,ON)/' \
            -e 's/^button_key_inactive_color = .*/button_key_inactive_color = (RED,BLUE,OFF)/' \
            -e 's/^button_label_active_color = .*/button_label_active_color = (BLACK,CYAN,ON)/' \
            -e 's/^button_label_inactive_color = .*/button_label_inactive_color = (WHITE,BLUE,OFF)/' \
            -e 's/^inputbox_color = .*/inputbox_color = (WHITE,BLUE,ON)/' \
            -e 's/^inputbox_border_color = .*/inputbox_border_color = (CYAN,BLUE,ON)/' \
            -e 's/^inputbox_border2_color = .*/inputbox_border2_color = (CYAN,BLUE,ON)/' \
            -e 's/^menubox_color = .*/menubox_color = (WHITE,BLUE,ON)/' \
            -e 's/^menubox_border_color = .*/menubox_border_color = (CYAN,BLUE,ON)/' \
            -e 's/^menubox_border2_color = .*/menubox_border2_color = (CYAN,BLUE,ON)/' \
            -e 's/^item_color = .*/item_color = (WHITE,BLUE,ON)/' \
            -e 's/^item_selected_color = .*/item_selected_color = (BLACK,CYAN,ON)/' \
            -e 's/^tag_color = .*/tag_color = (YELLOW,BLUE,ON)/' \
            -e 's/^tag_selected_color = .*/tag_selected_color = (BLACK,CYAN,ON)/' \
            -e 's/^form_active_text_color = .*/form_active_text_color = (BLACK,CYAN,ON)/' \
            -e 's/^form_text_color = .*/form_text_color = (WHITE,BLUE,ON)/' \
            "$rc"
        export DIALOGRC="$rc"
        DIALOGRC_IS_TEMP=1
    else
        : "${NEWT_COLORS:="root=white,blue
border=cyan,blue
window=white,blue
shadow=black,black
title=yellow,blue
button=black,cyan
actbutton=white,red
compactbutton=black,cyan
checkbox=white,blue
actcheckbox=black,cyan
entry=black,cyan
disentry=white,blue
label=white,blue
listbox=white,blue
actlistbox=black,cyan
sellistbox=black,cyan
actsellistbox=black,cyan
textbox=white,blue
acttextbox=black,cyan
helpline=white,black
roottext=yellow,blue
emptyscale=,cyan
fullscale=,blue"}"
        export NEWT_COLORS
    fi
}
setup_theme

# The above only picks WHICH of the terminal's 16 ANSI color slots each UI element uses - it can't
# control what RGB value those slots actually render as (that's the terminal emulator's active
# theme). See configure.sh's copy of this same block for the full rationale; overrides the slots
# themselves via OSC 4 for the wizard's lifetime, then restores them via OSC 104 on exit.
apply_dos_palette() {
    [ -n "${QUERYPLUS_NO_THEME:-}" ] && return
    [ -t 1 ] || return
    printf '\033]4;0;rgb:00/00/00;1;rgb:CC/22/22;2;rgb:22/AA/22;3;rgb:FF/CC/00;4;rgb:00/00/AA;5;rgb:AA/00/AA;6;rgb:00/CC/CC;7;rgb:FF/FF/FF;8;rgb:55/55/55;9;rgb:FF/55/55;10;rgb:55/FF/55;11;rgb:FF/FF/66;12;rgb:55/55/FF;13;rgb:FF/55/FF;14;rgb:55/FF/FF;15;rgb:FF/FF/FF\033\\'
}

restore_palette() {
    [ -n "${QUERYPLUS_NO_THEME:-}" ] && return
    [ -t 1 ] || return
    printf '\033]104\033\\'
}

apply_dos_palette

box() {
    "$TUI" --backtitle "$BACKTITLE" "$@" 3>&1 1>&2 2>&3
}

confirm() { # $1 = text, $2 = height, $3 = width
    "$TUI" --backtitle "$BACKTITLE" --yesno "$1" "${2:-10}" "${3:-70}"
}

# See configure.sh's copy of this same pattern for why a persistent flag-checking trap replaced
# the old "arm, then `trap - EXIT` right before success" approach - restore_palette must run on
# every exit path, not just the cancelled one.
SUCCEEDED=0
cleanup() {
    restore_palette
    [ "$DIALOGRC_IS_TEMP" = "1" ] && rm -f "$DIALOGRC"
    if [ "$SUCCEEDED" != "1" ]; then
        echo
        echo "Setup cancelled - no accounts were created or changed." >&2
    fi
}
trap cleanup EXIT

box --title "Accounts" --msgbox \
    "QueryPlus runs as least-privilege OS accounts, deliberately kept separate from root and from each other - so that compromising one doesn't hand over the others:\n\n- Deploy account: the ONLY account allowed to write into /opt/queryplus/scripts (publishing Jobs module scripts). Not used to run anything.\n- Api account: what queryplus-api.service actually runs as. Owns nothing outside its own web app directory.\n- Api group: gets READ-ONLY access to job run logs, so the Api can display them in the UI, without needing to run jobs itself.\n\nSee docs/local/jobs-module-deployment.md 'Account model' for the full rationale. Any account entered below that doesn't exist yet can be created right here, with a real password disabled, non-interactive shell (useradd --system --no-create-home --shell /usr/sbin/nologin)." \
    24 78 >/dev/null

DEPLOY_USER=$(box --title "Accounts" --inputbox \
    "Deploy account name. This is the ONLY account allowed to publish/replace job scripts under /opt/queryplus/scripts - keep it separate from the Api account so a compromised Api process can't rewrite scripts that are pending approval or already approved:" \
    11 78 "$DEPLOY_USER")
API_USER=$(box --title "Accounts" --inputbox \
    "Api service account name. This is who queryplus-api.service actually runs as (User= in the systemd unit) - give it no login shell and no privileges beyond its own web app directory:" \
    10 78 "$API_USER")
API_GROUP=$(box --title "Accounts" --inputbox \
    "Api group name. Job run logs (stdout/stderr, written by whichever OS account each job runs as) are made group-readable by this group, so the Api process can display them in the UI without running jobs itself. Usually the same name as the Api account above:" \
    12 78 "$API_USER")

ensure_account() { # $1 = username
    local name="$1"
    if account_exists "$name"; then
        return 0
    fi
    if confirm "OS account '$name' does not exist yet. Create it now?\n\nsudo useradd --system --no-create-home --shell /usr/sbin/nologin $name" 11 76; then
        useradd --system --no-create-home --shell /usr/sbin/nologin "$name"
    else
        echo "error: '$name' does not exist and was not created." >&2
        exit 1
    fi
}

ensure_group() { # $1 = group name, $2 = username to add to it if the group is newly created
    local name="$1" member="$2"
    if group_exists "$name"; then
        return 0
    fi
    if confirm "Group '$name' does not exist yet. Create it now (and add '$member' to it)?\n\nsudo groupadd --system $name\nsudo usermod -aG $name $member" 13 76; then
        groupadd --system "$name"
        usermod -aG "$name" "$member"
    else
        echo "error: group '$name' does not exist and was not created." >&2
        exit 1
    fi
}

ensure_account "$DEPLOY_USER"
ensure_account "$API_USER"
ensure_group "$API_GROUP" "$API_USER"

upsert_mk() { # $@ = "KEY = value" lines to set/replace; other existing lines are preserved as-is
    local tmp
    tmp=$(mktemp)
    if [ -f "$LOCAL_MK" ]; then
        while IFS= read -r line || [ -n "$line" ]; do
            local key drop=0
            key=$(printf '%s' "$line" | sed -E 's/^([A-Za-z0-9_]+)[[:space:]]*=.*/\1/')
            for kv in "$@"; do
                local kvkey="${kv%%=*}"
                kvkey="${kvkey% }"
                [ "$key" = "$kvkey" ] && drop=1 && break
            done
            [ "$drop" -eq 1 ] || printf '%s\n' "$line" >>"$tmp"
        done <"$LOCAL_MK"
    else
        {
            printf '# Generated/updated by deploy/scripts/accounts-wizard.sh (via "make install").\n'
            printf '# See queryplus.local.mk.example for the full list of variables this file can override.\n'
        } >>"$tmp"
    fi
    for kv in "$@"; do
        printf '%s\n' "$kv" >>"$tmp"
    done
    mv "$tmp" "$LOCAL_MK"
    chmod 0644 "$LOCAL_MK"
    # This script runs as root (make install escalates via sudo internally - see the Makefile's
    # header comment); without this, queryplus.local.mk would end up root-owned, mode 0600 from
    # mktemp - unreadable by the SAME `make install` run's own earlier `make publish` step (which
    # also -includes this file and runs as the normal user, before the sudo escalation), and by
    # every later unprivileged `make` invocation too.
    if [ -n "${SUDO_USER:-}" ]; then
        chown "$SUDO_USER":"$(id -gn "$SUDO_USER")" "$LOCAL_MK" 2>/dev/null || true
    fi
}

upsert_mk "DEPLOY_USER = $DEPLOY_USER" "API_USER = $API_USER" "API_GROUP = $API_GROUP"

SUCCEEDED=1
echo "Accounts ready: DEPLOY_USER=$DEPLOY_USER, API_USER=$API_USER, API_GROUP=$API_GROUP"
echo "Persisted to $LOCAL_MK."
