using System.Diagnostics;
using System.Text.RegularExpressions;
using Protos.Native;

namespace Protos.Core
{
    public class WindowManager
    {
        private readonly AppState _state;
        private int _resizeStep = 80;

        public int ResizeStep
        {
            get => _resizeStep;
            set => _resizeStep = value;
        }

        public WindowManager(AppState state)
        {
            _state = state;
        }

        // ── Cursor window helpers ────────────────────────────────────────────

        public IntPtr GetWindowUnderCursor()
        {
            NativeMethods.GetCursorPos(out POINT pt);
            IntPtr hwnd = NativeMethods.WindowFromPoint(pt);
            if (hwnd == IntPtr.Zero) return IntPtr.Zero;

            // Walk to root owner
            hwnd = NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOTOWNER);

            // Exclude desktop and shell
            IntPtr desktop = NativeMethods.GetDesktopWindow();
            IntPtr shell   = NativeMethods.GetShellWindow();
            if (hwnd == desktop || hwnd == shell) return IntPtr.Zero;

            return hwnd;
        }

        public void ActivateWindowUnderCursor()
        {
            IntPtr hwnd = GetWindowUnderCursor();
            if (hwnd == IntPtr.Zero) return;
            ActivateWindow(hwnd);
        }

        public void ActivateWindow(IntPtr hwnd)
        {
            if (NativeMethods.IsIconic(hwnd))
                NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_RESTORE);
            NativeMethods.SetForegroundWindow(hwnd);
        }

        // ── Always-on-top + transparency ─────────────────────────────────────

        public void ToggleAlwaysOnTop(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            bool isTopmost = (exStyle & NativeMethods.WS_EX_TOPMOST) != 0;

            if (!isTopmost)
            {
                // Set always-on-top and add layered style for transparency
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                    exStyle | NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_LAYERED);
                // Position: HWND_TOPMOST
                SetWindowPos(hwnd, new IntPtr(-1));
                NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 250, NativeMethods.LWA_ALPHA);
            }
            else
            {
                // Remove always-on-top
                SetWindowPos(hwnd, new IntPtr(-2)); // HWND_NOTOPMOST
                // Remove transparency
                int newStyle = exStyle & ~(NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_LAYERED);
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, newStyle);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static void SetWindowPos(IntPtr hwnd, IntPtr insertAfter)
        {
            const uint SWP_NOMOVE = 0x0002;
            const uint SWP_NOSIZE = 0x0001;
            SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        // ── Min / Max / Restore ──────────────────────────────────────────────

        public void MinimizeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_MINIMIZE);
        }

        public void MaximizeWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_MAXIMIZE);
        }

        public void RestoreWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;
            NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_RESTORE);
        }

        // ── Snap / Move to monitor ───────────────────────────────────────────

        public void SnapWindow(bool left)
        {
            // Win+Left or Win+Right via SendInput
            SendKeys.SendKey(WinVK.LWIN,  down: true);
            SendKeys.SendKey(left ? WinVK.LEFT : WinVK.RIGHT, down: true);
            SendKeys.SendKey(left ? WinVK.LEFT : WinVK.RIGHT, down: false);
            SendKeys.SendKey(WinVK.LWIN,  down: false);
        }

        public void MoveWindowToMonitor(bool left)
        {
            // Win+Shift+Left or Win+Shift+Right
            SendKeys.SendKey(WinVK.LWIN,   down: true);
            SendKeys.SendKey(WinVK.LSHIFT, down: true);
            SendKeys.SendKey(left ? WinVK.LEFT : WinVK.RIGHT, down: true);
            SendKeys.SendKey(left ? WinVK.LEFT : WinVK.RIGHT, down: false);
            SendKeys.SendKey(WinVK.LSHIFT, down: false);
            SendKeys.SendKey(WinVK.LWIN,   down: false);
        }

        public void VirtualDesktopSwitch(bool left)
        {
            // Ctrl+Win+Left or Ctrl+Win+Right
            SendKeys.SendKey(WinVK.LCONTROL, down: true);
            SendKeys.SendKey(WinVK.LWIN,     down: true);
            SendKeys.SendKey(left ? WinVK.LEFT : WinVK.RIGHT, down: true);
            SendKeys.SendKey(left ? WinVK.LEFT : WinVK.RIGHT, down: false);
            SendKeys.SendKey(WinVK.LWIN,     down: false);
            SendKeys.SendKey(WinVK.LCONTROL, down: false);
        }

        // ── Monitor info ─────────────────────────────────────────────────────

        public (int left, int top, int width, int height) GetMonitorInfoForWindow(IntPtr hwnd)
        {
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            return GetMonitorBounds(hMonitor);
        }

        public (int left, int top, int width, int height) GetMonitorInfoForMouse()
        {
            NativeMethods.GetCursorPos(out POINT pt);
            IntPtr hMonitor = NativeMethods.MonitorFromPoint(pt, NativeMethods.MONITOR_DEFAULTTONEAREST);
            return GetMonitorBounds(hMonitor);
        }

        private static (int left, int top, int width, int height) GetMonitorBounds(IntPtr hMonitor)
        {
            var mi = new MONITORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
            NativeMethods.GetMonitorInfo(hMonitor, ref mi);
            return (mi.rcWork.Left, mi.rcWork.Top, mi.rcWork.Width, mi.rcWork.Height);
        }

        // ── Resize ───────────────────────────────────────────────────────────

        public void ResizeActiveWindow(double scaleX, double scaleY)
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            // Restore if maximized
            if (NativeMethods.IsZoomed(hwnd))
                NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_RESTORE);

            var (ml, mt, mw, mh) = GetMonitorInfoForWindow(hwnd);

            int newW = (int)(mw * scaleX);
            int newH = (int)(mh * scaleY);
            int newX = ml + (mw - newW) / 2;
            int newY = mt + (mh - newH) / 2;

            NativeMethods.MoveWindow(hwnd, newX, newY, newW, newH, true);
        }

        public void EnlargeWindowUnderCursor()
        {
            IntPtr hwnd = GetWindowUnderCursor();
            if (hwnd == IntPtr.Zero) return;
            ResizeWindowByCursor(hwnd, +_resizeStep / 2);
        }

        public void ShrinkWindowUnderCursor()
        {
            IntPtr hwnd = GetWindowUnderCursor();
            if (hwnd == IntPtr.Zero) return;
            ResizeWindowByCursor(hwnd, -_resizeStep / 2);
        }

        private static void ResizeWindowByCursor(IntPtr hwnd, int delta)
        {
            if (!NativeMethods.GetWindowRect(hwnd, out RECT r)) return;

            int cx   = r.Left + r.Width  / 2;
            int cy   = r.Top  + r.Height / 2;
            int newW = Math.Max(100, r.Width  + delta * 2);
            int newH = Math.Max(100, r.Height + delta * 2);
            int newX = cx - newW / 2;
            int newY = cy - newH / 2;

            NativeMethods.MoveWindow(hwnd, newX, newY, newW, newH, true);
        }

        // ── Drag ─────────────────────────────────────────────────────────────

        public void StartDrag(IntPtr hwnd, POINT startMouse)
        {
            if (hwnd == IntPtr.Zero) return;
            if (NativeMethods.IsZoomed(hwnd)) return; // don't drag maximized

            if (!NativeMethods.GetWindowRect(hwnd, out RECT r)) return;

            _state.DragActive      = true;
            _state.DragHwnd        = hwnd;
            _state.DragStartMouse  = startMouse;
            _state.DragStartWindow = new POINT(r.Left, r.Top);
        }

        public void UpdateDrag(POINT currentMouse)
        {
            if (!_state.DragActive || _state.DragHwnd == IntPtr.Zero) return;

            int dx = currentMouse.X - _state.DragStartMouse.X;
            int dy = currentMouse.Y - _state.DragStartMouse.Y;
            int newX = _state.DragStartWindow.X + dx;
            int newY = _state.DragStartWindow.Y + dy;

            if (!NativeMethods.GetWindowRect(_state.DragHwnd, out RECT r)) return;
            NativeMethods.MoveWindow(_state.DragHwnd, newX, newY, r.Width, r.Height, true);
        }

        public void EndDrag()
        {
            _state.DragActive = false;
            _state.DragHwnd   = IntPtr.Zero;
        }

        // ── querySelector clipboard transform ────────────────────────────────

        public void ToggleQuerySelectorClipboard()
        {
            string clip = GetClipboardText();
            if (string.IsNullOrEmpty(clip)) return;

            string result;
            if (!clip.Contains("document.query"))
            {
                // Strip outer quotes
                string inner = Regex.Replace(clip, @"^""(.+)""$", "$1");
                inner = inner.Replace("\"\"", "\"");
                result = $"document.querySelector('{inner}')";
            }
            else
            {
                // Unwrap querySelector → quoted string
                string inner = Regex.Replace(clip,
                    @"document\.querySelector(?:All)?\('?([^\(\)]*)'?\)", "$1");
                inner = inner.Replace("\"", "\"\"");
                result = $"\"{inner}\"";
            }

            SetClipboardText(result);
            // Send Ctrl+V to paste
            SendKeys.SendKey(WinVK.LCONTROL, down: true);
            SendKeys.SendKey(WinVK.V,        down: true);
            SendKeys.SendKey(WinVK.V,        down: false);
            SendKeys.SendKey(WinVK.LCONTROL, down: false);
        }

        private static string GetClipboardText()
        {
            try
            {
                if (!NativeMethods.OpenClipboard(IntPtr.Zero)) return string.Empty;
                IntPtr hData = NativeMethods.GetClipboardData(NativeMethods.CF_UNICODETEXT);
                if (hData == IntPtr.Zero) { NativeMethods.CloseClipboard(); return string.Empty; }
                IntPtr ptr = NativeMethods.GlobalLock(hData);
                if (ptr == IntPtr.Zero) { NativeMethods.CloseClipboard(); return string.Empty; }
                string text = System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr) ?? string.Empty;
                NativeMethods.GlobalUnlock(hData);
                NativeMethods.CloseClipboard();
                return text;
            }
            catch { return string.Empty; }
        }

        private static void SetClipboardText(string text)
        {
            try
            {
                if (!NativeMethods.OpenClipboard(IntPtr.Zero)) return;
                NativeMethods.EmptyClipboard();
                int    byteCount = (text.Length + 1) * 2;
                IntPtr hMem = NativeMethods.GlobalAlloc(NativeMethods.GMEM_MOVEABLE, (UIntPtr)byteCount);
                if (hMem == IntPtr.Zero) { NativeMethods.CloseClipboard(); return; }
                IntPtr ptr = NativeMethods.GlobalLock(hMem);
                System.Runtime.InteropServices.Marshal.Copy(text.ToCharArray(), 0, ptr, text.Length);
                System.Runtime.InteropServices.Marshal.WriteInt16(ptr, text.Length * 2, 0);
                NativeMethods.GlobalUnlock(hMem);
                NativeMethods.SetClipboardData(NativeMethods.CF_UNICODETEXT, hMem);
                NativeMethods.CloseClipboard();
            }
            catch { }
        }

        // ── Paste raw clipboard ──────────────────────────────────────────────

        public void PasteRawClipboard()
        {
            string text = GetClipboardText();
            if (string.IsNullOrEmpty(text)) return;
            SendKeys.SendUnicodeString(text);
        }

        // ── Force-kill active window process ─────────────────────────────────

        public void ForceKillActiveWindow(bool forceFlag)
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return;

            try
            {
                var proc = Process.GetProcessById((int)pid);
                string exeName = proc.ProcessName + ".exe";
                string args = forceFlag ? $"/IM \"{exeName}\" /F" : $"/IM \"{exeName}\"";
                Process.Start(new ProcessStartInfo("taskkill", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                });
            }
            catch { }
        }
    }

    /// <summary>Simple SendInput helper.</summary>
    public static class SendKeys
    {
        public static void SendKey(uint vk, bool down, bool extended = false)
        {
            var input = new INPUT
            {
                type = InputType.INPUT_KEYBOARD,
                u = new INPUT_UNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk     = (ushort)vk,
                        wScan   = 0,
                        dwFlags = (down ? 0u : KeyEventF.KEYEVENTF_KEYUP) |
                                  (extended ? KeyEventF.KEYEVENTF_EXTENDEDKEY : 0u),
                        time    = 0,
                        dwExtraInfo = UIntPtr.Zero,
                    }
                }
            };
            NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
        }

        public static void SendMediaKey(uint vk)
        {
            SendKey(vk, down: true,  extended: true);
            SendKey(vk, down: false, extended: true);
        }

        public static void SendUnicodeString(string text)
        {
            var inputs = new INPUT[text.Length * 2];
            int idx = 0;
            foreach (char c in text)
            {
                inputs[idx++] = new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    u = new INPUT_UNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk     = 0,
                            wScan   = c,
                            dwFlags = KeyEventF.KEYEVENTF_UNICODE,
                        }
                    }
                };
                inputs[idx++] = new INPUT
                {
                    type = InputType.INPUT_KEYBOARD,
                    u = new INPUT_UNION
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk     = 0,
                            wScan   = c,
                            dwFlags = KeyEventF.KEYEVENTF_UNICODE | KeyEventF.KEYEVENTF_KEYUP,
                        }
                    }
                };
            }
            NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
        }

        public static void SendBackspaces(int count)
        {
            var inputs = new INPUT[count * 2];
            for (int i = 0; i < count; i++)
            {
                inputs[i * 2] = MakeKey(0x08 /* VK_BACK */, false);
                inputs[i * 2 + 1] = MakeKey(0x08 /* VK_BACK */, true);
            }
            NativeMethods.SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
        }

        private static INPUT MakeKey(uint vk, bool keyUp)
        {
            return new INPUT
            {
                type = InputType.INPUT_KEYBOARD,
                u = new INPUT_UNION
                {
                    ki = new KEYBDINPUT
                    {
                        wVk     = (ushort)vk,
                        dwFlags = keyUp ? KeyEventF.KEYEVENTF_KEYUP : 0u,
                    }
                }
            };
        }
    }

    /// <summary>VK constants used by WindowManager / SendKeys (avoids ambiguous resolution).</summary>
    internal static class WinVK
    {
        public const uint LSHIFT   = 0xA0;
        public const uint LCONTROL = 0xA2;
        public const uint LMENU    = 0xA4;
        public const uint LWIN     = 0x5B;
        public const uint LEFT     = 0x25;
        public const uint RIGHT    = 0x27;
        public const uint UP       = 0x26;
        public const uint DOWN     = 0x28;
        public const uint V        = 0x56;
        public const uint BACK     = 0x08;
    }
}
