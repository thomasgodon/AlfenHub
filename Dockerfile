# syntax=docker/dockerfile:1

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore as a separate layer for better caching.
# Copy every project file first so restoring the host transitively restores the layer projects.
COPY AlfenHub/AlfenHub.csproj AlfenHub/
COPY AlfenHub.Application/AlfenHub.Application.csproj AlfenHub.Application/
COPY AlfenHub.Domain/AlfenHub.Domain.csproj AlfenHub.Domain/
COPY AlfenHub.Infrastructure/AlfenHub.Infrastructure.csproj AlfenHub.Infrastructure/
RUN dotnet restore AlfenHub/AlfenHub.csproj

# Copy the rest and publish (framework-dependent; runtime comes from the base image)
COPY . .
RUN dotnet publish AlfenHub/AlfenHub.csproj -c Release -o /app/publish

# --- Runtime stage ---
# Hosts the read-only web dashboard (Kestrel) alongside the polling worker, so use the ASP.NET
# Core runtime image (the plain runtime image lacks the ASP.NET shared framework).
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_ENVIRONMENT=Production

# Read-only dashboard port (DashboardOptions.Port, default 8080). Set DashboardOptions__Enabled=false
# to run as a plain worker with no listening port.
EXPOSE 8080

# Override config at `docker run` time via env vars using .NET's `__` section separator, e.g.:
#   -e AlfenModbusOptions__Host=192.168.0.19
#   -e KnxOptions__Host=192.168.0.16
#   -e DashboardOptions__Port=8080
ENTRYPOINT ["dotnet", "AlfenHub.dll"]
