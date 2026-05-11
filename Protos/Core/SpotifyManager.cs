using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Protos.Native;

namespace Protos.Core
{
    public class SpotifyManager
    {
        private const string SpotifyProcessName = "Spotify";

        public string SpotifyPath { get; set; } = string.Empty;

        // Exact regex from AHK: (?:Spotify Premium)|(?:[^\s]+\s-\s[^\s]+).*
        // Partial (unanchored) match, same as AHK SetTitleMatchMode RegEx.
        private static readonly Regex TitleRegex =
            new(@"(?:Spotify Premium)|(?:[^\s]+\s-\s[^\s]+)", RegexOptions.Compiled);

        public void Toggle()
        {
            // AHK: Process, Exist, Spotify.exe
            var procs = Process.GetProcessesByName(SpotifyProcessName);
            if (procs.Length == 0)
            {
                // AHK: Run, Spotify.exe
                Launch();
                return;
            }

            // AHK: sWinTitle := "(?:Spotify Premium)|(?:[^\s]+\s-\s[^\s]+).* ahk_exe Spotify.exe"
            // EnumWindows finds ALL top-level windows including hidden ones,
            // matching AHK DetectHiddenWindows, On + WinExist behaviour.
            IntPtr hwnd = FindWindow(procs);

            if (hwnd == IntPtr.Zero)
                return; // no matching window — AHK would also silently do nothing

            // AHK: if (isWindowVisible(sWinTitle))
            bool visible = NativeMethods.IsWindowVisible(hwnd);

            if (visible)
            {
                // AHK: If (WinActive(sWinTitle)) → WinHide
                //      Else                      → WinRestore + WinActivate
                IntPtr foreground = NativeMethods.GetForegroundWindow();
                if (foreground == hwnd)
                {
                    NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_HIDE);
                }
                else
                {
                    NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_RESTORE);
                    Activate(hwnd);
                }
            }
            else
            {
                // AHK: WinShow + WinActivate
                NativeMethods.ShowWindow(hwnd, ShowWindowCommands.SW_SHOW);
                Activate(hwnd);
            }
        }

        /// <summary>
        /// If the foreground window belongs to Spotify, hide it. Returns true if Esc was consumed.
        /// </summary>
        public bool TryHideOnEsc()
        {
            IntPtr foreground = NativeMethods.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;

            NativeMethods.GetWindowThreadProcessId(foreground, out uint pid);
            if (pid == 0) return false;

            try
            {
                var proc = Process.GetProcessById((int)pid);
                if (proc.ProcessName.Equals(SpotifyProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    NativeMethods.ShowWindow(foreground, ShowWindowCommands.SW_HIDE);
                    return true;
                }
            }
            catch { }

            return false;
        }

        // ── private helpers ───────────────────────────────────────────────────

        private void Launch()
        {
            try
            {
                string path = SpotifyPath;

                // Fall back to auto-detection when stored path is missing or invalid
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    path = Settings.AppSettings.DetectSpotifyPathPublic();

                // UseShellExecute = true is required in .NET 8; without it
                // Process.Start only searches PATH and Spotify is not in PATH.
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { }
        }

        /// <summary>
        /// Replicates AHK WinExist with DetectHiddenWindows On + ahk_exe Spotify.exe + title regex.
        /// EnumWindows enumerates ALL top-level windows, including hidden ones.
        /// Returns the first HWND whose process is Spotify.exe and whose title matches TitleRegex.
        /// </summary>
        private static IntPtr FindWindow(Process[] procs)
        {
            var spotifyPids = new HashSet<uint>();
            foreach (var p in procs)
            {
                try { spotifyPids.Add((uint)p.Id); }
                catch { }
            }

            IntPtr found = IntPtr.Zero;

            EnumWindows((hwnd, _) =>
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                if (!spotifyPids.Contains(pid)) return true;

                var sb = new StringBuilder(512);
                if (GetWindowText(hwnd, sb, sb.Capacity) == 0) return true;

                if (TitleRegex.IsMatch(sb.ToString()))
                {
                    found = hwnd;
                    return false; // stop — first match wins, same as AHK WinExist
                }

                return true;
            }, IntPtr.Zero);

            return found;
        }

        /// <summary>
        /// Replicates AHK WinActivate: attach to foreground thread first so
        /// SetForegroundWindow is not blocked by the foreground lock.
        /// </summary>
        private static void Activate(IntPtr hwnd)
        {
            IntPtr fg  = NativeMethods.GetForegroundWindow();
            uint fgTid = NativeMethods.GetWindowThreadProcessId(fg, out _);
            uint myTid = GetCurrentThreadId();

            if (fgTid != myTid)
                AttachThreadInput(myTid, fgTid, true);

            NativeMethods.SetForegroundWindow(hwnd);

            if (fgTid != myTid)
                AttachThreadInput(myTid, fgTid, false);
        }

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    }
}
