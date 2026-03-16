# 1. 빌드 스테이지
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 프로젝트 파일 복사 및 복구
COPY *.csproj ./
RUN dotnet restore

# 나머지 소스 복사 및 빌드
COPY . ./
RUN dotnet publish -c Release -o out

# 2. 실행 스테이지
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# 포트 설정 (물댕님이 사용하시던 3000번)
EXPOSE 3000
ENTRYPOINT ["dotnet", "MooldangAPI.dll"]