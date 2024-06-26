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
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using YourePlugin.Data;
using YourePlugin.Utils;
using YourePlugin.WebView;
using System.Runtime.InteropServices;

namespace YourePlugin
{
    public class Authentication
    {

		[DllImport("__Internal")]
    	private static extern void GoToUrl(string url);

		[DllImport("__Internal")]
		private static extern string GetCurrentUrl();

        public event Action<YoureUser> SignInSucceeded;
        public event Action SignInShown;
        public event Action SignInRemoved;
        public event Action<string> SignInFailed;

        private readonly string _redirectUrl;
        private readonly string _endpoint;
        private readonly string _clientId;
        private readonly WebViewHandler _webviewHandler;

        public Authentication(string clientId, string endpointUrl, string redirectUrl)
        {
            Pkce.Init();
            _clientId = clientId;
            _endpoint = endpointUrl.TrimEnd('/');
           
            if (!redirectUrl.StartsWith("https://"))
                redirectUrl = "https://" + redirectUrl;
            
            _redirectUrl = redirectUrl.TrimEnd('/');
            

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _webviewHandler = new WinWebViewHandler();
#else 
            _webviewHandler = new MobileWebViewHandler();
#endif
            
        }

        /// <summary>
        /// Deletes previous cached access token data
        /// </summary>
        public void ClearSignInCache()
        {
            FlushTokenSetCache();
        }
        
        /// <summary>
        /// Requests global logout from currently logged in YOURE account
        /// </summary>
        public async Task Logout()
        {
            FlushTokenSetCache();
            string url = $"{_endpoint}/v2/logout?returnTo={UnityWebRequest.EscapeURL(_redirectUrl)}&client_id={_clientId}";
            UnityWebRequest request = UnityWebRequest.Get(url);
        }

