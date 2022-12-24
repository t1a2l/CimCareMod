using System;
using UnityEngine;

namespace CimCareMod {
    internal static class Logger {
        private static readonly string Prefix = "CimCareMod: ";

        public static readonly bool LOG_OPTIONS = false;
        public static readonly bool LOG_CAPACITY_MANAGEMENT = false;
        public static readonly bool LOG_INCOME = false;

        public static void logInfo(bool shouldLog, string message, params object[] args) 
        {
            if (shouldLog) 
            {
                Logger.logInfo(message, args);
            }
        }

        internal static void logInfo(object lOG_OPTIONS, string v) 
        {
            throw new NotImplementedException();
        }

        public static void logInfo(string message, params object[] args) 
        {
            Debug.Log(Prefix + string.Format(message, args));
        }

        public static void logWarning(bool shouldLog, string message, params object[] args) 
        {
            if (shouldLog) 
            {
                Logger.logWarning(message, args);
            }
        }

        public static void logWarning(string message, params object[] args) 
        {
            Debug.LogWarning(Prefix + string.Format(message, args));
        }

        public static void logError(bool shouldLog, string message, params object[] args) 
        {
            if (shouldLog) 
            {
                Logger.logError(message, args);
            }
        }

        public static void logError(string message, params object[] args) 
        {
            Debug.LogError(Prefix + string.Format(message, args));
        }
    }
}