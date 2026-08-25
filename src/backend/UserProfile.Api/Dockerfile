FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS build
WORKDIR /src

COPY global.json ./
COPY src/backend/UserProfile.Api/UserProfile.Api.csproj \
     src/backend/UserProfile.Api/packages.lock.json \
     src/backend/UserProfile.Api/

RUN dotnet restore src/backend/UserProfile.Api/UserProfile.Api.csproj --locked-mode

COPY src/backend/UserProfile.Api/ src/backend/UserProfile.Api/

WORKDIR /src/src/backend/UserProfile.Api
RUN dotnet publish UserProfile.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS final
WORKDIR /app

RUN mkdir -p /data && chown app:app /data

COPY --from=build --chown=app:app /app/publish/ ./

USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UserProfile.Api.dll"]
