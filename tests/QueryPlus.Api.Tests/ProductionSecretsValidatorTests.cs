using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using QueryPlus.Api.Hosting;

namespace QueryPlus.Api.Tests;

public sealed class ProductionSecretsValidatorTests
{
    private static IConfiguration BuildConfig(string? connectionString, string? clientSecret)
    {
        var values = new Dictionary<string, string?>();
        if (connectionString is not null)
        {
            values["ConnectionStrings:DefaultConnection"] = connectionString;
        }

        if (clientSecret is not null)
        {
            values["Keycloak:ClientSecret"] = clientSecret;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static FakeHostEnvironment Environment(string name) => new(name);

    [Theory]
    [InlineData("Development")]
    [InlineData("Docker")]
    [InlineData("Staging")]
    public void Validate_DoesNothing_OutsideProduction(string environmentName)
    {
        var config = BuildConfig(connectionString: null, clientSecret: null);

        var act = () => ProductionSecretsValidator.Validate(config, Environment(environmentName));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Throws_WhenConnectionStringIsMissing()
    {
        var config = BuildConfig(connectionString: null, clientSecret: "real-secret");

        var act = () => ProductionSecretsValidator.Validate(config, Environment("Production"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*DefaultConnection*");
    }

    [Fact]
    public void Validate_Throws_WhenConnectionStringUsesTheDummyDevPassword()
    {
        var config = BuildConfig(
            connectionString: "Server=prod-sql;Database=QueryPlus;User Id=queryplus_app;Password=Your_strong_Password123;",
            clientSecret: "real-secret");

        var act = () => ProductionSecretsValidator.Validate(config, Environment("Production"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*dummy password*");
    }

    [Fact]
    public void Validate_Throws_WhenKeycloakClientSecretIsMissing()
    {
        var config = BuildConfig(
            connectionString: "Server=prod-sql;Database=QueryPlus;User Id=queryplus_app;Password=real;",
            clientSecret: null);

        var act = () => ProductionSecretsValidator.Validate(config, Environment("Production"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Keycloak:ClientSecret*");
    }

    [Fact]
    public void Validate_Throws_WhenKeycloakClientSecretIsTheDummyPlaceholder()
    {
        var config = BuildConfig(
            connectionString: "Server=prod-sql;Database=QueryPlus;User Id=queryplus_app;Password=real;",
            clientSecret: "change-me-in-production");

        var act = () => ProductionSecretsValidator.Validate(config, Environment("Production"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*placeholder*");
    }

    [Fact]
    public void Validate_DoesNothing_WhenProductionSecretsAreReal()
    {
        var config = BuildConfig(
            connectionString: "Server=prod-sql;Database=QueryPlus;User Id=queryplus_app;Password=a-real-rotated-secret;",
            clientSecret: "a-real-rotated-secret");

        var act = () => ProductionSecretsValidator.Validate(config, Environment("Production"));

        act.Should().NotThrow();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QueryPlus.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
