# Stage 1: Build (Compile) the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY EMS.slnx ./EMS.sln
COPY EMS.Core/*.csproj ./EMS.Core/
COPY EMS.Infrastructure/*.csproj ./EMS.Infrastructure/
COPY EMS.Services/*.csproj ./EMS.Services/
COPY EMS.API/*.csproj ./EMS.API/

# Restore dependencies
RUN dotnet restore

# Copy all the source code
COPY . .

# Publish the application to a folder called "/app/publish"
WORKDIR /src/EMS.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime (Run the application)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy the published files from the build stage
COPY --from=build /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "EMS.API.dll"]