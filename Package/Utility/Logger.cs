#if UNITY_EDITOR
using UnityEngine;
#else
using System;
#endif

namespace com.Dunkingmachine.Utility
{
    public static class Logger
    {
        public static void Log(object msg)
        {
#if UNITY_EDITOR
            Debug.Log(msg);
#else
            Console.WriteLine(msg);
#endif
        }
        
        public static void LogWarning(object msg)
        {
#if UNITY_EDITOR
            Debug.LogWarning(msg);
#else
            Console.WriteLine(msg);
#endif
        }
        
        public static void LogError(object msg)
        {
#if UNITY_EDITOR
            Debug.LogError(msg);
#else
            Console.WriteLine(msg);
#endif
        }
    }
}