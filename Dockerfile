# Force fresh build - v3
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Different WORKDIR to bust cache
WORKDIR /source

# Copy csproj first
COPY ["SleepEditWeb/SleepEditWeb.csproj", "SleepEditWeb/"]
WORKDIR /source/SleepEditWeb
RUN dotnet restore "SleepEditWeb.csproj"

# Copy all source files
WORKDIR /source
COPY SleepEditWeb/. SleepEditWeb/

# Verify medlist.txt is there
RUN echo "=== Checking source ===" && \
    ls -la /source/SleepEditWeb/Resources/ && \
    wc -l /source/SleepEditWeb/Resources/medlist.txt

# Build and publish
WORKDIR /source/SleepEditWeb
RUN dotnet publish "SleepEditWeb.csproj" -c Release -o /publish

# Verify publish output
RUN echo "=== Checking publish ===" && \
    ls -la /publish/Resources/ && \
    wc -l /publish/Resources/medlist.txt

# Runtime stage  
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published app
COPY --from=build /publish .

# Final verification
RUN echo "=== Final check ===" && \
    ls -la /app/Resources/ && \
    head -3 /app/Resources/medlist.txt

ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000

ENTRYPOINT ["dotnet", "SleepEditWeb.dll"]
