using System;
using UnityEngine;

namespace YourePlugin.WebView
{
    public class MobileWebViewHandler : WebViewHandler
    {
        private WinWebViewObject _webView;

        public override void CreateWebView(Authentication.AuthOptions.Margins margins, bool isBackgroundTransparent = false)
        {

            var webViewObject = (new GameObject("YOURESignInWebView")).AddComponent<WinWebViewObject>();
            UnityEngine.Object.DontDestroyOnLoad(webViewObject.gameObject);

           
            webViewObject.Init(
                cb: (msg) =>
                {
                },
                err: (msg) =>
                {
                    Youre.LogDebug(string.Format("Error while loading login url: [{0}]", msg));
                    DestroyWebView();
                },
                httpErr: (msg) =>
                {
                    Youre.LogDebug(string.Format("Error while loading login url: CallOnHttpError[{0}]", msg));
                    DestroyWebView();
                },
                started: (msg) =>
                {
                    Youre.LogDebug("started: "+msg);
                    if (msg.Contains("?code="))
                    {
                        string authCode = msg.Split(new string[] { "?code=" }, StringSplitOptions.None)[1];
                        Youre.LogDebug($"AuthorisationCode received: {authCode}");
                        OnAuthCodeReceived?.Invoke(authCode);
                        DestroyWebView();
                    }

                    if (msg.Contains("?error="))
                    {
                        Youre.LogDebug("Authentication error");
                        DestroyWebView();
                        OnAuthError?.Invoke("Authentication error");
                    }
                },
                hooked: (msg) =>
                {
                },
                cookies: (msg) =>
                {
                },
                ld: (msg) =>
                {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
                    // NOTE: the following js definition is required only for UIWebView; if
                    // enabledWKWebView is true and runtime has WKWebView, Unity.call is defined
                    // directly by the native plugin.
#if true
                    var js = @"
                    if (!(window.webkit && window.webkit.messageHandlers)) {
                        window.Unity = {
                            call: function(msg) {
                                window.location = 'unity:' + msg;
                            }
                        };
                    }
                ";
#else
                // NOTE: depending on the situation, you might prefer this 'iframe' approach.
                // cf. https://github.com/gree/unity-webview/issues/189
                var js = @"
                    if (!(window.webkit && window.webkit.messageHandlers)) {
                        window.Unity = {
                            call: function(msg) {
                                var iframe = document.createElement('IFRAME');
                                iframe.setAttribute('src', 'unity:' + msg);
                                document.documentElement.appendChild(iframe);
                                iframe.parentNode.removeChild(iframe);
                                iframe = null;
                            }
                        };
                    }
                ";
#endif
#elif UNITY_WEBPLAYER || UNITY_WEBGL
                var js = @"
                    window.Unity = {
                        call:function(msg) {
                            parent.unityWebView.sendMessage('WebViewObject', msg);
                        }
                    };
                ";
#else
                var js = "";
#endif
                    webViewObject.EvaluateJS(js + @"Unity.call('ua=' + navigator.userAgent)");
                },
                transparent: isBackgroundTransparent,
                enableWKWebView: true
                //zoom: true,
                //ua: "custom user agent string",
                //radius: 0,  // rounded corner radius in pixel
                //// android
                //androidForceDarkMode: 0,  // 0: follow system setting, 1: force dark off, 2: force dark on
                //// ios
                //wkContentMode: 0,  // 0: recommended, 1: mobile, 2: desktop
                //wkAllowsLinkPreview: true,
                //// editor
                //separated: false
            );
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            webViewObject.bitmapRefreshCycle = 1;
#endif
            // cf. https://github.com/gree/unity-webview/pull/512
            // Added alertDialogEnabled flag to enable/disable alert/confirm/prompt dialogs. by KojiNakamaru · Pull Request #512 · gree/unity-webview
            //webViewObject.SetAlertDialogEnabled(false);

            // cf. https://github.com/gree/unity-webview/pull/728
            //webViewObject.SetCameraAccess(true);
            //webViewObject.SetMicrophoneAccess(true);

            // cf. https://github.com/gree/unity-webview/pull/550
            // introduced SetURLPattern(..., hookPattern). by KojiNakamaru · Pull Request #550 · gree/unity-webview
            //webViewObject.SetURLPattern("", "^https://.*youtube.com", "^https://.*google.com");

            // cf. https://github.com/gree/unity-webview/pull/570
            // Add BASIC authentication feature (Android and iOS with WKWebView only) by takeh1k0 · Pull Request #570 · gree/unity-webview
            //webViewObject.SetBasicAuthInfo("id", "password");

            //webViewObject.SetScrollbarsVisibility(true);

            if (margins != null)
                webViewObject.SetMargins(margins.Left, margins.Top, margins.Right, margins.Bottom);
            else
                webViewObject.SetMargins(20, 20, 20, 20);

            webViewObject.SetTextZoom(100); // android only. cf. https://stackoverflow.com/questions/21647641/android-webview-set-font-size-system-default/47017410#47017410
            webViewObject.SetVisibility(true);
            
            _webView = webViewObject;
        }

        public override void DestroyWebView()
        {
            if (_webView != null)
                UnityEngine.Object.Destroy(_webView.gameObject);
            _webView = null;
        }

        public override void LoadUrl(string url)
        {
            if (_webView != null)
                _webView.LoadURL(url);
        }
    }
}
