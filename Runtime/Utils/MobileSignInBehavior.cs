/*
 * Copyright (C) 2024 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using System.Threading.Tasks;
using Auth;
using JetBrains.Annotations;
using UnityEngine;
using YourePlugin;

public class MobileSignInBehavior : MonoBehaviour {

    private bool _replyReceived;
    private bool _authOperationInProgress;
    private bool _signinCancelled;
    private DateTime _watchForReplyStartTime;
    private bool _watchForReply;
      
    private bool _signedIn;
    private const double MaxSecondsToWaitForAuthReply = 3;

    private AuthClient _authClient;
        
    public Action SignInFailed;
    public Action SignInCancelled;
        
    [ItemCanBeNull]
    public async Task<AuthClientResult> SignIn(AuthClient authClient)
    {
        
        _authClient = authClient;
        _replyReceived = false;
        _signinCancelled = false;
        _authOperationInProgress = true;
        _watchForReply = false;
            
        AuthClientResult result = null;
        
        try
        {
            result = await _authClient.LoginAsync();
        }
        catch (Exception e)
        {
            Youre.LogDebug(e.ToString());
        }
        _signedIn = result != null;
            
        _authOperationInProgress = false;
        _watchForReply = false;

        if (_signedIn)
        {
            return result;
        }
        else if (_signinCancelled)
        {
            Youre.LogDebug("Sign-in was cancelled by the user.");
            SignInCancelled?.Invoke();
        }
        else
        {
            Youre.LogDebug("Failed to perform sign-in.");
            SignInFailed?.Invoke();
        }
        return null;
    }

    public async Task<bool> SignOut(AuthClient authClient)
    {
        Youre.LogDebug("Signing out...");
        _authClient = authClient;
        _signedIn = !await _authClient.LogoutAsync();

        _authOperationInProgress = false;
        _watchForReply = false;

        if (!_signedIn)
        {
            Youre.LogDebug("Sign-out was successfull.");
            return true;
        }
        else if (_signinCancelled)
        {
            Youre.LogDebug("Sign-out was cancelled by the user.");
        }
        else
        {
            Youre.LogDebug("Failed to perform sign-out.");
        }

        return false;
    }

       

    void OnApplicationPause(bool pauseStatus)
    {
        Youre.LogDebug("OnApplicationPause: "+pauseStatus);
        
        var resumed = !pauseStatus;
        if (resumed)
        {
            Youre.LogDebug("App was resumed.");
            if (_authOperationInProgress)
            {
                if (!_replyReceived)
                {
                    Youre.LogDebug(Application.absoluteURL);
                    if (Application.absoluteURL != "" && !Application.absoluteURL.EndsWith("keycloak_callback"))
                        _authClient.Browser.OnAuthReply(Application.absoluteURL);
           
                    _watchForReply = true;
                    _watchForReplyStartTime = DateTime.Now;
                }
            }
        }
    }

    void Update()
    {
        if (_watchForReply && DateTime.Now - _watchForReplyStartTime > TimeSpan.FromSeconds(MaxSecondsToWaitForAuthReply))
        {
            Youre.LogDebug("No auth reply received, assuming the user cancelled or was unable to complete the sign-in.");
            _watchForReply = false;
            _signinCancelled = true;
            _authClient.Browser.OnAuthReply(null);
        }
    }

    public void OnSafariClosed()
    {
        Youre.LogDebug("OnSafariClosed");
        _watchForReply = false;
        _signinCancelled = true;
        _authClient.Browser.OnAuthReply(null);
    }
    public void OnAuthReply(object value)
    {
        if (!_signinCancelled)
        {
            _watchForReply = false;
            _replyReceived = true;
            _authClient.Browser.OnAuthReply(value as string);
        }
    }
}