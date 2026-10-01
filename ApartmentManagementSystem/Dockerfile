# Build from the repo root:  docker build -f ApartmentManagementSystem/Dockerfile .
# (docker compose up does this for you)

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, in its own layer, so changing code doesn't re-download packages
COPY ApartmentManagementSystem/ApartmentManagementSystem.csproj ApartmentManagementSystem/
RUN dotnet restore ApartmentManagementSystem/ApartmentManagementSystem.csproj

COPY ApartmentManagementSystem/ ApartmentManagementSystem/
RUN dotnet publish ApartmentManagementSystem/ApartmentManagementSystem.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# The .NET 8 images listen on 8080 and include a non-root "app" user
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "ApartmentManagementSystem.dll"]
