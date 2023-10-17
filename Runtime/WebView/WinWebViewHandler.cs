#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Net;
using UnityEngine;
using WebView2;
using WebView2Forms;
using YourePlugin.Data;
using static YourePlugin.Authentication;
using System.IO;

namespace YourePlugin.WebView
{
    public class WinWebViewHandler : WebViewHandler
    {
        public override event Action<string> OnAuthCodeReceived;
        public override event Action<string> OnAuthError;
        private string _targetUrl;
        private GameObject _webViewObject;
        private IntPtr _hProcess;
        private PipeConnection _pipeConnection;

    
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
            Youre.LogDebug("OnMessage:"+ inputString);
            if (!string.IsNullOrEmpty(inputString))
            {
                if (inputString.StartsWith("URL="))
                {
                    string url = inputString.Substring(4);
                    Youre.LogDebug("Check URL: " + url);
                    if (url.Contains("?code="))
                    {
                        string authCode = url.Split(new string[] { "?code=" }, StringSplitOptions.None)[1];
                        Youre.LogDebug($"AuthorisationCode received: {authCode}");
                        OnAuthCodeReceived?.Invoke(authCode);
                        DestroyWebView();
                    }
                    if (url.Contains("?error="))
                    {
                        OnAuthError?.Invoke("Authorisation failed");
                        Youre.LogDebug($"Authorisation failed");
                        DestroyWebView();
                    }
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
            OnAuthError?.Invoke("Auth process disconnected");
            DestroyWebView();
        }


        public override void CreateWebView(AuthOptions.Margins margins, bool isBackgroundTransparent = false)
        {
            Application.runInBackground = true;

            DestroyWebView();

            _webViewObject = new GameObject("YOURESignInWebView");
            var script = _webViewObject.AddComponent<WinWebViewGameObject>();
            script.OnDestroyed += DestroyWebView;
            UnityEngine.Object.DontDestroyOnLoad(_webViewObject);


            Youre.LogDebug("CreateWebView");
            _pipeConnection = new PipeConnection();
            _pipeConnection.Connected += OnConnected;
            _pipeConnection.Disconnected += OnDisconnected;
            _pipeConnection.MessageReceived += OnMessage;

            StartWebViewProcess();
            
        }

        public override void DestroyWebView()
        {
            if(_webViewObject == null) 
                return;
            
            var script = _webViewObject.GetComponent<WinWebViewGameObject>();
            script.OnDestroyed -= DestroyWebView;

            UnityEngine.Object.Destroy(_webViewObject);
            _webViewObject = null;

            _pipeConnection.MessageReceived -= OnMessage;
            _pipeConnection.Connected -= OnConnected;
            _pipeConnection.Disconnected -= OnDisconnected;
            _pipeConnection.StopConnection();
            _pipeConnection = null;


            if (_hProcess != null)
                WinApi.Close(_hProcess);
            _hProcess = IntPtr.Zero;

        }

        public override void LoadUrl(string url)
        {
            _targetUrl = url;
            _pipeConnection.StartConnection();
        }
    }
}
#endif