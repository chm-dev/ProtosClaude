using Protos.Native;

namespace Protos.UI
{
    /// <summary>
    /// Two semi-transparent borderless progress-bar overlays shown at the bottom of
    /// the primary monitor: blue for master volume, green for Spotify/wave volume.
    /// </summary>
    public class VolumeOverlay : IDisposable
    {
        private readonly Form        _masterForm;
        private readonly Form        _waveForm;
        private readonly ProgressBar _masterBar;
        private readonly ProgressBar _waveBar;
        private readonly System.Windows.Forms.Timer _hideTimer;

        private const int BarWidth  = 320;
        private const int BarHeight = 20;
        private const int BarMargin = 8;
        private const int HideDelay = 2000;

        // Opacity value: 0.20 = 20%
        private const double OverlayOpacity = 0.20;

        public VolumeOverlay()
        {
            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            int screenW = screen.Bounds.Width;
            int screenH = screen.Bounds.Height;
            int centerX = screen.Bounds.Left + (screenW - BarWidth) / 2;
            int masterY = screen.Bounds.Top + screenH - 80;
            int waveY   = masterY + BarHeight + BarMargin;

            _masterBar = new ProgressBar
            {
                Dock    = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value   = 0,
                Style   = ProgressBarStyle.Continuous,
            };
            ApplyBarColor(_masterBar, Color.FromArgb(0x42, 0xa5, 0xf5)); // blue

            _waveBar = new ProgressBar
            {
                Dock    = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value   = 0,
                Style   = ProgressBarStyle.Continuous,
            };
            ApplyBarColor(_waveBar, Color.FromArgb(0x1e, 0xd7, 0x60)); // green

            _masterForm = CreateOverlayForm(centerX, masterY, BarWidth, BarHeight);
            _masterForm.Controls.Add(_masterBar);

            _waveForm = CreateOverlayForm(centerX, waveY, BarWidth, BarHeight);
            _waveForm.Controls.Add(_waveBar);

            _hideTimer = new System.Windows.Forms.Timer { Interval = HideDelay };
            _hideTimer.Tick += (_, _) =>
            {
                _hideTimer.Stop();
                _masterForm.Visible = false;
                _waveForm.Visible   = false;
            };
        }

        private static Form CreateOverlayForm(int x, int y, int w, int h)
        {
            var f = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar   = false,
                TopMost         = true,
                StartPosition   = FormStartPosition.Manual,
                Location        = new Point(x, y),
                Size            = new Size(w, h),
                Opacity         = OverlayOpacity,
                BackColor       = Color.Black,
            };
            // Make click-through: WS_EX_TRANSPARENT
            f.Load += (_, _) =>
            {
                int exStyle = NativeMethods.GetWindowLong(f.Handle, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(f.Handle, NativeMethods.GWL_EXSTYLE,
                    exStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED);
            };
            return f;
        }

        private static void ApplyBarColor(ProgressBar bar, Color color)
        {
            // ProgressBar color via custom painting
            bar.Paint += (_, e) =>
            {
                Rectangle rect = new(0, 0,
                    (int)(bar.Width * (bar.Value / (double)bar.Maximum)),
                    bar.Height);
                using var brush = new SolidBrush(color);
                e.Graphics.FillRectangle(brush, rect);
            };
        }

        /// <summary>Show overlay with given volume levels (0.0–1.0).</summary>
        public void Show(float masterLevel, float waveLevel)
        {
            if (IsFullScreenForeground()) return;

            _masterBar.Value = (int)Math.Clamp(masterLevel * 100, 0, 100);
            _waveBar.Value   = (int)Math.Clamp(waveLevel   * 100, 0, 100);

            _masterForm.Visible = true;
            _waveForm.Visible   = true;

            _masterForm.Invalidate();
            _waveForm.Invalidate();

            _hideTimer.Stop();
            _hideTimer.Start();
        }

        private static bool IsFullScreenForeground()
        {
            IntPtr hwnd = NativeMethods.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            if (!NativeMethods.GetWindowRect(hwnd, out RECT r)) return false;

            var screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            return r.Left  <= screen.Bounds.Left &&
                   r.Top   <= screen.Bounds.Top  &&
                   r.Right >= screen.Bounds.Right &&
                   r.Bottom >= screen.Bounds.Bottom;
        }

        public bool InvokeRequired => _masterForm.InvokeRequired;

        public void BeginInvoke(Action action) => _masterForm.BeginInvoke(action);

        public void Dispose()
        {
            _hideTimer.Dispose();
            _masterForm.Dispose();
            _waveForm.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
