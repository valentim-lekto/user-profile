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

FROM mcr.microsoft.com/dotnet/sdk:10.0.400-noble AS test
WORKDIR /src

COPY global.json UserProfile.sln ./
COPY src/backend/UserProfile.Api/UserProfile.Api.csproj \
     src/backend/UserProfile.Api/packages.lock.json \
     src/backend/UserProfile.Api/
COPY tests/backend/UserProfile.Api.IntegrationTests/UserProfile.Api.IntegrationTests.csproj \
     tests/backend/UserProfile.Api.IntegrationTests/packages.lock.json \
     tests/backend/UserProfile.Api.IntegrationTests/

RUN dotnet restore UserProfile.sln --locked-mode

COPY src/backend/UserProfile.Api/ src/backend/UserProfile.Api/
COPY tests/backend/UserProfile.Api.IntegrationTests/ tests/backend/UserProfile.Api.IntegrationTests/

RUN dotnet build UserProfile.sln --configuration Release --no-restore --property:TreatWarningsAsErrors=true

CMD ["dotnet", "test", "UserProfile.sln", "--configuration", "Release", "--no-build", "--no-restore", "--verbosity", "minimal"]

FROM test AS mutation-test
WORKDIR /src

COPY .config/dotnet-tools.json .config/dotnet-tools.json
RUN dotnet tool restore

COPY scripts/run-mutation-tests.sh scripts/validate-mutation-report.sh /src/scripts/
RUN chmod +x /src/scripts/run-mutation-tests.sh /src/scripts/validate-mutation-report.sh

WORKDIR /src/tests/backend/UserProfile.Api.IntegrationTests
CMD ["/src/scripts/run-mutation-tests.sh", "--output", "/artifacts", "--skip-version-check"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-noble AS final
WORKDIR /app

RUN mkdir -p /data && chown app:app /data

COPY --from=build --chown=app:app /app/publish/ ./

USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UserProfile.Api.dll"]
