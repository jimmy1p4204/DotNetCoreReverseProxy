# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /dotnetcorereverseproxy

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /dotnetcorereverseproxy
COPY --from=build-env /dotnetcorereverseproxy/out .
ENTRYPOINT ["dotnet", "DotNetCoreReverseProxy.dll"]