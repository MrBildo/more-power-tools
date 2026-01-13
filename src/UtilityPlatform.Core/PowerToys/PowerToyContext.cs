using UtilityPlatform.Core.Notifications;
using UtilityPlatform.Core.Settings;

namespace UtilityPlatform.Core.PowerToys;

public sealed class PowerToyContext(IPowerToyNotificationService notificationService, PowerToyConfiguration configuration)
{
    private readonly IPowerToyNotificationService _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly PowerToyConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public IPowerToyNotificationService Notifications => _notificationService;

    public PowerToyConfiguration Configuration => _configuration;
}

