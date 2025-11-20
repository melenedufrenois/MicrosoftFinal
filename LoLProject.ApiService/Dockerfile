FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LoLProject.ApiService/LoLProject.ApiService.csproj", "LoLProject.ApiService/"]
COPY ["LoLProject.ServiceDefaults/LoLProject.ServiceDefaults.csproj", "LoLProject.ServiceDefaults/"]
COPY ["LoLProject.Persistence/LoLProject.Persistence.csproj", "LoLProject.Persistence/"]
RUN dotnet restore "LoLProject.ApiService/LoLProject.ApiService.csproj"
COPY . .
WORKDIR "/src/LoLProject.ApiService"
RUN dotnet build "./LoLProject.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LoLProject.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LoLProject.ApiService.dll"]
