# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/GrpcChat.Contracts/GrpcChat.Contracts.csproj", "GrpcChat.Contracts/"]
COPY ["src/GrpcChat.Server/GrpcChat.Server.csproj", "GrpcChat.Server/"]

# Restore dependencies
RUN dotnet restore "GrpcChat.Server/GrpcChat.Server.csproj"

# Copy source code
COPY ["src/GrpcChat.Contracts/", "GrpcChat.Contracts/"]
COPY ["src/GrpcChat.Server/", "GrpcChat.Server/"]

# Build
WORKDIR /src/GrpcChat.Server
RUN dotnet build "GrpcChat.Server.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GrpcChat.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=publish /app/publish .

# Expose gRPC port
EXPOSE 5001

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "GrpcChat.Server.dll"]
