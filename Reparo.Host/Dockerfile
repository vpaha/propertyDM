FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["nuget.config", "."]
COPY ["artifacts", "artifacts"]

COPY ["Reparo.Host/Reparo.Host.csproj", "Reparo.Host/"]
COPY ["Reparo.Client/Reparo.Client.csproj", "Reparo.Client/"]
COPY ["Reparo.Shared/Components/Reparo.Components.csproj", "Reparo.Shared/Components/"]
COPY ["Reparo.Shared/Models/Reparo.Models.csproj", "Reparo.Shared/Models/"]
COPY ["Reparo.Shared/Utilities/Reparo.Utilities.csproj", "Reparo.Shared/Utilities/"]
COPY ["Reparo.Shared/Services/Reparo.Services.csproj", "Reparo.Shared/Services/"]

RUN dotnet restore "./Reparo.Host/Reparo.Host.csproj" --configfile /src/nuget.config

COPY . .
WORKDIR "/src/Reparo.Host"
RUN dotnet build "./Reparo.Host.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Reparo.Host.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Reparo.Host.dll"]