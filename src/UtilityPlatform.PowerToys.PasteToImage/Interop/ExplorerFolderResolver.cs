using System.Runtime.InteropServices;

namespace UtilityPlatform.PowerToys.PasteToImage.Interop;

internal static class ExplorerFolderResolver
{
    private static readonly string[] _explorerWindowClassNames = ["CabinetWClass", "ExploreWClass"];

    public static bool IsExplorerWindow(nint hWnd)
    {
        var className = Win32.GetWindowClassName(hWnd);

        return _explorerWindowClassNames.Contains(className, StringComparer.Ordinal);
    }

    public static string? TryGetExplorerFolderPath(nint explorerHwnd)
    {
        object? shell = null;
        object? windows = null;

        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");

            if (shellType is null)
            {
                return null;
            }

            shell = Activator.CreateInstance(shellType);

            if (shell is null)
            {
                return null;
            }

            windows = shellType.InvokeMember("Windows", System.Reflection.BindingFlags.InvokeMethod, null, shell, null);

            if (windows is null)
            {
                return null;
            }

            foreach (var window in (System.Collections.IEnumerable)windows)
            {
                if (window is null)
                {
                    continue;
                }

                try
                {
                    var hwnd = GetHwnd(window);

                    if (hwnd != explorerHwnd)
                    {
                        continue;
                    }

                    var document = window.GetType().InvokeMember("Document", System.Reflection.BindingFlags.GetProperty, null, window, null);

                    if (document is null)
                    {
                        return null;
                    }

                    var folder = document.GetType().InvokeMember("Folder", System.Reflection.BindingFlags.GetProperty, null, document, null);

                    if (folder is null)
                    {
                        return null;
                    }

                    var self = folder.GetType().InvokeMember("Self", System.Reflection.BindingFlags.GetProperty, null, folder, null);

                    if (self is null)
                    {
                        return null;
                    }

                    var path = self.GetType().InvokeMember("Path", System.Reflection.BindingFlags.GetProperty, null, self, null) as string;

                    return path;
                }
                finally
                {
                    TryReleaseComObject(window);
                }
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            TryReleaseComObject(windows);
            TryReleaseComObject(shell);
        }

        return null;

        static nint GetHwnd(object window)
        {
            var hwndValue = window.GetType().InvokeMember("HWND", System.Reflection.BindingFlags.GetProperty, null, window, null);

            return hwndValue switch
            {
                int i => i,
                long l => (nint)l,
                _ => nint.Zero
            };
        }

        static void TryReleaseComObject(object? obj)
        {
            if (obj is null)
            {
                return;
            }

            try
            {
                if (Marshal.IsComObject(obj))
                {
                    Marshal.FinalReleaseComObject(obj);
                }
            }
            catch
            {
            }
        }
    }
}

