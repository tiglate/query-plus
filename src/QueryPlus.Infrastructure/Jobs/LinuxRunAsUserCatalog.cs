using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using QueryPlus.Application.Interfaces;
using QueryPlus.Application.Options;

namespace QueryPlus.Infrastructure.Jobs;

/// <summary>
/// Enumerates eligible RunAsUser accounts by shelling out to `getent passwd` and keeping non-root
/// entries whose shell is a non-interactive one (nologin/false) and whose name isn't a known
/// system/service account. No user-controlled input reaches this call, but it still uses
/// ArgumentList (no shell interpolation) per this codebase's established process-spawning idiom -
/// see QueryPlus.SchedulerSync/CronSyncService.cs.
/// </summary>
public sealed class LinuxRunAsUserCatalog(IOptions<JobsOptions> jobsOptions) : IJobRunAsUserCatalog
{
    /// <summary>
    /// Common default Debian/Ubuntu base-install and packaged service accounts. All of these
    /// typically ship with a non-interactive shell (nologin/false), so the shell check alone
    /// doesn't exclude them - they're excluded by name because running a job as one of them would
    /// grant it whatever that specific service's own file/socket access is, which has nothing to
    /// do with running an IT Ops job. Not exhaustive by design (any installed package can add its
    /// own service account) - see JobsOptions.DenylistedRunAsUsers for site-specific additions.
    /// </summary>
    public static readonly IReadOnlySet<string> BuiltInDenylist = new HashSet<string>(StringComparer.Ordinal)
    {
        "daemon", "bin", "sys", "sync", "games", "man", "lp", "mail", "news", "uucp", "proxy",
        "www-data", "backup", "list", "irc", "gnats", "nobody", "_apt", "systemd-network",
        "systemd-resolve", "systemd-timesync", "systemd-oom", "messagebus", "syslog", "tss",
        "uuidd", "tcpdump", "avahi", "avahi-autoipd", "usbmux", "rtkit", "dnsmasq",
        "cups-pk-helper", "speech-dispatcher", "kernoops", "saned", "nm-openvpn", "colord",
        "hplip", "geoclue", "pulse", "pulseaudio", "gdm", "sshd", "landscape", "fwupd-refresh",
        "chrony", "_chrony", "_rpc", "_flatpak", "polkitd", "statd", "sslh", "ftp", "lightdm",
        "lxd", "libvirt-qemu", "dhcpd", "_dhcp", "postgres", "mysql", "redis", "_redis",
        "mongodb", "sssd", "ntp", "named", "bind",
    };

    public async Task<IReadOnlyList<string>> GetEligibleUsersAsync(CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo("getent")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("passwd");

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Win32Exception)
        {
            // getent is not present on this machine (e.g. non-Linux dev/CI environment) - the
            // module must remain usable without a real passwd database to query.
            return Array.Empty<string>();
        }

        if (process is null)
        {
            return Array.Empty<string>();
        }

        using (process)
        {
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return Array.Empty<string>();
            }

            return ParseEligibleUsers(stdout, jobsOptions.Value.DenylistedRunAsUsers);
        }
    }

    /// <summary>
    /// Pure parsing/filtering step, split out from GetEligibleUsersAsync so it's testable without
    /// mocking Process/getent.
    /// </summary>
    public static IReadOnlyList<string> ParseEligibleUsers(
        string passwdContent,
        IEnumerable<string> extraDenylistedNames)
    {
        var denylist = new HashSet<string>(BuiltInDenylist, StringComparer.Ordinal);
        denylist.UnionWith(extraDenylistedNames);

        var users = new List<string>();
        foreach (var line in passwdContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = line.Split(':');
            if (fields.Length != 7)
            {
                continue;
            }

            var name = fields[0];
            var uidField = fields[2];
            var shell = fields[6];

            if (!int.TryParse(uidField, out var uid) || uid == 0 || denylist.Contains(name))
            {
                continue;
            }

            var isNonInteractive = shell.EndsWith("nologin", StringComparison.Ordinal)
                || shell is "/bin/false" or "/usr/bin/false";

            if (isNonInteractive)
            {
                users.Add(name);
            }
        }

        users.Sort(StringComparer.Ordinal);
        return users;
    }
}
