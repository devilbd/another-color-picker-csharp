using System;
using System.Threading.Tasks;
using Avalonia.Media;

namespace AnotherColorPicker.Services;

/// <summary>
/// Interface for screen color picking (eyedropper) functionality.
/// </summary>
public interface IEyedropperService
{
    /// <summary>
    /// Starts the eyedropper pick mode. Returns the picked color when the user clicks.
    /// </summary>
    Task<Color?> PickColorAsync();

    /// <summary>
    /// Raised during picking with the color under the current cursor position.
    /// </summary>
    event Action<Color>? ColorPreview;

    /// <summary>
    /// Whether this service is available on the current platform.
    /// </summary>
    bool IsAvailable { get; }
}
