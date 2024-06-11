#if UNITY_EDITOR_WIN
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public static class WebView2PostBuildCopyFiles
{
    private const string WebView2Exe = "/Youre-ID.exe";
    private const string WebView2Folder = "/WinWebView";

    private static readonly string[] WebView2Dlls =
    {
        "/Microsoft.Web.WebView2.Core.dll",
        "/Microsoft.Web.WebView2.WinForms.dll",

    };

    [PostProcessBuild]
    public static void Start(BuildTarget target, string buildDirectory)
    {
        if(target == BuildTarget.Android || target == BuildTarget.iOS)
            return;

        var path = new StackTrace(true).GetFrame(0).GetFileName();
        var pathWebView2 = Directory.GetParent(path).Parent.Parent.FullName;
        var pathBuild = Directory.GetParent(buildDirectory).FullName + "/" + Application.productName + "_Data";

        Copy(pathWebView2 + WebView2Folder + WebView2Exe, pathBuild + WebView2Exe);

        foreach (var dll in WebView2Dlls)
            Copy(pathWebView2 + WebView2Folder + dll, pathBuild + dll);

        Directory.CreateDirectory(pathBuild + "/runtimes/win-arm64/native");
        Directory.CreateDirectory(pathBuild + "/runtimes/win-x64/native");
        Directory.CreateDirectory(pathBuild + "/runtimes/win-x86/native");
        Copy(pathWebView2 + WebView2Folder + "/runtimes/win-arm64/native/WebView2Loader.dll", pathBuild + "/runtimes/win-arm64/native/WebView2Loader.dll");
        Copy(pathWebView2 + WebView2Folder + "/runtimes/win-x64/native/WebView2Loader.dll", pathBuild + "/runtimes/win-x64/native/WebView2Loader.dll");
        Copy(pathWebView2 + WebView2Folder + "/runtimes/win-x86/native/WebView2Loader.dll", pathBuild + "/runtimes/win-x86/native/WebView2Loader.dll");

    }

    private static void Copy(string from, string dest) => File.Copy(from, dest, true);
}
#endif