/*
 * Copyright (C) 2023 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using Auth;
using UnityEngine;

namespace YourePlugin
{
    public sealed class Youre
    {
        /// <summary>
        /// If TRUE, debug output is enabled
        /// </summary>
        public static bool DebugMode { get; set; }

        /// <summary>
        /// YOURE authentication module; used to request YoureID;
        /// </summary>
        public static Authentication Auth
        {
            get
            {
                if (_auth == null)
                    Debug.LogError("Please call Youre.Init() before using any service.");
                return _auth;
            }
        }
    
        public static Youre Instance
        {
            get
            {
                if (_instance == null)
                    Debug.LogError("Please call Youre.Init() before using any service.");
                return _instance;
            }
        }
    
        private static Youre _instance;
        private static Authentication _auth;

        private Youre(string clientId, string endpointUrl, string deeplinkScheme)
        {
           
            _auth = new Authentication(clientId, endpointUrl, deeplinkScheme);
        }
    
        /// <summary>
        /// Init has to be called 'once' before any other actions
        /// </summary>
        /// <param name="clientId">Please request this id from technical support</param>
        /// <param name="endpointUrl">Please request this id from technical support</param>
        /// <param name="deeplinkScheme">Please request this id from technical support</param>
        /// <returns></returns>
        public static Youre Init(string clientId, string endpointUrl, string deeplinkScheme)
        {
            if (_instance != null)
            {
                LogDebug("Already initiated");
                return _instance;
            }

            _instance = new Youre(clientId, endpointUrl, deeplinkScheme);
            return _instance;
        }
    
        internal static void LogDebug(string message)
        {

            if (DebugMode)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                Debug.Log($"[YOURE] {message}");
            }
 
        }
 
    }
}