using System;
using MySqlConnector;

try
{
    // [물멍]: DB 연결 문자열 (지휘관 환경에 맞춰 조정됨)
    string connString = "Server=localhost;User ID=mooldang_guest;Database=mooldang_bot;AllowUserVariables=True";
    using var conn = new MySqlConnection(connString);
    conn.Open();

    // 모든 스트리머의 오염된(복호화 불가) 토큰 초기화
    string sql = "UPDATE core_streamer_profiles SET chzzk_access_token = NULL, chzzk_refresh_token = NULL";
    using var cmd = new MySqlCommand(sql, conn);
    int affected = cmd.ExecuteNonQuery();

    Console.WriteLine($"✅ Successfully cleared corrupted tokens for all streamers. (Affected: {affected} rows)");
    Console.WriteLine("🚀 Now the system will boot without CryptographicException.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error during DB cleaning: {ex.Message}");
}
