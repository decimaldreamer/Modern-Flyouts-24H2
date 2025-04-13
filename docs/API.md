# Modern Flyouts API Documentation

## Core Components

### FlyoutManager
The main manager class that handles flyout window operations and display management.

```csharp
public class FlyoutManager : ObservableObject
{
    // Singleton instance
    public static FlyoutManager Instance { get; }

    // Properties
    public FlyoutWindow OnScreenFlyoutWindow { get; }
    public FlyoutView OnScreenFlyoutView { get; }
    public UIManager UIManager { get; }
    public DisplayMonitor OnScreenFlyoutPreferredMonitor { get; set; }
    public BindablePoint DefaultFlyoutPosition { get; }

    // Methods
    public void Initialize();
    public void AlignFlyout(BindablePoint toPos = null);
}
```

### FlyoutWindow
A custom window class that provides flyout functionality with various customization options.

```csharp
public class FlyoutWindow : Window
{
    // Properties
    public FlyoutWindowPlacementMode PlacementMode { get; set; }
    public DisplayMonitor PreferredDisplayMonitor { get; set; }
    public FlyoutWindowAlignment Alignment { get; set; }
    public Thickness Margin { get; set; }
    public FlyoutWindowExpandDirection ExpandDirection { get; set; }
    public bool FlyoutAnimationEnabled { get; set; }
    public int Timeout { get; set; }
    public bool IsOpen { get; set; }
    public bool Activatable { get; set; }
    public ZBandID ZBandID { get; set; }
    public FlyoutWindowType FlyoutWindowType { get; set; }
    public Thickness Offset { get; set; }

    // Methods
    public void StartCloseTimer();
    public void StopCloseTimer();
    public void CreateWindow();
    public void AlignToPosition();
}
```

### Timer
A custom timer implementation for managing flyout timeouts.

```csharp
public class Timer
{
    // Properties
    public event EventHandler Tick;

    // Methods
    public void Start();
    public void Stop();
}
```

## Enums

### FlyoutWindowPlacementMode
```csharp
public enum FlyoutWindowPlacementMode
{
    Auto,
    Manual
}
```

### FlyoutWindowAlignment
```csharp
public enum FlyoutWindowAlignment
{
    TopLeft,
    TopCenter,
    TopRight,
    CenterLeft,
    Center,
    CenterRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}
```

### FlyoutWindowExpandDirection
```csharp
public enum FlyoutWindowExpandDirection
{
    Auto,
    Up,
    Down,
    Left,
    Right
}
```

### FlyoutWindowType
```csharp
public enum FlyoutWindowType
{
    OnScreen,
    Settings
}
```

## Logging

### Logger
A static logging utility for application-wide logging.

```csharp
public static class Logger
{
    // Methods
    public static void Initialize();
    public static void Log(LogLevel level, string message, Exception exception = null);
    public static void Debug(string message);
    public static void Info(string message);
    public static void Warning(string message, Exception exception = null);
    public static void Error(string message, Exception exception = null);
    public static void Fatal(string message, Exception exception = null);
}
```

### LogLevel
```csharp
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
```

## Usage Examples

### Creating a Flyout Window
```csharp
var flyoutWindow = new FlyoutWindow
{
    PlacementMode = FlyoutWindowPlacementMode.Auto,
    Alignment = FlyoutWindowAlignment.Center,
    Timeout = 3000,
    FlyoutAnimationEnabled = true
};
```

### Managing Flyouts
```csharp
// Initialize the flyout manager
FlyoutManager.Instance.Initialize();

// Align flyout to a specific position
var position = new BindablePoint { X = 100, Y = 100 };
FlyoutManager.Instance.AlignFlyout(position);
```

### Logging
```csharp
// Initialize logging
Logger.Initialize();

// Log messages
Logger.Info("Application started");
Logger.Error("An error occurred", exception);
```

## Notes
- All flyout windows are DPI-aware and support high DPI displays
- The application requires Windows 10 1809 or later, and Windows 11 24H2
- Flyout windows are designed to work with multiple monitors
- Accessibility features are built-in and can be customized 