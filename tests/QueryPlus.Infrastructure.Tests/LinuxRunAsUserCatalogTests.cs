using FluentAssertions;
using QueryPlus.Infrastructure.Jobs;

namespace QueryPlus.Infrastructure.Tests;

public class LinuxRunAsUserCatalogTests
{
    private const string SamplePasswd =
        "root:x:0:0:root:/root:/bin/bash\n" +
        "daemon:x:1:1:daemon:/usr/sbin:/usr/sbin/nologin\n" +
        "_apt:x:105:65534::/nonexistent:/usr/sbin/nologin\n" +
        "www-data:x:33:33:www-data:/var/www:/usr/sbin/nologin\n" +
        "nobody:x:65534:65534:nobody:/nonexistent:/usr/sbin/nologin\n" +
        "postgres:x:120:125:PostgreSQL:/var/lib/postgresql:/bin/bash\n" +
        "queryplus-job-etl:x:998:998::/nonexistent:/usr/sbin/nologin\n" +
        "queryplus-job-legacy:x:997:997::/nonexistent:/bin/false\n" +
        "daniel:x:1000:1000:Daniel:/home/daniel:/bin/bash\n";

    [Fact]
    public void ParseEligibleUsers_excludes_root()
    {
        var result = LinuxRunAsUserCatalog.ParseEligibleUsers(SamplePasswd, []);

        result.Should().NotContain("root");
    }

    [Fact]
    public void ParseEligibleUsers_excludes_interactive_shell_accounts()
    {
        var result = LinuxRunAsUserCatalog.ParseEligibleUsers(SamplePasswd, []);

        // "daniel" (interactive /bin/bash) and "postgres" (/bin/bash despite being a service
        // account) both fail the non-interactive-shell check on their own.
        result.Should().NotContain("daniel");
        result.Should().NotContain("postgres");
    }

    [Fact]
    public void ParseEligibleUsers_excludes_builtin_denylisted_system_accounts_despite_nologin_shell()
    {
        var result = LinuxRunAsUserCatalog.ParseEligibleUsers(SamplePasswd, []);

        // These all have a non-interactive shell, which is why the denylist (not just the shell
        // check) is what has to exclude them.
        result.Should().NotContain("daemon");
        result.Should().NotContain("_apt");
        result.Should().NotContain("www-data");
        result.Should().NotContain("nobody");
    }

    [Fact]
    public void ParseEligibleUsers_keeps_non_denylisted_noninteractive_accounts()
    {
        var result = LinuxRunAsUserCatalog.ParseEligibleUsers(SamplePasswd, []);

        result.Should().BeEquivalentTo(["queryplus-job-etl", "queryplus-job-legacy"]);
    }

    [Fact]
    public void ParseEligibleUsers_applies_extra_configured_denylist_entries()
    {
        var result = LinuxRunAsUserCatalog.ParseEligibleUsers(SamplePasswd, ["queryplus-job-legacy"]);

        result.Should().BeEquivalentTo(["queryplus-job-etl"]);
    }

    [Fact]
    public void ParseEligibleUsers_ignores_malformed_lines()
    {
        const string malformed = "queryplus-job-etl:x:998:998::/nonexistent:/usr/sbin/nologin\n" +
                                  "not-enough-fields:x:999\n";

        var result = LinuxRunAsUserCatalog.ParseEligibleUsers(malformed, []);

        result.Should().BeEquivalentTo(["queryplus-job-etl"]);
    }

    [Fact]
    public void ParseEligibleUsers_returns_sorted_results()
    {
        const string passwd = "queryplus-job-zeta:x:999:999::/nonexistent:/usr/sbin/nologin\n" +
                               "queryplus-job-alpha:x:998:998::/nonexistent:/usr/sbin/nologin\n";

        var result = LinuxRunAsUserCatalog.ParseEligibleUsers(passwd, []);

        result.Should().Equal("queryplus-job-alpha", "queryplus-job-zeta");
    }
}
