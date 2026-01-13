namespace UtilityPlatform.Core.PowerToys;

public record PowerToySettingDefinition
(
    string Key,
    string DisplayName,
    string Description,
    PowerToySettingType SettingType,
    object? DefaultValue
);

