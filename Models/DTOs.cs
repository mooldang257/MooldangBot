namespace MooldangAPI.Models
{
    public class SetupRequest
    {
        public string ChzzkUid { get; set; } = "";
    }

    public class SonglistSettingsUpdateRequest
    {
        public string SongCommand { get; set; } = "!신청";
        public List<SongRequestCommandDto> SongRequestCommands { get; set; } = new();
        public int SongCheesePrice { get; set; } = 0;
        public string DesignSettingsJson { get; set; } = "{}";
        public List<OmakaseDto> Omakases { get; set; } = new();
    }

    public class SongRequestCommandDto
    {
        public string Keyword { get; set; } = "!신청";
        public int Price { get; set; } = 0;
    }

    public class OmakaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "오마카세";
        public string Command { get; set; } = "!물마카세";
        public string Icon { get; set; } = "🍣";
        public int Price { get; set; } = 1000;
    }

    public class PeriodicMessageDto
    {
        public int Id { get; set; }
        public int IntervalMinutes { get; set; }
        public string Message { get; set; } = "";
        public bool IsEnabled { get; set; }
    }

    public class OverlayPresetDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ConfigJson { get; set; } = "{}";
        public DateTime UpdatedAt { get; set; }
    }

    public class SharedComponentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ConfigJson { get; set; } = "{}";
    }
}
