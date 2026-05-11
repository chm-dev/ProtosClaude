using System.Text.Json;
using System.Text.Json.Serialization;

namespace Protos.Settings
{
    public class AppSettings
    {
        public Dictionary<string, HotkeyBinding> Hotkeys    { get; set; } = new();
        public List<HotstringEntry>              Hotstrings { get; set; } = new();
        public int  VolumeStep            { get; set; } = 4;
        public int  ResizeStep            { get; set; } = 80;
        public string SpotifyPath         { get; set; } = DetectSpotifyPath();

        public static string DetectSpotifyPathPublic() => DetectSpotifyPath();

        private static string DetectSpotifyPath()
        {
            string[] candidates =
            [
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                             "Spotify", "Spotify.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                             "Microsoft", "WindowsApps", "Spotify.exe"),
            ];
            foreach (var path in candidates)
                if (File.Exists(path)) return path;
            return "Spotify.exe";
        }

        private static readonly string SettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "Protos", "settings.json");

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented    = true,
            Converters       = { new HotkeyBindingConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOpts);
                    if (settings != null)
                    {
                        var knownKeys = CreateDefaults().Hotkeys.Keys.ToHashSet();
                        foreach (var key in settings.Hotkeys.Keys.Except(knownKeys).ToList())
                            settings.Hotkeys.Remove(key);
                        return settings;
                    }
                }
            }
            catch { /* fall through to defaults */ }

            var defaults = CreateDefaults();
            defaults.Save();
            return defaults;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                string json = JsonSerializer.Serialize(this, JsonOpts);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public static AppSettings CreateDefaults()
        {
            var s = new AppSettings();

            s.Hotkeys = new Dictionary<string, HotkeyBinding>
            {
                ["ToggleAlwaysOnTop"]    = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.LButton),
                ["MinimizeWindow"]       = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.WheelDown),
                ["MaximizeWindow"]       = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.WheelUp),
                ["RestoreWindow"]        = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.MButton),
                ["SnapLeft_CapsLock"]    = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.XButton1),
                ["SnapRight_CapsLock"]   = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.XButton2),
                ["PasteRaw"]             = new(ModifierKeys.Ctrl|ModifierKeys.Alt, SpecialModifier.None, TriggerKey.VK_V),
                ["VolumeMute"]           = new(ModifierKeys.Ctrl|ModifierKeys.Alt, SpecialModifier.None, TriggerKey.MButton),
                ["SnapLeft"]             = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.XButton1),
                ["SnapRight"]            = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.XButton2),
                ["MoveMonitorLeft"]      = new(ModifierKeys.Ctrl,      SpecialModifier.None,      TriggerKey.XButton1),
                ["MoveMonitorRight"]     = new(ModifierKeys.Ctrl,      SpecialModifier.None,      TriggerKey.XButton2),
                ["MoveMonitorLeft_Alt"]  = new(ModifierKeys.Alt,       SpecialModifier.None,      TriggerKey.XButton1),
                ["MoveMonitorRight_Alt"] = new(ModifierKeys.Alt,       SpecialModifier.None,      TriggerKey.XButton2),
                ["MediaPlayPause"]       = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.VK_INSERT),
                ["MediaStop"]            = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.VK_DELETE),
                ["MediaPrev"]            = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.VK_END),
                ["MediaNext"]            = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.VK_DOWN),
                ["VolumeUp"]             = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.VK_ADD),
                ["VolumeDown"]           = new(ModifierKeys.None,      SpecialModifier.None,      TriggerKey.VK_SUBTRACT),
                ["WinVRemap"]            = new(ModifierKeys.Win,       SpecialModifier.None,      TriggerKey.VK_V),
                ["SpotifyToggle"]        = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.VK_S),
                ["ForceKill"]            = new(ModifierKeys.None,      SpecialModifier.F4,        TriggerKey.RButton),
                ["MonitorOff"]           = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.VK_NUMPAD5),
                ["Suspend_Resume"]       = new(ModifierKeys.None,      SpecialModifier.Home,      TriggerKey.VK_PRIOR),
                ["Suspend_Suspend"]      = new(ModifierKeys.None,      SpecialModifier.Home,      TriggerKey.VK_NEXT),
                ["Exit"]                 = new(ModifierKeys.Ctrl|ModifierKeys.Alt|ModifierKeys.Shift, SpecialModifier.None, TriggerKey.VK_END),
                ["Reload"]               = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.VK_R),
                ["ResizeSmall"]          = new(ModifierKeys.None,      SpecialModifier.CapsLock,  TriggerKey.VK_Z),
                ["EnlargeWindow"]        = new(ModifierKeys.Ctrl|ModifierKeys.Shift, SpecialModifier.None, TriggerKey.WheelUp),
                ["ShrinkWindow"]         = new(ModifierKeys.Ctrl|ModifierKeys.Shift, SpecialModifier.None, TriggerKey.WheelDown),
                ["DragWindow"]           = new(ModifierKeys.None,      SpecialModifier.RAlt,      TriggerKey.LButton),
            };

            s.Hotstrings = new List<HotstringEntry>
            {
                new() { Trigger = "@gm",  Replacement = "chmielciu@gmail.com",              RequireEndingChar = true, Enabled = true },

            };

            return s;
        }
    }

    /// <summary>JSON converter for HotkeyBinding using its string representation.</summary>
    public class HotkeyBindingConverter : JsonConverter<HotkeyBinding>
    {
        public override HotkeyBinding? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? s = reader.GetString();
            return s == null ? null : HotkeyBinding.Parse(s);
        }

        public override void Write(Utf8JsonWriter writer, HotkeyBinding value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}