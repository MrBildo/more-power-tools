namespace UtilityPlatform.Core.Settings;

public interface IPowerToySettingsStore
{
    Task<PlatformSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(PlatformSettings settings, CancellationToken cancellationToken);
}

