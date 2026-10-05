# ============================================================
# Stage 1: Angular (Frontend) build
# ============================================================
FROM node:22-slim AS frontend
WORKDIR /src/Frontend

# पहले सिर्फ़ package files copy करें, ताकि npm install cache हो सके
COPY Frontend/package*.json ./
RUN npm ci

COPY Frontend/ ./
# angular.json का outputPath = ../Backend/EMS.API/wwwroot
# यानी output /src/Backend/EMS.API/wwwroot में बनेगा
RUN npm run build

# ============================================================
# Stage 2: .NET (Backend) build + publish
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src/Backend

# पहले सिर्फ़ csproj files, ताकि restore cache हो सके
COPY Backend/EMS.API/EMS.API.csproj EMS.API/
COPY Backend/EMS.Core/EMS.Core.csproj EMS.Core/
COPY Backend/EMS.Infrastructure/EMS.Infrastructure.csproj EMS.Infrastructure/
COPY Backend/EMS.Services/EMS.Services.csproj EMS.Services/
RUN dotnet restore "EMS.API/EMS.API.csproj"

# बाकी Backend code
COPY Backend/ ./

# Stage 1 में बना Angular output यहाँ लाएँ
COPY --from=frontend /src/Backend/EMS.API/wwwroot ./EMS.API/wwwroot

RUN dotnet publish "EMS.API/EMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================================
# Stage 3: Runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EMS.API.dll"]