# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SleepEditWeb.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Download Vosk speech model and bake into published output
RUN apt-get update && apt-get install -y --no-install-recommends curl unzip \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/publish/wwwroot/vosk-model \
    && curl -sL https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip \
       -o /tmp/vosk-model.zip \
    && unzip -q /tmp/vosk-model.zip -d /tmp/vosk-model \
    && tar -czf /app/publish/wwwroot/vosk-model/model.tar.gz \
       -C /tmp/vosk-model/vosk-model-small-en-us-0.15 . \
    && rm -rf /tmp/vosk-model /tmp/vosk-model.zip

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create directory for persistent data
RUN mkdir -p /app/Resources

COPY --from=build /app/publish .

# Expose port 8080 (Koyeb expects this)
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "SleepEditWeb.dll"]
