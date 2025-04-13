using System;
using System.IO;
using System.Text;

namespace ModernFlyouts.Core.Logging
{
    public static class Logger
    {
        private static readonly string logFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModernFlyouts",
            "logs",
            $"ModernFlyouts_{DateTime.Now:yyyyMMdd}.log");

        public static void Initialize()
        {
            var logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void Log(LogLevel level, string message, Exception exception = null)
        {
            var logMessage = new StringBuilder();
            logMessage.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");

            if (exception != null)
            {
                logMessage.AppendLine($"Exception: {exception.Message}");
                logMessage.AppendLine($"Stack Trace: {exception.StackTrace}");
            }

            try
            {
                File.AppendAllText(logFilePath, logMessage.ToString());
            }
            catch (Exception ex)
            {
                // Loglama hatası durumunda konsola yazdır
                Console.WriteLine($"Logging error: {ex.Message}");
            }
        }

        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public static void Warning(string message, Exception exception = null)
        {
            Log(LogLevel.Warning, message, exception);
        }

        public static void Error(string message, Exception exception = null)
        {
            Log(LogLevel.Error, message, exception);
        }

        public static void Fatal(string message, Exception exception = null)
        {
            Log(LogLevel.Fatal, message, exception);
        }
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }
} 