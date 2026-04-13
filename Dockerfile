# ------------------------------------------
# ?룛截?Build Stage
# ------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# EF Core ?꾧뎄 ?ㅼ튂 (留덉씠洹몃젅?댁뀡 踰덈뱾 ?앹꽦??
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# 1. ?꾨줈?앺듃 ?뚯씪 蹂듭궗 (罹먯떆 ?⑥쑉??
COPY ["MooldangBot.Api/MooldangBot.Api.csproj", "MooldangBot.Api/"]
COPY ["MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj", "MooldangBot.ChzzkAPI/"]
COPY ["MooldangBot.Domain/MooldangBot.Domain.csproj", "MooldangBot.Domain/"]
COPY ["MooldangBot.Application/MooldangBot.Application.csproj", "MooldangBot.Application/"]
COPY ["MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj", "MooldangBot.Infrastructure/"]
COPY ["MooldangBot.Presentation/MooldangBot.Presentation.csproj", "MooldangBot.Presentation/"]
COPY ["MooldangBot.Cli/MooldangBot.Cli.csproj", "MooldangBot.Cli/"]
COPY ["MooldangBot.Contracts/MooldangBot.Contracts.csproj", "MooldangBot.Contracts/"]
COPY ["MooldangBot.Verifier/MooldangBot.Verifier.csproj", "MooldangBot.Verifier/"]
COPY ["MooldangBot.Modules.SongBook/MooldangBot.Modules.SongBook.csproj", "MooldangBot.Modules.SongBook/"]
COPY ["MooldangBot.Modules.Roulette/MooldangBot.Modules.Roulette.csproj", "MooldangBot.Modules.Roulette/"]
COPY ["MooldangBot.Modules.Point/MooldangBot.Modules.Point.csproj", "MooldangBot.Modules.Point/"]
COPY ["MooldangBot.Modules.Commands/MooldangBot.Modules.Commands.csproj", "MooldangBot.Modules.Commands/"]
COPY ["MooldangAPI.sln", "./"]

# 2. ?⑦궎吏 蹂듭썝
RUN dotnet restore "MooldangBot.Api/MooldangBot.Api.csproj"
RUN dotnet restore "MooldangBot.ChzzkAPI/MooldangBot.ChzzkAPI.csproj"
RUN dotnet restore "MooldangBot.Cli/MooldangBot.Cli.csproj"
RUN dotnet restore "MooldangBot.Infrastructure/MooldangBot.Infrastructure.csproj"
RUN dotnet restore "MooldangBot.Verifier/MooldangBot.Verifier.csproj"

# 3. ?뚯뒪 肄붾뱶 ?꾩껜 蹂듭궗 諛?鍮뚮뱶
COPY . .
WORKDIR "/src/MooldangBot.Api"
RUN dotnet build "MooldangBot.Api.csproj" -c Release -o /app/build/api
WORKDIR "/src/MooldangBot.ChzzkAPI"
RUN dotnet build "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/build/chzzk
WORKDIR "/src/MooldangBot.Cli"
RUN dotnet build "MooldangBot.Cli.csproj" -c Release -o /app/build/cli
WORKDIR "/src/MooldangBot.Verifier"
RUN dotnet build "MooldangBot.Verifier.csproj" -c Release -o /app/build/verifier

# 5. 寃뚯떆(Publish)
FROM build AS publish
WORKDIR "/src/MooldangBot.Api"
RUN dotnet publish "MooldangBot.Api.csproj" -c Release -o /app/publish/api /p:UseAppHost=false
WORKDIR "/src/MooldangBot.ChzzkAPI"
RUN dotnet publish "MooldangBot.ChzzkAPI.csproj" -c Release -o /app/publish/chzzk /p:UseAppHost=false
WORKDIR "/src/MooldangBot.Cli"
RUN dotnet publish "MooldangBot.Cli.csproj" -c Release -o /app/publish/cli /p:UseAppHost=false
WORKDIR "/src/MooldangBot.Verifier"
RUN dotnet publish "MooldangBot.Verifier.csproj" -c Release -o /app/publish/verifier /p:UseAppHost=false

# ------------------------------------------
# ?? Runtime Stage
# ------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
# efbundle ???MooldangBot.Cli.dll???ъ슜?섎?濡?踰덈뱾 蹂듭궗 遺덊븘??

# ?낅줈???곗씠???대뜑 ?앹꽦 諛?沅뚰븳 ?ㅼ젙
RUN mkdir -p /app/wwwroot/images/avatars && \
    mkdir -p /app/db_data && \
    chmod -R 777 /app/wwwroot/images/avatars

EXPOSE 8080
ENTRYPOINT ["dotnet", "api/MooldangBot.Api.dll"]