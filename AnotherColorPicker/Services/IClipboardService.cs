using System.Threading.Tasks;

namespace AnotherColorPicker.Services;

/// <summary>
/// Interface for clipboard operations.
/// </summary>
public interface IClipboardService
{
    Task CopyToClipboardAsync(string text);
}
