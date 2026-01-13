using System.Drawing.Imaging;
using System.Windows.Forms;
using UtilityPlatform.Core.PowerToys;
using UtilityPlatform.Core.Settings;
using UtilityPlatform.PowerToys.PasteToImage.Interop;

namespace UtilityPlatform.PowerToys.PasteToImage;

public class PasteToImagePowerToy : IPowerToy
{
    private const string _fileNamePrefixKey = "FileNamePrefix";
    private const string _showNotificationKey = "ShowNotification";

    private StaThreadDispatcher? _staDispatcher;
    private KeyboardHook? _keyboardHook;

    private string _fileNamePrefix = "clipboard-image-";
    private bool _showNotification = true;

    public PowerToyDescriptor Descriptor => new
    (
        "paste-to-image",
        "Paste to Image",
        "Paste clipboard images as numbered PNG files into the active File Explorer folder."
    );

    public IReadOnlyList<PowerToySettingDefinition> SettingDefinitions => [
        new(_fileNamePrefixKey, "File name prefix", "Prefix used for new files (e.g., clipboard-image-)", PowerToySettingType.String, "clipboard-image-"),
        new(_showNotificationKey, "Show notification", "Show a tray notification after saving the file.", PowerToySettingType.Boolean, true)
    ];

    public Task StartAsync(PowerToyContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        ApplyConfiguration(context.Configuration);

        _staDispatcher = new("PasteToImagePowerToy STA");
        _keyboardHook = new(HandleKeyboardEvent);
        _keyboardHook.Start();

        return Task.CompletedTask;

        bool HandleKeyboardEvent(KeyboardHookEvent keyboardEvent)
        {
            if (keyboardEvent.IsKeyDown is false)
            {
                return false;
            }

            if (keyboardEvent.ControlPressed is false)
            {
                return false;
            }

            if (keyboardEvent.VirtualKey is not Keys.V)
            {
                return false;
            }

            var foregroundWindow = Win32.GetForegroundWindow();

            if (foregroundWindow == nint.Zero)
            {
                return false;
            }

            if (ExplorerFolderResolver.IsExplorerWindow(foregroundWindow) is false)
            {
                return false;
            }

            if (Win32.ContainsClipboardImage() is false)
            {
                return false;
            }

            _ = _staDispatcher?.RunAsync(() => PasteClipboardImageToExplorer(context, foregroundWindow));

            return true;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _keyboardHook?.Dispose();
        _keyboardHook = null;

        _staDispatcher?.Dispose();
        _staDispatcher = null;

        return Task.CompletedTask;
    }

    public Task OnSettingsChangedAsync(PowerToyContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        ApplyConfiguration(context.Configuration);

        return Task.CompletedTask;
    }

    private void ApplyConfiguration(PowerToyConfiguration configuration)
    {
        _fileNamePrefix = configuration.GetString(_fileNamePrefixKey, "clipboard-image-");
        _showNotification = configuration.GetBoolean(_showNotificationKey, true);
    }

    private void PasteClipboardImageToExplorer(PowerToyContext context, nint explorerHwnd)
    {
        try
        {
            var targetFolderPath = ExplorerFolderResolver.TryGetExplorerFolderPath(explorerHwnd);

            if (string.IsNullOrWhiteSpace(targetFolderPath))
            {
                context.Notifications.ShowError(Descriptor.DisplayName, "Unable to determine the current File Explorer folder.");
                return;
            }

            if (Clipboard.ContainsImage() is false)
            {
                return;
            }

            using var image = Clipboard.GetImage();

            if (image is null)
            {
                return;
            }

            var filePath = GetNextFilePath(targetFolderPath);

            image.Save(filePath, ImageFormat.Png);

            if (_showNotification)
            {
                context.Notifications.ShowInfo(Descriptor.DisplayName, $"Saved '{Path.GetFileName(filePath)}'.");
            }
        }
        catch (Exception ex)
        {
            context.Notifications.ShowError(Descriptor.DisplayName, ex.Message);
        }
    }

    private string GetNextFilePath(string folderPath)
    {
        var index = 1;

        while (true)
        {
            var fileName = $"{_fileNamePrefix}{index}.png";
            var filePath = Path.Combine(folderPath, fileName);

            if (File.Exists(filePath) is false)
            {
                return filePath;
            }

            index++;
        }
    }
}

