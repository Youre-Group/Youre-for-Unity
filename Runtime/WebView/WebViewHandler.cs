using System;
using System.Collections.Generic;

namespace YourePlugin.WebView
{
    public abstract class WebViewHandler
    {
        public abstract void CreateWebView(Authentication.AuthOptions.Margins margins, bool isBackgroundTransparent = false);

        public abstract event Action<string> OnAuthCodeReceived;
        public abstract void LoadUrl(string url);
        public abstract void DestroyWebView();
    }
}