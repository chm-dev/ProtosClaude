using System.Runtime.InteropServices;
using Protos.Native;

namespace Protos.Hooks
{
    public enum MouseButton { Left, Right, Middle, XButton1, XButton2 }
    public enum WheelDirection { Up, Down }

    public class MouseButtonEventArgs : EventArgs
    {
        public MouseButton Button { get; }
        public POINT       Position { get; }
        public MouseButtonEventArgs(MouseButton btn, POINT pt) { Button = btn; Position = pt; }
    }

    public class MouseWheelEventArgs : EventArgs
    {
        public WheelDirection Direction { get; }
        public POINT          Position  { get; }
        public int            Delta     { get; }
        public MouseWheelEventArgs(WheelDirection dir, POINT pt, int delta) { Direction = dir; Position = pt; Delta = delta; }
    }

    public class MouseMoveEventArgs : EventArgs
    {
        public POINT Position { get; }
        public MouseMoveEventArgs(POINT pt) { Position = pt; }
    }

    public class MouseHook : IDisposable
    {
        private IntPtr   _hookHandle = IntPtr.Zero;
        private HookProc _hookProc;

        public event EventHandler<MouseButtonEventArgs>? ButtonDown;
        public event EventHandler<MouseButtonEventArgs>? ButtonUp;
        public event EventHandler<MouseWheelEventArgs>?  WheelEvent;
        public event EventHandler<MouseMoveEventArgs>?   MouseMove;

        // Return true to suppress
        public Func<MouseButtonEventArgs, bool>? ButtonDownFilter { get; set; }
        public Func<MouseButtonEventArgs, bool>? ButtonUpFilter   { get; set; }
        public Func<MouseWheelEventArgs, bool>?  WheelFilter      { get; set; }

        public MouseHook()
        {
            _hookProc = HookCallback;
        }

        public void Install()
        {
            if (_hookHandle != IntPtr.Zero) return;
            IntPtr hMod = NativeMethods.GetModuleHandle(null);
            _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _hookProc, hMod, 0);
            if (_hookHandle == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to install mouse hook. Error: {Marshal.GetLastWin32Error()}");
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

            var mll = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            uint msg = (uint)wParam;
            bool suppress = false;

            switch (msg)
            {
                case NativeMethods.WM_LBUTTONDOWN:
                    suppress = FireButtonDown(MouseButton.Left, mll.pt);
                    break;
                case NativeMethods.WM_LBUTTONUP:
                    suppress = FireButtonUp(MouseButton.Left, mll.pt);
                    break;
                case NativeMethods.WM_RBUTTONDOWN:
                    suppress = FireButtonDown(MouseButton.Right, mll.pt);
                    break;
                case NativeMethods.WM_RBUTTONUP:
                    suppress = FireButtonUp(MouseButton.Right, mll.pt);
                    break;
                case NativeMethods.WM_MBUTTONDOWN:
                    suppress = FireButtonDown(MouseButton.Middle, mll.pt);
                    break;
                case NativeMethods.WM_MBUTTONUP:
                    suppress = FireButtonUp(MouseButton.Middle, mll.pt);
                    break;
                case NativeMethods.WM_XBUTTONDOWN:
                {
                    int which = (int)(mll.mouseData >> 16);
                    var btn   = which == 1 ? MouseButton.XButton1 : MouseButton.XButton2;
                    suppress  = FireButtonDown(btn, mll.pt);
                    break;
                }
                case NativeMethods.WM_XBUTTONUP:
                {
                    int which = (int)(mll.mouseData >> 16);
                    var btn   = which == 1 ? MouseButton.XButton1 : MouseButton.XButton2;
                    suppress  = FireButtonUp(btn, mll.pt);
                    break;
                }
                case NativeMethods.WM_MOUSEWHEEL:
                {
                    short delta = (short)(mll.mouseData >> 16);
                    var dir = delta > 0 ? WheelDirection.Up : WheelDirection.Down;
                    var args = new MouseWheelEventArgs(dir, mll.pt, delta);
                    if (WheelFilter != null)
                        suppress = WheelFilter(args);
                    if (!suppress)
                        WheelEvent?.Invoke(this, args);
                    break;
                }
                default:
                    // WM_MOUSEMOVE and others — no suppression
                    if (msg == 0x0200) // WM_MOUSEMOVE
                        MouseMove?.Invoke(this, new MouseMoveEventArgs(mll.pt));
                    break;
            }

            return suppress ? (IntPtr)1 : NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private bool FireButtonDown(MouseButton btn, POINT pt)
        {
            var args = new MouseButtonEventArgs(btn, pt);
            bool suppress = false;
            if (ButtonDownFilter != null)
                suppress = ButtonDownFilter(args);
            if (!suppress)
                ButtonDown?.Invoke(this, args);
            return suppress;
        }

        private bool FireButtonUp(MouseButton btn, POINT pt)
        {
            var args = new MouseButtonEventArgs(btn, pt);
            bool suppress = false;
            if (ButtonUpFilter != null)
                suppress = ButtonUpFilter(args);
            if (!suppress)
                ButtonUp?.Invoke(this, args);
            return suppress;
        }

        public void Dispose()
        {
            Uninstall();
            GC.SuppressFinalize(this);
        }

        ~MouseHook() => Uninstall();
    }
}
