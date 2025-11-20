# ==== STAGE 1 : build & publish ====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# On copie uniquement les .csproj nécessaires pour le restore
COPY ["LoLProject.WebApp/LoLProject.WebApp.csproj", "LoLProject.WebApp/"]
COPY ["LoLProject.Persistence/LoLProject.Persistence.csproj", "LoLProject.Persistence/"]
COPY ["LoLProject.ServiceDefaults/LoLProject.ServiceDefaults.csproj", "LoLProject.ServiceDefaults/"]

RUN dotnet restore "LoLProject.WebApp/LoLProject.WebApp.csproj"

# Maintenant on copie le code des projets nécessaires (PAS l'ApiService)
COPY LoLProject.WebApp/ LoLProject.WebApp/
COPY LoLProject.Persistence/ LoLProject.Persistence/
COPY LoLProject.ServiceDefaults/ LoLProject.ServiceDefaults/

WORKDIR /src/LoLProject.WebApp
RUN dotnet publish "LoLProject.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ==== STAGE 2 : runtime ====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LoLProject.WebApp.dll"]
