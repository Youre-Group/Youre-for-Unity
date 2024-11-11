/*
 * Copyright (C) 2024 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */


using YourePlugin;

namespace Auth
{
    public class AndroidChromeCustomTabBrowser : MobileBrowser
    {
        protected override void Launch(string url)
        {
            AndroidChromeCustomTab.LaunchUrl(url);
        }
    }
}
