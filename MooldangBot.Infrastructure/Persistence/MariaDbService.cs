
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Dapper;
using MySqlConnector;
using System;
using System.Data;
using System.Threading.Tasks;

namespace MooldangBot.Infrastructure.Persistence
{
    public class MariaDbService(IConfiguration configuration)
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection") 
                               ?? throw new InvalidOperationException("DB 연결 문자열이 'DefaultConnection' 섹션에 정의되어 있지 않습니다.");

        public IDbConnection CreateConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// [피닉스의 기록]: 발급받은 토큰과 스트리머 정보를 영구 보존합니다.
        /// 이미 존재하는 채널이라면 덮어씁니다(Upsert).
        /// </summary>
        public async Task SaveTokenAsync(string channelId, string channelName, string accessToken, string? refreshToken)
        {
            // DB 생성 로직
            // CREATE DATABASE IF NOT EXISTS chzzk_matrix;
            // USE chzzk_matrix;
            
            // CREATE TABLE IF NOT EXISTS core_streamer_tokens(
            //     ChannelId VARCHAR(100) PRIMARY KEY, /* 치지직 채널 ID (고유 식별자) */
            //     ChannelName VARCHAR(100) NOT NULL,  /* 스트리머 닉네임 */
            //     AccessToken TEXT NOT NULL,          /* 인증 토큰 */
            //     RefreshToken TEXT,                  /* 갱신용 토큰 (향후 연장 시 사용) */
            //     UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            // );

            using var db = CreateConnection();

            string sql = @"
            INSERT INTO core_streamer_tokens (ChannelId, ChannelName, AccessToken, RefreshToken, UpdatedAt)
            VALUES (@ChannelId, @ChannelName, @AccessToken, @RefreshToken, NOW())
            ON DUPLICATE KEY UPDATE 
                ChannelName = @ChannelName,
                AccessToken = @AccessToken,
                RefreshToken = @RefreshToken,
                UpdatedAt = NOW();";

            await db.ExecuteAsync(sql, new
            {
                ChannelId = channelId,
                ChannelName = channelName,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });

            Console.WriteLine($"[피닉스] {channelName}님의 파동이 기록소에 안전하게 보존되었습니다.");
        }

        /// <summary>
        /// [피닉스의 회상]: 서버가 재시작되었을 때, 기록소에서 마지막 토큰을 꺼내옵니다.
        /// </summary>
        public async Task<string?> GetLatestAccessTokenAsync()
        {
            using var db = CreateConnection();
            // 단일 스트리머 로컬용이므로 가장 최근에 업데이트된 토큰 하나만 가져옵니다.
            string sql = "SELECT AccessToken FROM core_streamer_tokens ORDER BY UpdatedAt DESC LIMIT 1;";
            return await db.QueryFirstOrDefaultAsync<string>(sql);
        }
    }
}
