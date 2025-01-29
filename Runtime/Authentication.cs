using System;
using System.Threading.Tasks;
using Auth;
using Data;
using IdentityModel.OidcClient.Results;
using UnityEngine;
using UnityEngine.Networking;

namespace YourePlugin
{
    public class Authentication
    {
        public event Action<YoureUser> SignInSucceeded;
        public event Action SignInFailed;

        private readonly string _deeplinkScheme;
        private readonly string _authority;
        private readonly string _clientId;
        private AuthClient _authClient;
        private bool _signInInProgress;

        public Authentication(string clientId,  string authority, string deeplinkScheme) 
        {
            _clientId = clientId;
            _authority = authority;
            _deeplinkScheme = $"{deeplinkScheme}://keycloak_callback";
        }
        
        
        /// <summary>
        /// Will return TRUE if sign-in was used.
        /// </summary>
        public bool WasSignedIn()
        {
            string savedId = PlayerPrefs.GetString("YREID_lastID");
            if (string.IsNullOrEmpty(savedId))
                return false;

            return true;
        }
        
        private async Task SendRequestAsync(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Will return YoureUser Object if previous session is still valid
        /// </summary>
        public async Task<YoureUser> GetActiveUser()
        {
            string savedId = PlayerPrefs.GetString("YREID_lastID");
            if (string.IsNullOrEmpty(savedId))
                return null;

            string savedAccessToken = PlayerPrefs.GetString("YREID_lastAccessToken");
            if(string.IsNullOrEmpty(savedAccessToken))
                return null;

            UnityWebRequest request = UnityWebRequest.Get($"{_authority}/protocol/openid-connect/userinfo");
            request.SetRequestHeader("Authorization", "Bearer " + savedAccessToken);
            await SendRequestAsync(request);
    
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.downloadHandler.text.Contains(savedId))
                {
                    Youre.LogDebug("Old session valid");
                    YoureUser user = new()
                    {
                        Id = savedId,
                        Email = PlayerPrefs.GetString("YREID_lastEmail"),
                        UserName = PlayerPrefs.GetString("YREID_lastUserName"),
                        AccessToken = savedAccessToken,
                    };
                    return user;
                }
            }
            else
            {
                
                Youre.LogDebug("Old session not valid: " + request.error);
            }

            return null;
        }

        public async Task<bool> SignOut()
        {
            if (_authClient == null)
            {
                Youre.LogDebug("No user signed in");
                return false;
            }

            bool isSignedOut;
            
            if (Application.isMobilePlatform)
            {
                GameObject oldSignInGO = GameObject.Find("SignInCanvas");
                if (oldSignInGO != null)
                    UnityEngine.Object.Destroy(oldSignInGO);
                GameObject signIn = new GameObject("SignInCanvas");
                MobileSignInBehavior go = signIn.AddComponent<MobileSignInBehavior>();
                go.SignInFailed = () => SignInFailed?.Invoke();
                isSignedOut = await go.SignOut(_authClient);
                UnityEngine.Object.Destroy(signIn);
            }
            else
                isSignedOut = await _authClient.LogoutAsync();
          
            PlayerPrefs.DeleteKey("YREID_lastAccessToken");
            PlayerPrefs.DeleteKey("YREID_lastID");
            PlayerPrefs.DeleteKey("YREID_lastEmail");
            PlayerPrefs.DeleteKey("YREID_lastUserName");
            _authClient = null;
            
            return isSignedOut;
        }

        public async Task SignIn() 
        {
            if(_signInInProgress)
                return;

            
            _authClient = new AuthClient(_clientId, _authority, _deeplinkScheme);

            // Try to renew token if available
            if (WasSignedIn())
            {
                YoureUser user = await GetActiveUser();
                if (user == null)
                {
                    Youre.LogDebug("Refreshing token...");
                    String lastRefreshToken = PlayerPrefs.GetString("YREID_lastRefreshToken");
                    if (!string.IsNullOrEmpty(lastRefreshToken))
                    {
                        RefreshTokenResult refreshTokenResult = await _authClient.Refresh(lastRefreshToken);
                        if (!refreshTokenResult.IsError)
                        {
                            PlayerPrefs.SetString("YREID_lastAccessToken", refreshTokenResult.AccessToken);
                            PlayerPrefs.SetString("YREID_lastRefreshToken", refreshTokenResult.RefreshToken);
                            user = new()
                            {
                                Id = PlayerPrefs.GetString("YREID_lastID"),
                                Email = PlayerPrefs.GetString("YREID_lastEmail"),
                                UserName = PlayerPrefs.GetString("YREID_lastUserName"),
                                AccessToken = refreshTokenResult.AccessToken,
                            };
                            Youre.LogDebug("Token refreshed");
                            SignInSucceeded?.Invoke(user);
                            return;
                        }
                    }
                    else
                    {
                        Youre.LogDebug("Refresh Token was not stored yet. Will be stored with next login");
                    }
                }
                else
                {
                    SignInSucceeded?.Invoke(user);
                    return;
                }

            }
         
            Youre.LogDebug("Signing in...");
            AuthClientResult result = null;

            _signInInProgress = true;
            if (Application.isMobilePlatform)
            {
                GameObject oldSignInGO = GameObject.Find("SignInCanvas");
                if (oldSignInGO != null)
                {
                    UnityEngine.Object.Destroy(oldSignInGO);
                }
                GameObject signIn = new GameObject("SignInCanvas");
                MobileSignInBehavior go = signIn.AddComponent<MobileSignInBehavior>();
                result = await go.SignIn(_authClient);
                UnityEngine.Object.Destroy(signIn);
            }
            else
            {
                try
                {
                    result = await _authClient.LoginAsync();
                }
                catch (Exception e)
                {
                    Youre.LogDebug("error:");
                    Youre.LogDebug(e.ToString());
                }
            }
            
            _signInInProgress = false;
            
            if (result != null)
            {
                YoureUser user = new YoureUser
                {
                    Id = result.UserId,
                    Email = result.Email,
                    UserName = result.UserName,
                    AccessToken = result.AccessToken,
                };
                
                PlayerPrefs.SetString("YREID_lastRefreshToken", result.RefreshToken);
                PlayerPrefs.SetString("YREID_lastAccessToken", user.AccessToken);
                PlayerPrefs.SetString("YREID_lastID", user.Id);
                PlayerPrefs.SetString("YREID_lastEmail", user.Email);
                PlayerPrefs.SetString("YREID_lastUserName", user.UserName);
                
                SignInSucceeded?.Invoke(user);
            } 
            else
            {
                SignInFailed?.Invoke();
            }
        }
    }
}