using System;
using UnityEngine;

namespace CimCareMod.Utils
{
    internal static class Logger
    {
        private static readonly string Prefix = "CimCareMod: ";

        public static bool LOG_BASE = false;

        public static bool LOG_OPTIONS = false;
        public static bool LOG_CAPACITY_MANAGEMENT = false;
        public static bool LOG_INCOME = false;
        public static bool LOG_CHANCES = false;

        public static bool LOG_PRODUCTION = false;
        public static bool LOG_SIMULATION = false;

        public static bool LOG_SENIORS = false;
        public static bool LOG_CHILDREN = false;

        public static void LogInfo(bool shouldLog, string message, params object[] args)
        {
            if (shouldLog)
            {
                LogInfo(message, args);
            }
        }

        internal static void LogInfo(object lOG_OPTIONS, string v)
        {
            throw new NotImplementedException();
        }

        public static void LogInfo(string message, params object[] args)
        {
            Debug.Log(Prefix + string.Format(message, args));
        }

        public static void LogWarning(bool shouldLog, string message, params object[] args)
        {
            if (shouldLog)
            {
                LogWarning(message, args);
            }
        }

        public static void LogWarning(string message, params object[] args)
        {
            Debug.LogWarning(Prefix + string.Format(message, args));
        }

        public static void LogError(bool shouldLog, string message, params object[] args)
        {
            if (shouldLog)
            {
                LogError(message, args);
            }
        }

        public static void LogError(string message, params object[] args)
        {
            Debug.LogError(Prefix + string.Format(message, args));
        }
    }
}