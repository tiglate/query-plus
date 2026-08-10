# QueryPlus - build/clean/publish/install automation for Linux.
#
# Drives the whole repo (the .NET solution + the React SPA) for everyday build/test/clean work,
# and does a full bare-Linux/systemd install of the whole app: the Api itself
# (queryplus-api.service), the Jobs module's two standalone executables (QueryPlus.Runner,
# QueryPlus.SchedulerSync) and their systemd timers, and /opt/queryplus/bin/{start,stop}.sh
# convenience wrappers around all three services.
#
# `make install` IS THE WHOLE INSTALL - run it as YOURSELF, never as root / via sudo:
#   make install
# It runs `make publish` first, as you (needs YOUR PATH - pnpm/node/dotnet, often under $HOME via
# nvm/volta/corepack) - then escalates to root itself via `sudo`, but ONLY for the specific
# install steps that actually need it (copying into /opt, chowning, writing systemd units,
# starting services). `sudo` prompts for your password at that point, once. Running `sudo make
# install` directly is refused on purpose (see check-not-root below): `sudo` resets PATH
# (secure_path in /etc/sudoers) by default, which would make a per-user pnpm/dotnet invisible to
# the publish step - the exact "pnpm: command not found" bug this split exists to avoid. Building
# as root is also how a `publish/` directory earlier ended up root-owned and impossible for the
# normal user to clean up afterwards - a second reason the publish step must run as you, not root.
# If you've already published and just want to (re-)run the root-only half on its own, `sudo make
# install-root` is still available directly.
#
# Targets that touch /opt or /etc/systemd/system require root - `make install` gets there via
# sudo automatically; other standalone admin targets (status, logs-*, uninstall, purge,
# install-systemd, configure, configure-accounts, ...) still expect a literal `sudo make <target>`
# from you. Read docs/local/jobs-module-deployment.md before running `make install` for real on a
# target host. This Makefile does not stand up SQL Server, OpenBao, or Keycloak, and this whole
# systemd-based path is an ALTERNATIVE to IIS/Docker, not a wrapper around them - see
# docs/deploy-producao.md if you're deploying via IIS or Docker instead.

SHELL := /bin/bash
.DEFAULT_GOAL := help

# Persist variable overrides (DEPLOY_USER, API_USER, PREFIX, etc.) across invocations instead of
# retyping them on every `make` call: create queryplus.local.mk (gitignored) in the repo root with
# plain assignments, e.g.:
#   DEPLOY_USER = daniel
#   API_USER = daniel
# This is included BEFORE the ?= defaults below, so a value set here wins over this file's own
# defaults; a value passed on the command line (make install DEPLOY_USER=...) still wins over
# both, since command-line assignments always take highest precedence in Make.
-include queryplus.local.mk

CONFIGURATION ?= Release
SOLUTION      := QueryPlus.sln
CLIENT_DIR    := client/queryplus-react
PUBLISH_DIR   ?= $(CURDIR)/publish
RUNTIME_ID    ?= linux-x64

PREFIX        ?= /opt/queryplus
SYSTEMD_DIR   ?= /etc/systemd/system
# 5000, not 8080: 8080 is Keycloak's port in this repo's local dev docker-compose stack (see
# CLAUDE.md), and queryplus-api.service runs directly on the host, sharing its real port space -
# unlike the Docker deployment path, where the Api's in-container 8080 never collides with
# anything. Override if this collides with something else on your host (e.g. `docker compose
# --profile full`, which also defaults to host port 5000 - see CLAUDE.md's Infra section).
API_PORT      ?= 5000
# Correct for a Microsoft package-repo install on Debian/Ubuntu (the documented target - see
# docs/deploy-producao.md). If .NET was installed a different way (dotnet-install.sh, tarball,
# snap), override this or queryplus-api.service will fail to start with status=203/EXEC.
DOTNET_BIN    ?= /usr/bin/dotnet
SYSTEMD_UNITS := queryplus-api.service \
                 queryplus-scheduler-sync.service queryplus-scheduler-sync.timer \
                 queryplus-scheduler-sync-watchdog.service queryplus-scheduler-sync-watchdog.timer

