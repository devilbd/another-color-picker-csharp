using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnotherColorPicker.Models;

namespace AnotherColorPicker.Services;

/// <summary>
/// Manages color history and custom palettes, persisting them to a local JSON file.
/// </summary>
public class PaletteService
{
    private readonly string _filePath;
    private PaletteData _data;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public PaletteService()
    {
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AnotherColorPicker");

        Directory.CreateDirectory(configDir);
        _filePath = Path.Combine(configDir, "palettes.json");
        _data = Load();
    }

    /// <summary>
    /// Gets the color history (most recent first).
    /// </summary>
    public IReadOnlyList<ColorEntry> History => _data.History.AsReadOnly();

    /// <summary>
    /// Gets the saved palettes.
    /// </summary>
    public IReadOnlyList<ColorPalette> Palettes => _data.Palettes.AsReadOnly();

    /// <summary>
    /// Adds a color to the history. Deduplicates by HEX value.
    /// </summary>
    public void AddToHistory(ColorModel color, string? name = null)
    {
        // Remove existing entry with same HEX if present
        _data.History.RemoveAll(c =>
            string.Equals(c.Hex, color.Hex, StringComparison.OrdinalIgnoreCase));

        _data.History.Insert(0, new ColorEntry
        {
            Hex = color.Hex,
            Name = name,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });

        // Trim history to max size
        while (_data.History.Count > _data.MaxHistorySize)
        {
            _data.History.RemoveAt(_data.History.Count - 1);
        }

        Save();
    }

    /// <summary>
    /// Creates a new palette with the given name.
    /// </summary>
    public ColorPalette CreatePalette(string name)
    {
        var palette = new ColorPalette { Name = name };
        _data.Palettes.Add(palette);
        Save();
        return palette;
    }

    /// <summary>
    /// Adds a color to an existing palette.
    /// </summary>
    public void AddToPalette(int paletteIndex, ColorModel color, string? name = null)
    {
        if (paletteIndex < 0 || paletteIndex >= _data.Palettes.Count)
            return;

        _data.Palettes[paletteIndex].Colors.Add(new ColorEntry
        {
            Hex = color.Hex,
            Name = name,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });

        Save();
    }

    /// <summary>
    /// Removes a color from the history by index.
    /// </summary>
    public void RemoveFromHistory(int index)
    {
        if (index >= 0 && index < _data.History.Count)
        {
            _data.History.RemoveAt(index);
            Save();
        }
    }

    /// <summary>
    /// Removes a palette by index.
    /// </summary>
    public void RemovePalette(int index)
    {
        if (index >= 0 && index < _data.Palettes.Count)
        {
            _data.Palettes.RemoveAt(index);
            Save();
        }
    }

    /// <summary>
    /// Clears all color history.
    /// </summary>
    public void ClearHistory()
    {
        _data.History.Clear();
        Save();
    }

    private PaletteData Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<PaletteData>(json, JsonOptions) ?? new PaletteData();
            }
        }
        catch
        {
            // If corrupt, start fresh
        }

        return new PaletteData();
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Silently fail if we can't write
        }
    }
}
