FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore "GarnetServerHost/GarnetServerHost.csproj"
# Build and publish a release
RUN dotnet build "GarnetServerHost/GarnetServerHost.csproj" -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY --from=build-env --chown=app /app/out .

EXPOSE 3278

USER app
ENTRYPOINT ["dotnet", "GarnetServerHost.dll"]