# Least-privilege accounts - see docs/local/jobs-module-deployment.md "Account model":
#   DEPLOY_USER/DEPLOY_GROUP - owns ONLY $(PREFIX)/scripts (publishing job scripts)
#   API_USER/API_GROUP       - runs queryplus-api.service, and API_GROUP gets group-read on job logs
# This Makefile does not create these accounts (see check-accounts below); they must already
# exist, e.g.:
#   sudo useradd --system --no-create-home --shell /usr/sbin/nologin $(DEPLOY_USER)
#   sudo useradd --system --no-create-home --shell /usr/sbin/nologin $(API_USER)
# Deliberately NOT used for the runner/scheduler-sync binaries, the systemd units, or .env - those
# stay root-owned (see install-bin/install-systemd below and the doc for why).
DEPLOY_USER   ?= queryplus-deploy
DEPLOY_GROUP  ?= $(DEPLOY_USER)
API_USER      ?= queryplus-api
API_GROUP     ?= $(API_USER)

.PHONY: help restore build frontend-build test test-backend test-frontend clean distclean \
        publish publish-api publish-runner publish-scheduler-sync \
        check-not-root install install-root install-real install-dirs install-api install-bin \
        install-systemd install-scripts \
        accounts-wizard configure-accounts configure ensure-configured \
        enable-api enable-timers \
        uninstall purge status logs-api logs-sync logs-watchdog migrate require-root check-accounts \
        check-published-api check-published-runner check-published-scheduler-sync

help: ## Show this help
	@echo "QueryPlus make targets"
	@echo
	@grep -E '^[a-zA-Z0-9_-]+:.*## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*## "}; {printf "  \033[36m%-22s\033[0m %s\n", $$1, $$2}'

## --- Build / test -----------------------------------------------------------

restore: ## Restore .NET and frontend dependencies
	dotnet restore $(SOLUTION)
	cd $(CLIENT_DIR) && pnpm install --frozen-lockfile

frontend-build: ## Build the React SPA into src/QueryPlus.Api/wwwroot (always fresh - see CLAUDE.md)
	cd $(CLIENT_DIR) && pnpm install --frozen-lockfile && pnpm run build

build: frontend-build ## Build the full solution (Api + Runner + SchedulerSync); SPA rebuilt first, always
	dotnet build $(SOLUTION) -c $(CONFIGURATION)

test-backend: ## Run the fast .NET test suite (excludes Integration tests, which need Docker)
	dotnet test $(SOLUTION) --filter "Category!=Integration"

test-frontend: ## Run the frontend test suite
	cd $(CLIENT_DIR) && pnpm test

test: test-backend test-frontend ## Run backend + frontend test suites

## --- Clean --------------------------------------------------------------------

clean: ## Remove build output (bin/obj, built SPA, publish/) - keeps node_modules and package caches
	dotnet clean $(SOLUTION) -c $(CONFIGURATION)
	find src tests -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
	rm -rf src/QueryPlus.Api/wwwroot
	rm -rf $(PUBLISH_DIR)

distclean: clean ## clean, plus remove node_modules (forces a full frontend reinstall next build)
	rm -rf $(CLIENT_DIR)/node_modules

## --- Publish (framework-dependent output under $(PUBLISH_DIR)) ----------------

publish: publish-api publish-runner publish-scheduler-sync ## Publish Api, Runner and SchedulerSync to $(PUBLISH_DIR)

publish-api: frontend-build ## Publish the Api (SPA included) to $(PUBLISH_DIR)/api
	dotnet publish src/QueryPlus.Api/QueryPlus.Api.csproj -c $(CONFIGURATION) \
		-o $(PUBLISH_DIR)/api /p:UseAppHost=false /p:SkipClientAppBuild=true

