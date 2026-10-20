# Build stage: the full SDK compiles and publishes the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first, so the NuGet restore layer is cached
# and re-runs only when dependencies change.
COPY Directory.Build.props ./
COPY src/TourShuttle.Domain/TourShuttle.Domain.csproj src/TourShuttle.Domain/
COPY src/TourShuttle.Application/TourShuttle.Application.csproj src/TourShuttle.Application/
COPY src/TourShuttle.Infrastructure/TourShuttle.Infrastructure.csproj src/TourShuttle.Infrastructure/
COPY src/TourShuttle.API/TourShuttle.API.csproj src/TourShuttle.API/
RUN dotnet restore src/TourShuttle.API/TourShuttle.API.csproj

COPY src/ src/
RUN dotnet publish src/TourShuttle.API/TourShuttle.API.csproj -c Release -o /app --no-restore

# Runtime stage: only the ASP.NET runtime and the published output, so the image stays small.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# NLog writes daily files to /app/logs. /app belongs to root, so the folder is created here and
# handed to the "app" user below; otherwise the file target could not write. (Render's disk is
# reset on every deploy, so on Render the console output is the log that lasts.)
RUN mkdir -p /app/logs && chown app:app /app/logs

# Render routes traffic to this port (set PORT=8080 on the Render service).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# The image runs as the non-root "app" user that the .NET base image provides.
USER app
ENTRYPOINT ["dotnet", "TourShuttle.API.dll"]
