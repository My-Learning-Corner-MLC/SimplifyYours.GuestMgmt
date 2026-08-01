# syntax=docker/dockerfile:1
#
# This service depends on the private SimplifyYours.Event.Consumer package published
# to GitHub Packages. Like the rest of this project's local dev setup, package auth
# is handled via each developer's own local, gitignored NuGet credentials
# (**/nuget.config is git-ignored repo-wide) rather than baked into the image.
# Build with:
#
#   docker buildx build --build-context hostnuget=$HOME/.nuget/packages -t <tag> .
#
# which bind-mounts your already-authenticated host NuGet cache into the restore
# step, read-only, so the private package resolves without embedding any credential
# in the image or its build history. If this Dockerfile is ever adapted for CI, swap
# this for an authenticated `dotnet nuget add source` step using a GH_PACKAGES_TOKEN
# secret instead (a CI runner has no pre-populated host cache).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["GuestManagementService.sln", "./"]
COPY ["src/GuestManagementService.Api/GuestManagementService.Api.csproj", "src/GuestManagementService.Api/"]
COPY ["src/GuestManagementService.Application/GuestManagementService.Application.csproj", "src/GuestManagementService.Application/"]
COPY ["src/GuestManagementService.Domain/GuestManagementService.Domain.csproj", "src/GuestManagementService.Domain/"]
COPY ["src/GuestManagementService.Infrastructure/GuestManagementService.Infrastructure.csproj", "src/GuestManagementService.Infrastructure/"]
COPY ["src/GuestManagementService.Contracts/GuestManagementService.Contracts.csproj", "src/GuestManagementService.Contracts/"]
RUN --mount=type=bind,from=hostnuget,target=/root/.nuget/packages,ro \
    dotnet restore "src/GuestManagementService.Api/GuestManagementService.Api.csproj"

COPY src/ src/
WORKDIR /src/src/GuestManagementService.Api
RUN --mount=type=bind,from=hostnuget,target=/root/.nuget/packages,ro \
    dotnet publish "GuestManagementService.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_HTTPS_PORTS=8081
EXPOSE 8080 8081

# librdkafka (via Confluent.Kafka, used by SimplifyYours.Event.Consumer) links against
# libgssapi_krb5 for optional SASL/GSSAPI support even when it's unused — not present in
# the minimal aspnet base image, so the Kafka client silently fails to load without it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

USER app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GuestManagementService.Api.dll"]
