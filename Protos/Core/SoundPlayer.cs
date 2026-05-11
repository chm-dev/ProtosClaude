using Protos.Native;

namespace Protos.Core
{
    public static class SoundPlayer
    {
        private static int _counter;

        /// <summary>Play an audio file asynchronously (fire-and-forget).</summary>
        public static void Play(string filePath)
        {
            if (!File.Exists(filePath)) return;

            Task.Run(() =>
            {
                string alias = $"snd{System.Threading.Interlocked.Increment(ref _counter)}";
                NativeMethods.mciSendString($"open \"{filePath}\" type mpegvideo alias {alias}", null, 0, IntPtr.Zero);
                NativeMethods.mciSendString($"play {alias} wait", null, 0, IntPtr.Zero);
                NativeMethods.mciSendString($"close {alias}", null, 0, IntPtr.Zero);
            });
        }

        public static string GetSoundPath(string fileName)
        {
            string dir = AppContext.BaseDirectory;
            return Path.Combine(dir, "sounds", fileName);
        }
    }
}
