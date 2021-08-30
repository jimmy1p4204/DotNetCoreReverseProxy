# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /dotnetcorereverseproxy
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["DotNetCoreReverseProxy.csproj", ""]
RUN dotnet restore "./DotNetCoreReverseProxy.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DotNetCoreReverseProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DotNetCoreReverseProxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DotNetCoreReverseProxy.dll"]
