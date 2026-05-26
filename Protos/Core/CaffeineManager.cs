using Protos.Native;

namespace Protos.Core
{
    public sealed class CaffeineManager : IDisposable
    {
        private const int VK_CONTROL = 0x11;
        private const int IntervalMs  = 60_000;

        private System.Threading.Timer? _timer;
        private NotifyIcon?             _trayIcon;

        public bool IsActive { get; private set; }

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;

            if (active)
            {
                _timer = new System.Threading.Timer(_ => SendCtrl(), null, IntervalMs, IntervalMs);
                EnsureTrayIcon();
                _trayIcon!.Visible = true;
            }
            else
            {
                _timer?.Dispose();
                _timer = null;
                if (_trayIcon != null) _trayIcon.Visible = false;
            }
        }

        private void EnsureTrayIcon()
        {
            if (_trayIcon != null) return;

            Icon icon;
            try
            {
                string? exePath = Environment.ProcessPath;
                icon = !string.IsNullOrEmpty(exePath)
                    ? Icon.ExtractAssociatedIcon(exePath) ?? SystemIcons.Application
                    : SystemIcons.Application;
            }
            catch { icon = SystemIcons.Application; }

            _trayIcon = new NotifyIcon
            {
                Icon    = icon,
                Text    = "Caffeine: ON",
                Visible = false,
            };
        }

        private static void SendCtrl()
        {
            NativeMethods.keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            NativeMethods.keybd_event(VK_CONTROL, 0, 0x0002 /* KEYEVENTF_KEYUP */, UIntPtr.Zero);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
        }
    }
}
