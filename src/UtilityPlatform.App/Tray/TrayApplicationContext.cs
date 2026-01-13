using System.Drawing;
using System.Threading;
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
    private readonly CancellationTokenSource _initializationCts = new();
    private readonly Task _initializationTask;

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

        _initializationTask = InitializePowerToysAsync(_initializationCts.Token);

        _notifyIcon.DoubleClick += async (_, _) => await ShowSettingsAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _initializationCts.Cancel();
            _initializationCts.Dispose();
            _settingsForm?.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += async (_, _) => await ShowSettingsAsync();

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitThread();

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        return contextMenu;
    }

    private async Task InitializePowerToysAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Don't block the UI thread with a synchronous wait. This avoids deadlocks during tray/menu interaction.
            await _powerToyManager.InitializeAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Utility Platform", ex.Message);
        }
    }

    private async Task ShowSettingsAsync()
    {
        try
        {
            await _initializationTask;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("Utility Platform", ex.Message);
            return;
        }

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

