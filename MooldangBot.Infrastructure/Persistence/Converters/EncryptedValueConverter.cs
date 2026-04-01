using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MooldangBot.Infrastructure.Persistence.Converters;

/// <summary>
/// [v4.0] 수호자의 장막: EF Core 수준에서 민감 데이터를 암호화/복호화하는 컨버터입니다.
/// </summary>
public class EncryptedValueConverter : ValueConverter<string, string>
{
    private const string Prefix = "ENC:";

    public EncryptedValueConverter(IDataProtector protector) 
        : base(
            // [최적화]: null 체크 후 암호화 연산 1회만 수행
            v => v == null ? null! : $"{Prefix}{protector.Protect(v)}",
            v => Decrypt(v, protector))
    { }

    private static string Decrypt(string value, IDataProtector protector)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // [v4.0 전환 전략]: 접두사가 있으면 복호화 시도
        if (value.StartsWith(Prefix))
        {
            // C# 8.0+ Range 연산자 활용
            var protectedData = value[Prefix.Length..];
            // 복호화 실패 시 CryptographicException이 발생하며, 이는 보안상 권장되는 동작입니다.
            return protector.Unprotect(protectedData);
        }

        // 접두사가 없으면 전환기 평문으로 간주 (Graceful Migration)
        return value;
    }
}
