namespace UtilityPlatform.Core.Settings;

public static class PowerToyConfigurationExtensions
{
    public static string? GetString(this PowerToyConfiguration configuration, string key)
        => configuration.Settings.TryGetValue(key, out var value) ? value : null;

    public static string GetString(this PowerToyConfiguration configuration, string key, string defaultValue)
        => configuration.GetString(key) ?? defaultValue;

    public static bool GetBoolean(this PowerToyConfiguration configuration, string key, bool defaultValue)
        => bool.TryParse(configuration.GetString(key), out var value) ? value : defaultValue;

    public static int GetInt32(this PowerToyConfiguration configuration, string key, int defaultValue)
        => int.TryParse(configuration.GetString(key), out var value) ? value : defaultValue;
}

