FROM node:26-alpine AS node-build
WORKDIR /app

RUN corepack enable

# no globs: a missing lockfile must fail the build, not silently install unpinned
COPY Sso/package.json Sso/pnpm-lock.yaml Sso/pnpm-workspace.yaml ./
# corepack install verifies the pnpm version+hash pinned in packageManager before running it
RUN corepack install && pnpm install --frozen-lockfile

# we need everything including the dotnet app for purgecss
COPY Sso ./
RUN pnpm run build


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /app

RUN dotnet nuget locals all --clear

# copy csproj and restore as distinct layers
COPY *.sln .
COPY Sso/*.csproj ./Sso/
COPY Sso/packages.lock.json ./Sso/
RUN dotnet restore Sso

# copy everything else and build app
COPY Sso Sso
RUN dotnet publish -c Release -o dist --no-restore Sso

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=dotnet-build /app/dist .
COPY --from=node-build /app/wwwroot/app ./wwwroot/app/

EXPOSE 8080
ENV ConnectionStrings__Db "Data Source=/data/sso.db"
# keep the data protection key ring on the volume so IdentityServer signing keys
# stay readable across restarts
ENV DataProtection__KeyPath=/data/keys
VOLUME /data

HEALTHCHECK CMD curl --fail http://localhost:8080/health/live || exit
CMD dotnet Sso.dll