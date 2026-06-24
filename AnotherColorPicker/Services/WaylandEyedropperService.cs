using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using Tmds.DBus;

namespace AnotherColorPicker.Services;

[DBusInterface("org.freedesktop.portal.Screenshot")]
public interface IScreenshotPortal : IDBusObject
{
    Task<ObjectPath> PickColorAsync(string parent_window, IDictionary<string, object> options);
    Task<ObjectPath> ScreenshotAsync(string parent_window, IDictionary<string, object> options);
}

[DBusInterface("org.freedesktop.portal.Request")]
public interface IRequest : IDBusObject
{
    Task CloseAsync();
    Task<IDisposable> WatchResponseAsync(Action<(uint response, IDictionary<string, object> results)> handler, Action<Exception>? onError = null);
}

/// <summary>
/// Native Wayland Eyedropper using the XDG Desktop Portal DBus interface.
/// </summary>
public class WaylandEyedropperService : IEyedropperService
{
    // Wayland portal doesn't support live preview during picking.
    public event Action<Color>? ColorPreview; 
    
    public bool IsAvailable => true;

    public async Task<Color?> PickColorAsync()
    {
        try
        {
            using var connection = new Connection(Address.Session);
            await connection.ConnectAsync();

            var portal = connection.CreateProxy<IScreenshotPortal>("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
            
            // Call the portal to show the native color picker
            var requestPath = await portal.PickColorAsync("", new Dictionary<string, object>());
            var request = connection.CreateProxy<IRequest>("org.freedesktop.portal.Desktop", requestPath);
            
            var tcs = new TaskCompletionSource<Color?>();
            
            IDisposable? watcher = null;
            watcher = await request.WatchResponseAsync(
                args => 
                {
                    try
                    {
                        if (args.response == 0 && args.results != null && args.results.TryGetValue("color", out var colorObj))
                        {
                            // DBus color is a struct (ddd) inside a variant
                            if (colorObj is ValueTuple<double, double, double> c)
                            {
                                var color = Color.FromRgb(
                                    (byte)Math.Round(c.Item1 * 255),
                                    (byte)Math.Round(c.Item2 * 255),
                                    (byte)Math.Round(c.Item3 * 255)
                                );
                                tcs.TrySetResult(color);
                            }
                            else if (colorObj is Tuple<double, double, double> tc)
                            {
                                var color = Color.FromRgb(
                                    (byte)Math.Round(tc.Item1 * 255),
                                    (byte)Math.Round(tc.Item2 * 255),
                                    (byte)Math.Round(tc.Item3 * 255)
                                );
                                tcs.TrySetResult(color);
                            }
                            else
                            {
                                tcs.TrySetResult(null);
                            }
                        }
                        else
                        {
                            tcs.TrySetResult(null);
                        }
                    }
                    catch
                    {
                        tcs.TrySetResult(null);
                    }
                    finally
                    {
                        watcher?.Dispose();
                    }
                },
                onError: ex => 
                {
                    tcs.TrySetResult(null);
                    watcher?.Dispose();
                });
                
            return await tcs.Task;
        }
        catch
        {
            return null;
        }
    }
}
