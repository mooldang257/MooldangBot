using Xunit;
using MooldangAPI.Models;

namespace MooldangAPI.Tests
{
    public class AvatarSettingTests
    {
        [Fact]
        public void AvatarSetting_ShouldHaveCorrectDefaults()
        {
            // Arrange
            var setting = new AvatarSetting { ChzzkUid = "test_uid" };

            // Act & Assert
            Assert.Equal("test_uid", setting.ChzzkUid);
            Assert.True(setting.IsEnabled, "아바타 기능은 기본적으로 활성화되어야 합니다.");
            Assert.True(setting.ShowNickname, "닉네임 표시는 기본적으로 활성화되어야 합니다.");
            Assert.True(setting.ShowChat, "채팅 표시는 기본적으로 활성화되어야 합니다.");
            Assert.Equal(60, setting.DisappearTimeSeconds);
            
            // 이미지들은 기본적으로 비어 있어야 함
            Assert.Null(setting.NormalAvatarUrl);
            Assert.Null(setting.SubscriberAvatarUrl);
            Assert.Null(setting.Tier2AvatarUrl);
        }
    }
}
