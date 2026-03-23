# 버전을 .NET 10.0에 맞게 설정합니다.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# EF Core 도구 설치 (Bundle 생성을 위해 필요)
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# 소스 복사 및 빌드
COPY . ./
RUN dotnet restore

# 마이그레이션 번들 생성 (서버에서 수동 실행 가능하도록)
RUN dotnet ef migrations bundle -o efbundle --runtime linux-x64 --self-contained

RUN dotnet publish -c Release -o out

# 런타임 이미지 설정
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
COPY --from=build /app/efbundle .

# 업로드된 이미지 보존을 위한 볼륨 폴더 생성
RUN mkdir -p /app/wwwroot/images/avatars

EXPOSE 8080
ENTRYPOINT ["dotnet", "MooldangAPI.dll"]