## PublishSingleFile is required here, not optional: a plain framework-dependent apphost publish
## produces the native launcher PLUS ~40 loose dependency DLLs, and the launcher has the *original*
## managed DLL name baked in at publish time - renaming just the launcher (which install-bin does,
## to get "runner"/"scheduler-sync" instead of "QueryPlus.Runner"/"QueryPlus.SchedulerSync") breaks
## that lookup ("The application to execute does not exist: '.../QueryPlus.Runner.dll'"), and even
## without the rename, none of those dependency DLLs would have been installed. Single-file bundles
## every managed dependency inside the one executable, so both problems disappear together.

publish-runner: ## Publish QueryPlus.Runner (framework-dependent linux-x64 single-file) to $(PUBLISH_DIR)/runner
	dotnet publish src/QueryPlus.Runner/QueryPlus.Runner.csproj -c $(CONFIGURATION) \
		-r $(RUNTIME_ID) --self-contained false -p:PublishSingleFile=true -o $(PUBLISH_DIR)/runner

publish-scheduler-sync: ## Publish QueryPlus.SchedulerSync (framework-dependent linux-x64 single-file) to $(PUBLISH_DIR)/scheduler-sync
	dotnet publish src/QueryPlus.SchedulerSync/QueryPlus.SchedulerSync.csproj -c $(CONFIGURATION) \
		-r $(RUNTIME_ID) --self-contained false -p:PublishSingleFile=true -o $(PUBLISH_DIR)/scheduler-sync

## --- Install: Api + Jobs module (Runner + SchedulerSync) + systemd units -----------
## Root-only. Intended for the real target host - review docs/local/jobs-module-deployment.md
## first, especially the script-permission guidance in its §3.

require-root:
	@if [ "$$(id -u)" != "0" ]; then \
		echo "error: this target must run as root (try: sudo make $(MAKECMDGOALS))" >&2; \
		exit 1; \
	fi

check-accounts: require-root ## Verify DEPLOY_USER/DEPLOY_GROUP/API_USER/API_GROUP exist before anything gets chowned to them (normally already handled by 'make install'/'sudo make accounts-wizard')
	@id -u $(DEPLOY_USER) >/dev/null 2>&1 || { \
		echo "error: user '$(DEPLOY_USER)' does not exist. Run 'sudo make accounts-wizard' (creates it" >&2; \
		echo "interactively), or create it yourself:" >&2; \
		echo "  sudo useradd --system --no-create-home --shell /usr/sbin/nologin $(DEPLOY_USER)" >&2; \
		exit 1; \
	}
	@getent group $(DEPLOY_GROUP) >/dev/null 2>&1 || { \
		echo "error: group '$(DEPLOY_GROUP)' does not exist. Run 'sudo make accounts-wizard', or:" >&2; \
		echo "  sudo groupadd --system $(DEPLOY_GROUP)" >&2; \
		exit 1; \
	}
	@id -u $(API_USER) >/dev/null 2>&1 || { \
		echo "error: user '$(API_USER)' does not exist. Run 'sudo make accounts-wizard' (creates it" >&2; \
		echo "interactively), or create it yourself:" >&2; \
		echo "  sudo useradd --system --no-create-home --shell /usr/sbin/nologin $(API_USER)" >&2; \
		exit 1; \
	}
	@getent group $(API_GROUP) >/dev/null 2>&1 || { \
		echo "error: group '$(API_GROUP)' does not exist. Run 'sudo make accounts-wizard', or:" >&2; \
		echo "  sudo groupadd --system $(API_GROUP)" >&2; \
		echo "  sudo usermod -aG $(API_GROUP) $(API_USER)" >&2; \
		exit 1; \
	}

