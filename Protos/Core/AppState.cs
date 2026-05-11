namespace Protos.Core
{
    /// <summary>
    /// Shared mutable state for modifier keys and application modes.
    /// All fields are accessed only from the hook/dispatcher thread; no locking needed.
    /// </summary>
    public class AppState
    {
        // ── Keyboard modifier state ──────────────────────────────────────────
        public bool CapsLockHeld  { get; set; }
        public bool HomeHeld      { get; set; }
        public bool RAltHeld      { get; set; }
        public bool F4Held        { get; set; }
        public bool LCtrlHeld     { get; set; }
        public bool LShiftHeld    { get; set; }
        public bool LAltHeld      { get; set; }

        // ── Mouse button state ───────────────────────────────────────────────
        public bool LButtonDown   { get; set; }
        public bool RButtonDown   { get; set; }
        public bool MButtonDown   { get; set; }
        public bool XButton1Down  { get; set; }
        public bool XButton2Down  { get; set; }

        // ── Application modes ────────────────────────────────────────────────
        public bool Suspended     { get; set; }

        // ── Drag state ───────────────────────────────────────────────────────
        public bool  DragActive   { get; set; }
        public Native.POINT DragStartMouse { get; set; }
        public Native.POINT DragStartWindow { get; set; }
        public IntPtr DragHwnd    { get; set; }

        // ── Monitor off wait state ───────────────────────────────────────────
        public bool MonitorOffActive { get; set; }
    }
}
