/*
 * Copyright (C) 2023 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Utils;
using Object = UnityEngine.Object;

public class Authentication
{
    public event Action<YoureUser> SignInSucceeded;
    public event Action SignInShown;
    public event Action SignInRemoved;
    public event Action<AuthError> SignInFailed;
    
    private string _endpoint;
    private readonly string _clientId;

    const string RedirectUrl = "unity:oauth";
    const string EndPointUrlExtension = "/oauth2";
    private const string DefaultEndPointUrl = "https://sso.prepro.youre.id";
    
    private WebViewObject _currentWebViewObject;

    public Authentication(string clientId)
    {
        Pkce.Init();
        _clientId = clientId;
    }

    /// <summary>
    /// Deletes previous cached access token data
    /// </summary>
    public void ClearLoginCache()
    {
        FlushTokenSetCache();
    }

    /// <summary>
    /// Will try to retrieve new access token with cached refresh token
    /// </summary>
    /// <returns>AuthToken</returns>
    private async Task<AuthToken> RefreshTokenSet()
    {
        TaskCompletionSource<AuthToken> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        string url = $"{_endpoint}/token";

        Dictionary<string, string> data = new()
        {
            { "grant_type", "refresh_token" },
            { "client_id", _clientId },
            { "refresh_token", GetAuthToken().RefreshToken }
        };

        UnityWebRequest request = UnityWebRequest.Post(url, data);
        UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();

        asyncOperation.completed += aop =>
        {
            Youre.LogDebug("Refreshed Access Token received");

            try
            {
                string tokenData = request.downloadHandler.text;
                Youre.LogDebug("Token data received: " + tokenData);
                AuthToken refreshedAuthToken = JsonConvert.DeserializeObject<AuthToken>(tokenData);
                tcs.SetResult(refreshedAuthToken);
            }
            catch (Exception e)
            {
                // Error while parsing token data.
                AuthError error = new(103, request.error);
                SignInFailed?.Invoke(error);
            }
        };
        await tcs.Task;
        return tcs.Task.Result;
    }

    /// <summary>
    /// Returns AuthToken from cache or null if not found in cache
    /// </summary>
    /// <returns>AuthToken</returns>
    private AuthToken GetAuthToken()
    {
        AuthToken set = new(
            PlayerPrefs.GetString("YOURE_token_id_token"),
            PlayerPrefs.GetString("YOURE_token_access_token"),
            PlayerPrefs.GetString("YOURE_token_refresh_token"));

        // Return null if token set is not valid
        if (string.IsNullOrEmpty(set.IdToken))
            return null;

        return set;
    }


    /// <summary>
    /// Will login to YOURE service; shows login form if necessary.
    /// Task will NOT complete if any error occurs.
    /// </summary>
    /// <param name="authOptions"></param>
    public async Task AuthenticateAsync(AuthOptions authOptions = new())
    {
        // change endpoint to custom or to default
        if (!string.IsNullOrEmpty(authOptions.CustomEndpointUrl))
            _endpoint = authOptions.CustomEndpointUrl.TrimEnd('/')+EndPointUrlExtension;
        else
            _endpoint = DefaultEndPointUrl.TrimEnd('/')+EndPointUrlExtension;
        
        
        AuthToken cachedAuthToken = GetAuthToken();
        if (cachedAuthToken == null)
        {
            string loginUrl = await RequestLoginAsync();
            string authCode = await RequestAuthCodeAsync(loginUrl, authOptions);
            AuthToken newAuthToken = await RequestAccessTokenAsync(authCode);
            cachedAuthToken = CacheTokenSet(newAuthToken);
        }
     
        // Try to get UserInfo with current access token
        YoureUser user = await GetUserInfo(cachedAuthToken);
        if (user == null) // we assume that token is expired here
        {
            // Try to refresh access tokens with refresh token
            AuthToken newAuthToken = await RefreshTokenSet();
            cachedAuthToken = CacheTokenSet(newAuthToken);

            // Try to get UserInfo with refreshed access token
            user = await GetUserInfo(cachedAuthToken);
            if (user == null) // refreshing access token does not worked out so well
            {
                // Clear token cache and restart auth process from scratch (after delay)
                FlushTokenSetCache();
                await Task.Delay(2000);
                await AuthenticateAsync(authOptions);
            }
        }

        SignInSucceeded?.Invoke(user);
    }
    
    /// <summary>
    /// Flush Token Cache
    /// </summary>
    private void FlushTokenSetCache()
    {
        PlayerPrefs.SetString("YOURE_token_id_token", string.Empty);
        PlayerPrefs.SetString("YOURE_token_access_token", string.Empty);
        PlayerPrefs.SetString("YOURE_token_refresh_token", string.Empty);
    }
    
    /// <summary>
    /// Saves token data to player prefs
    /// </summary>
    /// <param name="authToken">Will be modified and set equal to cached AuthToken</param>
    /// <returns>Cached AuthToken</returns>
    private AuthToken CacheTokenSet(AuthToken authToken)
    {
        PlayerPrefs.SetString("YOURE_token_id_token", authToken.IdToken);
        PlayerPrefs.SetString("YOURE_token_access_token", authToken.AccessToken);

        // Only replace if new refresh token is set
        if (!string.IsNullOrEmpty(authToken.RefreshToken))
            PlayerPrefs.SetString("YOURE_token_refresh_token", authToken.RefreshToken);

        authToken = GetAuthToken(); // set parameter to fresh token set (refresh token could be the old one)
        return authToken;
    }

    /// <summary>
    /// Will try to retrieve redirect url for 'YOURE login'
    /// </summary>
    /// <returns>Redirect url used to show 'YOURE login'</returns>
    private async Task<string> RequestLoginAsync()
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        string url = $"{_endpoint}/authorize?";
        url += $"client_id={_clientId}";
        url += $"&redirect_uri={RedirectUrl}";
        url += "&response_type=code";
        url += "&token_endpoint_auth_method=none";
        url += "&scope=openid email profile";
        url += $"&code_challenge={Pkce.CodeChallenge}";
        url += "&code_challenge_method=S256";
        UnityWebRequest request = UnityWebRequest.Get(url);
        UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();
        asyncOperation.completed += (obj) =>
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                Youre.LogDebug("Login redirect received");
                tcs.SetResult(request.url);
            }
            else
            {
                // Error while requesting login url
                AuthError exception = new(102, request.error);
                SignInFailed?.Invoke(exception);
            }
        };
        await tcs.Task;
        return tcs.Task.Result;
    }

    /// <summary>
    /// Will try to retrieve YOURE Id using access token
    /// </summary>
    /// <returns>YoureUser</returns>
    private Task<YoureUser> GetUserInfo(AuthToken authToken)
    {
        TaskCompletionSource<YoureUser> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        string url = $"{_endpoint}/userInfo";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", $"Bearer {authToken.AccessToken}");
        UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();
        asyncOperation.completed += (obj) =>
        {
            Youre.LogDebug("UserInfo response: " + request.downloadHandler.text);
            try
            {
                var user = JsonConvert.DeserializeObject<YoureUser>(request.downloadHandler.text);
                tcs.SetResult(user);
            }
            catch (Exception e)
            {
                AuthError error = JsonConvert.DeserializeObject<AuthError>(request.downloadHandler.text);
                if (error.Error == "invalid_token")
                {
                    Youre.LogDebug("Access Token expired");
                    tcs.SetResult(null);
                }
                else if (!string.IsNullOrEmpty(error.Error))
                {
                    error.Code = 105;
                    SignInFailed?.Invoke(error);
                }
                else
                {
                    SignInFailed?.Invoke( new AuthError(101, request.error));
                }
            }
        };
        
        return tcs.Task;
    }
    /// <summary>
    /// Force destroy Login View
    /// </summary>
    public void ForceDestroyLoginView()
    {
        DestroyWebView();
    }

    private void DestroyWebView()
    {
        if (_currentWebViewObject != null)
        {
            Object.DestroyImmediate(_currentWebViewObject.gameObject);
            _currentWebViewObject = null;
            SignInRemoved?.Invoke();
        }
    }

    /// <summary>
    /// Will try to retrieve auth code by showing login view if neccessary
    /// </summary>
    /// <returns>Auth code used to requeset openid access token</returns>
    private async Task<string> RequestAuthCodeAsync(string authUrl, AuthOptions authOptions)
    {
        TaskCompletionSource<string> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        _currentWebViewObject = CreateWebview(authOptions.SignInViewMargins,authOptions.SignInViewBackgroundTransparent, msg =>
        {
            if (msg.StartsWith("oauth?code="))
            {
                string authCode = msg.Replace("oauth?code=", "");
                Youre.LogDebug($"AuthorisationCode received: {authCode}");
                tcs.SetResult(authCode);

                DestroyWebView();
            }
        }, errorMsg =>
        {
            AuthError error = new(104, "Error while loading login url: "+errorMsg);
            SignInFailed?.Invoke(error);
        });

        _currentWebViewObject.LoadURL(authUrl.Replace(" ", "%20"));
        SignInShown?.Invoke();
        await tcs.Task;
        return tcs.Task.Result;
    }

    /// <summary>
    /// Will try to retrieve access tokens
    /// </summary>
    /// <returns>AuthToken</returns>
    private async Task<AuthToken> RequestAccessTokenAsync(string authCode)
    {
        TaskCompletionSource<AuthToken> tcs = new (TaskCreationOptions.RunContinuationsAsynchronously);

        string url = $"{_endpoint}/token";

        Dictionary<string, string> data = new()
        {
            { "grant_type", "authorization_code" },
            { "client_id", _clientId },
            { "code_verifier", Pkce.CodeVerifier },
            { "code", authCode },
            { "redirect_uri", RedirectUrl },
            { "token_endpoint_auth_method", "none" }
        };

        UnityWebRequest request = UnityWebRequest.Post(url, data);
        UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();
        asyncOperation.completed += aop =>
        {
        
            try
            {
                string tokenData = request.downloadHandler.text;
                AuthToken authToken = JsonConvert.DeserializeObject<AuthToken>(tokenData);
                Youre.LogDebug("Access Token set received: "+tokenData);
                tcs.SetResult(authToken);
            }
            catch (Exception e)
            {
                Youre.LogDebug("Error while parsing token data: "+request.downloadHandler.text);
            }
        };
        
        await tcs.Task;
        return tcs.Task.Result;
    }

    /// <summary>
    /// Creates webview object to show YOURE login page
    /// </summary>
    /// <param name="onMessageReceived">Callback used for listening to openid callback url</param>
    /// <param name="onError">Callback used for listening errors from webviewobject</param>
    /// <returns></returns>
    private WebViewObject CreateWebview(AuthOptions.Margins margins, bool isBackgroundTransparent, Action<string> onMessageReceived, Action<string> onError)
    {
        var webViewObject = (new GameObject("YOURESignInWebView")).AddComponent<WebViewObject>();
        Object.DontDestroyOnLoad(webViewObject.gameObject);
        
        webViewObject.Init(
            cb: (msg) =>
            {
                onMessageReceived?.Invoke(msg);
            },
            err: (msg) =>
            {
                Youre.LogDebug(string.Format("Error while loading login url: [{0}]", msg));
            },
            httpErr: (msg) =>
            {
                Youre.LogDebug(string.Format("Error while loading login url: CallOnHttpError[{0}]", msg));
                onError?.Invoke(msg);
            },
            started: (msg) => { },
            hooked: (msg) => { },
            cookies: (msg) => { },
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

        if(margins != null)
            webViewObject.SetMargins(margins.Left, margins.Top, margins.Right, margins.Bottom);
        else
            webViewObject.SetMargins(20, 20, 20, 20);
        
        webViewObject.SetTextZoom(100); // android only. cf. https://stackoverflow.com/questions/21647641/android-webview-set-font-size-system-default/47017410#47017410
        webViewObject.SetVisibility(true);
        return webViewObject;
    }

    public struct AuthOptions
    {
        public bool SignInViewBackgroundTransparent { get; set; }
        public Margins SignInViewMargins { get; set; }
        public string CustomEndpointUrl { get; set; }
        
        public class Margins
        {
            public int Left { get; set; }
            public int Right { get; set; }
            public int Top { get; set; }
            public int Bottom { get; set; }

            public Margins(int left, int right, int top, int bottom)
            {
                Left = left;
                Right = right;
                Top = top;
                Bottom = bottom;
            }
        }
    }
}

