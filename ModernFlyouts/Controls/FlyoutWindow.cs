using ModernFlyouts.Core.Display;
using ModernFlyouts.Core.UI;
using ModernWpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using static ModernFlyouts.Core.Interop.NativeMethods;

namespace ModernFlyouts.Controls
{
    public class FlyoutWindow : Window
    {
        private bool isDragging;
        private Point dragStartPoint;
        private HwndSource hwndSource;
        private Timer closeTimer;
        private bool isTimeoutEnabled = true;

        public static readonly DependencyProperty PlacementModeProperty =
            DependencyProperty.Register(nameof(PlacementMode), typeof(FlyoutWindowPlacementMode),
                typeof(FlyoutWindow), new PropertyMetadata(FlyoutWindowPlacementMode.Auto));

        public static readonly DependencyProperty PreferredDisplayMonitorProperty =
            DependencyProperty.Register(nameof(PreferredDisplayMonitor), typeof(DisplayMonitor),
                typeof(FlyoutWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty AlignmentProperty =
            DependencyProperty.Register(nameof(Alignment), typeof(FlyoutWindowAlignment),
                typeof(FlyoutWindow), new PropertyMetadata(FlyoutWindowAlignment.Center));

        public static readonly DependencyProperty MarginProperty =
            DependencyProperty.Register(nameof(Margin), typeof(Thickness),
                typeof(FlyoutWindow), new PropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty ExpandDirectionProperty =
            DependencyProperty.Register(nameof(ExpandDirection), typeof(FlyoutWindowExpandDirection),
                typeof(FlyoutWindow), new PropertyMetadata(FlyoutWindowExpandDirection.Auto));

        public static readonly DependencyProperty FlyoutAnimationEnabledProperty =
            DependencyProperty.Register(nameof(FlyoutAnimationEnabled), typeof(bool),
                typeof(FlyoutWindow), new PropertyMetadata(true));

        public static readonly DependencyProperty TimeoutProperty =
            DependencyProperty.Register(nameof(Timeout), typeof(int),
                typeof(FlyoutWindow), new PropertyMetadata(3000));

        public FlyoutWindowPlacementMode PlacementMode
        {
            get => (FlyoutWindowPlacementMode)GetValue(PlacementModeProperty);
            set => SetValue(PlacementModeProperty, value);
        }

        public DisplayMonitor PreferredDisplayMonitor
        {
            get => (DisplayMonitor)GetValue(PreferredDisplayMonitorProperty);
            set => SetValue(PreferredDisplayMonitorProperty, value);
        }

        public FlyoutWindowAlignment Alignment
        {
            get => (FlyoutWindowAlignment)GetValue(AlignmentProperty);
            set => SetValue(AlignmentProperty, value);
        }

        public Thickness Margin
        {
            get => (Thickness)GetValue(MarginProperty);
            set => SetValue(MarginProperty, value);
        }

        public FlyoutWindowExpandDirection ExpandDirection
        {
            get => (FlyoutWindowExpandDirection)GetValue(ExpandDirectionProperty);
            set => SetValue(ExpandDirectionProperty, value);
        }

        public bool FlyoutAnimationEnabled
        {
            get => (bool)GetValue(FlyoutAnimationEnabledProperty);
            set => SetValue(FlyoutAnimationEnabledProperty, value);
        }

        public int Timeout
        {
            get => (int)GetValue(TimeoutProperty);
            set => SetValue(TimeoutProperty, value);
        }

        public bool IsOpen
        {
            get => Visibility == Visibility.Visible;
            set
            {
                if (value)
                {
                    Show();
                    if (isTimeoutEnabled)
                    {
                        StartCloseTimer();
                    }
                }
                else
                {
                    Hide();
                    StopCloseTimer();
                }
            }
        }

        public bool Activatable { get; set; } = true;

        public ZBandID ZBandID { get; set; } = ZBandID.Default;

        public FlyoutWindowType FlyoutWindowType { get; set; }

        public Thickness Offset { get; set; }

        public event EventHandler DragMoved;

        public FlyoutWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            SizeToContent = SizeToContent.WidthAndHeight;

            MouseLeftButtonDown += FlyoutWindow_MouseLeftButtonDown;
            MouseLeftButtonUp += FlyoutWindow_MouseLeftButtonUp;
            MouseMove += FlyoutWindow_MouseMove;
            MouseLeave += FlyoutWindow_MouseLeave;

            closeTimer = new Timer(Timeout);
            closeTimer.Tick += CloseTimer_Tick;

            // Erişilebilirlik özellikleri
            AutomationProperties.SetName(this, "Modern Flyouts Window");
            AutomationProperties.SetHelpText(this, "A modern, Fluent Design-based set of flyouts for Windows");
            AutomationProperties.SetIsRequiredForForm(this, false);
            AutomationProperties.SetLabeledBy(this, null);
            AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Erişilebilirlik için ek özellikler
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);

                var accessibility = new AccessibilityProperties
                {
                    Name = "Modern Flyouts Window",
                    Description = "A modern, Fluent Design-based set of flyouts for Windows",
                    Role = "Window",
                    State = "Normal"
                };

                hwndSource.RootVisual.SetValue(AutomationProperties.NameProperty, accessibility.Name);
                hwndSource.RootVisual.SetValue(AutomationProperties.HelpTextProperty, accessibility.Description);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WindowMessage)msg)
            {
                case WindowMessage.WM_ACTIVATE:
                    if (!Activatable)
                    {
                        handled = true;
                        return new IntPtr(1);
                    }
                    break;

                case WindowMessage.WM_NCHITTEST:
                    if (!Activatable)
                    {
                        handled = true;
                        return new IntPtr((int)HitTestValues.HTTRANSPARENT);
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        private void FlyoutWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PlacementMode == FlyoutWindowPlacementMode.Manual)
            {
                isDragging = true;
                dragStartPoint = e.GetPosition(this);
                CaptureMouse();
            }
        }

