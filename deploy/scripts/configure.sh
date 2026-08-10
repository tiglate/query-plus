#!/usr/bin/env bash
# Interactive first-run setup for the bare-metal systemd deployment (see the repo root Makefile's
# `configure`/`ensure-configured` targets). Collects the settings QueryPlus.Api genuinely cannot
# start without under ASPNETCORE_ENVIRONMENT=Production - ConnectionStrings__DefaultConnection,
# Keycloak__ClientSecret, Cors__AllowedOrigins__0 - plus a few optional ones (Keycloak
# Authority/ClientId, SMTP for Jobs module notifications), and writes them into $PREFIX/.env.
#
# Why this exists: those three settings are each a SEPARATE fail-fast startup check
# (ProductionSecretsValidator.cs for the first two, Program.cs's CORS setup for the third), so
# without this script an operator discovers them one at a time, via journalctl, across repeated
# `systemctl restart` cycles. This script collects all of them up front, in one pass, before the
# service is ever started - see `make install`'s target chain in the Makefile.
#
# Idempotent: if $PREFIX/.env already has all three required keys set, this exits immediately
# without prompting (so a routine `make install` re-run/upgrade never re-prompts). Set
# FORCE_RECONFIGURE=1 (the `sudo make configure` target does this) to always run interactively -
# fields are pre-filled with the current values where technically possible (passwords are never
# pre-filled/displayed; leave them blank to keep the existing value).
set -euo pipefail

PREFIX="${PREFIX:-/opt/queryplus}"
API_PORT="${API_PORT:-5000}"
ENV_FILE="$PREFIX/.env"
BACKTITLE="QueryPlus Setup"

get_env_value() { # $1 = key
    if [ -f "$ENV_FILE" ]; then
        grep -m1 "^$1=" "$ENV_FILE" 2>/dev/null | cut -d= -f2- || true
    fi
}

REQUIRED_KEYS=(ConnectionStrings__DefaultConnection Keycloak__ClientSecret Cors__AllowedOrigins__0)

is_complete() {
    for k in "${REQUIRED_KEYS[@]}"; do
        [ -n "$(get_env_value "$k")" ] || return 1
    done
    return 0
}

if is_complete && [ "${FORCE_RECONFIGURE:-0}" != "1" ]; then
    echo "$ENV_FILE already has all required settings (ConnectionStrings__DefaultConnection,"
    echo "Keycloak__ClientSecret, Cors__AllowedOrigins__0) - skipping interactive setup."
    echo "Run 'make configure' if you want to change them."
    exit 0
fi

if [ ! -t 0 ]; then
    echo "error: $ENV_FILE is missing required settings and this isn't running in a terminal" >&2
    echo "(stdin is not a tty), so the interactive setup can't run. Either run 'make install'" >&2
    echo "(as yourself, not root) / 'sudo make configure' from an interactive shell, or" >&2
    echo "pre-populate $ENV_FILE yourself with (mode 0600, root-owned):" >&2
    echo "  ConnectionStrings__DefaultConnection=<real SQL Server connection string>" >&2
    echo "  Keycloak__ClientSecret=<real Keycloak client secret>" >&2
    echo "  Cors__AllowedOrigins__0=http://<this host>:$API_PORT" >&2
    exit 1
fi

