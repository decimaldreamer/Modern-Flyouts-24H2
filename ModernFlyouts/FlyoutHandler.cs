using CommunityToolkit.Mvvm.ComponentModel;
using ModernFlyouts.AppLifecycle;
using ModernFlyouts.Controls;
using ModernFlyouts.Core;
using ModernFlyouts.Core.Display;
using ModernFlyouts.Core.Interop;
using ModernFlyouts.Core.UI;
using ModernFlyouts.Core.Utilities;
using ModernFlyouts.Helpers;
using ModernFlyouts.Interop;
using ModernFlyouts.UI;
using ModernFlyouts.UI.Media;
using ModernFlyouts.Views;
using ModernFlyouts.Workarounds;
using ModernWpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static ModernFlyouts.Core.Interop.NativeMethods;

namespace ModernFlyouts
{
    public class FlyoutHandler : ObservableObject
    {
        public static event EventHandler Initialized;

        private bool _isPreferredMonitorChanging;
        private bool _savePreferredMonitor = true;

        private List<FlyoutHelperBase> flyoutHelpers = new();
        private AirplaneModeWatcher airplaneModeWatcher = new();
        private FlyoutTriggerData prevTriggerData;

        #region Properties

        public static FlyoutHandler Instance { get; set; }

        public static bool HasInitialized { get; private set; }

        public KeyboardHook KeyboardHook { get; private set; }

        public SettingsWindow SettingsWindow { get; set; }

        public AudioFlyoutHelper AudioFlyoutHelper { get; set; }

        public AirplaneModeFlyoutHelper AirplaneModeFlyoutHelper { get; set; }

        public LockKeysFlyoutHelper LockKeysFlyoutHelper { get; set; }

        public BrightnessFlyoutHelper BrightnessFlyoutHelper { get; set; }

        private bool brightnessCompatibility = DefaultValuesStore.BrightnessCompatibility;

        public bool BrightnessCompatibility
        {
            get => brightnessCompatibility;
            set
            {
                if (SetProperty(ref brightnessCompatibility, value))
                {
                    OnBrightnessCompatibilityChanged();
                }
            }
        }

        private Orientation onScreenFlyoutOrientation = DefaultValuesStore.FlyoutOrientation;

        public Orientation OnScreenFlyoutOrientation
        {
            get => onScreenFlyoutOrientation;
            set
            {
                if (SetProperty(ref onScreenFlyoutOrientation, value))
                {
                    OnFlyoutOrientationChanged();
                }
            }
        }

        private DefaultFlyout defaultFlyout = DefaultValuesStore.PreferredDefaultFlyout;

        public DefaultFlyout DefaultFlyout
        {
            get => defaultFlyout;
            set
            {
                if (SetProperty(ref defaultFlyout, value))
                {
                    OnDefaultFlyoutChanged();
                }
            }
        }

        private bool runAtStartup;

        public bool RunAtStartup
        {
            get => runAtStartup;
            set
            {
                if (SetProperty(ref runAtStartup, value))
                {
                    OnRunAtStartupChanged();
                }
            }
        }

        #endregion

        public void Initialize()
        {
            FlyoutManager.Instance.Initialize();

            CreateWndProc();

            RenderLoopFix.Initialize();

            NativeFlyoutHandler.Instance.NativeFlyoutShown += (_, _) => OnNativeFlyoutShown();
            KeyboardHook = new KeyboardHook();

            #region App Data

            var adEnabled = AppDataHelper.AudioModuleEnabled;
            var apmdEnabled = AppDataHelper.AirplaneModeModuleEnabled;
            var lkkyEnabled = AppDataHelper.LockKeysModuleEnabled;
            var brEnabled = AppDataHelper.BrightnessModuleEnabled;

            DefaultFlyout = AppDataHelper.DefaultFlyout;

            onScreenFlyoutOrientation = AppDataHelper.FlyoutOrientation;
            
            FlyoutManager.Instance.OnScreenFlyoutView.ContentStackPanel.Orientation = OnScreenFlyoutOrientation switch
            {
                Orientation.Vertical => Orientation.Horizontal,
                _ => Orientation.Vertical,
            };

            async void getStartupStatus()
            {
                RunAtStartup = await StartupHelper.GetRunAtStartupEnabled();
            }

            getStartupStatus();

            #endregion

            #region Initiate Helpers

            AudioFlyoutHelper = new AudioFlyoutHelper(OnScreenFlyoutOrientation) { IsEnabled = adEnabled };
            AudioFlyoutHelper.SetupEvents();
            AirplaneModeFlyoutHelper = new AirplaneModeFlyoutHelper() { IsEnabled = apmdEnabled };
            LockKeysFlyoutHelper = new LockKeysFlyoutHelper() { IsEnabled = lkkyEnabled };
            BrightnessFlyoutHelper = new BrightnessFlyoutHelper() { IsEnabled = brEnabled, CompatibilityMode = AppDataHelper.BrightnessCompatibility };

            flyoutHelpers.Add(AudioFlyoutHelper);
            flyoutHelpers.Add(AirplaneModeFlyoutHelper);
            flyoutHelpers.Add(LockKeysFlyoutHelper);
            flyoutHelpers.Add(BrightnessFlyoutHelper);

            foreach (var flyoutHelper in flyoutHelpers)
            {
                flyoutHelper.ShowFlyoutRequested += ShowFlyout;
            }

            #endregion

            HasInitialized = true;
            Initialized?.Invoke(this, EventArgs.Empty);

            DisplayManager.Instance.DisplayUpdated += Instance_DisplayUpdated;
            airplaneModeWatcher.Changed += AirplaneModeWatcher_Changed;
            airplaneModeWatcher.Start();
        }

