# [v2.4.4] 함대 전개 안정화: ChzzkAPI 전 전용 Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. 프로젝트 파일 복사
COPY ["MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj", "MooldangBot.ChzzkAPI/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]

# 2. 패키지 복원
RUN dotnet restore "MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj"

# 3. 소스 코드 복사 및 빌드
COPY . .
WORKDIR "/src/MooldangBot.ChzzkAPI"
RUN dotnet build "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/build

# 4. 게시
FROM build AS publish
RUN dotnet publish "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 5. 런타임 이미지
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 메트릭 서버용 포트 개방
EXPOSE 8080

ENTRYPOINT ["dotnet", "MooldangBot.ChzzkAPI.dll"]
