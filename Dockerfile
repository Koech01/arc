FROM node:20-alpine AS frontend-build
WORKDIR /src

COPY frontend/package*.json frontend/
RUN cd frontend && npm ci

COPY frontend frontend
COPY backend/src/Arc.Api backend/src/Arc.Api
RUN cd frontend && npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /src

COPY backend backend
COPY --from=frontend-build /src/backend/src/Arc.Api/wwwroot backend/src/Arc.Api/wwwroot

RUN dotnet restore backend/Arc.sln
RUN dotnet publish backend/src/Arc.Api/Arc.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5266

COPY --from=backend-build /app/publish .
ENTRYPOINT ["dotnet", "Arc.Api.dll"]
