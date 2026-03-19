# 버전을 .NET 10.0에 맞게 설정합니다.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# 소스 복사 및 빌드
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# 런타임 이미지 설정
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# 업로드된 이미지 보존을 위한 볼륨 폴더 생성
RUN mkdir -p /app/wwwroot/images/avatars

EXPOSE 8080
ENTRYPOINT ["dotnet", "MooldangAPI.dll"]