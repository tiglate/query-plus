using Microsoft.Extensions.Configuration;
using QueryPlus.Application.Abstractions;

namespace QueryPlus.Data.StoredProcedures;

public sealed class ProcedureConnectionCatalog(IConfiguration configuration) : IProcedureConnectionCatalog
{
    public IReadOnlyCollection<string> GetConnectionNames() =>
        configuration.GetSection("ConnectionStrings").GetChildren()
            .Select(c => c.Key)
            .Where(key => !string.IsNullOrWhiteSpace(configuration.GetConnectionString(key)))
            .ToArray();
}
