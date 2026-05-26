using Protos.Native;

namespace Protos.Core
{
    public sealed class CaffeineManager : IDisposable
    {
        private const int VK_CONTROL = 0x11;
        private const int IntervalMs  = 60_000;

        private System.Threading.Timer? _timer;

        public bool IsActive { get; private set; }

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;

            if (active)
            {
                _timer = new System.Threading.Timer(_ => SendCtrl(), null, IntervalMs, IntervalMs);
            }
            else
            {
                _timer?.Dispose();
                _timer = null;
            }
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
        }
    }
}
