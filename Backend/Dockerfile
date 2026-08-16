# syntax=docker/dockerfile:1

# Multi-stage build for Warehouse.Api (net10.0, see Warehouse.Api.csproj).
# Build context is expected to be the Backend/ directory (see docker-compose.yml:
# services.backend.build.context), so paths below are relative to Backend/.

# ---------------------------------------------------------------------------
# Stage 1: build & publish
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the .csproj files first so `dotnet restore` gets its own Docker layer —
# it's only invalidated when a dependency actually changes, not on every source edit.
COPY Warehouse.Domain/Warehouse.Domain.csproj Warehouse.Domain/
COPY Warehouse.Application/Warehouse.Application.csproj Warehouse.Application/
COPY Warehouse.Infrastructure/Warehouse.Infrastructure.csproj Warehouse.Infrastructure/
COPY Warehouse.Api/Warehouse.Api.csproj Warehouse.Api/
RUN dotnet restore Warehouse.Api/Warehouse.Api.csproj

# Now bring in the rest of the source (test projects are deliberately not copied —
# they aren't needed to run the API and would only slow the image down) and publish.
COPY Warehouse.Domain/ Warehouse.Domain/
COPY Warehouse.Application/ Warehouse.Application/
COPY Warehouse.Infrastructure/ Warehouse.Infrastructure/
COPY Warehouse.Api/ Warehouse.Api/

RUN dotnet publish Warehouse.Api/Warehouse.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------------------------------------------------------------------------
# Stage 2: runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as a non-root user rather than the image's default root. UID/GID 10001 is
# picked well above the base image's own reserved system-account range (1654
# collided with one of those on mcr.microsoft.com/dotnet/aspnet:10.0).
RUN groupadd --system --gid 10001 appgroup \
    && useradd --system --gid appgroup --uid 10001 appuser
USER appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

# Modern .NET container convention (replaces ASPNETCORE_URLS for the common case).
# Kept in sync with docker-compose.yml's backend port mapping (host:5124 -> 8080).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Warehouse.Api.dll"]
