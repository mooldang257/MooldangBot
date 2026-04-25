# [물멍]: 차세대 MooldangBot 엔진 빌드 스테이션 (net10.0 + Alpine)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 8080
# [v19.5] 리눅스 타임존 데이터베이스 설치
RUN apk add --no-cache tzdata

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# [오시리스의 가호]: 의존성 레이어 최적화
COPY ["MooldangBot.Api/MooldangBot.Api.csproj", "MooldangBot.Api/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Verifier/MooldangBot.Verifier.csproj", "MooldangBot.Verifier/"]
COPY ["MooldangBot.Modules.SongBook/MooldangBot.Modules.SongBook.csproj", "MooldangBot.Modules.SongBook/"]
COPY ["MooldangBot.Modules.Roulette/MooldangBot.Modules.Roulette.csproj", "MooldangBot.Modules.Roulette/"]
COPY ["MooldangBot.Modules.Point/MooldangBot.Modules.Point.csproj", "MooldangBot.Modules.Point/"]
COPY ["MooldangBot.Modules.Commands/MooldangBot.Modules.Commands.csproj", "MooldangBot.Modules.Commands/"]
COPY ["MooldangBot.Cli/MooldangBot.Cli.csproj", "MooldangBot.Cli/"]

RUN dotnet restore "MooldangBot.Api/MooldangBot.Api.csproj"
RUN dotnet restore "MooldangBot.Cli/MooldangBot.Cli.csproj"
COPY . .

# Verifier 빌드 추가 (필요 시 운영 환경에서 자가진단 가능하도록)
WORKDIR "/src/MooldangBot.Verifier"
RUN dotnet build "MooldangBot.Verifier.csproj" -c Release -o /app/verifier

WORKDIR "/src/MooldangBot.Api"
RUN dotnet build "MooldangBot.Api.csproj" -c Release -o /app/build

WORKDIR /src
FROM build AS publish
RUN dotnet publish "MooldangBot.Api/MooldangBot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish "MooldangBot.Cli/MooldangBot.Cli.csproj" -c Release -o /app/cli /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/cli ./cli
ENTRYPOINT ["dotnet", "MooldangBot.Api.dll"]
