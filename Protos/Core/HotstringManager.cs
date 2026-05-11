using Protos.Settings;

namespace Protos.Core
{
    public class HotstringManager
    {
        private const int BufferSize = 256;
        private readonly char[] _buffer = new char[BufferSize];
        private int _length;

        private List<HotstringEntry> _hotstrings = new();

        private static readonly HashSet<uint> EndingKeys = new()
        {
            HotstringVK.VK_SPACE,
            HotstringVK.VK_RETURN,
            HotstringVK.VK_TAB,
            HotstringVK.VK_OEM_COMMA,    // ,
            HotstringVK.VK_OEM_PERIOD,   // .
            // ! and ? need special handling (VK codes for OEM chars)
            0xDE, // VK_OEM_7 = '  (apostrophe / quote on US keyboard)
        };

        private static readonly HashSet<uint> ExclamationMarkVKs = new()
        {
            0x31, // '1' key — shift+1 = !
        };

        private static readonly HashSet<uint> ClearKeys = new()
        {
            HotstringVK.VK_ESCAPE,
            HotstringVK.VK_HOME,
            HotstringVK.VK_END,
            HotstringVK.VK_LEFT,
            HotstringVK.VK_RIGHT,
            HotstringVK.VK_UP,
            HotstringVK.VK_DOWN,
            HotstringVK.VK_PRIOR,
            HotstringVK.VK_NEXT,
        };

        public void SetHotstrings(IEnumerable<HotstringEntry> hotstrings)
        {
            _hotstrings = hotstrings.Where(h => h.Enabled).ToList();
        }

        /// <summary>
        /// Called from keyboard hook on key-down.
        /// Returns true if the key was consumed by a hotstring expansion.
        /// </summary>
        public bool OnKeyDown(uint vkCode, bool shiftHeld)
        {
            // Clear buffer on navigation / escape
            if (ClearKeys.Contains(vkCode))
            {
                Clear();
                return false;
            }

            // Backspace: remove last char
            if (vkCode == HotstringVK.VK_BACK)
            {
                if (_length > 0) _length--;
                return false;
            }

            char? ch = VkToChar(vkCode, shiftHeld);

            bool isEndingChar = IsEndingChar(vkCode, shiftHeld);

            if (isEndingChar)
            {
                // Check for matching hotstring BEFORE appending the ending char
                bool matched = TryExpand(isEndingChar: true);
                if (!matched && ch.HasValue)
                    AppendChar(ch.Value);
                return matched; // suppress ending char if we expanded
            }

            if (ch.HasValue)
                AppendChar(ch.Value);

            return false;
        }

        public void ClearOnMouseClick()
        {
            Clear();
        }

        private void Clear()
        {
            _length = 0;
        }

        private void AppendChar(char c)
        {
            if (_length < BufferSize)
            {
                _buffer[_length++] = c;
            }
            else
            {
                // Shift buffer left by one
                Array.Copy(_buffer, 1, _buffer, 0, BufferSize - 1);
                _buffer[BufferSize - 1] = c;
                // length stays at BufferSize
            }
        }

        private bool TryExpand(bool isEndingChar)
        {
            string typed = new string(_buffer, 0, _length);

            foreach (var hs in _hotstrings)
            {
                if (!hs.Enabled) continue;
                if (hs.RequireEndingChar && !isEndingChar) continue;

                if (typed.EndsWith(hs.Trigger, StringComparison.Ordinal))
                {
                    // Send backspaces: trigger.Length + 1 (for ending char if required)
                    int backspaces = hs.Trigger.Length + (hs.RequireEndingChar ? 1 : 0);
                    Core.SendKeys.SendBackspaces(backspaces);

                    // Send replacement
                    Core.SendKeys.SendUnicodeString(hs.Replacement);

                    // Clear matched portion from buffer
                    int removeFrom = _length - hs.Trigger.Length;
                    _length = Math.Max(0, removeFrom);

                    return true;
                }
            }
            return false;
        }

        private static bool IsEndingChar(uint vkCode, bool shiftHeld)
        {
            if (vkCode == HotstringVK.VK_SPACE)  return true;
            if (vkCode == HotstringVK.VK_RETURN)  return true;
            if (vkCode == HotstringVK.VK_TAB)     return true;
            if (vkCode == HotstringVK.VK_OEM_COMMA)  return true;
            if (vkCode == HotstringVK.VK_OEM_PERIOD) return true;

            // ! = Shift+1 (0x31) on US keyboard
            if (vkCode == 0x31 && shiftHeld) return true;
            // ? = Shift+/ (VK_OEM_2 = 0xBF) on US keyboard
            if (vkCode == HotstringVK.VK_OEM_2 && shiftHeld) return true;

            return false;
        }

        private static char? VkToChar(uint vkCode, bool shiftHeld)
        {
            // Letters A-Z
            if (vkCode >= 0x41 && vkCode <= 0x5A)
                return (char)(shiftHeld ? vkCode : vkCode + 32);

            // Digits 0-9
            if (vkCode >= 0x30 && vkCode <= 0x39)
            {
                if (!shiftHeld) return (char)vkCode;
                // shifted digits
                return (char)vkCode switch
                {
                    '0' => ')',  '1' => '!', '2' => '@', '3' => '#', '4' => '$',
                    '5' => '%',  '6' => '^', '7' => '&', '8' => '*', '9' => '(',
                    _ => null
                };
            }

            // Numpad digits
            if (vkCode >= 0x60 && vkCode <= 0x69)
                return (char)('0' + (vkCode - 0x60));

            // Common OEM keys
            return vkCode switch
            {
                HotstringVK.VK_OEM_COMMA   => shiftHeld ? '<' : ',',
                HotstringVK.VK_OEM_PERIOD  => shiftHeld ? '>' : '.',
                HotstringVK.VK_OEM_2       => shiftHeld ? '?' : '/',
                0xBA                        => shiftHeld ? ':' : ';',
                0xBB                        => shiftHeld ? '+' : '=',
                0xBD                        => shiftHeld ? '_' : '-',
                0xDB                        => shiftHeld ? '{' : '[',
                0xDC                        => shiftHeld ? '|' : '\\',
                0xDD                        => shiftHeld ? '}' : ']',
                0xDE                        => shiftHeld ? '"' : '\'',
                0xC0                        => shiftHeld ? '~' : '`',
                HotstringVK.VK_SPACE        => ' ',
                _                           => null,
            };
        }
    }

    // Local alias so we don't need to fully qualify everywhere
    internal static class HotstringVK
    {
        public const uint VK_BACK       = 0x08;
        public const uint VK_TAB        = 0x09;
        public const uint VK_RETURN     = 0x0D;
        public const uint VK_ESCAPE     = 0x1B;
        public const uint VK_SPACE      = 0x20;
        public const uint VK_PRIOR      = 0x21;
        public const uint VK_NEXT       = 0x22;
        public const uint VK_END        = 0x23;
        public const uint VK_HOME       = 0x24;
        public const uint VK_LEFT       = 0x25;
        public const uint VK_UP         = 0x26;
        public const uint VK_RIGHT      = 0x27;
        public const uint VK_DOWN       = 0x28;
        public const uint VK_OEM_COMMA  = 0xBC;
        public const uint VK_OEM_PERIOD = 0xBE;
        public const uint VK_OEM_2      = 0xBF;
    }
}
