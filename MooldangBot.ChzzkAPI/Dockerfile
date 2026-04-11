# 1. Runtime Base Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# [오시리스의 보철]: 헬스체크용 curl 설치 및 캐시 정리
RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# 2. Build Environment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and Restore
COPY ["MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj", "MooldangBot.ChzzkAPI/"]
COPY ["MooldangBot.ChzzkAPI.Contracts/MooldangBot.ChzzkAPI.Contracts.csproj", "MooldangBot.ChzzkAPI.Contracts/"]
RUN dotnet restore "MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/MooldangBot.ChzzkAPI"
RUN dotnet build "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/build

# 3. Publish
FROM build AS publish
RUN dotnet publish "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MooldangBot.ChzzkAPI.dll"]
