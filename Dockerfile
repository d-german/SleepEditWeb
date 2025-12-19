# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SleepEditWeb/SleepEditWeb.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY SleepEditWeb/. .

# Show what we have before publish
RUN echo "=== Source Resources folder ===" && ls -la Resources/

RUN dotnet publish -c Release -o /app/publish

# Show what's in publish output
RUN echo "=== Publish Resources folder ===" && ls -la /app/publish/Resources/ || echo "Resources NOT in publish output!"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Fallback: explicitly copy Resources if not in publish
COPY --from=build /src/Resources ./Resources/

# Verify the file exists
RUN echo "=== Final Resources folder ===" && ls -la /app/Resources/

# Expose port 8000 (Koyeb default)
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "SleepEditWeb.dll"]
