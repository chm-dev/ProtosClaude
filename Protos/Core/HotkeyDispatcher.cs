using Protos.Hooks;
using Protos.Native;
using Protos.Settings;

namespace Protos.Core
{
    /// <summary>
    /// Central dispatcher: subscribes to keyboard and mouse hooks, checks AppState,
    /// and dispatches to action handlers.
    /// </summary>
    public class HotkeyDispatcher : IDisposable
    {
        private readonly AppState       _state;
        private readonly KeyboardHook   _kbdHook;
        private readonly MouseHook      _mouseHook;
        private readonly WindowManager  _winMgr;
        private readonly VolumeManager  _volMgr;
        private readonly MonitorManager _monMgr;
        private readonly SpotifyManager _spotifyMgr;
        private readonly HotstringManager _hotstringMgr;

        private AppSettings _settings;

        public HotkeyDispatcher(
            AppState       state,
            KeyboardHook   kbdHook,
            MouseHook      mouseHook,
            WindowManager  winMgr,
            VolumeManager  volMgr,
            MonitorManager monMgr,
            SpotifyManager spotifyMgr,
            HotstringManager hotstringMgr,
            AppSettings    settings)
        {
            _state        = state;
            _kbdHook      = kbdHook;
            _mouseHook    = mouseHook;
            _winMgr       = winMgr;
            _volMgr       = volMgr;
            _monMgr       = monMgr;
            _spotifyMgr   = spotifyMgr;
            _hotstringMgr = hotstringMgr;
            _settings     = settings;

            _kbdHook.KeyDownFilter = OnKeyDown;
            _kbdHook.KeyUpFilter   = OnKeyUp;
            _mouseHook.ButtonDownFilter = OnMouseButtonDown;
            _mouseHook.ButtonUpFilter   = OnMouseButtonUp;
            _mouseHook.WheelFilter      = OnMouseWheel;
            _mouseHook.MouseMove       += OnMouseMove;
        }

        public void UpdateSettings(AppSettings settings)
        {
            _settings = settings;
            _winMgr.ResizeStep       = settings.ResizeStep;
            _volMgr.VolumeStep       = settings.VolumeStep;
            _spotifyMgr.SpotifyPath  = settings.SpotifyPath;
            _hotstringMgr.SetHotstrings(settings.Hotstrings);
        }

        // ── Keyboard: key down ────────────────────────────────────────────────

