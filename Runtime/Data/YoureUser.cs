/*
 * Copyright (C) 2023 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using Newtonsoft.Json;

namespace YourePlugin.Data
{
    [Serializable]
    public class YoureUser
    {
        [JsonProperty("sub"), JsonRequired]
        public string Sub;
        public string AccessToken { get; set; }
        public string Id => Sub.Split('|')[1];

        [JsonProperty("email")] 
        public string Email;

        [JsonProperty("username")] 
        public string UserName;

        [JsonProperty("email_verified")] 
        public string EmailVerified;
        
        [JsonProperty("newsletter")] 
        public bool NewsletterAccepted;
        
        [JsonProperty("terms")] 
        public bool TermsAccepted;
    }
}