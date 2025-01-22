/*
 * Copyright (C) 2024 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */


namespace Auth
{
    public class SFSafariViewBrowser : Browser
    {
        protected override void Launch(string url)
        {
            SFSafariView.LaunchUrl(url);
        }

        public override void Dismiss()
        {
            SFSafariView.Dismiss();
        }
    }
}
