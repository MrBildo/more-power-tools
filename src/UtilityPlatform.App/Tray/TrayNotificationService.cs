using System.Threading;
using System.Windows.Forms;
using UtilityPlatform.Core.Notifications;

namespace UtilityPlatform.App.Tray;

public class TrayNotificationService(NotifyIcon notifyIcon) : IPowerToyNotificationService
{
    private readonly NotifyIcon _notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
    private readonly SynchronizationContext? _synchronizationContext = SynchronizationContext.Current;

    public void ShowInfo(string title, string message)
    {
        ShowBalloonTip(title, message, ToolTipIcon.Info, 3000);
    }

    public void ShowError(string title, string message)
    {
        ShowBalloonTip(title, message, ToolTipIcon.Error, 5000);
    }

    private void ShowBalloonTip(string title, string message, ToolTipIcon icon, int timeout)
    {
        if (_synchronizationContext is not null && SynchronizationContext.Current != _synchronizationContext)
        {
            _synchronizationContext.Post(_ => ShowBalloonTip(title, message, icon, timeout), null);
            return;
        }

        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(timeout);
    }
}

