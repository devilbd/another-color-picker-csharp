using System.Collections.Generic;

namespace AnotherColorPicker.Models;

/// <summary>
/// Represents a named color palette containing a collection of colors.
/// </summary>
public class ColorPalette
{
    public string Name { get; set; } = "Untitled Palette";
    public List<ColorEntry> Colors { get; set; } = new();
}

/// <summary>
/// Represents a single color entry with an optional custom name.
/// </summary>
public class ColorEntry
{
    public string Hex { get; set; } = "#000000";
    public string? Name { get; set; }
    public long Timestamp { get; set; }
}

/// <summary>
/// Root data model for serialized palette storage.
/// </summary>
public class PaletteData
{
    public List<ColorEntry> History { get; set; } = new();
    public List<ColorPalette> Palettes { get; set; } = new();
    public int MaxHistorySize { get; set; } = 50;
}
