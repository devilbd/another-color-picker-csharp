using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnotherColorPicker.Models;
using AnotherColorPicker.Helpers;
using AnotherColorPicker.Services;

namespace AnotherColorPicker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PaletteService _paletteService;
    private readonly ClipboardService _clipboardService;
    private readonly IEyedropperService _eyedropperService;

    private bool _suppressUpdates;

    // ========================
    // Current Color
    // ========================

    [ObservableProperty]
    private ColorModel _currentColor = ColorModel.FromHex("#6C63FF");

    // --- RGB ---
    [ObservableProperty] private byte _red = 108;
    [ObservableProperty] private byte _green = 99;
    [ObservableProperty] private byte _blue = 255;

    // --- HSV ---
    [ObservableProperty] private double _hsvHue = 243.46;
    [ObservableProperty] private double _hsvSaturation = 61.18;
    [ObservableProperty] private double _hsvValue = 100;

    // --- HSL ---
    [ObservableProperty] private double _hslHue = 243.46;
    [ObservableProperty] private double _hslSaturation = 100;
    [ObservableProperty] private double _hslLightness = 69.41;

    // --- CMYK ---
    [ObservableProperty] private double _cmykC;
    [ObservableProperty] private double _cmykM;
    [ObservableProperty] private double _cmykY;
    [ObservableProperty] private double _cmykK;

    // --- HEX ---
    [ObservableProperty] private string _hexText = "#6C63FF";

    // --- Preview color brush hex ---
    [ObservableProperty] private string _previewHex = "#6C63FF";

    // ========================
    // Color Canvas Properties
    // ========================

    [ObservableProperty] private double _canvasSaturation = 61.18;
    [ObservableProperty] private double _canvasValue = 100;
    [ObservableProperty] private double _hueSliderValue = 243.46;


    // ========================
    // History
    // ========================

    [ObservableProperty] private ObservableCollection<HistoryColorItem> _colorHistory = new();

    // ========================
    // Toast
    // ========================

    [ObservableProperty] private bool _showToast;
    [ObservableProperty] private string _toastMessage = "";

    // ========================
    // Eyedropper
    // ========================

    [ObservableProperty] private bool _isEyedropperActive;
    [ObservableProperty] private string _eyedropperPreviewHex = "#000000";

    public MainWindowViewModel()
    {
        _paletteService = new PaletteService();
        _clipboardService = new ClipboardService();
        _eyedropperService = new NativeEyedropperService();

        // Load history
        LoadHistory();

        // Initial sync from hex color
        SyncAllFromColor(CurrentColor);
    }

    // ========================
    // RGB Change Handlers
    // ========================

    partial void OnRedChanged(byte value) => OnRgbComponentChanged();
    partial void OnGreenChanged(byte value) => OnRgbComponentChanged();
    partial void OnBlueChanged(byte value) => OnRgbComponentChanged();

    private void OnRgbComponentChanged()
    {
        if (_suppressUpdates) return;
        _suppressUpdates = true;

        CurrentColor = ColorModel.FromRgb(Red, Green, Blue);
        SyncNonRgbFromColor(CurrentColor);

        _suppressUpdates = false;
    }

    // ========================
    // HSV Change Handlers
    // ========================

    partial void OnHsvHueChanged(double value) => OnHsvComponentChanged();
    partial void OnHsvSaturationChanged(double value) => OnHsvComponentChanged();
    partial void OnHsvValueChanged(double value) => OnHsvComponentChanged();

    private void OnHsvComponentChanged()
    {
        if (_suppressUpdates) return;
        _suppressUpdates = true;

        CurrentColor = ColorModel.FromHsv(HsvHue, HsvSaturation, HsvValue);
        SyncNonHsvFromColor(CurrentColor);

        _suppressUpdates = false;
    }

    // ========================
    // HSL Change Handlers
    // ========================

    partial void OnHslHueChanged(double value) => OnHslComponentChanged();
    partial void OnHslSaturationChanged(double value) => OnHslComponentChanged();
    partial void OnHslLightnessChanged(double value) => OnHslComponentChanged();

    private void OnHslComponentChanged()
    {
        if (_suppressUpdates) return;
        _suppressUpdates = true;

        CurrentColor = ColorModel.FromHsl(HslHue, HslSaturation, HslLightness);
        SyncNonHslFromColor(CurrentColor);

        _suppressUpdates = false;
    }

    // ========================
    // CMYK Change Handlers
    // ========================

    partial void OnCmykCChanged(double value) => OnCmykComponentChanged();
    partial void OnCmykMChanged(double value) => OnCmykComponentChanged();
    partial void OnCmykYChanged(double value) => OnCmykComponentChanged();
    partial void OnCmykKChanged(double value) => OnCmykComponentChanged();

    private void OnCmykComponentChanged()
    {
        if (_suppressUpdates) return;
        _suppressUpdates = true;

        CurrentColor = ColorModel.FromCmyk(CmykC, CmykM, CmykY, CmykK);
        SyncNonCmykFromColor(CurrentColor);

        _suppressUpdates = false;
    }

    // ========================
    // Canvas Handlers
    // ========================

    partial void OnCanvasSaturationChanged(double value)
    {
        if (_suppressUpdates) return;
        _suppressUpdates = true;

        CurrentColor = ColorModel.FromHsv(HueSliderValue, value, CanvasValue);
        SyncAllFromColor(CurrentColor);

        _suppressUpdates = false;
    }

    partial void OnCanvasValueChanged(double value)
    {
        if (_suppressUpdates) return;
        _suppressUpdates = true;

        CurrentColor = ColorModel.FromHsv(HueSliderValue, CanvasSaturation, value);
        SyncAllFromColor(CurrentColor);

        _suppressUpdates = false;
    }

    partial void OnHueSliderValueChanged(double value)
    {
        if (_suppressUpdates) return;
        _suppressUpdates = true;

        CurrentColor = ColorModel.FromHsv(value, CanvasSaturation, CanvasValue);
        SyncAllFromColor(CurrentColor);

        _suppressUpdates = false;
    }

    // ========================
    // HEX Change Handler
    // ========================

    partial void OnHexTextChanged(string value)
    {
        if (_suppressUpdates) return;

        if (ColorModel.TryParseHex(value, out var r, out var g, out var b))
        {
            _suppressUpdates = true;

            CurrentColor = ColorModel.FromRgb(r, g, b);
            SyncAllFromColor(CurrentColor);

            _suppressUpdates = false;
        }
    }


    // ========================
    // Sync Methods
    // ========================

    private void SyncAllFromColor(ColorModel color)
    {
        bool prev = _suppressUpdates;
        _suppressUpdates = true;

        Red = color.R;
        Green = color.G;
        Blue = color.B;

        HsvHue = color.HsvH;
        HsvSaturation = color.HsvS;
        HsvValue = color.HsvV;

        HslHue = color.HslH;
        HslSaturation = color.HslS;
        HslLightness = color.HslL;

        CmykC = color.CmykC;
        CmykM = color.CmykM;
        CmykY = color.CmykY;
        CmykK = color.CmykK;

        HexText = color.Hex;
        PreviewHex = color.Hex;

        CanvasSaturation = color.HsvS;
        CanvasValue = color.HsvV;
        HueSliderValue = color.HsvH;

        _suppressUpdates = prev;
    }

    private void SyncNonRgbFromColor(ColorModel color)
    {
        HsvHue = color.HsvH;
        HsvSaturation = color.HsvS;
        HsvValue = color.HsvV;

        HslHue = color.HslH;
        HslSaturation = color.HslS;
        HslLightness = color.HslL;

        CmykC = color.CmykC;
        CmykM = color.CmykM;
        CmykY = color.CmykY;
        CmykK = color.CmykK;

        HexText = color.Hex;
        PreviewHex = color.Hex;

        CanvasSaturation = color.HsvS;
        CanvasValue = color.HsvV;
        HueSliderValue = color.HsvH;
    }

    private void SyncNonHsvFromColor(ColorModel color)
    {
        Red = color.R;
        Green = color.G;
        Blue = color.B;

        HslHue = color.HslH;
        HslSaturation = color.HslS;
        HslLightness = color.HslL;

        CmykC = color.CmykC;
        CmykM = color.CmykM;
        CmykY = color.CmykY;
        CmykK = color.CmykK;

        HexText = color.Hex;
        PreviewHex = color.Hex;

        CanvasSaturation = color.HsvS;
        CanvasValue = color.HsvV;
        HueSliderValue = color.HsvH;
    }

    private void SyncNonHslFromColor(ColorModel color)
    {
        Red = color.R;
        Green = color.G;
        Blue = color.B;

        HsvHue = color.HsvH;
        HsvSaturation = color.HsvS;
        HsvValue = color.HsvV;

        CmykC = color.CmykC;
        CmykM = color.CmykM;
        CmykY = color.CmykY;
        CmykK = color.CmykK;

        HexText = color.Hex;
        PreviewHex = color.Hex;

        CanvasSaturation = color.HsvS;
        CanvasValue = color.HsvV;
        HueSliderValue = color.HsvH;
    }

    private void SyncNonCmykFromColor(ColorModel color)
    {
        Red = color.R;
        Green = color.G;
        Blue = color.B;

        HsvHue = color.HsvH;
        HsvSaturation = color.HsvS;
        HsvValue = color.HsvV;

        HslHue = color.HslH;
        HslSaturation = color.HslS;
        HslLightness = color.HslL;

        HexText = color.Hex;
        PreviewHex = color.Hex;

        CanvasSaturation = color.HsvS;
        CanvasValue = color.HsvV;
        HueSliderValue = color.HsvH;
    }


    // ========================
    // Commands
    // ========================

    [RelayCommand]
    private async Task CopyHex()
    {
        await _clipboardService.CopyToClipboardAsync(HexText);
        await ShowToastAsync($"Copied {HexText}");
    }

    [RelayCommand]
    private async Task CopyRgb()
    {
        var text = $"rgb({Red}, {Green}, {Blue})";
        await _clipboardService.CopyToClipboardAsync(text);
        await ShowToastAsync($"Copied {text}");
    }

    [RelayCommand]
    private async Task CopyHsl()
    {
        var text = $"hsl({HslHue:F0}, {HslSaturation:F0}%, {HslLightness:F0}%)";
        await _clipboardService.CopyToClipboardAsync(text);
        await ShowToastAsync($"Copied {text}");
    }

    [RelayCommand]
    private async Task CopyCmyk()
    {
        var text = $"cmyk({CmykC:F0}%, {CmykM:F0}%, {CmykY:F0}%, {CmykK:F0}%)";
        await _clipboardService.CopyToClipboardAsync(text);
        await ShowToastAsync($"Copied {text}");
    }



    [RelayCommand]
    private void AddToHistory()
    {
        _paletteService.AddToHistory(CurrentColor);
        LoadHistory();
    }

    [RelayCommand]
    private void ApplyHistoryColor(string? hex)
    {
        if (!string.IsNullOrEmpty(hex))
        {
            var color = ColorModel.FromHex(hex);
            CurrentColor = color;
            SyncAllFromColor(color);
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _paletteService.ClearHistory();
        ColorHistory.Clear();
    }

    [RelayCommand]
    private async Task PickFromScreen()
    {
        if (!_eyedropperService.IsAvailable)
        {
            await ShowToastAsync("Eyedropper not available on this platform");
            return;
        }

        IsEyedropperActive = true;

        _eyedropperService.ColorPreview += OnEyedropperPreview;

        try
        {
            var pickedColor = await _eyedropperService.PickColorAsync();

            if (pickedColor.HasValue)
            {
                var c = pickedColor.Value;
                var color = ColorModel.FromRgb(c.R, c.G, c.B, c.A);
                CurrentColor = color;
                SyncAllFromColor(color);
                _paletteService.AddToHistory(color);
                LoadHistory();
                await ShowToastAsync($"Picked {color.Hex}");
            }
            else
            {
                await ShowToastAsync("Eyedropper failed or canceled.");
            }
        }
        finally
        {
            _eyedropperService.ColorPreview -= OnEyedropperPreview;
            IsEyedropperActive = false;
        }
    }

    private void OnEyedropperPreview(Avalonia.Media.Color color)
    {
        EyedropperPreviewHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    // ========================
    // Toast Helper
    // ========================

    private async Task ShowToastAsync(string message)
    {
        ToastMessage = message;
        ShowToast = true;
        await Task.Delay(2000);
        ShowToast = false;
    }

    // ========================
    // History Helper
    // ========================

    private void LoadHistory()
    {
        ColorHistory.Clear();
        foreach (var entry in _paletteService.History)
        {
            ColorHistory.Add(new HistoryColorItem(entry.Hex, entry.Name ?? entry.Hex));
        }
    }
}

// ========================
// Supporting Types
// ========================

public record HistoryColorItem(string Hex, string Label);
