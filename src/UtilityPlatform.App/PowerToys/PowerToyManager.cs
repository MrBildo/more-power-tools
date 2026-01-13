using UtilityPlatform.Core.Notifications;
using UtilityPlatform.Core.PowerToys;
using UtilityPlatform.Core.Settings;

namespace UtilityPlatform.App.PowerToys;

public record PowerToyListItem(string Id, string DisplayName, string Description, bool IsEnabled);

public record PowerToyDetails
(
    PowerToyDescriptor Descriptor,
    bool IsEnabled,
    IReadOnlyList<PowerToySettingDefinition> SettingDefinitions,
    IReadOnlyDictionary<string, string?> Settings
);

public class PowerToyManager
(
    IReadOnlyList<IPowerToy> powerToys,
    IPowerToySettingsStore settingsStore,
    IPowerToyNotificationService notificationService
)
{
    private readonly IReadOnlyDictionary<string, IPowerToy> _powerToysById = powerToys
        .GroupBy(pt => pt.Descriptor.Id)
        .ToDictionary(g => g.Key, g => g.First());

    private readonly IPowerToySettingsStore _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    private readonly IPowerToyNotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

    private readonly SemaphoreSlim _gate = new(1, 1);
    private PlatformSettings _settings = new();

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);

            foreach (var powerToy in _powerToysById.Values)
            {
                EnsurePowerToyHasConfiguration(powerToy);
            }

            await _settingsStore.SaveAsync(_settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        foreach (var (id, powerToy) in _powerToysById)
        {
            if (GetPowerToyConfigurationSnapshot(id).IsEnabled)
            {
                await StartPowerToyAsync(id, powerToy, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public IReadOnlyList<PowerToyListItem> GetPowerToys()
        => _powerToysById.Values
            .Select(pt => CreateListItem(pt))
                .OrderBy(pt => pt.DisplayName)
                    .ToList();

    public PowerToyDetails GetPowerToyDetails(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));

        var powerToy = GetPowerToy(id);
        var configuration = GetPowerToyConfigurationSnapshot(id);

        return new
        (
            powerToy.Descriptor,
            configuration.IsEnabled,
            powerToy.SettingDefinitions,
            configuration.Settings
        );
    }

    public async Task SetEnabledAsync(string id, bool isEnabled, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));

        var powerToy = GetPowerToy(id);
        var shouldStart = false;
        var shouldStop = false;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            EnsurePowerToyHasConfiguration(powerToy);

            var currentEnabled = _settings.PowerToys[id].IsEnabled;

            if (currentEnabled == isEnabled)
            {
                return;
            }

            _settings.PowerToys[id].IsEnabled = isEnabled;
            await _settingsStore.SaveAsync(_settings, cancellationToken).ConfigureAwait(false);

            shouldStart = isEnabled;
            shouldStop = isEnabled is false;
        }
        finally
        {
            _gate.Release();
        }

        if (shouldStart)
        {
            await StartPowerToyAsync(id, powerToy, cancellationToken).ConfigureAwait(false);
        }

        if (shouldStop)
        {
            await StopPowerToyAsync(id, powerToy, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpdateSettingAsync(string id, string key, string? value, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        var powerToy = GetPowerToy(id);
        var shouldNotifyToy = false;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            EnsurePowerToyHasConfiguration(powerToy);

            _settings.PowerToys[id].Settings[key] = value;
            await _settingsStore.SaveAsync(_settings, cancellationToken).ConfigureAwait(false);

            shouldNotifyToy = _settings.PowerToys[id].IsEnabled;
        }
        finally
        {
            _gate.Release();
        }

        if (shouldNotifyToy)
        {
            var context = CreateContext(id);
            await powerToy.OnSettingsChangedAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private PowerToyListItem CreateListItem(IPowerToy powerToy)
    {
        var configuration = GetPowerToyConfigurationSnapshot(powerToy.Descriptor.Id);

        return new
        (
            powerToy.Descriptor.Id,
            powerToy.Descriptor.DisplayName,
            powerToy.Descriptor.Description,
            configuration.IsEnabled
        );
    }

    private PowerToyContext CreateContext(string id)
        => new(_notificationService, GetPowerToyConfigurationSnapshot(id));

    private async Task StartPowerToyAsync(string id, IPowerToy powerToy, CancellationToken cancellationToken)
    {
        try
        {
            var context = CreateContext(id);
            await powerToy.StartAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(powerToy.Descriptor.DisplayName, ex.Message);
        }
    }

    private async Task StopPowerToyAsync(string id, IPowerToy powerToy, CancellationToken cancellationToken)
    {
        try
        {
            await powerToy.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError(powerToy.Descriptor.DisplayName, ex.Message);
        }
    }

    private IPowerToy GetPowerToy(string id)
        => _powerToysById.TryGetValue(id, out var powerToy)
            ? powerToy
            : throw new InvalidOperationException($"Power toy '{id}' not found.");

    private PowerToyConfiguration GetPowerToyConfigurationSnapshot(string id)
    {
        _gate.Wait();

        try
        {
            return _settings.PowerToys.TryGetValue(id, out var configuration)
                ? new()
                {
                    IsEnabled = configuration.IsEnabled,
                    Settings = new Dictionary<string, string?>(configuration.Settings)
                }
                : new();
        }
        finally
        {
            _gate.Release();
        }
    }

    private void EnsurePowerToyHasConfiguration(IPowerToy powerToy)
    {
        if (_settings.PowerToys.ContainsKey(powerToy.Descriptor.Id) is false)
        {
            _settings.PowerToys[powerToy.Descriptor.Id] = new()
            {
                IsEnabled = false
            };
        }

        foreach (var setting in powerToy.SettingDefinitions)
        {
            if (_settings.PowerToys[powerToy.Descriptor.Id].Settings.ContainsKey(setting.Key))
            {
                continue;
            }

            _settings.PowerToys[powerToy.Descriptor.Id].Settings[setting.Key] = setting.DefaultValue?.ToString();
        }
    }
}

