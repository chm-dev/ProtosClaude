namespace Protos
{
    internal static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main()
        {
            // Single-instance guard
            _mutex = new Mutex(true, "Global\\Protos_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "Protos is already running.",
                    "Protos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Hidden main form owns the NotifyIcon (prevents premature disposal)
            using var mainForm = new MainForm();
            Application.Run(mainForm);

            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }

    /// <summary>
    /// Hidden form that owns the message loop. Creates and disposes <see cref="App"/>.
    /// </summary>
    internal class MainForm : Form
    {
        private App? _app;

        public MainForm()
        {
            // Make the form invisible
            Text            = "Protos";
            WindowState     = FormWindowState.Minimized;
            ShowInTaskbar   = false;
            Opacity         = 0;
            FormBorderStyle = FormBorderStyle.None;
            Size            = new Size(1, 1);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Visible = false;

            try
            {
                _app = new App();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize Protos:\n{ex.Message}",
                    "Protos Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _app?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
