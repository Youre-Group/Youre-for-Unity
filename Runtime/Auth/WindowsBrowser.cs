/*
 * Copyright (C) 2024 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using UnityEngine;
using WebView2Forms;
using System.IO;
using WebView2;
using System.Threading;
using YourePlugin;

namespace Auth
{
    public class WindowsBrowser: Browser
    {
        private string _targetUrl;
        private IntPtr _hProcess;
        private PipeConnection _pipeConnection;

        protected override void Launch(string url)
        {
            CreateWebView();
            LoadUrl(url);
        }

        public override void Dismiss()
        {
            if(_pipeConnection != null)
            {
                DestroyWebView();
            }
        }

        private static string WebViewProcessBinPath
        {
            get
            {
#if UNITY_EDITOR
                return Path.GetFullPath(@"Packages\de.youre.forunity\WinWebView\Youre-ID.exe");
#else
                return Application.dataPath + @"\Youre-ID.exe";
#endif
            }
        }

        private void StartWebViewProcess() => _hProcess = WinApi.Open(WebViewProcessBinPath);

        private void OnMessage(string inputString)
        {
            Youre.LogDebug("OnMessage:" + inputString);
            if (!string.IsNullOrEmpty(inputString))
            {
                if (inputString.Contains("keycloak_callback"))
                {
                    string url = inputString.Substring(4);
                    OnAuthReply(url);
                }
            }
        }

        private void OnConnected()
        {
            _pipeConnection.SendString($"URL={_targetUrl}");
        }

        private void OnDisconnected()
        {
            Youre.LogDebug("Auth process disconnected");
            OnAuthReply("");
            DestroyWebView();
        }

        public void CreateWebView()
        {
            Application.runInBackground = true;

            Youre.LogDebug("CreateWebView");
            _pipeConnection = new PipeConnection();
            _pipeConnection.Connected += OnConnected;
            _pipeConnection.Disconnected += OnDisconnected;
            _pipeConnection.MessageReceived += OnMessage;

            StartWebViewProcess();
        }


        public void DestroyWebView()
        {
            Youre.LogDebug("DestroyWebView");
            _pipeConnection.MessageReceived -= OnMessage;
            _pipeConnection.Connected -= OnConnected;
            _pipeConnection.Disconnected -= OnDisconnected;
            _pipeConnection.StopConnection();
            _pipeConnection = null;

            if (_hProcess != null)
               WinApi.Close(_hProcess);
            _hProcess = IntPtr.Zero;
        }

        public void LoadUrl(string url)
        {
            _targetUrl = url;
            _pipeConnection.StartConnection();
        }
    }
}