install-dirs: check-accounts ## Create the /opt/queryplus layout (scripts/, scripts/uploads/, App_Data/jobs) with least-privilege ownership
	install -d -m 0755 -o root -g root $(PREFIX)
	install -d -m 0755 -o $(DEPLOY_USER) -g $(DEPLOY_GROUP) $(PREFIX)/scripts
	install -d -m 0755 -o $(API_USER) -g $(API_GROUP) $(PREFIX)/scripts/uploads
	install -d -m 0755 -o root -g root $(PREFIX)/App_Data
	install -d -m 02750 -o root -g $(API_GROUP) $(PREFIX)/App_Data/jobs
	@echo "$(PREFIX)/scripts is owned by $(DEPLOY_USER):$(DEPLOY_GROUP) (0755) - that account is the only"
	@echo "one that can publish manually-placed scripts. No job's run-as-user identity may have write"
	@echo "access to it or any parent directory - see docs/local/jobs-module-deployment.md section 3."
	@echo "$(PREFIX)/scripts/uploads is a SEPARATE subtree owned by $(API_USER):$(API_GROUP) (0755) -"
	@echo "the Api process writes here itself when an analyst uploads a script through the UI, so it"
	@echo "needs write access, but only within this one subdirectory, never to $(PREFIX)/scripts itself."
	@echo "$(PREFIX)/App_Data/jobs is setgid to $(API_GROUP) (02750) so job logs - written at runtime"
	@echo "by whichever run-as-user executed that job - stay group-readable by the Api process."

check-published-api: ## Verify 'make publish-api' has already been run (as yourself, not sudo)
	@test -f $(PUBLISH_DIR)/api/QueryPlus.Api.dll || { \
		echo "error: $(PUBLISH_DIR)/api/QueryPlus.Api.dll not found." >&2; \
		echo "Run 'make publish-api' (or 'make publish') as your normal user FIRST - not under sudo," >&2; \
		echo "install only copies already-published output, it never builds anything itself." >&2; \
		exit 1; \
	}

check-published-runner: ## Verify 'make publish-runner' has already been run (as yourself, not sudo)
	@test -f $(PUBLISH_DIR)/runner/QueryPlus.Runner || { \
		echo "error: $(PUBLISH_DIR)/runner/QueryPlus.Runner not found." >&2; \
		echo "Run 'make publish-runner' (or 'make publish') as your normal user FIRST - not under sudo," >&2; \
		echo "install only copies already-published output, it never builds anything itself." >&2; \
		exit 1; \
	}

check-published-scheduler-sync: ## Verify 'make publish-scheduler-sync' has already been run (as yourself, not sudo)
	@test -f $(PUBLISH_DIR)/scheduler-sync/QueryPlus.SchedulerSync || { \
		echo "error: $(PUBLISH_DIR)/scheduler-sync/QueryPlus.SchedulerSync not found." >&2; \
		echo "Run 'make publish-scheduler-sync' (or 'make publish') as your normal user FIRST - not" >&2; \
		echo "under sudo, install only copies already-published output, it never builds anything itself." >&2; \
		exit 1; \
	}

install-api: require-root check-published-api install-dirs ## Install the already-published Api (SPA included) to $(PREFIX)/api, owned by API_USER:API_GROUP
	install -d -m 0755 -o $(API_USER) -g $(API_GROUP) $(PREFIX)/api
	cp -a $(PUBLISH_DIR)/api/. $(PREFIX)/api/
	chown -R $(API_USER):$(API_GROUP) $(PREFIX)/api
	@echo "Installed $(PREFIX)/api, owned by $(API_USER):$(API_GROUP) (the Api's own App_Data/exports"
	@echo "directory is created under here at runtime and needs to stay writable by that account)."
	@echo ""
	@echo "queryplus-api.service will NOT start until $(PREFIX)/.env (or $(PREFIX)/api/.env) has"
	@echo "ConnectionStrings__DefaultConnection, Keycloak__ClientSecret, AND Cors__AllowedOrigins__0"
	@echo "all set - three separate fail-fast startup checks. 'make install' (rather than this"
	@echo "target alone) asks for all three interactively - see 'sudo make configure' to run that"
	@echo "wizard on its own. If you're invoking install-api directly/non-interactively instead,"
	@echo "set them yourself: see docs/deploy-producao.md section 4 (SQL Server login) and"
	@echo "docs/local/jobs-module-deployment.md section 1 (.env / OpenBao) for the exact keys."

