# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Cache bust - change this to force rebuild: v2
ARG CACHEBUST=2

# Copy csproj and restore dependencies
COPY ["SleepEditWeb/SleepEditWeb.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY SleepEditWeb/. .

# Verify source files
RUN echo "=== Source check ===" && ls -la Resources/

# Publish
RUN dotnet publish -c Release -o /app/publish

# Force copy Resources after publish
RUN cp -rv Resources /app/publish/ && \
    echo "=== After copy ===" && \
    ls -la /app/publish/Resources/

# Runtime stage  
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set working directory
WORKDIR /app

# Copy from build stage - use explicit path
COPY --from=build /app/publish/. .

# Debug: show what we have
RUN echo "=== Runtime /app contents ===" && \
    ls -la /app/ && \
    echo "=== Runtime /app/Resources contents ===" && \
    ls -la /app/Resources/ && \
    echo "=== File size check ===" && \
    wc -l /app/Resources/medlist.txt

# Expose port 8000 (Koyeb default)
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "SleepEditWeb.dll"]