        /// <summary>
        /// Returns AuthToken from cache or null if not found in cache
        /// </summary>
        /// <returns>AuthToken</returns>
        private AuthToken GetAuthToken()
        {
            AuthToken set = new AuthToken(
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
        public async Task AuthenticateAsync(AuthOptions authOptions = new AuthOptions())
        {
            AuthToken cachedAuthToken = GetAuthToken();
            if (cachedAuthToken == null)
            {
                string loginUrl = GetLoginUrl();
                string authCode = string.Empty;
#if UNITY_WEBGL
				var u = GetCurrentUrl();
				if(u.Contains("code="))
				{
					Pkce.CodeVerifier = PlayerPrefs.GetString("YOURE_last_code_verifier");
                    authCode = u.Split(new string[] { "code=" }, StringSplitOptions.None)[1].Split('&')[0];
				} else {
					// Save current code verifier used to create login url for later use
					PlayerPrefs.SetString("YOURE_last_code_verifier", Pkce.CodeVerifier);
					GoToUrl(loginUrl);
					return;
				}
#else
                authCode = await RequestAuthCodeAsync(loginUrl, authOptions);
#endif
                if (authCode == "-1") {
                    FlushTokenSetCache();
                    return;
                }
                else if (!string.IsNullOrEmpty(authCode))
                {
                    AuthToken newAuthToken = await RequestAccessTokenAsync(authCode);
                    cachedAuthToken = CacheTokenSet(newAuthToken);
                }
                else
                {
					FlushTokenSetCache();
					await Task.Delay(2000);
					await AuthenticateAsync(authOptions);
					return;
                }
            }

            // Try to get UserInfo with current access token
            YoureUser user = await GetUserInfo(cachedAuthToken);
            if (user == null) // we assume that token is expired here
            {
                AuthToken newAuthToken = await RefreshTokenAsync(cachedAuthToken.RefreshToken);
                if (newAuthToken == null)
                {
                    // Clear token cache and restart auth process from scratch (after delay)
                    FlushTokenSetCache();
                    await Task.Delay(2000);
                    await AuthenticateAsync(authOptions);
                    return;
                }
                
                cachedAuthToken = CacheTokenSet(newAuthToken);
                await Task.Delay(1000);
                AuthenticateAsync(authOptions);
                return;
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
        /// Create login url for 'YOURE login'
        /// </summary>
        /// <returns>Redirect url used to show 'YOURE login'</returns>
        private string GetLoginUrl()
        {
            string url = $"{_endpoint}/authorize?";
            url += $"client_id={_clientId}";
            url += $"&redirect_uri={UnityWebRequest.EscapeURL(_redirectUrl)}";
            url += "&response_type=code";
            url += "&token_endpoint_auth_method=none";
            url += $"&scope={UnityWebRequest.EscapeURL("openid email profile offline_access")}";
            url += $"&code_challenge={Pkce.CodeChallenge}";
            url += "&code_challenge_method=S256";
            return url;
        }

        /// <summary>
        /// Will try to retrieve YOURE Id using access token
        /// </summary>
        /// <returns>YoureUser</returns>
        private Task<YoureUser> GetUserInfo(AuthToken authToken)
        {


            TaskCompletionSource<YoureUser> tcs = new TaskCompletionSource<YoureUser>(TaskCreationOptions.RunContinuationsAsynchronously);

            string url = $"{_endpoint}/oauth/userInfo";
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {authToken.AccessToken}");
            UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();
            asyncOperation.completed += (obj) =>
            {
                try
                {
                	var user = JsonConvert.DeserializeObject<YoureUser>(request.downloadHandler.text);
                    user.AccessToken = authToken.AccessToken;
                    tcs.SetResult(user);
                }
                catch (Exception e)
                {
                    Youre.LogDebug($"Error while parsing userInfo response: {e.Message}");
                    tcs.SetResult(null);
                }
            };
        
            return tcs.Task;
        }
        /// <summary>
        /// Force destroy Login View
        /// </summary>
        public void DestroySignInOverlay()
        {
            _webviewHandler.DestroyWebView();
            SignInRemoved?.Invoke();
        }

        /// <summary>
        /// Will try to retrieve auth code by showing login view if neccessary
        /// </summary>
        /// <returns>Auth code used to requeset openid access token</returns>
        private async Task<string> RequestAuthCodeAsync(string authUrl, AuthOptions authOptions)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            void OnSuccess(string authCode) {
                SignInRemoved?.Invoke();
                tcs.SetResult(authCode);
            };
            void OnError(string error)
            {
                tcs.SetResult("-1");
                SignInFailed?.Invoke(error);
            };

            _webviewHandler.CreateWebView(authOptions.SignInViewMargins, authOptions.SignInViewBackgroundTransparent);

            _webviewHandler.OnAuthCodeReceived = OnSuccess;
            _webviewHandler.OnAuthError = OnError;

            _webviewHandler.LoadUrl(authUrl.Replace(" ", "%20"));

            SignInShown?.Invoke();
            await tcs.Task;
            return tcs.Task.Result;
        }

        
        
        
        /// <summary>
        /// Will try to refresh tokes with refreshtoken
        /// /// </summary>
        /// <returns>AuthToken</returns>
        private async Task<AuthToken?> RefreshTokenAsync(string refreshToken)
        {
            Youre.LogDebug("Try to refresh tokenset");
            TaskCompletionSource<AuthToken> tcs = new TaskCompletionSource<AuthToken>(TaskCreationOptions.RunContinuationsAsynchronously);

            string url = $"{_endpoint}/oauth/token";

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "grant_type", "refresh_token" },
                { "client_id", _clientId },
                {"scope", "openid email profile offline_access"},
                { "redirect_uri", _redirectUrl },
                { "refresh_token", refreshToken }
            };
            UnityWebRequest request = UnityWebRequest.Post(url, data);
            UnityWebRequestAsyncOperation asyncOperation = request.SendWebRequest();
            asyncOperation.completed += aop =>
            {
        
                try
                {
                    string tokenData = request.downloadHandler.text;
                    AuthToken authToken = JsonConvert.DeserializeObject<AuthToken>(tokenData);
                    Youre.LogDebug("Access Token set received: "+authToken.AccessToken);
                    tcs.SetResult(authToken);
                }
                catch (Exception)
                {
                    Youre.LogDebug("Error while parsing token data: "+request.downloadHandler.text);
                    tcs.SetResult(null);
                }
            };
        
            await tcs.Task;
            return tcs.Task.Result;
        }

        
        
        /// <summary>
        /// Will try to retrieve access tokens
        /// </summary>
        /// <returns>AuthToken</returns>
        private async Task<AuthToken> RequestAccessTokenAsync(string authCode)
        {
            TaskCompletionSource<AuthToken> tcs = new TaskCompletionSource<AuthToken>(TaskCreationOptions.RunContinuationsAsynchronously);

            string url = $"{_endpoint}/oauth/token";

            Dictionary<string, string> data = new Dictionary<string, string>()
            {
                { "grant_type", "authorization_code" },
                { "client_id", _clientId },
                { "code_verifier", Pkce.CodeVerifier },
                { "code", authCode },
                { "redirect_uri", _redirectUrl },
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
                    Youre.LogDebug("Access Token set received: "+authToken.AccessToken);
                    tcs.SetResult(authToken);
                }
                catch (Exception)
                {
                    Youre.LogDebug("Error while parsing token data: "+request.downloadHandler.text);
                }
            };
        
            await tcs.Task;
            return tcs.Task.Result;
        }

   
        public struct AuthOptions
        {
            public bool SignInViewBackgroundTransparent { get; set; }
            public Margins SignInViewMargins { get; set; }
        
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
}

