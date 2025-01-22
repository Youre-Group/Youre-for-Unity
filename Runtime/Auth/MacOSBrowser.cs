using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Utils;
using YourePlugin;
using Debug = UnityEngine.Debug;

namespace Auth
{
    using Debug = Debug;

    public class MacOSBrowser : Browser
    {
        private bool _hasLogin;
        private SocketServer _socketServer;
        private string _targetUrl;

        private static string WebViewProcessBinPath
        {
            get
            {
#if UNITY_EDITOR
                return Path.GetFullPath(@"Packages/Youre-for-Unity-Desktop/macOSWebView/YoureLogin.app");
#else
                return Application.dataPath + @"\YoureLogin.app";
#endif
            }
        }

        protected override void Launch(string url)
        {
            _targetUrl = url;
            CreateWebView();
            SendClientData();
        }


        private void SendClientData()
        {
            Console.WriteLine("_socketServer Start!");
            _socketServer = new SocketServer();
            _socketServer.Connected += OnConnected;
            _socketServer.Disconnected += OnDisconnected;
            _socketServer.MessageReceived += OnMessage;
            _socketServer.Start();
        }

        private void StartMacOSWebViewProcess()
        {
            try
            {
                var commandArguments = $"\"{WebViewProcessBinPath}\" --args \"{_targetUrl}\"";
                
                Process.Start("killall", "YoureLogin");
                Process.Start("open", commandArguments);

                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                Debug.Log($"Successfully launched WebViewProcessBinPath: {WebViewProcessBinPath}");
                Debug.Log($"Successfully launched _targetUrl: {_targetUrl}");
                Debug.Log(_targetUrl);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to launch app: {e.Message}");
            }
        }

        public void CreateWebView()
        {
            Application.runInBackground = true;
            StartMacOSWebViewProcess();
        }

        private void OnConnected()
        {
            Debug.Log("OnConnected");
        }

        private void OnMessage(string inputString)
        {
            
            Youre.LogDebug("OnMessage:" + inputString);
            
            if (!string.IsNullOrEmpty(inputString))
            {
                if (inputString.Contains("keycloak_callback"))
                {
                    _hasLogin = true;
                    var url = inputString.Substring(4);
                    OnAuthReply(url);
                }
                else if (inputString.Contains("CLOSE"))
                {
                    if (_hasLogin == false)
                        OnDisconnected();
                }
            }
        }

        private void OnDisconnected()
        {
            Youre.LogDebug("Auth process disconnected");
            OnAuthReply("");
            DestroyWebView();
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Application is exiting.");
            Process.Start("killall", "YoureLogin");
        }

        public override void Dismiss()
        {
            DestroyWebView();
        }

        public void DestroyWebView()
        {
            try
            {
                _socketServer.MessageReceived -= OnMessage;
                _socketServer.Connected -= OnConnected;
                _socketServer.Disconnected -= OnDisconnected;
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                _socketServer.Disconnect();

                Process.Start("killall", "YoureLogin");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to stop app: {e.Message}");
            }
        }
    }
}