# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApiDemo.Api/ApiDemo.Api.csproj", "ApiDemo.Api/"]
RUN dotnet restore "ApiDemo.Api/ApiDemo.Api.csproj"
COPY . .
WORKDIR "/src/ApiDemo.Api"
RUN dotnet build "ApiDemo.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiDemo.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiDemo.Api.dll"]
