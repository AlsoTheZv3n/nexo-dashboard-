# syntax=docker/dockerfile:1.7

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only csproj files first to maximise layer caching.
COPY backend/Directory.Build.props backend/Directory.Build.props
COPY backend/Dashboard.sln backend/Dashboard.sln
COPY backend/Dashboard.Core/Dashboard.Core.csproj backend/Dashboard.Core/
COPY backend/Dashboard.Infrastructure/Dashboard.Infrastructure.csproj backend/Dashboard.Infrastructure/
COPY backend/Dashboard.PowerShell/Dashboard.PowerShell.csproj backend/Dashboard.PowerShell/
COPY backend/Dashboard.Api/Dashboard.Api.csproj backend/Dashboard.Api/
COPY backend/Dashboard.Tests/Dashboard.Tests.csproj backend/Dashboard.Tests/
RUN dotnet restore backend/Dashboard.Api/Dashboard.Api.csproj

# Now the full sources.
COPY backend/ backend/
RUN dotnet publish backend/Dashboard.Api/Dashboard.Api.csproj \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- runtime ----
# The PowerShell SDK is already bundled via System.Management.Automation, but
# invoking native PS modules benefits from having pwsh available on PATH too.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl \
 && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    PowerShell__ScriptsDirectory=/app/scripts

COPY --from=build /app/publish ./
# Default scripts ship with the image. Prod can mount a volume to override.
COPY powershell/scripts/ ./scripts/

EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Dashboard.Api.dll"]
