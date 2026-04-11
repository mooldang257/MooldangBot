# [물멍]: 차세대 MooldangBot 엔진 빌드 스테이션 (net10.0 + Alpine)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# [오시리스의 열쇠]: 의존성 레이어 최적화
COPY ["MooldangBot.Api/MooldangBot.Api.csproj", "MooldangBot.Api/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Presentation/MooldangBot.Presentation.csproj", "MooldangBot.Presentation/"]
COPY ["MooldangBot.ChzzkAPI.Contracts/MooldangBot.ChzzkAPI.Contracts.csproj", "MooldangBot.ChzzkAPI.Contracts/"]

RUN dotnet restore "MooldangBot.Api/MooldangBot.Api.csproj"
COPY . .
WORKDIR "/src/MooldangBot.Api"
RUN dotnet build "MooldangBot.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MooldangBot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MooldangBot.Api.dll"]
