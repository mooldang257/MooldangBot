# ------------------------------------------
# 🏗️ Build Stage
# ------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# EF Core 도구 설치 (마이그레이션 번들 생성용)
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# 1. 프로젝트 파일 복사 (캐시 효율화)
COPY ["MooldangBot.Api/MooldangBot.Api.csproj", "MooldangBot.Api/"]
COPY ["MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj", "MooldangBot.ChzzkAPI/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Presentation/MooldangBot.Presentation.csproj", "MooldangBot.Presentation/"]
COPY ["MooldangBot.Cli/MooldangBot.Cli.csproj", "MooldangBot.Cli/"]
COPY ["MooldangBot.Contracts/MooldangBot.Contracts.csproj", "MooldangBot.Contracts/"]
COPY ["MooldangBot.Modules.SongBook/MooldangBot.Modules.SongBook.csproj", "MooldangBot.Modules.SongBook/"]
COPY ["MooldangBot.Modules.Roulette/MooldangBot.Modules.Roulette.csproj", "MooldangBot.Modules.Roulette/"]
COPY ["MooldangBot.Modules.Point/MooldangBot.Modules.Point.csproj", "MooldangBot.Modules.Point/"]
COPY ["MooldangBot.Modules.Commands/MooldangBot.Modules.Commands.csproj", "MooldangBot.Modules.Commands/"]
COPY ["MooldangAPI.sln", "./"]

# 2. 패키지 복원
RUN dotnet restore "MooldangBot.Api/MooldangBot.Api.csproj"
RUN dotnet restore "MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj"
RUN dotnet restore "MooldangBot.Cli/MooldangBot.Cli.csproj"
RUN dotnet restore "MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj"

# 3. 소스 코드 전체 복사 및 빌드
COPY . .
WORKDIR "/src/MooldangBot.Api"
RUN dotnet build "MooldangBot.Api.csproj" -c Release -o /app/build/api
WORKDIR "/src/MooldangBot.ChzzkAPI"
RUN dotnet build "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/build/chzzk
WORKDIR "/src/MooldangBot.Cli"
RUN dotnet build "MooldangBot.Cli.csproj" -c Release -o /app/build/cli

# 5. 게시(Publish)
FROM build AS publish
WORKDIR "/src/MooldangBot.Api"
RUN dotnet publish "MooldangBot.Api.csproj" -c Release -o /app/publish/api /p:UseAppHost=false
WORKDIR "/src/MooldangBot.ChzzkAPI"
RUN dotnet publish "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/publish/chzzk /p:UseAppHost=false
WORKDIR "/src/MooldangBot.Cli"
RUN dotnet publish "MooldangBot.Cli.csproj" -c Release -o /app/publish/cli /p:UseAppHost=false

# ------------------------------------------
# 🚀 Runtime Stage
# ------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
# efbundle 대신 MooldangBot.Cli.dll을 사용하므로 번들 복사 불필요

# 업로드/데이터 폴더 생성 및 권한 설정
RUN mkdir -p /app/wwwroot/images/avatars && \
    mkdir -p /app/db_data && \
    chmod -R 777 /app/wwwroot/images/avatars

EXPOSE 8080
ENTRYPOINT ["dotnet", "api/MooldangBot.Api.dll"]