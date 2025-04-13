using CommunityToolkit.Mvvm.ComponentModel;
using ModernFlyouts.Core.Display;
using ModernFlyouts.Core.UI;
using ModernFlyouts.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ModernFlyouts.Core
{
    public class FlyoutManager : ObservableObject
    {
        private static FlyoutManager _instance;
        public static FlyoutManager Instance => _instance ??= new FlyoutManager();

        private bool _isPreferredMonitorChanging;
        private bool _savePreferredMonitor = true;
        private List<FlyoutHelperBase> _flyoutHelpers = new();
        private DisplayMonitor _onScreenFlyoutPreferredMonitor;
        private BindablePoint _flyoutPosition;
        private BindablePoint _defaultFlyoutPosition;

        public FlyoutWindow OnScreenFlyoutWindow { get; private set; }
        public FlyoutView OnScreenFlyoutView { get; private set; }
        public UIManager UIManager { get; private set; }

        public DisplayMonitor OnScreenFlyoutPreferredMonitor
        {
            get => _onScreenFlyoutPreferredMonitor;
            set
            {
                if (SetProperty(ref _onScreenFlyoutPreferredMonitor, value))
                {
                    if (!_isPreferredMonitorChanging)
                        MoveFlyoutToAnotherMonitor();

                    if (_savePreferredMonitor)
                        AppDataHelper.PreferredDisplayMonitorId = value.DeviceId;
                }
            }
        }

        public BindablePoint DefaultFlyoutPosition
        {
            get => _defaultFlyoutPosition;
            private set
            {
                _defaultFlyoutPosition = value;
                _defaultFlyoutPosition.ValueChanged += (s, _) =>
                {
                    AppDataHelper.SavePropertyValue(s.ToString(), nameof(AppDataHelper.DefaultFlyoutPosition));
                };
            }
        }

        public void Initialize()
        {
            DisplayManager.Initialize();
            var preferredDisplayMonitorId = AppDataHelper.PreferredDisplayMonitorId;

            UIManager = new UIManager();
            UIManager.Initialize();

            CreateOnScreenFlyoutWindow();
            InitializeDisplayMonitor(preferredDisplayMonitorId);
        }

        private void InitializeDisplayMonitor(string preferredDisplayMonitorId)
        {
            if (DisplayManager.Instance.DisplayMonitors.Any(x => x.DeviceId == preferredDisplayMonitorId))
            {
                OnScreenFlyoutPreferredMonitor = DisplayManager.Instance.DisplayMonitors
                    .First(x => x.DeviceId == preferredDisplayMonitorId);
            }
            else
            {
                OnScreenFlyoutPreferredMonitor = DisplayManager.Instance.PrimaryDisplayMonitor;
                if (_onScreenFlyoutPreferredMonitor.Bounds.Contains(_flyoutPosition.ToPoint()))
                {
                    AlignFlyout(_flyoutPosition);
                }
                else
                {
                    AlignFlyout();
                }
            }
        }

        private void CreateOnScreenFlyoutWindow()
        {
            OnScreenFlyoutWindow = new FlyoutWindow();
            OnScreenFlyoutView = new FlyoutView();
            OnScreenFlyoutWindow.Content = OnScreenFlyoutView;
        }

        public void AlignFlyout(BindablePoint toPos = null)
        {
            if (toPos != null)
            {
                AlignFlyout(toPos.ToPoint(), true, true);
            }
            else
            {
                AlignFlyout(DefaultFlyoutPosition.ToPoint(), true, true);
            }
        }

        private void AlignFlyout(Point toPos, bool savePos = true, bool updateMonitor = true)
        {
            if (OnScreenFlyoutWindow == null) return;

            OnScreenFlyoutWindow.Left = toPos.X;
            OnScreenFlyoutWindow.Top = toPos.Y;

            if (savePos)
            {
                SaveOnScreenFlyoutPosition();
            }

            if (updateMonitor)
            {
                UpdatePreferredMonitor();
            }
        }

        private void MoveFlyoutToAnotherMonitor()
        {
            if (OnScreenFlyoutWindow == null) return;

            var monitor = OnScreenFlyoutPreferredMonitor;
            var bounds = monitor.Bounds;
            var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

            AlignFlyout(center, true, false);
        }

        private void SaveOnScreenFlyoutPosition()
        {
            if (OnScreenFlyoutWindow != null)
            {
                _flyoutPosition.X = OnScreenFlyoutWindow.Left;
                _flyoutPosition.Y = OnScreenFlyoutWindow.Top;
            }
        }

        private void UpdatePreferredMonitor()
        {
            if (OnScreenFlyoutWindow == null) return;

            var point = new Point(OnScreenFlyoutWindow.Left, OnScreenFlyoutWindow.Top);
            var monitor = DisplayManager.Instance.GetMonitorFromPoint(point);

            if (monitor != null)
            {
                _isPreferredMonitorChanging = true;
                OnScreenFlyoutPreferredMonitor = monitor;
                _isPreferredMonitorChanging = false;
            }
        }
    }
} 