using System.Diagnostics;
using System.Runtime.InteropServices;
using Protos.Native;
using Protos.UI;

namespace Protos.Core
{
    public class VolumeManager
    {
        private int _volumeStep = 4;
        private VolumeOverlay? _overlay;

        public int VolumeStep
        {
            get => _volumeStep;
            set => _volumeStep = Math.Max(1, Math.Min(100, value));
        }

        public void SetOverlay(VolumeOverlay overlay)
        {
            _overlay = overlay;
        }

        // ── Master volume ────────────────────────────────────────────────────

        public void MasterVolumeUp()   => ChangeMasterVolume(+_volumeStep);
        public void MasterVolumeDown() => ChangeMasterVolume(-_volumeStep);

        private void ChangeMasterVolume(int delta)
        {
            float current = GetMasterVolume();
            float newVol  = Math.Clamp(current + delta / 100f, 0f, 1f);
            SetMasterVolume(newVol);
            ShowOverlay();
        }

        public float GetMasterVolume()
        {
            try
            {
                var enumerator = CreateEnumerator();
                if (enumerator == null) return 0f;

                enumerator.GetDefaultAudioEndpoint(0, 0, out IMMDevice device);
                if (device == null) return 0f;

                Guid epVolId = typeof(IAudioEndpointVolume).GUID;
                device.Activate(ref epVolId, 1 /*CLSCTX_INPROC_SERVER*/, IntPtr.Zero, out object epVolObj);
                var epVol = (IAudioEndpointVolume)epVolObj;
                epVol.GetMasterVolumeLevelScalar(out float level);
                return level;
            }
            catch { return 0f; }
        }

        private void SetMasterVolume(float level)
        {
            try
            {
                var enumerator = CreateEnumerator();
                if (enumerator == null) return;

                enumerator.GetDefaultAudioEndpoint(0, 0, out IMMDevice device);
                if (device == null) return;

                Guid epVolId = typeof(IAudioEndpointVolume).GUID;
                device.Activate(ref epVolId, 1, IntPtr.Zero, out object epVolObj);
                var epVol = (IAudioEndpointVolume)epVolObj;
                Guid eventCtx = Guid.Empty;
                epVol.SetMasterVolumeLevelScalar(level, ref eventCtx);
            }
            catch { }
        }

        // ── Spotify (per-process) volume ─────────────────────────────────────

        public void SpotifyVolumeUp()   => ChangeSpotifyVolume(+_volumeStep);
        public void SpotifyVolumeDown() => ChangeSpotifyVolume(-_volumeStep);

        private void ChangeSpotifyVolume(int delta)
        {
            try
            {
                var session = FindSpotifySession();
                if (session == null) return;

                session.GetMasterVolume(out float current);
                float newVol = Math.Clamp(current + delta / 100f, 0f, 1f);
                Guid empty = Guid.Empty;
                session.SetMasterVolume(newVol, ref empty);
            }
            catch { }
            ShowOverlay();
        }

        public float GetSpotifyVolume()
        {
            try
            {
                var session = FindSpotifySession();
                if (session == null) return 0f;
                session.GetMasterVolume(out float level);
                return level;
            }
            catch { return 0f; }
        }

        private ISimpleAudioVolume? FindSpotifySession()
        {
            var enumerator = CreateEnumerator();
            if (enumerator == null) return null;

            enumerator.GetDefaultAudioEndpoint(0, 0, out IMMDevice device);
            if (device == null) return null;

            Guid mgr2Id = typeof(IAudioSessionManager2).GUID;
            device.Activate(ref mgr2Id, 1, IntPtr.Zero, out object mgr2Obj);
            var mgr2 = (IAudioSessionManager2)mgr2Obj;

            mgr2.GetSessionEnumerator(out IAudioSessionEnumerator sessionEnum);
            sessionEnum.GetCount(out int count);

            for (int i = 0; i < count; i++)
            {
                try
                {
                    sessionEnum.GetSession(i, out IAudioSessionControl2 ctrl2);
                    ctrl2.GetProcessId(out uint pid);
                    if (pid == 0) continue;

                    var proc = Process.GetProcessById((int)pid);
                    if (proc.ProcessName.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
                    {
                        // QI for ISimpleAudioVolume
                        var simpleVol = ctrl2 as ISimpleAudioVolume;
                        if (simpleVol != null) return simpleVol;
                    }
                }
                catch { }
            }
            return null;
        }

        // ── Volume mute ──────────────────────────────────────────────────────

        public void ToggleMute()
        {
            SendKeys.SendMediaKey(0xAD); // VK_VOLUME_MUTE
        }

        // ── Overlay ──────────────────────────────────────────────────────────

        private void ShowOverlay()
        {
            if (_overlay == null) return;

            float master  = GetMasterVolume();
            float spotify = GetSpotifyVolume();

            if (_overlay.InvokeRequired)
                _overlay.BeginInvoke(() => _overlay.Show(master, spotify));
            else
                _overlay.Show(master, spotify);
        }

        // ── COM helper ───────────────────────────────────────────────────────

        private static IMMDeviceEnumerator? CreateEnumerator()
        {
            try
            {
                var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"));
                if (type == null) return null;
                return (IMMDeviceEnumerator?)Activator.CreateInstance(type);
            }
            catch { return null; }
        }
    }
}
