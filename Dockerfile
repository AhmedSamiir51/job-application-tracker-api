# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so this layer is cached until a project file changes.
COPY global.json Directory.Build.props Directory.Packages.props JobTracker.slnx ./
COPY src/JobTracker.Domain/*.csproj src/JobTracker.Domain/
COPY src/JobTracker.Application/*.csproj src/JobTracker.Application/
COPY src/JobTracker.Infrastructure/*.csproj src/JobTracker.Infrastructure/
COPY src/JobTracker.Api/*.csproj src/JobTracker.Api/
RUN dotnet restore src/JobTracker.Api/JobTracker.Api.csproj

COPY src/ src/
RUN dotnet publish src/JobTracker.Api/JobTracker.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# The aspnet image ships an unprivileged "app" user.
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobTracker.Api.dll"]
