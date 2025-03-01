using System;
using System.Collections.Generic;

namespace NeuralNetworkLib
{
    public enum LogType
    {
        Warning,
        Error,
        Epoch,
        StateTransition,
        ActionDone,
        Simulation
    }

    public static class ConsoleLogger
    {
        private static HashSet<LogType> _enabledLogTypes = new HashSet<LogType>
        {
            LogType.Epoch, LogType.Warning, LogType.Error, LogType.StateTransition, LogType.ActionDone, LogType.Simulation
        };

        private static bool _includeTimestamp = true;
        private static bool _useColors = true;

        public static void EnableLogType(LogType type) => _enabledLogTypes.Add(type);
        public static void DisableLogType(LogType type) => _enabledLogTypes.Remove(type);
        public static void SetLogTypeEnabled(LogType type, bool enabled)
        {
            if (enabled)
                EnableLogType(type);
            else
                DisableLogType(type);
        }

        public static void Log(string message, LogType logType)
        {
            if (!_enabledLogTypes.Contains(logType))
                return;

            string timestamp = _includeTimestamp ? $"[{DateTime.Now:HH:mm:ss}] " : "";
            string logTypeStr = $"[{logType}] ";
            string fullMessage = timestamp + logTypeStr + message;

            if (_useColors)
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = GetColorForLogType(logType);
                Console.WriteLine(fullMessage);
                Console.ForegroundColor = originalColor;
            }
            else
            {
                Console.WriteLine(fullMessage);
            }
        }

        public static void Warning(string message) => Log(message, LogType.Warning);
        public static void Error(string message) => Log(message, LogType.Error);
        public static void Epoch(string message) => Log(message, LogType.Epoch);
        public static void StateTransition(string message) => Log(message, LogType.StateTransition);
        public static void ActionDone(string message) => Log(message, LogType.ActionDone);
        public static void Simulation(string message) => Log(message, LogType.Simulation);
        
        private static ConsoleColor GetColorForLogType(LogType logType)
        {
            return logType switch
            {
                LogType.Warning => ConsoleColor.Yellow,
                LogType.Error => ConsoleColor.Red,
                LogType.Epoch => ConsoleColor.Magenta,
                LogType.StateTransition => ConsoleColor.Cyan,
                LogType.ActionDone => ConsoleColor.Blue,
                LogType.Simulation => ConsoleColor.DarkGray,
                _ => ConsoleColor.White
            };
        }
    }
}