        private bool OnKeyDown(KeyboardEventArgs e)
        {
            uint vk = e.VkCode;

            // Always suppress NumLock and handle state
            if (vk == VirtualKeys.VK_NUMLOCK)
                return true;

            // Always suppress CapsLock and track state
            if (vk == (uint)NativeVK.VK_CAPITAL)
            {
                _state.CapsLockHeld = true;
                return true;
            }

            // If monitor is off, any key wakes it
            if (_state.MonitorOffActive)
            {
                _state.MonitorOffActive = false;
                _monMgr.TurnOnMonitor();
                return true; // consume the wake key
            }

            // Track modifier states
            UpdateModifierDown(vk);

            // Suspended: only allow resume and exit
            if (_state.Suspended)
            {
                // Home + PgUp → resume
                if (_state.HomeHeld && vk == (uint)NativeVK.VK_PRIOR)
                {
                    Resume();
                    return false; // pass through Home
                }
                // Ctrl+Alt+Shift+End → exit
                if (IsCtrlAltShift() && vk == (uint)NativeVK.VK_END)
                {
                    ExitApp();
                    return true;
                }
                return false;
            }

            // ── Hotkeys ──────────────────────────────────────────────────────

            bool capsLock = _state.CapsLockHeld;
            bool lCtrl    = _state.LCtrlHeld;
            bool lShift   = _state.LShiftHeld;
            bool lAlt     = _state.LAltHeld;

            bool ctrl  = IsCtrlHeld();
            bool alt   = IsAltHeld();
            bool shift = IsShiftHeld();
            bool win   = IsWinHeld();

            // Reload
            if (capsLock && vk == (uint)NativeVK.VK_R) { ReloadApp(); return true; }

            // Suspend resume / suspend
            if (_state.HomeHeld && vk == (uint)NativeVK.VK_PRIOR) { Resume();  return false; } // Home pass-through
            if (_state.HomeHeld && vk == (uint)NativeVK.VK_NEXT)  { Suspend(); return false; }

            // Exit
            if (ctrl && alt && shift && vk == (uint)NativeVK.VK_END) { ExitApp(); return true; }

            // CapsLock + WheelDown/Up → min/max handled in mouse
            // CapsLock + MButton → restore handled in mouse

            // CapsLock + XButton1/2 → snap (handled in mouse)

            // Ctrl+Alt+V → paste raw
            if (ctrl && alt && !shift && vk == (uint)NativeVK.VK_V) { _winMgr.PasteRawClipboard(); return true; }

            // Win+V → Shift+Alt+V
            if (win && !shift && vk == (uint)NativeVK.VK_V && !ctrl && !alt)
            {
                SendKeys.SendKey(VirtualKeys_Native.VK_LSHIFT, true);
                SendKeys.SendKey(VirtualKeys_Native.VK_LMENU,  true);
                SendKeys.SendKey(VirtualKeys_Native.VK_V,      true);
                SendKeys.SendKey(VirtualKeys_Native.VK_V,      false);
                SendKeys.SendKey(VirtualKeys_Native.VK_LMENU,  false);
                SendKeys.SendKey(VirtualKeys_Native.VK_LSHIFT, false);
                return true;
            }

            // Win+Shift+V → Win+V
            if (win && shift && vk == (uint)NativeVK.VK_V)
            {
                SendKeys.SendKey(VirtualKeys_Native.VK_LWIN, true);
                SendKeys.SendKey(VirtualKeys_Native.VK_V,    true);
                SendKeys.SendKey(VirtualKeys_Native.VK_V,    false);
                SendKeys.SendKey(VirtualKeys_Native.VK_LWIN, false);
                return true;
            }

            // CapsLock + S → Spotify toggle
            if (capsLock && vk == (uint)NativeVK.VK_S) { _spotifyMgr.Toggle(); return true; }

            // CapsLock + Z → resize active to 60%×60%
            if (capsLock && vk == (uint)NativeVK.VK_Z) { _winMgr.ResizeActiveWindow(0.6, 0.6); return true; }

            // CapsLock + Numpad5 → monitor off
            if (capsLock && vk == (uint)NativeVK.VK_NUMPAD5) { _monMgr.TurnOffMonitor(); return true; }

            // Escape → hide Spotify if active
            if (vk == (uint)NativeVK.VK_ESCAPE)
            {
                if (_spotifyMgr.TryHideOnEsc()) return true;
            }

            // Numpad media keys (NumLock off, non-extended)
            if (!e.IsExtended)
            {
                if (vk == (uint)NativeVK.VK_INSERT) { SendMediaKey(0xB3); return true; } // Media_Play_Pause
                if (vk == (uint)NativeVK.VK_DELETE) { SendMediaKey(0xB2); return true; } // Media_Stop
                if (vk == (uint)NativeVK.VK_END)    { SendMediaKey(0xB1); return true; } // Media_Prev
                if (vk == (uint)NativeVK.VK_DOWN)   { SendMediaKey(0xB0); return true; } // Media_Next
            }

            // NumpadAdd/Sub → volume
            if (vk == (uint)NativeVK.VK_ADD)
            {
                for (int i = 0; i < 4; i++) SendMediaKey(0xAF); // VK_VOLUME_UP
                return true;
            }
            if (vk == (uint)NativeVK.VK_SUBTRACT)
            {
                for (int i = 0; i < 4; i++) SendMediaKey(0xAE); // VK_VOLUME_DOWN
                return true;
            }

            // Hotstring buffer update
            bool shiftHeld = IsShiftHeld();
            bool consumed = _hotstringMgr.OnKeyDown(vk, shiftHeld);
            return consumed;
        }

        // ── Keyboard: key up ─────────────────────────────────────────────────

