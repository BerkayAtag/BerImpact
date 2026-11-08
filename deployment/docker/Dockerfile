FROM node:24.22.1-bookworm-slim AS frontend
WORKDIR /build/frontend
COPY src/frontend/package*.json ./
RUN npm ci
COPY src/frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0.403 AS build
WORKDIR /build
COPY Directory.Build.props global.json ./
COPY src/ src/
RUN dotnet publish src/backend/Api -c Release -o /out/api
RUN dotnet publish src/backend/Workers -c Release -o /out/worker
COPY --from=frontend /build/frontend/dist /out/api/wwwroot

FROM build AS tests
COPY BerImpact.sln ./
COPY tests/ tests/
RUN dotnet restore BerImpact.sln
ENTRYPOINT ["dotnet", "test", "BerImpact.sln", "--logger", "trx", "--results-directory", "/results"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS api
WORKDIR /app
COPY --from=build /out/api ./
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID
ENTRYPOINT ["dotnet", "BerImpact.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS worker
WORKDIR /app
COPY --from=build /out/worker ./
USER $APP_UID
ENTRYPOINT ["dotnet", "BerImpact.Workers.dll"]
