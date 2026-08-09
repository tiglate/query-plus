namespace QueryPlus.Api.Hosting;

/// <summary>
/// Fails startup fast when running with <c>ASPNETCORE_ENVIRONMENT=Production</c> and a critical
/// secret is missing or still holds one of the documented local-dev placeholder values (see
/// .env.example / docker/keycloak/realm-export.json) - catches a forgotten OpenBao/env override
/// before the app silently starts against dummy credentials.
/// </summary>
public static class ProductionSecretsValidator
{
    private const string DummyConnectionStringPassword = "Your_strong_Password123";
    private const string DummyKeycloakClientSecret = "change-me-in-production";

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured when ASPNETCORE_ENVIRONMENT=Production.");
        }

        if (connectionString.Contains(DummyConnectionStringPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection still uses the local-development dummy password " +
                $"('{DummyConnectionStringPassword}'). Configure a real production credential before " +
                "starting with ASPNETCORE_ENVIRONMENT=Production.");
        }

        var clientSecret = configuration["Keycloak:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "Keycloak:ClientSecret must be configured when ASPNETCORE_ENVIRONMENT=Production.");
        }

        if (string.Equals(clientSecret, DummyKeycloakClientSecret, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Keycloak:ClientSecret still uses the local-development placeholder value " +
                $"('{DummyKeycloakClientSecret}'). Configure the real production client secret before " +
                "starting with ASPNETCORE_ENVIRONMENT=Production.");
        }
    }
}
