using System;
using System.IO;
using System.Text;

namespace SearchFilesTool
{
    /// <summary>
    /// A comprehensive logging utility for debugging and crash analysis.
    /// Logs to both file and debug output.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SearchFilesTool");

        private static readonly string LogFilePath = Path.Combine(LogDirectory, "Application.log");
        private static readonly object LockObject = new object();
        private static bool _initialized = false;

        /// <summary>
        /// Initializes the logger and sets up the log file.
        /// Called automatically on first use.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (LockObject)
            {
                if (_initialized)
                    return;

                try
                {
                    // Create directory if it doesn't exist
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    // Write startup header to log
                    WriteToFile($"{'='} Application Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {'='}");
                    WriteToFile($"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                    WriteToFile($"OS: {Environment.OSVersion}");
                    WriteToFile($"Architecture: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
                    WriteToFile($"Available Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB");
                    WriteToFile("");

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize logger: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void Info(string message)
        {
            LogMessage("INFO", message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void Warn(string message)
        {
            LogMessage("WARN", message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void Error(string message)
        {
            LogMessage("ERROR", message);
        }

        /// <summary>
        /// Logs an exception with full details.
        /// </summary>
        public static void Error(string message, Exception exception)
        {
            lock (LockObject)
            {
                LogMessage("ERROR", message);
                LogMessage("ERROR", $"Exception Type: {exception.GetType().FullName}");
                LogMessage("ERROR", $"Exception Message: {exception.Message}");
                LogMessage("ERROR", $"Stack Trace: {exception.StackTrace}");

                if (exception.InnerException != null)
                {
                    LogMessage("ERROR", $"Inner Exception: {exception.InnerException.Message}");
                    LogMessage("ERROR", $"Inner Stack Trace: {exception.InnerException.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Logs a debug message (only in Debug configuration).
        /// </summary>
        public static void Debug(string message)
        {
#if DEBUG
            LogMessage("DEBUG", message);
#endif
        }

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        /// <summary>
        /// Gets the log directory path.
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }

        /// <summary>
        /// Internal method to format and write log messages.
        /// </summary>
        private static void LogMessage(string level, string message)
        {
            if (!_initialized)
                Initialize();

            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            // Write to debug output
            System.Diagnostics.Debug.WriteLine(formattedMessage);

            // Write to file
            WriteToFile(formattedMessage);
        }

        /// <summary>
        /// Internal method to safely write to the log file.
        /// </summary>
        private static void WriteToFile(string message)
        {
            lock (LockObject)
            {
                try
                {
                    // Ensure directory exists
                    if (!Directory.Exists(LogDirectory))
                        Directory.CreateDirectory(LogDirectory);

                    // Append to log file with UTF-8 encoding
                    File.AppendAllText(LogFilePath, message + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Silently fail if we can't write to log
                    // Don't throw exceptions in the logger itself
                }
            }
        }

        /// <summary>
        /// Clears old log files older than the specified number of days.
        /// </summary>
        public static void CleanupOldLogs(int daysToKeep = 7)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                string[] logFiles = Directory.GetFiles(LogDirectory, "*.log");
                DateTime cutoffDate = DateTime.Now.AddDays(-daysToKeep);

                foreach (string logFile in logFiles)
                {
                    FileInfo fileInfo = new FileInfo(logFile);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        try
                        {
                            fileInfo.Delete();
                        }
                        catch
                        {
                            // Ignore errors when deleting old logs
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore cleanup errors
            }
        }
    }
}
