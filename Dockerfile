# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY SportsAPP/SportsAPP.csproj SportsAPP/
RUN dotnet restore "SportsAPP/SportsAPP.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SportsAPP"
RUN dotnet build "SportsAPP.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "SportsAPP.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "SportsAPP.dll"]
