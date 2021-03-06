﻿using System;
using System.Globalization;
using System.IO;
using System.Security;

namespace EasyImgur
{
    public static class Log
    {
        private static readonly Object LogfileLock = new Object();
        private static bool firstInvocation = true;
        private static string SaveLocation => Program.RootFolder;
        private static string LogFile { get { return Path.Combine(SaveLocation, "log.log"); } }

        /// <summary>
        /// Logs an informative message.
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="consoleOnly">If <see langword="true"/>, instead of writing the message to both console and file, it will be written only to the console.</param>
        /// <returns>Logged line containing the log channel and timestamp.</returns>
        public static string Info(string message, bool consoleOnly = false)
        {
            return LogMessage("INFO", message, consoleOnly);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="consoleOnly">If <see langword="true"/>, instead of writing the message to both console and file, it will be written only to the console.</param>
        /// <returns>Logged line containing the log channel and timestamp.</returns>
        public static string Warning(string message, bool consoleOnly = false)
        {
            return LogMessage("WARNING", message, consoleOnly);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="consoleOnly">If <see langword="true"/>, instead of writing the message to both console and file, it will be written only to the console.</param>
        /// <returns>Logged line containing the log channel and timestamp.</returns>
        public static string Error(string message, bool consoleOnly = false)
        {
            return LogMessage("ERROR", message, consoleOnly);
        }

        /// <summary>
        /// Logs a message to the console and log file.
        /// </summary>
        /// <param name="prefix">String to prepend to the message.</param>
        /// <param name="message">Log message.</param>
        /// <param name="consoleOnly">If <see langword="true"/>, instead of writing the message to both console and file, it will be written only to the console.</param>
        /// <returns>Logged line containing the log channel and timestamp.</returns>
        private static string LogMessage(string prefix, string message, bool consoleOnly)
        {
            var timeStamp = DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss:ffff]", CultureInfo.InvariantCulture);
            var line = timeStamp + " [" + prefix + "] " + message;

            Console.WriteLine(line);
            if (consoleOnly)
                return line;

            lock (LogfileLock)
            {
                if (firstInvocation)
                    firstInvocation = false;

                try
                {
                    if (firstInvocation)
                        File.WriteAllText(LogFile, line + Environment.NewLine);
                    else
                        File.AppendAllText(LogFile, line + Environment.NewLine);
                }
                catch (SecurityException ex)
                {
                    Error("A security exception occurred while trying to append to the history file: " + ex, true);
                }
                catch (Exception ex)
                {
                    Error("Failed to append to the log file: " + ex, true);
                }
            }
            return line;
        }
    }
}
