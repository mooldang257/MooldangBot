# ------------------------------------------
# 🏗️ Build Stage
# ------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and Restore
COPY ["MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj", "MooldangBot.ChzzkAPI/"]
COPY ["MooldangBot.Contracts/MooldangBot.Contracts.csproj", "MooldangBot.Contracts/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Verifier/MooldangBot.Verifier.csproj", "MooldangBot.Verifier/"]

RUN dotnet restore "MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj"

# Copy source and build
COPY . .

# Verifier 빌드 추가 (필요 시 운영 환경에서 자기진단 가능하도록)
WORKDIR "/src/MooldangBot.Verifier"
RUN dotnet build "MooldangBot.Verifier.csproj" -c Release -o /app/verifier

WORKDIR "/src/MooldangBot.ChzzkAPI"
RUN dotnet build "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/build

# 3. Publish
FROM build AS publish
RUN dotnet publish "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /app/verifier ./verifier
ENTRYPOINT ["dotnet", "MooldangBot.ChzzkAPI.dll"]
