# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SleepEditWeb/SleepEditWeb.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY SleepEditWeb/. .

# Publish and explicitly copy Resources to ensure it's included
RUN dotnet publish -c Release -o /app/publish && \
    mkdir -p /app/publish/Resources && \
    cp Resources/medlist.txt /app/publish/Resources/medlist.txt && \
    echo "=== Publish folder contents ===" && \
    ls -la /app/publish/Resources/

# Runtime stage  
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy everything from publish
COPY --from=build /app/publish/ /app/

# Verify the file is there
RUN ls -la /app/Resources/ && cat /app/Resources/medlist.txt | head -3

# Expose port 8000 (Koyeb default)
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "SleepEditWeb.dll"]
