using System.Text;

namespace Protos.Settings
{
    [Flags]
    public enum ModifierKeys
    {
        None      = 0,
        Ctrl      = 1,
        Alt       = 2,
        Shift     = 4,
        Win       = 8,
        LCtrlOnly = 16,  // LCtrl without intercepting RCtrl
    }

    public enum SpecialModifier
    {
        None,
        CapsLock,
        Home,
        RAlt,
        F4,
        XButton1,   // XButton1 used as modifier (held)
    }

    public enum TriggerKey
    {
        None = 0,
        LButton,
        RButton,
        MButton,
        XButton1,
        XButton2,
        WheelUp,
        WheelDown,
        VK_R       = 0x52,
        VK_S       = 0x53,
        VK_V       = 0x56,
        VK_Z       = 0x5A,
        VK_LWIN    = 0x5B,
        VK_NUMPAD5 = 0x65,
        VK_ADD     = 0x6B,
        VK_SUBTRACT= 0x6D,
        VK_F4      = 0x73,
        VK_PRIOR   = 0x21,  // PgUp
        VK_NEXT    = 0x22,  // PgDn
        VK_END     = 0x23,
        VK_DOWN    = 0x28,
        VK_INSERT  = 0x2D,
        VK_DELETE  = 0x2E,
        VK_ESCAPE  = 0x1B,
    }

    public record HotkeyBinding(
        ModifierKeys    Modifiers,
        SpecialModifier Special,
        TriggerKey      Key)
    {
        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Special != SpecialModifier.None)
            {
                sb.Append(Special);
                sb.Append('+');
            }

            if ((Modifiers & ModifierKeys.Ctrl) != 0)   { sb.Append("Ctrl+"); }
            if ((Modifiers & ModifierKeys.Alt)  != 0)   { sb.Append("Alt+"); }
            if ((Modifiers & ModifierKeys.Shift) != 0)  { sb.Append("Shift+"); }
            if ((Modifiers & ModifierKeys.Win)  != 0)   { sb.Append("Win+"); }
            if ((Modifiers & ModifierKeys.LCtrlOnly) != 0) { sb.Append("LCtrl+"); }

            sb.Append(Key switch
            {
                TriggerKey.LButton    => "LButton",
                TriggerKey.RButton    => "RButton",
                TriggerKey.MButton    => "MButton",
                TriggerKey.XButton1   => "XButton1",
                TriggerKey.XButton2   => "XButton2",
                TriggerKey.WheelUp    => "WheelUp",
                TriggerKey.WheelDown  => "WheelDown",
                TriggerKey.VK_R       => "R",
                TriggerKey.VK_S       => "S",
                TriggerKey.VK_V       => "V",
                TriggerKey.VK_Z       => "Z",
                TriggerKey.VK_LWIN    => "LWin",
                TriggerKey.VK_NUMPAD5 => "Numpad5",
                TriggerKey.VK_ADD     => "NumpadAdd",
                TriggerKey.VK_SUBTRACT=> "NumpadSub",
                TriggerKey.VK_F4      => "F4",
                TriggerKey.VK_PRIOR   => "PgUp",
                TriggerKey.VK_NEXT    => "PgDn",
                TriggerKey.VK_END     => "End",
                TriggerKey.VK_DOWN    => "Down",
                TriggerKey.VK_INSERT  => "Insert",
                TriggerKey.VK_DELETE  => "Delete",
                TriggerKey.VK_ESCAPE  => "Escape",
                _                     => Key.ToString(),
            });

            return sb.ToString();
        }

        public static HotkeyBinding? Parse(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            var parts = s.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0) return null;

            var modifiers = ModifierKeys.None;
            var special   = SpecialModifier.None;
            TriggerKey key = TriggerKey.None;

            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].ToLowerInvariant();
                bool isLast = (i == parts.Length - 1);

                if (!isLast)
                {
                    switch (p)
                    {
                        case "ctrl":     modifiers |= ModifierKeys.Ctrl;      break;
                        case "alt":      modifiers |= ModifierKeys.Alt;       break;
                        case "shift":    modifiers |= ModifierKeys.Shift;     break;
                        case "win":      modifiers |= ModifierKeys.Win;       break;
                        case "lctrl":    modifiers |= ModifierKeys.LCtrlOnly; break;
                        case "capslock": special    = SpecialModifier.CapsLock; break;
                        case "home":     special    = SpecialModifier.Home;    break;
                        case "ralt":     special    = SpecialModifier.RAlt;    break;
                        case "f4":       special    = SpecialModifier.F4;      break;
                        case "xbutton1": special    = SpecialModifier.XButton1; break;
                    }
                }
                else
                {
                    key = p switch
                    {
                        "lbutton"   => TriggerKey.LButton,
                        "rbutton"   => TriggerKey.RButton,
                        "mbutton"   => TriggerKey.MButton,
                        "xbutton1"  => TriggerKey.XButton1,
                        "xbutton2"  => TriggerKey.XButton2,
                        "wheelup"   => TriggerKey.WheelUp,
                        "wheeldown" => TriggerKey.WheelDown,
                        "r"         => TriggerKey.VK_R,
                        "s"         => TriggerKey.VK_S,
                        "v"         => TriggerKey.VK_V,
                        "z"         => TriggerKey.VK_Z,
                        "lwin"      => TriggerKey.VK_LWIN,
                        "numpad5"   => TriggerKey.VK_NUMPAD5,
                        "numpadadd" => TriggerKey.VK_ADD,
                        "numpadsub" => TriggerKey.VK_SUBTRACT,
                        "f4"        => TriggerKey.VK_F4,
                        "pgup"      => TriggerKey.VK_PRIOR,
                        "pgdn"      => TriggerKey.VK_NEXT,
                        "end"       => TriggerKey.VK_END,
                        "down"      => TriggerKey.VK_DOWN,
                        "insert"    => TriggerKey.VK_INSERT,
                        "delete"    => TriggerKey.VK_DELETE,
                        "escape"    => TriggerKey.VK_ESCAPE,
                        _           => TriggerKey.None,
                    };
                }
            }

            if (key == TriggerKey.None) return null;
            return new HotkeyBinding(modifiers, special, key);
        }
    }
}
