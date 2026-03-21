namespace MooldangAPI.Models
{
    public class SetupRequest
    {
        public string ChzzkUid { get; set; } = "";
    }

    public class SonglistSettingsUpdateRequest
    {
        public string SongCommand { get; set; } = "!신청";
        public int SongCheesePrice { get; set; } = 0;
        public string DesignSettingsJson { get; set; } = "{}";
        public List<OmakaseDto> Omakases { get; set; } = new();
    }

    public class OmakaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "오마카세";
        public string Command { get; set; } = "!물마카세";
        public string Icon { get; set; } = "🍣";
        public int Price { get; set; } = 1000;
    }
}
