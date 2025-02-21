using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace MetaLogger
{
    /// <summary>
    /// A static logger class to handle application logging with support for debug mode,
    /// custom log directory, and optional caller information.
    /// </summary>
    public static class MetaLogger
    {
        // Private fields
        private static string logDir = "logs";  // Default log directory
        private static readonly string LogFileName = "log.txt";
        private static readonly string DebugLogFileName = "debug_log.txt";
        private static string logFileSuffix = "";  // Optional suffix for log file names

        // Public properties
        public static bool DebugMode = false; // Enable or disable debug mode.
        public static bool IncludeCallerInfo = true;  // Include caller info in logs.

        private static readonly int MaxRetries = 3;   // Maximum retries when file is locked
        private static readonly int RetryDelayMs = 100; // Delay between retries (ms)

        /// <summary>
        /// Enables or disables the inclusion of caller information in log messages.
        /// </summary>
        public static void SetIncludeCallerInfo(bool include) => IncludeCallerInfo = include;

        /// <summary>
        /// Retrieves the current setting for including caller information.
        /// </summary>
        public static bool GetIncludeCallerInfo() => IncludeCallerInfo;

        /// <summary>
        /// Sets a suffix to append to log file names (e.g., timestamps or build identifiers).
        /// </summary>
        public static void SetLogFileSuffix(string suffix) => logFileSuffix = suffix;

        /// <summary>
        /// Enables or disables debug mode for logging.
        /// </summary>
        public static void EnableDebugMode(bool enable) => DebugMode = enable;

        /// <summary>
        /// Retrieves the current debug mode setting.
        /// </summary>
        public static bool GetDebugMode() => DebugMode;

        /// <summary>
        /// Sets the directory where log files will be stored.
        /// </summary>
        public static void SetLogDirectory(string directory)
        {
            logDir = directory;
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        // Logging Methods

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void LogInformation(string message, params object[] args)
        {
            Log("INFO", FormatMessage(message, args), ConsoleColor.Green);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void LogWarning(string message, params object[] args)
        {
            Log("WARNING", FormatMessage(message, args), ConsoleColor.Yellow);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(string message, params object[] args)
        {
            Log("ERROR", FormatMessage(message, args), ConsoleColor.Red);
        }

        /// <summary>
        /// Logs an error message with exception details.
        /// </summary>
        public static void LogError(Exception exception, string message, params object[] args)
        {
            string formattedMessage = FormatMessage(message, args);
            string errorDetails = $"{formattedMessage}\nException: {exception.GetType().Name} - {exception.Message}\nStackTrace: {exception.StackTrace}";
            Log("ERROR", errorDetails, ConsoleColor.Red);
        }

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public static void LogDebug(string message, params object[] args)
        {
            string debugMessage = FormatLogMessage("DEBUG", FormatMessage(message, args));
            WriteToFileWithRetry(GetDebugLogFilePath(), debugMessage);
            if (DebugMode)
            {
                WriteToConsole(debugMessage, ConsoleColor.Blue);
            }
        }

        /// <summary>
        /// Logs a crash message.
        /// </summary>
        public static void LogCrash(Exception crash)
        {
            DateTime now = DateTime.Now;
            string formattedMessage = FormatMessage("Crash occurred at {0}", now);
            string errorDetails = $"{formattedMessage}\nException: {crash.GetType().Name} - {crash.Message}\nStackTrace: {crash.StackTrace}";
            string crashFile = Path.Combine(logDir, $"crash_{now:yyyy-MM-dd_HH-mm-ss}.txt");
            WriteToFileWithRetry(crashFile, errorDetails);
        }

        // Internal Methods

        private static void Log(string level, string message, ConsoleColor color)
        {
            string logMessage = FormatLogMessage(level, message);
            WriteToFileWithRetry(GetLogFilePath(), logMessage);
            WriteToConsole(logMessage, color);
        }

        /// <summary>
        /// Formats a log message by including the log level, message, and caller info.
        /// </summary>
        private static string FormatLogMessage(string level, string message)
        {
            string callerInfo = "";
            if (IncludeCallerInfo)
            {
                callerInfo = GetCallerInfo();
                if (!string.IsNullOrWhiteSpace(callerInfo))
                {
                    callerInfo = " | " + callerInfo;
                }
            }
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{callerInfo}";
        }

        /// <summary>
        /// Scans the call stack and returns the first frame outside of the CustomLogger class.
        /// </summary>
        private static string GetCallerInfo()
        {
            var stackTrace = new StackTrace(true);
            // Start from frame 1 to skip the current method.
            for (int i = 1; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame.GetMethod();
                if (method.DeclaringType != typeof(MetaLogger))
                {
                    string fileName = frame.GetFileName();
                    int lineNumber = frame.GetFileLineNumber();
                    // Return only the file name for brevity.
                    string shortFileName = fileName != null ? Path.GetFileName(fileName) : "UnknownFile";
                    return $"{method.DeclaringType?.FullName}.{method.Name} (Line {lineNumber} in {shortFileName})";
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Replaces placeholders in the message with the provided values.
        /// </summary>
        public static string FormatMessage(string message, params object[] values)
        {
            var pattern = @"\{(\w+)\}";
            int valueIndex = 0;
            return Regex.Replace(message, pattern, match =>
            {
                return valueIndex < values.Length ? values[valueIndex++]?.ToString() : match.Value;
            });
        }

        private static void WriteToConsole(string message, ConsoleColor color)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        private static void WriteToFileWithRetry(string filePath, string message)
        {
            int attempt = 0;
            bool written = false;

            while (attempt < MaxRetries && !written)
            {
                try
                {
                    File.AppendAllText(filePath, message + Environment.NewLine);
                    written = true;
                }
                catch (IOException ex)
                {
                    if (attempt == MaxRetries - 1)
                    {
                        WriteToConsole($"Failed to write to log file after {MaxRetries} attempts: {ex.Message}", ConsoleColor.Red);
                    }
                    else
                    {
                        Thread.Sleep(RetryDelayMs);
                    }
                }
                attempt++;
            }
        }

        private static string GetLogFilePath()
        {
            string fileName = string.IsNullOrEmpty(logFileSuffix)
                ? LogFileName
                : $"{Path.GetFileNameWithoutExtension(LogFileName)}_{logFileSuffix}{Path.GetExtension(LogFileName)}";
            return Path.Combine(logDir, fileName);
        }

        private static string GetDebugLogFilePath()
        {
            string fileName = string.IsNullOrEmpty(logFileSuffix)
                ? DebugLogFileName
                : $"{Path.GetFileNameWithoutExtension(DebugLogFileName)}_{logFileSuffix}{Path.GetExtension(DebugLogFileName)}";
            return Path.Combine(logDir, fileName);
        }
    }
}