install-bin: require-root check-published-runner check-published-scheduler-sync install-dirs ## Install the already-published runner/scheduler-sync binaries + shared appsettings.json (root-owned - see doc)
	install -m 0755 -o root -g root $(PUBLISH_DIR)/runner/QueryPlus.Runner $(PREFIX)/runner
	install -m 0755 -o root -g root $(PUBLISH_DIR)/scheduler-sync/QueryPlus.SchedulerSync $(PREFIX)/scheduler-sync
	@# appsettings.json is a template (src/QueryPlus.SchedulerSync/appsettings.json has @PREFIX@
	@# tokens in Jobs:ScriptAllowlistRoot/Jobs:LogRoot) substituted the same way the systemd units
	@# are - but unlike the units, it is also operator-editable at runtime (SMTP credentials,
	@# ConnectionStrings), so a reinstall must NOT blindly overwrite it. Only generate it if absent.
	if [ -f $(PREFIX)/appsettings.json ]; then \
		echo "$(PREFIX)/appsettings.json already exists - leaving it as-is (delete it first if you" >&2; \
		echo "want it regenerated from the repo template with the current PREFIX substituted)." >&2; \
	else \
		sed -e 's|@PREFIX@|$(PREFIX)|g' src/QueryPlus.SchedulerSync/appsettings.json > $(PREFIX)/appsettings.json; \
		chmod 0644 $(PREFIX)/appsettings.json; \
		chown root:root $(PREFIX)/appsettings.json; \
	fi
	@echo "Installed $(PREFIX)/runner and $(PREFIX)/scheduler-sync as root:root, deliberately NOT"
	@echo "owned by $(DEPLOY_USER): scheduler-sync runs as root, and runner is what enforces the"
	@echo "script hash pin, so write access to either is a privilege-escalation path, not a content"
	@echo "change - see docs/local/jobs-module-deployment.md 'Account model'."
	@echo "Both read $(PREFIX)/appsettings.json (0644, world-readable), since they share this install"
	@echo "directory as their base directory. Jobs:ScriptAllowlistRoot/Jobs:LogRoot are pre-filled to"
	@echo "$(PREFIX)/scripts and $(PREFIX)/App_Data/jobs; set ConnectionStrings:DefaultConnection and"
	@echo "Smtp:* there, or create $(PREFIX)/.env (mode 0600, root-owned) for secrets instead - see"
	@echo "docs/local/jobs-module-deployment.md sections 1-2."

install-scripts: require-root install-dirs ## Install /opt/queryplus/bin/{start,stop}.sh convenience wrappers
	install -d -m 0755 -o root -g root $(PREFIX)/bin
	install -m 0755 -o root -g root deploy/scripts/start.sh $(PREFIX)/bin/start.sh
	install -m 0755 -o root -g root deploy/scripts/stop.sh $(PREFIX)/bin/stop.sh
	@echo "Installed $(PREFIX)/bin/start.sh and stop.sh - run them (as root) to start/stop the Api"
	@echo "service and the two Jobs module timers together."

