# Youre-for-Unity

> The YOURE Sign In Component Unity Package provides a simple and convenient way for Unity developers to integrate YOURE sign-in functionality into their applications. With this package, users can quickly and easily sign in to YOURE and access their accounts without leaving the Unity environment.

## Installation via Unity Package Manager

Installing a Unity Package via Git URL
You can install a Unity package via Git URL using the Package Manager. Here are the steps to follow:
1. Open your Unity project.
2. Open the Package Manager window by selecting Window > Package Manager from the Unity Editor menu.
3. Click the "+" button at the top left corner of the Package Manager window and select "Add package from git URL".
4. In the text field that appears, enter the Git URL: https://github.com/Youre-Group/Youre-for-Unity.git 
5. Click the Add button to begin the installation process.
6. Once the installation is complete, the package will be available in your project and you can start using it.

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