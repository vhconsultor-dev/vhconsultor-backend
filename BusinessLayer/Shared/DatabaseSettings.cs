namespace BusinessLayer.Shared;

public class DatabaseSettings
{
    public bool UseLegacyDatabase { get; set; } = true;
    public string LegacyConnectionString { get; set; } = string.Empty;
    public string NewConnectionString { get; set; } = string.Empty;
} 