# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SleepEditWeb/SleepEditWeb.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY SleepEditWeb/. .
RUN dotnet publish -c Release -o /app/publish

# Debug: List what's in the publish output
RUN ls -la /app/publish/Resources/ || echo "Resources folder not found in publish"

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output (includes Resources/medlist.txt)
COPY --from=build /app/publish .

# Debug: Verify the file was copied
RUN ls -la /app/Resources/ || echo "Resources folder not found"

# Expose port 8000 (Koyeb default)
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "SleepEditWeb.dll"]
