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
# Worker / BackgroundService -> use the plain runtime image (no web server needed)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV DOTNET_ENVIRONMENT=Production

# Override config at `docker run` time via env vars using .NET's `__` section separator, e.g.:
#   -e AlfenModbusOptions__Host=192.168.0.19
#   -e KnxOptions__Host=192.168.0.16
ENTRYPOINT ["dotnet", "AlfenHub.dll"]