# dialog is preferred over whiptail: both are drop-in compatible for the widgets this script uses
# (msgbox/yesno/menu/inputbox/passwordbox - whiptail was explicitly built to mimic dialog's CLI,
# not the other way around, so none of the box()/confirm() calls below need to differ by backend),
# but dialog visibly distinguishes the focused button from the unfocused one (confirmed by
# rendering both and diffing the actual SGR color codes - whiptail's newt renders them byte-for-
# byte identically, so Tab silently does nothing visible even though it does move focus) and
# enables real xterm mouse tracking automatically (confirmed via the `\e[?1006;1000h`/`l` sequences
# it emits around each dialog - no flag needed, `--no-mouse` only exists to turn it back off).
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
# operator's own terminal emulator applies. Both backends render using literal ANSI color numbers
# 30-37/40-47 and assume their traditional meaning (blue is blue, cyan is cyan, ...) - a terminal
# theme that remaps those 16 slots to something else (Tokyo Night, Solarized, ...) can leave
# dialogs unreadable (e.g. "blue" rendering as near-black-on-black) no matter what color NAME is
# requested. DIALOGRC/NEWT_COLORS below only picks which of the terminal's slots each element
# uses; apply_dos_palette() further down is what actually fixes the slots' RGB values, and is
# needed regardless of which backend is in use, for the same reason.
DIALOGRC_IS_TEMP=0
setup_theme() {
    if [ "$TUI" = "dialog" ]; then
        # Respect an operator's own DIALOGRC, same as NEWT_COLORS below - never override an
        # explicit existing choice.
        [ -n "${DIALOGRC:-}" ] && return
        local rc
        rc=$(mktemp)
        # Generates the CURRENT, correct default template for whatever dialog version is actually
        # installed, rather than hand-authoring a full file from a possibly-stale key list -
        # dialog's own config format supports one key referencing another by name (e.g.
        # `inputbox_color = dialog_color`), which most of the defaults already do; overriding just
        # dialog_color/title_color/border_color below cascades to every key that still references
        # them, so only the keys that need to be genuinely DIFFERENT from that cascade (mainly the
        # active/inactive button pair - the whole reason for switching to dialog at all) need an
        # explicit literal override. The sed patches are name-matched and simply no-op on any key
        # a different dialog version doesn't emit, so this stays version-tolerant.
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
        # whiptail (via the newt library) - only applied if the operator hasn't already exported
        # their own NEWT_COLORS. Key names and color values verified directly against this host's
        # libnewt (`strings libnewt*.so*`) - "root"/"window"/"border" etc. are the real,
        # whiptail-specific NEWT_COLORS keys (not dialog's separate DIALOGRC format above).
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
# control what RGB value those slots actually render as. That's entirely up to the terminal
# emulator's active theme, which is exactly what causes a themed terminal (Tokyo Night, etc.) to
# still look wrong/washed-out even with a color scheme set: "blue"/"cyan"/"yellow"/"white" may all
# be remapped to similar low-contrast shades. Fix this by temporarily overriding the 16 ANSI slots
# themselves via OSC 4 (widely supported: xterm, Ghostty, kitty, alacritty, wezterm, iTerm2,
# Windows Terminal, ...) to authentic high-contrast values for the wizard's lifetime, then
# restoring the terminal's own palette via OSC 104 on exit (see cleanup() below) - success or
# cancelled, the palette must never stay overridden after this script ends. Skipped if stdout
# isn't a real terminal, or if QUERYPLUS_NO_THEME is set (e.g. a terminal that mishandles OSC 4).
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

# Captures the box's stdout via the classic fd-swap trick, while still letting a non-zero exit
# (Cancel/Esc) propagate under `set -e` - a cancelled setup must abort `make install`, not
# silently continue toward starting a service with an incomplete .env.
box() {
    "$TUI" --backtitle "$BACKTITLE" "$@" 3>&1 1>&2 2>&3
}

confirm() { # $1 = text, $2 = height, $3 = width
    "$TUI" --backtitle "$BACKTITLE" --yesno "$1" "${2:-10}" "${3:-70}"
}

# A single persistent trap (rather than the old "arm, then `trap - EXIT` right before success"
# pattern) so restore_palette runs on EVERY exit path - success, Cancel/Esc, or an unexpected
# error under `set -e` - not just the cancelled one. SUCCEEDED is flipped just before the final
# success message below; the cancellation message only prints when that never happened.
SUCCEEDED=0
cleanup() {
    restore_palette
    [ "$DIALOGRC_IS_TEMP" = "1" ] && rm -f "$DIALOGRC"
    if [ "$SUCCEEDED" != "1" ]; then
        echo
        echo "Setup cancelled - $ENV_FILE was not modified." >&2
    fi
}
trap cleanup EXIT

box --title "QueryPlus Setup" --msgbox \
    "This wizard collects the settings QueryPlus needs to start in production, and explains what each one is for as it asks:\n\n- SQL Server connection (where the catalog/audit/execution-log tables live)\n- Keycloak client secret (login/authentication)\n- The public origin the SPA is served from (a startup safety check, see below)\n- Optionally, SMTP for Jobs module email notifications\n\nValues are written to $ENV_FILE (mode 0600, root-owned, read only by root and the Api process via systemd). Press Cancel/Esc at any point to abort without changing anything." \
    20 76 >/dev/null

## --- SQL Server -------------------------------------------------------------

existing_conn="$(get_env_value ConnectionStrings__DefaultConnection)"
box --title "SQL Server connection" --msgbox \
    "QueryPlus needs its OWN database connection - this is where the catalog (categories/procedures/parameters), audit trail, and execution/job history are stored. It is separate from whatever SQL Server instance individual catalogued procedures might run against (Procedure.ConnectionName picks that per-procedure)." \
    11 76 >/dev/null
conn_mode=$(box --title "SQL Server connection" --menu \
    "How should the connection string be built? (see the previous screen for what this connects to)" 14 78 2 \
    "guided" "Host/database/user/password, entered separately (recommended)" \
    "raw" "Full connection string (named instances, Azure SQL, etc.)")

if [ "$conn_mode" = "raw" ]; then
    conn_string=$(box --title "SQL Server connection" --inputbox \
        "Full connection string, e.g.:\nServer=host,1433;Database=QueryPlus;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False" \
        10 76 "$existing_conn")
else
    db_host=$(box --title "SQL Server connection" --inputbox \
        "SQL Server hostname or IP - must be reachable from THIS machine (not from your workstation):" \
        9 76 "localhost")
    db_port=$(box --title "SQL Server connection" --inputbox \
        "SQL Server port (1433 is the SQL Server default):" 8 70 "1433")
    db_name=$(box --title "SQL Server connection" --inputbox \
        "Database name (the QueryPlus catalog database, created by EF Core migrations):" 9 76 "QueryPlus")
    db_user=$(box --title "SQL Server connection" --inputbox \
        "SQL Server login. Never use 'sa' here - create a dedicated least-privilege login first (db_datareader/db_datawriter/EXECUTE only); see docs/deploy-producao.md section 4.3 for the exact script:" \
        11 76 "${db_user:-queryplus_app}")
    db_pass=$(box --title "SQL Server connection" --passwordbox \
        "Password for that SQL Server login (hidden while typing):" 8 70)
    while [ -z "$db_pass" ]; do
        box --title "SQL Server connection" --msgbox "Password can't be empty." 8 60 >/dev/null
        db_pass=$(box --title "SQL Server connection" --passwordbox \
            "Password for that SQL Server login (hidden while typing):" 8 70)
    done
    if confirm "Does this SQL Server have a trusted TLS certificate configured (a real CA-issued cert, not a self-signed one)?\n\nYes (recommended for production) uses Encrypt=True;TrustServerCertificate=False.\nNo falls back to Encrypt=False;TrustServerCertificate=True - the same permissive/dev-only setting used in this repo's local Docker Compose stack. Only choose No if you know your SQL Server's certificate isn't trusted and can't fix that right now." \
        17 76; then
        tls_opts="Encrypt=True;TrustServerCertificate=False"
    else
        tls_opts="Encrypt=False;TrustServerCertificate=True"
    fi
    conn_string="Server=$db_host,$db_port;Database=$db_name;User Id=$db_user;Password=$db_pass;$tls_opts"
fi

## --- Keycloak -----------------------------------------------------------------

box --title "Keycloak" --msgbox \
    "QueryPlus authenticates users via Keycloak (OpenID Connect). These three values must match the realm/client you set up per docs/deploy-producao.md section 3.6 - QueryPlus does not create them for you." \
    11 76 >/dev/null

existing_authority="$(get_env_value Keycloak__Authority)"
kc_authority=$(box --title "Keycloak" --inputbox \
    "Keycloak Authority URL - the PUBLIC, browser-facing realm URL (users' browsers are redirected here to log in, so it must be reachable from their machines, not just from this server):" \
    11 76 "${existing_authority:-http://localhost:8080/realms/queryplus}")

existing_client_id="$(get_env_value Keycloak__ClientId)"
kc_client_id=$(box --title "Keycloak" --inputbox \
    "Keycloak client ID (must already exist in the realm above - 'queryplus-web' if you imported this repo's docker/keycloak/realm-export.json):" \
    10 76 "${existing_client_id:-queryplus-web}")

kc_secret=$(box --title "Keycloak" --passwordbox \
    "Keycloak client secret (Keycloak admin console -> Clients -> $kc_client_id -> Credentials). Hidden while typing; leave blank to keep the current value:" \
    10 76)
if [ -z "$kc_secret" ]; then
    kc_secret="$(get_env_value Keycloak__ClientSecret)"
fi
while [ -z "$kc_secret" ]; do
    box --title "Keycloak" --msgbox "Client secret can't be empty." 8 60 >/dev/null
    kc_secret=$(box --title "Keycloak" --passwordbox \
        "Keycloak client secret (hidden while typing):" 8 70)
done

## --- CORS ----------------------------------------------------------------------

default_host="$(hostname -I 2>/dev/null | awk '{print $1}')"
[ -z "$default_host" ] && default_host="localhost"
existing_cors="$(get_env_value Cors__AllowedOrigins__0)"
box --title "Public origin (CORS)" --msgbox \
    "In this deployment shape the Api serves the SPA itself (same origin, one process) - so this setting is never actually exercised by a real cross-origin browser request. It only satisfies a blanket ASP.NET Core startup guard ('CORS origins must be explicit outside Development') that can't tell same-origin apart from cross-origin. Any well-formed, non-empty origin works; use the real public URL if you have one (e.g. behind a reverse proxy with TLS)." \
    14 76 >/dev/null
cors_origin=$(box --title "Public origin (CORS)" --inputbox \
    "Public origin the SPA is served from (scheme + host + port, no trailing slash):" \
    9 76 "${existing_cors:-http://$default_host:$API_PORT}")

## --- SMTP (optional) -------------------------------------------------------------

smtp_host="" smtp_port="" smtp_ssl="" smtp_user="" smtp_pass="" smtp_from=""
if confirm "Configure SMTP now?\n\nThis is only used by the Jobs module (QueryPlus.Runner/QueryPlus.SchedulerSync) to email job-failure, missed-trigger, and lost-run notifications. If you're not using the Jobs module yet, you can safely skip this and configure it later ('make configure' or edit $ENV_FILE directly)." \
    13 76; then
    existing_smtp_host="$(get_env_value Smtp__Host)"
    smtp_host=$(box --title "SMTP" --inputbox \
        "SMTP server hostname:" 8 70 "${existing_smtp_host:-localhost}")
    existing_smtp_port="$(get_env_value Smtp__Port)"
    smtp_port=$(box --title "SMTP" --inputbox \
        "SMTP port (587 is the standard submission port with StartTLS):" 8 70 "${existing_smtp_port:-587}")
    if confirm "Use an encrypted connection (StartTLS)? Recommended, unless this is a local relay that doesn't support it." 9 70; then
        smtp_ssl="true"
    else
        smtp_ssl="false"
    fi
    existing_smtp_user="$(get_env_value Smtp__Username)"
    smtp_user=$(box --title "SMTP" --inputbox \
        "SMTP username (leave blank if this server accepts unauthenticated mail):" 8 76 "$existing_smtp_user")
    if [ -n "$smtp_user" ]; then
        smtp_pass=$(box --title "SMTP" --passwordbox \
            "SMTP password (hidden while typing; leave blank to keep the current value):" 8 70)
        [ -z "$smtp_pass" ] && smtp_pass="$(get_env_value Smtp__Password)"
    fi
    existing_smtp_from="$(get_env_value Smtp__FromAddress)"
    smtp_from=$(box --title "SMTP" --inputbox \
        "\"From\" address on notification emails:" 8 70 "${existing_smtp_from:-queryplus-jobs@example.local}")
fi

## --- Summary + write ---------------------------------------------------------------

masked_conn=$(printf '%s' "$conn_string" | sed -E 's/(Password=)[^;]*/\1********/I')
summary="The following will be written to $ENV_FILE:\n\n"
summary+="ConnectionStrings__DefaultConnection=$masked_conn\n"
summary+="Keycloak__Authority=$kc_authority\n"
summary+="Keycloak__ClientId=$kc_client_id\n"
summary+="Keycloak__ClientSecret=********\n"
summary+="Cors__AllowedOrigins__0=$cors_origin\n"
if [ -n "$smtp_host" ]; then
    summary+="Smtp__Host=$smtp_host\n"
fi
summary+="\nProceed?"
if ! confirm "$summary" 22 78; then
    exit 1
fi

upsert_env() { # $@ = KEY=VALUE pairs to set/replace; other existing lines are preserved as-is
    local tmp
    tmp=$(mktemp)
    if [ -f "$ENV_FILE" ]; then
        while IFS= read -r line || [ -n "$line" ]; do
            local key="${line%%=*}"
            local drop=0
            for kv in "$@"; do
                [ "$key" = "${kv%%=*}" ] && drop=1 && break
            done
            [ "$drop" -eq 1 ] || printf '%s\n' "$line" >>"$tmp"
        done <"$ENV_FILE"
    fi
    for kv in "$@"; do
        printf '%s\n' "$kv" >>"$tmp"
    done
    install -m 0600 -o root -g root "$tmp" "$ENV_FILE"
    rm -f "$tmp"
}

env_lines=(
    "ConnectionStrings__DefaultConnection=$conn_string"
    "Keycloak__Authority=$kc_authority"
    "Keycloak__ClientId=$kc_client_id"
    "Keycloak__ClientSecret=$kc_secret"
    "Cors__AllowedOrigins__0=$cors_origin"
)
if [ -n "$smtp_host" ]; then
    env_lines+=(
        "Smtp__Host=$smtp_host"
        "Smtp__Port=$smtp_port"
        "Smtp__EnableSsl=$smtp_ssl"
        "Smtp__Username=$smtp_user"
        "Smtp__Password=$smtp_pass"
        "Smtp__FromAddress=$smtp_from"
    )
fi
upsert_env "${env_lines[@]}"

SUCCEEDED=1
echo "Wrote $ENV_FILE (mode 0600, root:root)."
