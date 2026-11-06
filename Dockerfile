# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY ["Secondhand-Marketplace.sln", "./"]
COPY ["src/SecondHandMarketplace.Web/SecondHandMarketplace.Web.csproj", "src/SecondHandMarketplace.Web/"]
COPY ["tests/SecondHandMarketplace.Tests/SecondHandMarketplace.Tests.csproj", "tests/SecondHandMarketplace.Tests/"]

# Restore NuGet dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Publish Web project in Release mode
WORKDIR /app/src/SecondHandMarketplace.Web
RUN dotnet publish -c Release -o /app/out

# Stage 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy compiled files from build stage
COPY --from=build /app/out ./

# Ensure wwwroot/uploads directory exists
RUN mkdir -p /app/wwwroot/uploads

# ASP.NET Core in .NET 8/9/10 listens on port 8080 by default
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SecondHandMarketplace.Web.dll"]
