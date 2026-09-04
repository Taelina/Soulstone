FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY publish/ .
ENV ASPNETCORE_URLS=http://0.0.0.0:5077
EXPOSE 5077
ENTRYPOINT ["dotnet", "Soulstone.SyncServer.dll"]