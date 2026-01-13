namespace UtilityPlatform.Core.PowerToys;

public interface IPowerToy
{
    PowerToyDescriptor Descriptor { get; }

    IReadOnlyList<PowerToySettingDefinition> SettingDefinitions { get; }

    Task StartAsync(PowerToyContext context, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    Task OnSettingsChangedAsync(PowerToyContext context, CancellationToken cancellationToken);
}

