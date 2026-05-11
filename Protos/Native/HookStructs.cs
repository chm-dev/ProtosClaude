using System.Runtime.InteropServices;

namespace Protos.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y) { X = x; Y = y; }
        public override string ToString() => $"({X},{Y})";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public uint length;
        public uint flags;
        public uint showCmd;
        public POINT ptMinPosition;
        public POINT ptMaxPosition;
        public RECT rcNormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public INPUT_UNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUT_UNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // Constants for KBDLLHOOKSTRUCT.flags
    public static class KbdFlags
    {
        public const uint LLKHF_EXTENDED = 0x01;
        public const uint LLKHF_INJECTED = 0x10;
        public const uint LLKHF_ALTDOWN  = 0x20;
        public const uint LLKHF_UP       = 0x80;
    }

    // Constants for INPUT
    public static class InputType
    {
        public const uint INPUT_MOUSE    = 0;
        public const uint INPUT_KEYBOARD = 1;
        public const uint INPUT_HARDWARE = 2;
    }

    public static class KeyEventF
    {
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP       = 0x0002;
        public const uint KEYEVENTF_UNICODE     = 0x0004;
        public const uint KEYEVENTF_SCANCODE    = 0x0008;
    }

    public static class VirtualKeys
    {
        public const uint VK_LBUTTON   = 0x01;
        public const uint VK_RBUTTON   = 0x02;
        public const uint VK_CANCEL    = 0x03;
        public const uint VK_MBUTTON   = 0x04;
        public const uint VK_XBUTTON1  = 0x05;
        public const uint VK_XBUTTON2  = 0x06;
        public const uint VK_BACK      = 0x08;
        public const uint VK_TAB       = 0x09;
        public const uint VK_RETURN    = 0x0D;
        public const uint VK_SHIFT     = 0x10;
        public const uint VK_CONTROL   = 0x11;
        public const uint VK_MENU      = 0x12;
        public const uint VK_CAPITAL   = 0x14;
        public const uint VK_ESCAPE    = 0x1B;
        public const uint VK_SPACE     = 0x20;
        public const uint VK_PRIOR     = 0x21; // Page Up
        public const uint VK_NEXT      = 0x22; // Page Down
        public const uint VK_END       = 0x23;
        public const uint VK_HOME      = 0x24;
        public const uint VK_LEFT      = 0x25;
        public const uint VK_UP        = 0x26;
        public const uint VK_RIGHT     = 0x27;
        public const uint VK_DOWN      = 0x28;
        public const uint VK_INSERT    = 0x2D;
        public const uint VK_DELETE    = 0x2E;
        public const uint VK_NUMLOCK   = 0x90;
        public const uint VK_SCROLL    = 0x91;
        public const uint VK_LSHIFT    = 0xA0;
        public const uint VK_RSHIFT    = 0xA1;
        public const uint VK_LCONTROL  = 0xA2;
        public const uint VK_RCONTROL  = 0xA3;
        public const uint VK_LMENU     = 0xA4;
        public const uint VK_RMENU     = 0xA5;
        public const uint VK_LWIN      = 0x5B;
        public const uint VK_RWIN      = 0x5C;
        public const uint VK_NUMPAD0   = 0x60;
        public const uint VK_NUMPAD5   = 0x65;
        public const uint VK_MULTIPLY  = 0x6A;
        public const uint VK_ADD       = 0x6B;
        public const uint VK_SUBTRACT  = 0x6D;
        public const uint VK_F4        = 0x73;
        public const uint VK_F5        = 0x74;
        public const uint VK_R         = 0x52;
        public const uint VK_S         = 0x53;
        public const uint VK_V         = 0x56;
        public const uint VK_Z         = 0x5A;
        public const uint VK_OEM_COMMA = 0xBC;
        public const uint VK_OEM_PERIOD= 0xBE;
        public const uint VK_OEM_1     = 0xBA; // ;:
        public const uint VK_OEM_2     = 0xBF; // /?
    }

    // Window placement show commands
    public static class ShowWindowCommands
    {
        public const int SW_HIDE     = 0;
        public const int SW_NORMAL   = 1;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE  = 9;
        public const int SW_SHOW     = 5;
        public const int SW_MAXIMIZE = 3;
    }
}
