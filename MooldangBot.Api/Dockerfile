# [물멍]: 차세대 MooldangBot 엔진 빌드 스테이션 (net10.0 + Alpine)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# [오시리스의 가호]: 의존성 레이어 최적화
COPY ["MooldangBot.Api/MooldangBot.Api.csproj", "MooldangBot.Api/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Presentation/MooldangBot.Presentation.csproj", "MooldangBot.Presentation/"]
COPY ["MooldangBot.Contracts/MooldangBot.Contracts.csproj", "MooldangBot.Contracts/"]
COPY ["MooldangBot.Verifier/MooldangBot.Verifier.csproj", "MooldangBot.Verifier/"]
COPY ["MooldangBot.Modules.SongBook/MooldangBot.Modules.SongBook.csproj", "MooldangBot.Modules.SongBook/"]
COPY ["MooldangBot.Modules.Roulette/MooldangBot.Modules.Roulette.csproj", "MooldangBot.Modules.Roulette/"]
COPY ["MooldangBot.Modules.Point/MooldangBot.Modules.Point.csproj", "MooldangBot.Modules.Point/"]
COPY ["MooldangBot.Modules.Commands/MooldangBot.Modules.Commands.csproj", "MooldangBot.Modules.Commands/"]

RUN dotnet restore "MooldangBot.Api/MooldangBot.Api.csproj"
COPY . .

# Verifier 빌드 추가 (필요 시 운영 환경에서 자가진단 가능하도록)
WORKDIR "/src/MooldangBot.Verifier"
RUN dotnet build "MooldangBot.Verifier.csproj" -c Release -o /app/verifier

WORKDIR "/src/MooldangBot.Api"
RUN dotnet build "MooldangBot.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MooldangBot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MooldangBot.Api.dll"]
