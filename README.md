# Youre-for-Unity

> The YOURE Sign In Component Unity Package provides a simple and convenient way for Unity developers to integrate YOURE sign-in functionality into their applications. With this package, users can quickly and easily sign in to YOURE and access their accounts without leaving the Unity environment.


### Supported Platforms: 
Android, iOS


## Installation via Unity Package Manager

Installing a Unity Package via Git URL
You can install a Unity package via Git URL using the Package Manager. Here are the steps to follow:
1. Open your Unity project.
2. Open the Package Manager window by selecting Window > Package Manager from the Unity Editor menu.
3. Click the "+" button at the top left corner of the Package Manager window and select "Add package from git URL".
4. In the text field that appears, enter the Git URL: https://github.com/Youre-Group/Youre-for-Unity.git 
5. Click the Add button to begin the installation process.
6. Once the installation is complete, the package will be available in your project and you can start using it.

### iOS
To compile on iOS you will need to add **WebKit.framework** to the **UnityFramework Target** in XCODE manually.

### Android Manifest
If you experience low performance within the signup, please make sure to activate hardware acceleration (`android:hardwareAccelerated="true"`) for the main activity (AndroidManifest.xml in `unityLibrary` module)

> More information regarding hardware acceleration in unity: https://forum.unity.com/threads/android-hardwareaccelerated-is-forced-false-in-all-activities.532786/




## Usage

```c#
public class SimpleAuthenticate : MonoBehaviour
{
    private void Start()
    {
        // YOURE Games will provide you with client id, endpoint url, redirect url
        Youre.Init("ENTER YOUR CLIENT ID","https://ENTER YOUR ENDPOINT URL","https://ENTER_YOUR_REDIRECT_URL");
    
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
            Debug.Log("Received YOURE Auth0 access token from callback: "+user.AccessToken);
        };
        
        StartAuthenticationAsync();
    }

    private async void StartAuthenticationAsync()
    {
        var options = new Authentication.AuthOptions
        {
            // SignInViewBackgroundTransparent = true,
            SignInViewMargins = new Authentication.AuthOptions.Margins(0,0,0,0),
        };
        
        await Youre.Auth.AuthenticateAsync(options);
    }
}
```

## Example Server Validation (php)
```php
$endpoint = 'YOUR_ENDPOINT_URL'; // Replace with your actual endpoint URL
$authToken =  'YOUR_ACCESS_TOKEN'; // Replace with actual access token generated from client


$url = $endpoint . '/oauth/userInfo';

$ch = curl_init($url);

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_HTTPHEADER, [
    'Authorization: Bearer ' . $authToken
]);

$response = curl_exec($ch);

if (curl_errno($ch)) {
    echo 'Error: ' . curl_error($ch);
} else {
    // Handle the response here
    echo $response;
}

curl_close($ch);
```


## Misc

### Logout
```c#
Youre.Auth.Logout();
```

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
