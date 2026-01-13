using System.Text.Json;
using UtilityPlatform.Core.Settings;

namespace UtilityPlatform.App.Settings;

public class JsonPowerToySettingsStore : IPowerToySettingsStore
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public JsonPowerToySettingsStore()
    {
        var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var utilityDirectory = Path.Combine(appDataDirectory, "UtilityPlatform");

        Directory.CreateDirectory(utilityDirectory);

        _settingsPath = Path.Combine(utilityDirectory, "settings.json");
    }

    public async Task<PlatformSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_settingsPath) is false)
        {
            return new();
        }

        await using var stream = File.OpenRead(_settingsPath);

        var settings = await JsonSerializer.DeserializeAsync<PlatformSettings>(stream, _serializerOptions, cancellationToken);

        return settings ?? new();
    }

    public async Task SaveAsync(PlatformSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings, nameof(settings));

        await using var stream = File.Create(_settingsPath);

        await JsonSerializer.SerializeAsync(stream, settings, _serializerOptions, cancellationToken);
    }
}

