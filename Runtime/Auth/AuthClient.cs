/*
 * Copyright (C) 2024 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using IdentityModel.OidcClient.Infrastructure;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using UnityEngine;
using YourePlugin;

namespace Auth
{
    public class AuthClient
    {
        private OidcClient _client;
        private LoginResult _result;

        public AuthClient(string clientId, string authority, string redirectUrl)
        {
            // We must disable the IdentityModel log serializer to avoid Json serialize exceptions on IOS.
#if UNITY_IOS
            LogSerializer.Enabled = false;
#endif
            // See: https://www.youtube.com/watch?v=DdQTXrk6YTk
            // And for unity integration, see: https://qiita.com/lucifuges/items/b17d602417a9a249689f (Google translate to English!)
#if UNITY_ANDROID
            Browser = new AndroidChromeCustomTabBrowser();
#elif UNITY_IOS
            Browser = new SFSafariViewBrowser();
#endif
            CertificateHandler.Initialize();
            
            var options = new OidcClientOptions()
            {
                Authority = authority,
                TokenClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader,
                ClientId = clientId,
                Scope = "openid email profile",
                RedirectUri = redirectUrl,
                PostLogoutRedirectUri = redirectUrl,
                LoadProfile = false,
                Browser = Browser,
                LoggerFactory = new LoggerFactory()
            };
            _client = new OidcClient(options);
        }


        [ItemCanBeNull]
        public async Task<AuthClientResult> LoginAsync()
        {
            try
            {
                if (_client == null)
                {
                    Youre.LogDebug("no client inited");
                }
                _result = await _client.LoginAsync(new LoginRequest());
            }
            catch (Exception e)
            {
                Youre.LogDebug("Exception during login: " + e.Message);
            }
            finally
            {
                Youre.LogDebug("Dismissing sign-in browser.");
                Browser.Dismiss();
            }

            if (_result == null || _result.IsError)
            {
                Youre.LogDebug("Error authenticating: " + _result?.Error);
            }
            else
            {
                Youre.LogDebug("success");
                AuthClientResult result = new AuthClientResult
                {
                  Email = _result.User.Claims.First((c)=>c.Type == "email").Value,
                  UserName = _result.User.Claims.First((c) => c.Type == "preferred_username").Value,
                  UserId = _result.User.Claims.First((c) => c.Type == "sub").Value,
                  AccessToken = _result.AccessToken,
                };
                return result;
            }
            return null;
        }

        public async Task<bool> LogoutAsync()
        {
       
            try
            {
                if (_result != null)
                {
                    await _client.LogoutAsync(new LogoutRequest() {
                        BrowserDisplayMode = DisplayMode.Hidden,
                        IdTokenHint = _result.IdentityToken });
                    Youre.LogDebug("Signed out successfully.");
                    return true;
                }
            }
            catch (Exception e)
            {
                Youre.LogDebug("Failed to sign out: " + e.Message);
            }
            finally
            {
                Youre.LogDebug("Dismissing sign-out browser.");
                Browser.Dismiss();
                _client = null;
            }

            return false;
        }

        public MobileBrowser Browser { get; }
    }
}
