using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace MetaLogging
{
    /// <summary>
    /// An instance-based logger class that handles logging with support for debug mode,
    /// custom log directories, file suffixes, and optional caller information.  
    /// Additionally, this version lets you set a message prefix and suffix.
    /// </summary>
    public class InstanceLogger
    {
        // Instance fields for file and message configuration.
        private string logDir;
        private readonly string logFileName = "log.txt";
        private readonly string debugLogFileName = "debug_log.txt";
        private string logFileSuffix;  // Optional suffix for log file names

        // Additional fields for message customization.
        private string messagePrefix;
        private string messageSuffix;

        // Public properties for configuration.
        public bool DebugMode { get; set; }
        public bool IncludeCallerInfo { get; set; }

        private readonly int MaxRetries = 3;   // Maximum retries when file is locked
        private readonly int RetryDelayMs = 100; // Delay between retries (ms)

        /// <summary>
        /// Initializes a new instance of the InstanceLogger class with default settings.
        /// </summary>
        public InstanceLogger()
        {
            logDir = "logs";
            logFileSuffix = "";
            messagePrefix = "";
            messageSuffix = "";
            DebugMode = false;
            IncludeCallerInfo = true;

            // Ensure the log directory exists.
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        // Methods to configure caller info and file suffix.
        public void SetIncludeCallerInfo(bool include) => IncludeCallerInfo = include;
        public bool GetIncludeCallerInfo() => IncludeCallerInfo;
        public void SetLogFileSuffix(string suffix) => logFileSuffix = suffix;
        public void EnableDebugMode(bool enable) => DebugMode = enable;
        public bool GetDebugMode() => DebugMode;

        /// <summary>
        /// Sets the directory where log files will be stored.
        /// </summary>
        public void SetLogDirectory(string directory)
        {
            logDir = directory;
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        // New functions to set a prefix and suffix for log messages.
        public void SetMessagePrefix(string prefix) => messagePrefix = prefix;
        public void SetMessageSuffix(string suffix) => messageSuffix = suffix;

        // Logging Methods

        public void LogInformation(string message, params object[] args)
        {
            Log("INFO", FormatMessage(message, args), ConsoleColor.Green);
        }

        public void LogWarning(string message, params object[] args)
        {
            Log("WARNING", FormatMessage(message, args), ConsoleColor.Yellow);
        }

        public void LogError(string message, params object[] args)
        {
            Log("ERROR", FormatMessage(message, args), ConsoleColor.Red);
        }

        public void LogError(Exception exception, string message, params object[] args)
        {
            string formattedMessage = FormatMessage(message, args);
            string errorDetails = $"{formattedMessage}\nException: {exception.GetType().Name} - {exception.Message}\nStackTrace: {exception.StackTrace}";
            Log("ERROR", errorDetails, ConsoleColor.Red);
        }

        public void LogDebug(string message, params object[] args)
        {
            string debugMessage = FormatLogMessage("DEBUG", FormatMessage(message, args));
            WriteToFileWithRetry(GetDebugLogFilePath(), debugMessage);
            if (DebugMode)
            {
                WriteToConsole(debugMessage, ConsoleColor.Blue);
            }
        }

        public void LogCrash(Exception crash)
        {
            DateTime now = DateTime.Now;
            string formattedMessage = FormatMessage("Crash occurred at {0}", now);
            string errorDetails = $"{formattedMessage}\nException: {crash.GetType().Name} - {crash.Message}\nStackTrace: {crash.StackTrace}";
            string crashFile = Path.Combine(logDir, $"crash_{now:yyyy-MM-dd_HH-mm-ss}.txt");
            WriteToFileWithRetry(crashFile, errorDetails);
        }

        // Internal Methods

        private void Log(string level, string message, ConsoleColor color)
        {
            string logMessage = FormatLogMessage(level, message);
            WriteToFileWithRetry(GetLogFilePath(), logMessage);
            WriteToConsole(logMessage, color);
        }

        /// <summary>
        /// Formats a log message by including the timestamp, level, message prefix/suffix, and caller info.
        /// </summary>
        private string FormatLogMessage(string level, string message)
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
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {messagePrefix}{message}{messageSuffix}{callerInfo}";
        }

        /// <summary>
        /// Scans the call stack and returns the first frame outside of the InstanceLogger class.
        /// </summary>
        private string GetCallerInfo()
        {
            var stackTrace = new StackTrace(true);
            // Start from frame 1 to skip the current method.
            for (int i = 1; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame.GetMethod();
                if (method.DeclaringType != typeof(InstanceLogger))
                {
                    string fileName = frame.GetFileName();
                    int lineNumber = frame.GetFileLineNumber();
                    string shortFileName = fileName != null ? Path.GetFileName(fileName) : "UnknownFile";
                    return $"{method.DeclaringType?.FullName}.{method.Name} (Line {lineNumber} in {shortFileName})";
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Replaces placeholders in the message with the provided values.
        /// </summary>
        public string FormatMessage(string message, params object[] values)
        {
            var pattern = @"\{(\w+)\}";
            int valueIndex = 0;
            return Regex.Replace(message, pattern, match =>
            {
                return valueIndex < values.Length ? values[valueIndex++]?.ToString() : match.Value;
            });
        }

        private void WriteToConsole(string message, ConsoleColor color)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        private void WriteToFileWithRetry(string filePath, string message)
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

        private string GetLogFilePath()
        {
            string fileName = string.IsNullOrEmpty(logFileSuffix)
                ? logFileName
                : $"{Path.GetFileNameWithoutExtension(logFileName)}_{logFileSuffix}{Path.GetExtension(logFileName)}";
            return Path.Combine(logDir, fileName);
        }

        private string GetDebugLogFilePath()
        {
            string fileName = string.IsNullOrEmpty(logFileSuffix)
                ? debugLogFileName
                : $"{Path.GetFileNameWithoutExtension(debugLogFileName)}_{logFileSuffix}{Path.GetExtension(debugLogFileName)}";
            return Path.Combine(logDir, fileName);
        }
    }
}
