/*
 * Copyright (C) 2024 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */


using System.Runtime.InteropServices;
using UnityEngine;

public static class SFSafariView
{
#if UNITY_IOS
    [DllImport("__Internal")]
    extern static void launchUrl(string url);
    [DllImport("__Internal")]
    extern static void dismiss();
#endif

    public static void LaunchUrl(string url)
    {
#if UNITY_IOS
		launchUrl(url);
#endif
    }

    public static void Dismiss()
    {
#if UNITY_IOS
        dismiss();
#endif
    }
}
