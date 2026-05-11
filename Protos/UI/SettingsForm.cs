using Protos.Settings;

namespace Protos.UI
{
    public class SettingsForm : Form
    {
        private readonly AppSettings          _settings;
        private readonly DataGridView         _hotkeysGrid;
        private readonly DataGridView         _hotstringsGrid;
        private readonly TabControl           _tabs;
        private readonly Button               _btnSave;
        private readonly Button               _btnCancel;
        private readonly Button               _btnOpenSounds;
        private readonly TextBox              _spotifyPathBox;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;

            Text           = "Protos Settings";
            Size           = new Size(800, 600);
            MinimumSize    = new Size(600, 450);
            StartPosition  = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;

            // ── Tab control ───────────────────────────────────────────────────
            _tabs = new TabControl { Dock = DockStyle.Fill };

            var hotkeysPage    = new TabPage("Hotkeys");
            var hotstringsPage = new TabPage("Hotstrings");

            // ── Hotkeys grid ──────────────────────────────────────────────────
            _hotkeysGrid = new DataGridView
            {
                Dock              = DockStyle.Fill,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                ReadOnly          = false,
                SelectionMode     = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
            };

            _hotkeysGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Action",
                Name       = "Action",
                ReadOnly   = true,
                FillWeight = 40,
            });
            _hotkeysGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Binding",
                Name       = "Binding",
                ReadOnly   = true,
                FillWeight = 40,
            });

            var changeBtn = new DataGridViewButtonColumn
            {
                HeaderText = "Change",
                Name       = "Change",
                Text       = "Change",
                UseColumnTextForButtonValue = true,
                FillWeight = 20,
            };
            _hotkeysGrid.Columns.Add(changeBtn);

            _hotkeysGrid.CellContentClick += HotkeysGrid_CellContentClick;

            PopulateHotkeys();
            hotkeysPage.Controls.Add(_hotkeysGrid);

            // ── Hotstrings grid ───────────────────────────────────────────────
            _hotstringsGrid = new DataGridView
            {
                Dock              = DockStyle.Fill,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode     = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
            };

            _hotstringsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Trigger",
                Name       = "Trigger",
                FillWeight = 20,
            });
            _hotstringsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Replacement",
                Name       = "Replacement",
                FillWeight = 45,
            });
            _hotstringsGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = "Require End Char",
                Name       = "RequireEndChar",
                FillWeight = 15,
            });
            _hotstringsGrid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                HeaderText = "Enabled",
                Name       = "Enabled",
                FillWeight = 10,
            });

            var deleteCol = new DataGridViewButtonColumn
            {
                HeaderText = "Delete",
                Name       = "Delete",
                Text       = "Delete",
                UseColumnTextForButtonValue = true,
                FillWeight = 10,
            };
            _hotstringsGrid.Columns.Add(deleteCol);
            _hotstringsGrid.CellContentClick += HotstringsGrid_CellContentClick;

            PopulateHotstrings();

            // Add button below hotstrings grid
            var addBtn = new Button { Text = "Add Hotstring", Dock = DockStyle.Bottom };
            addBtn.Click += AddHotstring_Click;

            var hotstringsPanel = new Panel { Dock = DockStyle.Fill };
            hotstringsPanel.Controls.Add(_hotstringsGrid);
            hotstringsPanel.Controls.Add(addBtn);
            hotstringsPage.Controls.Add(hotstringsPanel);

            _tabs.TabPages.Add(hotkeysPage);
            _tabs.TabPages.Add(hotstringsPage);

            // ── Spotify tab ───────────────────────────────────────────────────
            var spotifyPage = new TabPage("Spotify");

            // TableLayoutPanel guarantees correct layout regardless of DPI/scaling
            var spotifyLayout = new TableLayoutPanel
            {
                Dock        = DockStyle.Top,
                AutoSize    = true,
                ColumnCount = 2,
                RowCount    = 3,
                Padding     = new Padding(10, 12, 10, 0),
            };
            spotifyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            spotifyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            spotifyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // label
            spotifyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // textbox + browse
            spotifyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // detect button

            var spotifyPathLabel = new Label
            {
                Text     = "Spotify executable path:",
                AutoSize = true,
                Margin   = new Padding(0, 0, 0, 4),
            };
            spotifyLayout.Controls.Add(spotifyPathLabel, 0, 0);
            spotifyLayout.SetColumnSpan(spotifyPathLabel, 2);

            _spotifyPathBox = new TextBox
            {
                Text   = _settings.SpotifyPath,
                Dock   = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0),
            };
            spotifyLayout.Controls.Add(_spotifyPathBox, 0, 1);

            var btnBrowse = new Button
            {
                Text     = "Browse...",
                AutoSize = true,
                MinimumSize = new Size(90, 0),
            };
            btnBrowse.Click += (_, _) =>
            {
                string? dir = null;
                try { dir = Path.GetDirectoryName(_spotifyPathBox.Text); } catch { }
                using var dlg = new OpenFileDialog
                {
                    Title            = "Select Spotify.exe",
                    Filter           = "Spotify|Spotify.exe|Executables|*.exe|All files|*.*",
                    InitialDirectory = dir ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    _spotifyPathBox.Text = dlg.FileName;
            };
            spotifyLayout.Controls.Add(btnBrowse, 1, 1);

            var btnDetect = new Button
            {
                Text     = "Detect automatically",
                AutoSize = true,
                Margin   = new Padding(0, 8, 0, 0),
            };
            btnDetect.Click += (_, _) =>
            {
                string detected = Settings.AppSettings.DetectSpotifyPathPublic();
                _spotifyPathBox.Text = detected;
                bool found = File.Exists(detected);
                MessageBox.Show(
                    found ? $"Found:\n{detected}"
                          : "Spotify not found in default locations.\nPlease use Browse to locate Spotify.exe.",
                    "Detect Spotify",
                    MessageBoxButtons.OK,
                    found ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            };
            spotifyLayout.Controls.Add(btnDetect, 0, 2);

            spotifyPage.Controls.Add(spotifyLayout);
            _tabs.TabPages.Add(spotifyPage);

            // ── Bottom buttons ────────────────────────────────────────────────
            var bottomPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height        = 40,
                Padding       = new Padding(4),
            };

            _btnCancel = new Button { Text = "Cancel", Width = 80, Height = 28 };
            _btnCancel.Click += (_, _) => Close();

            _btnSave = new Button { Text = "Save", Width = 80, Height = 28 };
            _btnSave.Click += BtnSave_Click;

            _btnOpenSounds = new Button { Text = "Open Sound Folder", Width = 140, Height = 28 };
            _btnOpenSounds.Click += (_, _) =>
            {
                string soundDir = Path.Combine(AppContext.BaseDirectory, "sounds");
                Directory.CreateDirectory(soundDir);
                System.Diagnostics.Process.Start("explorer.exe", soundDir);
            };

            bottomPanel.Controls.Add(_btnCancel);
            bottomPanel.Controls.Add(_btnSave);
            bottomPanel.Controls.Add(_btnOpenSounds);

            Controls.Add(_tabs);
            Controls.Add(bottomPanel);
        }

        // ── Hotkeys ───────────────────────────────────────────────────────────

        private void PopulateHotkeys()
        {
            _hotkeysGrid.Rows.Clear();
            foreach (var (action, binding) in _settings.Hotkeys)
            {
                int idx = _hotkeysGrid.Rows.Add(action, binding.ToString());
                _hotkeysGrid.Rows[idx].Tag = action;
            }
        }

        private void HotkeysGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_hotkeysGrid.Columns[e.ColumnIndex].Name != "Change") return;

            string action = _hotkeysGrid.Rows[e.RowIndex].Tag as string ?? string.Empty;
            using var dlg = new HotkeyCapture();
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var newBinding = dlg.CapturedBinding;
            if (newBinding == null) return;

            _settings.Hotkeys[action] = newBinding;
            _hotkeysGrid.Rows[e.RowIndex].Cells["Binding"].Value = newBinding.ToString();
        }

        // ── Hotstrings ────────────────────────────────────────────────────────

        private void PopulateHotstrings()
        {
            _hotstringsGrid.Rows.Clear();
            foreach (var hs in _settings.Hotstrings)
            {
                _hotstringsGrid.Rows.Add(hs.Trigger, hs.Replacement, hs.RequireEndingChar, hs.Enabled);
            }
        }

        private void HotstringsGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_hotstringsGrid.Columns[e.ColumnIndex].Name != "Delete") return;

            _hotstringsGrid.Rows.RemoveAt(e.RowIndex);
        }

        private void AddHotstring_Click(object? sender, EventArgs e)
        {
            _hotstringsGrid.Rows.Add("", "", true, true);
        }

        // ── Save ──────────────────────────────────────────────────────────────

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            // Collect hotstrings from grid
            var newHotstrings = new List<HotstringEntry>();
            foreach (DataGridViewRow row in _hotstringsGrid.Rows)
            {
                if (row.IsNewRow) continue;
                string trigger     = row.Cells["Trigger"].Value?.ToString()     ?? "";
                string replacement = row.Cells["Replacement"].Value?.ToString() ?? "";
                bool   requireEnd  = row.Cells["RequireEndChar"].Value is bool b && b;
                bool   enabled     = row.Cells["Enabled"].Value is bool en && en;

                if (!string.IsNullOrWhiteSpace(trigger))
                    newHotstrings.Add(new HotstringEntry
                    {
                        Trigger          = trigger,
                        Replacement      = replacement,
                        RequireEndingChar = requireEnd,
                        Enabled          = enabled,
                    });
            }
            _settings.Hotstrings  = newHotstrings;
            _settings.SpotifyPath = _spotifyPathBox.Text.Trim();
            _settings.Save();

            App.Instance?.ReloadSettings(_settings);
            Close();
        }
    }

    // ── HotkeyCapture dialog ──────────────────────────────────────────────────

    public class HotkeyCapture : Form
    {
        public HotkeyBinding? CapturedBinding { get; private set; }

        private readonly Label  _label;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        private uint _capturedVk;
        private bool _ctrl, _alt, _shift, _win;

        public HotkeyCapture()
        {
            Text           = "Press a key combination";
            Size           = new Size(380, 160);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition  = FormStartPosition.CenterParent;
            MaximizeBox    = false;
            MinimizeBox    = false;

            _label = new Label
            {
                Text     = "Press any key combination...",
                Dock     = DockStyle.Top,
                Height   = 60,
                TextAlign = ContentAlignment.MiddleCenter,
                Font     = new Font(Font.FontFamily, 11),
            };

            var bottomPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height        = 40,
                Padding       = new Padding(4),
            };

            _btnCancel = new Button { Text = "Cancel", Width = 80, Height = 28, DialogResult = DialogResult.Cancel };
            _btnOk     = new Button { Text = "OK",     Width = 80, Height = 28, DialogResult = DialogResult.OK, Enabled = false };

            bottomPanel.Controls.Add(_btnCancel);
            bottomPanel.Controls.Add(_btnOk);

            Controls.Add(_label);
            Controls.Add(bottomPanel);

            KeyPreview = true;
            KeyDown   += HotkeyCapture_KeyDown;
            KeyUp     += HotkeyCapture_KeyUp;
        }

        private void HotkeyCapture_KeyDown(object? sender, KeyEventArgs e)
        {
            _ctrl  = e.Control;
            _alt   = e.Alt;
            _shift = e.Shift;
            _win   = (Native.NativeMethods.GetAsyncKeyState(0x5B) & 0x8000) != 0 ||
                     (Native.NativeMethods.GetAsyncKeyState(0x5C) & 0x8000) != 0;

            // Ignore pure modifier presses
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey ||
                e.KeyCode == Keys.Menu       || e.KeyCode == Keys.LWin    ||
                e.KeyCode == Keys.RWin)
                return;

            _capturedVk = (uint)e.KeyCode;

            var mods = Settings.ModifierKeys.None;
            if (_ctrl)  mods |= Settings.ModifierKeys.Ctrl;
            if (_alt)   mods |= Settings.ModifierKeys.Alt;
            if (_shift) mods |= Settings.ModifierKeys.Shift;
            if (_win)   mods |= Settings.ModifierKeys.Win;

            Settings.TriggerKey trigger = VkToTrigger(_capturedVk);

            if (trigger != Settings.TriggerKey.None)
            {
                CapturedBinding = new HotkeyBinding(mods, Settings.SpecialModifier.None, trigger);
                _label.Text     = CapturedBinding.ToString();
                _btnOk.Enabled  = true;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void HotkeyCapture_KeyUp(object? sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private static Settings.TriggerKey VkToTrigger(uint vk)
        {
            return vk switch
            {
                0x52 => Settings.TriggerKey.VK_R,
                0x53 => Settings.TriggerKey.VK_S,
                0x56 => Settings.TriggerKey.VK_V,
                0x5A => Settings.TriggerKey.VK_Z,
                0x5B => Settings.TriggerKey.VK_LWIN,
                0x65 => Settings.TriggerKey.VK_NUMPAD5,
                0x6B => Settings.TriggerKey.VK_ADD,
                0x6D => Settings.TriggerKey.VK_SUBTRACT,
                0x73 => Settings.TriggerKey.VK_F4,
                0x21 => Settings.TriggerKey.VK_PRIOR,
                0x22 => Settings.TriggerKey.VK_NEXT,
                0x23 => Settings.TriggerKey.VK_END,
                0x28 => Settings.TriggerKey.VK_DOWN,
                0x2D => Settings.TriggerKey.VK_INSERT,
                0x2E => Settings.TriggerKey.VK_DELETE,
                0x1B => Settings.TriggerKey.VK_ESCAPE,
                _    => Settings.TriggerKey.None,
            };
        }
    }
}
