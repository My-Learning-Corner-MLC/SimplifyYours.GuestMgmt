# syntax=docker/dockerfile:1
#
# This service depends on SimplifyYours.Event.Consumer from platform-libraries.
# Rather than resolving it from GitHub Packages (needs a token) or a host NuGet
# cache bind-mount, this build rebuilds it from source via a named build
# context pointing at the platform-libraries repo, packs it locally, and
# restores against that local package source instead. Build with:
#
#   docker buildx build --build-context platformlibs-src=../platform-libraries -t <tag> .
#
# (docker compose's `build.additional_contexts` supplies this automatically --
# see code/infra/local-dev/docker-compose.yml.) This works identically in CI:
# a CI runner just needs the platform-libraries repo checked out at that
# relative path, no GH_PACKAGES_TOKEN required for this package.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS platformlibs
WORKDIR /libs
COPY --from=platformlibs-src . .
# 1.1.0 must match the PackageReference version in GuestManagementService.Infrastructure.csproj.
RUN dotnet pack SimplifyYours.Event.PlatformLibraries.sln -c Release \
    -p:PackageVersion=1.1.0 -o /packages

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY --from=platformlibs /packages /local-nuget
RUN dotnet nuget add source /local-nuget --name platform-libs

COPY ["GuestManagementService.sln", "./"]
COPY ["src/GuestManagementService.Api/GuestManagementService.Api.csproj", "src/GuestManagementService.Api/"]
COPY ["src/GuestManagementService.Application/GuestManagementService.Application.csproj", "src/GuestManagementService.Application/"]
COPY ["src/GuestManagementService.Domain/GuestManagementService.Domain.csproj", "src/GuestManagementService.Domain/"]
COPY ["src/GuestManagementService.Infrastructure/GuestManagementService.Infrastructure.csproj", "src/GuestManagementService.Infrastructure/"]
COPY ["src/GuestManagementService.Contracts/GuestManagementService.Contracts.csproj", "src/GuestManagementService.Contracts/"]
RUN dotnet restore "src/GuestManagementService.Api/GuestManagementService.Api.csproj"

COPY src/ src/
WORKDIR /src/src/GuestManagementService.Api
RUN dotnet publish "GuestManagementService.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_HTTPS_PORTS=8081
EXPOSE 8080 8081

# librdkafka (via Confluent.Kafka, used by SimplifyYours.Event.Consumer) links against
# libgssapi_krb5 for optional SASL/GSSAPI support even when it's unused — not present in
# the minimal aspnet base image, so the Kafka client silently fails to load without it.
# curl is installed alongside it for HEALTHCHECK below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*

HEALTHCHECK --interval=5s --timeout=3s --retries=10 --start-period=10s \
    CMD curl -fsS http://localhost:8080/ping || exit 1

USER app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GuestManagementService.Api.dll"]
