# ------------------------------------------
# ?룛截?Build Stage
# ------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS base
WORKDIR /app
EXPOSE 8080
# [v19.5] 리눅스 타임존 데이터베이스 설치
RUN apt-get update && apt-get install -y tzdata && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and Restore
COPY ["MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj", "MooldangBot.ChzzkAPI/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Verifier/MooldangBot.Verifier.csproj", "MooldangBot.Verifier/"]
COPY ["MooldangBot.Modules.SongBook/MooldangBot.Modules.SongBook.csproj", "MooldangBot.Modules.SongBook/"]
COPY ["MooldangBot.Modules.Roulette/MooldangBot.Modules.Roulette.csproj", "MooldangBot.Modules.Roulette/"]
COPY ["MooldangBot.Modules.Point/MooldangBot.Modules.Point.csproj", "MooldangBot.Modules.Point/"]
COPY ["MooldangBot.Modules.Commands/MooldangBot.Modules.Commands.csproj", "MooldangBot.Modules.Commands/"]
COPY ["MooldangBot.Cli/MooldangBot.Cli.csproj", "MooldangBot.Cli/"]

RUN dotnet restore "MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj"
RUN dotnet restore "MooldangBot.Cli/MooldangBot.Cli.csproj"

# Copy source and build
COPY . .

# Verifier 鍮뚮뱶 異붽? (?꾩슂 ???댁쁺 ?섍꼍?먯꽌 ?먭린吏꾨떒 媛?ν븯?꾨줉)
WORKDIR "/src/MooldangBot.Verifier"
RUN dotnet build "MooldangBot.Verifier.csproj" -c Release -o /app/verifier

WORKDIR /src
FROM build AS publish
RUN dotnet publish "MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false
RUN dotnet publish "MooldangBot.Cli/MooldangBot.Cli.csproj" -c Release -o /app/cli /p:UseAppHost=false

# 4. Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /app/verifier ./verifier
COPY --from=publish /app/cli ./cli
ENTRYPOINT ["dotnet", "MooldangBot.ChzzkAPI.dll"]