install-systemd: require-root check-accounts ## Install all systemd units (Api + Jobs module) and reload the daemon
	# All three .service files are TEMPLATES (@PREFIX@, and @API_USER@/@API_GROUP@/@API_PORT@/
	# @DOTNET_BIN@ for the Api unit specifically) - they must be substituted, not installed
	# verbatim, or overriding PREFIX/API_USER/API_GROUP/API_PORT/DOTNET_BIN would install/chown
	# files in one place while the services keep pointing at the untouched defaults, which then
	# fail to start (or worse, silently run against the wrong path/account/port) the moment that
	# mismatch surfaces. The two .timer files have no paths or accounts in them and are installed
	# as-is.
	@test -x "$(DOTNET_BIN)" || { \
		echo "error: DOTNET_BIN='$(DOTNET_BIN)' does not exist or is not executable." >&2; \
		echo "Run 'which dotnet' and pass the result as DOTNET_BIN=... if .NET wasn't installed" >&2; \
		echo "from the Microsoft package repo (see docs/deploy-producao.md)." >&2; \
		exit 1; \
	}
	sed -e 's/@API_USER@/$(API_USER)/g' -e 's/@API_GROUP@/$(API_GROUP)/g' -e 's|@PREFIX@|$(PREFIX)|g' \
		-e 's/@API_PORT@/$(API_PORT)/g' -e 's|@DOTNET_BIN@|$(DOTNET_BIN)|g' \
		deploy/systemd/queryplus-api.service > $(SYSTEMD_DIR)/queryplus-api.service
	sed -e 's|@PREFIX@|$(PREFIX)|g' \
		deploy/systemd/queryplus-scheduler-sync.service > $(SYSTEMD_DIR)/queryplus-scheduler-sync.service
	sed -e 's|@PREFIX@|$(PREFIX)|g' \
		deploy/systemd/queryplus-scheduler-sync-watchdog.service > $(SYSTEMD_DIR)/queryplus-scheduler-sync-watchdog.service
	chmod 0644 $(SYSTEMD_DIR)/queryplus-api.service $(SYSTEMD_DIR)/queryplus-scheduler-sync.service \
		$(SYSTEMD_DIR)/queryplus-scheduler-sync-watchdog.service
	chown root:root $(SYSTEMD_DIR)/queryplus-api.service $(SYSTEMD_DIR)/queryplus-scheduler-sync.service \
		$(SYSTEMD_DIR)/queryplus-scheduler-sync-watchdog.service
	install -m 0644 -o root -g root deploy/systemd/queryplus-scheduler-sync.timer $(SYSTEMD_DIR)/
	install -m 0644 -o root -g root deploy/systemd/queryplus-scheduler-sync-watchdog.timer $(SYSTEMD_DIR)/
	systemctl daemon-reload

enable-api: require-root ## Enable and start queryplus-api.service
	systemctl enable --now queryplus-api.service
	systemctl status queryplus-api.service --no-pager || true

enable-timers: require-root ## Enable and start the two Jobs module timers
	systemctl enable --now queryplus-scheduler-sync.timer
	systemctl enable --now queryplus-scheduler-sync-watchdog.timer
	systemctl list-timers 'queryplus*' --no-pager

## --- Interactive setup ------------------------------------------------------------
## Both wizards are idempotent (they skip prompting once already configured, unless forced) and
## write their answers to durable state - accounts-wizard to queryplus.local.mk, configure to
## $(PREFIX)/.env - specifically so `make install` produces a working, running Api on the first
## try, instead of an operator discovering each missing setting one at a time via journalctl.

accounts-wizard: require-root ## Internal: interactively choose/create DEPLOY_USER, API_USER, API_GROUP (persisted to queryplus.local.mk) - see 'install' for why this runs in its own sub-make
	@DEPLOY_USER=$(DEPLOY_USER) DEPLOY_GROUP=$(DEPLOY_GROUP) API_USER=$(API_USER) API_GROUP=$(API_GROUP) \
		bash deploy/scripts/accounts-wizard.sh

configure-accounts: require-root ## Re-run the account wizard even if DEPLOY_USER/API_USER/API_GROUP already resolve to existing accounts
	@DEPLOY_USER=$(DEPLOY_USER) DEPLOY_GROUP=$(DEPLOY_GROUP) API_USER=$(API_USER) API_GROUP=$(API_GROUP) \
		FORCE_RECONFIGURE=1 bash deploy/scripts/accounts-wizard.sh

ensure-configured: require-root install-dirs ## Internal: interactive $(PREFIX)/.env setup, only if required settings are missing - see 'configure' to force it
	@PREFIX=$(PREFIX) API_PORT=$(API_PORT) bash deploy/scripts/configure.sh

configure: require-root install-dirs ## Interactively (re)configure $(PREFIX)/.env - DB connection, Keycloak secret, CORS origin, SMTP
	@PREFIX=$(PREFIX) API_PORT=$(API_PORT) FORCE_RECONFIGURE=1 bash deploy/scripts/configure.sh

