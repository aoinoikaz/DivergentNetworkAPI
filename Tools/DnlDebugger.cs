using System;
using System.IO;

namespace DivergentNetwork.Tools {

    public static class DnlDebugger {

        public static string FilePath { get; set; }

        public static void LogMessage(string logMessage, bool formatted) {

            if (string.IsNullOrEmpty(logMessage))
                throw new NullReferenceException("The provided log message is null. Ensure that string arguments are used.");

            logMessage += Environment.NewLine;

            Console.Write(formatted ? (DateTime.Now.ToString() + ": "  + logMessage) : logMessage);
        }


        public static void LogToFile(string logMessage, bool formatted) {

            if (string.IsNullOrEmpty(FilePath))
                throw new NullReferenceException("Debug file path cannot be null! Either set the file path or use a different logging method.");

            logMessage += Environment.NewLine;

            File.AppendAllText(FilePath, formatted ? (DateTime.Now.ToString() + ": " + logMessage) : logMessage);
        }
    }
}
