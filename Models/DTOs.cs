namespace MooldangAPI.Models
{
    public class SetupRequest
    {
        public string ChzzkUid { get; set; } = "";
    }

    public class SettingsUpdateRequest
    {
        public string SongCommand { get; set; } = "!신청";
        public int SongCheesePrice { get; set; } = 0;
        public string OmakaseCommand { get; set; } = "!오마카세";
        public int OmakaseCheesePrice { get; set; } = 1000;
        public string DesignSettingsJson { get; set; } = "{}";
    }
}
