# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["EMS.API/EMS.API.csproj", "EMS.API/"]
COPY ["EMS.Core/EMS.Core.csproj", "EMS.Core/"]
COPY ["EMS.Infrastructure/EMS.Infrastructure.csproj", "EMS.Infrastructure/"]
COPY ["EMS.Services/EMS.Services.csproj", "EMS.Services/"]
RUN dotnet restore "EMS.API/EMS.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/EMS.API"
RUN dotnet build "EMS.API.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "EMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published output
COPY --from=publish /app/publish .

# Set environment variable for Render
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EMS.API.dll"]