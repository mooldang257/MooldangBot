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
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Presentation/MooldangBot.Presentation.csproj", "MooldangBot.Presentation/"]
COPY ["MooldangAPI.sln", "./"]

# 2. 패키지 복원
RUN dotnet restore "MooldangBot.Api/MooldangBot.Api.csproj" -r linux-x64
RUN dotnet restore "MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj" -r linux-x64

# 3. 소스 코드 전체 복사 및 빌드
COPY . .
WORKDIR "/src/MooldangBot.Api"
RUN dotnet build "MooldangBot.Api.csproj" -c Release -o /app/build

# 4. 마이그레이션 번들 생성 (MooldangBot.Api에서 실행)
RUN dotnet ef migrations bundle -o /app/efbundle \
    --runtime linux-x64 \
    --self-contained \
    -p ../MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj \
    -s MooldangBot.Api.csproj

# 5. 게시(Publish)
FROM build AS publish
RUN dotnet publish "MooldangBot.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ------------------------------------------
# 🚀 Runtime Stage
# ------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /app/efbundle .

# 업로드/데이터 폴더 생성 및 권한 설정
RUN mkdir -p /app/wwwroot/images/avatars && \
    mkdir -p /app/db_data && \
    chmod -R 777 /app/wwwroot/images/avatars

EXPOSE 8080
ENTRYPOINT ["dotnet", "MooldangBot.Api.dll"]