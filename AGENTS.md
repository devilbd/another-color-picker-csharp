# Another Color Picker - Agent Guidelines

These rules and guidelines apply to all development, bug-fixing, refactoring, and testing tasks in this workspace.

## 🛠 Tech Stack & Environment
- **Runtime & Language**: .NET 10.0, C# 12
- **Framework**: Avalonia UI (v12.0.5)
- **MVVM Library**: CommunityToolkit.Mvvm (v8.4.1)
- **Target Platforms**: Cross-platform (primarily Linux/X11/Wayland, macOS, and Windows)
- **Unsafe Blocks**: Enabled (`<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`) for low-level screen captures and native X11 integration.

---

## 🏛 Architecture & MVVM Guidelines

### 1. MVVM Design
- **ViewModels**: Must inherit from [ViewModelBase](file:///run/media/devilbd/d/Development/another-color-picker-csharp/AnotherColorPicker/ViewModels/ViewModelBase.cs) which extends `ObservableObject`. Use CommunityToolkit source generators (`[ObservableProperty]`, `[RelayCommand]`) for boilerplate properties and commands.
- **Views**: UI layouts reside in `.axaml` files in the `Views` namespace. Code-behinds (`.axaml.cs`) must only handle view-specific lifecycle details and direct UI interactions, keeping all logic in the respective ViewModel.
- **Compiled Bindings**: Compiled bindings are enabled by default (`<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`). Always specify `x:DataType` on the root container of your views or data templates to ensure build-time type checking.

### 2. Service Layer
- **Interface-Driven Development**: Define dependencies as interfaces under `Services/` (e.g., `IEyedropperService`, `IClipboardService`) to facilitate testing, cross-platform abstractions, and mocking.
- **Native OS Interop & Safety**: When dealing with platform-specific code (e.g. X11 via Xlib or Wayland portals):
  - Properly dispose of native pointers, connections (`XCloseDisplay`), or window handles.
  - Implement safe fallbacks for cases when X11 is not available (such as Wayland sessions).
  - Encapsulate native code in `try-catch` blocks and log failures gracefully.

---

## 🎨 Styling & Design Aesthetics
- **Theme**: Fluent design with a dark theme aesthetic.
- **Typography**: Inter font family is standard (`Avalonia.Fonts.Inter`). Use hierarchy for text sizes and weights.
- **Rich User Experience**:
  - UI colors should be dynamic and visually previewed (e.g. magnifying glass preview during eyedropper picking).
  - Implement micro-animations, transitions, and clear hover/press states.
  - Use custom title bars or sleek margins rather than standard system frames.

---

## 🧪 Testing & Validation
- **Unit Tests**: All core utilities, conversion algorithms, and harmony helpers must be tested under `AnotherColorPicker.Tests`.
- **Framework**: Use `xUnit` for tests.
- **Accuracy**: Color conversions (RGB, HSV, HSL, CMYK, HEX) must maintain high precision and be validated with comprehensive boundary condition tests.

---

## ⚙ Build & Deployment Commands
- **Run project locally**: `dotnet run --project AnotherColorPicker`
- **Run unit tests**: `dotnet test`
- **Publish (Linux Single File)**: `dotnet publish -c Release -r linux-x64 --self-contained`
