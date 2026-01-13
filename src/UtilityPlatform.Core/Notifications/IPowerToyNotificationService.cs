namespace UtilityPlatform.Core.Notifications;

public interface IPowerToyNotificationService
{
    void ShowInfo(string title, string message);

    void ShowError(string title, string message);
}

