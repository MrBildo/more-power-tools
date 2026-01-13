using System.Drawing;
using System.Windows.Forms;
using UtilityPlatform.App.PowerToys;
using UtilityPlatform.App.Settings;
using UtilityPlatform.App.UI;

namespace UtilityPlatform.App.Tray;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly TrayNotificationService _notificationService;
    private readonly PowerToyManager _powerToyManager;

    private SettingsForm? _settingsForm;

    public TrayApplicationContext()
    {
        _notifyIcon = new()
        {
            Icon = SystemIcons.Application,
            Text = "Utility Platform",
            Visible = true
        };

        _notifyIcon.ContextMenuStrip = BuildContextMenu();

        _notificationService = new(_notifyIcon);

        var settingsStore = new JsonPowerToySettingsStore();
        var powerToys = PowerToyDiscovery.DiscoverPowerToys();

        _powerToyManager = new(powerToys, settingsStore, _notificationService);
        _powerToyManager.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

        _notifyIcon.DoubleClick += (_, _) => ShowSettings();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _settingsForm?.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => ShowSettings();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        return contextMenu;
    }

    private void ShowSettings()
    {
        if (_settingsForm is null || _settingsForm.IsDisposed)
        {
            _settingsForm = new(_powerToyManager);
            _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        }

        if (_settingsForm.Visible is false)
        {
            _settingsForm.Show();
        }

        _settingsForm.BringToFront();
        _settingsForm.Activate();
    }
}

