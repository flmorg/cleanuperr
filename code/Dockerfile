# Build Angular frontend
FROM --platform=$BUILDPLATFORM node:18-alpine AS frontend-build
ARG BASE_PATH=""
WORKDIR /app
COPY frontend/package*.json ./
RUN npm ci --only=production
COPY frontend/ .
RUN npm run build -- --base-href="${BASE_PATH}/" --deploy-url="${BASE_PATH}/"

# Build .NET backend
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG TARGETARCH
ARG VERSION=0.0.1
ARG PACKAGES_USERNAME
ARG PACKAGES_PAT
WORKDIR /app
EXPOSE 11011
COPY . ./
RUN dotnet nuget add source --username ${PACKAGES_USERNAME} --password ${PACKAGES_PAT} --store-password-in-clear-text --name Cleanuparr https://nuget.pkg.github.com/flmorg/index.json
RUN dotnet publish ./backend/Cleanuparr.Api/Cleanuparr.Api.csproj \
    -a $TARGETARCH \
    -c Release \
    -o /app/publish \
    /p:Version=${VERSION} \
    /p:PublishSingleFile=true

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim
RUN apt-get update && apt-get install -y tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Etc/UTC
# Fix FileSystemWatcher in Docker: https://github.com/dotnet/dotnet-docker/issues/3546
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
WORKDIR /app

# Copy backend
COPY --from=build /app/publish .

# Copy frontend to wwwroot
COPY --from=frontend-build /app/dist ./wwwroot

ENTRYPOINT ["./cleanuparr"]