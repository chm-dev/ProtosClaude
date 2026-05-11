using System.Runtime.InteropServices;
using Protos.Native;

namespace Protos.Hooks
{
    public class KeyboardEventArgs : EventArgs
    {
        public uint VkCode  { get; }
        public uint ScanCode { get; }
        public uint Flags   { get; }
        public bool IsExtended => (Flags & KbdFlags.LLKHF_EXTENDED) != 0;
        public bool IsInjected => (Flags & KbdFlags.LLKHF_INJECTED) != 0;

        public KeyboardEventArgs(uint vkCode, uint scanCode, uint flags)
        {
            VkCode   = vkCode;
            ScanCode = scanCode;
            Flags    = flags;
        }
    }

    public class KeyboardHook : IDisposable
    {
        private IntPtr   _hookHandle = IntPtr.Zero;
        private HookProc _hookProc;   // keep ref to prevent GC

        public event EventHandler<KeyboardEventArgs>? KeyDown;
        public event EventHandler<KeyboardEventArgs>? KeyUp;

        // Return true to suppress the key
        public Func<KeyboardEventArgs, bool>? KeyDownFilter { get; set; }
        public Func<KeyboardEventArgs, bool>? KeyUpFilter   { get; set; }

        public KeyboardHook()
        {
            _hookProc = HookCallback;
        }

        public void Install()
        {
            if (_hookHandle != IntPtr.Zero) return;
            IntPtr hMod = NativeMethods.GetModuleHandle(null);
            _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _hookProc, hMod, 0);
            if (_hookHandle == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to install keyboard hook. Error: {Marshal.GetLastWin32Error()}");
        }

        public void Uninstall()
        {
            if (_hookHandle == IntPtr.Zero) return;
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
                return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

            var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            bool isKeyUp = ((uint)wParam == NativeMethods.WM_KEYUP || (uint)wParam == NativeMethods.WM_SYSKEYUP);
            var args = new KeyboardEventArgs(kbd.vkCode, kbd.scanCode, kbd.flags);

            bool suppress = false;
            if (!isKeyUp)
            {
                if (KeyDownFilter != null)
                    suppress = KeyDownFilter(args);
                if (!suppress)
                    KeyDown?.Invoke(this, args);
            }
            else
            {
                if (KeyUpFilter != null)
                    suppress = KeyUpFilter(args);
                if (!suppress)
                    KeyUp?.Invoke(this, args);
            }

            return suppress ? (IntPtr)1 : NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Uninstall();
            GC.SuppressFinalize(this);
        }

        ~KeyboardHook() => Uninstall();
    }
}