        private bool OnKeyUp(KeyboardEventArgs e)
        {
            uint vk = e.VkCode;

            // Always suppress CapsLock
            if (vk == (uint)NativeVK.VK_CAPITAL)
            {
                _state.CapsLockHeld = false;
                return true;
            }

            // Always suppress NumLock
            if (vk == VirtualKeys.VK_NUMLOCK)
                return true;

            UpdateModifierUp(vk);

            if (_state.Suspended) return false;

            return false;
        }

        // ── Mouse: button down ───────────────────────────────────────────────

        private bool OnMouseButtonDown(MouseButtonEventArgs e)
        {
            // Update state
            switch (e.Button)
            {
                case MouseButton.Left:     _state.LButtonDown  = true; break;
                case MouseButton.Right:    _state.RButtonDown  = true; break;
                case MouseButton.Middle:   _state.MButtonDown  = true; break;
                case MouseButton.XButton1: _state.XButton1Down = true; break;
                case MouseButton.XButton2: _state.XButton2Down = true; break;
            }

            // Clear hotstring buffer on mouse click
            if (e.Button == MouseButton.Left || e.Button == MouseButton.Right)
                _hotstringMgr.ClearOnMouseClick();

            if (_state.Suspended) return false;

            bool capsLock = _state.CapsLockHeld;
            bool ctrl     = IsCtrlHeld();
            bool alt      = IsAltHeld();
            bool shift    = IsShiftHeld();

            switch (e.Button)
            {
                case MouseButton.Left:
                    // CapsLock + LButton → toggle always-on-top
                    if (capsLock)
                    {
                        IntPtr hwnd = _winMgr.GetWindowUnderCursor();
                        if (hwnd != IntPtr.Zero) _winMgr.ToggleAlwaysOnTop(hwnd);
                        return true;
                    }
                    // RAlt + LButton → start drag
                    if (_state.RAltHeld)
                    {
                        IntPtr hwnd = _winMgr.GetWindowUnderCursor();
                        if (hwnd != IntPtr.Zero) _winMgr.StartDrag(hwnd, e.Position);
                        return false; // pass through (AHK uses ~)
                    }
                    break;

                case MouseButton.Right:
                    // F4 + RButton → force-kill active window
                    if (_state.F4Held)
                    {
                        _winMgr.ForceKillActiveWindow(shift);
                        return true;
                    }
                    break;

                case MouseButton.Middle:
                    // CapsLock + MButton → restore window under cursor
                    if (capsLock)
                    {
                        IntPtr hwnd = _winMgr.GetWindowUnderCursor();
                        if (hwnd != IntPtr.Zero) _winMgr.RestoreWindow(hwnd);
                        return true;
                    }
                    // Ctrl+Alt+MButton or XButton1+MButton → mute
                    if ((ctrl && alt) || _state.XButton1Down)
                    {
                        _volMgr.ToggleMute();
                        return true;
                    }
                    break;

                case MouseButton.XButton1:
                    // CapsLock + XButton1 → activate cursor window + snap left
                    if (capsLock)
                    {
                        _winMgr.ActivateWindowUnderCursor();
                        _winMgr.SnapWindow(left: true);
                        return true;
                    }
                    // Alt + XButton1 → activate cursor window + move to left monitor
                    if (alt)
                    {
                        _winMgr.ActivateWindowUnderCursor();
                        _winMgr.MoveWindowToMonitor(left: true);
                        return true;
                    }
                    // Ctrl + XButton1 → move to left monitor
                    if (ctrl)
                    {
                        _winMgr.MoveWindowToMonitor(left: true);
                        return true;
                    }
                    // Plain XButton1 or Shift+XButton1 → snap left
                    {
                        _winMgr.ActivateWindowUnderCursor();
                        _winMgr.SnapWindow(left: true);
                        return true;
                    }

                case MouseButton.XButton2:
                    if (capsLock)
                    {
                        _winMgr.ActivateWindowUnderCursor();
                        _winMgr.SnapWindow(left: false);
                        return true;
                    }
                    if (alt)
                    {
                        _winMgr.ActivateWindowUnderCursor();
                        _winMgr.MoveWindowToMonitor(left: false);
                        return true;
                    }
                    if (ctrl)
                    {
                        _winMgr.MoveWindowToMonitor(left: false);
                        return true;
                    }
                    {
                        _winMgr.ActivateWindowUnderCursor();
                        _winMgr.SnapWindow(left: false);
                        return true;
                    }
            }

            return false;
        }

