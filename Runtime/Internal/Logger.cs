using System.Diagnostics;
using Debug = UnityEngine.Debug;

// ReSharper disable once CheckNamespace
namespace PCore.Networking.Internal
{
    internal static class Logger
    {
        [Conditional("PCORE_NETWORK_STATUS_LOG")]
        public static void Log(string category, string message)
        {
            Debug.Log($"[{category}] {message}");
        }

        [Conditional("PCORE_NETWORK_R3_STATUS_LOG")]
        public static void LogError(string category, string message)
        {
            Debug.LogError($"[{category}] {message}");
        }
    }
}