FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src
COPY Soulstone.SyncServer/Soulstone.SyncServer.csproj Soulstone.SyncServer/
RUN dotnet restore Soulstone.SyncServer/Soulstone.SyncServer.csproj
COPY Soulstone.SyncServer/ Soulstone.SyncServer/
RUN dotnet publish Soulstone.SyncServer/Soulstone.SyncServer.csproj -c Release --no-restore --self-contained false -o /app

FROM build AS native
RUN apt-get update && apt-get install -y --no-install-recommends build-essential curl ca-certificates libssl-dev \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /cipher
# Pin and verify the upstream Community edition source; no paid packages or license.
RUN curl --fail --location --retry 3 https://codeload.github.com/sqlcipher/sqlcipher/tar.gz/refs/tags/v4.19.0 -o source.tar.gz \
    && echo '7075f96cbabe45b4ecfc2e6b1745a625f856f695b0827a5506ce9ed85b906aa0  source.tar.gz' | sha256sum -c - \
    && tar -xzf source.tar.gz --strip-components=1 \
    && ./configure --disable-tcl --with-tempstore=yes --dll-basename=libsqlcipher --soname=none \
        CFLAGS='-O2 -DSQLITE_HAS_CODEC -DSQLITE_EXTRA_INIT=sqlcipher_extra_init -DSQLITE_EXTRA_SHUTDOWN=sqlcipher_extra_shutdown' LDFLAGS='-lcrypto' \
    && make -j2 libsqlcipher.so sqlite3 \
    && mkdir -p /native/notices \
    && cp libsqlcipher.so /native/ \
    && cp sqlite3 /native/sqlcipher \
    && cp LICENSE.md /native/notices/SQLCipher-LICENSE.md \
    && cp /usr/share/doc/libssl-dev/copyright /native/notices/OpenSSL-copyright

# Run the same persistence and protocol tests with the current library built above.
FROM native AS test
WORKDIR /src
COPY Soulstone.SyncServer.Tests/ Soulstone.SyncServer.Tests/
RUN dotnet build Soulstone.SyncServer.Tests/Soulstone.SyncServer.Tests.csproj -c Release \
    && cp /native/libsqlcipher.so Soulstone.SyncServer.Tests/bin/Release/net8.0/ \
    && dotnet test Soulstone.SyncServer.Tests/Soulstone.SyncServer.Tests.csproj -c Release --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends libssl3 curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /var/lib/soulstone-sync
COPY --from=build /app/ .
COPY --from=test /native/libsqlcipher.so /app/
COPY --from=test /native/sqlcipher /usr/local/bin/sqlcipher
COPY --from=test /native/notices/ /app/notices/
ENV ASPNETCORE_URLS=http://0.0.0.0:5077 \
    Storage__DatabasePath=/var/lib/soulstone-sync/publications.db \
    Storage__KeyFile=/run/secrets/publications-key
EXPOSE 5077
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:5077/health || exit 1
ENTRYPOINT ["dotnet", "Soulstone.SyncServer.dll"]
