FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AssistantEngineer.sln ./
COPY src/Backend/ ./src/Backend/
RUN dotnet restore src/Backend/AssistantEngineer.Api/AssistantEngineer.Api.csproj
RUN dotnet publish src/Backend/AssistantEngineer.Api/AssistantEngineer.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish ./
RUN mkdir -p /app/artifacts/operations && chown -R $APP_UID:$APP_UID /app/artifacts
USER $APP_UID
ENTRYPOINT ["dotnet", "AssistantEngineer.Api.dll"]