## --- Install: Api + Jobs module, end to end --------------------------------------
## `make install` is the single entrypoint - see the header comment at the top of this file for
## the full rationale (why it must run as you, not root, and how it still gets root when needed).

check-not-root: ## Internal: refuse to run as root - 'make install' escalates to root itself, via sudo, only where needed
	@if [ "$$(id -u)" = "0" ]; then \
		echo "error: don't run 'make install' as root (e.g. via 'sudo make install')." >&2; \
		echo "Run it as your normal user - it escalates to root itself, via sudo, only for the" >&2; \
		echo "specific steps that need it. The first thing it does is 'make publish', which needs" >&2; \
		echo "YOUR PATH (pnpm/dotnet, often under \$$HOME via nvm/volta/corepack) - running the" >&2; \
		echo "whole thing as root from the start would hide that PATH from the build step. If you" >&2; \
		echo "want to (re-)run only the root-only install steps on their own (e.g. you already" >&2; \
		echo "published separately), call 'sudo make install-root' directly instead." >&2; \
		exit 1; \
	fi

# DEPLOY_USER/DEPLOY_GROUP/API_USER/API_GROUP are deliberately NOT unconditionally forwarded here
# like the other variables below - only if the caller genuinely set them on THIS `make install`
# command line (checked via $(origin ...), not just "resolved to something"). Forwarding one of
# these unconditionally would pass e.g. API_USER=queryplus-api (its plain ?= default) as a literal
# command-line argument to `sudo $(MAKE) install-root ...` - and GNU Make auto-propagates
# command-line-set variables to every subsequent $(MAKE) sub-invocation, with the HIGHEST
# precedence of any source, for the rest of that process tree. That would "lock" the stale default
# in place for install-root's entire subtree, including install-real - so even after
# accounts-wizard interactively picks a different account and writes it to queryplus.local.mk,
# install-real's own fresh parse of that file would be silently overruled by the earlier
# command-line value, and check-accounts would fail looking for an account nobody chose. (This is
# exactly the bug that shipped initially: a plain `make install` with no account override still
# forwarded the ?= defaults this way, so the wizard's choice never reached install-real.) Omitting
# the variable here entirely - when it wasn't genuinely overridden - lets install-root's own fresh
# queryplus.local.mk parse (which the wizard just updated) be the one true source instead.
ACCOUNT_VAR_OVERRIDES :=
ifeq ($(origin DEPLOY_USER),command line)
ACCOUNT_VAR_OVERRIDES += DEPLOY_USER=$(DEPLOY_USER)
endif
ifeq ($(origin DEPLOY_GROUP),command line)
ACCOUNT_VAR_OVERRIDES += DEPLOY_GROUP=$(DEPLOY_GROUP)
endif
ifeq ($(origin API_USER),command line)
ACCOUNT_VAR_OVERRIDES += API_USER=$(API_USER)
endif
ifeq ($(origin API_GROUP),command line)
ACCOUNT_VAR_OVERRIDES += API_GROUP=$(API_GROUP)
endif

# Every OTHER Makefile variable install-root's subtree actually reads, re-listed explicitly rather
# than relying on `sudo -E`/environment propagation - see install-root's comment below for why.
# (These are safe to always forward: nothing downstream ever rewrites PREFIX/API_PORT/DOTNET_BIN/
# PUBLISH_DIR/SYSTEMD_DIR the way accounts-wizard rewrites the account variables above.)
INSTALL_ROOT_VARS = PREFIX=$(PREFIX) API_PORT=$(API_PORT) DOTNET_BIN=$(DOTNET_BIN) \
        PUBLISH_DIR=$(PUBLISH_DIR) SYSTEMD_DIR=$(SYSTEMD_DIR) $(ACCOUNT_VAR_OVERRIDES)

install: check-not-root ## THE install entrypoint - builds/publishes as you, then escalates to root via sudo (asks for your password once) for the steps that need it
	@$(MAKE) --no-print-directory publish
	sudo $(MAKE) --no-print-directory install-root $(INSTALL_ROOT_VARS)

