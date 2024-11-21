# Youre-for-Unity

> The YOURE.ID Sign In Component Unity Package provides a simple and convenient way for Unity developers to integrate YOURE sign-in functionality into their applications. With this package, users can quickly and easily sign in to YOURE and access their accounts without leaving the Unity environment.

### Supported Platforms: 
Android, iOS, 

## Installation via Unity Package Manager
s
Installing a Unity Package via Git URL
You can install a Unity package via Git URL using the Package Manager. Here are the steps to follow:
1. Open your Unity project.
2. Open the Package Manager window by selecting Window > Package Manager from the Unity Editor menu.
3. Click the "+" button at the top left corner of the Package Manager window and select "Add package from git URL".
4. In the text field that appears, enter the Git URL: https://github.com/Youre-Group/Youre-for-Unity.git 
5. Click the Add button to begin the installation process.
6. Once the installation is complete, the package will be available in your project and you can start using it.

### iOS
+ To compile on iOS you will need to add **SafariServices.framework** to the **UnityFramework Target** AND the **Unity-Iphone Target** (main build target) in XCODE manually. (General > Frameworks, Libraries, and Embedded Content)
+ Add following into the info.plist and
fill in your deeplink scheme (you will have to coordinate this with YOURE Dev Support)

```
<key>CFBundleURLTypes</key>
    <array>
    ....
        <dict>
            <key>CFBundleTypeRole</key>
            <string>Editor</string>
            <key>CFBundleURLName</key>
            <string>youre.id</string>
            <key>CFBundleURLSchemes</key>
            <array>
                <string>{YOUR_DEEPLINK_SCHEME}</string>
            </array>
        </dict>
    ....
    </array>
```

### Android Manifest
+ Add following into the main < activity > tag and fill in your deeplink scheme (you will have to coordinate this with YOURE Dev Support)
```
  <intent-filter>
    <action android:name="android.intent.action.VIEW"/>
    <category android:name="android.intent.category.DEFAULT"/>
    <category android:name="android.intent.category.BROWSABLE"/>
    <data android:scheme="{YOUR_DEEPLINK_SCHEME}" android:host="keycloak_callback" />
  </intent-filter>
```


## Usage

```c#
public class SimpleAuthenticate : MonoBehaviour
{
    private void Start()
    {
        // YOURE Games will provide you with client id, endpoint url
        // The deeplink scheme has to be coordinated with YOURE.
        Youre.Init("ENTER YOUR CLIENT ID","https://ENTER YOUR ENDPOINT URL","ENTER_YOUR_DEEPLINK_SCHEME");
    
        Youre.Auth.SignInFailed += (string error) =>
        {
            Debug.Log("SignIn failed:" + error);
        };

        Youre.Auth.SignInSucceeded += user =>
        {
            Debug.Log("Received YOURE.ID from callback: "+user.Id);
            Debug.Log("Received YOURE AccessToken from callback: "+user.AccessToken);
            Debug.Log("Received YOURE User Name from callback: "+user.UserName);
            Debug.Log("Received YOURE User Email from callback: "+user.Email);
        };
        
        Youre.Auth.SignIn();
    }
}
```

## Misc

### Logout 
```c#
bool isSignedOut = await Youre.Auth.SignOut();
```

## ISSUES
Due to compatibility we removed this dll from package. Please add this to project manually if you have compiler issues.
- System.Runtime.CompilerServices.Unsafe.dll

### License

Copyright © 2024, YOURE Games, The MIT License (MIT)