        // ── Mouse: button up ─────────────────────────────────────────────────

        private bool OnMouseButtonUp(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:     _state.LButtonDown  = false; break;
                case MouseButton.Right:    _state.RButtonDown  = false; break;
                case MouseButton.Middle:   _state.MButtonDown  = false; break;
                case MouseButton.XButton1: _state.XButton1Down = false; break;
                case MouseButton.XButton2: _state.XButton2Down = false; break;
            }

            // End drag on LButton up
            if (e.Button == MouseButton.Left && _state.DragActive)
            {
                _winMgr.EndDrag();
                return false;
            }

            return false;
        }

        // ── Mouse: wheel ─────────────────────────────────────────────────────

        private bool OnMouseWheel(MouseWheelEventArgs e)
        {
            if (_state.Suspended) return false;

            bool up      = e.Direction == WheelDirection.Up;
            bool capsLock = _state.CapsLockHeld;
            bool ctrl    = IsCtrlHeld();
            bool alt     = IsAltHeld();
            bool shift   = IsShiftHeld();

            // CapsLock + WheelUp → maximize; CapsLock + WheelDown → minimize
            if (capsLock)
            {
                IntPtr hwnd = _winMgr.GetWindowUnderCursor();
                if (hwnd != IntPtr.Zero)
                {
                    if (up)   _winMgr.MaximizeWindow(hwnd);
                    else      _winMgr.MinimizeWindow(hwnd);
                }
                return true;
            }

            // Ctrl+Shift+Wheel → window resize
            if (ctrl && shift && !alt)
            {
                if (up) _winMgr.EnlargeWindowUnderCursor();
                else    _winMgr.ShrinkWindowUnderCursor();
                return true;
            }

            return false;
        }

        // ── Mouse: move ──────────────────────────────────────────────────────

        private void OnMouseMove(object? sender, MouseMoveEventArgs e)
        {
            if (_state.DragActive)
                _winMgr.UpdateDrag(e.Position);
        }

        // ── Application actions ──────────────────────────────────────────────

        private static void ReloadApp()
        {
            var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            System.Diagnostics.Process.Start(exePath);
            Application.Exit();
        }

        private void Suspend()
        {
            _state.Suspended = true;
            App.Instance?.SetSuspended(true);
        }

        private void Resume()
        {
            _state.Suspended = false;
            App.Instance?.SetSuspended(false);
        }

        private static void ExitApp() => Application.Exit();

        // ── Helper predicates ────────────────────────────────────────────────

        private bool IsCtrlHeld()
        {
            short s = NativeMethods.GetAsyncKeyState((int)NativeVK.VK_CONTROL);
            return (s & 0x8000) != 0;
        }
        private bool IsAltHeld()
        {
            short s = NativeMethods.GetAsyncKeyState((int)NativeVK.VK_MENU);
            return (s & 0x8000) != 0;
        }
        private bool IsShiftHeld()
        {
            short s = NativeMethods.GetAsyncKeyState((int)NativeVK.VK_SHIFT);
            return (s & 0x8000) != 0;
        }
        private bool IsWinHeld()
        {
            short l = NativeMethods.GetAsyncKeyState((int)NativeVK.VK_LWIN);
            short r = NativeMethods.GetAsyncKeyState((int)NativeVK.VK_RWIN);
            return (l & 0x8000) != 0 || (r & 0x8000) != 0;
        }
        private bool IsCtrlAltShift()
        {
            return IsCtrlHeld() && IsAltHeld() && IsShiftHeld();
        }
        private static bool IsScrollLockOn()
        {
            return (NativeMethods.GetKeyState((int)NativeVK.VK_SCROLL) & 1) != 0;
        }

