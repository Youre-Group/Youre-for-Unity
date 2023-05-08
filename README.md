# Youre-for-Unity

> The YOURE Sign In Component Unity Package provides a simple and convenient way for Unity developers to integrate YOURE sign-in functionality into their applications. With this package, users can quickly and easily sign in to YOURE and access their accounts without leaving the Unity environment.

## Usage

```c#
public class SimpleAuthenticate : MonoBehaviour
{
    private void Start()
    {
        Youre.Init("ENTER YOUR CLIENT ID");
    
        Youre.Auth.SignInShown += () =>
        {
            Debug.Log("SignIn overlay visible");
        };        
    
        Youre.Auth.SignInRemoved += () =>
        {
            Debug.Log("SignIn overlay closed");
        }; 
    
        Youre.Auth.SignInSucceeded += user =>
        {
            Debug.Log("Received YOURE User Id from callback: "+user.Id);
        };
        Youre.Auth.SignInFailed += error =>
        {
            Debug.Log(error);
        };
        
        StartAuthenticationAsync();
    }

    private async void StartAuthenticationAsync()
    {
        var options = new Authentication.AuthOptions
        {
            // SignInViewBackgroundTransparent = true,
            // SignInViewMargins = new Authentication.AuthOptions.Margins(50,50,50,50),
            // CustomEndpointUrl = "https://sso.prepro.youre.id"
        };
        
        await Youre.Auth.AuthenticateAsync(options);
    }
}
```
## Error Codes

| Error Code | Description                      |
|------------|----------------------------------|
| 101        | Error while requesting login url |
| 102        | Error while requesting login url |
| 103        | Error while parsing token data   |
| 104        | Error while loading login url    |
| 105        | User info response error         |


## Misc

### Force SignIn Overlay to close
```c#
Youre.Auth.DestroySignInOverlay();
```

### Clear signin cache to force fresh signin process
```c#
Youre.Auth.ClearSignInCache();
```

### License

Copyright © 2023, YOURE Games, The MIT License (MIT)