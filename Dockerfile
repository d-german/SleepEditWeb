# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SleepEditWeb/SleepEditWeb.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY SleepEditWeb/. .

RUN dotnet publish -c Release -o /app/publish

# Explicitly ensure Resources is in publish output
RUN cp -r Resources /app/publish/Resources 2>/dev/null || true

# Runtime stage  
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output (should include Resources)
COPY --from=build /app/publish .

# Verify at build time
RUN echo "=== Build verification ===" && \
    ls -la /app/ && \
    echo "=== Resources folder ===" && \
    ls -la /app/Resources/ && \
    echo "=== medlist.txt check ===" && \
    head -5 /app/Resources/medlist.txt

# Expose port 8000 (Koyeb default)
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "SleepEditWeb.dll"]
