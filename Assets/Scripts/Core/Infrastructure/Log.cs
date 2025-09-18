using System.Diagnostics;
using UnityEngine;

namespace Azurin.Core
{
    /// <summary>
    /// Simple logging wrapper with levels. Detailed logs are compiled out in Release.
    /// </summary>
    public static class Log
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")] public static void Trace(object msg) => UnityEngine.Debug.Log($"[TRACE] {msg}");
        public static void Info(object msg) => UnityEngine.Debug.Log($"[INFO] {msg}");
        public static void Warn(object msg) => UnityEngine.Debug.LogWarning($"[WARN] {msg}");
        public static void Error(object msg) => UnityEngine.Debug.LogError($"[ERROR] {msg}");
    }
}


