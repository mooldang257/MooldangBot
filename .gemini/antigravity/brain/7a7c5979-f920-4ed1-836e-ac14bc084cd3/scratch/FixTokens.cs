using System;
using MySqlConnector;

try
{
    string connString = "Server=localhost;User ID=mooldang_guest;Database=mooldang_bot;AllowUserVariables=True";
    using var conn = new MySqlConnection(connString);
    conn.Open();

    string sql = "UPDATE core_streamer_profiles SET chzzk_access_token = NULL, chzzk_refresh_token = NULL WHERE chzzk_uid = 'c74931e68d4d90ce9f11d6f343c1d54c'";
    using var cmd = new MySqlCommand(sql, conn);
    int affected = cmd.ExecuteNonQuery();

    Console.WriteLine($"✅ Successfully cleared tokens for 'c74931e68d4d90ce9f11d6f343c1d54c'. Affected rows: {affected}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
