using ModernFlyouts.Core.Display;
using ModernFlyouts.Core.Interop;
using ModernFlyouts.Helpers;
using System;
using static ModernFlyouts.Core.Interop.NativeMethods;

namespace ModernFlyouts.Core
{
    public class WndProc
    {
        private ShellMessageHookHandler shellHook;
        private WndProcHookManager hookManager;

        public void Initialize()
        {
            shellHook = new ShellMessageHookHandler();
            hookManager = WndProcHookManager.GetForBandWindow(FlyoutManager.Instance.OnScreenFlyoutWindow);
            hookManager.RegisterHookHandler(shellHook);

            var displayManager = DisplayManager.Instance;
            hookManager.RegisterHookHandlerForMessage((uint)WindowMessage.WM_SETTINGCHANGE, displayManager);
            hookManager.RegisterHookHandlerForMessage((uint)WindowMessage.WM_DISPLAYCHANGE, displayManager);

            FlyoutManager.Instance.OnScreenFlyoutWindow.CreateWindow();

            hookManager.RegisterCallbackForMessage((uint)WindowMessage.WM_QUERYENDSESSION,
                (_, _, _, _) =>
                {
                    RelaunchHelper.RegisterApplicationRestart(
                        JumpListHelper.arg_appupdated,
                        RelaunchHelper.RestartFlags.RESTART_NO_CRASH |
                        RelaunchHelper.RestartFlags.RESTART_NO_HANG |
                        RelaunchHelper.RestartFlags.RESTART_NO_REBOOT);

                    AppLifecycleManager.PrepareToDie();
                    return IntPtr.Zero;
                });
        }
    }
} 