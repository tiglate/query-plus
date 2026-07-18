# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY QueryPlus.sln ./
COPY src/QueryPlus.Domain/QueryPlus.Domain.csproj src/QueryPlus.Domain/
COPY src/QueryPlus.Application/QueryPlus.Application.csproj src/QueryPlus.Application/
COPY src/QueryPlus.Data/QueryPlus.Data.csproj src/QueryPlus.Data/
COPY src/QueryPlus.Infrastructure/QueryPlus.Infrastructure.csproj src/QueryPlus.Infrastructure/
COPY src/QueryPlus.Web/QueryPlus.Web.csproj src/QueryPlus.Web/

RUN dotnet restore src/QueryPlus.Web/QueryPlus.Web.csproj

COPY src/ src/
RUN dotnet publish src/QueryPlus.Web/QueryPlus.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "QueryPlus.Web.dll"]
