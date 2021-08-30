# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /dotnetcorereverseproxy
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src/dotnetcorereverseproxy
COPY ["DotNetCoreReverseProxy.csproj", ""]
RUN dotnet restore "./DotNetCoreReverseProxy.csproj"
COPY . .
WORKDIR "/src/dotnetcorereverseproxy"
RUN dotnet build "DotNetCoreReverseProxy.csproj" -c Release -o /app/dotnetcorereverseproxy/build

FROM build AS publish
RUN dotnet publish "DotNetCoreReverseProxy.csproj" -c Release -o /app/dotnetcorereverseproxy/publish

FROM base AS final
WORKDIR /app/dotnetcorereverseproxy
COPY --from=publish /app/dotnetcorereverseproxy/publish .
ENTRYPOINT ["dotnet", "DotNetCoreReverseProxy.dll"]
