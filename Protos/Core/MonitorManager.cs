using Protos.Native;

namespace Protos.Core
{
    public class MonitorManager
    {
        private readonly AppState _state;

        public MonitorManager(AppState state)
        {
            _state = state;
        }

        /// <summary>
        /// Turns the monitor off, then waits for a key press (via hook) to turn it back on.
        /// The actual key-wake is handled by HotkeyDispatcher setting MonitorOffActive=false.
        /// </summary>
        public void TurnOffMonitor()
        {
            if (_state.MonitorOffActive) return;

            // Small delay so the hotkey key-up event doesn't immediately wake the monitor
            Task.Run(async () =>
            {
                await Task.Delay(1000);

                _state.MonitorOffActive = true;

                // Send SC_MONITORPOWER = 2 (power off) to the desktop window
                IntPtr progMan = NativeMethods.GetDesktopWindow();
                NativeMethods.SendMessage(progMan,
                    NativeMethods.WM_SYSCOMMAND,
                    (IntPtr)NativeMethods.SC_MONITORPOWER,
                    (IntPtr)2);

                // MonitorOffActive stays true; first key-down in HotkeyDispatcher clears it
                // and sends MONITOR_ON
            });
        }

        public void TurnOnMonitor()
        {
            _state.MonitorOffActive = false;
            IntPtr progMan = NativeMethods.GetDesktopWindow();
            NativeMethods.SendMessage(progMan,
                NativeMethods.WM_SYSCOMMAND,
                (IntPtr)NativeMethods.SC_MONITORPOWER,
                (IntPtr)(-1));
        }
    }
}
