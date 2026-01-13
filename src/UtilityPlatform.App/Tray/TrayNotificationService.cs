using System.Windows.Forms;
using UtilityPlatform.Core.Notifications;

namespace UtilityPlatform.App.Tray;

public class TrayNotificationService(NotifyIcon notifyIcon) : IPowerToyNotificationService
{
    private readonly NotifyIcon _notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));

    public void ShowInfo(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        _notifyIcon.ShowBalloonTip(3000);
    }

    public void ShowError(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
        _notifyIcon.ShowBalloonTip(5000);
    }
}

