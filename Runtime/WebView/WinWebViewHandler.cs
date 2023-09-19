#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using UnityEngine;
using ZenFulcrum.EmbeddedBrowser;

using static Youre_for_Unity.Runtime.Authentication;

namespace YourePlugin.WebView
{
    public class WinWebViewHandler : WebViewHandler
    {
        public override event Action<string> OnAuthCodeReceived;

        private Browser _browser;

        public override void CreateWebView(AuthOptions.Margins margins, bool isBackgroundTransparent = false)
        {
            var pPrefab = Resources.Load("YOUREWin64WebView");
            GameObject instance = (GameObject)GameObject.Instantiate(pPrefab, Vector3.zero, Quaternion.identity);
        
            _browser = instance.GetComponentInChildren<Browser>();
            _browser.onNavStateChange += () =>
            {
                string url = _browser.Url;
            
                if (url.Contains("?code="))
                {
                    string authCode = url.Split("?code=")[1];
                    Youre.LogDebug($"AuthorisationCode received: {authCode}");
                    OnAuthCodeReceived?.Invoke(authCode);
                
                    DestroyWebView();
                }
            };

            instance.name = "YOUREWin64WebView";
            UnityEngine.Object.DontDestroyOnLoad(instance);
        }

        public override void DestroyWebView()
        {
            if(_browser != null)
                UnityEngine.Object.Destroy( _browser.gameObject);

            _browser = null;
        }

        public override void LoadUrl(string url)
        {
            _browser?.LoadURL(url, false);
        }
    }
}
#endif