        private void AirplaneModeWatcher_Changed(object sender, AirplaneModeChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                FlyoutTriggerData triggerData = new()
                {
                    TriggerType = FlyoutTriggerType.AirplaneMode,
                    AirplaneModeState = e.IsEnabled
                };
                ProcessFlyoutTrigger(triggerData);
            });
        }

        private void Instance_DisplayUpdated(object sender, EventArgs e)
        {
            if (FlyoutManager.Instance.OnScreenFlyoutWindow != null)
            {
                FlyoutManager.Instance.AlignFlyout();
            }
        }

        private void OnNativeFlyoutShown()
        {
            if (DefaultFlyout == DefaultFlyout.WindowsDefault)
            {
                return;
            }

            if (DefaultFlyout == DefaultFlyout.None)
            {
                NativeFlyoutHandler.Instance.HideNativeFlyout();
                return;
            }

            if (prevTriggerData != null)
            {
                ProcessFlyoutTrigger(prevTriggerData);
            }
        }

        internal void ProcessFlyoutTrigger(FlyoutTriggerData triggerData = null)
        {
            if (triggerData != null)
            {
                prevTriggerData = triggerData;
            }

            if (DefaultFlyout == DefaultFlyout.WindowsDefault)
            {
                return;
            }

            if (DefaultFlyout == DefaultFlyout.None)
            {
                NativeFlyoutHandler.Instance.HideNativeFlyout();
                return;
            }

            if (triggerData != null)
            {
                var helper = flyoutHelpers.FirstOrDefault(x => x.IsEnabled && x.CanHandleTrigger(triggerData));
                if (helper != null)
                {
                    ShowFlyout(helper);
                }
            }
        }

        private void ShowFlyout(FlyoutHelperBase helper)
        {
            if (helper == null || !helper.IsEnabled)
            {
                return;
            }

            if (DefaultFlyout == DefaultFlyout.WindowsDefault)
            {
                return;
            }

            if (DefaultFlyout == DefaultFlyout.None)
            {
                NativeFlyoutHandler.Instance.HideNativeFlyout();
                return;
            }

            NativeFlyoutHandler.Instance.HideNativeFlyout();
            helper.ShowFlyout();
        }

        private bool Handled()
        {
            return DefaultFlyout != DefaultFlyout.WindowsDefault;
        }

        private void OnDefaultFlyoutChanged()
        {
            if (DefaultFlyout == DefaultFlyout.WindowsDefault)
            {
                NativeFlyoutHandler.Instance.ShowNativeFlyout();
            }
            else if (DefaultFlyout == DefaultFlyout.None)
            {
                NativeFlyoutHandler.Instance.HideNativeFlyout();
            }
            else
            {
                if (prevTriggerData != null)
                {
                    ProcessFlyoutTrigger(prevTriggerData);
                }
            }
        }

        private void OnRunAtStartupChanged()
        {
            StartupHelper.SetRunAtStartupEnabled(RunAtStartup);
        }

        private void CreateWndProc()
        {
            var wndProc = new WndProc();
            wndProc.Initialize();
        }

        private void OnBrightnessCompatibilityChanged()
        {
            if (BrightnessFlyoutHelper != null)
            {
                BrightnessFlyoutHelper.CompatibilityMode = BrightnessCompatibility;
            }
        }

        private void OnFlyoutOrientationChanged()
        {
            if (FlyoutManager.Instance.OnScreenFlyoutView != null)
            {
                FlyoutManager.Instance.OnScreenFlyoutView.ContentStackPanel.Orientation = OnScreenFlyoutOrientation switch
                {
                    Orientation.Vertical => Orientation.Horizontal,
                    _ => Orientation.Vertical,
                };
            }
        }

        public static void SafelyExitApplication()
        {
            Application.Current.Shutdown();
        }

        public static void ShowSettingsWindow()
        {
            if (Instance.SettingsWindow == null)
            {
                Instance.SettingsWindow = new SettingsWindow();
            }
            Instance.SettingsWindow.Show();
            Instance.SettingsWindow.Activate();
        }
    }

    public enum DefaultFlyout
    {
        WindowsDefault = 0,
        ModernFlyouts = 1,
        None = 2
    }
}