        private void UpdateModifierDown(uint vk)
        {
            switch ((NativeVK)vk)
            {
                case NativeVK.VK_HOME:     _state.HomeHeld   = true; break;
                case NativeVK.VK_RMENU:    _state.RAltHeld   = true; break;
                case NativeVK.VK_F4:       _state.F4Held     = true; break;
                case NativeVK.VK_LCONTROL: _state.LCtrlHeld  = true; break;
                case NativeVK.VK_LSHIFT:   _state.LShiftHeld = true; break;
                case NativeVK.VK_LMENU:    _state.LAltHeld   = true; break;
            }
        }

        private void UpdateModifierUp(uint vk)
        {
            switch ((NativeVK)vk)
            {
                case NativeVK.VK_HOME:     _state.HomeHeld   = false; break;
                case NativeVK.VK_RMENU:    _state.RAltHeld   = false; break;
                case NativeVK.VK_F4:       _state.F4Held     = false; break;
                case NativeVK.VK_LCONTROL: _state.LCtrlHeld  = false; break;
                case NativeVK.VK_LSHIFT:   _state.LShiftHeld = false; break;
                case NativeVK.VK_LMENU:    _state.LAltHeld   = false; break;
            }
        }

        private static void SendMediaKey(uint vk)
        {
            SendKeys.SendKey(vk, down: true,  extended: true);
            SendKeys.SendKey(vk, down: false, extended: true);
        }

        public void Dispose()
        {
            // Hooks are disposed by App; nothing extra here
        }
    }

    /// <summary>Local VK enum to avoid confusion with the struct-based VirtualKeys in HookStructs.</summary>
    internal enum NativeVK : uint
    {
        VK_LBUTTON   = 0x01,
        VK_RBUTTON   = 0x02,
        VK_MBUTTON   = 0x04,
        VK_BACK      = 0x08,
        VK_TAB       = 0x09,
        VK_RETURN    = 0x0D,
        VK_SHIFT     = 0x10,
        VK_CONTROL   = 0x11,
        VK_MENU      = 0x12,
        VK_CAPITAL   = 0x14,
        VK_ESCAPE    = 0x1B,
        VK_SPACE     = 0x20,
        VK_PRIOR     = 0x21,
        VK_NEXT      = 0x22,
        VK_END       = 0x23,
        VK_HOME      = 0x24,
        VK_LEFT      = 0x25,
        VK_UP        = 0x26,
        VK_RIGHT     = 0x27,
        VK_DOWN      = 0x28,
        VK_INSERT    = 0x2D,
        VK_DELETE    = 0x2E,
        VK_NUMLOCK   = 0x90,
        VK_SCROLL    = 0x91,
        VK_LSHIFT    = 0xA0,
        VK_RSHIFT    = 0xA1,
        VK_LCONTROL  = 0xA2,
        VK_LMENU     = 0xA4,
        VK_RMENU     = 0xA5,
        VK_LWIN      = 0x5B,
        VK_RWIN      = 0x5C,
        VK_NUMPAD5   = 0x65,
        VK_ADD       = 0x6B,
        VK_SUBTRACT  = 0x6D,
        VK_F4        = 0x73,
        VK_R         = 0x52,
        VK_S         = 0x53,
        VK_V         = 0x56,
        VK_Z         = 0x5A,
    }

    /// <summary>VK constants used within SendKeys in Core layer (avoids ambiguity).</summary>
    internal static class VirtualKeys_Native
    {
        public const uint VK_LSHIFT   = 0xA0;
        public const uint VK_LMENU    = 0xA4;
        public const uint VK_LCONTROL = 0xA2;
        public const uint VK_LWIN     = 0x5B;
        public const uint VK_V        = 0x56;
        public const uint VK_BACK     = 0x08;
        public const uint VK_LEFT     = 0x25;
        public const uint VK_RIGHT    = 0x27;
    }

    internal static class VirtualKeys
    {
        public const uint VK_NUMLOCK = 0x90;
    }
}
