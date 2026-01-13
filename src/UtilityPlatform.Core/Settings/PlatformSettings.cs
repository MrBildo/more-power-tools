namespace UtilityPlatform.Core.Settings;

public sealed class PlatformSettings
{
    public Dictionary<string, PowerToyConfiguration> PowerToys { get; init; } = [];
}

public sealed class PowerToyConfiguration
{
    public bool IsEnabled { get; set; }

    public Dictionary<string, string?> Settings { get; init; } = [];
}

