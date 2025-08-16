# Dockerfile optimized for Railway deployment - DeliveryValidationService
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Set Railway-specific environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Accept build arguments for GitHub authentication
ARG GITHUB_USERNAME
ARG GITHUB_TOKEN

# Set them as environment variables for the build process
ENV GITHUB_USERNAME=$GITHUB_USERNAME
ENV GITHUB_TOKEN=$GITHUB_TOKEN

WORKDIR /src
COPY . .

# Debug: Show what we have
RUN echo "=== Environment Check ===" && \
    echo "GITHUB_USERNAME: $GITHUB_USERNAME" && \
    echo "GITHUB_TOKEN: [REDACTED]" && \
    echo "=== Listing root directory ===" && ls -la

RUN echo "=== Listing DeliveryValidationService.Api directory ===" && ls -la DeliveryValidationService.Api/
RUN echo "=== Checking for csproj files ===" && find . -name "*.csproj" | head -10

# Configure NuGet authentication if GitHub credentials are provided
RUN if [ ! -z "$GITHUB_USERNAME" ] && [ ! -z "$GITHUB_TOKEN" ]; then \
        echo "=== Configuring GitHub NuGet source ===" && \
        dotnet nuget add source https://nuget.pkg.github.com/$GITHUB_USERNAME/index.json \
            --name github \
            --username $GITHUB_USERNAME \
            --password $GITHUB_TOKEN \
            --store-password-in-clear-text && \
        echo "GitHub NuGet source configured successfully"; \
    else \
        echo "=== No GitHub credentials provided, skipping NuGet source configuration ==="; \
    fi

# Clear NuGet cache to avoid conflicts
RUN dotnet nuget locals all --clear

# Check what's in our working directory and find the project
RUN echo "=== Current working directory contents ===" && \
    pwd && ls -la && \
    echo "=== Looking for .csproj files ===" && \
    find . -name "*.csproj" -type f

# Restore with enhanced error handling - try both possible paths
RUN echo "=== Starting restore ===" && \
    if [ -f "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" ]; then \
        echo "Found project at DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" && \
        dotnet restore "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" --verbosity normal --no-cache; \
    elif [ -f "DeliveryValidationService.Api.csproj" ]; then \
        echo "Found project at DeliveryValidationService.Api.csproj" && \
        dotnet restore "DeliveryValidationService.Api.csproj" --verbosity normal --no-cache; \
    else \
        echo "=== PROJECT FILE NOT FOUND ===" && \
        echo "=== Available files ===" && find . -name "*.csproj" && \
        exit 1; \
    fi || \
    (echo "=== RESTORE FAILED ===" && \
     echo "=== NuGet sources ===" && dotnet nuget list source && \
     exit 1)

# Build with error handling - use the same logic to find the project
RUN echo "=== Starting build ===" && \
    if [ -f "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" ]; then \
        dotnet build "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" -c Release -o /app/build --verbosity normal --no-restore; \
    elif [ -f "DeliveryValidationService.Api.csproj" ]; then \
        dotnet build "DeliveryValidationService.Api.csproj" -c Release -o /app/build --verbosity normal --no-restore; \
    else \
        echo "=== PROJECT FILE NOT FOUND FOR BUILD ===" && exit 1; \
    fi || \
    (echo "=== BUILD FAILED ===" && \
     echo "=== Build logs ===" && find . -name "*.binlog" -exec dotnet build-server shutdown \; && \
     exit 1)

FROM build AS publish
# Publish with error handling and detailed output
RUN echo "=== Starting publish ===" && \
    if [ -f "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" ]; then \
        dotnet publish "DeliveryValidationService.Api/DeliveryValidationService.Api.csproj" \
            -c Release \
            -o /app/publish \
            /p:UseAppHost=false \
            --no-restore \
            --no-build \
            --verbosity normal; \
    elif [ -f "DeliveryValidationService.Api.csproj" ]; then \
        dotnet publish "DeliveryValidationService.Api.csproj" \
            -c Release \
            -o /app/publish \
            /p:UseAppHost=false \
            --no-restore \
            --no-build \
            --verbosity normal; \
    else \
        echo "=== PROJECT FILE NOT FOUND FOR PUBLISH ===" && exit 1; \
    fi || \
    (echo "=== PUBLISH FAILED ===" && \
     echo "=== Checking publish directory ===" && ls -la /app/ && \
     echo "=== Looking for error logs ===" && find /app -name "*.log" -exec cat {} \; && \
     exit 1)

# Verify published files
RUN echo "=== Verifying published files ===" && \
    ls -la /app/publish && \
    echo "=== Checking for main DLL ===" && \
    ls -la /app/publish/DeliveryValidationService.Api.dll || \
    (echo "=== Main DLL not found! ===" && exit 1)

FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Verify final image contents
RUN echo "=== Final image contents ===" && ls -la

# Health check for Railway
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Use non-root user for security
RUN addgroup --system --gid 1001 dotnetgroup && \
    adduser --system --uid 1001 --ingroup dotnetgroup dotnetuser
USER dotnetuser

ENTRYPOINT ["dotnet", "DeliveryValidationService.Api.dll"]