        private void FlyoutWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                ReleaseMouseCapture();
                DragMoved?.Invoke(this, EventArgs.Empty);
            }
        }

        private void FlyoutWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                Vector offset = currentPosition - dragStartPoint;

                Left += offset.X;
                Top += offset.Y;
            }
        }

        private void FlyoutWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                ReleaseMouseCapture();
                DragMoved?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            IsOpen = false;
        }

        public void StartCloseTimer()
        {
            closeTimer.Start();
        }

        public void StopCloseTimer()
        {
            closeTimer.Stop();
        }

        public void CreateWindow()
        {
            SourceInitialized += (s, e) =>
            {
                hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                if (hwndSource != null)
                {
                    hwndSource.AddHook(WndProc);
                }
            };
        }

        public void AlignToPosition()
        {
            if (PreferredDisplayMonitor == null) return;

            var monitorBounds = PreferredDisplayMonitor.Bounds;
            var windowSize = new Size(ActualWidth, ActualHeight);

            double x = 0, y = 0;

            switch (Alignment)
            {
                case FlyoutWindowAlignment.TopLeft:
                    x = monitorBounds.Left + Margin.Left;
                    y = monitorBounds.Top + Margin.Top;
                    break;

                case FlyoutWindowAlignment.TopCenter:
                    x = monitorBounds.Left + (monitorBounds.Width - windowSize.Width) / 2;
                    y = monitorBounds.Top + Margin.Top;
                    break;

                case FlyoutWindowAlignment.TopRight:
                    x = monitorBounds.Right - windowSize.Width - Margin.Right;
                    y = monitorBounds.Top + Margin.Top;
                    break;

                case FlyoutWindowAlignment.CenterLeft:
                    x = monitorBounds.Left + Margin.Left;
                    y = monitorBounds.Top + (monitorBounds.Height - windowSize.Height) / 2;
                    break;

                case FlyoutWindowAlignment.Center:
                    x = monitorBounds.Left + (monitorBounds.Width - windowSize.Width) / 2;
                    y = monitorBounds.Top + (monitorBounds.Height - windowSize.Height) / 2;
                    break;

                case FlyoutWindowAlignment.CenterRight:
                    x = monitorBounds.Right - windowSize.Width - Margin.Right;
                    y = monitorBounds.Top + (monitorBounds.Height - windowSize.Height) / 2;
                    break;

                case FlyoutWindowAlignment.BottomLeft:
                    x = monitorBounds.Left + Margin.Left;
                    y = monitorBounds.Bottom - windowSize.Height - Margin.Bottom;
                    break;

                case FlyoutWindowAlignment.BottomCenter:
                    x = monitorBounds.Left + (monitorBounds.Width - windowSize.Width) / 2;
                    y = monitorBounds.Bottom - windowSize.Height - Margin.Bottom;
                    break;

                case FlyoutWindowAlignment.BottomRight:
                    x = monitorBounds.Right - windowSize.Width - Margin.Right;
                    y = monitorBounds.Bottom - windowSize.Height - Margin.Bottom;
                    break;
            }

            Left = x;
            Top = y;
        }

        private class AccessibilityProperties
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Role { get; set; }
            public string State { get; set; }
        }
    }

    public enum FlyoutWindowPlacementMode
    {
        Auto,
        Manual
    }

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

    public enum FlyoutWindowExpandDirection
    {
        Auto,
        Up,
        Down,
        Left,
        Right
    }

    public enum FlyoutWindowType
    {
        OnScreen,
        Settings
    }
} 