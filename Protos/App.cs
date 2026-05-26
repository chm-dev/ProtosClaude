using Protos.Core;
using Protos.Hooks;
using Protos.Native;
using Protos.Settings;
using Protos.UI;

namespace Protos
{
    /// <summary>
    /// Application lifecycle: tray icon, hooks, manager wiring.
    /// </summary>
    public class App : IDisposable
    {
        public static App? Instance { get; private set; }

        private readonly NotifyIcon      _trayIcon;
        private readonly KeyboardHook    _kbdHook;
        private readonly MouseHook       _mouseHook;
        private readonly AppState        _appState;
        private readonly WindowManager   _winMgr;
        private readonly VolumeManager   _volMgr;
        private readonly MonitorManager  _monMgr;
        private readonly SpotifyManager  _spotifyMgr;
        private readonly HotstringManager _hotstringMgr;
        private readonly HotkeyDispatcher _dispatcher;
        private readonly VolumeOverlay   _volOverlay;

        private AppSettings              _settings;
        private ToolStripMenuItem?       _suspendItem;
        private ToolStripMenuItem?       _caffeineItem;
        private readonly CaffeineManager _caffeineMgr;

        public App()
        {
            Instance = this;

            // ── Load settings ─────────────────────────────────────────────────
            _settings = AppSettings.Load();

            // ── Build state and managers ──────────────────────────────────────
            _caffeineMgr   = new CaffeineManager();
            _appState      = new AppState();
            _winMgr        = new WindowManager(_appState)    { ResizeStep = _settings.ResizeStep };
            _volMgr        = new VolumeManager()             { VolumeStep = _settings.VolumeStep };
            _monMgr        = new MonitorManager(_appState);
            _spotifyMgr    = new SpotifyManager { SpotifyPath = _settings.SpotifyPath };
            _hotstringMgr  = new HotstringManager();
            _hotstringMgr.SetHotstrings(_settings.Hotstrings);

            // ── Build hooks ───────────────────────────────────────────────────
            _kbdHook   = new KeyboardHook();
            _mouseHook = new MouseHook();

            // ── Volume overlay ────────────────────────────────────────────────
            _volOverlay = new VolumeOverlay();
            _volMgr.SetOverlay(_volOverlay);

            // ── Dispatcher ────────────────────────────────────────────────────
            _dispatcher = new HotkeyDispatcher(
                _appState, _kbdHook, _mouseHook,
                _winMgr, _volMgr, _monMgr, _spotifyMgr,
                _hotstringMgr, _settings);

            // ── Tray icon ─────────────────────────────────────────────────────
            _trayIcon = BuildTrayIcon();

            // ── Install hooks ─────────────────────────────────────────────────
            _kbdHook.Install();
            _mouseHook.Install();

            // ── Init: force CapsLock and NumLock off ─────────────────────────
            ForceKeyOff(0x14); // VK_CAPITAL
            ForceKeyOff(0x90); // VK_NUMLOCK

        }

        // ── Tray icon ─────────────────────────────────────────────────────────

        private NotifyIcon BuildTrayIcon()
        {
            var icon = LoadTrayIcon();
            var menu = new ContextMenuStrip();

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (_, _) => OpenSettings();

            var reloadItem = new ToolStripMenuItem("Reload");
            reloadItem.Click += (_, _) => ReloadProcess();

            menu.Items.Add(settingsItem);
            menu.Items.Add(reloadItem);
            menu.Items.Add(new ToolStripSeparator());

            _suspendItem = new ToolStripMenuItem("Suspend");
            _suspendItem.CheckOnClick = false;
            _suspendItem.Click += (_, _) => ToggleSuspend();
            menu.Items.Add(_suspendItem);

            _caffeineItem = new ToolStripMenuItem("Caffeine");
            _caffeineItem.CheckOnClick = false;
            _caffeineItem.Click += (_, _) => ToggleCaffeine();
            menu.Items.Add(_caffeineItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => Application.Exit();
            menu.Items.Add(exitItem);

            var tray = new NotifyIcon
            {
                Icon             = icon,
                Visible          = true,
                Text             = "Protos",
                ContextMenuStrip = menu,
            };
            tray.DoubleClick += (_, _) => OpenSettings();
            return tray;
        }

        private static Icon LoadTrayIcon()
        {
            string icoPath = Path.Combine(AppContext.BaseDirectory, "protos.ico");
            if (File.Exists(icoPath))
            {
                try { return new Icon(icoPath); }
                catch { }
            }
            return SystemIcons.Application;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void SetSuspended(bool suspended)
        {
            _appState.Suspended = suspended;
            if (_suspendItem != null)
                _suspendItem.Checked = suspended;
        }

        public void ReloadSettings(AppSettings settings)
        {
            _settings = settings;
            _dispatcher.UpdateSettings(settings);
        }

        // ── Private actions ───────────────────────────────────────────────────

        private void OpenSettings()
        {
            var form = new SettingsForm(_settings);
            form.ShowDialog();
        }

        private void ToggleSuspend()
        {
            bool nowSuspended = !_appState.Suspended;
            SetSuspended(nowSuspended);
        }

        private void ToggleCaffeine()
        {
            _caffeineMgr.SetActive(!_caffeineMgr.IsActive);
            if (_caffeineItem != null)
                _caffeineItem.Checked = _caffeineMgr.IsActive;
        }

        private static void ReloadProcess()
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
                exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            System.Diagnostics.Process.Start(exePath);
            Application.Exit();
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            _caffeineMgr.Dispose();
            _dispatcher.Dispose();
            _kbdHook.Dispose();
            _mouseHook.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _volOverlay.Dispose();
            Instance = null;
            GC.SuppressFinalize(this);
        }

        // ── Static helper ─────────────────────────────────────────────────────

        private static void ForceKeyOff(int vk)
        {
            if ((NativeMethods.GetKeyState(vk) & 1) != 0)
            {
                NativeMethods.keybd_event((byte)vk, 0, 0, UIntPtr.Zero);
                NativeMethods.keybd_event((byte)vk, 0, 0x0002 /* KEYEVENTF_KEYUP */, UIntPtr.Zero);
            }
        }
    }
}
