using System;
using System.IO;

namespace Grex.Services
{
    /// <summary>
    /// Simple, thread-safe application logger that caps %Temp%\Grex.log so it cannot
    /// grow without bound and cause UI stalls or disk bloat.
    /// </summary>
    public static class LogService
    {
        /// <summary>Full path of the default application log file (%Temp%\Grex.log).</summary>
        public static string LogFilePath { get; } = Path.Combine(Path.GetTempPath(), "Grex.log");
        private static readonly long MaxLogFileBytes = 1024 * 1024; // 1 MB
        private static readonly object LogLock = new object();

        /// <summary>
        /// Writes a timestamped message to the application log. The file is trimmed
        /// to roughly half of the max size when it exceeds the cap.
        /// </summary>
        public static void Write(string message) => Write(message, LogFilePath);

        /// <summary>
        /// Writes a timestamped message to the specified log file. The file is trimmed
        /// to roughly half of the max size when it exceeds the cap.
        /// </summary>
        public static void Write(string message, string logFilePath)
        {
            try
            {
                lock (LogLock)
                {
                    var logDir = Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var entry = $"[{timestamp}] {message}\n";
                    TrimLogIfNeeded(logFilePath);
                    File.AppendAllText(logFilePath, entry);
                }
            }
            catch
            {
                // Logging must never crash the application.
            }
        }

        private static void TrimLogIfNeeded(string logFilePath)
        {
            try
            {
                var fileInfo = new FileInfo(logFilePath);
                if (!fileInfo.Exists || fileInfo.Length <= MaxLogFileBytes)
                {
                    return;
                }

                // Keep the most recent ~512 KB of log entries.
                var bytesToKeep = MaxLogFileBytes / 2;
                var bytes = File.ReadAllBytes(logFilePath);
                if (bytes.Length <= bytesToKeep)
                {
                    return;
                }

                var start = bytes.Length - (int)bytesToKeep;

                // Advance to a clean line boundary so we don't keep a partial first line.
                // If there is no newline within the retained window (e.g. one huge entry),
                // keep the raw tail rather than scanning past the end and wiping the file.
                var lineBoundary = start;
                while (lineBoundary < bytes.Length && bytes[lineBoundary] != '\n')
                {
                    lineBoundary++;
                }

                if (lineBoundary < bytes.Length)
                {
                    start = lineBoundary + 1;
                }

                using var stream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.Write(bytes, start, bytes.Length - start);
            }
            catch
            {
                // If trimming fails, continue logging; the next write may succeed.
            }
        }
    }
}