# Reached via `install` above (typical use) or directly, e.g. `sudo make install-root`, if you've
# already published and want to skip straight to the root-only half. Either way this - and
# everything below it - assumes it's already running as root.
#
# Why `install`'s sudo call above re-passes every variable explicitly instead of using `sudo -E`:
# sudo resets the environment by default (secure_path etc.), which would silently drop any
# PREFIX=/DEPLOY_USER=/... override the outer `make install` resolved the moment it crosses the
# sudo boundary - explicit VAR=value arguments on the sudo command line don't depend on that.
install-root: require-root ## Internal: root half of 'make install' - accounts, then the actual install steps
	@$(MAKE) --no-print-directory accounts-wizard
	@$(MAKE) --no-print-directory install-real

# GNU Make resolves `-include queryplus.local.mk` once, at parse time, before any recipe runs -
# accounts-wizard writing fresh DEPLOY_USER/API_USER/API_GROUP into that file mid-recipe does NOT
# change what $(DEPLOY_USER) etc. resolve to for the rest of THIS `make` process (see the
# accounts-wizard.sh header comment for the full explanation). install-real is therefore invoked
# as a genuinely separate `make` process (above), which re-parses queryplus.local.mk with the
# wizard's fresh values before running any of the steps below - do not fold this back into
# `install-root` directly, or a freshly-chosen account would still install/chown against stale
# defaults. (This is a plain `$(MAKE)` call, not `sudo $(MAKE)` - install-root is already root by
# this point, having been reached via `install`'s sudo call or a direct `sudo make install-root`.)
install-real: install-api install-bin install-scripts install-systemd ensure-configured enable-api enable-timers
	@echo "Done. Verify with 'make status', then walk the manual smoke test in"
	@echo "docs/local/jobs-module-deployment.md section 5 before trusting this in production."
	@echo "Use $(PREFIX)/bin/start.sh and $(PREFIX)/bin/stop.sh (or 'make status'/'make logs-api') for"
	@echo "day-to-day start/stop - this target already enabled everything to start on boot too."

## --- Uninstall ------------------------------------------------------------------

uninstall: require-root ## Stop/disable all units and remove them (leaves $(PREFIX) data in place)
	-systemctl disable --now queryplus-api.service
	-systemctl disable --now queryplus-scheduler-sync.timer
	-systemctl disable --now queryplus-scheduler-sync-watchdog.timer
	rm -f $(addprefix $(SYSTEMD_DIR)/,$(SYSTEMD_UNITS))
	systemctl daemon-reload
	@echo "Systemd units removed. $(PREFIX) (Api, binaries, scripts, job logs) was left untouched -"
	@echo "use 'make purge' to remove it too."

purge: require-root uninstall ## uninstall, plus DELETE $(PREFIX) entirely (binaries, scripts, job logs) - destructive
	@echo "About to permanently delete $(PREFIX), including any job scripts and logs under it."
	@read -p "Type the full path to confirm ($(PREFIX)): " confirm; \
		if [ "$$confirm" != "$(PREFIX)" ]; then echo "Aborted."; exit 1; fi
	rm -rf $(PREFIX)

## --- Operations convenience -------------------------------------------------------

status: ## Show queryplus-api.service and the two Jobs module timers' status
	systemctl status queryplus-api.service --no-pager || true
	systemctl list-timers 'queryplus*' --no-pager
	systemctl status queryplus-scheduler-sync.timer queryplus-scheduler-sync-watchdog.timer --no-pager || true

logs-api: ## Tail the Api service's logs
	journalctl -u queryplus-api.service -n 100 --no-pager

logs-sync: ## Tail the last sync pass's logs
	journalctl -u queryplus-scheduler-sync.service -n 100 --no-pager

logs-watchdog: ## Tail the last watchdog pass's logs
	journalctl -u queryplus-scheduler-sync-watchdog.service -n 100 --no-pager

migrate: ## Apply pending EF Core migrations to the catalog database
	dotnet ef database update --project src/QueryPlus.Data --startup-project src/QueryPlus.Api
