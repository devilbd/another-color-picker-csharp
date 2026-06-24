using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace AnotherColorPicker.Services;

/// <summary>
/// Clipboard service implementation using Avalonia's built-in clipboard API.
/// Works cross-platform on Linux (X11/Wayland), Windows, and macOS.
/// </summary>
public class ClipboardService : IClipboardService
{
    public async Task CopyToClipboardAsync(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
            }
        }
    }
}
