using System;
using System.Threading.Tasks;
using Auth;
using Data;
using UnityEngine;

namespace YourePlugin
{
    public class Authentication
    {
        public event Action<YoureUser> SignInSucceeded;
        public event Action<string> SignInFailed;

        private readonly string _deeplinkScheme;
        private readonly string _authority;
        private readonly string _clientId;
        private AuthClient _authClient;

        public Authentication(string clientId,  string authority, string deeplinkScheme) 
        {
            _clientId = clientId;
            _authority = authority;
            _deeplinkScheme = $"{deeplinkScheme}://keycloak_callback";
        }

        public async Task<bool> SignOut()
        {
            if (_authClient == null)
            {
                Youre.LogDebug("No user signed in");
                return false;
            }
            GameObject oldSignInGO = GameObject.Find("SignInCanvas");
            if (oldSignInGO != null)
            {
                UnityEngine.Object.Destroy(oldSignInGO);
            }
            GameObject signIn = new GameObject("SignInCanvas");
            SignInBehavior go = signIn.AddComponent<SignInBehavior>();
            go.SignInFailed = () => SignInFailed?.Invoke("failed");
            bool signedOut = await go.SignOut(_authClient);
            UnityEngine.Object.Destroy(signIn);
            _authClient = null;
            return signedOut;
        }

        public async Task SignIn() 
        {
            _authClient = new AuthClient(_clientId, _authority, _deeplinkScheme);
            GameObject oldSignInGO = GameObject.Find("SignInCanvas");
            if (oldSignInGO != null)
            {
                UnityEngine.Object.Destroy(oldSignInGO);
            }
            GameObject signIn = new GameObject("SignInCanvas");
            SignInBehavior go = signIn.AddComponent<SignInBehavior>();
            
            go.SignInFailed = () => SignInFailed?.Invoke("failed");
            go.SignInCancelled = () => SignInFailed?.Invoke("canceled");
            
            AuthClientResult result = await go.SignIn(_authClient);
            
            UnityEngine.Object.Destroy(signIn);
            
            if (result != null)
            {
                YoureUser user = new YoureUser
                {
                    Id = result.UserId,
                    Email = result.Email,
                    UserName = result.UserName,
                    AccessToken = result.AccessToken,
                };
                SignInSucceeded?.Invoke(user);
            }
        }
    }
}