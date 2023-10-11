#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using UnityEngine;
using static YourePlugin.Authentication;

namespace YourePlugin.WebView
{
    public class WinWebViewHandler : WebViewHandler
    {
        public override event Action<string> OnAuthCodeReceived;


        public override void CreateWebView(AuthOptions.Margins margins, bool isBackgroundTransparent = false)
        {
        }

        public override void DestroyWebView()
        {
        }

        public override void LoadUrl(string url)
        {
        }
    }
}
#endif