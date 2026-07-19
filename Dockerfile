# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Node + pnpm for ClientApp (Vite+ / Tailwind 4 → wwwroot/dist)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && corepack enable \
    && corepack prepare pnpm@10.14.0 --activate \
    && rm -rf /var/lib/apt/lists/*

COPY QueryPlus.sln ./
COPY src/QueryPlus.Domain/QueryPlus.Domain.csproj src/QueryPlus.Domain/
COPY src/QueryPlus.Application/QueryPlus.Application.csproj src/QueryPlus.Application/
COPY src/QueryPlus.Data/QueryPlus.Data.csproj src/QueryPlus.Data/
COPY src/QueryPlus.Infrastructure/QueryPlus.Infrastructure.csproj src/QueryPlus.Infrastructure/
COPY src/QueryPlus.Web/QueryPlus.Web.csproj src/QueryPlus.Web/

RUN dotnet restore src/QueryPlus.Web/QueryPlus.Web.csproj

COPY src/ src/
# MSBuild BuildClientApp target runs pnpm install + pnpm run build before publish.
RUN dotnet publish src/QueryPlus.Web/QueryPlus.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "QueryPlus.Web.dll"]
