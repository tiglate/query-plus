# Node.js toolchain, copied in verbatim from the official image instead of curl-piping
# NodeSource's install script into bash as root.
FROM node:22-slim AS node

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules /usr/local/lib/node_modules
# COPY --from dereferences single-file symlinks (like the npm/npx/corepack shims in
# /usr/local/bin) into flat copies of their target's content, breaking the target's own
# relative requires - so these are recreated as real symlinks instead of copied directly.
RUN ln -s /usr/local/lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
    && ln -s /usr/local/lib/node_modules/npm/bin/npx-cli.js /usr/local/bin/npx \
    && ln -s /usr/local/lib/node_modules/corepack/dist/corepack.js /usr/local/bin/corepack \
    && corepack enable \
    && corepack prepare pnpm@10.14.0 --activate

COPY QueryPlus.sln ./
COPY src/QueryPlus.Domain/QueryPlus.Domain.csproj src/QueryPlus.Domain/
COPY src/QueryPlus.Application/QueryPlus.Application.csproj src/QueryPlus.Application/
COPY src/QueryPlus.Data/QueryPlus.Data.csproj src/QueryPlus.Data/
COPY src/QueryPlus.Infrastructure/QueryPlus.Infrastructure.csproj src/QueryPlus.Infrastructure/
COPY src/QueryPlus.Api/QueryPlus.Api.csproj src/QueryPlus.Api/
COPY client/queryplus-react/package.json client/queryplus-react/
COPY client/queryplus-react/pnpm-lock.yaml client/queryplus-react/

RUN dotnet restore src/QueryPlus.Api/QueryPlus.Api.csproj

COPY src/ src/
COPY client/queryplus-react/ client/queryplus-react/
# MSBuild BuildClientApp target runs pnpm install + pnpm run build before publish.
RUN dotnet publish src/QueryPlus.Api/QueryPlus.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# curl is needed only for the HEALTHCHECK below; the aspnet base image doesn't include it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data/exports \
    && chown -R $APP_UID:$APP_UID /app
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "QueryPlus.Api.dll"]
